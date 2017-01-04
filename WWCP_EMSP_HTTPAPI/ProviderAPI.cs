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
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Sockets;
using org.GraphDefined.Vanaheimr.Hermod.Sockets.TCP;

#endregion

namespace org.GraphDefined.WWCP.EMSP
{

    public class ProviderAPI
    {

        #region Data

        private                HTTPEventSource  DebugLog;

        private const          String           HTTPRoot                            = "org.GraphDefined.WWCP.EMSP.HTTPRoot";


        /// <summary>
        /// The default HTTP server name.
        /// </summary>
        public const           String           DefaultHTTPServerName               = "GraphDefined Provider API HTTP Service v0.3";

        /// <summary>
        /// The default HTTP server TCP port.
        /// </summary>
        public static readonly IPPort           DefaultHTTPServerPort               = new IPPort(3200);

        /// <summary>
        /// The default HTTP server URI prefix.
        /// </summary>
        public const           String           DefaultURIPrefix                    = "/emsp";

        /// <summary>
        /// The default HTTP logfile.
        /// </summary>
        public const           String           DefaultLogfileName                  = "ProviderMap_HTTPAPI.log";


        public  const           String          HTTPLogin                           = "chargingmap";
        public  const           String          HTTPPassword                        = "gf0c31j08ufgw3j9w3t";

        public const            String          WWWAuthenticationRealm              = "Open Charging Cloud";

        public readonly static  HTTPMethod      RESERVE                             = HTTPMethod.Create("RESERVE",     IsSafe: false, IsIdempotent: true);
        public readonly static  HTTPMethod      REMOTESTART                         = HTTPMethod.Create("REMOTESTART", IsSafe: false, IsIdempotent: true);
        public readonly static  HTTPMethod      REMOTESTOP                          = HTTPMethod.Create("REMOTESTOP",  IsSafe: false, IsIdempotent: true);

        #endregion

        #region Properties

        /// <summary>
        /// The attached e-mobility service provider.
        /// </summary>
        public IEMobilityProviderUserInterface         EMSP            { get; }

        /// <summary>
        /// The HTTP server of the API.
        /// </summary>
        public HTTPServer                 HTTPServer      { get; }

        /// <summary>
        /// The HTTP hostname for all URIs within this API.
        /// </summary>
        public HTTPHostname               HTTPHostname    { get; }

        /// <summary>
        /// A common URI prefix for all URIs within this API.
        /// </summary>
        public String                     URIPrefix       { get; }

        /// <summary>
        /// The DNS resolver to use.
        /// </summary>
        public DNSClient                  DNSClient       { get; }

        #endregion

        #region Events

        #region OnReserveEVSE

        /// <summary>
        /// An event sent whenever an EVSE reservation request was received.
        /// </summary>
        public event RequestLogHandler      OnReserveEVSELog;

        /// <summary>
        /// An event sent whenever an EVSE is reserved.
        /// </summary>
        public event OnReserveEVSEDelegate  OnReserveEVSE;

        /// <summary>
        /// An event sent whenever an EVSE reservation request response was sent.
        /// </summary>
        public event AccessLogHandler       OnEVSEReservedLog;

        #endregion

        #region OnCancelReservation

        /// <summary>
        /// An event sent whenever a reservation will be cancelled by an EVSE operator.
        /// </summary>
        public event RequestLogHandler            OnReservationCancel;

        /// <summary>
        /// An event sent whenever a reservation will be cancelled by an EVSE operator.
        /// </summary>
        public event OnCancelReservationDelegate  OnCancelReservation;

        /// <summary>
        /// An event sent whenever a reservation was cancelled by an EVSE operator.
        /// </summary>
        public event AccessLogHandler             OnReservationCancelled;

        #endregion


        #region OnRemoteStartEVSE

        /// <summary>
        /// An event sent whenever a remote start EVSE request was received.
        /// </summary>
        public event RequestLogHandler      OnRemoteStartEVSELog;

        /// <summary>
        /// An event sent whenever an EVSE should start charging.
        /// </summary>
        public event OnRemoteStartDelegate  OnRemoteStartEVSE;

        /// <summary>
        /// An event sent whenever a remote start EVSE response was sent.
        /// </summary>
        public event AccessLogHandler       OnEVSERemoteStartedLog;

        #endregion

        #region OnRemoteStopEVSE

        /// <summary>
        /// An event sent whenever a remote stop EVSE request was received.
        /// </summary>
        public event RequestLogHandler     OnRemoteStopEVSELog;

        /// <summary>
        /// An event sent whenever an EVSE should stop charging.
        /// </summary>
        public event OnRemoteStopDelegate  OnRemoteStopEVSE;

        /// <summary>
        /// An event sent whenever a remote stop EVSE response was sent.
        /// </summary>
        public event AccessLogHandler      OnEVSERemoteStoppedLog;

        #endregion


        #region RequestLog

        /// <summary>
        /// An event called whenever a request came in.
        /// </summary>
        public event RequestLogHandler RequestLog
        {

            add
            {
                HTTPServer.RequestLog += value;
            }

            remove
            {
                HTTPServer.RequestLog -= value;
            }

        }

        #endregion

        #region AccessLog

        /// <summary>
        /// An event called whenever a request could successfully be processed.
        /// </summary>
        public event AccessLogHandler AccessLog
        {

            add
            {
                HTTPServer.AccessLog += value;
            }

            remove
            {
                HTTPServer.AccessLog -= value;
            }

        }

        #endregion

        #region ErrorLog

        /// <summary>
        /// An event called whenever a request resulted in an error.
        /// </summary>
        public event ErrorLogHandler ErrorLog
        {

            add
            {
                HTTPServer.ErrorLog += value;
            }

            remove
            {
                HTTPServer.ErrorLog -= value;
            }

        }

        #endregion

        #endregion

        #region Constructor(s)

        #region ProviderAPI(HTTPServerName = DefaultHTTPServerName, ...)

        public ProviderAPI(eMobilityServiceProvider          EMSP,

                           String                            HTTPServerName                    = DefaultHTTPServerName,
                           IPPort                            HTTPServerPort                    = null,
                           HTTPHostname                      HTTPHostname                      = null,
                           String                            URIPrefix                         = DefaultURIPrefix,

                           String                            ServerThreadName                  = null,
                           ThreadPriority                    ServerThreadPriority              = ThreadPriority.AboveNormal,
                           Boolean                           ServerThreadIsBackground          = true,
                           ConnectionIdBuilder               ConnectionIdBuilder               = null,
                           ConnectionThreadsNameBuilder      ConnectionThreadsNameBuilder      = null,
                           ConnectionThreadsPriorityBuilder  ConnectionThreadsPriorityBuilder  = null,
                           Boolean                           ConnectionThreadsAreBackground    = true,
                           TimeSpan?                         ConnectionTimeout                 = null,
                           UInt32                            MaxClientConnections              = TCPServer.__DefaultMaxClientConnections,

                           DNSClient                         DNSClient                         = null,
                           Boolean                           Autostart                         = false)

            : this(EMSP,
                   new HTTPServer(TCPPort:                           HTTPServerPort ?? DefaultHTTPServerPort,
                                  DefaultServerName:                 HTTPServerName,
                                  ServerThreadName:                  ServerThreadName,
                                  ServerThreadPriority:              ServerThreadPriority,
                                  ServerThreadIsBackground:          ServerThreadIsBackground,
                                  ConnectionIdBuilder:               ConnectionIdBuilder,
                                  ConnectionThreadsNameBuilder:      ConnectionThreadsNameBuilder,
                                  ConnectionThreadsPriorityBuilder:  ConnectionThreadsPriorityBuilder,
                                  ConnectionThreadsAreBackground:    ConnectionThreadsAreBackground,
                                  ConnectionTimeout:                 ConnectionTimeout,
                                  MaxClientConnections:              MaxClientConnections,
                                  DNSClient:                         DNSClient,
                                  Autostart:                         false),
                   HTTPHostname,
                   URIPrefix)

        {

            if (Autostart)
                HTTPServer.Start();

        }

        #endregion

        #region (private) ProviderAPI(HTTPServer, HTTPHostname = "*", URIPrefix = "/", ...)

        private ProviderAPI(eMobilityServiceProvider  EMSP,
                            HTTPServer                HTTPServer,
                            HTTPHostname              HTTPHostname  = null,
                            String                    URIPrefix     = "/")
        {

            #region Initial checks

            if (EMSP == null)
                throw new ArgumentNullException(nameof(EMSP),        "The given e-mobility service provider must not be null!");

            if (HTTPServer == null)
                throw new ArgumentNullException(nameof(HTTPServer),  "The given HTTP server must not be null!");

            if (URIPrefix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(URIPrefix),   "The given URI prefix must not be null or empty!");

            if (!URIPrefix.StartsWith("/", StringComparison.Ordinal))
                URIPrefix = "/" + URIPrefix;

            #endregion

            #region Init data

            this.EMSP          = EMSP;

            this.HTTPServer    = HTTPServer;
            this.HTTPHostname  = HTTPHostname ?? HTTPHostname.Any;
            this.URIPrefix     = URIPrefix;

            this.DNSClient     = HTTPServer.DNSClient;

            #endregion

            RegisterURITemplates();

        }

        #endregion

        #endregion


        #region (static) AttachToHTTPAPI(HTTPServer, HTTPHostname = "*", URIPrefix = "/", ...)

        /// <summary>
        /// Attach this HTTP API to the given HTTP server.
        /// </summary>
        public static ProviderAPI AttachToHTTPAPI(eMobilityServiceProvider  EMSP,
                                                  HTTPServer                HTTPServer,
                                                  HTTPHostname              HTTPHostname  = null,
                                                  String                    URIPrefix     = "/")

            => new ProviderAPI(EMSP,
                               HTTPServer,
                               HTTPHostname,
                               URIPrefix);

        #endregion

        #region (private) RegisterURITemplates()

        private void RegisterURITemplates()
        {

            DebugLog  = HTTPServer.AddEventSource(EventIdentification:      "DebugLog",
                                                  MaxNumberOfCachedEvents:  1000,
                                                  RetryIntervall:           TimeSpan.FromSeconds(5),
                                                  URITemplate:              URIPrefix + "/DebugLog");

            #region / (HTTPRoot)

            HTTPServer.RegisterResourcesFolder(HTTPHostname,
                                               URIPrefix,
                                               HTTPRoot,
                                               DefaultFilename: "index.html");

            #endregion


            #region RESERVE      ~/EVSEs/{EVSEId}

            #region Documentation

            // RESERVE ~/ChargingStations/DE*822*S123456789  // optional
            // RESERVE ~/EVSEs/DE*822*E123456789*1
            // 
            // {
            //     "ReservationId":      "5c24515b-0a88-1296-32ea-1226ce8a3cd0",                   // optional
            //     "StartTime":          "2015-10-20T11:25:43.511Z",                               // optional; default: current timestamp
            //     "Duration":           3600,                                                     // optional; default: 900 [seconds]
            //     "IntendedCharging":   {                                                         // optional; (good for energy management)
            //                               "StartTime":          "2015-10-20T11:30:00.000Z",     // optional; default: reservation start time
            //                               "Duration":           1800,                           // optional; default: reservation duration [seconds]
            //                               "ChargingProductId":  "AC1"                           // optional; default: Default product
            //                               "Plug":               "TypeFSchuko|Type2Outlet|...",  // optional;
            //                               "Consumption":        20,                             // optional; [kWh]
            //                               "ChargePlan":         "fastest"                       // optional;
            //                           },
            //     "AuthorizedIds":      {                                                         // optional; List of authentication methods...
            //                               "AuthTokens",  ["012345ABCDEF", ...],                    // optional; List of RFID Ids
            //                               "eMAIds",   ["DE*ICE*I00811*1", ...],                 // optional; List of eMA Ids
            //                               "PINs",     ["123456", ...],                          // optional; List of keypad Pins
            //                               "Liste",    [...]                                     // optional; List of known (white-)lists
            //                           }
            // }

            #endregion

            // -----------------------------------------------------------------------
            // curl -v -X RESERVE -H "Content-Type: application/json" \
            //                    -H "Accept:       application/json"  \
            //      -d "{ \"eMAId\":         \"DE*BSI*I00811*1\", \
            //            \"StartTime\":     \"2015-10-20T11:25:43.511Z\", \
            //            \"Duration\":        3600, \
            //            \"IntendedCharging\": { \
            //                                 \"Consumption\": 20, \
            //                                 \"Plug\":        \"TypeFSchuko\" \
            //                               }, \
            //            \"AuthorizedIds\": { \
            //                                 \"AuthTokens\": [\"1AA234BB\", \"012345ABCDEF\"], \
            //                                 \"eMAIds\":  [\"DE*ICE*I00811*1\"], \
            //                                 \"PINs\":    [\"1234\", \"6789\"] \
            //                               } \
            //          }" \
            //      http://127.0.0.1:3004/RNs/Prod/EVSEs/49*822*066268034*1
            // -----------------------------------------------------------------------
            HTTPServer.AddMethodCallback(HTTPHostname,
                                         RESERVE,
                                         URIPrefix + "/EVSEs/{EVSEId}",
                                         HTTPContentType.JSON_UTF8,
                                         HTTPDelegate: async Request => {

                                             SendReserveEVSE(Request);

                                             #region Check HTTP Basic Authentication

                                             if (Request.Authorization          == null        ||
                                                 Request.Authorization.Username != HTTPLogin   ||
                                                 Request.Authorization.Password != HTTPPassword)
                                             {

                                                 return SendEVSERemoteStarted(
                                                     new HTTPResponseBuilder(Request) {
                                                         HTTPStatusCode   = HTTPStatusCode.Unauthorized,
                                                         WWWAuthenticate  = @"Basic realm=""" + WWWAuthenticationRealm + @"""",
                                                         Server           = HTTPServer.DefaultServerName,
                                                         Date             = DateTime.Now,
                                                         Connection       = "close"
                                                     });

                                             }

                                             #endregion

                                             #region Get EVSEId URI parameter

                                             HTTPResponse  _HTTPResponse;
                                             EVSE_Id       EVSEId;

                                             if (!Request.ParseEVSEId(DefaultHTTPServerName,
                                                                      out EVSEId,
                                                                      out _HTTPResponse))
                                             {
                                                 return SendEVSERemoteStarted(_HTTPResponse);
                                             }

                                             #endregion

                                             #region Parse JSON  [mandatory]

                                             JSONWrapper              JSON               = null;

                                             eMobilityAccount_Id?     eMAId              = null;
                                             ChargingReservation_Id?  ReservationId      = null;
                                             DateTime?                StartTime          = null;
                                             TimeSpan?                Duration           = null;

                                             // IntendedCharging
                                             DateTime?                ChargingStartTime  = null;
                                             TimeSpan?                CharingDuration    = null;
                                             ChargingProduct_Id?      ChargingProductId  = null;
                                             var                      Plug               = PlugTypes.Unspecified;
                                             var                      Consumption        = 0U;

                                             // AuthorizedIds
                                             var                      AuthTokens         = new List<Auth_Token>();
                                             var                      eMAIds             = new List<eMobilityAccount_Id>();
                                             var                      PINs               = new List<UInt32>();

                                             if (Request.TryParseJObjectRequestBody(out JSON, out _HTTPResponse, AllowEmptyHTTPBody: true))
                                             {

                                                 Object JSONToken;

                                                 #region Check StartTime            [optional]

                                                 if (JSON.TryGetValue("StartTime", out JSONToken))
                                                 {

                                                     try
                                                     {
                                                         StartTime = (DateTime) JSONToken;
                                                     }
                                                     catch (Exception)
                                                     {

                                                        return SendEVSEReserved(
                                                            new HTTPResponseBuilder(Request) {
                                                                HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                ContentType     = HTTPContentType.JSON_UTF8,
                                                                Content         = new JObject(new JProperty("description", "Invalid value for parameter 'StartTime'!")).ToUTF8Bytes()
                                                            });

                                                     }

                                                     if (StartTime <= DateTime.Now)
                                                         return SendEVSEReserved(
                                                             new HTTPResponseBuilder(Request) {
                                                                 HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                 ContentType     = HTTPContentType.JSON_UTF8,
                                                                 Content         = new JObject(new JProperty("description", "The starting time must be in the future!")).ToUTF8Bytes()
                                                             });

                                                 }

                                                 #endregion

                                                 #region Check Duration             [optional]

                                                 if (JSON.TryGetValue("Duration", out JSONToken))
                                                 {

                                                     try
                                                     {
                                                         Duration = TimeSpan.FromSeconds(Convert.ToUInt32(JSONToken));
                                                     }
                                                     catch (Exception)
                                                     {

                                                        return SendEVSEReserved(
                                                            new HTTPResponseBuilder(Request) {
                                                                HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                ContentType     = HTTPContentType.JSON_UTF8,
                                                                Content         = new JObject(new JProperty("description", "Invalid value for parameter 'duration'!")).ToUTF8Bytes()
                                                            });

                                                     }

                                                 }

                                                 #endregion

                                                 #region Check ReservationId        [optional]

                                                 if (!JSON.ParseOptionalN("ReservationId",
                                                                          "Charging reservation identification",
                                                                          HTTPServer.DefaultServerName,
                                                                          ChargingReservation_Id.TryParse,
                                                                          out ReservationId,
                                                                          Request,
                                                                          out _HTTPResponse))

                                                     return SendEVSEReserved(_HTTPResponse);

                                                 //if (JSON.TryGetValue("ReservationId", out JSONToken) &&
                                                 //    !ChargingReservation_Id.TryParse(JSONToken.ToString(), out ReservationId))
                                                 //{
                                                 //
                                                 //    return SendEVSEReserved(
                                                 //        new HTTPResponseBuilder(Request) {
                                                 //            HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                 //            ContentType     = HTTPContentType.JSON_UTF8,
                                                 //            Content         = new JObject(new JProperty("description", "Invalid ReservationId!")).ToUTF8Bytes()
                                                 //        });
                                                 //
                                                 //}

                                                 #endregion

                                                 #region Parse eMAId               [mandatory]

                                                 if (!JSON.ParseMandatory("eMAId",
                                                                          "e-Mobility account identification",
                                                                          HTTPServer.DefaultServerName,
                                                                          eMobilityAccount_Id.TryParse,
                                                                          out eMAId,
                                                                          Request,
                                                                          out _HTTPResponse))

                                                     return SendEVSERemoteStarted(_HTTPResponse);

                                                 #endregion


                                                 #region Check IntendedCharging     [optional] -> ...

                                                 JSONWrapper IntendedChargingJSON = null;

                                                 if (JSON.AsJSONObject("IntendedCharging", out IntendedChargingJSON))
                                                 {

                                                     if (IntendedChargingJSON == null)
                                                         return SendEVSEReserved(
                                                             new HTTPResponseBuilder(Request) {
                                                                 HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                 ContentType     = HTTPContentType.JSON_UTF8,
                                                                 Content         = new JObject(new JProperty("description", "Invalid IntendedCharging section!")).ToUTF8Bytes()
                                                             });

                                                     #region Check ChargingStartTime    [optional]

                                                     if (IntendedChargingJSON.TryGetValue("StartTime", out JSONToken))
                                                     {

                                                         try
                                                         {
                                                             ChargingStartTime = (DateTime) JSONToken;
                                                         }
                                                         catch (Exception)
                                                         {

                                                            return SendEVSEReserved(
                                                                new HTTPResponseBuilder(Request) {
                                                                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                    ContentType     = HTTPContentType.JSON_UTF8,
                                                                    Content         = new JObject(new JProperty("description", "Invalid IntendedCharging/StartTime!")).ToUTF8Bytes()
                                                                });

                                                         }

                                                     }

                                                     #endregion

                                                     #region Check Duration             [optional]

                                                     if (IntendedChargingJSON.TryGetValue("Duration", out JSONToken))
                                                     {

                                                         try
                                                         {
                                                             CharingDuration = TimeSpan.FromSeconds(Convert.ToUInt32(JSONToken));
                                                         }
                                                         catch (Exception)
                                                         {

                                                            return SendEVSEReserved(
                                                                new HTTPResponseBuilder(Request) {
                                                                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                    ContentType     = HTTPContentType.JSON_UTF8,
                                                                    Content         = new JObject(new JProperty("description", "Invalid IntendedCharging/Duration!")).ToUTF8Bytes()
                                                                });

                                                         }

                                                     }

                                                     #endregion

                                                     #region Check ChargingProductId    [optional]

                                                     if (!JSON.ParseOptionalN("ChargingProductId",
                                                                              "Charging product identification",
                                                                              HTTPServer.DefaultServerName,
                                                                              ChargingProduct_Id.TryParse,
                                                                              out ChargingProductId,
                                                                              Request,
                                                                              out _HTTPResponse))

                                                         return SendEVSEReserved(_HTTPResponse);

                                                     //if (JSON.TryGetValue("ChargingProductId", out JSONToken) &&
                                                     //    !ChargingProduct_Id.TryParse(JSONToken.ToString(), out ChargingProductId))
                                                     //{
                                                     //
                                                     //    return SendEVSEReserved(
                                                     //        new HTTPResponseBuilder(Request) {
                                                     //            HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                     //            ContentType     = HTTPContentType.JSON_UTF8,
                                                     //            Content         = new JObject(new JProperty("description", "Invalid IntendedCharging/ChargingProductId!")).ToUTF8Bytes()
                                                     //        });
                                                     //
                                                     //}

                                                     #endregion

                                                     #region Check Plug                 [optional]

                                                     if (IntendedChargingJSON.TryGetValue("Plug", out JSONToken) &&
                                                         !Enum.TryParse<PlugTypes>(JSONToken.ToString(), out Plug))
                                                     {

                                                         return SendEVSEReserved(
                                                             new HTTPResponseBuilder(Request) {
                                                                 HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                 ContentType     = HTTPContentType.JSON_UTF8,
                                                                 Content         = new JObject(new JProperty("description", "Invalid IntendedCharging/Plug!")).ToUTF8Bytes()
                                                             });

                                                     }

                                                     #endregion

                                                     #region Check Consumption          [optional, kWh]

                                                     if (IntendedChargingJSON.TryGetValue("Consumption", out JSONToken))
                                                     {

                                                         try
                                                         {
                                                             Consumption = Convert.ToUInt32(JSONToken);
        }
                                                         catch (Exception)
                                                         {

                                                            return SendEVSEReserved(
                                                                new HTTPResponseBuilder(Request) {
                                                                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                    ContentType     = HTTPContentType.JSON_UTF8,
                                                                    Content         = new JObject(new JProperty("description", "Invalid IntendedCharging/Consumption!")).ToUTF8Bytes()
                                                                });

                                                         }

                                                     }

                                                     #endregion

                                                 }

                                                 #endregion

                                                 #region Check AuthorizedIds        [optional] -> ...

                                                 JSONWrapper AuthorizedIdsJSON = null;

                                                 if (JSON.AsJSONObject("AuthorizedIds", out AuthorizedIdsJSON))
                                                 {

                                                     if (AuthorizedIdsJSON == null)
                                                         return SendEVSEReserved(
                                                             new HTTPResponseBuilder(Request) {
                                                                 HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                 ContentType     = HTTPContentType.JSON_UTF8,
                                                                 Content         = new JObject(new JProperty("description", "Invalid AuthorizedIds section!")).ToUTF8Bytes()
                                                             });

                                                     #region Check RFIDIds      [optional]

                                                     if (AuthorizedIdsJSON.TryGetValue("RFIDIds", out JSONToken))
                                                     {

                                                         var AuthTokensJSON = JSONToken as JArray;

                                                         if (AuthTokensJSON == null)
                                                             return SendEVSEReserved(
                                                                 new HTTPResponseBuilder(Request) {
                                                                     HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                     ContentType     = HTTPContentType.JSON_UTF8,
                                                                     Content         = new JObject(new JProperty("description", "Invalid AuthorizedIds/RFIDIds section!")).ToUTF8Bytes()
                                                                 });

                                                         foreach (var jtoken in AuthTokensJSON)
                                                         {

                                                             Auth_Token AuthToken = null;

                                                             if (!Auth_Token.TryParse(jtoken.Value<String>(), out AuthToken))
                                                                 return SendEVSEReserved(
                                                                     new HTTPResponseBuilder(Request) {
                                                                         HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                         ContentType     = HTTPContentType.JSON_UTF8,
                                                                         Content         = new JObject(new JProperty("description", "Invalid AuthorizedIds/RFIDId '" + jtoken.Value<String>() + "' section!")).ToUTF8Bytes()
                                                                     });

                                                             AuthTokens.Add(AuthToken);

                                                         }

                                                     }

                                                     #endregion

                                                     #region Check eMAIds       [optional]

                                                     if (AuthorizedIdsJSON.TryGetValue("eMAIds", out JSONToken))
                                                     {

                                                         var eMAIdsJSON = JSONToken as JArray;

                                                         if (eMAIdsJSON == null)
                                                             return SendEVSEReserved(
                                                                 new HTTPResponseBuilder(Request) {
                                                                     HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                     ContentType     = HTTPContentType.JSON_UTF8,
                                                                     Content         = new JObject(new JProperty("description", "Invalid AuthorizedIds/eMAIds section!")).ToUTF8Bytes()
                                                                 });

                                                         foreach (var jtoken in eMAIdsJSON)
                                                         {

                                                             eMobilityAccount_Id? eMAId2 = null;

                                                             if (!eMobilityAccount_Id.TryParse(jtoken.Value<String>(), out eMAId2))
                                                                 return SendEVSEReserved(
                                                                     new HTTPResponseBuilder(Request) {
                                                                         HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                                         ContentType     = HTTPContentType.JSON_UTF8,
                                                                         Content         = new JObject(new JProperty("description", "Invalid AuthorizedIds/eMAIds '" + jtoken.Value<String>() + "' section!")).ToUTF8Bytes()
                                                                     });

                                                             eMAIds.Add(eMAId2.Value);

                                                         }

                                                     }

                                                     #endregion

                                                     #region Check PINs         [optional]

                                                     //if (AuthorizedIdsJSON.TryGetValue("PINs", out JSONToken))
                                                     //{

                                                     //    var PINsJSON = JSONToken as JArray;

                                                     //    if (PINsJSON == null)
                                                     //        return SendEVSEReserved(
                                                     //            new HTTPResponseBuilder(Request) {
                                                     //                HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                     //                ContentType     = HTTPContentType.JSON_UTF8,
                                                     //                Content         = new JObject(new JProperty("description", "Invalid AuthorizedIds/PINs section!")).ToUTF8Bytes()
                                                     //            });

                                                     //    foreach (var jtoken in PINsJSON)
                                                     //    {

                                                     //        UInt32 PIN = 0;

                                                     //        if (!UInt32.TryParse(jtoken.Value<String>(), out PIN))
                                                     //            return SendEVSEReserved(
                                                     //                new HTTPResponseBuilder(Request) {
                                                     //                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                                                     //                    ContentType     = HTTPContentType.JSON_UTF8,
                                                     //                    Content         = new JObject(new JProperty("description", "Invalid AuthorizedIds/PINs '" + jtoken.Value<String>() + "' section!")).ToUTF8Bytes()
                                                     //                });

                                                     //        PINs.Add(PIN);

                                                     //    }

                                                     //}

                                                     #endregion

                                                 }

                                                 #endregion

                                             }

                                             #endregion


                                             var result = await EMSP.Reserve(EVSEId,
                                                                             StartTime,
                                                                             Duration,
                                                                             ReservationId,
                                                                             eMAId,
                                                                             ChargingProductId.HasValue    // of IntendedCharging
                                                                                 ? new ChargingProduct(ChargingProductId.Value)
                                                                                 : null,
                                                                             AuthTokens,
                                                                             eMAIds,
                                                                             PINs,

                                                                             Request.Timestamp,
                                                                             Request.CancellationToken,
                                                                             Request.EventTrackingId);


                                             switch (result.Result)
                                             {

                                                 #region Success

                                                 case ReservationResultType.Success:
                                                     return SendEVSEReserved(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Created,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             Location                   = "~/ext/BoschEBike/Reservations/" + result.Reservation.Id.ToString(),
                                                             Connection                 = "close",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("ReservationId",           result.Reservation.Id.       ToString()),
                                                                                              new JProperty("StartTime",               result.Reservation.StartTime.ToIso8601()),
                                                                                              new JProperty("Duration",       (UInt32) result.Reservation.Duration. TotalSeconds),
                                                                                              //new JProperty("Level",                   result.Reservation.ReservationLevel.ToString()),
                                                                                              //new JProperty("EVSEId",                  result.Reservation.EVSEId.   ToString()),

                                                                                              (result.Reservation.AuthTokens.Any() ||
                                                                                               result.Reservation.eMAIds.    Any() ||
                                                                                               result.Reservation.PINs.      Any())
                                                                                                   ? new JProperty("AuthorizedIds", JSONObject.Create(

                                                                                                         result.Reservation.AuthTokens.Any()
                                                                                                             ? new JProperty("RFIDIds", new JArray(result.Reservation.AuthTokens.Select(v => v.ToString())))
                                                                                                             : null,

                                                                                                         result.Reservation.eMAIds.Any()
                                                                                                             ? new JProperty("eMAIds",  new JArray(result.Reservation.eMAIds. Select(v => v.ToString())))
                                                                                                             : null,

                                                                                                         result.Reservation.PINs.Any()
                                                                                                             ? new JProperty("PINs",    new JArray(result.Reservation.PINs.   Select(v => v.ToString())))
                                                                                                             : null))

                                                                                                   : null

                                                                                          ).ToUTF8Bytes()
                                                     });

                                                 #endregion

                                                 #region InvalidCredentials

                                                 case ReservationResultType.InvalidCredentials:
                                                     return SendEVSEReserved(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Unauthorized,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "Unauthorized remote start or invalid credentials!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                                 #region UnknownChargingReservationId

                                                 case ReservationResultType.UnknownChargingReservationId:
                                                     return SendEVSEReserved(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.NotFound,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "Unknown reservation identification!")
                                                                                          ).ToUTF8Bytes(),
                                                             Connection                 = "close"
                                                         });

                                                 #endregion

                                                 #region UnknownEVSE

                                                 case ReservationResultType.UnknownEVSE:
                                                     return SendEVSEReserved(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.NotFound,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "Unknown EVSE!")
                                                                                          ).ToUTF8Bytes(),
                                                             Connection                 = "close"
                                                         });

                                                 #endregion

                                                 #region AlreadyReserved

                                                 case ReservationResultType.AlreadyReserved:
                                                     return SendEVSEReserved(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Conflict,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "The EVSE is already reserved!")
                                                                                          ).ToUTF8Bytes(),
                                                             Connection                 = "close"
                                                         });

                                                 #endregion

                                                 #region AlreadyInUse

                                                 case ReservationResultType.AlreadyInUse:
                                                     return SendEVSEReserved(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Conflict,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "The EVSE is already in use!")
                                                                                          ).ToUTF8Bytes(),
                                                             Connection                 = "close"
                                                         });

                                                 #endregion

                                                 #region OutOfService

                                                 case ReservationResultType.OutOfService:
                                                     return SendEVSEReserved(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Conflict,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "The EVSE is out of service!")
                                                                                          ).ToUTF8Bytes(),
                                                             Connection                 = "close"
                                                         });

                                                 #endregion

                                                 #region Timeout

                                                 case ReservationResultType.Timeout:
                                                     return SendEVSEReserved(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.RequestTimeout,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "The request did not succeed within the given time!")
                                                                                          ).ToUTF8Bytes(),
                                                             Connection                 = "close"
                                                         });

                                                 #endregion

                                                 #region default => BadRequest

                                                 default:
                                                     return SendEVSEReserved(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.BadRequest,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "No reservation was possible!")
                                                                                          ).ToUTF8Bytes(),
                                                             Connection                 = "close"
                                                         });

                                                 #endregion

                                             }

                                         });

            #endregion

            #region REMOTESTART  ~/EVSEs/{EVSEId}

            // -----------------------------------------------------------------------
            // curl -v -k \
            //      -X REMOTESTART \
            //      -u chargingmap:gf0c31j08ufgw3j9w3t \
            //      -H "Content-Type: application/json" \
            //      -H "Accept:       application/json" \
            //      -d "{ \"eMAId\": \"DE*ICE*I00811*1\" }" \
            //      http://127.0.0.1:3004/RNs/Prod/EVSEs/DE*822*555555*100*1
            // -----------------------------------------------------------------------
            HTTPServer.AddMethodCallback(HTTPHostname,
                                         REMOTESTART,
                                         URIPrefix + "/EVSEs/{EVSEId}",
                                         HTTPContentType.JSON_UTF8,
                                         HTTPDelegate: async Request => {

                                             SendRemoteStartEVSE(Request);

                                             #region Check HTTP Basic Authentication

                                             if (Request.Authorization          == null        ||
                                                 Request.Authorization.Username != HTTPLogin   ||
                                                 Request.Authorization.Password != HTTPPassword)
                                             {

                                                 return SendEVSERemoteStarted(
                                                     new HTTPResponseBuilder(Request) {
                                                         HTTPStatusCode   = HTTPStatusCode.Unauthorized,
                                                         WWWAuthenticate  = @"Basic realm=""" + WWWAuthenticationRealm + @"""",
                                                         Server           = HTTPServer.DefaultServerName,
                                                         Date             = DateTime.Now,
                                                         Connection       = "close"
                                                     });

                                             }

                                             #endregion

                                             #region Get EVSEId URI parameter

                                             HTTPResponse  _HTTPResponse;
                                             EVSE_Id       EVSEId;

                                             if (!Request.ParseEVSEId(DefaultHTTPServerName,
                                                                      out EVSEId,
                                                                      out _HTTPResponse))
                                             {
                                                 return SendEVSERemoteStarted(_HTTPResponse);
                                             }

                                             #endregion

                                             #region Parse JSON  [mandatory]

                                             JSONWrapper              JSON               = null;
                                             ChargingProduct_Id?      ChargingProductId  = null;
                                             ChargingReservation_Id?  ReservationId      = null;
                                             ChargingSession_Id       SessionId          = default(ChargingSession_Id);
                                             eMobilityAccount_Id?     eMAId              = null;

                                             if (!Request.TryParseJObjectRequestBody(out JSON,
                                                                                     out _HTTPResponse,
                                                                                     AllowEmptyHTTPBody: false))

                                             {

                                                 #region Check ChargingProductId  [optional]

                                                 if (!JSON.ParseOptionalN("ChargingProductId",
                                                                          "Charging product identification",
                                                                          HTTPServer.DefaultServerName,
                                                                          ChargingProduct_Id.TryParse,
                                                                          out ChargingProductId,
                                                                          Request,
                                                                          out _HTTPResponse))

                                                     return SendEVSERemoteStarted(_HTTPResponse);

                                                 #endregion

                                                 #region Check ReservationId      [optional]

                                                 if (!JSON.ParseOptionalN("ReservationId",
                                                                          "Charging reservation identification",
                                                                          HTTPServer.DefaultServerName,
                                                                          ChargingReservation_Id.TryParse,
                                                                          out ReservationId,
                                                                          Request,
                                                                          out _HTTPResponse))

                                                     return SendEVSERemoteStarted(_HTTPResponse);

                                                 #endregion

                                                 #region Parse SessionId          [optional]

                                                 if (!JSON.ParseOptional("SessionId",
                                                                         "Charging session identification",
                                                                         HTTPServer.DefaultServerName,
                                                                         ChargingSession_Id.TryParse,
                                                                         out SessionId,
                                                                         Request,
                                                                         out _HTTPResponse))

                                                     return SendEVSERemoteStarted(_HTTPResponse);

                                                 #endregion

                                                 #region Parse eMAId             [mandatory]

                                                 if (!JSON.ParseMandatory("eMAId",
                                                                          "e-Mobility account identification",
                                                                          HTTPServer.DefaultServerName,
                                                                          eMobilityAccount_Id.TryParse,
                                                                          out eMAId,
                                                                          Request,
                                                                          out _HTTPResponse))

                                                     return SendEVSERemoteStarted(_HTTPResponse);

                                                 #endregion

                                             }

                                             else
                                                 return SendEVSERemoteStarted(_HTTPResponse);

                                             #endregion


                                             var response = await EMSP.RemoteStart(EVSEId,
                                                                                   ChargingProductId.HasValue
                                                                                       ? new ChargingProduct(ChargingProductId.Value)
                                                                                       : null,
                                                                                   ReservationId,
                                                                                   SessionId,
                                                                                   eMAId,

                                                                                   Request.Timestamp,
                                                                                   Request.CancellationToken,
                                                                                   Request.EventTrackingId);


                                             switch (response.Result)
                                             {

                                                 #region Success

                                                 case RemoteStartEVSEResultType.Success:
                                                     return SendEVSERemoteStarted(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Created,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("SessionId",  response?.Session?.Id.ToString())
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                                 #region InvalidCredentials

                                                 case RemoteStartEVSEResultType.InvalidCredentials:
                                                     return SendEVSERemoteStarted(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Unauthorized,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "Unauthorized remote start or invalid credentials!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                                 #region AlreadyInUse

                                                 case RemoteStartEVSEResultType.AlreadyInUse:
                                                     return SendEVSERemoteStarted(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Conflict,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "The EVSE is already in use!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                                 #region Reserved

                                                 case RemoteStartEVSEResultType.Reserved:
                                                     return SendEVSERemoteStarted(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Conflict,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description", response.Message.IsNotNullOrEmpty() ? response.Message : "The EVSE is reserved!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                                 #region OutOfService

                                                 case RemoteStartEVSEResultType.OutOfService:
                                                     return SendEVSERemoteStarted(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Conflict,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "The EVSE is out of service!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                                 #region Timeout

                                                 case RemoteStartEVSEResultType.Timeout:
                                                     return SendEVSERemoteStarted(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.RequestTimeout,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              new JProperty("description",  "The request did not succeed within the given period of time!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                                 #region default => BadRequest

                                                 default:
                                                     return SendEVSERemoteStarted(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.BadRequest,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              response.Session != null
                                                                                                  ? new JProperty("SessionId",  response.Session.Id.ToString())
                                                                                                  : null,
                                                                                              new JProperty("Result",      response.Result.ToString()),
                                                                                              new JProperty("description", response.Message.IsNotNullOrEmpty() ? response.Message : "General error!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                             }

                                         });

            #endregion

            #region REMOTESTOP   ~/EVSEs/{EVSEId}

            // -----------------------------------------------------------------------
            // curl -v -k \
            //      -X REMOTESTOP \
            //      -u chargingmap:gf0c31j08ufgw3j9w3t \
            //      -H "Content-Type: application/json" \
            //      -H "Accept:       application/json" \
            //      -d "{ \"SessionId\": \"60ce73f6-0a88-1296-3d3d-623fdd276ddc\" }" \
            //      http://127.0.0.1:3004/RNs/Prod/EVSEs/DE*822*555555*100*1
            // -----------------------------------------------------------------------
            HTTPServer.AddMethodCallback(HTTPHostname,
                                         REMOTESTOP,
                                         URIPrefix + "/EVSEs/{EVSEId}",
                                         HTTPContentType.JSON_UTF8,
                                         HTTPDelegate: async Request => {

                                             SendRemoteStopEVSE(Request);

                                             #region Check HTTP Basic Authentication

                                             if (Request.Authorization          == null        ||
                                                 Request.Authorization.Username != HTTPLogin   ||
                                                 Request.Authorization.Password != HTTPPassword)
                                             {

                                                 return SendEVSERemoteStopped(
                                                     new HTTPResponseBuilder(Request) {
                                                         HTTPStatusCode   = HTTPStatusCode.Unauthorized,
                                                         WWWAuthenticate  = @"Basic realm=""" + WWWAuthenticationRealm + @"""",
                                                         Server           = HTTPServer.DefaultServerName,
                                                         Date             = DateTime.Now,
                                                         Connection       = "close"
                                                     });

                                             }

                                             #endregion

                                             #region Get EVSEId URI parameter

                                             HTTPResponse  _HTTPResponse;
                                             EVSE_Id       EVSEId;

                                             if (!Request.ParseEVSEId(DefaultHTTPServerName,
                                                                      out EVSEId,
                                                                      out _HTTPResponse))
                                             {
                                                 return SendEVSERemoteStarted(_HTTPResponse);
                                             }

                                             #endregion

                                             #region Parse JSON  [mandatory]

                                             JSONWrapper           JSON       = null;
                                             ChargingSession_Id    SessionId  = default(ChargingSession_Id);
                                             eMobilityAccount_Id?  eMAId      = null;

                                             if (!Request.TryParseJObjectRequestBody(out JSON,
                                                                                     out _HTTPResponse,
                                                                                     AllowEmptyHTTPBody: false))

                                             {

                                                 #region Parse SessionId         [mandatory]

                                                 if (!JSON.ParseMandatory("SessionId",
                                                                          "Charging session identification",
                                                                          HTTPServer.DefaultServerName,
                                                                          ChargingSession_Id.TryParse,
                                                                          out SessionId,
                                                                          Request,
                                                                          out _HTTPResponse))

                                                     return SendEVSERemoteStarted(_HTTPResponse);

                                                 #endregion

                                                 #region Parse eMAId              [optional]

                                                 if (!JSON.ParseOptional("eMAId",
                                                                          "e-Mobility account identification",
                                                                          HTTPServer.DefaultServerName,
                                                                          eMobilityAccount_Id.TryParse,
                                                                          out eMAId,
                                                                          Request,
                                                                          out _HTTPResponse))

                                                     return SendEVSERemoteStarted(_HTTPResponse);

                                                 #endregion

                                                 // ReservationHandling

                                             }

                                             else
                                                 return SendEVSERemoteStarted(_HTTPResponse);

                                             #endregion


                                             var response = await EMSP.RemoteStop(EVSEId,
                                                                                  SessionId,
                                                                                  ReservationHandling.Close, //ReservationHandling.KeepAlive(TimeSpan.FromMinutes(1)), // ToDo: Parse this property!
                                                                                  eMAId,

                                                                                  Request.Timestamp,
                                                                                  Request.CancellationToken,
                                                                                  Request.EventTrackingId);


                                             switch (response.Result)
                                             {

                                                 #region Success

                                                 case RemoteStopEVSEResultType.Success:

                                                     if (response.ReservationHandling             == null ||
                                                         response.ReservationHandling.IsKeepAlive == false)
                                                         return SendEVSERemoteStopped(
                                                             new HTTPResponseBuilder(Request) {
                                                                 HTTPStatusCode             = HTTPStatusCode.NoContent,
                                                                 Server                     = HTTPServer.DefaultServerName,
                                                                 Date                       = DateTime.Now,
                                                                 AccessControlAllowOrigin   = "*",
                                                                 AccessControlAllowMethods  = "POST",
                                                                 AccessControlAllowHeaders  = "Content-Type, Accept, Authorization"
                                                             });

                                                     else
                                                         return SendEVSERemoteStopped(
                                                             new HTTPResponseBuilder(Request) {
                                                                 HTTPStatusCode             = HTTPStatusCode.OK,
                                                                 Server                     = HTTPServer.DefaultServerName,
                                                                 Date                       = DateTime.Now,
                                                                 AccessControlAllowOrigin   = "*",
                                                                 AccessControlAllowMethods  = "POST",
                                                                 AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                                 ContentType                = HTTPContentType.JSON_UTF8,
                                                                 Content                    = new JObject(
                                                                                                  new JProperty("KeepAlive", (Int32) response.ReservationHandling.KeepAliveTime.Value.TotalSeconds)
                                                                                              ).ToUTF8Bytes()
                                                             });

                                                 #endregion

                                                 #region InvalidCredentials

                                                 case RemoteStopEVSEResultType.InvalidCredentials:
                                                     return SendEVSERemoteStopped(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Unauthorized,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = new JObject(
                                                                                              new JProperty("description", "Unauthorized remote start or invalid credentials!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                                 #region InvalidSessionId

                                                 case RemoteStopEVSEResultType.InvalidSessionId:
                                                     return SendEVSERemoteStopped(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Conflict,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = new JObject(
                                                                                              new JProperty("description", "Invalid SessionId!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                                 #region OutOfService

                                                 case RemoteStopEVSEResultType.OutOfService:
                                                     return SendEVSERemoteStopped(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Conflict,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = new JObject(
                                                                                              new JProperty("description", "EVSE is out of service!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                                 #region Offline

                                                 case RemoteStopEVSEResultType.Offline:
                                                     return SendEVSERemoteStopped(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.Conflict,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = new JObject(
                                                                                              new JProperty("description", "EVSE is offline!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                                 #region default => BadRequest

                                                 default:
                                                     return SendEVSERemoteStopped(
                                                         new HTTPResponseBuilder(Request) {
                                                             HTTPStatusCode             = HTTPStatusCode.BadRequest,
                                                             Server                     = HTTPServer.DefaultServerName,
                                                             Date                       = DateTime.Now,
                                                             AccessControlAllowOrigin   = "*",
                                                             AccessControlAllowMethods  = "POST",
                                                             AccessControlAllowHeaders  = "Content-Type, Accept, Authorization",
                                                             ContentType                = HTTPContentType.JSON_UTF8,
                                                             Content                    = JSONObject.Create(
                                                                                              response.SessionId != null
                                                                                                  ? new JProperty("SessionId",  response.SessionId.ToString())
                                                                                                  : null,
                                                                                              new JProperty("Result",      response.Result.ToString()),
                                                                                              new JProperty("description", response.Message.IsNotNullOrEmpty() ? response.Message : "General error!")
                                                                                          ).ToUTF8Bytes()
                                                         });

                                                 #endregion

                                             }

                                         });

            #endregion


        }

        #endregion



        #region (internal) SendReserveEVSE(Request)

        internal HTTPRequest SendReserveEVSE(HTTPRequest Request)
        {

            OnReserveEVSELog?.Invoke(Request.Timestamp,
                                     this.HTTPServer,
                                     Request);

            return Request;

        }

        #endregion

        #region (internal) SendEVSEReserved(Response)

        internal HTTPResponse SendEVSEReserved(HTTPResponse Response)
        {

            OnEVSEReservedLog?.Invoke(Response.Timestamp,
                                      this.HTTPServer,
                                      Response.HTTPRequest,
                                      Response);

            return Response;

        }

        #endregion


        #region (protected internal) SendReservationCancel(Request)

        protected internal HTTPRequest SendReservationCancel(HTTPRequest Request)
        {

            OnReservationCancel?.Invoke(Request.Timestamp,
                                        this.HTTPServer,
                                        Request);

            return Request;

        }

        #endregion

        #region (internal) SendCancelReservation(...)

        internal async Task<CancelReservationResult>

            SendCancelReservation(DateTime                               Timestamp,
                                  CancellationToken                      CancellationToken,
                                  EventTracking_Id                       EventTrackingId,
                                  ChargingReservation_Id                 ReservationId,
                                  ChargingReservationCancellationReason  Reason,
                                  TimeSpan?                              QueryTimeout  = null)

        {

            var OnCancelReservationLocal = OnCancelReservation;
            if (OnCancelReservationLocal == null)
                return CancelReservationResult.Error(ReservationId,
                                                     Reason);

            var results = await Task.WhenAll(OnCancelReservationLocal.
                                                 GetInvocationList().
                                                 Select(subscriber => (subscriber as OnCancelReservationDelegate)
                                                     (Timestamp,
                                                      this,
                                                      CancellationToken,
                                                      EventTrackingId,
                                                      ReservationId,
                                                      Reason,
                                                      QueryTimeout)));

            return results.
                   //    Where(result => result.Result != RemoteStopEVSEResultType.Unspecified).
                       First();

        }

        #endregion

        #region (protected internal) SendReservationCancelled(Response)

        protected internal HTTPResponse SendReservationCancelled(HTTPResponse Response)
        {

            OnReservationCancelled?.Invoke(Response.Timestamp,
                                           this.HTTPServer,
                                           Response.HTTPRequest,
                                           Response);

            return Response;

        }

        #endregion


        #region (protected internal) SendRemoteStartEVSE(Request)

        protected internal HTTPRequest SendRemoteStartEVSE(HTTPRequest Request)
        {

            OnRemoteStartEVSELog?.Invoke(Request.Timestamp,
                                         this.HTTPServer,
                                         Request);

            return Request;

        }

        #endregion

        #region (protected internal) SendEVSERemoteStarted(Response)

        protected internal HTTPResponse SendEVSERemoteStarted(HTTPResponse Response)
        {

            OnEVSERemoteStartedLog?.Invoke(Response.Timestamp,
                                           this.HTTPServer,
                                           Response.HTTPRequest,
                                           Response);

            return Response;

        }

        #endregion


        #region (protected internal) SendRemoteStopEVSE(Request)

        protected internal HTTPRequest SendRemoteStopEVSE(HTTPRequest Request)
        {

            OnRemoteStopEVSELog?.Invoke(Request.Timestamp,
                                        this.HTTPServer,
                                        Request);

            return Request;

        }

        #endregion

        #region (protected internal) SendEVSERemoteStopped(Response)

        protected internal HTTPResponse SendEVSERemoteStopped(HTTPResponse Response)
        {

            OnEVSERemoteStoppedLog?.Invoke(Response.Timestamp,
                                           this.HTTPServer,
                                           Response.HTTPRequest,
                                           Response);

            return Response;

        }

        #endregion


        public void Start()
        {
            HTTPServer.Start();
        }

    }

}
