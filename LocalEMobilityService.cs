/*
 * Copyright (c) 2014-2015 GraphDefined GmbH
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
using System.Collections.Concurrent;

#endregion

namespace org.GraphDefined.WWCP.LocalService
{

    /// <summary>
    /// A local E-Mobility service implementation.
    /// </summary>
    public class LocalEMobilityService : IAuthServices
    {

        #region Data

        private readonly ConcurrentDictionary<Auth_Token,         TokenAuthorizationResultType>  AuthorizationDatabase;
        private readonly ConcurrentDictionary<ChargingSession_Id, SessionInfo>                   SessionDatabase;

        #endregion

        #region Properties

        #region RoamingNetwork

        private readonly RoamingNetwork _RoamingNetwork;

        public RoamingNetwork RoamingNetwork
        {
            get
            {
                return _RoamingNetwork;
            }
        }

        #endregion

        #region EVSP

        private readonly EVSP _EVSP;

        public EVSP EVSP
        {
            get
            {
                return _EVSP;
            }
        }

        #endregion

        #region AuthorizatorId

        private readonly Authorizator_Id _AuthorizatorId;

        public Authorizator_Id AuthorizatorId
        {
            get
            {
                return _AuthorizatorId;
            }
        }

        #endregion


        #region AllTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> AllTokens
        {
            get
            {
                return AuthorizationDatabase;
            }
        }

        #endregion

        #region AuthorizedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> AuthorizedTokens
        {
            get
            {
                return AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.Authorized);
            }
        }

        #endregion

        #region NotAuthorizedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> NotAuthorizedTokens
        {
            get
            {
                return AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.NotAuthorized);
            }
        }

        #endregion

        #region BlockedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> BlockedTokens
        {
            get
            {
                return AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.Blocked);
            }
        }

        #endregion

        #endregion

        #region Events

        #endregion

        #region Constructor(s)

        internal LocalEMobilityService(EVSP             EVSP,
                                       Authorizator_Id  AuthorizatorId = null)
        {

            this._RoamingNetwork        = EVSP.RoamingNetwork;
            this._EVSP                  = EVSP;
            this._AuthorizatorId        = (AuthorizatorId == null) ? Authorizator_Id.Parse("eMI3 Local E-Mobility Database") : AuthorizatorId;

            this.AuthorizationDatabase  = new ConcurrentDictionary<Auth_Token,         TokenAuthorizationResultType>();
            this.SessionDatabase        = new ConcurrentDictionary<ChargingSession_Id, SessionInfo>();

            EVSP.EMobilityService = this;

        }

        #endregion


        // User and credential management

        #region AddToken(Token, AuthenticationResult = AuthenticationResult.Allowed)

        public Boolean AddToken(Auth_Token                    Token,
                                TokenAuthorizationResultType  AuthenticationResult = TokenAuthorizationResultType.Authorized)
        {

            if (!AuthorizationDatabase.ContainsKey(Token))
                return AuthorizationDatabase.TryAdd(Token, AuthenticationResult);

            return false;

        }

        #endregion

        #region RemoveToken(Token)

        public Boolean RemoveToken(Auth_Token Token)
        {

            TokenAuthorizationResultType _AuthorizationResult;

            return AuthorizationDatabase.TryRemove(Token, out _AuthorizationResult);

        }

        #endregion


        // Incoming from the roaming network

        #region AuthorizeStart(OperatorId, AuthToken, EVSEId = null, ChargingProductId = null, SessionId = null, QueryTimeout = null)

        /// <summary>
        /// Create an authorize start request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">An optional EVSE identification.</param>
        /// <param name="ChargingProductId">An optional charging product identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<AuthStartEVSEResult> AuthorizeStart(EVSEOperator_Id     OperatorId,
                                                              Auth_Token          AuthToken,
                                                              EVSE_Id             EVSEId             = null,
                                                              ChargingProduct_Id  ChargingProductId  = null,
                                                              ChargingSession_Id  SessionId          = null,
                                                              TimeSpan?           QueryTimeout       = null)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException("OperatorId", "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException("AuthToken",  "The given parameter must not be null!");

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    var _SessionId = ChargingSession_Id.New;

                    SessionDatabase.TryAdd(_SessionId, new SessionInfo(AuthToken));

                    return AuthStartEVSEResult.Authorized(AuthorizatorId,
                                                          _SessionId,
                                                          EVSP.Id);

                }

                #endregion

                #region Token is blocked!

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStartEVSEResult.Blocked(AuthorizatorId,
                                                       EVSP.Id,
                                                       "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStartEVSEResult.Unspecified(AuthorizatorId);

                #endregion

            }

            #region Unkown Token!

            return AuthStartEVSEResult.NotAuthorized(AuthorizatorId,
                                                     EVSP.Id,
                                                     "Unkkown token!");

            #endregion

        }

        #endregion

        #region AuthorizeStart(OperatorId, AuthToken, ChargingStationId, ChargingProductId = null, SessionId = null, QueryTimeout = null)

        /// <summary>
        /// Create an AuthorizeStart request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingStationId">A charging station identification.</param>
        /// <param name="ChargingProductId">An optional charging product identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<AuthStartChargingStationResult>

            AuthorizeStart(EVSEOperator_Id     OperatorId,
                           Auth_Token          AuthToken,
                           ChargingStation_Id  ChargingStationId,
                           ChargingProduct_Id  ChargingProductId  = null,   // [maxlength: 100]
                           ChargingSession_Id  SessionId          = null,
                           TimeSpan?           QueryTimeout       = null)

        {

            #region Initial checks

            if (OperatorId        == null)
                throw new ArgumentNullException("OperatorId",         "The given parameter must not be null!");

            if (AuthToken         == null)
                throw new ArgumentNullException("AuthToken",          "The given parameter must not be null!");

            if (ChargingStationId == null)
                throw new ArgumentNullException("ChargingStationId",  "The given parameter must not be null!");

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    var _SessionId = ChargingSession_Id.New;

                    SessionDatabase.TryAdd(_SessionId, new SessionInfo(AuthToken));

                    return AuthStartChargingStationResult.Authorized(AuthorizatorId,
                                                                     _SessionId,
                                                                     EVSP.Id);

                }

                #endregion

                #region Token is blocked!

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStartChargingStationResult.Blocked(AuthorizatorId,
                                                                  EVSP.Id,
                                                                  "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStartChargingStationResult.Unspecified(AuthorizatorId);

                #endregion

            }

            #region Unkown Token!

            return AuthStartChargingStationResult.NotAuthorized(AuthorizatorId,
                                                                EVSP.Id,
                                                                "Unkkown token!");

            #endregion

        }

        #endregion

        #region AuthorizeStop(OperatorId, SessionId, AuthToken, EVSEId = null, QueryTimeout = null)

        /// <summary>
        /// Create an authorize stop request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">An optional EVSE identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<AuthStopEVSEResult> AuthorizeStop(EVSEOperator_Id     OperatorId,
                                                            ChargingSession_Id  SessionId,
                                                            Auth_Token          AuthToken,
                                                            EVSE_Id             EVSEId        = null,
                                                            TimeSpan?           QueryTimeout  = null)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException("OperatorId", "The given parameter must not be null!");

            if (SessionId  == null)
                throw new ArgumentNullException("SessionId",  "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException("AuthToken",  "The given parameter must not be null!");

            #endregion

            #region Check session identification

            SessionInfo SessionInfo = null;

            if (!SessionDatabase.TryGetValue(SessionId, out SessionInfo))
                return AuthStopEVSEResult.InvalidSessionId(AuthorizatorId);

            #endregion

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                #region Token is authorized

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    // Authorized
                    if (SessionInfo.ListOfAuthStopTokens.Contains(AuthToken))
                        return AuthStopEVSEResult.Authorized(AuthorizatorId,
                                                             EVSP.Id);

                    // Invalid Token for SessionId!
                    else
                        return AuthStopEVSEResult.NotAuthorized(AuthorizatorId,
                                                                EVSP.Id,
                                                                "Invalid token for given session identification!");

                }

                #endregion

                #region Token is blocked

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return AuthStopEVSEResult.Blocked(AuthorizatorId,
                                                      EVSP.Id,
                                                      "Token is blocked!");

                #endregion

                #region ...fall through!

                else
                    return AuthStopEVSEResult.Unspecified(AuthorizatorId);

                #endregion

            }

            // Unkown Token!
            return AuthStopEVSEResult.NotAuthorized(AuthorizatorId,
                                                    EVSP.Id,
                                                    "Unkown token!");

        }

        #endregion

        #region AuthorizeStop(OperatorId, SessionId, AuthToken, ChargingStationId, QueryTimeout = null)

        /// <summary>
        /// Create an authorize stop request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingStationId">A charging station identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<AuthStopChargingStationResult> AuthorizeStop(EVSEOperator_Id      OperatorId,
                                                                       ChargingSession_Id   SessionId,
                                                                       Auth_Token           AuthToken,
                                                                       ChargingStation_Id   ChargingStationId,
                                                                       TimeSpan?            QueryTimeout      = null)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException("OperatorId", "The given parameter must not be null!");

            if (SessionId  == null)
                throw new ArgumentNullException("SessionId",  "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException("AuthToken",  "The given parameter must not be null!");

            #endregion

            return new AuthStopChargingStationResult(AuthorizatorId) {
                       AuthorizationResult  = AuthStopChargingStationResultType.Error,
                       ProviderId           = EVSP.Id,
                   };

        }

        #endregion

        #region SendChargeDetailRecord(EVSEId, SessionId, SessionStart, SessionEnd, PartnerProductId, ..., QueryTimeout = null)

        /// <summary>
        /// Create a SendChargeDetailRecord request.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification.</param>
        /// <param name="SessionId">The session identification from the Authorize Start request.</param>
        /// <param name="PartnerProductId">An optional charging product identification.</param>
        /// <param name="SessionStart">The timestamp of the session start.</param>
        /// <param name="SessionEnd">The timestamp of the session end.</param>
        /// <param name="ChargingStart">An optional charging start timestamp.</param>
        /// <param name="ChargingEnd">An optional charging end timestamp.</param>
        /// <param name="MeterValueStart">An optional initial value of the energy meter.</param>
        /// <param name="MeterValueEnd">An optional final value of the energy meter.</param>
        /// <param name="MeterValuesInBetween">An optional enumeration of meter values during the charging session.</param>
        /// <param name="ConsumedEnergy">The optional amount of consumed energy.</param>
        /// <param name="MeteringSignature">An optional signature for the metering values.</param>
        /// <param name="HubOperatorId">An optional identification of the hub operator.</param>
        /// <param name="HubProviderId">An optional identification of the hub provider.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<SendCDRResult>

            SendChargeDetailRecord(EVSE_Id              EVSEId,
                                   ChargingSession_Id   SessionId,
                                   ChargingProduct_Id   PartnerProductId,
                                   DateTime             SessionStart,
                                   DateTime             SessionEnd,
                                   AuthInfo             AuthInfo, // REMOVE ME!
                                   DateTime?            ChargingStart         = null,
                                   DateTime?            ChargingEnd           = null,
                                   Double?              MeterValueStart       = null,
                                   Double?              MeterValueEnd         = null,
                                   IEnumerable<Double>  MeterValuesInBetween  = null,
                                   Double?              ConsumedEnergy        = null,
                                   String               MeteringSignature     = null,
                                   HubOperator_Id       HubOperatorId         = null,
                                   EVSP_Id              HubProviderId         = null,
                                   TimeSpan?            QueryTimeout          = null)

        {

            #region Initial checks

            if (EVSEId           == null)
                throw new ArgumentNullException("EVSEId",            "The given parameter must not be null!");

            if (SessionId        == null)
                throw new ArgumentNullException("SessionId",         "The given parameter must not be null!");

            if (SessionStart     == null)
                throw new ArgumentNullException("SessionStart",      "The given parameter must not be null!");

            if (SessionEnd       == null)
                throw new ArgumentNullException("SessionEnd",        "The given parameter must not be null!");

            #endregion

            SessionInfo _SessionInfo = null;

            if (SessionDatabase.TryRemove(SessionId, out _SessionInfo))
                return SendCDRResult.Forwarded(AuthorizatorId);

            return SendCDRResult.InvalidSessionId(AuthorizatorId);

        }

        #endregion



        // Outgoing to the roaming network

        #region RemoteStart

        #endregion

        #region RemoteStop

        #endregion

        #region Reserve

        #endregion


    }

}
