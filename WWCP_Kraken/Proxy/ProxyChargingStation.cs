/*
 * Copyright (c) 2014-2016 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of Bosch E-Bike API
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
using System.Net.Security;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using org.GraphDefined.WWCP;
using org.GraphDefined.WWCP.ChargingStations;
using System.IO;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// A Bosch E-Bike charging station.
    /// </summary>
    public class ProxyChargingStation : NetworkChargingStationStub
    {

        #region Data

        public const           String    DefaultHostname                 = "ahzf.de";
        public static readonly IPPort    DefaultTCPPort                  = new IPPort(3004);
        public const           String    DefaultVirtualHost              = DefaultHostname;
        public const           String    DefaultURIPrefix                = "/ext/BoschEBike";
        public const           String    HTTPLogin                       = "boschsi";
        public const           String    HTTPPassword                    = "fad/09q23w!rf";
        public static readonly TimeSpan  DefaultQueryTimeout             = TimeSpan.FromSeconds(180);

        private readonly Timer EVSEStatusImportTimer;

        public static readonly TimeSpan  BoschEBikeImportTimerIntervall  = TimeSpan.FromSeconds(5);

        private static Object UpdateEVSEDataAndStatusLock = new Object();

        #endregion

        #region Properties

        #region RemoteWhiteList

        private readonly HashSet<AuthInfo> _RemoteWhiteList;

        public HashSet<AuthInfo> RemoteWhiteList
        {
            get
            {
                return _RemoteWhiteList;
            }
        }

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a Bosch E-Bike charging station.
        /// </summary>
        /// <param name="ChargingStation">A local charging station.</param>
        /// <param name="DNSClient">An optional DNS client used to resolve DNS names.</param>
        public ProxyChargingStation(ChargingStation                      ChargingStation,
                                    TimeSpan?                            SelfCheckTimeSpan           = null,
                                    UInt16                               MaxStatusListSize           = DefaultMaxStatusListSize,
                                    UInt16                               MaxAdminStatusListSize      = DefaultMaxAdminStatusListSize,
                                    IPTransport                          IPTransport                 = IPTransport.IPv4only,
                                    DNSClient                            DNSClient                   = null,
                                    String                               Hostname                    = DefaultHostname,
                                    IPPort                               TCPPort                     = null,
                                    String                               Service                     = null,
                                    RemoteCertificateValidationCallback  RemoteCertificateValidator  = null,
                                    String                               VirtualHost                 = DefaultVirtualHost,
                                    String                               URIPrefix                   = DefaultURIPrefix,
                                    TimeSpan?                            QueryTimeout                = null)

            : base(ChargingStation,
                   SelfCheckTimeSpan,
                   MaxStatusListSize,
                   MaxAdminStatusListSize,
                   IPTransport,
                   DNSClient != null              ? DNSClient    : new DNSClient(SearchForIPv4DNSServers: true,
                                                                                 SearchForIPv6DNSServers: false),
                   Hostname.IsNotNullOrEmpty()    ? Hostname     : DefaultHostname,
                   TCPPort   != null              ? TCPPort      : DefaultTCPPort,
                   Service,
                   RemoteCertificateValidator,
                   VirtualHost.IsNotNullOrEmpty() ? VirtualHost  : DefaultVirtualHost,
                   URIPrefix.  IsNotNullOrEmpty() ? URIPrefix    : DefaultURIPrefix,
                   QueryTimeout.HasValue          ? QueryTimeout : DefaultQueryTimeout)

        {

            EVSEStatusImportTimer  = new Timer(EVSEStatusImporter, null, BoschEBikeImportTimerIntervall, BoschEBikeImportTimerIntervall);

            #region Download white list

            _RemoteWhiteList = new HashSet<AuthInfo>();

            // Will put the results into _RemoteWhiteList!
            GetWhiteList(DateTime.Now, new CancellationTokenSource().Token, EventTracking_Id.New).Wait();

            #endregion

        }

        #endregion



        private void EVSEStatusImporter(Object State)
        {

            if (Monitor.TryEnter(UpdateEVSEDataAndStatusLock))
            {

                try
                {

                    var EVSEStatus = GetEVSEStatus(DateTime.Now,
                                                   new CancellationTokenSource().Token,
                                                   EventTracking_Id.New).Result;

                    if (EVSEStatus != null)
                    {
                        foreach (var evsestatus in EVSEStatus)
                        {

                            var EVSE = _EVSEs.Where(evse => evse.Id == evsestatus.Id).FirstOrDefault();

                            if (EVSE != null)
                                EVSE.Status = evsestatus.Status;

                        }
                    }

                }
                catch (Exception e)
                {

                    while (e.InnerException != null)
                        e = e.InnerException;

                    DebugX.LogT("BoschEBike importer led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);

                }

                finally
                {
                    Monitor.Exit(UpdateEVSEDataAndStatusLock);
                }

            }

        }



        // Lemonage:
        // {
        //     Sessions: [
        //         {
        //             ChargingSessionId: "4cc38923-06b8-4a6f-bf4b-10223ef1e200",
        //             EVSEId: "49*822*483*1",
        //             SessionStart: "2016-02-26T14:55:27+01:00"
        //         }
        //     ]
        // }


        #region GetEVSEStatus

        public override async Task<IEnumerable<EVSEStatus>>

            GetEVSEStatus(DateTime                 Timestamp,
                          CancellationToken        CancellationToken,
                          EventTracking_Id         EventTrackingId,
                          TimeSpan?                QueryTimeout = null)

        {

            var response = await new HTTPClient(Hostname,
                                                TCPPort,
                                                _RemoteCertificateValidator,
                                                DNSClient).

                                     Execute(client => client.GET(URIPrefix + "/EVSEStatus",

                                                                   requestbuilder => {
                                                                       requestbuilder.Host         = VirtualHost;
                                                                       requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                       requestbuilder.ContentType  = HTTPContentType.JSON_UTF8;
                                                                   }),

                                             QueryTimeout.HasValue ? QueryTimeout : DefaultQueryTimeout,
                                             CancellationToken);

            if (response == null)
                return new EVSEStatus[0];

            // HTTP/1.1 200 OK
            // Date: Tue, 01 Mar 2016 20:39:24 GMT
            // Server: Belectric Drive API
            // Access-Control-Allow-Origin: *
            // Access-Control-Allow-Methods: GET
            // Access-Control-Allow-Headers: Content-Type, Authorization
            // ETag: 1
            // Content-Type: application/json; charset=utf-8
            // Content-Length: 1027
            // X-ExpectedTotalNumberOfItems: 12
            // 
            // {
            //   "DE*822*E22300735816*1": {
            //     "2016-03-01T17:00:03.782Z": "Available"
            //   },
            //   "DE*822*E22300735816*2": {
            //     "2016-03-01T17:00:08.782Z": "AVAILABLE_DOOR_NOT_CLOSED"
            //   },
            //   "DE*822*E22300735816*3": {
            //     "2016-03-01T17:00:03.785Z": "Available"
            //   },
            //   "DE*822*E22300735816*4": {
            //     "2016-03-01T17:00:03.787Z": "Available"
            //   },
            //   "DE*822*EVSE*BOSCHEBIKE*LIVE*1": {
            //     "2016-03-01T17:00:03.642Z": "Available"
            //   },
            //   "DE*822*EVSE*BOSCHEBIKE*LIVE*2": {
            //     "2016-03-01T17:00:03.643Z": "OutOfService"
            //   },
            //   "DE*822*EVSE*BOSCHEBIKE*LIVE*3": {
            //     "2016-03-01T17:00:03.644Z": "OutOfService"
            //   },
            //   "DE*822*EVSE*BOSCHEBIKE*LIVE*4": {
            //     "2016-03-01T17:00:03.645Z": "OutOfService"
            //   },
            //   "DE*822*EVSE*BOSCHEBIKE*TEST*1": {
            //     "2016-03-01T17:00:03.648Z": "Available"
            //   },
            //   "DE*822*EVSE*BOSCHEBIKE*TEST*2": {
            //     "2016-03-01T17:00:03.650Z": "Available"
            //   },
            //   "DE*822*EVSE*BOSCHEBIKE*TEST*3": {
            //     "2016-03-01T17:00:03.651Z": "Available"
            //   },
            //   "DE*822*EVSE*BOSCHEBIKE*TEST*4": {
            //     "2016-03-01T17:00:03.653Z": "Available"
            //   }
            // }

            if (response.HTTPStatusCode != HTTPStatusCode.OK)
                return new EVSEStatus[0];

            var List = new List<EVSEStatus>();

            foreach (var property in JObject.Parse(response.HTTPBody.ToUTF8String()) as JObject)
            {

                try
                {

                    var EVSEIdString = property.Key.Replace(@"""", "");

                    if (EVSEIdString.StartsWith(RemoteEVSEIdPrefix.IsNotNullOrEmpty()
                                                     ? RemoteEVSEIdPrefix
                                                     : Id.ToString()))
                    {

                        List.Add(new EVSEStatus(MapIncomingId(EVSE_Id.Parse(property.Key.Replace(@"""", ""))),
                                                (EVSEStatusType) Enum.Parse(typeof(EVSEStatusType), ((property.Value as JObject).First as JProperty).Value.Value<String>(), true)));

                    }

                }
                catch (Exception e)
                { }

            }

            return List;

        }

        #endregion


        #region GetReservations

        public async Task<IEnumerable<ChargingReservation>>

            GetReservations(DateTime                 Timestamp,
                            CancellationToken        CancellationToken,
                            EventTracking_Id         EventTrackingId,
                            TimeSpan?                QueryTimeout  = null)

        {

            var response = await new HTTPClient(Hostname,
                                                TCPPort,
                                                _RemoteCertificateValidator,
                                                DNSClient).

                                     Execute(client => client.GET(URIPrefix + "/Reservations",

                                                                  requestbuilder => {
                                                                      requestbuilder.Host         = VirtualHost;
                                                                      requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                      requestbuilder.ContentType  = HTTPContentType.JSON_UTF8;
                                                                  }),

                                             QueryTimeout.HasValue ? QueryTimeout : DefaultQueryTimeout,
                                             CancellationToken);

            // HTTP / 1.1 200 OK
            // Date: Thu, 11 Feb 2016 11:53:43 GMT
            // Server: Apache / 2.2.16(Debian)
            // Content - Length: 197
            // Content - Type: application / json
            // 
            // {
            //     "ReservationId":      "33983d9f-cec3-4da7-894a-c805a55eb897",
            //     "ChargingStationId":  "483",
            //     "EVSEId":             "49*822*483*1",
            //     "StartTime":          "2016-02-11T12:52:33CET",
            //     "Duration":           900,
            //     "AuthorizedIds": {
            //          "RFID_CARD":     [ "1234" ]
            //     }
            // }

            if (response.HTTPStatusCode == HTTPStatusCode.NoContent)
                return new ChargingReservation[0];

            JArray  _JSONArray  = null;
            JObject _JSONObject = null;

            if (response.ContentLength > 0)
            {

                try
                {
                    _JSONArray = JArray.Parse(response.HTTPBody.ToUTF8String());
                }
                catch (Exception)
                {
                    _JSONObject = JObject.Parse(response.HTTPBody.ToUTF8String());
                }

            }

            if (response.HTTPStatusCode == HTTPStatusCode.OK)
            {

                if (_JSONArray != null)
                    return _JSONArray.AsEnumerable().Select(jsonarray =>
                                   new ChargingReservation(
                                       ChargingReservation_Id.Parse(jsonarray["ReservationId"].Value<String>()),
                                       DateTime.Now,
                                       jsonarray["StartTime"].Value<DateTime>(),
                                       TimeSpan.FromSeconds(jsonarray["Duration"].Value<Int32>()),
                                       DateTime.Now + TimeSpan.FromSeconds(jsonarray["Duration"].Value<Int32>()),
                                       jsonarray["ConsumedReservationTime"] != null ? TimeSpan.FromSeconds(jsonarray["ConsumedReservationTime"].Value<UInt32>()) : TimeSpan.FromSeconds(0),
                                       ChargingReservationLevel.EVSE,
                                       EVSP_Id.Parse("DE*GEF"),
                                       null,
                                       this.ChargingStation.Operator.RoamingNetwork,
                                       EVSEId: EVSE_Id.Parse(jsonarray["EVSEId"].Value<String>()),
                                       PINs:   new UInt32[] { UInt32.Parse(jsonarray["pin"].Value<String>()) }
                                  ));

                if (_JSONObject != null)
                    return new ChargingReservation[] {
                                   new ChargingReservation(
                                       ChargingReservation_Id.Parse(_JSONObject["ReservationId"].Value<String>()),
                                       DateTime.Now,
                                       _JSONObject["StartTime"].Value<DateTime>(),
                                       TimeSpan.FromSeconds(_JSONObject["Duration"].Value<Int32>()),
                                       DateTime.Now + TimeSpan.FromSeconds(_JSONObject["Duration"].Value<Int32>()),
                                       _JSONObject["ConsumedReservationTime"] != null ? TimeSpan.FromSeconds(_JSONObject["ConsumedReservationTime"].Value<UInt32>()) : TimeSpan.FromSeconds(0),
                                       ChargingReservationLevel.EVSE,
                                       EVSP_Id.Parse("DE*GEF"),
                                       null,
                                       this.ChargingStation.Operator.RoamingNetwork,
                                       EVSEId: EVSE_Id.Parse(_JSONObject["EVSEId"].Value<String>()),
                                       PINs:   new UInt32[] { UInt32.Parse(_JSONObject["pin"].Value<String>()) }
                                  )};

            }

            return new ChargingReservation[0];

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
        /// <param name="eMAId">An optional unique identification of e-Mobility account/customer requesting this reservation.</param>
        /// <param name="ChargingProductId">An optional unique identification of the charging product to be reserved.</param>
        /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
        /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
        /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public override async Task<ReservationResult>

            Reserve(DateTime                 Timestamp,
                    CancellationToken        CancellationToken,
                    EventTracking_Id         EventTrackingId,
                    EVSE_Id                  EVSEId,
                    DateTime?                StartTime,
                    TimeSpan?                Duration,
                    ChargingReservation_Id   ReservationId      = null,
                    EVSP_Id                  ProviderId         = null,
                    eMA_Id                   eMAId              = null,
                    ChargingProduct_Id       ChargingProductId  = null,
                    IEnumerable<Auth_Token>  AuthTokens         = null,
                    IEnumerable<eMA_Id>      eMAIds             = null,
                    IEnumerable<UInt32>      PINs               = null,
                    TimeSpan?                QueryTimeout       = null)

        {


            #region Initial checks

            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

            //if (ReservationId == null)
            //    throw new ArgumentNullException("ReservationId",  "The given unique reservation identification must not be null!");

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId),         "The given unique EVSE identification must not be null!");

            if (!Duration.HasValue)
                Duration = TimeSpan.FromMinutes(15);

            #endregion


            var response = await new HTTPClient(Hostname,
                                                TCPPort,
                                                _RemoteCertificateValidator,
                                                DNSClient).

                                     Execute(client => client.POST(URIPrefix + "/EVSEs/" + MapOutgoingId(EVSEId) + "/Reservation",

                                                                   requestbuilder => {
                                                                       requestbuilder.Host           = VirtualHost;
                                                                       requestbuilder.Authorization  = new HTTPBasicAuthentication(HTTPLogin, HTTPPassword);
                                                                       requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                       requestbuilder.ContentType    = HTTPContentType.JSON_UTF8;
                                                                       requestbuilder.Content        = JSONObject.Create(
                                                                                                           Duration.HasValue
                                                                                                               ? new JProperty("Duration",           (Int32) Duration.Value.TotalSeconds)
                                                                                                               : null,
                                                                                                           ChargingProductId != null
                                                                                                               ? new JProperty("ChargingProductId",  ChargingProductId.ToString())
                                                                                                               : null,
                                                                                                           ReservationId     != null
                                                                                                               ? new JProperty("ReservationId",      ReservationId.    ToString())
                                                                                                               : null,
                                                                                                           ProviderId        != null
                                                                                                               ? new JProperty("ProviderId",         ProviderId.       ToString())
                                                                                                               : null,
                                                                                                           eMAId             != null
                                                                                                               ? new JProperty("eMAId",              eMAId.            ToString())
                                                                                                               : null,
                                                                                                           AuthTokens.NotNullAny()
                                                                                                               ? new JProperty("AuthorizedIds",
                                                                                                                     JSONObject.Create(
                                                                                                                         new JProperty("RFIDIds", new JArray(AuthTokens.SafeSelect(token => token.ToString()))),
                                                                                                                         new JProperty("eMAIds",  new JArray(eMAIds.    SafeSelect(emaid => emaid.ToString())))
                                                                                                                     )
                                                                                                                 )
                                                                                                               : null
                                                                                                       ).ToUTF8Bytes();
                                                                   }),

                                             QueryTimeout.HasValue ? QueryTimeout : DefaultQueryTimeout,
                                             CancellationToken);

            if (response == null)
            {

                using (var logfile = File.AppendText("BoschEBike_2lemonage_" + EVSEId.ToString().Replace("*", "_") + "_ReserveEVSE.log"))
                {
                    logfile.WriteLine(DateTime.Now);
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                    logfile.WriteLine("timeout...");
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                }

                return ReservationResult.Timeout;

            }

            using (var logfile = File.AppendText("BoschEBike_2lemonage_" + EVSEId.ToString().Replace("*", "_") + "_ReserveEVSE.log"))
            {
                logfile.WriteLine(DateTime.Now);
                logfile.WriteLine(response.HTTPRequest.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
                logfile.WriteLine(response.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
            }

            JObject JSON       = null;
            String  ErrorCode  = null;

            if (response.ContentLength > 0)
            {
                JSON       = JObject.Parse(response.HTTPBody.ToUTF8String());
                ErrorCode  = JSON["errorCode"] != null ? JSON["errorCode"].Value<String>() : null;
            }

            #region OK

            // HTTP/1.1 200 OK
            // Date: Thu, 11 Feb 2016 11:52:31 GMT
            // Server: Apache/2.2.16(Debian)
            // Content-Length: 187
            // Content-Type: application/json
            // 
            // {
            //     "evseId":        "49*822*483*1",
            //     "pin":           "538753",
            //     "ReservationId": "33983d9f-cec3-4da7-894a-c805a55eb897",
            //     "StartTime":     "2016-02-11T12:52:33CET",
            //     "Duration":      900,
            //     "State":         "CREATED",
            //     "errorCode":     "SUCCESS"
            // }

            if (response.HTTPStatusCode == HTTPStatusCode.OK &&
                (ErrorCode              == "SUCCESS" ||
                 ErrorCode              == "SUCCESS_UPDATED"))
            {

                var NewReservation = new ChargingReservation(
                                         ChargingReservation_Id.Parse(JSON["ReservationId"].Value<String>()),
                                         DateTime.Now,
                                         JSON["StartTime"].Value<DateTime>(),
                                         TimeSpan.FromSeconds(JSON["Duration"].Value<Int32>()),
                                         JSON["StartTime"].Value<DateTime>() + TimeSpan.FromSeconds(JSON["Duration"].Value<Int32>()),
                                         JSON["ConsumedReservationTime"] != null ? TimeSpan.FromSeconds(JSON["ConsumedReservationTime"].Value<UInt32>()) : TimeSpan.FromSeconds(0),
                                         ChargingReservationLevel.EVSE,
                                         ProviderId,
                                         eMAId,
                                         this.ChargingStation.Operator.RoamingNetwork,
                                         ChargingStationId:  Id,
                                         EVSEId:             EVSEId,
                                         AuthTokens:         AuthTokens,  // As it is not included in the HTTP response!
                                         eMAIds:             eMAIds,      // As it is not included in the HTTP response!
                                         PINs:               JSON["pin"] != null
                                                                 ? new UInt32[] { UInt32.Parse(JSON["pin"].Value<String>()) }
                                                                 : null
                                     );

                SendNewReservation(NewReservation);

                return ReservationResult.Success(NewReservation);

            }

            #endregion

            #region Conflict

            if (response.HTTPStatusCode == HTTPStatusCode.Conflict)
            {

                switch (ErrorCode)
                {

                    #region AlreadyReserved

                    // HTTP/1.1 409 Conflict
                    // Date: Thu, 11 Feb 2016 22:07:33 GMT
                    // Server: Apache/2.2.16 (Debian)
                    // Content-Length: 69
                    // Content-Type: application/json
                    // 
                    // {
                    //     "StartTime":  null,
                    //     "Duration":   0,
                    //     "errorCode":  "RESERVATION_EVSE_IN_USE"
                    // }
                    case "RESERVATION_EVSE_IN_USE":
                        return ReservationResult.AlreadyInUse;

                    #endregion

                    #region Offline

                    // HTTP/1.1 409 Conflict
                    // Date: Tue, 16 Feb 2016 14:39:09 GMT
                    // Server: Apache/2.2.16 (Debian)
                    // Content-Length: 68
                    // Content-Type: application/json
                    // 
                    // {
                    //     "StartTime":  null,
                    //     "Duration":   0,
                    //     "errorCode":  "LOCATION_NOT_REACHABLE"
                    // }
                    case "LOCATION_NOT_REACHABLE":
                        return ReservationResult.Offline;

                    #endregion

                }

            }

            #endregion

            else if (response.HTTPStatusCode == HTTPStatusCode.NotFound)
            {

                // HTTP/1.1 404 Not Found
                // Date: Tue, 01 Mar 2016 00:32:18 GMT
                // Server: Apache/2.2.16 (Debian)
                // Transfer-Encoding: chunked
                // Content-Type: text/plain; charset=UTF-8

                if (!RemoteWhiteList.Contains(AuthInfo.FromRemoteIdentification(eMAId)))
                    return ReservationResult.InvalidCredentials;

            }

            return ReservationResult.Error(ErrorCode);

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
        /// <param name="eMAId">An optional unique identification of e-Mobility account/customer requesting this reservation.</param>
        /// <param name="ChargingProductId">An optional unique identification of the charging product to be reserved.</param>
        /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
        /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
        /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public override async Task<ReservationResult>

            Reserve(DateTime                 Timestamp,
                    CancellationToken        CancellationToken,
                    EventTracking_Id         EventTrackingId,
                    DateTime?                StartTime,
                    TimeSpan?                Duration,
                    ChargingReservation_Id   ReservationId      = null,
                    EVSP_Id                  ProviderId         = null,
                    eMA_Id                   eMAId              = null,
                    ChargingProduct_Id       ChargingProductId  = null,
                    IEnumerable<Auth_Token>  AuthTokens         = null,
                    IEnumerable<eMA_Id>      eMAIds             = null,
                    IEnumerable<UInt32>      PINs               = null,
                    TimeSpan?                QueryTimeout       = null)

        {


            #region Initial checks

            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

            //if (ReservationId == null)
            //    throw new ArgumentNullException("ReservationId",  "The given unique reservation identification must not be null!");


            #endregion

            var ChargingStationIdLocal = Id;

            if (Id == ChargingStation_Id.Parse("DE*822*STATION*BOSCHEBIKE*LIVE"))
                ChargingStationIdLocal = ChargingStation_Id.Parse("+49*822*483");


            var response = await new HTTPClient(Hostname,
                                                TCPPort,
                                                _RemoteCertificateValidator,
                                                DNSClient).

                                     Execute(client => client.POST(URIPrefix + "/ChargingStations/" + ChargingStationIdLocal.ToFormat(IdFormatType.OLD).Replace("+", "") + "/Reservation",

                                                                   requestbuilder => {
                                                                       requestbuilder.Host           = VirtualHost;
                                                                       requestbuilder.Authorization  = new HTTPBasicAuthentication(HTTPLogin, HTTPPassword);
                                                                       requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                       requestbuilder.ContentType    = HTTPContentType.JSON_UTF8;
                                                                       requestbuilder.Content        = JSONObject.Create(
                                                                                                           Duration.HasValue
                                                                                                               ? new JProperty("Duration",           (Int32) Duration.Value.TotalSeconds)
                                                                                                               : null,
                                                                                                           ChargingProductId != null
                                                                                                               ? new JProperty("ChargingProductId",  ChargingProductId.ToString())
                                                                                                               : null,
                                                                                                           ReservationId     != null
                                                                                                               ? new JProperty("ReservationId",      ReservationId.    ToString())
                                                                                                               : null,
                                                                                                           ProviderId        != null
                                                                                                               ? new JProperty("ProviderId",         ProviderId.       ToString())
                                                                                                               : null,
                                                                                                           eMAId             != null
                                                                                                               ? new JProperty("eMAId",              eMAId.            ToString())
                                                                                                               : null,
                                                                                                           AuthTokens.NotNullAny()
                                                                                                               ? new JProperty("AuthorizedIds",
                                                                                                                     JSONObject.Create(
                                                                                                                         new JProperty("RFIDIds", new JArray(AuthTokens.SafeSelect(token => token.ToString()))),
                                                                                                                         new JProperty("eMAIds",  new JArray(eMAIds.    SafeSelect(emaid => emaid.ToString())))
                                                                                                                     )
                                                                                                                 )
                                                                                                               : null
                                                                                                       ).ToUTF8Bytes();
                                                                   }),

                                             QueryTimeout.HasValue ? QueryTimeout : DefaultQueryTimeout,
                                             CancellationToken);

            if (response == null)
            {

                using (var logfile = File.AppendText("BoschEBike_2lemonage_" + Id.ToString().Replace("*", "_") + "_ReserveChargingStation.log"))
                {
                    logfile.WriteLine(DateTime.Now);
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                    logfile.WriteLine("timeout...");
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                }

                return ReservationResult.Timeout;

            }

            using (var logfile = File.AppendText("BoschEBike_2lemonage_" + Id.ToString().Replace("*", "_") + "_ReserveChargingStation.log"))
            {
                logfile.WriteLine(DateTime.Now);
                logfile.WriteLine(response.HTTPRequest.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
                logfile.WriteLine(response.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
            }

            JObject JSON       = null;
            String  ErrorCode  = null;

            if (response.ContentLength > 0)
            {
                JSON      = JObject.Parse(response.HTTPBody.ToUTF8String());
                ErrorCode = JSON["errorCode"] != null ? JSON["errorCode"].Value<String>() : null;
            }

            #region OK

            // HTTP/1.1 200 OK
            // Date: Wed, 24 Feb 2016 14:40:28 GMT
            // Server: Apache/2.2.16 (Debian)
            // Content-Length: 190
            // Content-Type: application/json
            // 
            // {
            //     "evseId":         "49*822*483*1",
            //     "pin":            "615849",
            //     "ReservationId":  "54ccc5c8-8711-4f16-afba-cf6b0094580d",
            //     "StartTime":      "2016-02-24T15:40:28+01:00",
            //     "Duration":       120,
            //     "State":          "CREATED",
            //     "errorCode":      "SUCCESS"
            // }

            if (response.HTTPStatusCode == HTTPStatusCode.OK &&
                ErrorCode               == "SUCCESS")
            {

                var NewReservation = new ChargingReservation(
                                         ChargingReservation_Id.Parse(JSON["ReservationId"].Value<String>()),
                                         DateTime.Now,
                                         JSON["StartTime"].Value<DateTime>(),
                                         TimeSpan.FromSeconds(JSON["Duration"].Value<Int32>()),
                                         JSON["StartTime"].Value<DateTime>() + TimeSpan.FromSeconds(JSON["Duration"].Value<Int32>()),
                                         JSON["ConsumedReservationTime"] != null ? TimeSpan.FromSeconds(JSON["ConsumedReservationTime"].Value<UInt32>()) : TimeSpan.FromSeconds(0),
                                         ChargingReservationLevel.ChargingStation,
                                         ProviderId,
                                         eMAId,
                                         this.ChargingStation.Operator.RoamingNetwork,
                                         EVSEId:             MapIncomingId(EVSE_Id.Parse(JSON["EVSEId"].Value<String>())),
                                         ChargingStationId:  Id,
                                         AuthTokens:         AuthTokens,  // As it is not included in the HTTP response!
                                         eMAIds:             eMAIds,      // As it is not included in the HTTP response!
                                         PINs:               JSON["pin"] != null
                                                                 ? new UInt32[] { UInt32.Parse(JSON["pin"].Value<String>()) }
                                                                 : null
                                     );

                SendNewReservation(NewReservation);

                return ReservationResult.Success(NewReservation);

            }

            #endregion

            #region Conflict

            else if (response.HTTPStatusCode == HTTPStatusCode.Conflict)
            {

                switch (ErrorCode)
                {

                    #region UnknownChargingStation

                    // HTTP/1.1 409 Conflict
                    // Date: Thu, 25 Feb 2016 11:12:55 GMT
                    // Server: Apache/2.2.16 (Debian)
                    // Content-Length: 65
                    // Content-Type: application/json
                    // 
                    // {
                    //     "StartTime":  null,
                    //     "Duration":   0,
                    //     "errorCode":  "LOCATION_ID_UNKNOWN"
                    // }
                    case "LOCATION_ID_UNKNOWN":
                        return ReservationResult.UnknownChargingStation;

                    #endregion

                    #region RESERVATION_EVSE_IN_USE -> NoEVSEsAvailable

                    // HTTP/1.1 409 Conflict
                    // Date: Thu, 11 Feb 2016 22:07:33 GMT
                    // Server: Apache/2.2.16 (Debian)
                    // Content-Length: 69
                    // Content-Type: application/json
                    // 
                    // {
                    //     "StartTime":  null,
                    //     "Duration":   0,
                    //     "errorCode":  "RESERVATION_EVSE_IN_USE"
                    // }
                    case "RESERVATION_EVSE_IN_USE":
                        return ReservationResult.NoEVSEsAvailable;

                    #endregion

                    #region Offline

                    // HTTP/1.1 409 Conflict
                    // Date: Tue, 16 Feb 2016 14:39:09 GMT
                    // Server: Apache/2.2.16 (Debian)
                    // Content-Length: 68
                    // Content-Type: application/json
                    // 
                    // {
                    //     "StartTime":  null,
                    //     "Duration":   0,
                    //     "errorCode":  "LOCATION_NOT_REACHABLE"
                    // }
                    case "LOCATION_NOT_REACHABLE":
                        return ReservationResult.Offline;

                    #endregion

                }

            }

            #endregion

            else if (response.HTTPStatusCode == HTTPStatusCode.NotFound)
            {

                // HTTP/1.1 404 Not Found
                // Date: Tue, 01 Mar 2016 00:32:18 GMT
                // Server: Apache/2.2.16 (Debian)
                // Transfer-Encoding: chunked
                // Content-Type: text/plain; charset=UTF-8

                if (!RemoteWhiteList.Contains(AuthInfo.FromRemoteIdentification(eMAId)))
                    return ReservationResult.InvalidCredentials;

            }

            return ReservationResult.Error(ErrorCode);

        }

        #endregion


        #region CancelReservation(...ReservationId, Reason, ...)

        /// <summary>
        /// Try to remove the given charging reservation.
        /// </summary>
        /// <param name="Timestamp">The timestamp of this request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="ReservationId">The unique charging reservation identification.</param>
        /// <param name="Reason">A reason for this cancellation.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public override async Task<CancelReservationResult>

            CancelReservation(DateTime                               Timestamp,
                              CancellationToken                      CancellationToken,
                              EventTracking_Id                       EventTrackingId,
                              ChargingReservation_Id                 ReservationId,
                              ChargingReservationCancellationReason  Reason,
                              TimeSpan?                              QueryTimeout  = null)

        {

            if (Reason == ChargingReservationCancellationReason.Expired)
                return CancelReservationResult.Success(ReservationId);


            var response = await new HTTPClient(Hostname,
                                                TCPPort,
                                                _RemoteCertificateValidator,
                                                DNSClient).

                                     Execute(client => client.DELETE(URIPrefix + "/Reservations/" + ReservationId.ToString(),

                                                                     requestbuilder => {
                                                                         requestbuilder.Host         = VirtualHost;
                                                                         requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                         requestbuilder.ContentType  = HTTPContentType.JSON_UTF8;
                                                                     }),

                                             QueryTimeout.HasValue ? QueryTimeout : DefaultQueryTimeout,
                                             CancellationToken);

            if (response == null)
            {

                using (var logfile = File.AppendText("BoschEBike_2lemonage_" + Id.ToString() + "_CancelReservation.log"))
                {
                    logfile.WriteLine(DateTime.Now);
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                    logfile.WriteLine("timeout...");
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                }

                return CancelReservationResult.Timeout;

            }

            using (var logfile = File.AppendText("BoschEBike_2lemonage_" + Id.ToString() + "_CancelReservation.log"))
            {
                logfile.WriteLine(DateTime.Now);
                logfile.WriteLine(response.HTTPRequest.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
                logfile.WriteLine(response.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
            }


            // HTTP / 1.1 200 OK
            // Date: Thu, 11 Feb 2016 12:03:50 GMT
            // Server: Apache / 2.2.16(Debian)
            // Content - Length: 0
            // Content - Type: text / plain; charset = UTF - 8


            if (response.HTTPStatusCode == HTTPStatusCode.OK ||
                response.HTTPStatusCode == HTTPStatusCode.NoContent)
            {

                return CancelReservationResult.Success(ReservationId);

            }

            else if (response.HTTPStatusCode == HTTPStatusCode.NotFound)
                return CancelReservationResult.UnknownReservationId(ReservationId);


            return CancelReservationResult.Error("The charging reservation could not be cancelled!");

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
        public override async Task<RemoteStartEVSEResult>

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

            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId), "The given unique EVSE identification must not be null!");

            #endregion

            var response = await new HTTPClient(Hostname,
                                                TCPPort,
                                                _RemoteCertificateValidator,
                                                DNSClient).

                                     Execute(client => client.POST(URIPrefix + "/EVSEs/" + MapOutgoingId(EVSEId).ToString() + "/RemoteStart",

                                                                   requestbuilder => {
                                                                       requestbuilder.Host           = VirtualHost;
                                                                       requestbuilder.Authorization  = new HTTPBasicAuthentication(HTTPLogin, HTTPPassword);
                                                                       requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                       requestbuilder.ContentType    = HTTPContentType.JSON_UTF8;
                                                                       requestbuilder.Content        = JSONObject.Create(
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
                                                                   }),

                                             QueryTimeout.HasValue ? QueryTimeout : TimeSpan.FromSeconds(120),
                                             CancellationToken);


            if (response == null)
            {

                using (var logfile = File.AppendText("BoschEBike_2lemonage_" + EVSEId.ToString().Replace("*", "_") + "_RemoteStartEVSE.log"))
                {
                    logfile.WriteLine(DateTime.Now);
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                    logfile.WriteLine("timeout...");
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                }

                return RemoteStartEVSEResult.Timeout;

            }

            using (var logfile = File.AppendText("BoschEBike_2lemonage_" + EVSEId.ToString().Replace("*", "_") + "_RemoteStartEVSE.log"))
            {
                logfile.WriteLine(DateTime.Now);
                logfile.WriteLine(response.HTTPRequest.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
                logfile.WriteLine(response.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
            }


            JObject JSON         = null;
            String  Description  = "";

            if (response.ContentLength > 0)
            {
                JSON         = JObject.Parse(response.HTTPBody.ToUTF8String());
                Description  = JSON["description"] != null ? JSON["description"].Value<String>() : "";
            }

            if (response.HTTPStatusCode == HTTPStatusCode.OK ||
                response.HTTPStatusCode == HTTPStatusCode.Created)
            {

                var NewSession = new ChargingSession(ChargingSession_Id.Parse(JSON["SessionId"].Value<String>())) {
                                     EVSEId             = EVSEId,
                                     ChargingProductId  = ChargingProductId,
                                     ReservationId      = ReservationId,
                                     ProviderId         = ProviderId,
                                     eMAIdStart         = eMAId,
                                     SessionTime        = StartEndDateTime.Now
                                 };

                SendNewChargingSession(NewSession);

                return RemoteStartEVSEResult.Success(NewSession);

            }

            else if (response.HTTPStatusCode == HTTPStatusCode.Unauthorized)
            {

                switch (Description)
                {

                    case "Unauthorized remote start or invalid credentials!":
                        return RemoteStartEVSEResult.InvalidCredentials;

                    default:
                        return RemoteStartEVSEResult.CommunicationError(Description);

                }

            }

            else if (response.HTTPStatusCode == HTTPStatusCode.Conflict)
            {

                switch (Description)
                {

                    case "The EVSE is already in use!":
                        return RemoteStartEVSEResult.AlreadyInUse;

                    case "The EVSE is reserved!":
                        return RemoteStartEVSEResult.Reserved(Description);

                    case "Missing reservation identification!":
                        return RemoteStartEVSEResult.Reserved(Description);

                    case "Invalid reservation identification!":
                        return RemoteStartEVSEResult.Reserved(Description);

                    case "The EVSE is out of service!":
                        return RemoteStartEVSEResult.OutOfService;

                }

            }

            else if (response.HTTPStatusCode == HTTPStatusCode.RequestTimeout)
                return RemoteStartEVSEResult.Timeout;


            return RemoteStartEVSEResult.Error(Description);

        }

        #endregion

        #region RemoteStop(...EVSEId, SessionId, ReservationHandling, ProviderId = null, eMAId = null, ...)

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
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public override async Task<RemoteStopEVSEResult>

            RemoteStop(DateTime             Timestamp,
                       CancellationToken    CancellationToken,
                       EventTracking_Id     EventTrackingId,
                       EVSE_Id              EVSEId,
                       ChargingSession_Id   SessionId,
                       ReservationHandling  ReservationHandling,
                       EVSP_Id              ProviderId    = null,
                       eMA_Id               eMAId         = null,
                       TimeSpan?            QueryTimeout  = null)

        {

            #region Initial checks

            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

            if (EVSEId    == null)
                throw new ArgumentNullException(nameof(EVSEId),     "The given unique EVSE identification must not be null!");

            if (SessionId == null)
                throw new ArgumentNullException("SessionId",  "The given unique charging session identification must not be null!");

            if (ReservationHandling == null)
                ReservationHandling = ReservationHandling.Close;

            #endregion


            var response = await new HTTPClient(Hostname,
                                                TCPPort,
                                                _RemoteCertificateValidator,
                                                DNSClient).

                                     Execute(client => client.POST(URIPrefix + "/EVSEs/" + MapOutgoingId(EVSEId) + "/RemoteStop",

                                                                   requestbuilder => {
                                                                       requestbuilder.Host           = VirtualHost;
                                                                       requestbuilder.Authorization  = new HTTPBasicAuthentication(HTTPLogin, HTTPPassword);
                                                                       requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                       requestbuilder.ContentType    = HTTPContentType.JSON_UTF8;
                                                                       requestbuilder.Content        = JSONObject.Create(
                                                                                                           new JProperty("eMAId",                      eMAId.    ToString()),
                                                                                                           new JProperty("SessionId",                  SessionId.ToString()),
                                                                                                           ReservationHandling.IsKeepAlive == true
                                                                                                               ? new JProperty("ReservationHandling",  ReservationHandling.KeepAliveTime.Value.TotalSeconds)
                                                                                                               : null,
                                                                                                           ReservationHandling.IsKeepAlive == false
                                                                                                               ? new JProperty("ReservationHandling",  "close")
                                                                                                               : null,
                                                                                                           ProviderId != null
                                                                                                               ? new JProperty("ProviderId",           ProviderId.ToString())
                                                                                                               : null
                                                                                                       ).ToUTF8Bytes();
                                                                   }),

                                             QueryTimeout.HasValue ? QueryTimeout : DefaultQueryTimeout,
                                             CancellationToken);

            if (response == null)
            {

                using (var logfile = File.AppendText("BoschEBike_2lemonage_" + EVSEId.ToString().Replace("*", "_") + "_RemoteStopEVSE.log"))
                {
                    logfile.WriteLine(DateTime.Now);
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                    logfile.WriteLine("timeout...");
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                }

                return RemoteStopEVSEResult.Timeout(SessionId);

            }

            using (var logfile = File.AppendText("BoschEBike_2lemonage_" + EVSEId.ToString().Replace("*", "_") + "_RemoteStopEVSE.log"))
            {
                logfile.WriteLine(DateTime.Now);
                logfile.WriteLine(response.HTTPRequest.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
                logfile.WriteLine(response.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
            }


            JObject JSON         = null;
            String  Description  = "";

            if (response.ContentLength > 0)
            {
                JSON        = JObject.Parse(response.HTTPBody.ToUTF8String());
                Description = JSON["description"] != null ? JSON["description"].Value<String>() : "";
            }


            if (response.HTTPStatusCode == HTTPStatusCode.OK ||
                response.HTTPStatusCode == HTTPStatusCode.NoContent)
            {

                return RemoteStopEVSEResult.Success(SessionId);

            }

            else if (response.HTTPStatusCode == HTTPStatusCode.Unauthorized)
            {

                switch (Description)
                {

                    case "Unauthorized remote start or invalid credentials!":
                        return RemoteStopEVSEResult.InvalidCredentials(SessionId);

                    default:
                        return RemoteStopEVSEResult.CommunicationError(SessionId, Description);

                }

            }

            else if (response.HTTPStatusCode == HTTPStatusCode.Conflict)
            {

                switch (Description)
                {

                    case "Invalid SessionId!":
                        return RemoteStopEVSEResult.InvalidSessionId(SessionId);

                    case "Unauthorized remote start or invalid credentials!":
                        return RemoteStopEVSEResult.InvalidCredentials(SessionId);

                    case "EVSE is out of service!":
                        return RemoteStopEVSEResult.OutOfService(SessionId);

                    case "EVSE is offline!":
                        return RemoteStopEVSEResult.Offline(SessionId);

                }

            }

            else if (response.HTTPStatusCode == HTTPStatusCode.RequestTimeout)
                return RemoteStopEVSEResult.Timeout(SessionId);


            return RemoteStopEVSEResult.Error(SessionId, Description);

        }

        #endregion


        #region GetWhiteList

        public async Task<IEnumerable<AuthInfo>>

            GetWhiteList(DateTime           Timestamp,
                         CancellationToken  CancellationToken,
                         EventTracking_Id   EventTrackingId,
                         TimeSpan?          QueryTimeout = null)

        { 

            var response = await new HTTPClient(Hostname,
                                                TCPPort,
                                                _RemoteCertificateValidator,
                                                DNSClient).

                                     Execute(client => client.GET(URIPrefix + "/AuthLists/12345678",

                                                                   requestbuilder => {
                                                                       requestbuilder.Host         = VirtualHost;
                                                                       requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                       requestbuilder.ContentType  = HTTPContentType.JSON_UTF8;
                                                                   }),

                                             QueryTimeout.HasValue ? QueryTimeout : DefaultQueryTimeout,
                                             CancellationToken);

            if (response == null)
                return new AuthInfo[0];

            // HTTP / 1.1 200 OK
            // Date: Wed, 10 Feb 2016 16:10:01 GMT
            // Server: Apache / 2.2.16(Debian)
            // Content - Length: 74
            // Content - Type: application / json
            // 
            // { "EvseStatus":{ "\"49*822*483*1\"":{ "2016-02-10T17:09:28CET":"AVAILABLE"} } }

            if (response.HTTPStatusCode != HTTPStatusCode.OK)
                return new AuthInfo[0];

            Auth_Token AuthToken  = null;
            eMA_Id     eMAId      = null;
            var AuthInfos         = new HashSet<AuthInfo>();

            foreach (JObject entry in JObject.Parse(response.HTTPBody.ToUTF8String())["Identifications"] as JArray)
            {

                if (entry["status"].Value<String>() == "inactive")
                    continue;

                switch (entry["type"].Value<String>())
                {

                    case "RFID":
                        if (!Auth_Token.TryParse(entry["id"].Value<String>(), out AuthToken))
                            continue;
                        AuthInfos.Add(AuthInfo.FromAuthToken(AuthToken));
                        break;

                    case "eMAId":
                        if (!eMA_Id.TryParse(entry["id"].Value<String>(), out eMAId))
                            continue;
                        AuthInfos.Add(AuthInfo.FromRemoteIdentification(eMAId));
                        break;

                }

            }

            _RemoteWhiteList.Clear();
            AuthInfos.ForEach(authinfo => _RemoteWhiteList.Add(authinfo));

            return AuthInfos;

        }

        #endregion

        #region ADDToWhiteList

        public async Task<IEnumerable<AuthInfoStatus>>

            ADDToWhiteList(DateTime                 Timestamp,
                           CancellationToken        CancellationToken,
                           EventTracking_Id         EventTrackingId,
                           IEnumerable<Auth_Token>  AuthTokens,
                           IEnumerable<eMA_Id>      eMAIds,
                           TimeSpan?                QueryTimeout = null)

        {

            var _AuthTokens = AuthTokens != null ? AuthTokens : new Auth_Token[0];
            var _eMAIds     = eMAIds     != null ? eMAIds     : new eMA_Id[0];

            var response = await new HTTPClient(Hostname,
                                                TCPPort,
                                                _RemoteCertificateValidator,
                                                DNSClient: DNSClient).

                                 Execute(client => client.POST(URIPrefix + "/AuthLists/12345678",

                                                               requestbuilder => {
                                                                   requestbuilder.Host         = VirtualHost;
                                                                   requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                   requestbuilder.ContentType  = HTTPContentType.JSON_UTF8;
                                                                   requestbuilder.Content      = JSONObject.Create(
                                                                                                      new JProperty("Identifications", new JArray(
                                                                                                          _AuthTokens.Select(authtoken =>
                                                                                                              JSONObject.Create(
                                                                                                                  new JProperty("type",    "rfid"),
                                                                                                                  new JProperty("id",       authtoken.ToString()),
                                                                                                                  new JProperty("status",  "active")
                                                                                                              )).Concat(

                                                                                                          _eMAIds.Select(eMAId =>
                                                                                                              JSONObject.Create(
                                                                                                                  new JProperty("type",    "emaid"),
                                                                                                                  new JProperty("id",       eMAId.ToString()),
                                                                                                                  new JProperty("status",  "active")
                                                                                                              ))).ToArray()
                                                                                                          ))
                                                                                                  ).ToUTF8Bytes();
                                                                }),

                                         QueryTimeout.HasValue ? QueryTimeout : DefaultQueryTimeout,
                                         CancellationToken);

            if (response == null)
            {

                using (var logfile = File.AppendText("BoschEBike_2lemonage_" + Id.ToString() + "_ADDToWhiteList.log"))
                {
                    logfile.WriteLine(DateTime.Now);
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                    logfile.WriteLine("timeout...");
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                }

                return new AuthInfoStatus[0];

            }

            using (var logfile = File.AppendText("BoschEBike_2lemonage_" + Id.ToString() + "_ADDToWhiteList.log"))
            {
                logfile.WriteLine(DateTime.Now);
                logfile.WriteLine(response.HTTPRequest.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
                logfile.WriteLine(response.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
            }


            // HTTP/1.1 200 OK
            // Date: Sat, 27 Feb 2016 15:52:04 GMT
            // Server: Apache/2.2.16 (Debian)
            // Content-Length: 93
            // Content-Type: application/json
            // 
            // {
            //     "authListId":"     12345678",
            //     "detail":          {
            //                            "DE*BSI*I00811*1": "ERROR"
            //                        },
            //     "authListStatus":  "AUTHLIST_OK"
            // }

            // HTTP/1.1 200 OK
            // Date: Sat, 27 Feb 2016 15:56:40 GMT
            // Server: Apache/2.2.16 (Debian)
            // Content-Length: 95
            // Content-Type: application/json
            // 
            // {
            //     "authListId":"     12345678",
            //     "detail":          {
            //                            "DE*BSI*I00811*7": "CREATED"
            //                        },
            //     "authListStatus":  "AUTHLIST_OK"
            // }

            // HTTP/1.1 200 OK
            // Date: Sat, 27 Feb 2016 15:56:40 GMT
            // Server: Apache/2.2.16 (Debian)
            // Content-Length: 95
            // Content-Type: application/json
            // 
            // {
            //     "authListId":"     12345678",
            //     "detail":          {
            //                            "DE*BSI*I00811*7": "EXISTED_NOT_UPDATED"
            //                        },
            //     "authListStatus":  "AUTHLIST_OK"
            // }

            if (response.HTTPStatusCode == HTTPStatusCode.OK)
            {

                if (response.ContentLength > 0)
                {

                    var JSON              = JObject.Parse(response.HTTPBody.ToUTF8String());
                    var Detail            = JSON["detail"] != null ? JSON["detail"] as JObject : null;
                    var InvalidAuthInfos  = new List<AuthInfoStatus>();

                    #region Process AuthTokens

                    foreach (var AuthToken in AuthTokens)
                    {

                        var AuthTokenStatus = Detail[AuthToken.ToString()];

                        if (AuthTokenStatus != null)
                            switch (AuthTokenStatus.Value<String>())
                            {

                                case "CREATED":
                                case "EXISTED_NOT_UPDATED":
                                    RemoteWhiteList.Add(AuthInfo.FromAuthToken(AuthToken));
                                    break;

                                case "ERROR":
                                    InvalidAuthInfos.Add(new AuthInfoStatus(AuthInfo.FromAuthToken(AuthToken), AuthInfoStatusType.Error));
                                    break;

                            }

                        else
                            InvalidAuthInfos.Add(new AuthInfoStatus(AuthInfo.FromAuthToken(AuthToken), AuthInfoStatusType.Invalid));

                    }

                    #endregion

                    #region Process eMAIds

                    foreach (var eMAId in eMAIds)
                    {

                        var eMAIdStatus = Detail[eMAId.ToString()];

                        if (eMAIdStatus != null)
                            switch (eMAIdStatus.Value<String>())
                            {

                                case "CREATED":
                                case "EXISTED_NOT_UPDATED":
                                    RemoteWhiteList.Add(AuthInfo.FromRemoteIdentification(eMAId));
                                    break;

                                case "ERROR":
                                    InvalidAuthInfos.Add(new AuthInfoStatus(AuthInfo.FromRemoteIdentification(eMAId), AuthInfoStatusType.Error));
                                    break;

                            }

                        else
                            InvalidAuthInfos.Add(new AuthInfoStatus(AuthInfo.FromRemoteIdentification(eMAId), AuthInfoStatusType.Invalid));

                    }

                    #endregion


                    if (InvalidAuthInfos.Any())
                        return InvalidAuthInfos;


                    // Everything is ok!
                    return new AuthInfoStatus[0];

                }

            }

            // Everything is invalid!
            return _AuthTokens.
                       Select(authtoken => new AuthInfoStatus(AuthInfo.FromAuthToken(authtoken), AuthInfoStatusType.Error)).
                       Concat(_eMAIds.
                                  Select(emaid => new AuthInfoStatus(AuthInfo.FromRemoteIdentification(emaid), AuthInfoStatusType.Error)));

        }

        #endregion

        #region REMOVEFromWhiteList

        public async Task<IEnumerable<AuthInfoStatus>>

            REMOVEFromWhiteList(DateTime                 Timestamp,
                                CancellationToken        CancellationToken,
                                EventTracking_Id         EventTrackingId,
                                IEnumerable<Auth_Token>  AuthTokens,
                                IEnumerable<eMA_Id>      eMAIds,
                                TimeSpan?                QueryTimeout = null)

        {

            //var _AuthTokens = AuthList.Where(item => item.AuthToken            != null).Select(item => item.AuthToken);
            //var _eMAIds     = AuthList.Where(item => item.RemoteIdentification != null).Select(item => item.RemoteIdentification);


            var _AuthTokens = AuthTokens != null ? AuthTokens : new Auth_Token[0];
            var _eMAIds     = eMAIds     != null ? eMAIds     : new eMA_Id[0];

            var response = await new HTTPClient(Hostname,
                                                TCPPort,
                                                _RemoteCertificateValidator,
                                                DNSClient: DNSClient).

                                 Execute(client => client.DELETE(URIPrefix + "/AuthLists/12345678",

                                                                 requestbuilder => {
                                                                     requestbuilder.Host         = VirtualHost;
                                                                     requestbuilder.Accept.Add(HTTPContentType.JSON_UTF8);
                                                                     requestbuilder.ContentType  = HTTPContentType.JSON_UTF8;
                                                                     requestbuilder.Content      = JSONObject.Create(
                                                                                                       new JProperty("Identifications", new JArray(
                                                                                                           _AuthTokens.Select(authtoken =>
                                                                                                               JSONObject.Create(
                                                                                                                   new JProperty("type",    "rfid"),
                                                                                                                   new JProperty("id",       authtoken.ToString()),
                                                                                                                   new JProperty("status",  "active")
                                                                                                               )).Concat(

                                                                                                           _eMAIds.Select(eMAId =>
                                                                                                               JSONObject.Create(
                                                                                                                   new JProperty("type",    "emaid"),
                                                                                                                   new JProperty("id",       eMAId.ToString()),
                                                                                                                   new JProperty("status",  "active")
                                                                                                               ))).ToArray()
                                                                                                           ))
                                                                                                   ).ToUTF8Bytes();
                                                                 }),

                                         QueryTimeout.HasValue ? QueryTimeout : DefaultQueryTimeout,
                                         CancellationToken);

            if (response == null)
            {

                using (var logfile = File.AppendText("BoschEBike_2lemonage_" + Id.ToString() + "_REMOVEFromWhiteList.log"))
                {
                    logfile.WriteLine(DateTime.Now);
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                    logfile.WriteLine("timeout...");
                    logfile.WriteLine("--------------------------------------------------------------------------------");
                }

                return new AuthInfoStatus[0];

            }

            using (var logfile = File.AppendText("BoschEBike_2lemonage_" + Id.ToString() + "_REMOVEFromWhiteList.log"))
            {
                logfile.WriteLine(DateTime.Now);
                logfile.WriteLine(response.HTTPRequest.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
                logfile.WriteLine(response.EntirePDU);
                logfile.WriteLine("--------------------------------------------------------------------------------");
            }


            // HTTP/1.1 200 OK
            // Date: Wed, 24 Feb 2016 11:26:43 GMT
            // Server: Apache/2.2.16 (Debian)
            // Content-Length: 119
            // Content-Type: application/json
            // 
            // {
            //     "authListId":      "12345678",
            //     "detail":          {
            //                            "12344444":  "NOT_FOUND",
            //                            "22222222":  "EXISTED_UPDATED"
            //                        },
            //     "authListStatus":  "AUTHLIST_OK"
            // }

            if (response.HTTPStatusCode == HTTPStatusCode.OK)
            {

                if (response.ContentLength > 0)
                {

                    var JSON              = JObject.Parse(response.HTTPBody.ToUTF8String());
                    var Detail            = JSON["detail"] != null ? JSON["detail"] as JObject : null;
                    var InvalidAuthInfos  = new List<AuthInfoStatus>();

                    #region Process AuthTokens

                    foreach (var AuthToken in _AuthTokens)
                    {

                        var AuthTokenStatus = Detail[AuthToken.ToString()];

                        if (AuthTokenStatus != null)
                            switch (AuthTokenStatus.Value<String>())
                            {

                                case "EXISTED_UPDATED":
                                    RemoteWhiteList.Remove(AuthInfo.FromAuthToken(AuthToken));
                                    break;

                                case "NOT_FOUND":
                                    InvalidAuthInfos.Add(new AuthInfoStatus(AuthInfo.FromAuthToken(AuthToken), AuthInfoStatusType.Error));
                                    break;

                            }

                        else
                            InvalidAuthInfos.Add(new AuthInfoStatus(AuthInfo.FromAuthToken(AuthToken), AuthInfoStatusType.Invalid));

                    }

                    #endregion

                    #region Process eMAIds

                    foreach (var eMAId in _eMAIds)
                    {

                        var eMAIdStatus = Detail[eMAId.ToString()];

                        if (eMAIdStatus != null)
                            switch (eMAIdStatus.Value<String>())
                            {

                                case "EXISTED_UPDATED":
                                    RemoteWhiteList.Add(AuthInfo.FromRemoteIdentification(eMAId));
                                    break;

                                case "NOT_FOUND":
                                    InvalidAuthInfos.Add(new AuthInfoStatus(AuthInfo.FromRemoteIdentification(eMAId), AuthInfoStatusType.NotFound));

                                    break;

                            }

                        else
                            InvalidAuthInfos.Add(new AuthInfoStatus(AuthInfo.FromRemoteIdentification(eMAId), AuthInfoStatusType.Invalid));

                    }

                    #endregion


                    if (InvalidAuthInfos.Any())
                        return InvalidAuthInfos;


                    // Everything is ok!
                    return new AuthInfoStatus[0];

                }

            }

            // Everything is invalid!
            return _AuthTokens.
                       Select(authtoken => new AuthInfoStatus(AuthInfo.FromAuthToken(authtoken), AuthInfoStatusType.Error)).
                       Concat(_eMAIds.
                                  Select(emaid => new AuthInfoStatus(AuthInfo.FromRemoteIdentification(emaid), AuthInfoStatusType.Error)));

        }

        #endregion

        #region REPLACEWWhiteList

        public async Task<IEnumerable<AuthInfoStatus>>

            REPLACEWWhiteList(DateTime                 Timestamp,
                              CancellationToken        CancellationToken,
                              EventTracking_Id         EventTrackingId,
                              IEnumerable<Auth_Token>  AuthTokens,
                              IEnumerable<eMA_Id>      eMAIds,
                              TimeSpan?                QueryTimeout = null)

        {

            var CurrentWhiteList = await GetWhiteList(DateTime.Now,
                                                      CancellationToken,
                                                      EventTrackingId,
                                                      QueryTimeout);

            var ToRemove = CurrentWhiteList.Where(item => item.AuthToken            != null && !AuthTokens.Contains(item.AuthToken) ||
                                                          item.RemoteIdentification != null && !eMAIds.    Contains(item.RemoteIdentification)).ToArray();

            var ToInsert = AuthTokens.Where (item => !CurrentWhiteList.Contains(AuthInfo.FromAuthToken(item))).
                                      Select(item => AuthInfo.FromAuthToken(item)).Concat(
                           eMAIds.    Where(item => !CurrentWhiteList.Contains(AuthInfo.FromRemoteIdentification(item))).
                                      Select(item => AuthInfo.FromRemoteIdentification(item))).
                                      ToArray();

            var Removed = ToRemove.Any()
                              ? await REMOVEFromWhiteList(DateTime.Now,
                                                          CancellationToken,
                                                          EventTrackingId,
                                                          ToRemove.Where(item => item.AuthToken            != null).Select(item => item.AuthToken),
                                                          ToRemove.Where(item => item.RemoteIdentification != null).Select(item => item.RemoteIdentification),
                                                          QueryTimeout)
                              : new AuthInfoStatus[0];

            var Inserted = ToInsert.Any()
                              ? await ADDToWhiteList(DateTime.Now,
                                                     CancellationToken,
                                                     EventTrackingId,
                                                     ToInsert.Where(item => item.AuthToken            != null).Select(item => item.AuthToken),
                                                     ToInsert.Where(item => item.RemoteIdentification != null).Select(item => item.RemoteIdentification),
                                                     QueryTimeout)
                              : new AuthInfoStatus[0];

            return Removed.Concat(Inserted).
                       OrderBy(item => item.Id);

        }

        #endregion

    }

}
