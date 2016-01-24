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
using System.Collections.Concurrent;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.Sockets.TCP;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{



    /// <summary>
    /// A demo implementation of a virtual WWCP charging station.
    /// </summary>
    public class VirtualChargingStation : IRemoteChargingStation
    {

        #region Data

        private readonly  TCPClient  _TCPClient;

        #endregion

        #region Properties

        #region ChargingStationId

        private ChargingStation_Id _Id;

        public ChargingStation_Id Id
        {
            get
            {
                return _Id;
            }
        }

        #endregion

        #region ChargingStation

        private readonly ChargingStation _ChargingStation;

        public ChargingStation ChargingStation
        {
            get
            {
                return _ChargingStation;
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


        #region EVSEOperatorDNS

        public String EVSEOperatorDNS
        {

            get
            {
                return _TCPClient.RemoteHost;
            }

            //set
            //{
            //    if (value != null && value != String.Empty)
            //        _TCPClient.RemoteHost = value;
            //}

        }

        #endregion

        #region EVSEOperatorTimeout

        public TimeSpan EVSEOperatorTimeout
        {

            get
            {
                return _TCPClient.ConnectionTimeout;
            }

            set
            {
                _TCPClient.ConnectionTimeout = value;
            }

        }

        #endregion

        #region UseIPv4

        public Boolean UseIPv4
        {

            get
            {
                return _TCPClient.UseIPv4;
            }

            //set
            //{
            //    _TCPClient.UseIPv4 = value;
            //}

        }

        #endregion

        #region UseIPv6

        public Boolean UseIPv6
        {

            get
            {
                return _TCPClient.UseIPv6;
            }

            //set
            //{
            //    _TCPClient.UseIPv6 = value;
            //}

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

        #endregion

        #region Events


        // EVSE events

        #region EVSEAddition

        internal readonly IVotingNotificator<DateTime, IRemoteChargingStation, IRemoteEVSE, Boolean> EVSEAddition;

        /// <summary>
        /// Called whenever an EVSE will be or was added.
        /// </summary>
        public IVotingSender<DateTime, IRemoteChargingStation, IRemoteEVSE, Boolean> OnEVSEAddition
        {
            get
            {
                return EVSEAddition;
            }
        }

        #endregion

        #region OnEVSEDataChanged

        /// <summary>
        /// An event fired whenever the static data of any subordinated EVSE changed.
        /// </summary>
        public event OnEVSEDataChangedDelegate OnEVSEDataChanged;

        #endregion

        #region OnEVSE(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the dynamic status of any subordinated EVSE changed.
        /// </summary>
        public event OnEVSEStatusChangedDelegate       OnEVSEStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status of any subordinated EVSE changed.
        /// </summary>
        public event OnEVSEAdminStatusChangedDelegate  OnEVSEAdminStatusChanged;

        #endregion

        #region OnReserveEVSE / OnReservedEVSE

        /// <summary>
        /// An event fired whenever a reserve EVSE command was received.
        /// </summary>
        public event OnReserveEVSEDelegate   OnReserveEVSE;

        /// <summary>
        /// An event fired whenever a reserve EVSE command completed.
        /// </summary>
        public event OnEVSEReservedDelegate  OnEVSEReserved;

        #endregion

        #region OnRemoteEVSEStart / OnRemoteEVSEStarted

        /// <summary>
        /// An event fired whenever a remote start EVSE command was received.
        /// </summary>
        public event OnRemoteEVSEStartDelegate    OnRemoteEVSEStart;

        /// <summary>
        /// An event fired whenever a remote start EVSE command completed.
        /// </summary>
        public event OnRemoteEVSEStartedDelegate  OnRemoteEVSEStarted;

        #endregion

        #region OnRemoteEVSEStop / OnRemoteEVSEStopped

        /// <summary>
        /// An event fired whenever a remote stop EVSE command was received.
        /// </summary>
        public event OnRemoteEVSEStopDelegate     OnRemoteEVSEStop;

        /// <summary>
        /// An event fired whenever a remote stop EVSE command completed.
        /// </summary>
        public event OnRemoteEVSEStoppedDelegate  OnRemoteEVSEStopped;

        #endregion


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

        #endregion

        #region Constructor(s)

        /// <summary>
        /// A virtual WWCP charging station.
        /// </summary>
        /// <param name="ChargingStation">A local charging station.</param>
        public VirtualChargingStation(ChargingStation  ChargingStation)
        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException("ChargingStation", "The given charging station parameter must not be null!");

            #endregion

            this._Id               = ChargingStation.Id;
            this._ChargingStation  = ChargingStation;
            this._Status           = ChargingStationStatusType.Available;
            this._EVSEs            = new HashSet<VirtualEVSE>();

            #region Init events

            // ChargingStation events
            this.EVSEAddition      = new VotingNotificator<DateTime, IRemoteChargingStation, IRemoteEVSE, Boolean>(() => new VetoVote(), true);
            //  this.EVSERemoval                = new VotingNotificator<DateTime, ChargingStation, EVSE, Boolean>(() => new VetoVote(), true);

            //  // EVSE events
            //  this.SocketOutletAddition       = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);
            //  this.SocketOutletRemoval        = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);

            #endregion

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

            if (EVSEAddition.SendVoting(Now, this, _VirtualEVSE))
            {
                if (_EVSEs.Add(_VirtualEVSE))
                {

                    _VirtualEVSE.OnPropertyChanged     += (Timestamp, Sender, PropertyName, OldValue, NewValue)
                                                    => UpdateEVSEData       (Timestamp, Sender as VirtualEVSE, PropertyName, OldValue, NewValue);

                    _VirtualEVSE.OnStatusChanged       += (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus)
                                                    => UpdateEVSEStatus     (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus);

                    _VirtualEVSE.OnAdminStatusChanged  += (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus)
                                                    => UpdateEVSEAdminStatus(Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus);

                    OnSuccess.FailSafeInvoke(_VirtualEVSE);
                    EVSEAddition.SendNotification(Now, this, _VirtualEVSE);
               //     UpdateEVSEStatus(Now, _EVSE, new Timestamped<EVSEStatusType>(Now, EVSEStatusType.Unspecified), _EVSE.Status);

                    return _VirtualEVSE;

                }
            }

            //Debug.WriteLine("EVSE '" + EVSEId + "' was not created!");
            return null;

        }

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

            #region Send OnReserveEVSE event

            var OnReserveEVSELocal = OnReserveEVSE;
            if (OnReserveEVSELocal != null)
                OnReserveEVSELocal(this,
                                   Timestamp,
                                   EventTrackingId,
                                   _ChargingStation.ChargingPool.Operator.RoamingNetwork.Id,
                                   ReservationId,
                                   EVSEId,
                                   StartTime,
                                   Duration,
                                   ProviderId,
                                   ChargingProductId,
                                   AuthTokens,
                                   eMAIds,
                                   PINs);

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


            #region Send OnEVSEReserved event

            var OnEVSEReservedLocal = OnEVSEReserved;
            if (OnEVSEReservedLocal != null)
                OnEVSEReservedLocal(this,
                                    Timestamp,
                                    EventTrackingId,
                                    _ChargingStation.ChargingPool.Operator.RoamingNetwork.Id,
                                    ReservationId,
                                    EVSEId,
                                    StartTime,
                                    Duration,
                                    ProviderId,
                                    ChargingProductId,
                                    AuthTokens,
                                    eMAIds,
                                    PINs,
                                    result);

            #endregion

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



        #region RemoteStart(...EVSEId, ChargingProductId, ReservationId, SessionId, ProviderId, eMAId)

        /// <summary>
        /// Initiate a remote start of the given charging session at the given EVSE
        /// and for the given Provider/eMAId.
        /// </summary>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ChargingProductId">The unique identification of the choosen charging product at the given EVSE.</param>
        /// <param name="ReservationId">The unique identification for a charging reservation.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// <returns>A RemoteStartResult task.</returns>
        public async Task<RemoteStartEVSEResult> RemoteStart(DateTime                Timestamp,
                                                             CancellationToken       CancellationToken,
                                                             EventTracking_Id        EventTrackingId,
                                                             EVSE_Id                 EVSEId,
                                                             ChargingProduct_Id      ChargingProductId,
                                                             ChargingReservation_Id  ReservationId,
                                                             ChargingSession_Id      SessionId,
                                                             EVSP_Id                 ProviderId,
                                                             eMA_Id                  eMAId,
                                                             TimeSpan?               QueryTimeout  = null)
        {

            #region Initial checks

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId),  "The given EVSE identification must not be null!");

            RemoteStartEVSEResult result = null;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;

            #endregion

            #region Send OnRemoteEVSEStart event

            var OnRemoteEVSEStartLocal = OnRemoteEVSEStart;
            if (OnRemoteEVSEStartLocal != null)
                OnRemoteEVSEStartLocal(this,
                                       Timestamp,
                                       EventTrackingId,
                                       _ChargingStation.ChargingPool.Operator.RoamingNetwork.Id,
                                       EVSEId,
                                       ChargingProductId,
                                       ReservationId,
                                       SessionId,
                                       ProviderId,
                                       eMAId,
                                       QueryTimeout.Value);

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


            #region Send OnRemoteEVSEStarted event

            var OnRemoteEVSEStartedLocal = OnRemoteEVSEStarted;
            if (OnRemoteEVSEStartedLocal != null)
                OnRemoteEVSEStartedLocal(this,
                                         Timestamp,
                                         EventTrackingId,
                                         _ChargingStation.ChargingPool.Operator.RoamingNetwork.Id,
                                         EVSEId,
                                         ChargingProductId,
                                         ReservationId,
                                         SessionId,
                                         ProviderId,
                                         eMAId,
                                         QueryTimeout,
                                         result);

            #endregion

            return result;

        }

        #endregion

        #region RemoteStart(...ChargingProductId, ReservationId, SessionId, eMAId)

        /// <summary>
        /// Initiate a remote start of the given charging session at the given charging station
        /// and for the given provider/eMAId.
        /// </summary>
        /// <param name="ChargingProductId">The unique identification of the choosen charging product at the given EVSE.</param>
        /// <param name="ReservationId">The unique identification for a charging reservation.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// <returns>A RemoteStartResult task.</returns>
        public async Task<RemoteStartChargingStationResult> RemoteStart(DateTime                Timestamp,
                                                                        CancellationToken       CancellationToken,
                                                                        EventTracking_Id        EventTrackingId,
                                                                        ChargingProduct_Id      ChargingProductId,
                                                                        ChargingReservation_Id  ReservationId,
                                                                        ChargingSession_Id      SessionId,
                                                                        EVSP_Id                 ProviderId,
                                                                        eMA_Id                  eMAId,
                                                                        TimeSpan?               QueryTimeout  = null)
        {

            return RemoteStartChargingStationResult.OutOfService;

        }

        #endregion


        #region RemoteStop(Timestamp, CancellationToken, SessionId, ReservationHandling)

        /// <summary>
        /// Initiate a remote stop of the given charging session at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <returns>A RemoteStopResult task.</returns>
        public async Task<RemoteStopResult> RemoteStop(DateTime             Timestamp,
                                                       CancellationToken    CancellationToken,
                                                       EventTracking_Id     EventTrackingId,
                                                       ChargingSession_Id   SessionId,
                                                       ReservationHandling  ReservationHandling,
                                                       EVSP_Id              ProviderId,
                                                       TimeSpan?            QueryTimeout  = null)
        {

            return RemoteStopResult.OutOfService(SessionId);

        }

        #endregion

        #region RemoteStop(Timestamp, CancellationToken, EVSEId, SessionId, ReservationHandling)

        /// <summary>
        /// Initiate a remote stop of the given charging session at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <returns>A RemoteStopResult task.</returns>
        public async Task<RemoteStopEVSEResult> RemoteStop(DateTime             Timestamp,
                                                           CancellationToken    CancellationToken,
                                                           EventTracking_Id     EventTrackingId,
                                                           EVSE_Id              EVSEId,
                                                           ChargingSession_Id   SessionId,
                                                           ReservationHandling  ReservationHandling,
                                                           EVSP_Id              ProviderId,
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

            #region Send OnRemoteEVSEStop event

            var OnRemoteEVSEStopLocal = OnRemoteEVSEStop;
            if (OnRemoteEVSEStopLocal != null)
                OnRemoteEVSEStopLocal(this,
                                      Timestamp,
                                      EventTrackingId,
                                      _ChargingStation.ChargingPool.Operator.RoamingNetwork.Id,
                                      EVSEId,
                                      SessionId,
                                      ReservationHandling,
                                      ProviderId,
                                      QueryTimeout.Value);

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


            #region Send OnRemoteEVSEStarted event

            var OnRemoteEVSEStoppedLocal = OnRemoteEVSEStopped;
            if (OnRemoteEVSEStoppedLocal != null)
                OnRemoteEVSEStoppedLocal(this,
                                         Timestamp,
                                         EventTrackingId,
                                         _ChargingStation.ChargingPool.Operator.RoamingNetwork.Id,
                                         EVSEId,
                                         SessionId,
                                         ReservationHandling,
                                         ProviderId,
                                         QueryTimeout,
                                         result);

            #endregion

            return result;

        }

        #endregion

        #region RemoteStop(Timestamp, CancellationToken, ChargingStationId, SessionId, ReservationHandling)

        /// <summary>
        /// Initiate a remote stop of the given charging session at the given charging station.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of a charging station.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <returns>A RemoteStopResult task.</returns>
        public async Task<RemoteStopChargingStationResult> RemoteStop(DateTime             Timestamp,
                                                                      CancellationToken    CancellationToken,
                                                                      EventTracking_Id     EventTrackingId,
                                                                      ChargingStation_Id   ChargingStationId,
                                                                      ChargingSession_Id   SessionId,
                                                                      ReservationHandling  ReservationHandling,
                                                                      EVSP_Id              ProviderId,
                                                                      TimeSpan?            QueryTimeout  = null)
        {

            return RemoteStopChargingStationResult.OutOfService(SessionId);

        }

        #endregion



        #region (internal) UpdateEVSEData(Timestamp, VirtualEVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update the data of an EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="VirtualEVSE">The changed EVSE.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        internal void UpdateEVSEData(DateTime     Timestamp,
                                     IRemoteEVSE  VirtualEVSE,
                                     String       PropertyName,
                                     Object       OldValue,
                                     Object       NewValue)
        {

            var OnEVSEDataChangedLocal = OnEVSEDataChanged;
            if (OnEVSEDataChangedLocal != null)
                OnEVSEDataChangedLocal(Timestamp, VirtualEVSE, PropertyName, OldValue, NewValue);

        }

        #endregion

        #region (internal) UpdateEVSEStatus(Timestamp, VirtualEVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update the current charging station status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="VirtualEVSE">The updated EVSE.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal void UpdateEVSEStatus(DateTime                     Timestamp,
                                       IRemoteEVSE                  VirtualEVSE,
                                       Timestamped<EVSEStatusType>  OldStatus,
                                       Timestamped<EVSEStatusType>  NewStatus)
        {

            var OnEVSEStatusChangedLocal = OnEVSEStatusChanged;
            if (OnEVSEStatusChangedLocal != null)
                OnEVSEStatusChangedLocal(Timestamp, VirtualEVSE, OldStatus, NewStatus);

            //if (StatusAggregationDelegate != null)
            //    _StatusSchedule.Insert(Timestamp,
            //                           StatusAggregationDelegate(new EVSEStatusReport(_EVSEs)));

        }

        #endregion

        #region (internal) UpdateEVSEAdminStatus(Timestamp, VirtualEVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update the current charging station status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="VirtualEVSE">The updated EVSE.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal void UpdateEVSEAdminStatus(DateTime                          Timestamp,
                                            IRemoteEVSE                       VirtualEVSE,
                                            Timestamped<EVSEAdminStatusType>  OldStatus,
                                            Timestamped<EVSEAdminStatusType>  NewStatus)
        {

            var OnEVSEAdminStatusChangedLocal = OnEVSEAdminStatusChanged;
            if (OnEVSEAdminStatusChangedLocal != null)
                OnEVSEAdminStatusChangedLocal(Timestamp, VirtualEVSE, OldStatus, NewStatus);

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




        //-- Client-side methods -----------------------------------------

        #region AuthenticateToken(AuthToken)

        public Boolean AuthenticateToken(Auth_Token AuthToken)
        {
            return false;
        }

        #endregion



    }

}
