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
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.WWCP.EMSP
{

    /// <summary>
    /// An e-mobility service provider.
    /// </summary>
    public class eMobilityServiceProvider : IeMobilityServiceProvider
    {

        #region Data

        private readonly ConcurrentDictionary<Auth_Token,         TokenAuthorizationResultType>  AuthorizationDatabase;
        private readonly ConcurrentDictionary<ChargingSession_Id, SessionInfo>                   SessionDatabase;
        private readonly ConcurrentDictionary<ChargingSession_Id, ChargeDetailRecord>            ChargeDetailRecordDatabase;

        #endregion

        #region Properties

        #region Id

        private readonly EVSP_Id _Id;

        /// <summary>
        /// The unique identification of the e-mobility service provider.
        /// </summary>
        public EVSP_Id Id
            => _Id;

        #endregion

        #region RoamingNetwork

        private readonly RoamingNetwork _RoamingNetwork;

        public RoamingNetwork RoamingNetwork
            => _RoamingNetwork;

        #endregion

        #region EVSP

        private readonly EVSP _EVSP;

        public EVSP EVSP
            => _EVSP;

        #endregion

        #region AuthorizatorId

        private readonly Authorizator_Id _AuthorizatorId;

        public Authorizator_Id AuthorizatorId
            => _AuthorizatorId;

        #endregion


        #region AllTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> AllTokens
            => AuthorizationDatabase;

        #endregion

        #region AuthorizedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> AuthorizedTokens
            => AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.Authorized);

        #endregion

        #region NotAuthorizedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> NotAuthorizedTokens
            => AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.NotAuthorized);

        #endregion

        #region BlockedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> BlockedTokens
            => AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.Blocked);

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


        #region OnReserve... / OnReserved...

        /// <summary>
        /// An event fired whenever an EVSE is being reserved.
        /// </summary>
        public event OnEVSEReserveDelegate              OnReserveEVSE;

        /// <summary>
        /// An event fired whenever an EVSE was reserved.
        /// </summary>
        public event OnEVSEReservedDelegate             OnEVSEReserved;

        #endregion

        #region OnRemote...Start / OnRemote...Started

        /// <summary>
        /// An event fired whenever a remote start EVSE command was received.
        /// </summary>
        public event OnRemoteEVSEStartDelegate               OnRemoteEVSEStart;

        /// <summary>
        /// An event fired whenever a remote start EVSE command completed.
        /// </summary>
        public event OnRemoteEVSEStartedDelegate             OnRemoteEVSEStarted;

        #endregion

        #region OnRemote...Stop / OnRemote...Stopped

        /// <summary>
        /// An event fired whenever a remote stop EVSE command was received.
        /// </summary>
        public event OnRemoteEVSEStopDelegate                OnRemoteEVSEStop;

        /// <summary>
        /// An event fired whenever a remote stop EVSE command completed.
        /// </summary>
        public event OnRemoteEVSEStoppedDelegate             OnRemoteEVSEStopped;

        #endregion

        // CancelReservation

        #endregion

        #region Constructor(s)

        internal eMobilityServiceProvider(EVSP             EVSP,
                                          Authorizator_Id  AuthorizatorId = null)
        {

            this._Id                         = EVSP.Id;
            this._RoamingNetwork             = EVSP.RoamingNetwork;
            this._EVSP                       = EVSP;
            this._AuthorizatorId             = AuthorizatorId ?? Authorizator_Id.Parse("GraphDefined WWCP E-Mobility Database");

            this.AuthorizationDatabase       = new ConcurrentDictionary<Auth_Token,         TokenAuthorizationResultType>();
            this.SessionDatabase             = new ConcurrentDictionary<ChargingSession_Id, SessionInfo>();
            this.ChargeDetailRecordDatabase  = new ConcurrentDictionary<ChargingSession_Id, ChargeDetailRecord>();

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


        #region Incoming requests from the roaming network

        #region Receive incoming EVSEData

        private IPushData AsIPushData  => this;

        #region PushEVSEData(GroupedEVSEs,     ActionType = fullLoad, OperatorId, OperatorName, ...)

        /// <summary>
        /// Upload the EVSE data of the given lookup of EVSEs grouped by their EVSE operator.
        /// </summary>
        /// <param name="GroupedEVSEs">A lookup of EVSEs grouped by their EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushData.PushEVSEData(ILookup<EVSEOperator, EVSE>  GroupedEVSEs,
                                   ActionType                   ActionType,
                                   EVSEOperator_Id              OperatorId,
                                   String                       OperatorName,
                                   TimeSpan?                    QueryTimeout)

        {

            #region Initial checks

            if (GroupedEVSEs == null)
                throw new ArgumentNullException(nameof(GroupedEVSEs), "The given lookup of EVSEs must not be null!");

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

                OnEVSEDataPush?.Invoke(StartTime,
                                       this,
                                       this.Id.ToString(),
                                       this.RoamingNetwork.Id,
                                       ActionType,
                                       GroupedEVSEs,
                                       (UInt32) NumberOfEVSEs);

                #endregion

                //var result = await _CPORoaming.PushEVSEData(GroupedEVSEs.
                //                                                SelectMany(group => group).
                //                                                ToLookup  (evse  => evse.EVSEOperator,
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

            OnEVSEDataPushed?.Invoke(EndTime,
                                     this,
                                     this.Id.ToString(),
                                     this.RoamingNetwork.Id,
                                     ActionType,
                                     GroupedEVSEs,
                                     (UInt32) NumberOfEVSEs,
                                     Acknowledgement,
                                     EndTime - StartTime);

            #endregion

            return Acknowledgement;

        }

        #endregion

        #region PushEVSEData(EVSE,             ActionType = fullLoad, OperatorId, OperatorName, ...)

        /// <summary>
        /// Upload the EVSE data of the given EVSE.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushData.PushEVSEData(EVSE                 EVSE,
                                   ActionType           ActionType,
                                   EVSEOperator_Id      OperatorId,
                                   String               OperatorName,
                                   TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (EVSE == null)
                throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!");

            #endregion

            return await AsIPushData.PushEVSEData(new EVSE[] { EVSE },
                                                           ActionType,
                                                           OperatorId,
                                                           OperatorName.IsNotNullOrEmpty()
                                                               ? OperatorName
                                                               : EVSE.Operator.Name.Any()
                                                                     ? EVSE.Operator.Name.FirstText
                                                                     : null,
                                                           null,
                                                           QueryTimeout);

        }

        #endregion

        #region PushEVSEData(EVSEs,            ActionType = fullLoad, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload the EVSE data of the given enumeration of EVSEs.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushData.PushEVSEData(IEnumerable<EVSE>    EVSEs,
                                   ActionType           ActionType,
                                   EVSEOperator_Id      OperatorId,
                                   String               OperatorName,
                                   Func<EVSE, Boolean>  IncludeEVSEs,
                                   TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (EVSEs == null)
                throw new ArgumentNullException(nameof(EVSEs), "The given enumeration of EVSEs must not be null!");

            if (IncludeEVSEs == null)
                IncludeEVSEs = EVSE => true;

            #endregion

            #region Get effective number of EVSE status to upload

            var _EVSEs = EVSEs.
                             Where(evse => IncludeEVSEs(evse)).
                             ToArray();

            #endregion


            if (_EVSEs.Any())
                return await AsIPushData.PushEVSEData(_EVSEs.ToLookup(evse => evse.Operator,
                                                                               evse => evse),
                                                               ActionType,
                                                               OperatorId,
                                                               OperatorName,
                                                               QueryTimeout);

            return new Acknowledgement(true);

        }

        #endregion

        #region PushEVSEData(ChargingStation,  ActionType = fullLoad, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload the EVSE data of the given charging station.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushData.PushEVSEData(ChargingStation      ChargingStation,
                                   ActionType           ActionType,
                                   EVSEOperator_Id      OperatorId,
                                   String               OperatorName,
                                   Func<EVSE, Boolean>  IncludeEVSEs,
                                   TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation), "The given charging station must not be null!");

            #endregion

            return await AsIPushData.PushEVSEData(ChargingStation.EVSEs,
                                                           ActionType,
                                                           OperatorId   ?? ChargingStation.ChargingPool.Operator.Id,
                                                           OperatorName ?? ChargingStation.ChargingPool.Operator.Name.FirstText,
                                                           IncludeEVSEs,
                                                           QueryTimeout);

        }

        #endregion

        #region PushEVSEData(ChargingStations, ActionType = fullLoad, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload the EVSE data of the given charging stations.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushData.PushEVSEData(IEnumerable<ChargingStation>  ChargingStations,
                                   ActionType                    ActionType,
                                   EVSEOperator_Id               OperatorId,
                                   String                        OperatorName,
                                   Func<EVSE, Boolean>           IncludeEVSEs,
                                   TimeSpan?                     QueryTimeout)

        {

            #region Initial checks

            if (ChargingStations == null)
                throw new ArgumentNullException(nameof(ChargingStations), "The given enumeration of charging stations must not be null!");

            #endregion

            return await AsIPushData.PushEVSEData(ChargingStations.SelectMany(station => station.EVSEs),
                                                           ActionType,
                                                           OperatorId,
                                                           OperatorName,
                                                           IncludeEVSEs,
                                                           QueryTimeout);

        }

        #endregion

        #region PushEVSEData(ChargingPool,     ActionType = fullLoad, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload the EVSE data of the given charging pool.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushData.PushEVSEData(ChargingPool         ChargingPool,
                                   ActionType           ActionType,
                                   EVSEOperator_Id      OperatorId,
                                   String               OperatorName,
                                   Func<EVSE, Boolean>  IncludeEVSEs,
                                   TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException(nameof(ChargingPool), "The given charging pool must not be null!");

            #endregion

            return await AsIPushData.PushEVSEData(ChargingPool.EVSEs,
                                                           ActionType,
                                                           OperatorId   ?? ChargingPool.Operator.Id,
                                                           OperatorName ?? ChargingPool.Operator.Name.FirstText,
                                                           IncludeEVSEs,
                                                           QueryTimeout);

        }

        #endregion

        #region PushEVSEData(ChargingPools,    ActionType = fullLoad, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload the EVSE data of the given charging pools.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushData.PushEVSEData(IEnumerable<ChargingPool>  ChargingPools,
                                   ActionType                 ActionType,
                                   EVSEOperator_Id            OperatorId,
                                   String                     OperatorName,
                                   Func<EVSE, Boolean>        IncludeEVSEs,
                                   TimeSpan?                  QueryTimeout)

        {

            #region Initial checks

            if (ChargingPools == null)
                throw new ArgumentNullException(nameof(ChargingPools), "The given enumeration of charging pools must not be null!");

            #endregion

            return await AsIPushData.PushEVSEData(ChargingPools.SelectMany(pool    => pool.ChargingStations).
                                                                         SelectMany(station => station.EVSEs),
                                                           ActionType,
                                                           OperatorId,
                                                           OperatorName,
                                                           IncludeEVSEs,
                                                           QueryTimeout);

        }

        #endregion

        #region PushEVSEData(EVSEOperator,     ActionType = fullLoad, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload the EVSE data of the given EVSE operator.
        /// </summary>
        /// <param name="EVSEOperator">An EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushData.PushEVSEData(EVSEOperator         EVSEOperator,
                                   ActionType           ActionType,
                                   EVSEOperator_Id      OperatorId,
                                   String               OperatorName,
                                   Func<EVSE, Boolean>  IncludeEVSEs,
                                   TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (EVSEOperator == null)
                throw new ArgumentNullException(nameof(EVSEOperator), "The given EVSE operator must not be null!");

            #endregion

            return await AsIPushData.PushEVSEData(new EVSEOperator[] { EVSEOperator },
                                                           ActionType,
                                                           OperatorId,
                                                           OperatorName,
                                                           IncludeEVSEs,
                                                           QueryTimeout);

        }

        #endregion

        #region PushEVSEData(EVSEOperators,    ActionType = fullLoad, OperatorId, OperatorName, IncludeEVSEs, ...)

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
        async Task<Acknowledgement>

            IPushData.PushEVSEData(IEnumerable<EVSEOperator>  EVSEOperators,
                                   ActionType                 ActionType,
                                   EVSEOperator_Id            OperatorId,
                                   String                     OperatorName,
                                   Func<EVSE, Boolean>        IncludeEVSEs,
                                   TimeSpan?                  QueryTimeout)

        {

            #region Initial checks

            if (EVSEOperators == null)
                throw new ArgumentNullException(nameof(EVSEOperators),  "The given enumeration of EVSE operators must not be null!");

            #endregion

            return await AsIPushData.PushEVSEData(EVSEOperators.SelectMany(evseoperator => evseoperator.ChargingPools).
                                                                         SelectMany(pool         => pool.ChargingStations).
                                                                         SelectMany(station      => station.EVSEs),
                                                           ActionType,
                                                           OperatorId,
                                                           OperatorName,
                                                           IncludeEVSEs,
                                                           QueryTimeout);

        }

        #endregion

        #region PushEVSEData(RoamingNetwork,   ActionType = fullLoad, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload the EVSE data of the given roaming network.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushData.PushEVSEData(RoamingNetwork       RoamingNetwork,
                                   ActionType           ActionType,
                                   EVSEOperator_Id      OperatorId,
                                   String               OperatorName,
                                   Func<EVSE, Boolean>  IncludeEVSEs,
                                   TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (RoamingNetwork == null)
                throw new ArgumentNullException(nameof(RoamingNetwork), "The given roaming network must not be null!");

            #endregion

            return await AsIPushData.PushEVSEData(RoamingNetwork.EVSEs,
                                                           ActionType,
                                                           OperatorId,
                                                           OperatorName,
                                                           IncludeEVSEs,
                                                           QueryTimeout);

        }

        #endregion

        public void RemoveChargingStations(DateTime                      Timestamp,
                                           IEnumerable<ChargingStation>  ChargingStations)
        {

            foreach (var _ChargingStation in ChargingStations)
                Console.WriteLine(DateTime.Now + " LocalEMobilityService says: " + _ChargingStation.Id + " was removed!");

        }

        #endregion

        #region Receive incoming EVSEStatus

        private IPushStatus AsIPushStatus  => this;

        #region PushEVSEStatus(GroupedEVSEStatus, ActionType, OperatorId, OperatorName, ...)

        /// <summary>
        /// Upload the EVSE status of the given lookup of EVSE status types grouped by their EVSE operator.
        /// </summary>
        /// <param name="GroupedEVSEStatus">A lookup of EVSE status grouped by their EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(ILookup<EVSEOperator_Id, EVSEStatus>  GroupedEVSEStatus,
                                       ActionType                            ActionType,
                                       EVSEOperator_Id                       OperatorId,
                                       String                                OperatorName,
                                       TimeSpan?                             QueryTimeout)

        {

            #region Initial checks

            if (GroupedEVSEStatus == null)
                throw new ArgumentNullException(nameof(GroupedEVSEStatus), "The given lookup of EVSE status types must not be null!");

            #endregion

            #region Get effective number of EVSE status to upload

            Acknowledgement Acknowledgement = null;

            var _NumberOfEVSEStatus = GroupedEVSEStatus.
                                          Select(group => group.Count()).
                                          Sum();

            var StartTime = DateTime.Now;

            #endregion


            if (_NumberOfEVSEStatus > 0)
            {

                #region Send OnEVSEStatusPush event

                OnEVSEStatusPush?.Invoke(StartTime,
                                         this,
                                         this.Id.ToString(),
                                         this.RoamingNetwork.Id,
                                         ActionType,
                                         GroupedEVSEStatus,
                                         (UInt32) _NumberOfEVSEStatus);

                #endregion

                //  var result = await _CPORoaming.PushEVSEStatus(GroupedEVSEs.
                //                                                    SelectMany(group => group).
                //                                                    ToLookup  (evse  => evse.EVSEOperator.Id,
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

                #region Send OnEVSEStatusPushed event

                var EndTime = DateTime.Now;

                OnEVSEStatusPushed?.Invoke(EndTime,
                                           this,
                                           this.Id.ToString(),
                                           this.RoamingNetwork.Id,
                                           ActionType,
                                           GroupedEVSEStatus,
                                           (UInt32) _NumberOfEVSEStatus,
                                           Acknowledgement,
                                           EndTime - StartTime);

                #endregion

            }

            else
                Acknowledgement = new Acknowledgement(true);


            return Acknowledgement;

        }

        #endregion

        #region PushEVSEStatus(EVSEStatus,        ActionType, OperatorId, OperatorName, ...)

        /// <summary>
        /// Upload the given EVSE status.
        /// </summary>
        /// <param name="EVSEStatus">An EVSE status.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(EVSEStatus       EVSEStatus,
                                       ActionType       ActionType,
                                       EVSEOperator_Id  OperatorId,
                                       String           OperatorName,
                                       TimeSpan?        QueryTimeout)

        {

            #region Initial checks

            if (EVSEStatus == null)
                throw new ArgumentNullException(nameof(EVSEStatus), "The given EVSE status must not be null!");

            #endregion

            return await AsIPushStatus.PushEVSEStatus(new EVSEStatus[] { EVSEStatus },
                                                      ActionType,
                                                      OperatorId,
                                                      OperatorName,
                                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEStatus,        ActionType, OperatorId, OperatorName, ...)

        /// <summary>
        /// Upload the given enumeration of EVSE status.
        /// </summary>
        /// <param name="EVSEStatus">An enumeration of EVSE status.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(IEnumerable<EVSEStatus>  EVSEStatus,
                                       ActionType               ActionType,
                                       EVSEOperator_Id          OperatorId,
                                       String                   OperatorName,
                                       TimeSpan?                QueryTimeout)

        {

            #region Initial checks

            if (EVSEStatus == null)
                throw new ArgumentNullException(nameof(EVSEStatus), "The given enumeration of EVSEs must not be null!");

            var _EVSEStatus = EVSEStatus.ToArray();

            #endregion


            if (_EVSEStatus.Any())
                return await AsIPushStatus.PushEVSEStatus(_EVSEStatus.ToLookup(evsestatus => evsestatus.Id.OperatorId,
                                                                               evsestatus => evsestatus),
                                                          ActionType,
                                                          OperatorId,
                                                          OperatorName,
                                                          QueryTimeout);

            return new Acknowledgement(true);

        }

        #endregion

        #region PushEVSEStatus(EVSE,              ActionType, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload the EVSE status of the given EVSE.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(EVSE                 EVSE,
                                       ActionType           ActionType,
                                       EVSEOperator_Id      OperatorId,
                                       String               OperatorName,
                                       Func<EVSE, Boolean>  IncludeEVSEs,
                                       TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (EVSE == null)
                throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!");

            #endregion

            if (IncludeEVSEs != null && !IncludeEVSEs(EVSE))
                return new Acknowledgement(true);

            return await AsIPushStatus.PushEVSEStatus(EVSEStatus.Snapshot(EVSE),
                                                      ActionType,
                                                      OperatorId,
                                                      OperatorName,
                                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEs,             ActionType, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload all EVSE status of the given enumeration of EVSEs.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(IEnumerable<EVSE>    EVSEs,
                                       ActionType           ActionType,
                                       EVSEOperator_Id      OperatorId,
                                       String               OperatorName,
                                       Func<EVSE, Boolean>  IncludeEVSEs,
                                       TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (EVSEs == null)
                throw new ArgumentNullException(nameof(EVSEs), "The given enumeration of EVSEs must not be null!");

            var _EVSEs = IncludeEVSEs != null
                             ? EVSEs.Where(IncludeEVSEs).ToArray()
                             : EVSEs.                    ToArray();

            #endregion

            if (_EVSEs.Any())
                return await AsIPushStatus.PushEVSEStatus(EVSEs.Select(evse => EVSEStatus.Snapshot(evse)),
                                                          ActionType,
                                                          OperatorId,
                                                          OperatorName,
                                                          QueryTimeout);

            else
                return new Acknowledgement(true);

        }

        #endregion

        #region PushEVSEStatus(ChargingStation,   ActionType, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload all EVSE status of the given charging station.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(ChargingStation      ChargingStation,
                                       ActionType           ActionType,
                                       EVSEOperator_Id      OperatorId,
                                       String               OperatorName,
                                       Func<EVSE, Boolean>  IncludeEVSEs,
                                       TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation), "The given charging station must not be null!");

            #endregion

            return await AsIPushStatus.PushEVSEStatus(IncludeEVSEs != null
                                                          ? ChargingStation.EVSEs.Where(IncludeEVSEs).Select(evse => EVSEStatus.Snapshot(evse))
                                                          : ChargingStation.EVSEs.                    Select(evse => EVSEStatus.Snapshot(evse)),
                                                      ActionType,
                                                      OperatorId   ?? ChargingStation.ChargingPool.Operator.Id,
                                                      OperatorName ?? ChargingStation.ChargingPool.Operator.Name.FirstText,
                                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(ChargingStations,  ActionType, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload all EVSE status of the given enumeration of charging stations.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(IEnumerable<ChargingStation>  ChargingStations,
                                       ActionType                    ActionType,
                                       EVSEOperator_Id               OperatorId,
                                       String                        OperatorName,
                                       Func<EVSE, Boolean>           IncludeEVSEs,
                                       TimeSpan?                     QueryTimeout)

        {

            #region Initial checks

            if (ChargingStations == null)
                throw new ArgumentNullException(nameof(ChargingStations), "The given enumeration of charging stations must not be null!");

            #endregion

            return await AsIPushStatus.PushEVSEStatus(IncludeEVSEs != null
                                                          ? ChargingStations.SelectMany(station => station.EVSEs.Where(IncludeEVSEs).Select(evse => EVSEStatus.Snapshot(evse)))
                                                          : ChargingStations.SelectMany(station => station.EVSEs.                    Select(evse => EVSEStatus.Snapshot(evse))),
                                                      ActionType,
                                                      OperatorId,
                                                      OperatorName,
                                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(ChargingPool,      ActionType, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload all EVSE status of the given charging pool.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(ChargingPool         ChargingPool,
                                       ActionType           ActionType,
                                       EVSEOperator_Id      OperatorId,
                                       String               OperatorName,
                                       Func<EVSE, Boolean>  IncludeEVSEs,
                                       TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException(nameof(ChargingPool), "The given charging pool must not be null!");

            #endregion

            return await AsIPushStatus.PushEVSEStatus(IncludeEVSEs != null
                                                          ? ChargingPool.EVSEs.Where(IncludeEVSEs).Select(evse => EVSEStatus.Snapshot(evse))
                                                          : ChargingPool.EVSEs.                    Select(evse => EVSEStatus.Snapshot(evse)),
                                                      ActionType,
                                                      OperatorId   ?? ChargingPool.Operator.Id,
                                                      OperatorName ?? ChargingPool.Operator.Name.FirstText,
                                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(ChargingPools,     ActionType, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload all EVSE status of the given enumeration of charging pools.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(IEnumerable<ChargingPool>  ChargingPools,
                                       ActionType                 ActionType,
                                       EVSEOperator_Id            OperatorId,
                                       String                     OperatorName,
                                       Func<EVSE, Boolean>        IncludeEVSEs,
                                       TimeSpan?                  QueryTimeout)

        {

            #region Initial checks

            if (ChargingPools == null)
                throw new ArgumentNullException(nameof(ChargingPools), "The given enumeration of charging pools must not be null!");

            #endregion

            return await AsIPushStatus.PushEVSEStatus(IncludeEVSEs != null
                                                          ? ChargingPools.SelectMany(pool    => pool.ChargingStations).
                                                                          SelectMany(station => station.EVSEs.Where (IncludeEVSEs).
                                                                                                              Select(evse => EVSEStatus.Snapshot(evse)))
                                                          : ChargingPools.SelectMany(pool    => pool.ChargingStations).
                                                                          SelectMany(station => station.EVSEs.Select(evse => EVSEStatus.Snapshot(evse))),
                                                      ActionType,
                                                      OperatorId,
                                                      OperatorName,
                                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEOperator,      ActionType, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload all EVSE status of the given EVSE operator.
        /// </summary>
        /// <param name="EVSEOperator">An EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(EVSEOperator         EVSEOperator,
                                       ActionType           ActionType,
                                       EVSEOperator_Id      OperatorId,
                                       String               OperatorName,
                                       Func<EVSE, Boolean>  IncludeEVSEs,
                                       TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (EVSEOperator == null)
                throw new ArgumentNullException(nameof(EVSEOperator), "The given EVSE operator must not be null!");

            #endregion

            return await AsIPushStatus.PushEVSEStatus(IncludeEVSEs != null
                                                          ? EVSEOperator.EVSEs.Where(IncludeEVSEs).Select(evse => EVSEStatus.Snapshot(evse))
                                                          : EVSEOperator.EVSEs.                    Select(evse => EVSEStatus.Snapshot(evse)),
                                                      ActionType,
                                                      EVSEOperator.Id,
                                                      OperatorName.IsNotNullOrEmpty()
                                                          ? OperatorName
                                                          : EVSEOperator.Name.FirstText,
                                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEOperators,     ActionType, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload all EVSE status of the given enumeration of EVSE operators.
        /// </summary>
        /// <param name="EVSEOperators">An enumeration of EVSES operators.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(IEnumerable<EVSEOperator>  EVSEOperators,
                                       ActionType                 ActionType,
                                       EVSEOperator_Id            OperatorId,
                                       String                     OperatorName,
                                       Func<EVSE, Boolean>        IncludeEVSEs,
                                       TimeSpan?                  QueryTimeout)

        {

            #region Initial checks

            if (EVSEOperators == null)
                throw new ArgumentNullException(nameof(EVSEOperator), "The given enumeration of EVSE operators must not be null!");

            #endregion

            return await AsIPushStatus.PushEVSEStatus(IncludeEVSEs != null
                                                          ? EVSEOperators.SelectMany(evseoperator => evseoperator.ChargingPools).
                                                                          SelectMany(pool         => pool.ChargingStations).
                                                                          SelectMany(station      => station.EVSEs.Where(IncludeEVSEs).
                                                                                                                   Select(evse => EVSEStatus.Snapshot(evse)))
                                                          : EVSEOperators.SelectMany(evseoperator => evseoperator.ChargingPools).
                                                                          SelectMany(pool         => pool.ChargingStations).
                                                                          SelectMany(station      => station.EVSEs.Select(evse => EVSEStatus.Snapshot(evse))),
                                                      ActionType,
                                                      OperatorId,
                                                      OperatorName,
                                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(RoamingNetwork,    ActionType, OperatorId, OperatorName, IncludeEVSEs, ...)

        /// <summary>
        /// Upload all EVSE status of the given roaming network.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        async Task<Acknowledgement>

            IPushStatus.PushEVSEStatus(RoamingNetwork       RoamingNetwork,
                                       ActionType           ActionType,
                                       EVSEOperator_Id      OperatorId,
                                       String               OperatorName,
                                       Func<EVSE, Boolean>  IncludeEVSEs,
                                       TimeSpan?            QueryTimeout)

        {

            #region Initial checks

            if (RoamingNetwork == null)
                throw new ArgumentNullException(nameof(RoamingNetwork), "The given roaming network must not be null!");

            #endregion

            return await AsIPushStatus.PushEVSEStatus(IncludeEVSEs != null
                                                          ? RoamingNetwork.EVSEs.Where(IncludeEVSEs).Select(evse => EVSEStatus.Snapshot(evse))
                                                          : RoamingNetwork.EVSEs.                    Select(evse => EVSEStatus.Snapshot(evse)),
                                                      ActionType,
                                                      OperatorId,
                                                      OperatorName,
                                                      QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEStatusDiff, ...)

        /// <summary>
        /// Send EVSE status updates.
        /// </summary>
        /// <param name="EVSEStatusDiff">An EVSE status diff.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        async Task

            IPushStatus.PushEVSEStatus(EVSEStatusDiff  EVSEStatusDiff,
                                       TimeSpan?       QueryTimeout)

        {

            await Task.FromResult("");

        }

        #endregion

        #endregion

        #region Receive incoming AuthStart/-Stop

        #region AuthorizeStart(OperatorId, AuthToken, ChargingProductId, SessionId, ...)

        /// <summary>
        /// Create an authorize start request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingProductId">An optional charging product identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        async Task<AuthStartResult>

            IGeneralServices.AuthorizeStart(DateTime            Timestamp,
                                            CancellationToken   CancellationToken,
                                            EventTracking_Id    EventTrackingId,
                                            EVSEOperator_Id     OperatorId,
                                            Auth_Token          AuthToken,
                                            ChargingProduct_Id  ChargingProductId,
                                            ChargingSession_Id  SessionId,
                                            TimeSpan?           QueryTimeout)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException(nameof(OperatorId), "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given parameter must not be null!");

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

        #region AuthorizeStart(OperatorId, AuthToken, EVSEId, ChargingProductId, SessionId, ...)

        /// <summary>
        /// Create an authorize start request at the given EVSE.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ChargingProductId">An optional charging product identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        async Task<AuthStartEVSEResult>

            IGeneralServices.AuthorizeStart(DateTime            Timestamp,
                                            CancellationToken   CancellationToken,
                                            EventTracking_Id    EventTrackingId,
                                            EVSEOperator_Id     OperatorId,
                                            Auth_Token          AuthToken,
                                            EVSE_Id             EVSEId,
                                            ChargingProduct_Id  ChargingProductId,
                                            ChargingSession_Id  SessionId,
                                            TimeSpan?           QueryTimeout)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException(nameof(OperatorId), "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given parameter must not be null!");

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

        #region AuthorizeStart(OperatorId, AuthToken, ChargingStationId, ChargingProductId, SessionId, ...)

        /// <summary>
        /// Create an AuthorizeStart request at the given charging station.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingStationId">The unique identification of a charging station.</param>
        /// <param name="ChargingProductId">An optional charging product identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        async Task<AuthStartChargingStationResult>

            IGeneralServices.AuthorizeStart(DateTime            Timestamp,
                                            CancellationToken   CancellationToken,
                                            EventTracking_Id    EventTrackingId,
                                            EVSEOperator_Id     OperatorId,
                                            Auth_Token          AuthToken,
                                            ChargingStation_Id  ChargingStationId,
                                            ChargingProduct_Id  ChargingProductId,
                                            ChargingSession_Id  SessionId,
                                            TimeSpan?           QueryTimeout)

        {

            #region Initial checks

            if (OperatorId        == null)
                throw new ArgumentNullException(nameof(OperatorId),         "The given parameter must not be null!");

            if (AuthToken         == null)
                throw new ArgumentNullException(nameof(AuthToken),          "The given parameter must not be null!");

            if (ChargingStationId == null)
                throw new ArgumentNullException(nameof(ChargingStationId),  "The given parameter must not be null!");

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


        #region AuthorizeStop(OperatorId, SessionId, AuthToken, ...)

        /// <summary>
        /// Create an authorize stop request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        async Task<AuthStopResult>

            IGeneralServices.AuthorizeStop(DateTime            Timestamp,
                                           CancellationToken   CancellationToken,
                                           EventTracking_Id    EventTrackingId,
                                           EVSEOperator_Id     OperatorId,
                                           ChargingSession_Id  SessionId,
                                           Auth_Token          AuthToken,
                                           TimeSpan?           QueryTimeout)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException(nameof(OperatorId), "The given parameter must not be null!");

            if (SessionId  == null)
                throw new ArgumentNullException(nameof(SessionId),  "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given parameter must not be null!");

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

        #region AuthorizeStop(OperatorId, EVSEId, SessionId, AuthToken, ...)

        /// <summary>
        /// Create an authorize stop request at the given EVSE.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        async Task<AuthStopEVSEResult>

            IGeneralServices.AuthorizeStop(DateTime            Timestamp,
                                           CancellationToken   CancellationToken,
                                           EventTracking_Id    EventTrackingId,
                                           EVSEOperator_Id     OperatorId,
                                           EVSE_Id             EVSEId,
                                           ChargingSession_Id  SessionId,
                                           Auth_Token          AuthToken,
                                           TimeSpan?           QueryTimeout)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException(nameof(OperatorId), "The given parameter must not be null!");

            if (SessionId  == null)
                throw new ArgumentNullException(nameof(SessionId),  "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given parameter must not be null!");

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId),     "The given parameter must not be null!");

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

        #region AuthorizeStop(OperatorId, ChargingStationId, SessionId, AuthToken, ...)

        /// <summary>
        /// Create an authorize stop request at the given charging station.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="ChargingStationId">A charging station identification.</param>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        async Task<AuthStopChargingStationResult>

            IGeneralServices.AuthorizeStop(DateTime            Timestamp,
                                           CancellationToken   CancellationToken,
                                           EventTracking_Id    EventTrackingId,
                                           EVSEOperator_Id     OperatorId,
                                           ChargingStation_Id  ChargingStationId,
                                           ChargingSession_Id  SessionId,
                                           Auth_Token          AuthToken,
                                           TimeSpan?           QueryTimeout)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException(nameof(OperatorId), "The given parameter must not be null!");

            if (SessionId  == null)
                throw new ArgumentNullException(nameof(SessionId),  "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given parameter must not be null!");

            #endregion

            return AuthStopChargingStationResult.Error(AuthorizatorId);

        }

        #endregion

        #endregion

        #region Receive incoming ChargeDetailRecords

        #region SendChargeDetailRecord(ChargeDetailRecord, ...)

        async Task<SendCDRResult>

            IGeneralServices.SendChargeDetailRecord(DateTime            Timestamp,
                                                    CancellationToken   CancellationToken,
                                                    EventTracking_Id    EventTrackingId,
                                                    ChargeDetailRecord  ChargeDetailRecord,
                                                    TimeSpan?           QueryTimeout)
        {

            #region Initial checks

            if (ChargeDetailRecord == null)
                throw new ArgumentNullException(nameof(ChargeDetailRecord),  "The given charge detail record must not be null!");

            #endregion

            SessionInfo _SessionInfo = null;

            //ToDo: Add events!


            Debug.WriteLine("Received a CDR: " + ChargeDetailRecord.SessionId.ToString());


            if (ChargeDetailRecordDatabase.ContainsKey(ChargeDetailRecord.SessionId))
                return SendCDRResult.InvalidSessionId(AuthorizatorId);


            if (ChargeDetailRecordDatabase.TryAdd(ChargeDetailRecord.SessionId, ChargeDetailRecord))
            {

                SessionDatabase.TryRemove(ChargeDetailRecord.SessionId, out _SessionInfo);

                return SendCDRResult.Forwarded(AuthorizatorId);

            }

            //roamingprovider.OnEVSEStatusPush   += (Timestamp, Sender, SenderId, RoamingNetworkId, ActionType, GroupedEVSEs, NumberOfEVSEs) => {
            //    Console.WriteLine("[" + Timestamp + "] " + RoamingNetworkId.ToString() + ": Pushing " + NumberOfEVSEs + " EVSE status towards " + SenderId + "(" + ActionType + ")");
            //};

            //    Console.WriteLine("[" + Timestamp + "] " + RoamingNetworkId.ToString() + ": Pushed "  + NumberOfEVSEs + " EVSE status towards " + SenderId + "(" + ActionType + ") => " + Result.Result + " (" + Duration.TotalSeconds + " sec)");

            //    if (Result.Result == false)
            //    {

            //        var EMailTask = API_SMTPClient.Send(HubjectEVSEStatusPushFailedEMailProvider(Timestamp,
            //                                                                                       Sender,
            //                                                                                       SenderId,
            //                                                                                       RoamingNetworkId,
            //                                                                                       ActionType,
            //                                                                                       GroupedEVSEs,
            //                                                                                       NumberOfEVSEs,
            //                                                                                       Result,
            //                                                                                       Duration));

            //        EMailTask.Wait(TimeSpan.FromSeconds(30));

            //    }

            //};

            return SendCDRResult.InvalidSessionId(AuthorizatorId);

        }

        #endregion

        #endregion

        #endregion

        #region Outgoing requests towards the roaming network

        //ToDo: Send Tokens!
        //ToDo: Download CDRs!

        #region Reserve(...EVSEId, StartTime, Duration, ReservationId = null, ...)

        /// <summary>
        /// Reserve the possibility to charge at the given EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp of this request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="EVSEId">The unique identification of the EVSE to be reserved.</param>
        /// <param name="StartTime">The starting time of the reservation.</param>
        /// <param name="Duration">The duration of the reservation.</param>
        /// <param name="ReservationId">An optional unique identification of the reservation. Mandatory for updates.</param>
        /// <param name="eMAId">An optional unique identification of e-Mobility account/customer requesting this reservation.</param>
        /// <param name="ChargingProductId">An optional unique identification of the charging product to be reserved.</param>
        /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
        /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
        /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<ReservationResult>

            Reserve(DateTime                 Timestamp,
                    CancellationToken        CancellationToken,
                    EventTracking_Id         EventTrackingId,
                    EVSE_Id                  EVSEId,
                    DateTime?                StartTime          = null,
                    TimeSpan?                Duration           = null,
                    ChargingReservation_Id   ReservationId      = null,
                    eMA_Id                   eMAId              = null,
                    ChargingProduct_Id       ChargingProductId  = null,
                    IEnumerable<Auth_Token>  AuthTokens         = null,
                    IEnumerable<eMA_Id>      eMAIds             = null,
                    IEnumerable<UInt32>      PINs               = null,
                    TimeSpan?                QueryTimeout       = null)

        {

            #region Initial checks

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId),  "The given EVSE identification must not be null!");

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;

            #endregion

            #region Send OnReserveEVSE event

            var Runtime = Stopwatch.StartNew();

            try
            {

                OnReserveEVSE?.Invoke(this,
                                      Timestamp,
                                      EventTrackingId,
                                      RoamingNetwork.Id,
                                      ReservationId,
                                      EVSEId,
                                      StartTime,
                                      Duration,
                                      _Id,
                                      eMAId,
                                      ChargingProductId,
                                      AuthTokens,
                                      eMAIds,
                                      PINs,
                                      QueryTimeout);

            }
            catch (Exception e)
            {
                e.Log(nameof(RoamingNetwork) + "." + nameof(OnReserveEVSE));
            }

            #endregion


            var response = await RoamingNetwork.Reserve(Timestamp,
                                                        CancellationToken,
                                                        EventTrackingId,
                                                        EVSEId,
                                                        StartTime,
                                                        Duration,
                                                        ReservationId,
                                                        _Id,
                                                        eMAId,
                                                        ChargingProductId,
                                                        AuthTokens,
                                                        eMAIds,
                                                        PINs,
                                                        QueryTimeout);


            #region Send OnEVSEReserved event

            Runtime.Stop();

            try
            {

                OnEVSEReserved?.Invoke(this,
                                       Timestamp,
                                       EventTrackingId,
                                       RoamingNetwork.Id,
                                       ReservationId,
                                       EVSEId,
                                       StartTime,
                                       Duration,
                                       _Id,
                                       eMAId,
                                       ChargingProductId,
                                       AuthTokens,
                                       eMAIds,
                                       PINs,
                                       response,
                                       Runtime.Elapsed,
                                       QueryTimeout);

            }
            catch (Exception e)
            {
                e.Log(nameof(RoamingNetwork) + "." + nameof(OnEVSEReserved));
            }

            #endregion

            return response;

        }

        #endregion

        #region CancelReservation(...ReservationId, Reason, EVSEId = null, ...)

        /// <summary>
        /// Cancel the given charging reservation.
        /// </summary>
        /// <param name="Timestamp">The timestamp of this request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="ReservationId">The unique charging reservation identification.</param>
        /// <param name="Reason">A reason for this cancellation.</param>
        /// <param name="EVSEId">An optional identification of the EVSE.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<CancelReservationResult> CancelReservation(DateTime                               Timestamp,
                                                                     CancellationToken                      CancellationToken,
                                                                     EventTracking_Id                       EventTrackingId,
                                                                     ChargingReservation_Id                 ReservationId,
                                                                     ChargingReservationCancellationReason  Reason,
                                                                     EVSE_Id                                EVSEId        = null,
                                                                     TimeSpan?                              QueryTimeout  = null)
        {

            var response = await RoamingNetwork.CancelReservation(Timestamp,
                                                                  CancellationToken,
                                                                  EventTrackingId,
                                                                  ReservationId,
                                                                  Reason,
                                                                  _Id,
                                                                  EVSEId,
                                                                  QueryTimeout);


            //var OnReservationCancelledLocal = OnReservationCancelled;
            //if (OnReservationCancelledLocal != null)
            //    OnReservationCancelledLocal(DateTime.Now,
            //                                this,
            //                                EventTracking_Id.New,
            //                                ReservationId,
            //                                Reason);

            return response;

        }

        #endregion


        #region RemoteStart(...EVSEId, ChargingProductId = null, ReservationId = null, SessionId = null, eMAId = null, ...)

        /// <summary>
        /// Start a charging session at the given EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="EVSEId">The unique identification of the EVSE to be started.</param>
        /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
        /// <param name="ReservationId">The unique identification for a charging reservation.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStartEVSEResult>

            RemoteStart(DateTime                Timestamp,
                        CancellationToken       CancellationToken,
                        EventTracking_Id        EventTrackingId,
                        EVSE_Id                 EVSEId,
                        ChargingProduct_Id      ChargingProductId  = null,
                        ChargingReservation_Id  ReservationId      = null,
                        ChargingSession_Id      SessionId          = null,
                        eMA_Id                  eMAId              = null,
                        TimeSpan?               QueryTimeout       = null)

        {

            #region Initial checks

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId),  "The given EVSE identification must not be null!");

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;

            #endregion

            #region Send OnRemoteEVSEStart event

            var Runtime = Stopwatch.StartNew();

            try
            {

                OnRemoteEVSEStart?.Invoke(Timestamp,
                                          this,
                                          EventTrackingId,
                                          RoamingNetwork.Id,
                                          EVSEId,
                                          ChargingProductId,
                                          ReservationId,
                                          SessionId,
                                          _Id,
                                          eMAId,
                                          QueryTimeout);

            }
            catch (Exception e)
            {
                e.Log(nameof(RoamingNetwork) + "." + nameof(OnRemoteEVSEStart));
            }

            #endregion


            var response = await RoamingNetwork.RemoteStart(Timestamp,
                                                            CancellationToken,
                                                            EventTrackingId,
                                                            EVSEId,
                                                            ChargingProductId,
                                                            ReservationId,
                                                            SessionId,
                                                            _Id,
                                                            eMAId,
                                                            QueryTimeout);


            #region Send OnRemoteEVSEStarted event

            Runtime.Stop();

            try
            {

                OnRemoteEVSEStarted?.Invoke(Timestamp,
                                            this,
                                            EventTrackingId,
                                            RoamingNetwork.Id,
                                            EVSEId,
                                            ChargingProductId,
                                            ReservationId,
                                            SessionId,
                                            _Id,
                                            eMAId,
                                            QueryTimeout,
                                            response,
                                            Runtime.Elapsed);

            }
            catch (Exception e)
            {
                e.Log(nameof(RoamingNetwork) + "." + nameof(OnRemoteEVSEStarted));
            }

            #endregion

            return response;

        }

        #endregion

        #region RemoteStop(...EVSEId, SessionId, ReservationHandling, eMAId = null, ...)

        /// <summary>
        /// Stop the given charging session at the given EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="EVSEId">The unique identification of the EVSE to be stopped.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStopEVSEResult>

            RemoteStop(DateTime             Timestamp,
                       CancellationToken    CancellationToken,
                       EventTracking_Id     EventTrackingId,
                       EVSE_Id              EVSEId,
                       ChargingSession_Id   SessionId,
                       ReservationHandling  ReservationHandling,
                       eMA_Id               eMAId         = null,
                       TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId),     "The given EVSE identification must not be null!");

            if (SessionId == null)
                throw new ArgumentNullException(nameof(SessionId),  "The given charging session identification must not be null!");

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;

            #endregion

            #region Send OnRemoteEVSEStop event

            var Runtime = Stopwatch.StartNew();

            try
            {

                OnRemoteEVSEStop?.Invoke(this,
                                         Timestamp,
                                         EventTrackingId,
                                         RoamingNetwork.Id,
                                         EVSEId,
                                         SessionId,
                                         ReservationHandling,
                                         _Id,
                                         eMAId,
                                         QueryTimeout);

            }
            catch (Exception e)
            {
                e.Log(nameof(RoamingNetwork) + "." + nameof(OnRemoteEVSEStop));
            }

            #endregion


            var response = await RoamingNetwork.RemoteStop(Timestamp,
                                                           CancellationToken,
                                                           EventTrackingId,
                                                           EVSEId,
                                                           SessionId,
                                                           ReservationHandling,
                                                           _Id,
                                                           eMAId,
                                                           QueryTimeout);


            #region Send OnRemoteEVSEStopped event

            Runtime.Stop();

            try
            {

                OnRemoteEVSEStopped?.Invoke(this,
                                            Timestamp,
                                            EventTrackingId,
                                            RoamingNetwork.Id,
                                            EVSEId,
                                            SessionId,
                                            ReservationHandling,
                                            _Id,
                                            eMAId,
                                            QueryTimeout,
                                            response,
                                            Runtime.Elapsed);

            }
            catch (Exception e)
            {
                e.Log(nameof(RoamingNetwork) + "." + nameof(OnRemoteEVSEStopped));
            }

            #endregion

            return response;

        }

        #endregion

        #endregion


    }

}
