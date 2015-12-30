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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.Sockets.TCP;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using System.Threading;

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

        private Dictionary<EVSE_Id, EVSE> _EVSEs;

        public IEnumerable<EVSE> EVSEs
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

        #endregion

        #region Constructor(s)

        #region VirtualChargingStation(ChargingStation)

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
            this._Status           = ChargingStationStatusType.Offline;

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
        {

            if (Id == null)
                throw new ArgumentNullException("Id", "The charging station identifier must not be null!");

            this._Id         = Id;
            this._Status      = ChargingStationStatusType.Offline;

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


        #region Reserve(Timestamp, CancellationToken, 

        public async Task<ReservationResult> Reserve(DateTime                Timestamp,
                                                     CancellationToken       CancellationToken,
                                                     EVSP_Id                 ProviderId,
                                                     ChargingReservation_Id  ReservationId,
                                                     DateTime?               StartTime,
                                                     TimeSpan?               Duration,
                                                     ChargingProduct_Id      ChargingProductId  = null,
                                                     IEnumerable<Auth_Token> RFIDIds            = null,
                                                     IEnumerable<eMA_Id>     eMAIds             = null,
                                                     IEnumerable<UInt32>     PINs               = null)
        {

            return ReservationResult.OutOfService;

        }

        #endregion

        #region RemoteStart(Timestamp, CancellationToken, ChargingStationId, ChargingProductId, ReservationId, SessionId, eMAId)

        /// <summary>
        /// Initiate a remote start of the given charging session at the given charging station
        /// and for the given provider/eMAId.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of a charging station.</param>
        /// <param name="ChargingProductId">The unique identification of the choosen charging product at the given EVSE.</param>
        /// <param name="ReservationId">The unique identification for a charging reservation.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// <returns>A RemoteStartResult task.</returns>
        public async Task<RemoteStartChargingStationResult> RemoteStart(DateTime                Timestamp,
                                                                        CancellationToken       CancellationToken,
                                                                        ChargingStation_Id      ChargingStationId,
                                                                        ChargingProduct_Id      ChargingProductId,
                                                                        ChargingReservation_Id  ReservationId,
                                                                        ChargingSession_Id      SessionId,
                                                                        eMA_Id                  eMAId)
        {

            return RemoteStartChargingStationResult.OutOfService;

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
                                                             EVSE_Id                 EVSEId,
                                                             ChargingProduct_Id      ChargingProductId,
                                                             ChargingReservation_Id  ReservationId,
                                                             ChargingSession_Id      SessionId,
                                                             eMA_Id                  eMAId)
        {

            // SessionId_AlreadyInUse,
            // EVSE_NotReachable,
            // Start_Timeout

            EVSE _EVSE = null;

            if (_EVSEs.TryGetValue(EVSEId, out _EVSE))
            {

                #region Available

                if (_EVSE.Status.Value == EVSEStatusType.Available)
                {
                    _EVSE.CurrentChargingSession = ChargingSession_Id.New;
                    return RemoteStartEVSEResult.Success(_EVSE.CurrentChargingSession);
                }

                #endregion

                #region Reserved

                else if (_EVSE.Status.Value == EVSEStatusType.Reserved)
                {

                    if (_EVSE.ReservationId == ReservationId)
                    {
                        _EVSE.CurrentChargingSession = ChargingSession_Id.New;
                        return RemoteStartEVSEResult.Success(_EVSE.CurrentChargingSession);
                    }

                    else
                        return RemoteStartEVSEResult.Reserved;

                }

                #endregion

                #region Charging

                else if (_EVSE.Status.Value == EVSEStatusType.Charging)
                {
                    return RemoteStartEVSEResult.AlreadyInUse;
                }

                #endregion

                #region OutOfService

                else if (_EVSE.Status.Value == EVSEStatusType.OutOfService)
                {
                    return RemoteStartEVSEResult.OutOfService;
                }

                #endregion

                #region Offline

                else if (_EVSE.Status.Value == EVSEStatusType.Offline)
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

        #region RemoteStop(Timestamp, CancellationToken, ChargingStationId, ReservationHandling, SessionId)

        /// <summary>
        /// Initiate a remote stop of the given charging session at the given charging station.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of a charging station.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <returns>A RemoteStopResult task.</returns>
        public async Task<RemoteStopChargingStationResult> RemoteStop(DateTime             Timestamp,
                                                                      CancellationToken    CancellationToken,
                                                                      ChargingStation_Id   ChargingStationId,
                                                                      ReservationHandling  ReservationHandling,
                                                                      ChargingSession_Id   SessionId)
        {

            return RemoteStopChargingStationResult.OutOfService;

        }

        #endregion

        #region RemoteStop(Timestamp, CancellationToken, EVSEId, ReservationHandling, SessionId)

        /// <summary>
        /// Initiate a remote stop of the given charging session at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <returns>A RemoteStopResult task.</returns>
        public async Task<RemoteStopEVSEResult> RemoteStop(DateTime             Timestamp,
                                                           CancellationToken    CancellationToken,
                                                           EVSE_Id              EVSEId,
                                                           ReservationHandling  ReservationHandling,
                                                           ChargingSession_Id   SessionId)
        {

            EVSE _EVSE = null;

            if (_EVSEs.TryGetValue(EVSEId, out _EVSE))
            {

                #region Available

                if (_EVSE.Status.Value == EVSEStatusType.Available)
                {
                    return RemoteStopEVSEResult.InvalidSessionId(SessionId);
                }

                #endregion

                #region Reserved

                else if (_EVSE.Status.Value == EVSEStatusType.Reserved)
                {
                    return RemoteStopEVSEResult.InvalidSessionId(SessionId);
                }

                #endregion

                #region Charging

                else if (_EVSE.Status.Value == EVSEStatusType.Charging)
                {

                    if (_EVSE.CurrentChargingSession == SessionId)
                    {
                        _EVSE.CurrentChargingSession = null;
                        return RemoteStopEVSEResult.Success;
                    }

                    else
                        return RemoteStopEVSEResult.InvalidSessionId(SessionId);

                }

                #endregion

                #region OutOfService

                else if (_EVSE.Status.Value == EVSEStatusType.OutOfService)
                {
                    return RemoteStopEVSEResult.OutOfService;
                }

                #endregion

                #region Offline

                else if (_EVSE.Status.Value == EVSEStatusType.Offline)
                {
                    return RemoteStopEVSEResult.Offline;
                }

                #endregion

                else
                    return RemoteStopEVSEResult.Error();

            }

            return RemoteStopEVSEResult.UnknownEVSE;

        }

        #endregion


        #region AuthenticateToken(AuthToken)

        public Boolean AuthenticateToken(Auth_Token AuthToken)
        {
            return false;
        }

        #endregion


        #region Connect()

        /// <summary>
        /// Connect to the given EVSE operator backend.
        /// </summary>
        public TCPConnectResult Connect()
        {
            return _TCPClient.Connect();
        }

        #endregion

        #region Disconnect()

        /// <summary>
        /// Disconnect from the given EVSE operator backend.
        /// </summary>
        public TCPDisconnectResult Disconnect()
        {
            return _TCPClient.Disconnect();
        }

        #endregion


    }

}
