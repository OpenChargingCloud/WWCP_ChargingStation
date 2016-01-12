/*
 * Copyright (c) 2014-2016 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP Cloud <https://github.com/GraphDefined/WWCP_Cloud>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.WWCP.EMSP
{

    /// <summary>
    /// A local E-Mobility service implementation.
    /// </summary>
    public class LocalEMobilityService : IAuthServices
    {

        #region Data

        private readonly ConcurrentDictionary<Auth_Token,         TokenAuthorizationResultType>  AuthorizationDatabase;
        private readonly ConcurrentDictionary<ChargingSession_Id, SessionInfo>                   SessionDatabase;

        #endregion

        #region Properties

        #region Id

        private readonly String _Id;

        public String Id
        {
            get
            {
                return _Id;
            }
        }

        #endregion

        #region RoamingNetwork

        private readonly RoamingNetwork _RoamingNetwork;

        public RoamingNetwork RoamingNetwork
        {
            get
            {
                return _RoamingNetwork;
            }
        }

        #endregion

        #region EVSP

        private readonly EVSP _EVSP;

        public EVSP EVSP
        {
            get
            {
                return _EVSP;
            }
        }

        #endregion

        #region AuthorizatorId

        private readonly Authorizator_Id _AuthorizatorId;

        public Authorizator_Id AuthorizatorId
        {
            get
            {
                return _AuthorizatorId;
            }
        }

        #endregion


        #region AllTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> AllTokens
        {
            get
            {
                return AuthorizationDatabase;
            }
        }

        #endregion

        #region AuthorizedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> AuthorizedTokens
        {
            get
            {
                return AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.Authorized);
            }
        }

        #endregion

        #region NotAuthorizedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> NotAuthorizedTokens
        {
            get
            {
                return AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.NotAuthorized);
            }
        }

        #endregion

        #region BlockedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> BlockedTokens
        {
            get
            {
                return AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.Blocked);
            }
        }

        #endregion

        #endregion

        #region Events

        #region OnEVSEDataPush/-Pushed

        /// <summary>
        /// An event fired whenever new EVSE data will be send upstream.
        /// </summary>
        public event OnEVSEDataPushDelegate OnEVSEDataPush;

        /// <summary>
        /// An event fired whenever new EVSE data had been sent upstream.
        /// </summary>
        public event OnEVSEDataPushedDelegate OnEVSEDataPushed;

        #endregion

        #region OnEVSEStatusPush/-Pushed

        /// <summary>
        /// An event fired whenever new EVSE status will be send upstream.
        /// </summary>
        public event OnEVSEStatusPushDelegate OnEVSEStatusPush;

        /// <summary>
        /// An event fired whenever new EVSE status had been sent upstream.
        /// </summary>
        public event OnEVSEStatusPushedDelegate OnEVSEStatusPushed;

        #endregion

        #endregion

        #region Constructor(s)

        internal LocalEMobilityService(EVSP             EVSP,
                                       Authorizator_Id  AuthorizatorId = null)
        {

            this._RoamingNetwork        = EVSP.RoamingNetwork;
            this._EVSP                  = EVSP;
            this._AuthorizatorId        = (AuthorizatorId == null) ? Authorizator_Id.Parse("eMI3 Local E-Mobility Database") : AuthorizatorId;

            this.AuthorizationDatabase  = new ConcurrentDictionary<Auth_Token,         TokenAuthorizationResultType>();
            this.SessionDatabase        = new ConcurrentDictionary<ChargingSession_Id, SessionInfo>();

            EVSP.EMobilityService = this;

        }

        #endregion


        #region User and credential management

        #region AddToken(Token, AuthenticationResult = AuthenticationResult.Allowed)

        public Boolean AddToken(Auth_Token                    Token,
                                TokenAuthorizationResultType  AuthenticationResult = TokenAuthorizationResultType.Authorized)
        {

            if (!AuthorizationDatabase.ContainsKey(Token))
                return AuthorizationDatabase.TryAdd(Token, AuthenticationResult);

            return false;

        }

        #endregion

        #region RemoveToken(Token)

        public Boolean RemoveToken(Auth_Token Token)
        {

            TokenAuthorizationResultType _AuthorizationResult;

            return AuthorizationDatabase.TryRemove(Token, out _AuthorizationResult);

        }

        #endregion

        #endregion

        #region Incoming from the roaming network

        #region PushEVSEData(GroupedEVSEs,     ActionType = fullLoad, OperatorId = null, OperatorName = null,                      QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given lookup of EVSEs grouped by their EVSE operator.
        /// </summary>
        /// <param name="GroupedEVSEs">A lookup of EVSEs grouped by their EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(ILookup<EVSEOperator, EVSE>  GroupedEVSEs,
                         ActionType                   ActionType    = ActionType.fullLoad,
                         EVSEOperator_Id              OperatorId    = null,
                         String                       OperatorName  = null,
                         TimeSpan?                    QueryTimeout  = null)

        {

            #region Initial checks

            if (GroupedEVSEs == null)
                throw new ArgumentNullException("GroupedEVSEs", "The given lookup of EVSEs must not be null!");

            #endregion

            #region Get effective number of EVSE data records to upload

            Acknowledgement Acknowledgement = null;

            var NumberOfEVSEs = GroupedEVSEs.
                                    Select(group => group.Count()).
                                    Sum   ();

            var StartTime = DateTime.Now;

            #endregion


            if (NumberOfEVSEs > 0)
            {

                #region Send OnEVSEDataPush event

                var OnEVSEDataPushLocal = OnEVSEDataPush;
                if (OnEVSEDataPushLocal != null)
                    OnEVSEDataPushLocal(StartTime, this, this.Id.ToString(), this.RoamingNetwork.Id, ActionType, GroupedEVSEs, (UInt32) NumberOfEVSEs);

                #endregion

                //var result = await _CPORoaming.PushEVSEData(GroupedEVSEs.
                //                                                SelectMany(group => group).
                //                                                ToLookup  (evse  => evse.ChargingStation.ChargingPool.EVSEOperator,
                //                                                           evse  => evse.AsOICPEVSEDataRecord(_EVSEDataRecordProcessing)),
                //                                            ActionType.AsOICPActionType(),
                //                                            OperatorId,
                //                                            OperatorName,
                //                                            QueryTimeout);
                //
                //if (result.Result == true)
                    Acknowledgement = new Acknowledgement(true);

                //else
                //    Acknowledgement = new Acknowledgement(false, result.StatusCode.Description);

            }

            else
                Acknowledgement = new Acknowledgement(true);


            #region Send OnEVSEDataPushed event

            var EndTime = DateTime.Now;

            var OnEVSEDataPushedLocal = OnEVSEDataPushed;
            if (OnEVSEDataPushedLocal != null)
                OnEVSEDataPushedLocal(EndTime, this, this.Id.ToString(), this.RoamingNetwork.Id, ActionType, GroupedEVSEs, (UInt32) NumberOfEVSEs, Acknowledgement, EndTime - StartTime);

            #endregion

            return Acknowledgement;

        }

        #endregion

        #region PushEVSEData(EVSE,             ActionType = fullLoad, OperatorId = null, OperatorName = null,                      QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given EVSE.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(EVSE                 EVSE,
                         ActionType           ActionType    = ActionType.fullLoad,
                         EVSEOperator_Id      OperatorId    = null,
                         String               OperatorName  = null,
                         TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (EVSE == null)
                throw new ArgumentNullException("EVSE", "The given EVSE must not be null!");

            #endregion

            return await PushEVSEData(new EVSE[] { EVSE },
                                      ActionType,
                                      OperatorId,
                                      OperatorName.IsNotNullOrEmpty()
                                          ? OperatorName
                                          : EVSE.EVSEOperator.Name.Any()
                                                ? EVSE.EVSEOperator.Name.FirstText
                                                : null,
                                      null,
                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEData(EVSEs,            ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given enumeration of EVSEs.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(IEnumerable<EVSE>    EVSEs,
                         ActionType           ActionType    = ActionType.fullLoad,
                         EVSEOperator_Id      OperatorId    = null,
                         String               OperatorName  = null,
                         Func<EVSE, Boolean>  IncludeEVSEs  = null,
                         TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (EVSEs == null)
                throw new ArgumentNullException("EVSEs", "The given enumeration of EVSEs must not be null!");

            if (IncludeEVSEs == null)
                IncludeEVSEs = EVSE => true;

            #endregion

            #region Get effective number of EVSE status to upload

            var _EVSEs = EVSEs.
                             Where(evse => IncludeEVSEs(evse)).
                             ToArray();

            #endregion


            if (_EVSEs.Any())
                return await PushEVSEData(_EVSEs.ToLookup(evse => evse.ChargingStation.ChargingPool.EVSEOperator,
                                                          evse => evse),
                                          ActionType,
                                          OperatorId,
                                          OperatorName,
                                          QueryTimeout);

            return new Acknowledgement(true);

        }

        #endregion

        #region PushEVSEData(ChargingStation,  ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given charging station.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(ChargingStation      ChargingStation,
                         ActionType           ActionType    = ActionType.fullLoad,
                         EVSEOperator_Id      OperatorId    = null,
                         String               OperatorName  = null,
                         Func<EVSE, Boolean>  IncludeEVSEs  = null,
                         TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException("ChargingStation", "The given charging station must not be null!");

            #endregion

            return await PushEVSEData(ChargingStation.EVSEs,
                                      ActionType,
                                      OperatorId   != null ? OperatorId   : ChargingStation.ChargingPool.EVSEOperator.Id,
                                      OperatorName != null ? OperatorName : ChargingStation.ChargingPool.EVSEOperator.Name.FirstText,
                                      IncludeEVSEs,
                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEData(ChargingStations, ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given charging stations.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(IEnumerable<ChargingStation>  ChargingStations,
                         ActionType                    ActionType    = ActionType.fullLoad,
                         EVSEOperator_Id               OperatorId    = null,
                         String                        OperatorName  = null,
                         Func<EVSE, Boolean>           IncludeEVSEs  = null,
                         TimeSpan?                     QueryTimeout  = null)

        {

            #region Initial checks

            if (ChargingStations == null)
                throw new ArgumentNullException("ChargingStations", "The given enumeration of charging stations must not be null!");

            #endregion

            return await PushEVSEData(ChargingStations.SelectMany(station => station.EVSEs),
                                      ActionType,
                                      OperatorId,
                                      OperatorName,
                                      IncludeEVSEs,
                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEData(ChargingPool,     ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given charging pool.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(ChargingPool         ChargingPool,
                         ActionType           ActionType    = ActionType.fullLoad,
                         EVSEOperator_Id      OperatorId    = null,
                         String               OperatorName  = null,
                         Func<EVSE, Boolean>  IncludeEVSEs  = null,
                         TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException("ChargingPool", "The given charging pool must not be null!");

            #endregion

            return await PushEVSEData(ChargingPool.EVSEs,
                                      ActionType,
                                      OperatorId   != null ? OperatorId   : ChargingPool.EVSEOperator.Id,
                                      OperatorName != null ? OperatorName : ChargingPool.EVSEOperator.Name.FirstText,
                                      IncludeEVSEs,
                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEData(ChargingPools,    ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given charging pools.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(IEnumerable<ChargingPool>  ChargingPools,
                         ActionType                 ActionType    = ActionType.fullLoad,
                         EVSEOperator_Id            OperatorId    = null,
                         String                     OperatorName  = null,
                         Func<EVSE, Boolean>        IncludeEVSEs  = null,
                         TimeSpan?                  QueryTimeout  = null)

        {

            #region Initial checks

            if (ChargingPools == null)
                throw new ArgumentNullException("ChargingPools", "The given enumeration of charging pools must not be null!");

            #endregion

            return await PushEVSEData(ChargingPools.SelectMany(pool    => pool.ChargingStations).
                                                    SelectMany(station => station.EVSEs),
                                      ActionType,
                                      OperatorId,
                                      OperatorName,
                                      IncludeEVSEs,
                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEData(EVSEOperator,     ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given EVSE operator.
        /// </summary>
        /// <param name="EVSEOperator">An EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(EVSEOperator         EVSEOperator,
                         ActionType           ActionType    = ActionType.fullLoad,
                         EVSEOperator_Id      OperatorId    = null,
                         String               OperatorName  = null,
                         Func<EVSE, Boolean>  IncludeEVSEs  = null,
                         TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (EVSEOperator == null)
                throw new ArgumentNullException("EVSEOperator", "The given EVSE operator must not be null!");

            #endregion

            return await PushEVSEData(new EVSEOperator[] { EVSEOperator },
                                      ActionType,
                                      OperatorId,
                                      OperatorName,
                                      IncludeEVSEs,
                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEData(EVSEOperators,    ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given EVSE operators.
        /// </summary>
        /// <param name="EVSEOperators">An enumeration of EVSE operators.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId"></param>
        /// <param name="OperatorName">An optional alternative EVSE operator name used for uploading all EVSEs.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        /// <returns></returns>
        public async Task<Acknowledgement>

            PushEVSEData(IEnumerable<EVSEOperator>             EVSEOperators,
                         ActionType                            ActionType    = ActionType.fullLoad,
                         EVSEOperator_Id                       OperatorId    = null,
                         String                                OperatorName  = null,
                         Func<EVSE, Boolean>                   IncludeEVSEs  = null,
                         TimeSpan?                             QueryTimeout  = null)

        {

            #region Initial checks

            if (EVSEOperators == null)
                throw new ArgumentNullException("EVSEOperators",  "The given enumeration of EVSE operators must not be null!");

            #endregion

            return await PushEVSEData(EVSEOperators.SelectMany(evseoperator => evseoperator.ChargingPools).
                                                    SelectMany(pool         => pool.ChargingStations).
                                                    SelectMany(station      => station.EVSEs),
                                      ActionType,
                                      OperatorId,
                                      OperatorName,
                                      IncludeEVSEs,
                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEData(RoamingNetwork,   ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given roaming network.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(RoamingNetwork       RoamingNetwork,
                         ActionType           ActionType    = ActionType.fullLoad,
                         EVSEOperator_Id      OperatorId    = null,
                         String               OperatorName  = null,
                         Func<EVSE, Boolean>  IncludeEVSEs  = null,
                         TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (RoamingNetwork == null)
                throw new ArgumentNullException("RoamingNetwork", "The given roaming network must not be null!");

            #endregion

            return await PushEVSEData(RoamingNetwork.EVSEs,
                                      ActionType,
                                      OperatorId,
                                      OperatorName,
                                      IncludeEVSEs,
                                      QueryTimeout);

        }

        #endregion


        #region PushEVSEStatus(GroupedEVSEs,     ActionType = update, OperatorId = null, OperatorName = null,                      QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE status of the given lookup of EVSE status types grouped by their EVSE operator.
        /// </summary>
        /// <param name="GroupedEVSEs">A lookup of EVSEs grouped by their EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(ILookup<EVSEOperator, EVSE>  GroupedEVSEs,
                           ActionType                   ActionType    = ActionType.update,
                           EVSEOperator_Id              OperatorId    = null,
                           String                       OperatorName  = null,
                           TimeSpan?                    QueryTimeout  = null)

        {

            #region Initial checks

            if (GroupedEVSEs == null)
                throw new ArgumentNullException("GroupedEVSEStatusTypes", "The given lookup of EVSE status types must not be null!");

            #endregion

            #region Get effective number of EVSE status to upload

            Acknowledgement Acknowledgement = null;

            var NumberOfEVSEStatus = GroupedEVSEs.
                                         Select(group => group.Count()).
                                         Sum();

            var StartTime = DateTime.Now;

            #endregion


            if (NumberOfEVSEStatus > 0)
            {

                #region Send OnEVSEStatusPush event

                var OnEVSEStatusPushLocal = OnEVSEStatusPush;
                if (OnEVSEStatusPushLocal != null)
                    OnEVSEStatusPushLocal(StartTime, this, this.Id.ToString(), this.RoamingNetwork.Id, ActionType, GroupedEVSEs, (UInt32) NumberOfEVSEStatus);

                #endregion

              //  var result = await _CPORoaming.PushEVSEStatus(GroupedEVSEs.
              //                                                    SelectMany(group => group).
              //                                                    ToLookup  (evse  => evse.ChargingStation.ChargingPool.EVSEOperator.Id,
              //                                                               evse  => new EVSEStatusRecord(evse.Id, evse.Status.Value.AsOICPEVSEStatus())),
              //                                                ActionType.AsOICPActionType(),
              //                                                OperatorId,
              //                                                OperatorName,
              //                                                QueryTimeout);
              //
              //  if (result.Result == true)
                    Acknowledgement = new Acknowledgement(true);

              //  else
              //      Acknowledgement = new Acknowledgement(false, result.StatusCode.Description);

            }

            else
                Acknowledgement = new Acknowledgement(true);


            #region Send OnEVSEStatusPushed event

            var EndTime = DateTime.Now;

            var OnEVSEStatusPushedLocal = OnEVSEStatusPushed;
            if (OnEVSEStatusPushedLocal != null)
                OnEVSEStatusPushedLocal(EndTime, this, this.Id.ToString(), this.RoamingNetwork.Id, ActionType, GroupedEVSEs, (UInt32) NumberOfEVSEStatus, Acknowledgement, EndTime - StartTime);

            #endregion

            return Acknowledgement;

        }

        #endregion

        #region PushEVSEStatus(EVSE,             ActionType = update, OperatorId = null, OperatorName = null,                      QueryTimeout = null)

        /// <summary>
        /// Upload the given EVSE.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(EVSE                 EVSE,
                           ActionType           ActionType    = ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (EVSE == null)
                throw new ArgumentNullException("EVSE", "The given EVSE must not be null!");

            #endregion

            return await PushEVSEStatus(new EVSE[] { EVSE },
                                        ActionType,
                                        OperatorId,
                                        OperatorName,
                                        null,
                                        QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEs,            ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the status of the given enumeration of EVSEs.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(IEnumerable<EVSE>    EVSEs,
                           ActionType           ActionType    = ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           Func<EVSE, Boolean>  IncludeEVSEs  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (EVSEs == null)
                throw new ArgumentNullException("EVSEs", "The given enumeration of EVSEs must not be null!");

            if (IncludeEVSEs == null)
                IncludeEVSEs = EVSE => true;

            #endregion

            #region Get effective number of EVSE status to upload

            var _EVSEs = EVSEs.
                             Where(evse => IncludeEVSEs(evse)).
                             ToArray();

            #endregion


            if (_EVSEs.Any())
                return await PushEVSEStatus(_EVSEs.ToLookup(evse => evse.ChargingStation.ChargingPool.EVSEOperator,
                                                            evse => evse),
                                            ActionType,
                                            OperatorId,
                                            OperatorName,
                                            QueryTimeout);

            return new Acknowledgement(true);

        }

        #endregion

        #region PushEVSEStatus(ChargingStation,  ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE status of the given charging station.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(ChargingStation      ChargingStation,
                           ActionType           ActionType    = ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           Func<EVSE, Boolean>  IncludeEVSEs  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException("ChargingStation", "The given charging station must not be null!");

            #endregion

            return await PushEVSEStatus(ChargingStation.EVSEs,
                                        ActionType,
                                        OperatorId   != null ? OperatorId   : ChargingStation.ChargingPool.EVSEOperator.Id,
                                        OperatorName != null ? OperatorName : ChargingStation.ChargingPool.EVSEOperator.Name.FirstText,
                                        IncludeEVSEs,
                                        QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(ChargingStations, ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE status of the given charging stations.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(IEnumerable<ChargingStation>  ChargingStations,
                           ActionType                    ActionType    = ActionType.update,
                           EVSEOperator_Id               OperatorId    = null,
                           String                        OperatorName  = null,
                           Func<EVSE, Boolean>           IncludeEVSEs  = null,
                           TimeSpan?                     QueryTimeout  = null)

        {

            #region Initial checks

            if (ChargingStations == null)
                throw new ArgumentNullException("ChargingStations", "The given enumeration of charging stations must not be null!");

            #endregion

            return await PushEVSEStatus(ChargingStations.SelectMany(station => station.EVSEs),
                                        ActionType,
                                        OperatorId,
                                        OperatorName,
                                        IncludeEVSEs,
                                        QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(ChargingPool,     ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE status of the given charging pool.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(ChargingPool         ChargingPool,
                           ActionType           ActionType    = ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           Func<EVSE, Boolean>  IncludeEVSEs  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException("ChargingPool", "The given charging pool must not be null!");

            #endregion

            return await PushEVSEStatus(ChargingPool.EVSEs,
                                        ActionType,
                                        OperatorId   != null ? OperatorId   : ChargingPool.EVSEOperator.Id,
                                        OperatorName != null ? OperatorName : ChargingPool.EVSEOperator.Name.FirstText,
                                        IncludeEVSEs,
                                        QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(ChargingPools,    ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE status of the given charging pools.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(IEnumerable<ChargingPool>  ChargingPools,
                           ActionType                 ActionType    = ActionType.update,
                           EVSEOperator_Id            OperatorId    = null,
                           String                     OperatorName  = null,
                           Func<EVSE, Boolean>        IncludeEVSEs  = null,
                           TimeSpan?                  QueryTimeout  = null)

        {

            #region Initial checks

            if (ChargingPools == null)
                throw new ArgumentNullException("ChargingPools", "The given enumeration of charging pools must not be null!");

            #endregion

            return await PushEVSEStatus(ChargingPools.SelectMany(pool    => pool.ChargingStations).
                                                      SelectMany(station => station.EVSEs),
                                        ActionType,
                                        OperatorId,
                                        OperatorName,
                                        IncludeEVSEs,
                                        QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEOperator,     ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE status of the given EVSE operator.
        /// </summary>
        /// <param name="EVSEOperator">An EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(EVSEOperator         EVSEOperator,
                           ActionType           ActionType    = ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           Func<EVSE, Boolean>  IncludeEVSEs  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (EVSEOperator == null)
                throw new ArgumentNullException("EVSEOperator", "The given EVSE operator must not be null!");

            #endregion

            return await PushEVSEStatus(EVSEOperator.AllEVSEs,
                                        ActionType,
                                        EVSEOperator.Id,
                                        OperatorName.IsNotNullOrEmpty()
                                            ? OperatorName
                                            : EVSEOperator.Name.FirstText,
                                        IncludeEVSEs,
                                        QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEOperators,    ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE status of the given EVSE operators.
        /// </summary>
        /// <param name="EVSEOperators">An enumeration of EVSES operators.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(IEnumerable<EVSEOperator>  EVSEOperators,
                           ActionType                 ActionType    = ActionType.update,
                           EVSEOperator_Id            OperatorId    = null,
                           String                     OperatorName  = null,
                           Func<EVSE, Boolean>        IncludeEVSEs  = null,
                           TimeSpan?                  QueryTimeout  = null)

        {

            #region Initial checks

            if (EVSEOperators == null)
                throw new ArgumentNullException("EVSEOperator", "The given enumeration of EVSE operators must not be null!");

            #endregion

            return await PushEVSEStatus(EVSEOperators.SelectMany(evseoperator => evseoperator.ChargingPools).
                                                      SelectMany(pool         => pool.ChargingStations).
                                                      SelectMany(station      => station.EVSEs),
                                        ActionType,
                                        OperatorId,
                                        OperatorName,
                                        IncludeEVSEs,
                                        QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(RoamingNetwork,   ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE status of the given roaming network.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(RoamingNetwork       RoamingNetwork,
                           ActionType           ActionType    = ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           Func<EVSE, Boolean>  IncludeEVSEs  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (RoamingNetwork == null)
                throw new ArgumentNullException("RoamingNetwork", "The given roaming network must not be null!");

            #endregion

            return await PushEVSEStatus(RoamingNetwork.EVSEs,
                                        ActionType,
                                        OperatorId,
                                        OperatorName,
                                        IncludeEVSEs,
                                        QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEStatusDiff, QueryTimeout = null)

        /// <summary>
        /// Send EVSE status updates.
        /// </summary>
        /// <param name="EVSEStatusDiff">An EVSE status diff.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task PushEVSEStatus(EVSEStatusDiff  EVSEStatusDiff,
                                         TimeSpan?       QueryTimeout  = null)

        {

            await Task.FromResult("");

        }

        #endregion


        #region AuthorizeStart(OperatorId, AuthToken, ChargingProductId = null, SessionId = null, QueryTimeout = null)

        /// <summary>
        /// Create an authorize start request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingProductId">An optional charging product identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<AuthStartResult> AuthorizeStart(EVSEOperator_Id     OperatorId,
                                                          Auth_Token          AuthToken,
                                                          ChargingProduct_Id  ChargingProductId  = null,
                                                          ChargingSession_Id  SessionId          = null,
                                                          TimeSpan?           QueryTimeout       = null)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException("OperatorId", "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException("AuthToken",  "The given parameter must not be null!");

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    var _SessionId = ChargingSession_Id.New;

                    SessionDatabase.TryAdd(_SessionId, new SessionInfo(AuthToken));

                    return AuthStartResult.Authorized(AuthorizatorId,
                                                      _SessionId,
                                                      EVSP.Id);

                }

                #endregion

                #region Token is blocked!

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStartResult.Blocked(AuthorizatorId,
                                                   EVSP.Id,
                                                   "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStartResult.Unspecified(AuthorizatorId);

                #endregion

            }

            #region Unkown Token!

            return AuthStartResult.NotAuthorized(AuthorizatorId,
                                                 EVSP.Id,
                                                 "Unkown token!");

            #endregion

        }

        #endregion

        #region AuthorizeStart(OperatorId, AuthToken, EVSEId, ChargingProductId = null, SessionId = null, QueryTimeout = null)

        /// <summary>
        /// Create an authorize start request at the given EVSE.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ChargingProductId">An optional charging product identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<AuthStartEVSEResult> AuthorizeStart(EVSEOperator_Id     OperatorId,
                                                              Auth_Token          AuthToken,
                                                              EVSE_Id             EVSEId             = null,
                                                              ChargingProduct_Id  ChargingProductId  = null,
                                                              ChargingSession_Id  SessionId          = null,
                                                              TimeSpan?           QueryTimeout       = null)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException("OperatorId", "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException("AuthToken",  "The given parameter must not be null!");

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    var _SessionId = ChargingSession_Id.New;

                    SessionDatabase.TryAdd(_SessionId, new SessionInfo(AuthToken));

                    return AuthStartEVSEResult.Authorized(AuthorizatorId,
                                                          _SessionId,
                                                          EVSP.Id);

                }

                #endregion

                #region Token is blocked!

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStartEVSEResult.Blocked(AuthorizatorId,
                                                       EVSP.Id,
                                                       "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStartEVSEResult.Unspecified(AuthorizatorId);

                #endregion

            }

            #region Unkown Token!

            return AuthStartEVSEResult.NotAuthorized(AuthorizatorId,
                                                     EVSP.Id,
                                                     "Unkown token!");

            #endregion

        }

        #endregion

        #region AuthorizeStart(OperatorId, AuthToken, ChargingStationId, ChargingProductId = null, SessionId = null, QueryTimeout = null)

        /// <summary>
        /// Create an AuthorizeStart request at the given charging station.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingStationId">The unique identification of a charging station.</param>
        /// <param name="ChargingProductId">An optional charging product identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<AuthStartChargingStationResult>

            AuthorizeStart(EVSEOperator_Id     OperatorId,
                           Auth_Token          AuthToken,
                           ChargingStation_Id  ChargingStationId,
                           ChargingProduct_Id  ChargingProductId  = null,   // [maxlength: 100]
                           ChargingSession_Id  SessionId          = null,
                           TimeSpan?           QueryTimeout       = null)

        {

            #region Initial checks

            if (OperatorId        == null)
                throw new ArgumentNullException("OperatorId",         "The given parameter must not be null!");

            if (AuthToken         == null)
                throw new ArgumentNullException("AuthToken",          "The given parameter must not be null!");

            if (ChargingStationId == null)
                throw new ArgumentNullException("ChargingStationId",  "The given parameter must not be null!");

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    var _SessionId = ChargingSession_Id.New;

                    SessionDatabase.TryAdd(_SessionId, new SessionInfo(AuthToken));

                    return AuthStartChargingStationResult.Authorized(AuthorizatorId,
                                                                     _SessionId,
                                                                     EVSP.Id);

                }

                #endregion

                #region Token is blocked!

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStartChargingStationResult.Blocked(AuthorizatorId,
                                                                  EVSP.Id,
                                                                  "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStartChargingStationResult.Unspecified(AuthorizatorId);

                #endregion

            }

            #region Unkown Token!

            return AuthStartChargingStationResult.NotAuthorized(AuthorizatorId,
                                                                EVSP.Id,
                                                                "Unkown token!");

            #endregion

        }

        #endregion


        #region AuthorizeStop(OperatorId, SessionId, AuthToken, QueryTimeout = null)

        /// <summary>
        /// Create an authorize stop request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<AuthStopResult> AuthorizeStop(EVSEOperator_Id     OperatorId,
                                                        ChargingSession_Id  SessionId,
                                                        Auth_Token          AuthToken,
                                                        TimeSpan?           QueryTimeout  = null)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException("OperatorId", "The given parameter must not be null!");

            if (SessionId  == null)
                throw new ArgumentNullException("SessionId",  "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException("AuthToken",  "The given parameter must not be null!");

            #endregion

            #region Check session identification

            SessionInfo SessionInfo = null;

            if (!SessionDatabase.TryGetValue(SessionId, out SessionInfo))
                return AuthStopResult.InvalidSessionId(AuthorizatorId);

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Token is authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    // Authorized
                    if (SessionInfo.ListOfAuthStopTokens.Contains(AuthToken))
                        return AuthStopResult.Authorized(AuthorizatorId,
                                                         EVSP.Id);

                    // Invalid Token for SessionId!
                    else
                        return AuthStopResult.NotAuthorized(AuthorizatorId,
                                                            EVSP.Id,
                                                            "Invalid token for given session identification!");

                }

                #endregion

                #region Token is blocked

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStopResult.Blocked(AuthorizatorId,
                                                  EVSP.Id,
                                                  "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStopResult.Unspecified(AuthorizatorId);

                #endregion

            }

            // Unkown Token!
            return AuthStopResult.NotAuthorized(AuthorizatorId,
                                                EVSP.Id,
                                                "Unkown token!");

        }

        #endregion

        #region AuthorizeStop(OperatorId, EVSEId, SessionId, AuthToken, QueryTimeout = null)

        /// <summary>
        /// Create an authorize stop request at the given EVSE.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<AuthStopEVSEResult> AuthorizeStop(EVSEOperator_Id     OperatorId,
                                                            EVSE_Id             EVSEId,
                                                            ChargingSession_Id  SessionId,
                                                            Auth_Token          AuthToken,
                                                            TimeSpan?           QueryTimeout  = null)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException("OperatorId", "The given parameter must not be null!");

            if (SessionId  == null)
                throw new ArgumentNullException("SessionId",  "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException("AuthToken",  "The given parameter must not be null!");

            if (EVSEId == null)
                throw new ArgumentNullException("EVSEId",     "The given parameter must not be null!");

            #endregion

            #region Check session identification

            SessionInfo SessionInfo = null;

            if (!SessionDatabase.TryGetValue(SessionId, out SessionInfo))
                return AuthStopEVSEResult.InvalidSessionId(AuthorizatorId);

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Token is authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    // Authorized
                    if (SessionInfo.ListOfAuthStopTokens.Contains(AuthToken))
                        return AuthStopEVSEResult.Authorized(AuthorizatorId,
                                                             EVSP.Id);

                    // Invalid Token for SessionId!
                    else
                        return AuthStopEVSEResult.NotAuthorized(AuthorizatorId,
                                                                EVSP.Id,
                                                                "Invalid token for given session identification!");

                }

                #endregion

                #region Token is blocked

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStopEVSEResult.Blocked(AuthorizatorId,
                                                      EVSP.Id,
                                                      "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStopEVSEResult.Unspecified(AuthorizatorId);

                #endregion

            }

            // Unkown Token!
            return AuthStopEVSEResult.NotAuthorized(AuthorizatorId,
                                                    EVSP.Id,
                                                    "Unkown token!");

        }

        #endregion

        #region AuthorizeStop(OperatorId, ChargingStationId, SessionId, AuthToken, QueryTimeout = null)

        /// <summary>
        /// Create an authorize stop request at the given charging station.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="ChargingStationId">A charging station identification.</param>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<AuthStopChargingStationResult> AuthorizeStop(EVSEOperator_Id     OperatorId,
                                                                       ChargingStation_Id  ChargingStationId,
                                                                       ChargingSession_Id  SessionId,
                                                                       Auth_Token          AuthToken,
                                                                       TimeSpan?           QueryTimeout  = null)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException("OperatorId", "The given parameter must not be null!");

            if (SessionId  == null)
                throw new ArgumentNullException("SessionId",  "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException("AuthToken",  "The given parameter must not be null!");

            #endregion

            return AuthStopChargingStationResult.Error(AuthorizatorId);

        }

        #endregion


        #region SendChargeDetailRecord(EVSEId, SessionId, SessionStart, SessionEnd, PartnerProductId, ..., QueryTimeout = null)

        /// <summary>
        /// Create a SendChargeDetailRecord request.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification.</param>
        /// <param name="SessionId">The session identification from the Authorize Start request.</param>
        /// <param name="PartnerProductId">An optional charging product identification.</param>
        /// <param name="SessionStart">The timestamp of the session start.</param>
        /// <param name="SessionEnd">The timestamp of the session end.</param>
        /// <param name="ChargingStart">An optional charging start timestamp.</param>
        /// <param name="ChargingEnd">An optional charging end timestamp.</param>
        /// <param name="MeterValueStart">An optional initial value of the energy meter.</param>
        /// <param name="MeterValueEnd">An optional final value of the energy meter.</param>
        /// <param name="MeterValuesInBetween">An optional enumeration of meter values during the charging session.</param>
        /// <param name="ConsumedEnergy">The optional amount of consumed energy.</param>
        /// <param name="MeteringSignature">An optional signature for the metering values.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<SendCDRResult>

            SendChargeDetailRecord(EVSE_Id              EVSEId,
                                   ChargingSession_Id   SessionId,
                                   ChargingProduct_Id   PartnerProductId,
                                   DateTime             SessionStart,
                                   DateTime             SessionEnd,
                                   AuthInfo             AuthInfo, // REMOVE ME!
                                   DateTime?            ChargingStart         = null,
                                   DateTime?            ChargingEnd           = null,
                                   Double?              MeterValueStart       = null,
                                   Double?              MeterValueEnd         = null,
                                   IEnumerable<Double>  MeterValuesInBetween  = null,
                                   Double?              ConsumedEnergy        = null,
                                   String               MeteringSignature     = null,
                                   TimeSpan?            QueryTimeout          = null)

        {

            #region Initial checks

            if (EVSEId           == null)
                throw new ArgumentNullException("EVSEId",            "The given parameter must not be null!");

            if (SessionId        == null)
                throw new ArgumentNullException("SessionId",         "The given parameter must not be null!");

            if (SessionStart     == null)
                throw new ArgumentNullException("SessionStart",      "The given parameter must not be null!");

            if (SessionEnd       == null)
                throw new ArgumentNullException("SessionEnd",        "The given parameter must not be null!");

            #endregion

            SessionInfo _SessionInfo = null;

            if (SessionDatabase.TryRemove(SessionId, out _SessionInfo))
                return SendCDRResult.Forwarded(AuthorizatorId);

            return SendCDRResult.InvalidSessionId(AuthorizatorId);

        }

        #endregion

        public async Task<SendCDRResult> SendChargeDetailRecord(ChargeDetailRecord ChargeDetailRecord, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            return SendCDRResult.Forwarded(_AuthorizatorId);
        }

        #endregion

        #region Outgoing to the roaming network

        #region RemoteStart

        #endregion

        #region RemoteStop

        #endregion

        #region Reserve

        #endregion

        #endregion


    }

}
