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
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using System.Net.Security;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// A remote charging station attached via a computer network (TCP/IP).
    /// </summary>
    public class NetworkChargingStationStub : INetworkChargingStation
    {

        #region Data

        public static readonly TimeSpan  DefaultQueryTimeout  = TimeSpan.FromSeconds(180);

        #endregion

        #region Properties

        #region Id

        private ChargingStation_Id _Id;

        /// <summary>
        /// The unique identification of this network charging station.
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


        #region IPTransport

        private readonly IPTransport _IPTransport;

        public IPTransport IPTransport
        {
            get
            {
                return _IPTransport;
            }
        }

        #endregion

        #region DNSClient

        private readonly DNSClient _DNSClient;

        public DNSClient DNSClient
        {
            get
            {
                return _DNSClient;
            }
        }

        #endregion

        #region Hostname

        private readonly String _Hostname;

        public String Hostname
        {
            get
            {
                return _Hostname;
            }
        }

        #endregion

        #region TCPPort

        private readonly IPPort _TCPPort;

        public IPPort TCPPort
        {
            get
            {
                return _TCPPort;
            }
        }

        #endregion

        #region Service

        private readonly String _Service;

        public String Service
        {
            get
            {
                return _Service;
            }
        }

        #endregion

        #region RemoteCertificateValidator

        protected readonly RemoteCertificateValidationCallback _RemoteCertificateValidator;

        /// <summary>
        /// A delegate to verify the remote TLS certificate.
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidator
        {
            get
            {
                return _RemoteCertificateValidator;
            }
        }

        #endregion

        #region VirtualHost

        private readonly String _VirtualHost;

        public String VirtualHost
        {
            get
            {
                return _VirtualHost;
            }
        }

        #endregion

        #region URIPrefix

        private readonly String _URIPrefix;

        public String URIPrefix
        {
            get
            {
                return _URIPrefix;
            }
        }

        #endregion

        #region QueryTimeout

        private readonly TimeSpan _QueryTimeout;

        public TimeSpan QueryTimeout
        {
            get
            {
                return _QueryTimeout;
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

        #region EVSEs

        private readonly HashSet<NetworkEVSEStub> _EVSEs;

        /// <summary>
        /// All registered EVSEs.
        /// </summary>
        public IEnumerable<NetworkEVSEStub> EVSEs
        {
            get
            {
                return _EVSEs;
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
        public event OnRemoteEVSEDataChangedDelegate OnRemoteEVSEDataChanged;

        #endregion

        #region OnEVSE(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the dynamic status of any subordinated EVSE changed.
        /// </summary>
        public event OnRemoteEVSEStatusChangedDelegate       OnRemoteEVSEStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status of any subordinated EVSE changed.
        /// </summary>
        public event OnRemoteEVSEAdminStatusChangedDelegate  OnRemoteEVSEAdminStatusChanged;

        #endregion

        #region OnReserveEVSE / OnReservedEVSE

        /// <summary>
        /// An event fired whenever a reserve EVSE command was received.
        /// </summary>
        public event OnEVSEReserveDelegate   OnReserveEVSE;

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
        public event OnReservationCancelledDelegate OnReservationCancelled;

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

        #region NetworkChargingStationStub(ChargingStation)

        /// <summary>
        /// A charging station.
        /// </summary>
        /// <param name="ChargingStation">A local charging station.</param>
        public NetworkChargingStationStub(ChargingStation  ChargingStation)
        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException("ChargingStation", "The given charging station parameter must not be null!");

            #endregion

            this._Id               = ChargingStation.Id;
            this._ChargingStation  = ChargingStation;
            this._Status           = ChargingStationStatusType.Available;
            this._EVSEs            = new HashSet<NetworkEVSEStub>();

            #region Init events

            // ChargingStation events
            this.EVSEAddition               = new VotingNotificator<DateTime, IRemoteChargingStation, IRemoteEVSE, Boolean>(() => new VetoVote(), true);
          //  this.EVSERemoval                = new VotingNotificator<DateTime, ChargingStation, EVSE, Boolean>(() => new VetoVote(), true);

          //  // EVSE events
          //  this.SocketOutletAddition       = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);
          //  this.SocketOutletRemoval        = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);

            #endregion

        }

        #endregion

        #region NetworkChargingStationStub(Id, IPTransport = IPv4only, DNSClient = null, Hostname = DefaultHostname, TCPPort = null, ...)

        /// <summary>
        /// A virtual WWCP charging station.
        /// </summary>
        /// <param name="ChargingStation">A local charging station.</param>
        /// <param name="DNSClient">An optional DNS client used to resolve DNS names.</param>
        public NetworkChargingStationStub(ChargingStation                      ChargingStation,
                                          IPTransport                          IPTransport                 = IPTransport.IPv4only,
                                          DNSClient                            DNSClient                   = null,
                                          String                               Hostname                    = null,
                                          IPPort                               TCPPort                     = null,
                                          String                               Service                     = null,
                                          RemoteCertificateValidationCallback  RemoteCertificateValidator  = null,
                                          String                               VirtualHost                 = null,
                                          String                               URIPrefix                   = null,
                                          TimeSpan?                            QueryTimeout                = null)

            : this(ChargingStation)

        {

            this._IPTransport                 = IPTransport;
            this._DNSClient                   = DNSClient != null              ? DNSClient          : new DNSClient(SearchForIPv4DNSServers: true,
                                                                                                                    SearchForIPv6DNSServers: false);
            this._Hostname                    = Hostname;
            this._TCPPort                     = TCPPort;
            this._Service                     = Service;
            this._RemoteCertificateValidator  = RemoteCertificateValidator;
            this._VirtualHost                 = VirtualHost.IsNotNullOrEmpty() ? VirtualHost        : Hostname;
            this._URIPrefix                   = URIPrefix;
            this._QueryTimeout                = QueryTimeout.HasValue          ? QueryTimeout.Value : DefaultQueryTimeout;

        }

        #endregion

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
        public NetworkEVSEStub CreateNewEVSE(EVSE_Id                                 EVSEId,
                                        Action<NetworkEVSEStub>                      Configurator  = null,
                                        Action<NetworkEVSEStub>                      OnSuccess     = null,
                                        Action<NetworkChargingStationStub, EVSE_Id>  OnError       = null)
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
            var _NetworkEVSE  = new NetworkEVSEStub(EVSEId, this);

            Configurator.FailSafeInvoke(_NetworkEVSE);

            if (EVSEAddition.SendVoting(Now, this, _NetworkEVSE))
            {
                if (_EVSEs.Add(_NetworkEVSE))
                {

               //     _EVSE.OnPropertyChanged     += (Timestamp, Sender, PropertyName, OldValue, NewValue)
               //                                     => UpdateEVSEData       (Timestamp, Sender as EVSE, PropertyName, OldValue, NewValue);
               //
               //     _EVSE.OnStatusChanged       += (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus)
               //                                     => UpdateEVSEStatus     (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus);
               //
               //     _EVSE.OnAdminStatusChanged  += (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus)
               //                                     => UpdateEVSEAdminStatus(Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus);

                    OnSuccess.FailSafeInvoke(_NetworkEVSE);
                    EVSEAddition.SendNotification(Now, this, _NetworkEVSE);
               //     UpdateEVSEStatus(Now, _EVSE, new Timestamped<EVSEStatusType>(Now, EVSEStatusType.Unspecified), _EVSE.Status);

                    return _NetworkEVSE;

                }
            }

            //Debug.WriteLine("EVSE '" + EVSEId + "' was not created!");
            return null;

        }

        #endregion



        public virtual async Task<IEnumerable<EVSEStatus>> GetEVSEStatus(DateTime                 Timestamp,
                                                                         CancellationToken        CancellationToken,
                                                                         EventTracking_Id         EventTrackingId,
                                                                         TimeSpan?                QueryTimeout = null)
        {

            return new EVSEStatus[] {
                        new EVSEStatus(EVSE_Id.Parse("DE*822*E222*1"), EVSEStatusType.Charging)
                    };

        }


        public IEnumerable<ChargingReservation> ChargingReservations
        {
            get
            {
                return new ChargingReservation[0];
            }
        }

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
        public virtual async Task<ReservationResult> Reserve(DateTime                 Timestamp,
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

            return ReservationResult.OutOfService;

        }

        #endregion

        #region Reserve(...StartTime, Duration, ReservationId = null, ProviderId = null, ...)

        /// <summary>
        /// Reserve the possibility to charge at the given charging station.
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
        public virtual async Task<ReservationResult> Reserve(DateTime                 Timestamp,
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

        #region TryGetReservationById(ReservationId, out Reservation)

        public Boolean TryGetReservationById(ChargingReservation_Id ReservationId, out ChargingReservation Reservation)
        {

            Reservation = _EVSEs.Where (evse => evse.Reservation != null &&
                                                evse.Reservation.Id == ReservationId).
                                 Select(evse => evse.Reservation).
                                 FirstOrDefault();

            return Reservation != null;

        }

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
        public virtual async Task<RemoteStartEVSEResult>

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

            return RemoteStartEVSEResult.OutOfService;

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
        public virtual async Task<RemoteStopEVSEResult>

            RemoteStop(DateTime             Timestamp,
                       CancellationToken    CancellationToken,
                       EventTracking_Id     EventTrackingId,
                       EVSE_Id              EVSEId,
                       ChargingSession_Id   SessionId,
                       ReservationHandling  ReservationHandling,
                       EVSP_Id              ProviderId    = null,
                       TimeSpan?            QueryTimeout  = null)

        {

            return RemoteStopEVSEResult.OutOfService(SessionId);

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

    }

}
