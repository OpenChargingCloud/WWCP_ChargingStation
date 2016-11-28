/*
 * Copyright (c) 2014-2016 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP Cloud <https://git.graphdefined.com/OpenChargingCloud/WWCP_Cloud>
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
    public class eMobilityServiceProvider : IEMobilityProviderUserInterface,
                                            IRemoteEMobilityProvider
    {

        #region Data

        private readonly ConcurrentDictionary<Auth_Token,         TokenAuthorizationResultType>  AuthorizationDatabase;
        private readonly ConcurrentDictionary<ChargingSession_Id, SessionInfo>                   SessionDatabase;
        private readonly ConcurrentDictionary<ChargingSession_Id, ChargeDetailRecord>            ChargeDetailRecordDatabase;

        #endregion

        #region Properties

        /// <summary>
        /// The unique identification of the e-mobility service provider.
        /// </summary>
        public eMobilityProvider_Id  Id                 { get; }

        //public Authorizator_Id       AuthorizatorId     { get; }


        #region EVSP

        private readonly eMobilityProviderStub _EVSP;

        public eMobilityProviderStub EVSP
            => _EVSP;

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

        #region Links

        /// <summary>
        /// The parent roaming network.
        /// </summary>
        public RoamingNetwork RoamingNetwork { get; }

        #endregion

        #region Events

        #region OnEVSEDataPush/-Pushed

        ///// <summary>
        ///// An event fired whenever new EVSE data will be send upstream.
        ///// </summary>
        //public event OnPushEVSEDataRequestDelegate OnPushEVSEDataRequest;

        ///// <summary>
        ///// An event fired whenever new EVSE data had been sent upstream.
        ///// </summary>
        //public event OnPushEVSEDataResponseDelegate OnPushEVSEDataResponse;

        #endregion

        #region OnEVSEStatusPush/-Pushed

        ///// <summary>
        ///// An event fired whenever new EVSE status will be send upstream.
        ///// </summary>
        //public event OnPushEVSEStatusRequestDelegate OnPushEVSEStatusRequest;

        ///// <summary>
        ///// An event fired whenever new EVSE status had been sent upstream.
        ///// </summary>
        //public event OnPushEVSEStatusResponseDelegate OnPushEVSEStatusResponse;

        #endregion


        #region OnReserve... / OnReserved...

        /// <summary>
        /// An event fired whenever an EVSE is being reserved.
        /// </summary>
        public event OnReserveEVSERequestDelegate              OnReserveEVSE;

        /// <summary>
        /// An event fired whenever an EVSE was reserved.
        /// </summary>
        public event OnReserveEVSEResponseDelegate             OnEVSEReserved;

        #endregion

        #region OnRemote...Start / OnRemote...Started

        /// <summary>
        /// An event fired whenever a remote start EVSE command was received.
        /// </summary>
        public event OnRemoteStartEVSERequestDelegate              OnRemoteEVSEStart;

        /// <summary>
        /// An event fired whenever a remote start EVSE command completed.
        /// </summary>
        public event OnRemoteStartEVSEResponseDelegate             OnRemoteEVSEStarted;

        #endregion

        #region OnRemote...Stop / OnRemote...Stopped

        /// <summary>
        /// An event fired whenever a remote stop EVSE command was received.
        /// </summary>
        public event OnRemoteStopEVSERequestDelegate                OnRemoteEVSEStop;

        /// <summary>
        /// An event fired whenever a remote stop EVSE command completed.
        /// </summary>
        public event OnRemoteStopEVSEResponseDelegate             OnRemoteEVSEStopped;

        #endregion

        // CancelReservation

        #endregion

        #region Constructor(s)

        internal eMobilityServiceProvider(eMobilityProvider_Id  Id,
                                          RoamingNetwork        RoamingNetwork)
                                          //Authorizator_Id       AuthorizatorId = null)
        {

            this.Id                          = Id;
            this.RoamingNetwork              = RoamingNetwork;
            //this.AuthorizatorId              = AuthorizatorId ?? Authorizator_Id.Parse("GraphDefined WWCP E-Mobility Database");

            this.AuthorizationDatabase       = new ConcurrentDictionary<Auth_Token,         TokenAuthorizationResultType>();
            this.SessionDatabase             = new ConcurrentDictionary<ChargingSession_Id, SessionInfo>();
            this.ChargeDetailRecordDatabase  = new ConcurrentDictionary<ChargingSession_Id, ChargeDetailRecord>();

            //EVSP.RemoteEMobilityProvider = this;

        }

        internal eMobilityServiceProvider(eMobilityProviderStub  EVSP)
                                         // Authorizator_Id    AuthorizatorId = null)
        {

            this.Id                          = EVSP.Id;
            this.RoamingNetwork              = EVSP.RoamingNetwork;
            this._EVSP                       = EVSP;
            //this.AuthorizatorId              = AuthorizatorId ?? Authorizator_Id.Parse("GraphDefined WWCP E-Mobility Database");

            this.AuthorizationDatabase       = new ConcurrentDictionary<Auth_Token,         TokenAuthorizationResultType>();
            this.SessionDatabase             = new ConcurrentDictionary<ChargingSession_Id, SessionInfo>();
            this.ChargeDetailRecordDatabase  = new ConcurrentDictionary<ChargingSession_Id, ChargeDetailRecord>();

            //EVSP.RemoteEMobilityProvider = this;

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

        //private IRemotePushData AsIPushData  => this;

        //#region UpdateEVSEData                   (EVSE,             ActionType, ...)

        ///// <summary>
        ///// Upload the EVSE data of the given EVSE.
        ///// </summary>
        ///// <param name="EVSE">An EVSE.</param>
        ///// <param name="ActionType">The server-side data management operation.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //Task<Acknowledgement>

        //    IRemotePushData.UpdateEVSEData(EVSE                 EVSE,
        //                                    ActionType           ActionType,

        //                                    DateTime?            Timestamp          = null,
        //                                    CancellationToken?   CancellationToken  = null,
        //                                    EventTracking_Id     EventTrackingId    = null,
        //                                    TimeSpan?            RequestTimeout     = null)

        //{

        //    #region Initial checks

        //    if (EVSE == null)
        //        throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!");

        //    #endregion

        //    return Task.FromResult(new Acknowledgement(ResultType.True));

        //}

        //#endregion

        //#region UpdateEVSEData                   (EVSEs,            ActionType, ...)

        ///// <summary>
        ///// Upload the EVSE data of the given enumeration of EVSEs.
        ///// </summary>
        ///// <param name="EVSEs">An enumeration of EVSEs.</param>
        ///// <param name="ActionType">The server-side data management operation.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //Task<Acknowledgement>

        //    IRemotePushData.UpdateEVSEData(IEnumerable<EVSE>    EVSEs,
        //                                    ActionType           ActionType,

        //                                    DateTime?            Timestamp          = null,
        //                                    CancellationToken?   CancellationToken  = null,
        //                                    EventTracking_Id     EventTrackingId    = null,
        //                                    TimeSpan?            RequestTimeout     = null)

        //{

        //    #region Initial checks

        //    if (EVSEs == null)
        //        throw new ArgumentNullException(nameof(EVSEs), "The given enumeration of EVSEs must not be null!");

        //    #endregion

        //    return Task.FromResult(new Acknowledgement(ResultType.True));

        //}

        //#endregion

        //#region UpdateChargingStationData        (ChargingStation,  ActionType, ...)

        ///// <summary>
        ///// Upload the EVSE data of the given charging station.
        ///// </summary>
        ///// <param name="ChargingStation">A charging station.</param>
        ///// <param name="ActionType">The server-side data management operation.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //Task<Acknowledgement>

        //    IRemotePushData.UpdateChargingStationData(ChargingStation      ChargingStation,
        //                                              ActionType           ActionType,

        //                                              DateTime?            Timestamp          = null,
        //                                              CancellationToken?   CancellationToken  = null,
        //                                              EventTracking_Id     EventTrackingId    = null,
        //                                              TimeSpan?            RequestTimeout     = null)

        //{

        //    #region Initial checks

        //    if (ChargingStation == null)
        //        throw new ArgumentNullException(nameof(ChargingStation), "The given charging station must not be null!");

        //    #endregion

        //    return Task.FromResult(new Acknowledgement(ResultType.True));

        //}

        //#endregion

        //#region UpdateChargingStationData        (ChargingStations, ActionType, ...)

        ///// <summary>
        ///// Upload the EVSE data of the given charging stations.
        ///// </summary>
        ///// <param name="ChargingStations">An enumeration of charging stations.</param>
        ///// <param name="ActionType">The server-side data management operation.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //Task<Acknowledgement>

        //    IRemotePushData.UpdateChargingStationData(IEnumerable<ChargingStation>  ChargingStations,
        //                                              ActionType                    ActionType,

        //                                              DateTime?                     Timestamp          = null,
        //                                              CancellationToken?            CancellationToken  = null,
        //                                              EventTracking_Id              EventTrackingId    = null,
        //                                              TimeSpan?                     RequestTimeout     = null)

        //{

        //    #region Initial checks

        //    if (ChargingStations == null)
        //        throw new ArgumentNullException(nameof(ChargingStations), "The given enumeration of charging stations must not be null!");

        //    #endregion

        //    return Task.FromResult(new Acknowledgement(ResultType.True));

        //}

        //#endregion

        //#region UpdateChargingPoolData           (ChargingPool,     ActionType, ...)

        ///// <summary>
        ///// Upload the EVSE data of the given charging pool.
        ///// </summary>
        ///// <param name="ChargingPool">A charging pool.</param>
        ///// <param name="ActionType">The server-side data management operation.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //Task<Acknowledgement>

        //    IRemotePushData.UpdateChargingPoolData(ChargingPool         ChargingPool,
        //                                            ActionType           ActionType,

        //                                            DateTime?            Timestamp          = null,
        //                                            CancellationToken?   CancellationToken  = null,
        //                                            EventTracking_Id     EventTrackingId    = null,
        //                                            TimeSpan?            RequestTimeout     = null)

        //{

        //    #region Initial checks

        //    if (ChargingPool == null)
        //        throw new ArgumentNullException(nameof(ChargingPool), "The given charging pool must not be null!");

        //    #endregion

        //    return Task.FromResult(new Acknowledgement(ResultType.True));

        //}

        //#endregion

        //#region UpdateChargingPoolData           (ChargingPools,    ActionType, ...)

        ///// <summary>
        ///// Upload the EVSE data of the given charging pools.
        ///// </summary>
        ///// <param name="ChargingPools">An enumeration of charging pools.</param>
        ///// <param name="ActionType">The server-side data management operation.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //Task<Acknowledgement>

        //    IRemotePushData.UpdateChargingPoolData(IEnumerable<ChargingPool>  ChargingPools,
        //                                            ActionType                 ActionType,

        //                                            DateTime?                  Timestamp          = null,
        //                                            CancellationToken?         CancellationToken  = null,
        //                                            EventTracking_Id           EventTrackingId    = null,
        //                                            TimeSpan?                  RequestTimeout     = null)

        //{

        //    #region Initial checks

        //    if (ChargingPools == null)
        //        throw new ArgumentNullException(nameof(ChargingPools), "The given enumeration of charging pools must not be null!");

        //    #endregion

        //    return Task.FromResult(new Acknowledgement(ResultType.True));

        //}

        //#endregion

        //#region UpdateChargingStationOperatorData(EVSEOperator,     ActionType, ...)

        ///// <summary>
        ///// Upload the EVSE data of the given Charging Station Operator.
        ///// </summary>
        ///// <param name="ChargingStationOperator">An Charging Station Operator.</param>
        ///// <param name="ActionType">The server-side data management operation.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //Task<Acknowledgement>

        //    IRemotePushData.UpdateChargingStationOperatorData(ChargingStationOperator  ChargingStationOperator,
        //                                                       ActionType               ActionType,

        //                                                       DateTime?                Timestamp          = null,
        //                                                       CancellationToken?       CancellationToken  = null,
        //                                                       EventTracking_Id         EventTrackingId    = null,
        //                                                       TimeSpan?                RequestTimeout     = null)

        //{

        //    #region Initial checks

        //    if (ChargingStationOperator == null)
        //        throw new ArgumentNullException(nameof(ChargingStationOperator), "The given charging station operator must not be null!");

        //    #endregion

        //    return Task.FromResult(new Acknowledgement(ResultType.True));

        //}

        //#endregion

        //#region UpdateChargingStationOperatorData(EVSEOperators,    ActionType, ...)

        ///// <summary>
        ///// Upload the EVSE data of the given Charging Station Operators.
        ///// </summary>
        ///// <param name="ChargingStationOperators">An enumeration of Charging Station Operators.</param>
        ///// <param name="ActionType">The server-side data management operation.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //Task<Acknowledgement>

        //    IRemotePushData.UpdateChargingStationOperatorData(IEnumerable<ChargingStationOperator>  ChargingStationOperators,
        //                                                       ActionType                            ActionType,

        //                                                       DateTime?                             Timestamp          = null,
        //                                                       CancellationToken?                    CancellationToken  = null,
        //                                                       EventTracking_Id                      EventTrackingId    = null,
        //                                                       TimeSpan?                             RequestTimeout     = null)

        //{

        //    #region Initial checks

        //    if (ChargingStationOperators == null)
        //        throw new ArgumentNullException(nameof(ChargingStationOperators),  "The given enumeration of charging station operators must not be null!");

        //    #endregion

        //    return Task.FromResult(new Acknowledgement(ResultType.True));

        //}

        //#endregion

        //#region UpdateRoamingNetworkData         (RoamingNetwork,   ActionType, ...)

        ///// <summary>
        ///// Upload the EVSE data of the given roaming network.
        ///// </summary>
        ///// <param name="RoamingNetwork">A roaming network.</param>
        ///// <param name="ActionType">The server-side data management operation.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //Task<Acknowledgement>

        //    IRemotePushData.UpdateRoamingNetworkData(RoamingNetwork       RoamingNetwork,
        //                                              ActionType           ActionType,

        //                                              DateTime?            Timestamp          = null,
        //                                              CancellationToken?   CancellationToken  = null,
        //                                              EventTracking_Id     EventTrackingId    = null,
        //                                              TimeSpan?            RequestTimeout     = null)

        //{

        //    #region Initial checks

        //    if (RoamingNetwork == null)
        //        throw new ArgumentNullException(nameof(SmartCityStub), "The given roaming network must not be null!");

        //    #endregion

        //    return Task.FromResult(new Acknowledgement(ResultType.True));

        //}

        //#endregion

        //public void RemoveChargingStations(DateTime                      Timestamp,
        //                                   IEnumerable<ChargingStation>  ChargingStations)
        //{

        //    foreach (var _ChargingStation in ChargingStations)
        //        Console.WriteLine(DateTime.Now + " LocalEMobilityService says: " + _ChargingStation.Id + " was removed!");

        //}

        #endregion

        #region Receive incoming EVSEStatus

        //private IRemotePushStatus AsIPushStatus2Remote  => this;

        //#region UpdateEVSEStatus(EVSEStatus, ...)

        ///// <summary>
        ///// Upload the given EVSE status.
        ///// </summary>
        ///// <param name="EVSEStatus">An EVSE status.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //async Task<Acknowledgement>

        //    IRemotePushStatus.UpdateEVSEStatus(EVSEStatus          EVSEStatus,

        //                                        DateTime?           Timestamp,
        //                                        CancellationToken?  CancellationToken,
        //                                        EventTracking_Id    EventTrackingId,
        //                                        TimeSpan?           RequestTimeout)

        //{

        //    #region Initial checks

        //    if (EVSEStatus == null)
        //        throw new ArgumentNullException(nameof(EVSEStatus), "The given EVSE status must not be null!");


        //    Acknowledgement result;

        //    #endregion

        //    #region Send OnUpdateEVSEStatusRequest event

        //    //   OnPushEVSEStatusRequest?.Invoke(DateTime.Now,
        //    //                                   Timestamp.Value,
        //    //                                   this,
        //    //                                   this.Id.ToString(),
        //    //                                   EventTrackingId,
        //    //                                   this.RoamingNetwork.Id,
        //    //                                   ActionType,
        //    //                                   GroupedEVSEStatus,
        //    //                                   (UInt32) _NumberOfEVSEStatus,
        //    //                                   RequestTimeout);

        //    #endregion


        //    result = new Acknowledgement(ResultType.NoOperation);


        //    #region Send OnUpdateEVSEStatusResponse event

        //    // OnUpdateEVSEStatusResponse?.Invoke(DateTime.Now,
        //    //                                    Timestamp.Value,
        //    //                                    this,
        //    //                                    this.Id.ToString(),
        //    //                                    EventTrackingId,
        //    //                                    this.RoamingNetwork.Id,
        //    //                                    ActionType,
        //    //                                    GroupedEVSEStatus,
        //    //                                    (UInt32) _NumberOfEVSEStatus,
        //    //                                    RequestTimeout,
        //    //                                    result,
        //    //                                    DateTime.Now - Timestamp.Value);

        //    #endregion

        //    return result;

        //}

        //#endregion

        //#region UpdateEVSEStatus(EVSEStatus, ...)

        ///// <summary>
        ///// Upload the given enumeration of EVSE status.
        ///// </summary>
        ///// <param name="EVSEStatus">An enumeration of EVSE status.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //async Task<Acknowledgement>

        //    IRemotePushStatus.UpdateEVSEStatus(IEnumerable<EVSEStatus>  EVSEStatus,

        //                                        DateTime?                Timestamp,
        //                                        CancellationToken?       CancellationToken,
        //                                        EventTracking_Id         EventTrackingId,
        //                                        TimeSpan?                RequestTimeout)

        //{

        //    #region Initial checks

        //    if (EVSEStatus == null)
        //        throw new ArgumentNullException(nameof(EVSEStatus),  "The given enumeration of EVSE status must not be null!");


        //    Acknowledgement result;

        //    #endregion

        //    #region Send OnUpdateEVSEStatusRequest event

        //    //   OnPushEVSEStatusRequest?.Invoke(DateTime.Now,
        //    //                                   Timestamp.Value,
        //    //                                   this,
        //    //                                   this.Id.ToString(),
        //    //                                   EventTrackingId,
        //    //                                   this.RoamingNetwork.Id,
        //    //                                   ActionType,
        //    //                                   GroupedEVSEStatus,
        //    //                                   (UInt32) _NumberOfEVSEStatus,
        //    //                                   RequestTimeout);

        //    #endregion


        //    result = new Acknowledgement(ResultType.NoOperation);


        //    #region Send OnUpdateEVSEStatusResponse event

        //    // OnUpdateEVSEStatusResponse?.Invoke(DateTime.Now,
        //    //                                    Timestamp.Value,
        //    //                                    this,
        //    //                                    this.Id.ToString(),
        //    //                                    EventTrackingId,
        //    //                                    this.RoamingNetwork.Id,
        //    //                                    ActionType,
        //    //                                    GroupedEVSEStatus,
        //    //                                    (UInt32) _NumberOfEVSEStatus,
        //    //                                    RequestTimeout,
        //    //                                    result,
        //    //                                    DateTime.Now - Timestamp.Value);

        //    #endregion

        //    return result;

        //}

        //#endregion

        //#region UpdateEVSEStatus(EVSE, ...)

        ///// <summary>
        ///// Upload the EVSE status of the given EVSE.
        ///// </summary>
        ///// <param name="EVSE">An EVSE.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //async Task<Acknowledgement>

        //    IRemotePushStatus.UpdateEVSEStatus(EVSE                 EVSE,

        //                                        DateTime?            Timestamp,
        //                                        CancellationToken?   CancellationToken,
        //                                        EventTracking_Id     EventTrackingId,
        //                                        TimeSpan?            RequestTimeout)

        //{

        //    #region Initial checks

        //    if (EVSE == null)
        //        throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!");


        //    Acknowledgement result;

        //    #endregion

        //    #region Send OnUpdateEVSEStatusRequest event

        //    //   OnPushEVSEStatusRequest?.Invoke(DateTime.Now,
        //    //                                   Timestamp.Value,
        //    //                                   this,
        //    //                                   this.Id.ToString(),
        //    //                                   EventTrackingId,
        //    //                                   this.RoamingNetwork.Id,
        //    //                                   ActionType,
        //    //                                   GroupedEVSEStatus,
        //    //                                   (UInt32) _NumberOfEVSEStatus,
        //    //                                   RequestTimeout);

        //    #endregion


        //    result = new Acknowledgement(ResultType.NoOperation);


        //    #region Send OnUpdateEVSEStatusResponse event

        //    // OnUpdateEVSEStatusResponse?.Invoke(DateTime.Now,
        //    //                                    Timestamp.Value,
        //    //                                    this,
        //    //                                    this.Id.ToString(),
        //    //                                    EventTrackingId,
        //    //                                    this.RoamingNetwork.Id,
        //    //                                    ActionType,
        //    //                                    GroupedEVSEStatus,
        //    //                                    (UInt32) _NumberOfEVSEStatus,
        //    //                                    RequestTimeout,
        //    //                                    result,
        //    //                                    DateTime.Now - Timestamp.Value);

        //    #endregion

        //    return result;

        //}

        //#endregion

        //#region UpdateEVSEStatus(EVSEs, ...)

        ///// <summary>
        ///// Upload all EVSE status of the given enumeration of EVSEs.
        ///// </summary>
        ///// <param name="EVSEs">An enumeration of EVSEs.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //async Task<Acknowledgement>

        //    IRemotePushStatus.UpdateEVSEStatus(IEnumerable<EVSE>    EVSEs,

        //                                        DateTime?            Timestamp,
        //                                        CancellationToken?   CancellationToken,
        //                                        EventTracking_Id     EventTrackingId,
        //                                        TimeSpan?            RequestTimeout)

        //{

        //    #region Initial checks

        //    if (EVSEs == null)
        //        throw new ArgumentNullException(nameof(EVSEs), "The given enumeration of EVSEs must not be null!");


        //    Acknowledgement result;

        //    #endregion

        //    #region Send OnUpdateEVSEStatusRequest event

        //    //   OnPushEVSEStatusRequest?.Invoke(DateTime.Now,
        //    //                                   Timestamp.Value,
        //    //                                   this,
        //    //                                   this.Id.ToString(),
        //    //                                   EventTrackingId,
        //    //                                   this.RoamingNetwork.Id,
        //    //                                   ActionType,
        //    //                                   GroupedEVSEStatus,
        //    //                                   (UInt32) _NumberOfEVSEStatus,
        //    //                                   RequestTimeout);

        //    #endregion


        //    result = new Acknowledgement(ResultType.NoOperation);


        //    #region Send OnUpdateEVSEStatusResponse event

        //    // OnUpdateEVSEStatusResponse?.Invoke(DateTime.Now,
        //    //                                    Timestamp.Value,
        //    //                                    this,
        //    //                                    this.Id.ToString(),
        //    //                                    EventTrackingId,
        //    //                                    this.RoamingNetwork.Id,
        //    //                                    ActionType,
        //    //                                    GroupedEVSEStatus,
        //    //                                    (UInt32) _NumberOfEVSEStatus,
        //    //                                    RequestTimeout,
        //    //                                    result,
        //    //                                    DateTime.Now - Timestamp.Value);

        //    #endregion

        //    return result;

        //}

        //#endregion


        //#region PushEVSEStatus(EVSEStatusDiff, ...)

        ///// <summary>
        ///// Send EVSE status updates.
        ///// </summary>
        ///// <param name="EVSEStatusDiff">An EVSE status diff.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        ////async Task

        ////    IPushStatus2Remote.PushEVSEStatus(EVSEStatusDiff      EVSEStatusDiff,

        ////                               DateTime?           Timestamp,
        ////                               CancellationToken?  CancellationToken,
        ////                               EventTracking_Id    EventTrackingId,
        ////                               TimeSpan?           RequestTimeout)

        ////{

        ////    await Task.FromResult("");

        ////}

        //#endregion

        #endregion

        #region Receive incoming AuthStart/-Stop

        //#region AuthorizeStart(ChargingStationOperatorId, AuthToken, ChargingProductId, SessionId, ...)

        ///// <summary>
        ///// Create an authorize start request.
        ///// </summary>
        ///// <param name="ChargingStationOperatorId">An Charging Station Operator identification.</param>
        ///// <param name="AuthToken">A (RFID) user identification.</param>
        ///// <param name="ChargingProductId">An optional charging product identification.</param>
        ///// <param name="SessionId">An optional session identification.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //async Task<AuthStartResult>

        //    IRemoteAuthorizeStartStop.AuthorizeStart(ChargingStationOperator_Id  ChargingStationOperatorId,
        //                                             Auth_Token                  AuthToken,
        //                                             ChargingProduct_Id?         ChargingProductId,
        //                                             ChargingSession_Id?         SessionId,

        //                                             DateTime?                   Timestamp,
        //                                             CancellationToken?          CancellationToken,
        //                                             EventTracking_Id            EventTrackingId,
        //                                             TimeSpan?                   RequestTimeout)

        //{

        //    #region Initial checks

        //    if (ChargingStationOperatorId == null)
        //        throw new ArgumentNullException(nameof(ChargingStationOperatorId), "The given parameter must not be null!");

        //    if (AuthToken  == null)
        //        throw new ArgumentNullException(nameof(AuthToken),  "The given parameter must not be null!");

        //    #endregion

        //    TokenAuthorizationResultType AuthenticationResult;

        //    if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
        //    {

        //        #region Authorized

        //        if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
        //        {

        //            var _SessionId = ChargingSession_Id.New;

        //            SessionDatabase.TryAdd(_SessionId, new SessionInfo(AuthToken));

        //            return AuthStartResult.Authorized(AuthorizatorId,
        //                                              _SessionId,
        //                                              EVSP.Id);

        //        }

        //        #endregion

        //        #region Token is blocked!

        //        else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
        //            return AuthStartResult.Blocked(AuthorizatorId,
        //                                           EVSP.Id,
        //                                           "Token is blocked!");

        //        #endregion

        //        #region ...fall through!

        //        else
        //            return AuthStartResult.Unspecified(AuthorizatorId);

        //        #endregion

        //    }

        //    #region Unkown Token!

        //    return AuthStartResult.NotAuthorized(AuthorizatorId,
        //                                         EVSP.Id,
        //                                         "Unkown token!");

        //    #endregion

        //}

        //#endregion

        //#region AuthorizeStart(ChargingStationOperatorId, AuthToken, EVSEId, ChargingProductId, SessionId, ...)

        ///// <summary>
        ///// Create an authorize start request at the given EVSE.
        ///// </summary>
        ///// <param name="ChargingStationOperatorId">An Charging Station Operator identification.</param>
        ///// <param name="AuthToken">A (RFID) user identification.</param>
        ///// <param name="EVSEId">The unique identification of an EVSE.</param>
        ///// <param name="ChargingProductId">An optional charging product identification.</param>
        ///// <param name="SessionId">An optional session identification.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //async Task<AuthStartEVSEResult>

        //    IRemoteAuthorizeStartStop.AuthorizeStart(ChargingStationOperator_Id  ChargingStationOperatorId,
        //                                             Auth_Token                  AuthToken,
        //                                             EVSE_Id                     EVSEId,
        //                                             ChargingProduct_Id?         ChargingProductId,
        //                                             ChargingSession_Id?         SessionId,

        //                                             DateTime?                   Timestamp,
        //                                             CancellationToken?          CancellationToken,
        //                                             EventTracking_Id            EventTrackingId,
        //                                             TimeSpan?                   RequestTimeout)

        //{

        //    #region Initial checks

        //    if (ChargingStationOperatorId == null)
        //        throw new ArgumentNullException(nameof(ChargingStationOperatorId), "The given parameter must not be null!");

        //    if (AuthToken  == null)
        //        throw new ArgumentNullException(nameof(AuthToken),  "The given parameter must not be null!");

        //    #endregion

        //    TokenAuthorizationResultType AuthenticationResult;

        //    if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
        //    {

        //        #region Authorized

        //        if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
        //        {

        //            var _SessionId = ChargingSession_Id.New;

        //            SessionDatabase.TryAdd(_SessionId, new SessionInfo(AuthToken));

        //            return AuthStartEVSEResult.Authorized(AuthorizatorId,
        //                                                  _SessionId,
        //                                                  EVSP.Id);

        //        }

        //        #endregion

        //        #region Token is blocked!

        //        else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
        //            return AuthStartEVSEResult.Blocked(AuthorizatorId,
        //                                               EVSP.Id,
        //                                               "Token is blocked!");

        //        #endregion

        //        #region ...fall through!

        //        else
        //            return AuthStartEVSEResult.Unspecified(AuthorizatorId);

        //        #endregion

        //    }

        //    #region Unkown Token!

        //    return AuthStartEVSEResult.NotAuthorized(AuthorizatorId,
        //                                             EVSP.Id,
        //                                             "Unkown token!");

        //    #endregion

        //}

        //#endregion

        //#region AuthorizeStart(ChargingStationOperatorId, AuthToken, ChargingStationId, ChargingProductId, SessionId, ...)

        ///// <summary>
        ///// Create an AuthorizeStart request at the given charging station.
        ///// </summary>
        ///// <param name="ChargingStationOperatorId">An Charging Station Operator identification.</param>
        ///// <param name="AuthToken">A (RFID) user identification.</param>
        ///// <param name="ChargingStationId">The unique identification of a charging station.</param>
        ///// <param name="ChargingProductId">An optional charging product identification.</param>
        ///// <param name="SessionId">An optional session identification.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //async Task<AuthStartChargingStationResult>

        //    IRemoteAuthorizeStartStop.AuthorizeStart(ChargingStationOperator_Id  ChargingStationOperatorId,
        //                                             Auth_Token                  AuthToken,
        //                                             ChargingStation_Id          ChargingStationId,
        //                                             ChargingProduct_Id?         ChargingProductId,
        //                                             ChargingSession_Id?         SessionId,

        //                                             DateTime?                   Timestamp,
        //                                             CancellationToken?          CancellationToken,
        //                                             EventTracking_Id            EventTrackingId,
        //                                             TimeSpan?                   RequestTimeout)

        //{

        //    #region Initial checks

        //    if (ChargingStationOperatorId        == null)
        //        throw new ArgumentNullException(nameof(ChargingStationOperatorId),         "The given parameter must not be null!");

        //    if (AuthToken         == null)
        //        throw new ArgumentNullException(nameof(AuthToken),          "The given parameter must not be null!");

        //    if (ChargingStationId == null)
        //        throw new ArgumentNullException(nameof(ChargingStationId),  "The given parameter must not be null!");

        //    #endregion

        //    TokenAuthorizationResultType AuthenticationResult;

        //    if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
        //    {

        //        #region Authorized

        //        if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
        //        {

        //            var _SessionId = ChargingSession_Id.New;

        //            SessionDatabase.TryAdd(_SessionId, new SessionInfo(AuthToken));

        //            return AuthStartChargingStationResult.Authorized(AuthorizatorId,
        //                                                             _SessionId,
        //                                                             EVSP.Id);

        //        }

        //        #endregion

        //        #region Token is blocked!

        //        else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
        //            return AuthStartChargingStationResult.Blocked(AuthorizatorId,
        //                                                          EVSP.Id,
        //                                                          "Token is blocked!");

        //        #endregion

        //        #region ...fall through!

        //        else
        //            return AuthStartChargingStationResult.Unspecified(AuthorizatorId);

        //        #endregion

        //    }

        //    #region Unkown Token!

        //    return AuthStartChargingStationResult.NotAuthorized(AuthorizatorId,
        //                                                        EVSP.Id,
        //                                                        "Unkown token!");

        //    #endregion

        //}

        //#endregion


        //#region AuthorizeStop(ChargingStationOperatorId, SessionId, AuthToken, ...)

        ///// <summary>
        ///// Create an authorize stop request.
        ///// </summary>
        ///// <param name="ChargingStationOperatorId">An Charging Station Operator identification.</param>
        ///// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        ///// <param name="AuthToken">A (RFID) user identification.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //async Task<AuthStopResult>

        //    IRemoteAuthorizeStartStop.AuthorizeStop(ChargingStationOperator_Id  ChargingStationOperatorId,
        //                                            ChargingSession_Id          SessionId,
        //                                            Auth_Token                  AuthToken,

        //                                            DateTime?                   Timestamp,
        //                                            CancellationToken?          CancellationToken,
        //                                            EventTracking_Id            EventTrackingId,
        //                                            TimeSpan?                   RequestTimeout)

        //{

        //    #region Initial checks

        //    if (ChargingStationOperatorId == null)
        //        throw new ArgumentNullException(nameof(ChargingStationOperatorId), "The given parameter must not be null!");

        //    if (SessionId  == null)
        //        throw new ArgumentNullException(nameof(SessionId),  "The given parameter must not be null!");

        //    if (AuthToken  == null)
        //        throw new ArgumentNullException(nameof(AuthToken),  "The given parameter must not be null!");

        //    #endregion

        //    #region Check session identification

        //    SessionInfo SessionInfo = null;

        //    if (!SessionDatabase.TryGetValue(SessionId, out SessionInfo))
        //        return AuthStopResult.InvalidSessionId(AuthorizatorId);

        //    #endregion

        //    TokenAuthorizationResultType AuthenticationResult;

        //    if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
        //    {

        //        #region Token is authorized

        //        if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
        //        {

        //            // Authorized
        //            if (SessionInfo.ListOfAuthStopTokens.Contains(AuthToken))
        //                return AuthStopResult.Authorized(AuthorizatorId,
        //                                                 EVSP.Id);

        //            // Invalid Token for SessionId!
        //            else
        //                return AuthStopResult.NotAuthorized(AuthorizatorId,
        //                                                    EVSP.Id,
        //                                                    "Invalid token for given session identification!");

        //        }

        //        #endregion

        //        #region Token is blocked

        //        else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
        //            return AuthStopResult.Blocked(AuthorizatorId,
        //                                          EVSP.Id,
        //                                          "Token is blocked!");

        //        #endregion

        //        #region ...fall through!

        //        else
        //            return AuthStopResult.Unspecified(AuthorizatorId);

        //        #endregion

        //    }

        //    // Unkown Token!
        //    return AuthStopResult.NotAuthorized(AuthorizatorId,
        //                                        EVSP.Id,
        //                                        "Unkown token!");

        //}

        //#endregion

        //#region AuthorizeStop(ChargingStationOperatorId, EVSEId, SessionId, AuthToken, ...)

        ///// <summary>
        ///// Create an authorize stop request at the given EVSE.
        ///// </summary>
        ///// <param name="ChargingStationOperatorId">An Charging Station Operator identification.</param>
        ///// <param name="EVSEId">The unique identification of an EVSE.</param>
        ///// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        ///// <param name="AuthToken">A (RFID) user identification.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //async Task<AuthStopEVSEResult>

        //    IRemoteAuthorizeStartStop.AuthorizeStop(ChargingStationOperator_Id  ChargingStationOperatorId,
        //                                            EVSE_Id                     EVSEId,
        //                                            ChargingSession_Id          SessionId,
        //                                            Auth_Token                  AuthToken,

        //                                            DateTime?                   Timestamp,
        //                                            CancellationToken?          CancellationToken,
        //                                            EventTracking_Id            EventTrackingId,
        //                                            TimeSpan?                   RequestTimeout)

        //{

        //    #region Initial checks

        //    if (ChargingStationOperatorId == null)
        //        throw new ArgumentNullException(nameof(ChargingStationOperatorId), "The given parameter must not be null!");

        //    if (SessionId  == null)
        //        throw new ArgumentNullException(nameof(SessionId),  "The given parameter must not be null!");

        //    if (AuthToken  == null)
        //        throw new ArgumentNullException(nameof(AuthToken),  "The given parameter must not be null!");

        //    if (EVSEId == null)
        //        throw new ArgumentNullException(nameof(EVSEId),     "The given parameter must not be null!");

        //    #endregion

        //    #region Check session identification

        //    SessionInfo SessionInfo = null;

        //    if (!SessionDatabase.TryGetValue(SessionId, out SessionInfo))
        //        return AuthStopEVSEResult.InvalidSessionId(AuthorizatorId);

        //    #endregion

        //    TokenAuthorizationResultType AuthenticationResult;

        //    if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
        //    {

        //        #region Token is authorized

        //        if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
        //        {

        //            // Authorized
        //            if (SessionInfo.ListOfAuthStopTokens.Contains(AuthToken))
        //                return AuthStopEVSEResult.Authorized(AuthorizatorId,
        //                                                     EVSP.Id);

        //            // Invalid Token for SessionId!
        //            else
        //                return AuthStopEVSEResult.NotAuthorized(AuthorizatorId,
        //                                                        EVSP.Id,
        //                                                        "Invalid token for given session identification!");

        //        }

        //        #endregion

        //        #region Token is blocked

        //        else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
        //            return AuthStopEVSEResult.Blocked(AuthorizatorId,
        //                                              EVSP.Id,
        //                                              "Token is blocked!");

        //        #endregion

        //        #region ...fall through!

        //        else
        //            return AuthStopEVSEResult.Unspecified(AuthorizatorId);

        //        #endregion

        //    }

        //    // Unkown Token!
        //    return AuthStopEVSEResult.NotAuthorized(AuthorizatorId,
        //                                            EVSP.Id,
        //                                            "Unkown token!");

        //}

        //#endregion

        //#region AuthorizeStop(ChargingStationOperatorId, ChargingStationId, SessionId, AuthToken, ...)

        ///// <summary>
        ///// Create an authorize stop request at the given charging station.
        ///// </summary>
        ///// <param name="ChargingStationOperatorId">An Charging Station Operator identification.</param>
        ///// <param name="ChargingStationId">A charging station identification.</param>
        ///// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        ///// <param name="AuthToken">A (RFID) user identification.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //async Task<AuthStopChargingStationResult>

        //    IRemoteAuthorizeStartStop.AuthorizeStop(ChargingStationOperator_Id  ChargingStationOperatorId,
        //                                            ChargingStation_Id          ChargingStationId,
        //                                            ChargingSession_Id          SessionId,
        //                                            Auth_Token                  AuthToken,

        //                                            DateTime?                   Timestamp,
        //                                            CancellationToken?          CancellationToken,
        //                                            EventTracking_Id            EventTrackingId,
        //                                            TimeSpan?                   RequestTimeout)

        //{

        //    #region Initial checks

        //    if (ChargingStationOperatorId == null)
        //        throw new ArgumentNullException(nameof(ChargingStationOperatorId), "The given parameter must not be null!");

        //    if (SessionId  == null)
        //        throw new ArgumentNullException(nameof(SessionId),  "The given parameter must not be null!");

        //    if (AuthToken  == null)
        //        throw new ArgumentNullException(nameof(AuthToken),  "The given parameter must not be null!");

        //    #endregion

        //    return AuthStopChargingStationResult.Error(AuthorizatorId);

        //}

        //#endregion

        //#endregion

        #region Receive incoming ChargeDetailRecords

        //#region SendChargeDetailRecord(ChargeDetailRecord, ...)

        ///// <summary>
        ///// Send a charge detail record.
        ///// </summary>
        ///// <param name="ChargeDetailRecord">A charge detail record.</param>
        ///// 
        ///// <param name="Timestamp">The optional timestamp of the request.</param>
        ///// <param name="CancellationToken">An optional token to cancel this request.</param>
        ///// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        ///// <param name="RequestTimeout">An optional timeout for this request.</param>
        //async Task<SendCDRResult>

        //    IRemoteSendChargeDetailRecord.SendChargeDetailRecord(ChargeDetailRecord  ChargeDetailRecord,

        //                                                         DateTime?           Timestamp,
        //                                                         CancellationToken?  CancellationToken,
        //                                                         EventTracking_Id    EventTrackingId,
        //                                                         TimeSpan?           RequestTimeout)
        //{

        //    #region Initial checks

        //    if (ChargeDetailRecord == null)
        //        throw new ArgumentNullException(nameof(ChargeDetailRecord),  "The given charge detail record must not be null!");

        //    #endregion

        //    SessionInfo _SessionInfo = null;

        //    //ToDo: Add events!


        //    Debug.WriteLine("Received a CDR: " + ChargeDetailRecord.SessionId.ToString());


        //    if (ChargeDetailRecordDatabase.ContainsKey(ChargeDetailRecord.SessionId))
        //        return SendCDRResult.InvalidSessionId(AuthorizatorId);


        //    if (ChargeDetailRecordDatabase.TryAdd(ChargeDetailRecord.SessionId, ChargeDetailRecord))
        //    {

        //        SessionDatabase.TryRemove(ChargeDetailRecord.SessionId, out _SessionInfo);

        //        return SendCDRResult.Forwarded(AuthorizatorId);

        //    }

        //    //roamingprovider.OnEVSEStatusPush   += (Timestamp, Sender, SenderId, RoamingNetworkId, ActionType, GroupedEVSEs, NumberOfEVSEs) => {
        //    //    Console.WriteLine("[" + Timestamp + "] " + RoamingNetworkId.ToString() + ": Pushing " + NumberOfEVSEs + " EVSE status towards " + SenderId + "(" + ActionType + ")");
        //    //};

        //    //    Console.WriteLine("[" + Timestamp + "] " + RoamingNetworkId.ToString() + ": Pushed "  + NumberOfEVSEs + " EVSE status towards " + SenderId + "(" + ActionType + ") => " + Result.Result + " (" + Duration.TotalSeconds + " sec)");

        //    //    if (Result.Result == false)
        //    //    {

        //    //        var EMailTask = API_SMTPClient.Send(HubjectEVSEStatusPushFailedEMailProvider(Timestamp,
        //    //                                                                                       Sender,
        //    //                                                                                       SenderId,
        //    //                                                                                       RoamingNetworkId,
        //    //                                                                                       ActionType,
        //    //                                                                                       GroupedEVSEs,
        //    //                                                                                       NumberOfEVSEs,
        //    //                                                                                       Result,
        //    //                                                                                       Duration));

        //    //        EMailTask.Wait(TimeSpan.FromSeconds(30));

        //    //    }

        //    //};

        //    return SendCDRResult.InvalidSessionId(AuthorizatorId);

        //}

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
        /// <param name="EVSEId">The unique identification of the EVSE to be reserved.</param>
        /// <param name="StartTime">The starting time of the reservation.</param>
        /// <param name="Duration">The duration of the reservation.</param>
        /// <param name="ReservationId">An optional unique identification of the reservation. Mandatory for updates.</param>
        /// <param name="eMAId">An optional unique identification of e-Mobility account/customer requesting this reservation.</param>
        /// <param name="ChargingProductId">An optional unique identification of the charging product to be reserved.</param>
        /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
        /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
        /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<ReservationResult>

            Reserve(EVSE_Id                           EVSEId,
                    DateTime?                         StartTime           = null,
                    TimeSpan?                         Duration            = null,
                    ChargingReservation_Id?           ReservationId       = null,
                    eMobilityAccount_Id?              eMAId               = null,
                    ChargingProduct_Id?               ChargingProductId   = null,
                    IEnumerable<Auth_Token>           AuthTokens          = null,
                    IEnumerable<eMobilityAccount_Id>  eMAIds              = null,
                    IEnumerable<UInt32>               PINs                = null,

                    DateTime?                         Timestamp           = null,
                    CancellationToken?                CancellationToken   = null,
                    EventTracking_Id                  EventTrackingId     = null,
                    TimeSpan?                         RequestTimeout      = null)

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

                OnReserveEVSE?.Invoke(DateTime.Now,
                                      Timestamp.Value,
                                      this,
                                      EventTrackingId,
                                      RoamingNetwork.Id,
                                      ReservationId,
                                      EVSEId,
                                      StartTime,
                                      Duration,
                                      Id,
                                      eMAId,
                                      ChargingProductId,
                                      AuthTokens,
                                      eMAIds,
                                      PINs,
                                      RequestTimeout);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnReserveEVSE));
            }

            #endregion


            var response = await RoamingNetwork.Reserve(EVSEId,
                                                        StartTime,
                                                        Duration,
                                                        ReservationId,
                                                        Id,
                                                        eMAId,
                                                        ChargingProductId,
                                                        AuthTokens,
                                                        eMAIds,
                                                        PINs,

                                                        Timestamp,
                                                        CancellationToken,
                                                        EventTrackingId,
                                                        RequestTimeout);


            #region Send OnEVSEReserved event

            Runtime.Stop();

            try
            {

                OnEVSEReserved?.Invoke(DateTime.Now,
                                       Timestamp.Value,
                                       this,
                                       EventTrackingId,
                                       RoamingNetwork.Id,
                                       ReservationId,
                                       EVSEId,
                                       StartTime,
                                       Duration,
                                       Id,
                                       eMAId,
                                       ChargingProductId,
                                       AuthTokens,
                                       eMAIds,
                                       PINs,
                                       response,
                                       Runtime.Elapsed,
                                       RequestTimeout);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnEVSEReserved));
            }

            #endregion

            return response;

        }

        #endregion

        #region CancelReservation(...ReservationId, Reason, EVSEId = null, ...)

        /// <summary>
        /// Cancel the given charging reservation.
        /// </summary>
        /// <param name="ReservationId">The unique charging reservation identification.</param>
        /// <param name="Reason">A reason for this cancellation.</param>
        /// <param name="EVSEId">An optional identification of the EVSE.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<CancelReservationResult>

            CancelReservation(ChargingReservation_Id                 ReservationId,
                              ChargingReservationCancellationReason  Reason,
                              EVSE_Id?                               EVSEId             = null,

                              DateTime?                              Timestamp          = null,
                              CancellationToken?                     CancellationToken  = null,
                              EventTracking_Id                       EventTrackingId    = null,
                              TimeSpan?                              RequestTimeout     = null)

        {

            var response = await RoamingNetwork.CancelReservation(ReservationId,
                                                                  Reason,
                                                                  Id,
                                                                  EVSEId,

                                                                  Timestamp,
                                                                  CancellationToken,
                                                                  EventTrackingId,
                                                                  RequestTimeout);


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
        /// <param name="EVSEId">The unique identification of the EVSE to be started.</param>
        /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
        /// <param name="ReservationId">The unique identification for a charging reservation.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStartEVSEResult>

            RemoteStart(EVSE_Id                  EVSEId,
                        ChargingProduct_Id?      ChargingProductId   = null,
                        ChargingReservation_Id?  ReservationId       = null,
                        ChargingSession_Id?      SessionId           = null,
                        eMobilityAccount_Id?     eMAId               = null,

                        DateTime?                Timestamp           = null,
                        CancellationToken?       CancellationToken   = null,
                        EventTracking_Id         EventTrackingId     = null,
                        TimeSpan?                RequestTimeout      = null)

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

                OnRemoteEVSEStart?.Invoke(DateTime.Now,
                                          Timestamp.Value,
                                          this,
                                          EventTrackingId,
                                          RoamingNetwork.Id,
                                          EVSEId,
                                          ChargingProductId,
                                          ReservationId,
                                          SessionId,
                                          Id,
                                          eMAId,
                                          RequestTimeout);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnRemoteEVSEStart));
            }

            #endregion


            var response = await RoamingNetwork.RemoteStart(EVSEId,
                                                            ChargingProductId,
                                                            ReservationId,
                                                            SessionId,
                                                            Id,
                                                            eMAId,

                                                            Timestamp,
                                                            CancellationToken,
                                                            EventTrackingId,
                                                            RequestTimeout);


            #region Send OnRemoteEVSEStarted event

            Runtime.Stop();

            try
            {

                OnRemoteEVSEStarted?.Invoke(DateTime.Now,
                                            Timestamp.Value,
                                            this,
                                            EventTrackingId,
                                            RoamingNetwork.Id,
                                            EVSEId,
                                            ChargingProductId,
                                            ReservationId,
                                            SessionId,
                                            Id,
                                            eMAId,
                                            RequestTimeout,
                                            response,
                                            Runtime.Elapsed);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnRemoteEVSEStarted));
            }

            #endregion

            return response;

        }

        #endregion

        #region RemoteStop(...EVSEId, SessionId, ReservationHandling, eMAId = null, ...)

        /// <summary>
        /// Stop the given charging session at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of the EVSE to be stopped.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ReservationHandling">Whether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStopEVSEResult>

            RemoteStop(EVSE_Id               EVSEId,
                       ChargingSession_Id    SessionId,
                       ReservationHandling   ReservationHandling,
                       eMobilityAccount_Id?  eMAId               = null,

                       DateTime?             Timestamp           = null,
                       CancellationToken?    CancellationToken   = null,
                       EventTracking_Id      EventTrackingId     = null,
                       TimeSpan?             RequestTimeout      = null)

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

                OnRemoteEVSEStop?.Invoke(DateTime.Now,
                                         Timestamp.Value,
                                         this,
                                         EventTrackingId,
                                         RoamingNetwork.Id,
                                         EVSEId,
                                         SessionId,
                                         ReservationHandling,
                                         Id,
                                         eMAId,
                                         RequestTimeout);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnRemoteEVSEStop));
            }

            #endregion


            var response = await RoamingNetwork.RemoteStop(EVSEId,
                                                           SessionId,
                                                           ReservationHandling,
                                                           Id,
                                                           eMAId,

                                                           Timestamp,
                                                           CancellationToken,
                                                           EventTrackingId,
                                                           RequestTimeout);


            #region Send OnRemoteEVSEStopped event

            Runtime.Stop();

            try
            {

                OnRemoteEVSEStopped?.Invoke(DateTime.Now,
                                            Timestamp.Value,
                                            this,
                                            EventTrackingId,
                                            RoamingNetwork.Id,
                                            EVSEId,
                                            SessionId,
                                            ReservationHandling,
                                            Id,
                                            eMAId,
                                            RequestTimeout,
                                            response,
                                            Runtime.Elapsed);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnRemoteEVSEStopped));
            }

            #endregion

            return response;

        }

        #endregion

        #endregion


    }

}
