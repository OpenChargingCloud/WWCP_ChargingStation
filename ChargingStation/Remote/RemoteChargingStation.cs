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
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// A demo implementation of a remote charging station.
    /// </summary>
    public class RemoteChargingStation : IRemoteChargingStation
    {

        #region Data

        private        readonly TCPClient  _TCPClient;

        private const           String     BDLive_Hostname     = "hq.lemonage.de";
        private const           String     BDLive_VirtualHost  = BDLive_Hostname;
        private static readonly IPPort     BDLive_IPPort       = new IPPort(20081);
        private const           String     BDLive_URIPrefix    = "/ps/rest/ext/BoschEBike";

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

        #region RemoteChargingStation(ChargingStation)

        /// <summary>
        /// A virtual WWCP charging station.
        /// </summary>
        /// <param name="ChargingStation">A local charging station.</param>
        public RemoteChargingStation(ChargingStation  ChargingStation)
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

        #region RemoteChargingStation(Id, EVSEOperatorDNS = null, EVSEOperatorTimeout = default, EVSEOperatorTimeout = null, DNSClient = null, AutoConnect = false)

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

            this._DNSClient = DNSClient;

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
        public RemoteEVSE CreateNewEVSE(EVSE_Id                                 EVSEId,
                                        Action<RemoteEVSE>                      Configurator  = null,
                                        Action<RemoteEVSE>                      OnSuccess     = null,
                                        Action<RemoteChargingStation, EVSE_Id>  OnError       = null)
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
            var _EVSE = new RemoteEVSE(EVSEId, this);

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

            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

            EVSEId = EVSE_Id.Parse("+49*822*483*1");

            try
            {

                var httpresult = await new HTTPClient(BDLive_Hostname, BDLive_IPPort, false, _DNSClient).

                                           Execute(client => client.POST(BDLive_URIPrefix + "/EVSEs/" + EVSEId.ToFormat(IdFormatType.OLD).Replace("+", ""),

                                                                         requestbuilder => {
                                                                             requestbuilder.Host         = BDLive_VirtualHost;
                                                                             requestbuilder.ContentType  = HTTPContentType.JSON_UTF8;
                                                                             requestbuilder.Content      = JSONObject.Create(
                                                                                                               ChargingProductId != null
                                                                                                                   ? new JProperty("ChargingProductId",  ChargingProductId.ToString())
                                                                                                                   : null,
                                                                                                               ReservationId     != null
                                                                                                                   ? new JProperty("ReservationId",      ReservationId.    ToString())
                                                                                                                   : null,
                                                                                                               SessionId         != null
                                                                                                                   ? new JProperty("SessionId",          SessionId.        ToString())
                                                                                                                   : null,
                                                                                                               ProviderId        != null
                                                                                                                   ? new JProperty("ProviderId",         ProviderId.       ToString())
                                                                                                                   : null,
                                                                                                               eMAId             != null
                                                                                                                   ? new JProperty("eMAId",              eMAId.            ToString())
                                                                                                                   : null
                                                                                                           ).ToUTF8Bytes();
                                                                             requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                         }),

                                                    QueryTimeout.HasValue ? QueryTimeout : TimeSpan.FromSeconds(60),
                                                    CancellationToken);


                var result = RemoteStartEVSEResult.Error();

                #region HTTPStatusCode.OK

                if (httpresult.HTTPStatusCode == HTTPStatusCode.OK)
                {

                    // HTTP/1.1 200 OK
                    // Date: Fri, 28 Mar 2014 13:31:27 GMT
                    // Server: Apache/2.2.9 (Debian) mod_jk/1.2.26
                    // Content-Length: 34
                    // Content-Type: application/json
                    // 
                    // {
                    //   "code" : "EVSE_AlreadyInUse"
                    // }

                    JObject JSONResponse = null;

                    try
                    {

                        JSONResponse = JObject.Parse(httpresult.HTTPBody.ToUTF8String());

                    }
                    catch (Exception e)
                    {
                        DebugX.LogT("Belectric REMOTESTART response JSON could not be parsed! " + e.Message + " // " + httpresult.EntirePDU.ToString());
                        throw new Exception("Belectric REMOTESTART response JSON could not be parsed: " + e.Message);
                    }

                    switch (JSONResponse["code"].ToString())
                    {

                        case "EVSE_AlreadyInUse":
                            result = RemoteStartEVSEResult.AlreadyInUse;
                            break;

                        case "SessionId_AlreadyInUse":
                            result = RemoteStartEVSEResult.InvalidSessionId;
                            break;

                        case "EVSE_Unknown":
                            result = RemoteStartEVSEResult.UnknownEVSE;
                            break;

                        case "EVSE_NotReachable":
                            result = RemoteStartEVSEResult.Offline;
                            break;

                        case "Start_Timeout":
                            result = RemoteStartEVSEResult.Timeout;
                            break;

                        case "Success":
                            result = RemoteStartEVSEResult.Success(SessionId);
                            break;

                        default:
                            result = RemoteStartEVSEResult.Error();
                            break;

                    }

                }

                #endregion

                return result;

            }

            catch (Exception e)
            {
                return RemoteStartEVSEResult.Error(e.Message);
            }

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

            var RoamingNetworkId = RoamingNetwork_Id.Parse("Prod");

            #region Initial checks

            if (RoamingNetworkId == null)
                throw new ArgumentNullException("RoamingNetworkId", "The given parameter must not be null!");

            if (SessionId == null)
                throw new ArgumentNullException("SessionId", "The given parameter must not be null!");

            if (ProviderId == null)
                ProviderId = EVSP_Id.Parse("DE*BSI");

            if (EVSEId == null)
                throw new ArgumentNullException("EVSEId", "The given parameter must not be null!");

            #endregion

            DebugX.LogT("Belectric REMOTESTOP in " + RoamingNetworkId + " at " + EVSEId);
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

            try
            {

                #region Upstream HTTP request...

                var httpresult = await new HTTPClient(BDLive_Hostname, BDLive_IPPort, false, DNSClient).

                                           Execute(client => client.CreateRequest(new HTTPMethod("REMOTESTOP"),
                                                                                  "/ps/rest/hubject/RNs/" + RoamingNetworkId.ToString() + "/EVSEs/" + EVSEId.ToFormat(IdFormatType.OLD).Replace("+", ""),

                                                                               requestbuilder => {
                                                                                   requestbuilder.Host         = BDLive_VirtualHost;
                                                                                   requestbuilder.ContentType  = HTTPContentType.JSON_UTF8;
                                                                                   requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                                   requestbuilder.Content      = new JObject(
                                                                                                                           new JProperty("@id",         SessionId.ToString()),
                                                                                                                           new JProperty("ProviderId",  ProviderId.ToString())
                                                                                                                      ).ToString().
                                                                                                                        ToUTF8Bytes();
                                                                               }),

                                                             Timeout:            QueryTimeout.HasValue ? QueryTimeout : TimeSpan.FromSeconds(180),
                                                             CancellationToken:  CancellationToken);

                #endregion

                DebugX.LogT("Belectric REMOTESTOP response: '" + httpresult.HTTPStatusCode.ToString() + " / " +
                                                                 httpresult.HTTPBody.ToUTF8String() + "'");

                var result       = RemoteStopEVSEResult.Error(SessionId);

                #region HTTPStatusCode.OK

                if (httpresult.HTTPStatusCode == HTTPStatusCode.OK)
                {

                    // HTTP/1.1 200 OK
                    // Date: Fri, 28 Mar 2014 13:31:27 GMT
                    // Server: Apache/2.2.9 (Debian) mod_jk/1.2.26
                    // Content-Length: 34
                    // Content-Type: application/json
                    // 
                    // {
                    //   "code" : "EVSE_AlreadyInUse"
                    // }

                    JObject JSONResponse = null;

                    try
                    {

                        JSONResponse = JObject.Parse(httpresult.HTTPBody.ToUTF8String());

                    }
                    catch (Exception e)
                    {
                        DebugX.LogT("Belectric REMOTESTOP response JSON could not be parsed! " + e.Message + " // " + httpresult.EntirePDU.ToString());
                        throw new Exception("Belectric REMOTESTOP response JSON could not be parsed: " + e.Message);
                    }

                    switch (JSONResponse["code"].ToString())
                    {

                        case "SessionId_Unknown":
                            result = RemoteStopEVSEResult.InvalidSessionId(SessionId);
                            break;

                        case "EVSE_NotReachable":
                            result = RemoteStopEVSEResult.Offline(SessionId);
                            break;

                        case "Timeout":
                            result = RemoteStopEVSEResult.Timeout(SessionId);
                            break;

                        case "EVSE_unknown":
                            result = RemoteStopEVSEResult.UnknownEVSE(SessionId);
                            break;

                        case "EVSE_is_out_of_service":
                            result = RemoteStopEVSEResult.OutOfService(SessionId);
                            break;

                        case "Success":
                            result = RemoteStopEVSEResult.Success(SessionId);
                            break;

                        default:
                            result = RemoteStopEVSEResult.Error(SessionId);
                            break;

                    }

                }

                #endregion

                return result;

            }

            catch (Exception e)
            {
                return RemoteStopEVSEResult.Error(SessionId, e.Message);
            }

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
