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
using System.Threading.Tasks;
using System.Collections.Generic;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// A demo implementation of a virtual WWCP charging station.
    /// </summary>
    public class VirtualChargingStation : IRemoteChargingStation
    {

        #region Data

        /// <summary>
        /// The default max size of the status history.
        /// </summary>
        public const UInt16 DefaultMaxStatusListSize        = 50;

        /// <summary>
        /// The default max size of the admin status history.
        /// </summary>
        public const UInt16 DefaultMaxAdminStatusListSize   = 50;

        /// <summary>
        /// The maximum time span for a reservation.
        /// </summary>
        public static readonly TimeSpan MaxReservationDuration = TimeSpan.FromMinutes(15);

        #endregion

        #region Properties

        #region Id

        private ChargingStation_Id _Id;

        /// <summary>
        /// The unique identification of this virtual charging station.
        /// </summary>
        public ChargingStation_Id Id
        {
            get
            {
                return _Id;
            }
        }

        #endregion

        #region Description

        internal I18NString _Description;

        /// <summary>
        /// An optional (multi-language) description of this charging station.
        /// </summary>
        [Optional]
        public I18NString Description
        {

            get
            {

                return _Description;

            }

            set
            {

                if (value == _Description)
                    return;

                _Description = value;

            }

        }

        #endregion

        #region Status

        private ChargingStationStatusType _Status;

        public ChargingStationStatusType Status
        {
            get
            {
                return _Status;
            }
        }

        #endregion

        #endregion

        #region Links

        #region ChargingPool

        private readonly VirtualChargingPool _ChargingPool;

        public VirtualChargingPool ChargingPool
        {
            get
            {
                return _ChargingPool;
            }
        }

        #endregion

        #endregion

        #region Events



        #endregion

        #region Constructor(s)

        #region VirtualChargingStation(ChargingStation, MaxStatusListSize = DefaultMaxStatusListSize, MaxAdminStatusListSize = DefaultMaxAdminStatusListSize)

        /// <summary>
        /// Create a virtual charging station.
        /// </summary>
        /// <param name="ChargingStation">A local charging station.</param>
        /// <param name="MaxStatusListSize">The maximum size of the charging station status list.</param>
        /// <param name="MaxAdminStatusListSize">The maximum size of the charging station admin status list.</param>
        public VirtualChargingStation(ChargingStation  ChargingStation,
                                      UInt16           MaxStatusListSize       = DefaultMaxStatusListSize,
                                      UInt16           MaxAdminStatusListSize  = DefaultMaxAdminStatusListSize)
        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation),  "The given charging station must not be null!");

            #endregion

            this._Id            = ChargingStation.Id;
            this._ChargingPool  = ChargingPool;
            this._Status        = ChargingStationStatusType.Available;
            this._EVSEs         = new HashSet<VirtualEVSE>();

        }

        #endregion

        #region VirtualChargingStation(ChargingStation, ChargingPool, MaxStatusListSize = DefaultMaxStatusListSize, MaxAdminStatusListSize = DefaultMaxAdminStatusListSize)

        /// <summary>
        /// Create a virtual charging station.
        /// </summary>
        /// <param name="ChargingStation">A local charging station.</param>
        /// <param name="ChargingPool">The parent charging pool.</param>
        /// <param name="MaxStatusListSize">The maximum size of the charging station status list.</param>
        /// <param name="MaxAdminStatusListSize">The maximum size of the charging station admin status list.</param>
        public VirtualChargingStation(ChargingStation      ChargingStation,
                                      VirtualChargingPool  ChargingPool,
                                      UInt16               MaxStatusListSize       = DefaultMaxStatusListSize,
                                      UInt16               MaxAdminStatusListSize  = DefaultMaxAdminStatusListSize)
        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation),  "The given charging station must not be null!");

            if (ChargingPool    == null)
                throw new ArgumentNullException(nameof(ChargingStation),  "The given charging pool must not be null!");

            #endregion

            this._Id            = ChargingStation.Id;
            this._ChargingPool  = ChargingPool;
            this._Status        = ChargingStationStatusType.Available;
            this._EVSEs         = new HashSet<VirtualEVSE>();

        }

        #endregion

        #endregion


        #region EVSEs...

        #region EVSEs

        private readonly HashSet<VirtualEVSE> _EVSEs;

        /// <summary>
        /// All registered EVSEs.
        /// </summary>
        public IEnumerable<VirtualEVSE> EVSEs
        {
            get
            {
                return _EVSEs;
            }
        }

        #endregion

        #region CreateNewEVSE(EVSEId, Configurator = null, OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new EVSE having the given
        /// unique EVSE identification.
        /// </summary>
        /// <param name="EVSEId">The unique identification of the new EVSE.</param>
        /// <param name="Configurator">An optional delegate to configure the new EVSE after its creation.</param>
        /// <param name="OnSuccess">An optional delegate called after successful creation of the EVSE.</param>
        /// <param name="OnError">An optional delegate for signaling errors.</param>
        public VirtualEVSE CreateNewEVSE(EVSE_Id                                  EVSEId,
                                         Action<VirtualEVSE>                      Configurator  = null,
                                         Action<VirtualEVSE>                      OnSuccess     = null,
                                         Action<VirtualChargingStation, EVSE_Id>  OnError       = null)
        {

            #region Initial checks

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId), "The given EVSE identification must not be null!");

            if (_EVSEs.Any(evse => evse.Id == EVSEId))
            {
                if (OnError == null)
                    throw new EVSEAlreadyExistsInStation(EVSEId, this.Id);
                else
                    OnError.FailSafeInvoke(this, EVSEId);
            }

            #endregion

            var Now           = DateTime.Now;
            var _VirtualEVSE  = new VirtualEVSE(EVSEId, this);

            Configurator.FailSafeInvoke(_VirtualEVSE);

            if (_EVSEs.Add(_VirtualEVSE))
            {

                //_VirtualEVSE.OnPropertyChanged        += (Timestamp, Sender, PropertyName, OldValue, NewValue)
                //                                           => UpdateEVSEData(Timestamp, Sender as VirtualEVSE, PropertyName, OldValue, NewValue);
                //
                //_VirtualEVSE.OnStatusChanged          += UpdateEVSEStatus;
                //_VirtualEVSE.OnAdminStatusChanged     += UpdateEVSEAdminStatus;
                //_VirtualEVSE.OnNewReservation         += SendNewReservation;
                //_VirtualEVSE.OnNewChargingSession     += SendNewChargingSession;
                //_VirtualEVSE.OnNewChargeDetailRecord  += SendNewChargeDetailRecord;

                OnSuccess.FailSafeInvoke(_VirtualEVSE);

                return _VirtualEVSE;

            }

            return null;

        }

        #endregion


        #region OnRemoteEVSEDataChanged

        /// <summary>
        /// An event fired whenever the static data of any subordinated EVSE changed.
        /// </summary>
        public event OnRemoteEVSEDataChangedDelegate OnRemoteEVSEDataChanged;

        #endregion

        #region OnRemoteEVSE(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the dynamic status of any subordinated EVSE changed.
        /// </summary>
        public event OnRemoteEVSEStatusChangedDelegate       OnRemoteEVSEStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status of any subordinated EVSE changed.
        /// </summary>
        public event OnRemoteEVSEAdminStatusChangedDelegate  OnRemoteEVSEAdminStatusChanged;

        #endregion

        #region (internal) UpdateEVSEData(Timestamp, RemoteEVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update the data of a remote EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="RemoteEVSE">The remote EVSE.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        internal void UpdateEVSEData(DateTime     Timestamp,
                                     IRemoteEVSE  RemoteEVSE,
                                     String       PropertyName,
                                     Object       OldValue,
                                     Object       NewValue)
        {

            var OnRemoteEVSEDataChangedLocal = OnRemoteEVSEDataChanged;
            if (OnRemoteEVSEDataChangedLocal != null)
                OnRemoteEVSEDataChangedLocal(Timestamp, RemoteEVSE, PropertyName, OldValue, NewValue);

        }

        #endregion

        #region (internal) UpdateEVSEStatus(Timestamp, RemoteEVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update the remote EVSE station status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="RemoteEVSE">The updated EVSE.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal void UpdateEVSEStatus(DateTime                     Timestamp,
                                       IRemoteEVSE                  RemoteEVSE,
                                       Timestamped<EVSEStatusType>  OldStatus,
                                       Timestamped<EVSEStatusType>  NewStatus)
        {

            var OnRemoteEVSEStatusChangedLocal = OnRemoteEVSEStatusChanged;
            if (OnRemoteEVSEStatusChangedLocal != null)
                OnRemoteEVSEStatusChangedLocal(Timestamp, RemoteEVSE, OldStatus, NewStatus);

        }

        #endregion

        #region (internal) UpdateEVSEAdminStatus(Timestamp, RemoteEVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update the current charging station status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="RemoteEVSE">The updated remote EVSE.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal void UpdateEVSEAdminStatus(DateTime                          Timestamp,
                                            IRemoteEVSE                       RemoteEVSE,
                                            Timestamped<EVSEAdminStatusType>  OldStatus,
                                            Timestamped<EVSEAdminStatusType>  NewStatus)
        {

            var OnRemoteEVSEAdminStatusChangedLocal = OnRemoteEVSEAdminStatusChanged;
            if (OnRemoteEVSEAdminStatusChangedLocal != null)
                OnRemoteEVSEAdminStatusChangedLocal(Timestamp, RemoteEVSE, OldStatus, NewStatus);

        }

        #endregion


        IEnumerable<EVSE> IRemoteChargingStation.EVSEs
        {
            get
            {
                return null;
            }
        }

        IRemoteEVSE IRemoteChargingStation.CreateNewEVSE(EVSE_Id EVSEId, Action<EVSE> Configurator = null, Action<EVSE> OnSuccess = null, Action<ChargingStation, EVSE_Id> OnError = null)
        {
            return this.CreateNewEVSE(EVSEId);
        }


        // Socket events

        #region SocketOutletAddition

        internal readonly IVotingNotificator<DateTime, VirtualEVSE, SocketOutlet, Boolean> SocketOutletAddition;

        /// <summary>
        /// Called whenever a socket outlet will be or was added.
        /// </summary>
        public IVotingSender<DateTime, VirtualEVSE, SocketOutlet, Boolean> OnSocketOutletAddition
        {
            get
            {
                return SocketOutletAddition;
            }
        }

        #endregion

        #region SocketOutletRemoval

        internal readonly IVotingNotificator<DateTime, VirtualEVSE, SocketOutlet, Boolean> SocketOutletRemoval;

        /// <summary>
        /// Called whenever a socket outlet will be or was removed.
        /// </summary>
        public IVotingSender<DateTime, VirtualEVSE, SocketOutlet, Boolean> OnSocketOutletRemoval
        {
            get
            {
                return SocketOutletRemoval;
            }
        }

        #endregion


        #region GetEVSEStatus(...)

        public async Task<IEnumerable<EVSEStatus>> GetEVSEStatus(DateTime                 Timestamp,
                                                                 CancellationToken        CancellationToken,
                                                                 EventTracking_Id         EventTrackingId,
                                                                 TimeSpan?                QueryTimeout = null)
        {

            return _EVSEs.Select(evse => new EVSEStatus(evse.Id, evse.Status.Value));

        }

        #endregion

        #endregion

        #region Reservations...

        #region ChargingReservations

        /// <summary>
        /// All current charging reservations.
        /// </summary>
        public IEnumerable<ChargingReservation> ChargingReservations
        {
            get
            {

                return _EVSEs.
                           Select(evse        => evse.Reservation).
                           Where (reservation => reservation != null);

            }
        }

        #endregion

        #region OnNewReservation

        /// <summary>
        /// An event fired whenever a new charging reservation was created.
        /// </summary>
        public event OnNewReservationDelegate OnNewReservation;

        #endregion

        #region Reserve(...EVSEId, StartTime, Duration, ReservationId = null, ProviderId = null, ...)

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
        /// <param name="ProviderId">An optional unique identification of e-Mobility service provider.</param>
        /// <param name="ChargingProductId">An optional unique identification of the charging product to be reserved.</param>
        /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
        /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
        /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<ReservationResult> Reserve(DateTime                 Timestamp,
                                                     CancellationToken        CancellationToken,
                                                     EventTracking_Id         EventTrackingId,
                                                     EVSE_Id                  EVSEId,
                                                     DateTime?                StartTime,
                                                     TimeSpan?                Duration,
                                                     ChargingReservation_Id   ReservationId      = null,
                                                     EVSP_Id                  ProviderId         = null,
                                                     ChargingProduct_Id       ChargingProductId  = null,
                                                     IEnumerable<Auth_Token>  AuthTokens         = null,
                                                     IEnumerable<eMA_Id>      eMAIds             = null,
                                                     IEnumerable<UInt32>      PINs               = null,
                                                     TimeSpan?                QueryTimeout       = null)
        {

            #region Initial checks

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId),  "The given EVSE identification must not be null!");

            ReservationResult result = null;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;

            #endregion


            var _VirtualEVSE = _EVSEs.Where(evse => evse.Id == EVSEId).
                                      FirstOrDefault();

            if (_VirtualEVSE != null)
            {

                result = await _VirtualEVSE.Reserve(Timestamp,
                                                    CancellationToken,
                                                    EventTrackingId,
                                                    StartTime,
                                                    Duration,
                                                    ReservationId,
                                                    ProviderId,
                                                    ChargingProductId,
                                                    AuthTokens,
                                                    eMAIds,
                                                    PINs,
                                                    QueryTimeout);

            }

            else
                result = ReservationResult.UnknownEVSE;


            return result;

        }

        #endregion

        #region Reserve(...StartTime, Duration, ReservationId = null, ProviderId = null, ...)

        /// <summary>
        /// Reserve the possibility to charge at the given EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp of this request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="StartTime">The starting time of the reservation.</param>
        /// <param name="Duration">The duration of the reservation.</param>
        /// <param name="ReservationId">An optional unique identification of the reservation. Mandatory for updates.</param>
        /// <param name="ProviderId">An optional unique identification of e-Mobility service provider.</param>
        /// <param name="ChargingProductId">An optional unique identification of the charging product to be reserved.</param>
        /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
        /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
        /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<ReservationResult> Reserve(DateTime                 Timestamp,
                                                     CancellationToken        CancellationToken,
                                                     EventTracking_Id         EventTrackingId,
                                                     DateTime?                StartTime,
                                                     TimeSpan?                Duration,
                                                     ChargingReservation_Id   ReservationId      = null,
                                                     EVSP_Id                  ProviderId         = null,
                                                     ChargingProduct_Id       ChargingProductId  = null,
                                                     IEnumerable<Auth_Token>  AuthTokens         = null,
                                                     IEnumerable<eMA_Id>      eMAIds             = null,
                                                     IEnumerable<UInt32>      PINs               = null,
                                                     TimeSpan?                QueryTimeout       = null)
        {

            return ReservationResult.OutOfService;

        }

        #endregion

        #region (internal) SendNewReservation(Timestamp, Sender, Reservation)

        internal void SendNewReservation(DateTime             Timestamp,
                                         Object               Sender,
                                         ChargingReservation  Reservation)
        {

            var OnNewReservationLocal = OnNewReservation;
            if (OnNewReservationLocal != null)
                OnNewReservationLocal(Timestamp, Sender, Reservation);

        }

        #endregion


        #region TryGetReservationById(ReservationId, out Reservation)

        /// <summary>
        /// Return the charging reservation specified by its unique identification.
        /// </summary>
        /// <param name="ReservationId">The charging reservation identification.</param>
        /// <param name="Reservation">The charging reservation identification.</param>
        /// <returns>True when successful, false otherwise.</returns>
        public Boolean TryGetReservationById(ChargingReservation_Id ReservationId, out ChargingReservation Reservation)
        {

            Reservation = _EVSEs.Where (evse => evse.Reservation != null &&
                                                evse.Reservation.Id == ReservationId).
                                 Select(evse => evse.Reservation).
                                 FirstOrDefault();

            return Reservation != null;

        }

        #endregion


        #region OnReservationCancelled

        /// <summary>
        /// An event fired whenever a charging reservation was deleted.
        /// </summary>
        public event OnReservationCancelledDelegate OnReservationCancelled;

        #endregion

        #region CancelReservation(ReservationId)

        /// <summary>
        /// Try to remove the given charging reservation.
        /// </summary>
        /// <param name="ReservationId">The unique charging reservation identification.</param>
        /// <returns>True when successful, false otherwise</returns>
        public async Task<Boolean> CancelReservation(ChargingReservation_Id           ReservationId,
                                                     ChargingReservationCancellation  ReservationCancellation)
        {

            #region Initial checks

            if (ReservationId == null)
                throw new ArgumentNullException(nameof(ReservationId), "The given charging reservation identification must not be null!");

            #endregion


            return await _EVSEs.Where   (evse => evse.Reservation    != null &&
                                                 evse.Reservation.Id == ReservationId).
                                MapFirst(evse => evse.CancelReservation(ReservationId, ReservationCancellation),
                                         Task.FromResult(false));

        }

        #endregion

        #endregion

        #region RemoteStart/-Stop and Sessions

        #region ChargingSessions

        /// <summary>
        /// All current charging sessions.
        /// </summary>
        public IEnumerable<ChargingSession> ChargingSessions
        {
            get
            {

                return _EVSEs.
                           Select(evse    => evse.ChargingSession).
                           Where (session => session != null);

            }
        }

        #endregion

        #region OnNewChargingSession

        /// <summary>
        /// An event fired whenever a new charging session was created.
        /// </summary>
        public event OnNewChargingSessionDelegate  OnNewChargingSession;

        #endregion

        #region RemoteStart(...EVSEId, ChargingProductId = null, ReservationId = null, SessionId = null, ProviderId = null, eMAId = null, ...)

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
        /// <param name="ProviderId">The unique identification of the e-mobility service provider for the case it is different from the current message sender.</param>
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
                        EVSP_Id                 ProviderId         = null,
                        eMA_Id                  eMAId              = null,
                        TimeSpan?               QueryTimeout       = null)
        {

            #region Initial checks

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId),  "The given EVSE identification must not be null!");

            RemoteStartEVSEResult result = null;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;

            #endregion


            var _VirtualEVSE = _EVSEs.Where(evse => evse.Id == EVSEId).
                                      FirstOrDefault();

            if (_VirtualEVSE != null)
            {

                result = await _VirtualEVSE.RemoteStart(Timestamp,
                                                        CancellationToken,
                                                        EventTrackingId,
                                                        ChargingProductId,
                                                        ReservationId,
                                                        SessionId,
                                                        ProviderId,
                                                        eMAId,
                                                        QueryTimeout);

            }

            else
                result = RemoteStartEVSEResult.UnknownEVSE;


            return result;

        }

        #endregion

        #region RemoteStart(...ChargingProductId = null, ReservationId = null, SessionId = null, ProviderId = null, eMAId = null, ...)

        /// <summary>
        /// Start a charging session at the given charging station.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
        /// <param name="ReservationId">The unique identification for a charging reservation.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider for the case it is different from the current message sender.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStartChargingStationResult>

            RemoteStart(DateTime                Timestamp,
                        CancellationToken       CancellationToken,
                        EventTracking_Id        EventTrackingId,
                        ChargingProduct_Id      ChargingProductId  = null,
                        ChargingReservation_Id  ReservationId      = null,
                        ChargingSession_Id      SessionId          = null,
                        EVSP_Id                 ProviderId         = null,
                        eMA_Id                  eMAId              = null,
                        TimeSpan?               QueryTimeout       = null)

        {

            return RemoteStartChargingStationResult.OutOfService;

        }

        #endregion

        #region (internal) SendNewChargingSession(Timestamp, Sender, ChargingSession)

        internal void SendNewChargingSession(DateTime         Timestamp,
                                             Object           Sender,
                                             ChargingSession  ChargingSession)
        {

            var OnNewChargingSessionLocal = OnNewChargingSession;
            if (OnNewChargingSessionLocal != null)
                OnNewChargingSessionLocal(Timestamp, Sender, ChargingSession);

        }

        #endregion


        #region OnNewChargeDetailRecord

        /// <summary>
        /// An event fired whenever a new charge detail record was created.
        /// </summary>
        public event OnNewChargeDetailRecordDelegate  OnNewChargeDetailRecord;

        #endregion

        #region RemoteStop(...SessionId, ReservationHandling, ProviderId = null, QueryTimeout = null)

        /// <summary>
        /// Stop the given charging session.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStopResult>

            RemoteStop(DateTime             Timestamp,
                       CancellationToken    CancellationToken,
                       EventTracking_Id     EventTrackingId,
                       ChargingSession_Id   SessionId,
                       ReservationHandling  ReservationHandling,
                       EVSP_Id              ProviderId    = null,
                       TimeSpan?            QueryTimeout  = null)

        {

            return RemoteStopResult.OutOfService(SessionId);

        }

        #endregion

        #region RemoteStop(...EVSEId, SessionId, ReservationHandling, ProviderId = null, QueryTimeout = null)

        /// <summary>
        /// Stop the given charging session at the given EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="EVSEId">The unique identification of the EVSE to be stopped.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStopEVSEResult>

            RemoteStop(DateTime             Timestamp,
                       CancellationToken    CancellationToken,
                       EventTracking_Id     EventTrackingId,
                       EVSE_Id              EVSEId,
                       ChargingSession_Id   SessionId,
                       ReservationHandling  ReservationHandling,
                       EVSP_Id              ProviderId    = null,
                       TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId),     "The given EVSE identification must not be null!");

            if (SessionId == null)
                throw new ArgumentNullException("SessionId",  "The given charging session identification must not be null!");

            RemoteStopEVSEResult result = null;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;

            #endregion


            var _VirtualEVSE = _EVSEs.Where(evse => evse.Id == EVSEId).
                                      FirstOrDefault();

            if (_VirtualEVSE != null)
            {

                result = await _VirtualEVSE.RemoteStop(Timestamp,
                                                       CancellationToken,
                                                       EventTrackingId,
                                                       SessionId,
                                                       ReservationHandling,
                                                       ProviderId,
                                                       QueryTimeout);

            }

            else
                result = RemoteStopEVSEResult.UnknownEVSE(SessionId);


            return result;

        }

        #endregion

        #region RemoteStop(...ChargingStationId, SessionId, ReservationHandling, ProviderId = null, QueryTimeout = null)

        /// <summary>
        /// Stop the given charging session at the given charging station.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="ChargingStationId">The unique identification of the charging station to be stopped.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStopChargingStationResult>

            RemoteStop(DateTime             Timestamp,
                       CancellationToken    CancellationToken,
                       EventTracking_Id     EventTrackingId,
                       ChargingStation_Id   ChargingStationId,
                       ChargingSession_Id   SessionId,
                       ReservationHandling  ReservationHandling,
                       EVSP_Id              ProviderId    = null,
                       TimeSpan?            QueryTimeout  = null)

        {

            return RemoteStopChargingStationResult.OutOfService(SessionId);

        }

        #endregion

        #region (internal) SendNewChargeDetailRecord(Timestamp, Sender, ChargeDetailRecord)

        internal void SendNewChargeDetailRecord(DateTime            Timestamp,
                                                Object              Sender,
                                                ChargeDetailRecord  ChargeDetailRecord)
        {

            var OnNewChargeDetailRecordLocal = OnNewChargeDetailRecord;
            if (OnNewChargeDetailRecordLocal != null)
                OnNewChargeDetailRecordLocal(Timestamp, Sender, ChargeDetailRecord);

        }

        #endregion

        #endregion




        //-- Client-side methods -----------------------------------------

        #region AuthenticateToken(AuthToken)

        public Boolean AuthenticateToken(Auth_Token AuthToken)
        {
            return false;
        }

        #endregion



    }

}
