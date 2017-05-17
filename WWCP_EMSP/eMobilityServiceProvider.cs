/*
 * Copyright (c) 2014-2017 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

        private readonly ConcurrentDictionary<ChargingPool_Id,    ChargingPool>                  ChargingPools;

        private readonly ConcurrentDictionary<Auth_Token,         TokenAuthorizationResultType>  AuthorizationDatabase;
        private readonly ConcurrentDictionary<ChargingSession_Id, SessionInfo>                   SessionDatabase;
        private readonly ConcurrentDictionary<ChargingSession_Id, ChargeDetailRecord>            ChargeDetailRecordDatabase;

        #endregion

        #region Properties

        /// <summary>
        /// The unique identification of the e-mobility service provider.
        /// </summary>
        public eMobilityProvider_Id  Id                 { get; }

        IId IReceiveAuthorizeStartStop.AuthId
            => Id;

        public Boolean DisableAuthentication           { get; set; }
        public Boolean DisableSendChargeDetailRecords  { get; set; }


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
        public event OnReserveEVSERequestDelegate              OnReserveEVSERequest;

        /// <summary>
        /// An event fired whenever an EVSE was reserved.
        /// </summary>
        public event OnReserveEVSEResponseDelegate             OnReservedEVSEResponse;

        #endregion

        // CancelReservation

        #region OnRemote...Start / OnRemote...Started

        /// <summary>
        /// An event fired whenever a remote start EVSE command was received.
        /// </summary>
        public event OnRemoteStartEVSERequestDelegate              OnRemoteEVSEStartRequest;

        /// <summary>
        /// An event fired whenever a remote start EVSE command completed.
        /// </summary>
        public event OnRemoteStartEVSEResponseDelegate             OnRemoteEVSEStartResponse;

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



        // Incoming events from the roaming network

        public event OnAuthorizeStartRequestDelegate                  OnAuthorizeStartRequest;
        public event OnAuthorizeStartResponseDelegate                 OnAuthorizeStartResponse;

        public event OnAuthorizeEVSEStartRequestDelegate              OnAuthorizeEVSEStartRequest;
        public event OnAuthorizeEVSEStartResponseDelegate             OnAuthorizeEVSEStartResponse;

        public event OnAuthorizeChargingStationStartRequestDelegate   OnAuthorizeChargingStationStartRequest;
        public event OnAuthorizeChargingStationStartResponseDelegate  OnAuthorizeChargingStationStartResponse;


        public event OnAuthorizeStopRequestDelegate                   OnAuthorizeStopRequest;
        public event OnAuthorizeStopResponseDelegate                  OnAuthorizeStopResponse;

        public event OnAuthorizeEVSEStopRequestDelegate               OnAuthorizeEVSEStopRequest;
        public event OnAuthorizeEVSEStopResponseDelegate              OnAuthorizeEVSEStopResponse;

        public event OnAuthorizeChargingStationStopRequestDelegate    OnAuthorizeChargingStationStopRequest;
        public event OnAuthorizeChargingStationStopResponseDelegate   OnAuthorizeChargingStationStopResponse;

        #endregion

        #region Constructor(s)

        internal eMobilityServiceProvider(eMobilityProvider_Id  Id,
                                          RoamingNetwork        RoamingNetwork)
        {

            this.Id                          = Id;
            this.RoamingNetwork              = RoamingNetwork;

            this.ChargingPools               = new ConcurrentDictionary<ChargingPool_Id,    ChargingPool>();

            this.AuthorizationDatabase       = new ConcurrentDictionary<Auth_Token,         TokenAuthorizationResultType>();
            this.SessionDatabase             = new ConcurrentDictionary<ChargingSession_Id, SessionInfo>();
            this.ChargeDetailRecordDatabase  = new ConcurrentDictionary<ChargingSession_Id, ChargeDetailRecord>();

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

        #region Receive incoming Data/Status

        #region (Set/Add/Update/Delete) EVSE(s)...

        #region SetStaticData   (EVSE, ...)

        /// <summary>
        /// Set the given EVSE as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSE">An EVSE to upload.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.SetStaticData(EVSE                EVSE,

                                          DateTime?           Timestamp,
                                          CancellationToken?  CancellationToken,
                                          EventTracking_Id    EventTrackingId,
                                          TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (EVSE == null)
                throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region AddStaticData   (EVSE, ...)

        /// <summary>
        /// Add the given EVSE to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSE">An EVSE to upload.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.AddStaticData(EVSE                EVSE,

                                          DateTime?           Timestamp,
                                          CancellationToken?  CancellationToken,
                                          EventTracking_Id    EventTrackingId,
                                          TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (EVSE == null)
                throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateStaticData(EVSE, PropertyName = null, OldValue = null, NewValue = null, ...)

        /// <summary>
        /// Update the static data of the given EVSE.
        /// The EVSE can be uploaded as a whole, or just a single property of the EVSE.
        /// </summary>
        /// <param name="EVSE">An EVSE to update.</param>
        /// <param name="PropertyName">The name of the EVSE property to update.</param>
        /// <param name="OldValue">The old value of the EVSE property to update.</param>
        /// <param name="NewValue">The new value of the EVSE property to update.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.UpdateStaticData(EVSE                EVSE,
                                             String              PropertyName,
                                             Object              OldValue,
                                             Object              NewValue,

                                             DateTime?           Timestamp,
                                             CancellationToken?  CancellationToken,
                                             EventTracking_Id    EventTrackingId,
                                             TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (EVSE == null)
                throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region DeleteStaticData(EVSE, ...)

        /// <summary>
        /// Delete the static data of the given EVSE.
        /// </summary>
        /// <param name="EVSE">An EVSE to delete.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.DeleteStaticData(EVSE                EVSE,

                                             DateTime?           Timestamp,
                                             CancellationToken?  CancellationToken,
                                             EventTracking_Id    EventTrackingId,
                                             TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (EVSE == null)
                throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion


        #region SetStaticData   (EVSEs, ...)

        /// <summary>
        /// Set the given enumeration of EVSEs as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.SetStaticData(IEnumerable<EVSE>   EVSEs,

                                          DateTime?           Timestamp,
                                          CancellationToken?  CancellationToken,
                                          EventTracking_Id    EventTrackingId,
                                          TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (EVSEs == null)
                throw new ArgumentNullException(nameof(EVSEs), "The given enumeration of EVSEs must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region AddStaticData   (EVSEs, ...)

        /// <summary>
        /// Add the given enumeration of EVSEs to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.AddStaticData(IEnumerable<EVSE>   EVSEs,

                                          DateTime?           Timestamp,
                                          CancellationToken?  CancellationToken,
                                          EventTracking_Id    EventTrackingId,
                                          TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (EVSEs == null)
                throw new ArgumentNullException(nameof(EVSEs), "The given enumeration of EVSEs must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateStaticData(EVSEs, ...)

        /// <summary>
        /// Update the given enumeration of EVSEs within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.UpdateStaticData(IEnumerable<EVSE>   EVSEs,

                                             DateTime?           Timestamp,
                                             CancellationToken?  CancellationToken,
                                             EventTracking_Id    EventTrackingId,
                                             TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (EVSEs == null)
                throw new ArgumentNullException(nameof(EVSEs), "The given enumeration of EVSEs must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region DeleteStaticData(EVSEs, ...)

        /// <summary>
        /// Delete the given enumeration of EVSEs from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.DeleteStaticData(IEnumerable<EVSE>   EVSEs,

                                             DateTime?           Timestamp,
                                             CancellationToken?  CancellationToken,
                                             EventTracking_Id    EventTrackingId,
                                             TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (EVSEs == null)
                throw new ArgumentNullException(nameof(EVSEs), "The given enumeration of EVSEs must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion


        #region UpdateEVSEAdminStatus(AdminStatusUpdates, ...)

        /// <summary>
        /// Update the given enumeration of EVSE admin status updates.
        /// </summary>
        /// <param name="AdminStatusUpdates">An enumeration of EVSE admin status updates.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveStatus.UpdateAdminStatus(IEnumerable<EVSEAdminStatusUpdate>  AdminStatusUpdates,

                                                DateTime?                           Timestamp,
                                                CancellationToken?                  CancellationToken,
                                                EventTracking_Id                    EventTrackingId,
                                                TimeSpan?                           RequestTimeout)

        {

            #region Initial checks

            if (AdminStatusUpdates == null)
                throw new ArgumentNullException(nameof(AdminStatusUpdates), "The given enumeration of EVSE admin status updates must not be null!");


            Acknowledgement result;

            #endregion

            result = new Acknowledgement(ResultType.NoOperation);

            return result;

        }

        #endregion

        #region UpdateEVSEStatus     (StatusUpdates, ...)

        /// <summary>
        /// Update the given enumeration of EVSE status updates.
        /// </summary>
        /// <param name="StatusUpdates">An enumeration of EVSE status updates.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveStatus.UpdateStatus(IEnumerable<EVSEStatusUpdate>  StatusUpdates,

                                           DateTime?                      Timestamp,
                                           CancellationToken?             CancellationToken,
                                           EventTracking_Id               EventTrackingId,
                                           TimeSpan?                      RequestTimeout)

        {

            #region Initial checks

            if (StatusUpdates == null)
                throw new ArgumentNullException(nameof(StatusUpdates), "The given enumeration of evse status updates must not be null!");


            Acknowledgement result;

            #endregion

            result = new Acknowledgement(ResultType.NoOperation);

            return result;

        }

        #endregion

        #endregion

        #region (Set/Add/Update/Delete) Charging station(s)...

        #region SetStaticData   (ChargingStation, ...)

        /// <summary>
        /// Set the EVSE data of the given charging station as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.SetStaticData(ChargingStation     ChargingStation,

                                          DateTime?           Timestamp,
                                          CancellationToken?  CancellationToken,
                                          EventTracking_Id    EventTrackingId,
                                          TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation), "The given charging station must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region AddStaticData   (ChargingStation, ...)

        /// <summary>
        /// Add the EVSE data of the given charging station to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.AddStaticData(ChargingStation     ChargingStation,

                                          DateTime?           Timestamp,
                                          CancellationToken?  CancellationToken,
                                          EventTracking_Id    EventTrackingId,
                                          TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation), "The given charging station must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateStaticData(ChargingStation, PropertyName = null, OldValue = null, NewValue = null, ...)

        /// <summary>
        /// Update the EVSE data of the given charging station within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="PropertyName">The name of the charging station property to update.</param>
        /// <param name="OldValue">The old value of the charging station property to update.</param>
        /// <param name="NewValue">The new value of the charging station property to update.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.UpdateStaticData(ChargingStation     ChargingStation,
                                             String              PropertyName,
                                             Object              OldValue,
                                             Object              NewValue,

                                             DateTime?           Timestamp,
                                             CancellationToken?  CancellationToken,
                                             EventTracking_Id    EventTrackingId,
                                             TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation), "The given charging station must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region DeleteStaticData(ChargingStation, ...)

        /// <summary>
        /// Delete the EVSE data of the given charging station from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.DeleteStaticData(ChargingStation     ChargingStation,

                                             DateTime?           Timestamp,
                                             CancellationToken?  CancellationToken,
                                             EventTracking_Id    EventTrackingId,
                                             TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation), "The given charging station must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion


        #region SetStaticData   (ChargingStations, ...)

        /// <summary>
        /// Set the EVSE data of the given enumeration of charging stations as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.SetStaticData(IEnumerable<ChargingStation>  ChargingStations,

                                          DateTime?                     Timestamp,
                                          CancellationToken?            CancellationToken,
                                          EventTracking_Id              EventTrackingId,
                                          TimeSpan?                     RequestTimeout)

        {

            #region Initial checks

            if (ChargingStations == null)
                throw new ArgumentNullException(nameof(ChargingStations), "The given enumeration of charging stations must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region AddStaticData   (ChargingStations, ...)

        /// <summary>
        /// Add the EVSE data of the given enumeration of charging stations to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.AddStaticData(IEnumerable<ChargingStation>  ChargingStations,

                                          DateTime?                     Timestamp,
                                          CancellationToken?            CancellationToken,
                                          EventTracking_Id              EventTrackingId,
                                          TimeSpan?                     RequestTimeout)

        {

            #region Initial checks

            if (ChargingStations == null)
                throw new ArgumentNullException(nameof(ChargingStations), "The given enumeration of charging stations must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateStaticData(ChargingStations, ...)

        /// <summary>
        /// Update the EVSE data of the given enumeration of charging stations within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.UpdateStaticData(IEnumerable<ChargingStation>  ChargingStations,

                                             DateTime?                     Timestamp,
                                             CancellationToken?            CancellationToken,
                                             EventTracking_Id              EventTrackingId,
                                             TimeSpan?                     RequestTimeout)

        {

            #region Initial checks

            if (ChargingStations == null)
                throw new ArgumentNullException(nameof(ChargingStations), "The given enumeration of charging stations must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region DeleteStaticData(ChargingStations, ...)

        /// <summary>
        /// Delete the EVSE data of the given enumeration of charging stations from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.DeleteStaticData(IEnumerable<ChargingStation>  ChargingStations,

                                             DateTime?                     Timestamp,
                                             CancellationToken?            CancellationToken,
                                             EventTracking_Id              EventTrackingId,
                                             TimeSpan?                     RequestTimeout)

        {

            #region Initial checks

            if (ChargingStations == null)
                throw new ArgumentNullException(nameof(ChargingStations), "The given enumeration of charging stations must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion


        #region UpdateChargingStationAdminStatus(AdminStatusUpdates, ...)

        /// <summary>
        /// Update the given enumeration of charging station admin status updates.
        /// </summary>
        /// <param name="AdminStatusUpdates">An enumeration of charging station admin status updates.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveStatus.UpdateAdminStatus(IEnumerable<ChargingStationAdminStatusUpdate>  AdminStatusUpdates,

                                                DateTime?                                      Timestamp,
                                                CancellationToken?                             CancellationToken,
                                                EventTracking_Id                               EventTrackingId,
                                                TimeSpan?                                      RequestTimeout)

        {

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateChargingStationStatus     (StatusUpdates, ...)

        /// <summary>
        /// Update the given enumeration of charging station status updates.
        /// </summary>
        /// <param name="StatusUpdates">An enumeration of charging station status updates.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveStatus.UpdateStatus(IEnumerable<ChargingStationStatusUpdate>  StatusUpdates,

                                           DateTime?                                 Timestamp,
                                           CancellationToken?                        CancellationToken,
                                           EventTracking_Id                          EventTrackingId,
                                           TimeSpan?                                 RequestTimeout)

        {

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #endregion

        #region (Set/Add/Update/Delete) Charging pool(s)...

        #region SetStaticData   (ChargingPool, ...)

        /// <summary>
        /// Set the EVSE data of the given charging pool as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.SetStaticData(ChargingPool        ChargingPool,

                                          DateTime?           Timestamp,
                                          CancellationToken?  CancellationToken,
                                          EventTracking_Id    EventTrackingId,
                                          TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException(nameof(ChargingPool), "The given charging pool must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region AddStaticData   (ChargingPool, ...)

        /// <summary>
        /// Add the EVSE data of the given charging pool to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.AddStaticData(ChargingPool        ChargingPool,

                                          DateTime?           Timestamp,
                                          CancellationToken?  CancellationToken,
                                          EventTracking_Id    EventTrackingId,
                                          TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException(nameof(ChargingPool), "The given charging pool must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateStaticData(ChargingPool, PropertyName = null, OldValue = null, NewValue = null, ...)

        /// <summary>
        /// Update the EVSE data of the given charging pool within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="PropertyName">The name of the charging pool property to update.</param>
        /// <param name="OldValue">The old value of the charging pool property to update.</param>
        /// <param name="NewValue">The new value of the charging pool property to update.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.UpdateStaticData(ChargingPool        ChargingPool,
                                             String              PropertyName,
                                             Object              OldValue,
                                             Object              NewValue,

                                             DateTime?           Timestamp,
                                             CancellationToken?  CancellationToken,
                                             EventTracking_Id    EventTrackingId,
                                             TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException(nameof(ChargingPool), "The given charging pool must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region DeleteStaticData(ChargingPool, ...)

        /// <summary>
        /// Delete the EVSE data of the given charging pool from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.DeleteStaticData(ChargingPool        ChargingPool,

                                             DateTime?           Timestamp,
                                             CancellationToken?  CancellationToken,
                                             EventTracking_Id    EventTrackingId,
                                             TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException(nameof(ChargingPool), "The given charging pool must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion


        #region SetStaticData   (ChargingPools, ...)

        /// <summary>
        /// Set the EVSE data of the given enumeration of charging pools as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.SetStaticData(IEnumerable<ChargingPool>  ChargingPools,

                                          DateTime?                  Timestamp,
                                          CancellationToken?         CancellationToken,
                                          EventTracking_Id           EventTrackingId,
                                          TimeSpan?                  RequestTimeout)

        {

            #region Initial checks

            if (ChargingPools == null)
                throw new ArgumentNullException(nameof(ChargingPools), "The given enumeration of charging pools must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region AddStaticData   (ChargingPools, ...)

        /// <summary>
        /// Add the EVSE data of the given enumeration of charging pools to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.AddStaticData(IEnumerable<ChargingPool>  ChargingPools,

                                          DateTime?                  Timestamp,
                                          CancellationToken?         CancellationToken,
                                          EventTracking_Id           EventTrackingId,
                                          TimeSpan?                  RequestTimeout)

        {

            #region Initial checks

            if (ChargingPools == null)
                throw new ArgumentNullException(nameof(ChargingPools), "The given enumeration of charging pools must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateStaticData(ChargingPools, ...)

        /// <summary>
        /// Update the EVSE data of the given enumeration of charging pools within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.UpdateStaticData(IEnumerable<ChargingPool>  ChargingPools,

                                             DateTime?                  Timestamp,
                                             CancellationToken?         CancellationToken,
                                             EventTracking_Id           EventTrackingId,
                                             TimeSpan?                  RequestTimeout)

        {

            #region Initial checks

            if (ChargingPools == null)
                throw new ArgumentNullException(nameof(ChargingPools), "The given enumeration of charging pools must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region DeleteStaticData(ChargingPools, ...)

        /// <summary>
        /// Delete the EVSE data of the given enumeration of charging pools from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.DeleteStaticData(IEnumerable<ChargingPool>  ChargingPools,

                                             DateTime?                  Timestamp,
                                             CancellationToken?         CancellationToken,
                                             EventTracking_Id           EventTrackingId,
                                             TimeSpan?                  RequestTimeout)

        {

            #region Initial checks

            if (ChargingPools == null)
                throw new ArgumentNullException(nameof(ChargingPools), "The given enumeration of charging pools must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion


        #region UpdateChargingPoolAdminStatus(AdminStatusUpdates, ...)

        /// <summary>
        /// Update the given enumeration of charging pool admin status updates.
        /// </summary>
        /// <param name="AdminStatusUpdates">An enumeration of charging pool admin status updates.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveStatus.UpdateAdminStatus(IEnumerable<ChargingPoolAdminStatusUpdate>  AdminStatusUpdates,

                                                DateTime?                                   Timestamp,
                                                CancellationToken?                          CancellationToken,
                                                EventTracking_Id                            EventTrackingId,
                                                TimeSpan?                                   RequestTimeout)

        {

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateChargingPoolStatus     (StatusUpdates, ...)

        /// <summary>
        /// Update the given enumeration of charging pool status updates.
        /// </summary>
        /// <param name="StatusUpdates">An enumeration of charging pool status updates.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveStatus.UpdateStatus(IEnumerable<ChargingPoolStatusUpdate>  StatusUpdates,

                                           DateTime?                              Timestamp,
                                           CancellationToken?                     CancellationToken,
                                           EventTracking_Id                       EventTrackingId,
                                           TimeSpan?                              RequestTimeout)

        {

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #endregion

        #region (Set/Add/Update/Delete) Charging station operator(s)...

        #region SetStaticData   (ChargingStationOperator, ...)

        /// <summary>
        /// Set the EVSE data of the given charging station operator as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperator">A charging station operator.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.SetStaticData(ChargingStationOperator  ChargingStationOperator,

                                          DateTime?                Timestamp,
                                          CancellationToken?       CancellationToken,
                                          EventTracking_Id         EventTrackingId,
                                          TimeSpan?                RequestTimeout)

        {

            #region Initial checks

            if (ChargingStationOperator == null)
                throw new ArgumentNullException(nameof(ChargingStationOperator), "The given charging station operator must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region AddStaticData   (ChargingStationOperator, ...)

        /// <summary>
        /// Add the EVSE data of the given charging station operator to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperator">A charging station operator.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.AddStaticData(ChargingStationOperator  ChargingStationOperator,

                                          DateTime?                Timestamp,
                                          CancellationToken?       CancellationToken,
                                          EventTracking_Id         EventTrackingId,
                                          TimeSpan?                RequestTimeout)

        {

            #region Initial checks

            if (ChargingStationOperator == null)
                throw new ArgumentNullException(nameof(ChargingStationOperator), "The given charging station operator must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateStaticData(ChargingStationOperator, ...)

        /// <summary>
        /// Update the EVSE data of the given charging station operator within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperator">A charging station operator.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.UpdateStaticData(ChargingStationOperator  ChargingStationOperator,

                                             DateTime?                Timestamp,
                                             CancellationToken?       CancellationToken,
                                             EventTracking_Id         EventTrackingId,
                                             TimeSpan?                RequestTimeout)

        {

            #region Initial checks

            if (ChargingStationOperator == null)
                throw new ArgumentNullException(nameof(ChargingStationOperator), "The given charging station operator must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region DeleteStaticData(ChargingStationOperator, ...)

        /// <summary>
        /// Delete the EVSE data of the given charging station operator from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperator">A charging station operator.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.DeleteStaticData(ChargingStationOperator  ChargingStationOperator,

                                             DateTime?                Timestamp,
                                             CancellationToken?       CancellationToken,
                                             EventTracking_Id         EventTrackingId,
                                             TimeSpan?                RequestTimeout)

        {

            #region Initial checks

            if (ChargingStationOperator == null)
                throw new ArgumentNullException(nameof(ChargingStationOperator), "The given charging station operator must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion


        #region SetStaticData   (ChargingStationOperators, ...)

        /// <summary>
        /// Set the EVSE data of the given enumeration of charging station operators as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperators">An enumeration of charging station operators.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.SetStaticData(IEnumerable<ChargingStationOperator>  ChargingStationOperators,

                                          DateTime?                             Timestamp,
                                          CancellationToken?                    CancellationToken,
                                          EventTracking_Id                      EventTrackingId,
                                          TimeSpan?                             RequestTimeout)

        {

            #region Initial checks

            if (ChargingStationOperators == null)
                throw new ArgumentNullException(nameof(ChargingStationOperators), "The given enumeration of charging station operators must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region AddStaticData   (ChargingStationOperators, ...)

        /// <summary>
        /// Add the EVSE data of the given enumeration of charging station operators to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperators">An enumeration of charging station operators.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.AddStaticData(IEnumerable<ChargingStationOperator>  ChargingStationOperators,

                                          DateTime?                             Timestamp,
                                          CancellationToken?                    CancellationToken,
                                          EventTracking_Id                      EventTrackingId,
                                          TimeSpan?                             RequestTimeout)

        {

            #region Initial checks

            if (ChargingStationOperators == null)
                throw new ArgumentNullException(nameof(ChargingStationOperators), "The given enumeration of charging station operators must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);


        }

        #endregion

        #region UpdateStaticData(ChargingStationOperators, ...)

        /// <summary>
        /// Update the EVSE data of the given enumeration of charging station operators within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperators">An enumeration of charging station operators.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.UpdateStaticData(IEnumerable<ChargingStationOperator>  ChargingStationOperators,

                                             DateTime?                             Timestamp,
                                             CancellationToken?                    CancellationToken,
                                             EventTracking_Id                      EventTrackingId,
                                             TimeSpan?                             RequestTimeout)

        {

            #region Initial checks

            if (ChargingStationOperators == null)
                throw new ArgumentNullException(nameof(ChargingStationOperators), "The given enumeration of charging station operators must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region DeleteStaticData(ChargingStationOperators, ...)

        /// <summary>
        /// Delete the EVSE data of the given enumeration of charging station operators from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperators">An enumeration of charging station operators.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.DeleteStaticData(IEnumerable<ChargingStationOperator>  ChargingStationOperators,

                                             DateTime?                             Timestamp,
                                             CancellationToken?                    CancellationToken,
                                             EventTracking_Id                      EventTrackingId,
                                             TimeSpan?                             RequestTimeout)

        {

            #region Initial checks

            if (ChargingStationOperators == null)
                throw new ArgumentNullException(nameof(ChargingStationOperators), "The given enumeration of charging station operators must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion


        #region UpdateChargingStationOperatorAdminStatus(AdminStatusUpdates, ...)

        /// <summary>
        /// Update the given enumeration of charging station operator admin status updates.
        /// </summary>
        /// <param name="AdminStatusUpdates">An enumeration of charging station operator admin status updates.</param>
        /// <param name="TransmissionType">Whether to send the charging station operator admin status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveStatus.UpdateAdminStatus(IEnumerable<ChargingStationOperatorAdminStatusUpdate>  AdminStatusUpdates,

                                                DateTime?                                              Timestamp,
                                                CancellationToken?                                     CancellationToken,
                                                EventTracking_Id                                       EventTrackingId,
                                                TimeSpan?                                              RequestTimeout)

        {

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateChargingStationOperatorStatus     (StatusUpdates, ...)

        /// <summary>
        /// Update the given enumeration of charging station operator status updates.
        /// </summary>
        /// <param name="StatusUpdates">An enumeration of charging station operator status updates.</param>
        /// <param name="TransmissionType">Whether to send the charging station operator status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveStatus.UpdateStatus(IEnumerable<ChargingStationOperatorStatusUpdate>  StatusUpdates,

                                           DateTime?                                         Timestamp,
                                           CancellationToken?                                CancellationToken,
                                           EventTracking_Id                                  EventTrackingId,
                                           TimeSpan?                                         RequestTimeout)

        {

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #endregion

        #region (Set/Add/Update/Delete) Roaming network...

        #region SetStaticData   (RoamingNetwork, ...)

        /// <summary>
        /// Set the EVSE data of the given roaming network as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.SetStaticData(RoamingNetwork      RoamingNetwork,

                                          DateTime?           Timestamp,
                                          CancellationToken?  CancellationToken,
                                          EventTracking_Id    EventTrackingId,
                                          TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (RoamingNetwork == null)
                throw new ArgumentNullException(nameof(RoamingNetwork), "The given roaming network must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region AddStaticData   (RoamingNetwork, ...)

        /// <summary>
        /// Add the EVSE data of the given roaming network to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.AddStaticData(RoamingNetwork      RoamingNetwork,

                                          DateTime?           Timestamp,
                                          CancellationToken?  CancellationToken,
                                          EventTracking_Id    EventTrackingId,
                                          TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (RoamingNetwork == null)
                throw new ArgumentNullException(nameof(RoamingNetwork), "The given roaming network must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateStaticData(RoamingNetwork, ...)

        /// <summary>
        /// Update the EVSE data of the given roaming network within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.UpdateStaticData(RoamingNetwork      RoamingNetwork,

                                             DateTime?           Timestamp,
                                             CancellationToken?  CancellationToken,
                                             EventTracking_Id    EventTrackingId,
                                             TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (RoamingNetwork == null)
                throw new ArgumentNullException(nameof(RoamingNetwork), "The given roaming network must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region DeleteStaticData(RoamingNetwork, ...)

        /// <summary>
        /// Delete the EVSE data of the given roaming network from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network to upload.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveData.DeleteStaticData(RoamingNetwork      RoamingNetwork,

                                             DateTime?           Timestamp,
                                             CancellationToken?  CancellationToken,
                                             EventTracking_Id    EventTrackingId,
                                             TimeSpan?           RequestTimeout)

        {

            #region Initial checks

            if (RoamingNetwork == null)
                throw new ArgumentNullException(nameof(RoamingNetwork), "The given roaming network must not be null!");

            #endregion

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion


        #region UpdateRoamingNetworkAdminStatus(AdminStatusUpdates, ...)

        /// <summary>
        /// Update the given enumeration of roaming network admin status updates.
        /// </summary>
        /// <param name="AdminStatusUpdates">An enumeration of roaming network admin status updates.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveStatus.UpdateAdminStatus(IEnumerable<RoamingNetworkAdminStatusUpdate>  AdminStatusUpdates,

                                                DateTime?                                     Timestamp,
                                                CancellationToken?                            CancellationToken,
                                                EventTracking_Id                              EventTrackingId,
                                                TimeSpan?                                     RequestTimeout)

        {

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #region UpdateRoamingNetworkStatus     (StatusUpdates, ...)

        /// <summary>
        /// Update the given enumeration of roaming network status updates.
        /// </summary>
        /// <param name="StatusUpdates">An enumeration of roaming network status updates.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<Acknowledgement>

            IReceiveStatus.UpdateStatus(IEnumerable<RoamingNetworkStatusUpdate>  StatusUpdates,

                                           DateTime?                                Timestamp,
                                           CancellationToken?                       CancellationToken,
                                           EventTracking_Id                         EventTrackingId,
                                           TimeSpan?                                RequestTimeout)

        {

            return new Acknowledgement(ResultType.NoOperation);

        }

        #endregion

        #endregion

        #endregion

        #region Receive AuthorizeStarts/-Stops

        #region AuthorizeStart(AuthToken,                    ChargingProduct = null, SessionId = null, OperatorId = null, ...)

        /// <summary>
        /// Create an AuthorizeStart request.
        /// </summary>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingProduct">An optional charging product.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="OperatorId">An optional charging station operator identification.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<AuthStartResult>

            IReceiveAuthorizeStartStop.AuthorizeStart(Auth_Token                   AuthToken,
                                                      ChargingProduct              ChargingProduct,
                                                      ChargingSession_Id?          SessionId,
                                                      ChargingStationOperator_Id?  OperatorId,

                                                      DateTime?                    Timestamp,
                                                      CancellationToken?           CancellationToken,
                                                      EventTracking_Id             EventTrackingId,
                                                      TimeSpan?                    RequestTimeout)

        {

            #region Initial checks

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given authentication token must not be null!");

            TokenAuthorizationResultType AuthenticationResult;
            AuthStartResult              result;

            #endregion

            #region Send OnAuthorizeStartRequest event

            var StartTime = DateTime.Now;

            try
            {

                if (OnAuthorizeStartRequest != null)
                    await Task.WhenAll(OnAuthorizeStartRequest.GetInvocationList().
                                       Cast<OnAuthorizeStartRequestDelegate>().
                                       Select(e => e(StartTime,
                                                     Timestamp.Value,
                                                     this,
                                                     Id.ToString(),
                                                     EventTrackingId,
                                                     RoamingNetwork.Id,
                                                     OperatorId,
                                                     AuthToken,
                                                     ChargingProduct,
                                                     SessionId,
                                                     RequestTimeout ?? RequestTimeout.Value))).
                                       ConfigureAwait(false);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnAuthorizeEVSEStartRequest));
            }

            #endregion


            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    if (!SessionId.HasValue)
                        SessionId = ChargingSession_Id.New;

                    SessionDatabase.TryAdd(SessionId.Value, new SessionInfo(AuthToken));

                    result = AuthStartResult.Authorized(Id,
                                                                  SessionId,
                                                                  ProviderId: Id);

                }

                #endregion

                #region Token is blocked!

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    result = AuthStartResult.Blocked(Id,
                                                               ProviderId:   Id,
                                                               SessionId:    SessionId,
                                                               Description:  "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    result = AuthStartResult.Unspecified(Id,
                                                                   SessionId);

                #endregion

            }

            #region Unkown Token!

            result = AuthStartResult.NotAuthorized(Id,
                                                             ProviderId:   Id,
                                                             SessionId:    SessionId,
                                                             Description:  "Unkown token!");

            #endregion


            #region Send OnAuthorizeStartRequest event

            var EndTime = DateTime.Now;

            try
            {

                if (OnAuthorizeStartResponse != null)
                    await Task.WhenAll(OnAuthorizeStartResponse.GetInvocationList().
                                       Cast<OnAuthorizeStartResponseDelegate>().
                                       Select(e => e(EndTime,
                                                     Timestamp.Value,
                                                     this,
                                                     Id.ToString(),
                                                     EventTrackingId,
                                                     RoamingNetwork.Id,
                                                     OperatorId,
                                                     AuthToken,
                                                     ChargingProduct,
                                                     SessionId,
                                                     RequestTimeout ?? RequestTimeout.Value,
                                                     result,
                                                     EndTime - StartTime))).
                                       ConfigureAwait(false);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnAuthorizeEVSEStartRequest));
            }

            #endregion

            return result;

        }

        #endregion

        #region AuthorizeStart(AuthToken, EVSEId,            ChargingProduct = null, SessionId = null, OperatorId = null, ...)

        /// <summary>
        /// Create an AuthorizeStart request at the given EVSE.
        /// </summary>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ChargingProduct">An optional charging product.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="OperatorId">An optional charging station operator identification.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<AuthStartEVSEResult>

            IReceiveAuthorizeStartStop.AuthorizeStart(Auth_Token                   AuthToken,
                                                      EVSE_Id                      EVSEId,
                                                      ChargingProduct              ChargingProduct,
                                                      ChargingSession_Id?          SessionId,
                                                      ChargingStationOperator_Id?  OperatorId,

                                                      DateTime?                    Timestamp,
                                                      CancellationToken?           CancellationToken,
                                                      EventTracking_Id             EventTrackingId,
                                                      TimeSpan?                    RequestTimeout)

        {

            #region Initial checks

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given authentication token must not be null!");

            TokenAuthorizationResultType AuthenticationResult;
            AuthStartEVSEResult          result;

            #endregion

            #region Send OnAuthorizeEVSEStartRequest event

            var StartTime = DateTime.Now;

            try
            {

                if (OnAuthorizeEVSEStartRequest != null)
                    await Task.WhenAll(OnAuthorizeEVSEStartRequest.GetInvocationList().
                                       Cast<OnAuthorizeEVSEStartRequestDelegate>().
                                       Select(e => e(StartTime,
                                                     Timestamp.Value,
                                                     this,
                                                     Id.ToString(),
                                                     EventTrackingId,
                                                     RoamingNetwork.Id,
                                                     OperatorId,
                                                     AuthToken,
                                                     EVSEId,
                                                     ChargingProduct,
                                                     SessionId,
                                                     RequestTimeout ?? RequestTimeout.Value))).
                                       ConfigureAwait(false);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnAuthorizeEVSEStartRequest));
            }

            #endregion


            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    if (!SessionId.HasValue)
                        SessionId = ChargingSession_Id.New;

                    SessionDatabase.TryAdd(SessionId.Value, new SessionInfo(AuthToken));

                    result = AuthStartEVSEResult.Authorized(Id,
                                                            SessionId,
                                                            ProviderId: Id);

                }

                #endregion

                #region Token is blocked!

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    result = AuthStartEVSEResult.Blocked(Id,
                                                         ProviderId:   Id,
                                                         SessionId:    SessionId,
                                                         Description:  "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    result = AuthStartEVSEResult.Unspecified(Id,
                                                             SessionId);

                #endregion

            }

            #region Unkown Token!

            result = AuthStartEVSEResult.NotAuthorized(Id,
                                                       ProviderId:   Id,
                                                       SessionId:    SessionId,
                                                       Description:  "Unkown token!");

            #endregion


            #region Send OnAuthorizeEVSEStartResponse event

            var EndTime = DateTime.Now;

            try
            {

                if (OnAuthorizeEVSEStartResponse != null)
                    await Task.WhenAll(OnAuthorizeEVSEStartResponse.GetInvocationList().
                                       Cast<OnAuthorizeEVSEStartResponseDelegate>().
                                       Select(e => e(EndTime,
                                                     Timestamp.Value,
                                                     this,
                                                     Id.ToString(),
                                                     EventTrackingId,
                                                     RoamingNetwork.Id,
                                                     OperatorId,
                                                     AuthToken,
                                                     EVSEId,
                                                     ChargingProduct,
                                                     SessionId,
                                                     RequestTimeout ?? RequestTimeout.Value,
                                                     result,
                                                     EndTime - StartTime))).
                                       ConfigureAwait(false);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnAuthorizeEVSEStartResponse));
            }

            #endregion

            return result;

        }

        #endregion

        #region AuthorizeStart(AuthToken, EVSEId,            ChargingProduct = null, SessionId = null, OperatorId = null, ...)

        /// <summary>
        /// Create an AuthorizeStart request at the given EVSE.
        /// </summary>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ChargingProduct">An optional charging product.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="OperatorId">An optional charging station operator identification.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<AuthStartEVSEResult>

            IReceiveAuthorizeStartStop.AuthorizeStart(eMAIdWithPIN2                AuthToken,
                                                      EVSE_Id                      EVSEId,
                                                      ChargingProduct              ChargingProduct,
                                                      ChargingSession_Id?          SessionId,
                                                      ChargingStationOperator_Id?  OperatorId,

                                                      DateTime?                    Timestamp,
                                                      CancellationToken?           CancellationToken,
                                                      EventTracking_Id             EventTrackingId,
                                                      TimeSpan?                    RequestTimeout)

        {

            #region Initial checks

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given authentication token must not be null!");

            TokenAuthorizationResultType AuthenticationResult;
            AuthStartEVSEResult          result;

            #endregion

            #region Send OnAuthorizeEVSEStartRequest event

            //var StartTime = DateTime.Now;

            //try
            //{

            //    if (OnAuthorizeEVSEStartRequest != null)
            //        await Task.WhenAll(OnAuthorizeEVSEStartRequest.GetInvocationList().
            //                           Cast<OnAuthorizeEVSEStartRequestDelegate>().
            //                           Select(e => e(StartTime,
            //                                         Timestamp.Value,
            //                                         this,
            //                                         Id.ToString(),
            //                                         EventTrackingId,
            //                                         RoamingNetwork.Id,
            //                                         OperatorId,
            //                                         AuthToken,
            //                                         EVSEId,
            //                                         ChargingProduct,
            //                                         SessionId,
            //                                         RequestTimeout ?? RequestTimeout.Value))).
            //                           ConfigureAwait(false);

            //}
            //catch (Exception e)
            //{
            //    e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnAuthorizeEVSEStartRequest));
            //}

            #endregion


            //if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            //{

            //    #region Authorized

            //    if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
            //    {

            //        if (!SessionId.HasValue)
            //            SessionId = ChargingSession_Id.New;

            //        SessionDatabase.TryAdd(SessionId.Value, new SessionInfo(AuthToken));

            //        result = AuthStartEVSEResult.Authorized(Id,
            //                                                SessionId,
            //                                                ProviderId: Id);

            //    }

            //    #endregion

            //    #region Token is blocked!

            //    else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
            //        result = AuthStartEVSEResult.Blocked(Id,
            //                                             ProviderId:   Id,
            //                                             SessionId:    SessionId,
            //                                             Description:  "Token is blocked!");

            //    #endregion

            //    #region ...fall through!

            //    else
            //        result = AuthStartEVSEResult.Unspecified(Id,
            //                                                 SessionId);

            //    #endregion

            //}

            #region Unkown Token!

            result = AuthStartEVSEResult.NotAuthorized(Id,
                                                       ProviderId:   Id,
                                                       SessionId:    SessionId,
                                                       Description:  "Unkown token!");

            #endregion


            #region Send OnAuthorizeEVSEStartResponse event

            //var EndTime = DateTime.Now;

            //try
            //{

            //    if (OnAuthorizeEVSEStartResponse != null)
            //        await Task.WhenAll(OnAuthorizeEVSEStartResponse.GetInvocationList().
            //                           Cast<OnAuthorizeEVSEStartResponseDelegate>().
            //                           Select(e => e(EndTime,
            //                                         Timestamp.Value,
            //                                         this,
            //                                         Id.ToString(),
            //                                         EventTrackingId,
            //                                         RoamingNetwork.Id,
            //                                         OperatorId,
            //                                         AuthToken,
            //                                         EVSEId,
            //                                         ChargingProduct,
            //                                         SessionId,
            //                                         RequestTimeout ?? RequestTimeout.Value,
            //                                         result,
            //                                         EndTime - StartTime))).
            //                           ConfigureAwait(false);

            //}
            //catch (Exception e)
            //{
            //    e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnAuthorizeEVSEStartResponse));
            //}

            #endregion

            return result;

        }

        #endregion

        #region AuthorizeStart(AuthToken, ChargingStationId, ChargingProduct = null, SessionId = null, OperatorId = null, ...)

        /// <summary>
        /// Create an AuthorizeStart request at the given charging station.
        /// </summary>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingStationId">The unique identification of a charging station.</param>
        /// <param name="ChargingProduct">An optional charging product.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="OperatorId">An optional charging station operator identification.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<AuthStartChargingStationResult>

            IReceiveAuthorizeStartStop.AuthorizeStart(Auth_Token                   AuthToken,
                                                      ChargingStation_Id           ChargingStationId,
                                                      ChargingProduct              ChargingProduct,
                                                      ChargingSession_Id?          SessionId,
                                                      ChargingStationOperator_Id?  OperatorId,

                                                      DateTime?                    Timestamp,
                                                      CancellationToken?           CancellationToken,
                                                      EventTracking_Id             EventTrackingId,
                                                      TimeSpan?                    RequestTimeout)

        {

            #region Initial checks

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given authentication token must not be null!");

            TokenAuthorizationResultType AuthenticationResult;
            AuthStartChargingStationResult          result;

            #endregion

            #region Send OnAuthorizeChargingStationStartRequest event

            var StartTime = DateTime.Now;

            try
            {

                if (OnAuthorizeChargingStationStartRequest != null)
                    await Task.WhenAll(OnAuthorizeChargingStationStartRequest.GetInvocationList().
                                       Cast<OnAuthorizeChargingStationStartRequestDelegate>().
                                       Select(e => e(StartTime,
                                                     Timestamp.Value,
                                                     this,
                                                     Id.ToString(),
                                                     EventTrackingId,
                                                     RoamingNetwork.Id,
                                                     OperatorId,
                                                     AuthToken,
                                                     ChargingStationId,
                                                     ChargingProduct,
                                                     SessionId,
                                                     RequestTimeout ?? RequestTimeout.Value))).
                                       ConfigureAwait(false);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnAuthorizeChargingStationStartRequest));
            }

            #endregion


            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    if (!SessionId.HasValue)
                        SessionId = ChargingSession_Id.New;

                    SessionDatabase.TryAdd(SessionId.Value, new SessionInfo(AuthToken));

                    result = AuthStartChargingStationResult.Authorized(Id,
                                                            SessionId,
                                                            ProviderId: Id);

                }

                #endregion

                #region Token is blocked!

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    result = AuthStartChargingStationResult.Blocked(Id,
                                                         ProviderId:   Id,
                                                         SessionId:    SessionId,
                                                         Description:  "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    result = AuthStartChargingStationResult.Unspecified(Id,
                                                             SessionId);

                #endregion

            }

            #region Unkown Token!

            result = AuthStartChargingStationResult.NotAuthorized(Id,
                                                       ProviderId:   Id,
                                                       SessionId:    SessionId,
                                                       Description:  "Unkown token!");

            #endregion


            #region Send OnAuthorizeChargingStationStartResponse event

            var EndTime = DateTime.Now;

            try
            {

                if (OnAuthorizeChargingStationStartResponse != null)
                    await Task.WhenAll(OnAuthorizeChargingStationStartResponse.GetInvocationList().
                                       Cast<OnAuthorizeChargingStationStartResponseDelegate>().
                                       Select(e => e(EndTime,
                                                     Timestamp.Value,
                                                     this,
                                                     Id.ToString(),
                                                     EventTrackingId,
                                                     RoamingNetwork.Id,
                                                     OperatorId,
                                                     AuthToken,
                                                     ChargingStationId,
                                                     ChargingProduct,
                                                     SessionId,
                                                     RequestTimeout ?? RequestTimeout.Value,
                                                     result,
                                                     EndTime - StartTime))).
                                       ConfigureAwait(false);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnAuthorizeChargingStationStartResponse));
            }

            #endregion

            return result;

        }

        #endregion

        #region AuthorizeStart(AuthToken, ChargingPoolId,    ChargingProduct = null, SessionId = null, OperatorId = null, ...)

        /// <summary>
        /// Create an AuthorizeStart request at the given charging pool.
        /// </summary>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingPoolId">The unique identification of a charging pool.</param>
        /// <param name="ChargingProduct">An optional charging product.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="OperatorId">An optional charging station operator identification.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<AuthStartChargingPoolResult>

            IReceiveAuthorizeStartStop.AuthorizeStart(Auth_Token                   AuthToken,
                                                      ChargingPool_Id              ChargingPoolId,
                                                      ChargingProduct              ChargingProduct,
                                                      ChargingSession_Id?          SessionId,
                                                      ChargingStationOperator_Id?  OperatorId,

                                                      DateTime?                    Timestamp,
                                                      CancellationToken?           CancellationToken,
                                                      EventTracking_Id             EventTrackingId,
                                                      TimeSpan?                    RequestTimeout)

        {

            #region Initial checks

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given authentication token must not be null!");

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    if (!SessionId.HasValue)
                        SessionId = ChargingSession_Id.New;

                    SessionDatabase.TryAdd(SessionId.Value, new SessionInfo(AuthToken));

                    return AuthStartChargingPoolResult.Authorized(Id,
                                                                  SessionId,
                                                                  ProviderId: Id);

                }

                #endregion

                #region Token is blocked!

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStartChargingPoolResult.Blocked(Id,
                                                               ProviderId:   Id,
                                                               SessionId:    SessionId,
                                                               Description:  "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStartChargingPoolResult.Unspecified(Id,
                                                                   SessionId);

                #endregion

            }

            #region Unkown Token!

            return AuthStartChargingPoolResult.NotAuthorized(Id,
                                                             ProviderId:   Id,
                                                             SessionId:    SessionId,
                                                             Description:  "Unkown token!");

            #endregion

        }

        #endregion


        // UID => Not everybody can stop any session, but maybe another
        //        UID than the UID which started the session!
        //        (e.g. car sharing)

        #region AuthorizeStop (SessionId, AuthToken,                    OperatorId = null, ...)

        /// <summary>
        /// Create an authorize stop request at the given EVSE.
        /// </summary>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="OperatorId">An optional charging station operator identification.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<AuthStopResult>

            IReceiveAuthorizeStartStop.AuthorizeStop(ChargingSession_Id           SessionId,
                                                     Auth_Token                   AuthToken,
                                                     ChargingStationOperator_Id?  OperatorId,

                                                     DateTime?                    Timestamp,
                                                     CancellationToken?           CancellationToken,
                                                     EventTracking_Id             EventTrackingId,
                                                     TimeSpan?                    RequestTimeout)
        {

            #region Initial checks

            if (SessionId  == null)
                throw new ArgumentNullException(nameof(SessionId),  "The given charging session identification must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given authentication token must not be null!");

            #endregion

            #region Check session identification

            SessionInfo SessionInfo = null;

            if (!SessionDatabase.TryGetValue(SessionId, out SessionInfo))
                return AuthStopResult.InvalidSessionId(Id,
                                                       SessionId);

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Token is authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    // Authorized
                    if (SessionInfo.ListOfAuthStopTokens.Contains(AuthToken))
                        return AuthStopResult.Authorized(Id,
                                                         SessionId:   SessionId,
                                                         ProviderId:  Id);

                    // Invalid Token for SessionId!
                    else
                        return AuthStopResult.NotAuthorized(Id,
                                                            SessionId:    SessionId,
                                                            ProviderId:   Id,
                                                            Description:  "Invalid token for given session identification!");

                }

                #endregion

                #region Token is blocked

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStopResult.Blocked(Id,
                                                  SessionId:    SessionId,
                                                  ProviderId:   Id,
                                                  Description:  "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStopResult.Unspecified(Id,
                                                      SessionId);

                #endregion

            }

            #region Unkown Token!

            return AuthStopResult.NotAuthorized(Id,
                                                SessionId:    SessionId,
                                                ProviderId:   Id,
                                                Description:  "Unkown token!");

            #endregion

        }

        #endregion

        #region AuthorizeStop (SessionId, AuthToken, EVSEId,            OperatorId = null, ...)

        /// <summary>
        /// Create an authorize stop request at the given EVSE.
        /// </summary>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">An EVSE identification.</param>
        /// <param name="OperatorId">An optional charging station operator identification.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<AuthStopEVSEResult>

            IReceiveAuthorizeStartStop.AuthorizeStop(ChargingSession_Id           SessionId,
                                                     Auth_Token                   AuthToken,
                                                     EVSE_Id                      EVSEId,
                                                     ChargingStationOperator_Id?  OperatorId,

                                                     DateTime?                    Timestamp,
                                                     CancellationToken?           CancellationToken,
                                                     EventTracking_Id             EventTrackingId,
                                                     TimeSpan?                    RequestTimeout)

        {

            #region Initial checks

            if (SessionId  == null)
                throw new ArgumentNullException(nameof(SessionId),  "The given charging session identification must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given authentication token must not be null!");

            #endregion

            #region Check session identification

            SessionInfo SessionInfo = null;

            if (!SessionDatabase.TryGetValue(SessionId, out SessionInfo))
                return AuthStopEVSEResult.InvalidSessionId(Id,
                                                           SessionId);

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Token is authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    // Authorized
                    if (SessionInfo.ListOfAuthStopTokens.Contains(AuthToken))
                        return AuthStopEVSEResult.Authorized(Id,
                                                             SessionId:   SessionId,
                                                             ProviderId:  Id);

                    // Invalid Token for SessionId!
                    else
                        return AuthStopEVSEResult.NotAuthorized(Id,
                                                                SessionId:    SessionId,
                                                                ProviderId:   Id,
                                                                Description:  "Invalid token for given session identification!");

                }

                #endregion

                #region Token is blocked

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStopEVSEResult.Blocked(Id,
                                                      SessionId:    SessionId,
                                                      ProviderId:   Id,
                                                      Description:  "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStopEVSEResult.Unspecified(Id,
                                                          SessionId);

                #endregion

            }

            #region Unkown Token!

            return AuthStopEVSEResult.NotAuthorized(Id,
                                                    SessionId:    SessionId,
                                                    ProviderId:   Id,
                                                    Description:  "Unkown token!");

            #endregion

        }

        #endregion

        #region AuthorizeStop (SessionId, AuthToken, ChargingStationId, OperatorId = null, ...)

        /// <summary>
        /// Create an authorize stop request at the given EVSE.
        /// </summary>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingStationId">An charging station identification.</param>
        /// <param name="OperatorId">An optional charging station operator identification.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<AuthStopChargingStationResult>

            IReceiveAuthorizeStartStop.AuthorizeStop(ChargingSession_Id           SessionId,
                                                     Auth_Token                   AuthToken,
                                                     ChargingStation_Id           ChargingStationId,
                                                     ChargingStationOperator_Id?  OperatorId,

                                                     DateTime?                    Timestamp,
                                                     CancellationToken?           CancellationToken,
                                                     EventTracking_Id             EventTrackingId,
                                                     TimeSpan?                    RequestTimeout)

        {

            #region Initial checks

            if (SessionId  == null)
                throw new ArgumentNullException(nameof(SessionId),  "The given charging session identification must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given authentication token must not be null!");

            #endregion

            #region Check session identification

            SessionInfo SessionInfo = null;

            if (!SessionDatabase.TryGetValue(SessionId, out SessionInfo))
                return AuthStopChargingStationResult.InvalidSessionId(Id,
                                                                      SessionId);

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Token is authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    // Authorized
                    if (SessionInfo.ListOfAuthStopTokens.Contains(AuthToken))
                        return AuthStopChargingStationResult.Authorized(Id,
                                                                        SessionId:   SessionId,
                                                                        ProviderId:  Id);

                    // Invalid Token for SessionId!
                    else
                        return AuthStopChargingStationResult.NotAuthorized(Id,
                                                                           SessionId:    SessionId,
                                                                           ProviderId:   Id,
                                                                           Description:  "Invalid token for given session identification!");

                }

                #endregion

                #region Token is blocked

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStopChargingStationResult.Blocked(Id,
                                                                 SessionId:    SessionId,
                                                                 ProviderId:   Id,
                                                                 Description:  "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStopChargingStationResult.Unspecified(Id,
                                                                     SessionId);

                #endregion

            }

            #region Unkown Token!

            return AuthStopChargingStationResult.NotAuthorized(Id,
                                                               SessionId:    SessionId,
                                                               ProviderId:   Id,
                                                               Description:  "Unkown token!");

            #endregion

        }

        #endregion

        #region AuthorizeStop (SessionId, AuthToken, ChargingPoolId,    OperatorId = null, ...)

        /// <summary>
        /// Create an authorize stop request at the given EVSE.
        /// </summary>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingPoolId">An charging station identification.</param>
        /// <param name="OperatorId">An optional charging station operator identification.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<AuthStopChargingPoolResult>

            IReceiveAuthorizeStartStop.AuthorizeStop(ChargingSession_Id           SessionId,
                                                     Auth_Token                   AuthToken,
                                                     ChargingPool_Id              ChargingPoolId,
                                                     ChargingStationOperator_Id?  OperatorId,

                                                     DateTime?                    Timestamp,
                                                     CancellationToken?           CancellationToken,
                                                     EventTracking_Id             EventTrackingId,
                                                     TimeSpan?                    RequestTimeout)

        {

            #region Initial checks

            if (SessionId  == null)
                throw new ArgumentNullException(nameof(SessionId),  "The given charging session identification must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException(nameof(AuthToken),  "The given authentication token must not be null!");

            #endregion

            #region Check session identification

            SessionInfo SessionInfo = null;

            if (!SessionDatabase.TryGetValue(SessionId, out SessionInfo))
                return AuthStopChargingPoolResult.InvalidSessionId(Id,
                                                                   SessionId);

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Token is authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    // Authorized
                    if (SessionInfo.ListOfAuthStopTokens.Contains(AuthToken))
                        return AuthStopChargingPoolResult.Authorized(Id,
                                                                     SessionId:   SessionId,
                                                                     ProviderId:  Id);

                    // Invalid Token for SessionId!
                    else
                        return AuthStopChargingPoolResult.NotAuthorized(Id,
                                                                        SessionId:    SessionId,
                                                                        ProviderId:   Id,
                                                                        Description:  "Invalid token for given session identification!");

                }

                #endregion

                #region Token is blocked

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStopChargingPoolResult.Blocked(Id,
                                                              SessionId:    SessionId,
                                                              ProviderId:   Id,
                                                              Description:  "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStopChargingPoolResult.Unspecified(Id,
                                                                  SessionId);

                #endregion

            }

            #region Unkown Token!

            return AuthStopChargingPoolResult.NotAuthorized(Id,
                                                            SessionId:    SessionId,
                                                            ProviderId:   Id,
                                                            Description:  "Unkown token!");

            #endregion

        }

        #endregion

        #endregion

        #region SendChargeDetailRecord(ChargeDetailRecords, ...)

        /// <summary>
        /// Send a charge detail record.
        /// </summary>
        /// <param name="ChargeDetailRecords">An enumeration of charge detail records.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        async Task<SendCDRsResult>

            IRemoteSendChargeDetailRecords.SendChargeDetailRecords(IEnumerable<ChargeDetailRecord>  ChargeDetailRecords,

                                                                   DateTime?                        Timestamp,
                                                                   CancellationToken?               CancellationToken,
                                                                   EventTracking_Id                 EventTrackingId,
                                                                   TimeSpan?                        RequestTimeout)
        {

            #region Initial checks

            if (ChargeDetailRecords == null)
                throw new ArgumentNullException(nameof(ChargeDetailRecords),  "The given charge detail records must not be null!");

            #endregion

            SessionInfo _SessionInfo = null;


            //Debug.WriteLine("Received a CDR: " + ChargeDetailRecord.SessionId.ToString());


            //if (ChargeDetailRecordDatabase.ContainsKey(ChargeDetailRecord.SessionId))
            //    return SendCDRResult.InvalidSessionId(AuthorizatorId);


            //if (ChargeDetailRecordDatabase.TryAdd(ChargeDetailRecord.SessionId, ChargeDetailRecord))
            //{

            //    SessionDatabase.TryRemove(ChargeDetailRecord.SessionId, out _SessionInfo);

            //    return SendCDRResult.Forwarded(AuthorizatorId);

            //}




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

            return SendCDRsResult.InvalidSessionId(Id);

        }

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
        /// <param name="ChargingProduct">The charging product to be reserved.</param>
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
                    ChargingProduct                   ChargingProduct     = null,
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

            #region Send OnReserveEVSERequest event

            var Runtime = Stopwatch.StartNew();

            try
            {

                OnReserveEVSERequest?.Invoke(DateTime.Now,
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
                                             ChargingProduct,
                                             AuthTokens,
                                             eMAIds,
                                             PINs,
                                             RequestTimeout);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnReserveEVSERequest));
            }

            #endregion


            var response = await RoamingNetwork.
                                     Reserve(EVSEId,
                                             StartTime,
                                             Duration,
                                             ReservationId,
                                             Id,
                                             eMAId,
                                             ChargingProduct,
                                             AuthTokens,
                                             eMAIds,
                                             PINs,

                                             Timestamp,
                                             CancellationToken,
                                             EventTrackingId,
                                             RequestTimeout);


            #region Send OnReservedEVSEResponse event

            Runtime.Stop();

            try
            {

                OnReservedEVSEResponse?.Invoke(DateTime.Now,
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
                                               ChargingProduct,
                                               AuthTokens,
                                               eMAIds,
                                               PINs,
                                               response,
                                               Runtime.Elapsed,
                                               RequestTimeout);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnReservedEVSEResponse));
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


        #region RemoteStart(EVSEId, ChargingProduct = null, ReservationId = null, SessionId = null, eMAId = null, ...)

        /// <summary>
        /// Start a charging session at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of the EVSE to be started.</param>
        /// <param name="ChargingProduct">The choosen charging product.</param>
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
                        ChargingProduct          ChargingProduct     = null,
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

            #region Send OnRemoteEVSEStartRequest event

            var StartTime = DateTime.Now;

            try
            {

                OnRemoteEVSEStartRequest?.Invoke(StartTime,
                                                 Timestamp.Value,
                                                 this,
                                                 EventTrackingId,
                                                 RoamingNetwork.Id,
                                                 EVSEId,
                                                 ChargingProduct,
                                                 ReservationId,
                                                 SessionId,
                                                 Id,
                                                 eMAId,
                                                 RequestTimeout);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnRemoteEVSEStartRequest));
            }

            #endregion


            var response = await RoamingNetwork.
                                     RemoteStart(EVSEId,
                                                 ChargingProduct,
                                                 ReservationId,
                                                 SessionId,
                                                 Id,
                                                 eMAId,

                                                 Timestamp,
                                                 CancellationToken,
                                                 EventTrackingId,
                                                 RequestTimeout);


            #region Send OnRemoteEVSEStartResponse event

            var EndTime = DateTime.Now;

            try
            {

                OnRemoteEVSEStartResponse?.Invoke(EndTime,
                                                  Timestamp.Value,
                                                  this,
                                                  EventTrackingId,
                                                  RoamingNetwork.Id,
                                                  EVSEId,
                                                  ChargingProduct,
                                                  ReservationId,
                                                  SessionId,
                                                  Id,
                                                  eMAId,
                                                  RequestTimeout,
                                                  response,
                                                  EndTime - StartTime);

            }
            catch (Exception e)
            {
                e.Log(nameof(eMobilityServiceProvider) + "." + nameof(OnRemoteEVSEStartResponse));
            }

            #endregion

            return response;

        }

        #endregion

        #region RemoteStop (EVSEId, SessionId, ReservationHandling, eMAId = null, ...)

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

            var StartTime = DateTime.Now;

            try
            {

                OnRemoteEVSEStop?.Invoke(StartTime,
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

            var EndTime = DateTime.Now;

            try
            {

                OnRemoteEVSEStopped?.Invoke(EndTime,
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
                                            EndTime - StartTime);

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
