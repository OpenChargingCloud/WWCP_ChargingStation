/*
 * Copyright (c) 2014-2018 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// A demo implementation of a remote charging station.
    /// </summary>
    public class RemoteChargingStation
    {

        #region Data

        private        readonly TCPClient  _TCPClient;

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

        private ChargingStationStatusTypes _Status;

        public ChargingStationStatusTypes Status
        {
            get
            {
                return _Status;
            }
        }

        #endregion

        #region EVSEs

        private readonly ConcurrentDictionary<EVSE_Id, RemoteEVSE> _EVSEs;

        public IEnumerable<RemoteEVSE> EVSEs
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

        internal readonly IVotingNotificator<DateTime, RemoteChargingStation, RemoteEVSE, Boolean> EVSEAddition;

        /// <summary>
        /// Called whenever an EVSE will be or was added.
        /// </summary>
        public IVotingSender<DateTime, RemoteChargingStation, RemoteEVSE, Boolean> OnEVSEAddition
        {
            get
            {
                return EVSEAddition;
            }
        }

        #endregion

        #endregion

        #region Constructor(s)

        #region (private) RemoteChargingStation()

        private RemoteChargingStation()
        {

            this._EVSEs                     = new ConcurrentDictionary<EVSE_Id, RemoteEVSE>();

            #region Init events

            // ChargingStation events
            this.EVSEAddition               = new VotingNotificator<DateTime, RemoteChargingStation, RemoteEVSE, Boolean>(() => new VetoVote(), true);
          //  this.EVSERemoval                = new VotingNotificator<DateTime, ChargingStation, EVSE, Boolean>(() => new VetoVote(), true);

          //  // EVSE events
          //  this.SocketOutletAddition       = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);
          //  this.SocketOutletRemoval        = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);

            #endregion

        }

        #endregion

        #region RemoteChargingStation(Id, EVSEOperatorDNS = null, EVSEOperatorTimeout = default, EVSEOperatorTimeout = null, DNSClient = null, AutoConnect = false)

        /// <summary>
        /// A virtual WWCP charging station.
        /// </summary>
        /// <param name="Id">The unique identifier of the charging station.</param>
        /// <param name="EVSEOperatorDNS">The optional DNS name of the Charging Station Operator backend to connect to.</param>
        /// <param name="UseIPv4">Whether to use IPv4 as networking protocol.</param>
        /// <param name="UseIPv6">Whether to use IPv6 as networking protocol.</param>
        /// <param name="PreferIPv6">Prefer IPv6 (instead of IPv4) as networking protocol.</param>
        /// <param name="EVSEOperatorTimeout">The timeout connecting to the Charging Station Operator backend.</param>
        /// <param name="DNSClient">An optional DNS client used to resolve DNS names.</param>
        /// <param name="AutoConnect">Connect to the Charging Station Operator backend automatically on startup. Default is false.</param>
        public RemoteChargingStation(ChargingStation_Id  Id,
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
            this._Status     = ChargingStationStatusTypes.Offline;

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

            this._DNSClient = DNSClient;

           // if (AutoConnect)
           //     Connect();

        }

        #endregion

        #endregion



        #region Connect()

        /// <summary>
        /// Connect to the given Charging Station Operator backend.
        /// </summary>
        public TCPConnectResult Connect()
        {
            return _TCPClient.Connect();
        }

        #endregion

        #region Disconnect()

        /// <summary>
        /// Disconnect from the given Charging Station Operator backend.
        /// </summary>
        public TCPDisconnectResult Disconnect()
        {
            return _TCPClient.Disconnect();
        }

        #endregion


    }

}
