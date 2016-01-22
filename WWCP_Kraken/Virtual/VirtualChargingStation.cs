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

        private readonly ConcurrentDictionary<EVSE_Id, VirtualEVSE> _EVSEs;

        public IEnumerable<VirtualEVSE> EVSEs
        {
            get
            {
                return _EVSEs.Select(kvp => kvp.Value);
            }
        }

        #endregion

        #endregion

        #region Events

        #region Connected

        public event CSConnectedDelegate Connected;

        #endregion

        #region EVSEOperatorTimeoutReached

        public event CSEVSEOperatorTimeoutReachedDelegate EVSEOperatorTimeoutReached;

        #endregion

        #region Disconnected

        public event CSDisconnectedDelegate Disconnected;

        #endregion

        #region StateChanged

        public event CSStateChangedDelegate StateChanged;

        #endregion


        #region EVSEAddition

        internal readonly IVotingNotificator<DateTime, VirtualChargingStation, VirtualEVSE, Boolean> EVSEAddition;

        /// <summary>
        /// Called whenever an EVSE will be or was added.
        /// </summary>
        public IVotingSender<DateTime, VirtualChargingStation, VirtualEVSE, Boolean> OnEVSEAddition
        {
            get
            {
                return EVSEAddition;
            }
        }

        #endregion

        #endregion

        #region Constructor(s)

        #region (private) VirtualChargingStation()

        private VirtualChargingStation()
        {

            this._EVSEs                     = new ConcurrentDictionary<EVSE_Id, VirtualEVSE>();

            #region Init events

            // ChargingStation events
            this.EVSEAddition               = new VotingNotificator<DateTime, VirtualChargingStation, VirtualEVSE, Boolean>(() => new VetoVote(), true);
          //  this.EVSERemoval                = new VotingNotificator<DateTime, ChargingStation, EVSE, Boolean>(() => new VetoVote(), true);

          //  // EVSE events
          //  this.SocketOutletAddition       = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);
          //  this.SocketOutletRemoval        = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);

            #endregion

        }

        #endregion

        #region VirtualChargingStation(ChargingStation)

        /// <summary>
        /// A virtual WWCP charging station.
        /// </summary>
        /// <param name="ChargingStation">A local charging station.</param>
        public VirtualChargingStation(ChargingStation  ChargingStation)
            : this()
        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException("ChargingStation", "The given charging station parameter must not be null!");

            #endregion

            this._Id               = ChargingStation.Id;
            this._ChargingStation  = ChargingStation;
            this._Status           = ChargingStationStatusType.Available;

        }

        #endregion

        #region VirtualChargingStation(Id, EVSEOperatorDNS = null, EVSEOperatorTimeout = default, EVSEOperatorTimeout = null, DNSClient = null, AutoConnect = false)

        /// <summary>
        /// A virtual WWCP charging station.
        /// </summary>
        /// <param name="Id">The unique identifier of the charging station.</param>
        /// <param name="EVSEOperatorDNS">The optional DNS name of the EVSE operator backend to connect to.</param>
        /// <param name="UseIPv4">Wether to use IPv4 as networking protocol.</param>
        /// <param name="UseIPv6">Wether to use IPv6 as networking protocol.</param>
        /// <param name="PreferIPv6">Prefer IPv6 (instead of IPv4) as networking protocol.</param>
        /// <param name="EVSEOperatorTimeout">The timeout connecting to the EVSE operator backend.</param>
        /// <param name="DNSClient">An optional DNS client used to resolve DNS names.</param>
        /// <param name="AutoConnect">Connect to the EVSE operator backend automatically on startup. Default is false.</param>
        public VirtualChargingStation(ChargingStation_Id  Id,
                                      String              EVSEOperatorDNS      = "",
                                      Boolean             UseIPv4              = true,
                                      Boolean             UseIPv6              = false,
                                      Boolean             PreferIPv6           = false,
                                      TimeSpan?           EVSEOperatorTimeout  = null,
                                      DNSClient           DNSClient            = null,
                                      Boolean             AutoConnect          = false)
            : this()
        {

            if (Id == null)
                throw new ArgumentNullException("Id", "The charging station identifier must not be null!");

            this._Id         = Id;
            this._Status     = ChargingStationStatusType.Offline;

            this._TCPClient  = new TCPClient(DNSName:            EVSEOperatorDNS,
                                             ServiceName:        "WWCP",
                                             UseIPv4:            UseIPv4,
                                             UseIPv6:            UseIPv6,
                                             PreferIPv6:         PreferIPv6,
                                             ConnectionTimeout:  EVSEOperatorTimeout,
                                             DNSClient:          (DNSClient != null)
                                                                     ? DNSClient
                                                                     : new DNSClient(SearchForIPv4DNSServers: true,
                                                                                     SearchForIPv6DNSServers: false),
                                             AutoConnect:        false);

           // if (AutoConnect)
           //     Connect();

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
        public VirtualEVSE CreateNewEVSE(EVSE_Id                                  EVSEId,
                                         Action<VirtualEVSE>                      Configurator  = null,
                                         Action<VirtualEVSE>                      OnSuccess     = null,
                                         Action<VirtualChargingStation, EVSE_Id>  OnError       = null)
        {

            #region Initial checks

            if (EVSEId == null)
                throw new ArgumentNullException("EVSEId", "The given EVSE identification must not be null!");

            if (_EVSEs.ContainsKey(EVSEId))
            {
                if (OnError == null)
                    throw new EVSEAlreadyExistsInStation(EVSEId, this.Id);
                else
                    OnError.FailSafeInvoke(this, EVSEId);
            }

            #endregion

            var Now   = DateTime.Now;
            var _EVSE = new VirtualEVSE(EVSEId, this);

            Configurator.FailSafeInvoke(_EVSE);

            if (EVSEAddition.SendVoting(Now, this, _EVSE))
            {
                if (_EVSEs.TryAdd(EVSEId, _EVSE))
                {

               //     _EVSE.OnPropertyChanged     += (Timestamp, Sender, PropertyName, OldValue, NewValue)
               //                                     => UpdateEVSEData       (Timestamp, Sender as EVSE, PropertyName, OldValue, NewValue);
               //
               //     _EVSE.OnStatusChanged       += (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus)
               //                                     => UpdateEVSEStatus     (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus);
               //
               //     _EVSE.OnAdminStatusChanged  += (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus)
               //                                     => UpdateEVSEAdminStatus(Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus);

                    OnSuccess.FailSafeInvoke(_EVSE);
                    EVSEAddition.SendNotification(Now, this, _EVSE);
               //     UpdateEVSEStatus(Now, _EVSE, new Timestamped<EVSEStatusType>(Now, EVSEStatusType.Unspecified), _EVSE.Status);

                    return _EVSE;

                }
            }

            //Debug.WriteLine("EVSE '" + EVSEId + "' was not created!");
            return null;

        }

        #endregion



        #region Reserve(Timestamp, CancellationToken, ...)

        public async Task<ReservationResult> ReserveEVSE(DateTime                 Timestamp,
                                                         CancellationToken        CancellationToken,
                                                         EventTracking_Id         EventTrackingId,
                                                         EVSP_Id                  ProviderId,
                                                         ChargingReservation_Id   ReservationId,
                                                         DateTime?                StartTime,
                                                         TimeSpan?                Duration,
                                                         EVSE_Id                  EVSEId,
                                                         ChargingProduct_Id       ChargingProductId  = null,
                                                         IEnumerable<Auth_Token>  RFIDIds            = null,
                                                         IEnumerable<eMA_Id>      eMAIds             = null,
                                                         IEnumerable<UInt32>      PINs               = null,
                                                         TimeSpan?                QueryTimeout       = null)
        {

            switch (Status)
            {

                case ChargingStationStatusType.OutOfService:
                    return ReservationResult.OutOfService;

                case ChargingStationStatusType.Charging:
                    return ReservationResult.AlreadyInUse;

                case ChargingStationStatusType.Reserved:
                    return ReservationResult.AlreadyReserved;

                case ChargingStationStatusType.Available:

                    //this._Reservation = new ChargingReservation(Timestamp,
                    //                                            StartTime.HasValue ? StartTime.Value : DateTime.Now,
                    //                                            Duration.HasValue ? Duration.Value : MaxReservationDuration,
                    //                                            ProviderId,
                    //                                            ChargingReservationType.AtChargingStation,
                    //                                            ChargingPool.EVSEOperator.RoamingNetwork,
                    //                                            ChargingPool.Id,
                    //                                            Id,
                    //                                            null,
                    //                                            ChargingProductId,
                    //                                            RFIDIds,
                    //                                            eMAIds,
                    //                                            PINs);
                    //
                    //SetStatus(EVSEStatusType.Reserved);

                    //return ReservationResult.Success(_Reservation);
                    return ReservationResult.Error();

                default:
                    return ReservationResult.Error();

            }

        }

        #endregion


        #region RemoteStart(Timestamp, CancellationToken, EVSEId, ChargingProductId, ReservationId, SessionId, eMAId)

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

            // SessionId_AlreadyInUse,
            // EVSE_NotReachable,
            // Start_Timeout

            VirtualEVSE _VirtualEVSE = null;

            if (_EVSEs != null &&
                _EVSEs.TryGetValue(EVSEId, out _VirtualEVSE))
            {

                #region Available

                if (_VirtualEVSE.Status.Value == EVSEStatusType.Available)
                {
                    _VirtualEVSE.CurrentChargingSession = ChargingSession_Id.New;
                    return RemoteStartEVSEResult.Success(_VirtualEVSE.CurrentChargingSession);
                }

                #endregion

                #region Reserved

                else if (_VirtualEVSE.Status.Value == EVSEStatusType.Reserved)
                {

                    if (_VirtualEVSE.ReservationId == ReservationId)
                    {
                        _VirtualEVSE.CurrentChargingSession = ChargingSession_Id.New;
                        return RemoteStartEVSEResult.Success(_VirtualEVSE.CurrentChargingSession);
                    }

                    else
                        return RemoteStartEVSEResult.Reserved;

                }

                #endregion

                #region Charging

                else if (_VirtualEVSE.Status.Value == EVSEStatusType.Charging)
                {
                    return RemoteStartEVSEResult.AlreadyInUse;
                }

                #endregion

                #region OutOfService

                else if (_VirtualEVSE.Status.Value == EVSEStatusType.OutOfService)
                {
                    return RemoteStartEVSEResult.OutOfService;
                }

                #endregion

                #region Offline

                else if (_VirtualEVSE.Status.Value == EVSEStatusType.Offline)
                {
                    return RemoteStartEVSEResult.Offline;
                }

                #endregion

                else
                    return RemoteStartEVSEResult.Error();

            }

            return RemoteStartEVSEResult.UnknownEVSE;

        }

        #endregion

        #region RemoteStart(Timestamp, CancellationToken, ChargingProductId, ReservationId, SessionId, eMAId)

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

            VirtualEVSE _VirtualEVSE = null;

            if (_EVSEs != null &&
                _EVSEs.TryGetValue(EVSEId, out _VirtualEVSE))
            {

                #region Available

                if (_VirtualEVSE.Status.Value == EVSEStatusType.Available)
                {
                    return RemoteStopEVSEResult.InvalidSessionId(SessionId);
                }

                #endregion

                #region Reserved

                else if (_VirtualEVSE.Status.Value == EVSEStatusType.Reserved)
                {
                    return RemoteStopEVSEResult.InvalidSessionId(SessionId);
                }

                #endregion

                #region Charging

                else if (_VirtualEVSE.Status.Value == EVSEStatusType.Charging)
                {

                    if (_VirtualEVSE.CurrentChargingSession == SessionId)
                    {
                        _VirtualEVSE.CurrentChargingSession = null;
                        return RemoteStopEVSEResult.Success(SessionId, null, ReservationHandling);
                    }

                    else
                        return RemoteStopEVSEResult.InvalidSessionId(SessionId);

                }

                #endregion

                #region OutOfService

                else if (_VirtualEVSE.Status.Value == EVSEStatusType.OutOfService)
                {
                    return RemoteStopEVSEResult.OutOfService(SessionId);
                }

                #endregion

                #region Offline

                else if (_VirtualEVSE.Status.Value == EVSEStatusType.Offline)
                {
                    return RemoteStopEVSEResult.Offline(SessionId);
                }

                #endregion

                else
                    return RemoteStopEVSEResult.Error(SessionId);

            }

            return RemoteStopEVSEResult.UnknownEVSE(SessionId);

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
