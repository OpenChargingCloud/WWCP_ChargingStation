/*
 * Copyright (c) 2014-2018 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using org.GraphDefined.WWCP;

#endregion

namespace org.GraphDefined.WWCP.SmartCity
{

    /// <summary>
    /// Belectric Map I/O.
    /// </summary>
    public static class BelectricMap_IO
    {

        #region ParseChargingPoolId(this HTTPRequest, DefaultServerName, out ChargingPoolId, out HTTPResponse)

        public static Boolean ParseChargingPoolId(this HTTPRequest     HTTPRequest,
                                                  String               DefaultServerName,
                                                  out ChargingPool_Id  ChargingPoolId,
                                                  out HTTPResponse     HTTPResponse)
        {

            HTTPResponse = null;

            if (HTTPRequest.ParsedURIParameters.Length < 1)
            {

                HTTPResponse = new HTTPResponse.Builder(HTTPRequest) {
                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                    Server          = DefaultServerName,
                    Date            = DateTime.Now
                };

                ChargingPoolId = default(ChargingPool_Id);
                return false;

            }

            if (!ChargingPool_Id.TryParse(HTTPRequest.ParsedURIParameters[0], out ChargingPoolId))
            {

                HTTPResponse = new HTTPResponse.Builder(HTTPRequest) {
                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                    Server          = DefaultServerName,
                    Date            = DateTime.Now,
                    ContentType     = HTTPContentType.JSON_UTF8,
                    Content         = @"{ ""description"": ""Invalid charging pool identification!"" }".ToUTF8Bytes()
                };

                return false;

            }

            return true;

        }

        #endregion

        #region ParseChargingStationId(this HTTPRequest, DefaultServerName, out ChargingStationId, out HTTPResponse)

        public static Boolean ParseChargingStationId(this HTTPRequest        HTTPRequest,
                                                     String                  DefaultServerName,
                                                     out ChargingStation_Id  ChargingStationId,
                                                     out HTTPResponse        HTTPResponse)
        {

            HTTPResponse       = null;

            if (HTTPRequest.ParsedURIParameters.Length < 1)
            {

                HTTPResponse = new HTTPResponse.Builder(HTTPRequest) {
                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                    Server          = DefaultServerName,
                    Date            = DateTime.Now
                };

                ChargingStationId = default(ChargingStation_Id);

                return false;

            }

            if (!ChargingStation_Id.TryParse(HTTPRequest.ParsedURIParameters[0], out ChargingStationId))
            {

                HTTPResponse = new HTTPResponse.Builder(HTTPRequest) {
                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                    Server          = DefaultServerName,
                    Date            = DateTime.Now,
                    ContentType     = HTTPContentType.JSON_UTF8,
                    Content         = @"{ ""description"": ""Invalid charging station identification!"" }".ToUTF8Bytes()
                };

                return false;

            }

            return true;

        }

        #endregion

        #region ParseEVSEId(this HTTPRequest, DefaultServerName, out EVSEId, out HTTPResponse)

        public static Boolean ParseEVSEId(this HTTPRequest  HTTPRequest,
                                          String            DefaultServerName,
                                          out EVSE_Id       EVSEId,
                                          out HTTPResponse  HTTPResponse)
        {

            EVSEId        = default(EVSE_Id);
            HTTPResponse  = null;

            if (HTTPRequest.ParsedURIParameters.Length < 1)
            {

                HTTPResponse = new HTTPResponse.Builder(HTTPRequest) {
                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                    Server          = DefaultServerName,
                    Date            = DateTime.Now
                };

                return false;

            }

            if (!EVSE_Id.TryParse(HTTPRequest.ParsedURIParameters[0], out EVSEId))
            {

                HTTPResponse = new HTTPResponse.Builder(HTTPRequest) {
                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                    Server          = DefaultServerName,
                    Date            = DateTime.Now,
                    ContentType     = HTTPContentType.JSON_UTF8,
                    Content         = @"{ ""description"": ""Invalid EVSE identification!"" }".ToUTF8Bytes()
                };

                return false;

            }

            return true;

        }

        #endregion



        #region ParseChargingReservationId(this HTTPRequest, DefaultServerName, out ChargingReservationId, out HTTPResponse)

        public static Boolean ParseChargingReservationId(this HTTPRequest            HTTPRequest,
                                                         String                      DefaultServerName,
                                                         out ChargingReservation_Id  ChargingReservationId,
                                                         out HTTPResponse            HTTPResponse)
        {

            ChargingReservationId  = default(ChargingReservation_Id);
            HTTPResponse           = null;

            if (HTTPRequest.ParsedURIParameters.Length < 1)
            {

                HTTPResponse = new HTTPResponse.Builder(HTTPRequest) {
                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                    Server          = DefaultServerName,
                    Date            = DateTime.Now
                };

                return false;

            }

            if (!ChargingReservation_Id.TryParse(HTTPRequest.ParsedURIParameters[0], out ChargingReservationId))
            {

                HTTPResponse = new HTTPResponse.Builder(HTTPRequest) {
                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                    Server          = DefaultServerName,
                    Date            = DateTime.Now,
                    ContentType     = HTTPContentType.JSON_UTF8,
                    Content         = @"{ ""description"": ""Invalid charging reservation identification!"" }".ToUTF8Bytes()
                };

                return false;

            }

            return true;

        }

        #endregion

        #region ParseChargingReservation(this HTTPRequest, DefaultServerName, RoamingNetwork, out ChargingReservation, out HTTPResponse)

        public static Boolean ParseChargingReservation(this HTTPRequest         HTTPRequest,
                                                       String                   DefaultServerName,
                                                       RoamingNetwork           RoamingNetwork,
                                                       out ChargingReservation  ChargingReservation,
                                                       out HTTPResponse         HTTPResponse)
        {

            var ChargingReservationId  = default(ChargingReservation_Id);
                ChargingReservation    = null;

            if (!HTTPRequest.ParseChargingReservationId(DefaultServerName,
                                                        out ChargingReservationId,
                                                        out HTTPResponse))
                return false;


            if (!RoamingNetwork.TryGetReservationById(ChargingReservationId, out ChargingReservation))
            {

                HTTPResponse = new HTTPResponse.Builder(HTTPRequest) {
                    HTTPStatusCode  = HTTPStatusCode.NotFound,
                    Server          = DefaultServerName,
                    Date            = DateTime.Now,
                    ContentType     = HTTPContentType.JSON_UTF8,
                    Content         = @"{ ""description"": ""Unknown charging reservation identification!"" }".ToUTF8Bytes()
                };

                return false;

            }

            return true;

        }

        #endregion


        #region ParseChargingSessionId(this HTTPRequest, DefaultServerName, RoamingNetwork, out ChargingSessionId, out HTTPResponse)

        public static Boolean ParseChargingSessionId(this HTTPRequest        HTTPRequest,
                                                     String                  DefaultServerName,
                                                     RoamingNetwork          RoamingNetwork,
                                                     out ChargingSession_Id  ChargingSessionId,
                                                     out HTTPResponse        HTTPResponse)
        {

            var ChargingReservationId  = default(ChargingReservation_Id);
                ChargingSessionId      = default(ChargingSession_Id);
                HTTPResponse           = null;

            if (HTTPRequest.ParsedURIParameters.Length < 1)
            {

                HTTPResponse = new HTTPResponse.Builder(HTTPRequest) {
                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                    Server          = DefaultServerName,
                    Date            = DateTime.Now
                };

                return false;

            }

            if (!ChargingSession_Id.TryParse(HTTPRequest.ParsedURIParameters[0], out ChargingSessionId))
            {

                HTTPResponse = new HTTPResponse.Builder(HTTPRequest) {
                    HTTPStatusCode  = HTTPStatusCode.BadRequest,
                    Server          = DefaultServerName,
                    Date            = DateTime.Now,
                    ContentType     = HTTPContentType.JSON_UTF8,
                    Content         = @"{ ""description"": ""Invalid charging session identification!"" }".ToUTF8Bytes()
                };

                return false;

            }

            return true;

        }

        #endregion

    }

}
