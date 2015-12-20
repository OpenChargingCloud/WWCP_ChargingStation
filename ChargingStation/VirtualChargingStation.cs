/*
 * Copyright (c) 2014-2016 GraphDefined GmbH
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





        #region RemoteStart => (...)

        public Task<RemoteStartEVSEResult> RemoteStart(EVSE_Id                 EVSEId,
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
                    return Task.FromResult(RemoteStartEVSEResult.Success(_EVSE.CurrentChargingSession));
                }

                #endregion

                #region Reserved

                else if (_EVSE.Status.Value == EVSEStatusType.Reserved)
                {

                    if (_EVSE.ReservationId == ReservationId)
                    {
                        _EVSE.CurrentChargingSession = ChargingSession_Id.New;
                        return Task.FromResult(RemoteStartEVSEResult.Success(_EVSE.CurrentChargingSession));
                    }

                    else
                        return Task.FromResult(RemoteStartEVSEResult.Reserved);

                }

                #endregion

                #region Charging

                else if (_EVSE.Status.Value == EVSEStatusType.Charging)
                {
                    return Task.FromResult(RemoteStartEVSEResult.AlreadyInUse);
                }

                #endregion

                #region OutOfService

                else if (_EVSE.Status.Value == EVSEStatusType.OutOfService)
                {
                    return Task.FromResult(RemoteStartEVSEResult.OutOfService);
                }

                #endregion

                #region Offline

                else if (_EVSE.Status.Value == EVSEStatusType.Offline)
                {
                    return Task.FromResult(RemoteStartEVSEResult.Offline);
                }

                #endregion

                else
                    return Task.FromResult(RemoteStartEVSEResult.Error());

            }

            return Task.FromResult(RemoteStartEVSEResult.UnknownEVSE);

        }

        #endregion

        #region RemoteStop => (...)

        public Task<RemoteStopEVSEResult> RemoteStop(EVSE_Id              EVSEId,
                                                     ReservationHandling  ReservationHandling,
                                                     ChargingSession_Id   SessionId)
        {

            EVSE _EVSE = null;

            if (_EVSEs.TryGetValue(EVSEId, out _EVSE))
            {

                #region Available

                if (_EVSE.Status.Value == EVSEStatusType.Available)
                {
                    return Task.FromResult(RemoteStopEVSEResult.InvalidSessionId(SessionId));
                }

                #endregion

                #region Reserved

                else if (_EVSE.Status.Value == EVSEStatusType.Reserved)
                {
                    return Task.FromResult(RemoteStopEVSEResult.InvalidSessionId(SessionId));
                }

                #endregion

                #region Charging

                else if (_EVSE.Status.Value == EVSEStatusType.Charging)
                {

                    if (_EVSE.CurrentChargingSession == SessionId)
                    {
                        _EVSE.CurrentChargingSession = null;
                        return Task.FromResult(RemoteStopEVSEResult.Success);
                    }

                    else
                        return Task.FromResult(RemoteStopEVSEResult.InvalidSessionId(SessionId));

                }

                #endregion

                #region OutOfService

                else if (_EVSE.Status.Value == EVSEStatusType.OutOfService)
                {
                    return Task.FromResult(RemoteStopEVSEResult.OutOfService);
                }

                #endregion

                #region Offline

                else if (_EVSE.Status.Value == EVSEStatusType.Offline)
                {
                    return Task.FromResult(RemoteStopEVSEResult.Offline);
                }

                #endregion

                else
                    return Task.FromResult(RemoteStopEVSEResult.Error());

            }

            return Task.FromResult(RemoteStopEVSEResult.UnknownEVSE);

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


        #region AuthRFID(UID)

        public Boolean AuthRFID(String UID)
        {
            return false;
        }

        #endregion

    }

}
