﻿/*
 * Copyright (c) 2014-2015 GraphDefined GmbH
 * This file is part of WWCP Core <https://github.com/GraphDefined/WWCP_Core>
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

using org.GraphDefined.Vanaheimr.Hermod.HTTP;
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

        #region EVSPId

        private readonly EVSP_Id _EVSPId;

        public EVSP_Id EVSPId
        {
            get
            {
                return _EVSPId;
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


        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> AllTokens
        {
            get
            {
                return AuthorizationDatabase;
            }
        }

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> AuthorizedTokens
        {
            get
            {
                return AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.Authorized);
            }
        }

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> NotAuthorizedTokens
        {
            get
            {
                return AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.NotAuthorized);
            }
        }

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> BlockedTokens
        {
            get
            {
                return AuthorizationDatabase.Where(v => v.Value == TokenAuthorizationResultType.Blocked);
            }
        }

        #endregion

        #region Events

        #endregion

        #region Constructor(s)

        public LocalEMobilityService(EVSP_Id          EVSPId,
                                     Authorizator_Id  AuthorizatorId = null)
        {
            this._EVSPId                = EVSPId;
            this._AuthorizatorId        = (AuthorizatorId == null) ? Authorizator_Id.Parse("eMI3 Local E-Mobility Database") : AuthorizatorId;
            this.AuthorizationDatabase  = new ConcurrentDictionary<Auth_Token,     TokenAuthorizationResultType>();
            this.SessionDatabase        = new ConcurrentDictionary<ChargingSession_Id, SessionInfo>();
        }

        #endregion


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


        #region AuthorizeStart(OperatorId, AuthToken, EVSEId = null, SessionId = null, PartnerProductId = null, PartnerSessionId = null, QueryTimeout = null)

        /// <summary>
        /// Create an authorize start request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">An optional EVSE identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="PartnerProductId">An optional partner product identification.</param>
        /// <param name="PartnerSessionId">An optional partner session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<HTTPResponse<AuthStartResult>> AuthorizeStart(EVSEOperator_Id     OperatorId,
                                                                        Auth_Token          AuthToken,
                                                                        EVSE_Id             EVSEId            = null,
                                                                        ChargingSession_Id  SessionId         = null,
                                                                        ChargingProduct_Id  PartnerProductId  = null,
                                                                        ChargingSession_Id  PartnerSessionId  = null,
                                                                        TimeSpan?           QueryTimeout      = null)

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

                    return new HTTPResponse<AuthStartResult>(
                               new HTTPResponse(),
                               new AuthStartResult(AuthorizatorId) {
                                   AuthorizationResult  = AuthorizeStartResultType.Success,
                                   SessionId            = _SessionId,
                                   ProviderId           = EVSPId
                               });

                }

                #endregion

                #region Token is blocked!

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return new HTTPResponse<AuthStartResult>(
                               new HTTPResponse(),
                               new AuthStartResult(AuthorizatorId) {
                                   AuthorizationResult  = AuthorizeStartResultType.Error,
                                   ProviderId           = EVSPId,
                                   Description          = "Token is blocked!"
                               });

                #endregion

                #region ...fall through!

                else
                    return new HTTPResponse<AuthStartResult>(
                               new HTTPResponse(),
                               new AuthStartResult(AuthorizatorId) {
                                   AuthorizationResult  = AuthorizeStartResultType.Unspecified,
                                   ProviderId           = EVSPId,
                               });

                #endregion

            }

            #region Unkown Token!

            else
                return new HTTPResponse<AuthStartResult>(
                               new HTTPResponse(),
                               new AuthStartResult(AuthorizatorId) {
                                   AuthorizationResult  = AuthorizeStartResultType.Unspecified,
                                   ProviderId           = EVSPId,
                                   Description          = "Unkown token!"
                               });

            #endregion

        }

        #endregion

        #region AuthorizeStop(OperatorId, SessionId, AuthToken, EVSEId = null, PartnerSessionId = null, QueryTimeout = null)

        /// <summary>
        /// Create an authorize stop request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">An optional EVSE identification.</param>
        /// <param name="PartnerSessionId">An optional partner session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<HTTPResponse<AuthStopResult>> AuthorizeStop(EVSEOperator_Id      OperatorId,
                                                                      ChargingSession_Id   SessionId,
                                                                      Auth_Token           AuthToken,
                                                                      EVSE_Id              EVSEId            = null,   // OICP v2.0: Optional
                                                                      ChargingSession_Id   PartnerSessionId  = null,   // OICP v2.0: Optional [50]
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

            TokenAuthorizationResultType AuthenticationResult;

            if (AuthorizationDatabase.TryGetValue(AuthToken, out AuthenticationResult))
            {

                if (AuthenticationResult == TokenAuthorizationResultType.Authorized)
                {

                    SessionInfo SessionInfo = null;

                    if (SessionDatabase.TryGetValue(SessionId, out SessionInfo))
                    {

                        #region Authorized

                        if (AuthToken == SessionInfo.Token)
                            return new HTTPResponse<AuthStopResult>(
                                       new HTTPResponse(),
                                       new AuthStopResult(AuthorizatorId) {
                                           AuthorizationResult  = AuthorizeStopResultType.Success,
                                           SessionId            = SessionId,
                                           ProviderId           = EVSPId
                                       });

                        #endregion

                        #region Invalid Token for SessionId!

                        else
                        {
                            return new HTTPResponse<AuthStopResult>(
                                       new HTTPResponse(),
                                       new AuthStopResult(AuthorizatorId) {
                                           AuthorizationResult  = AuthorizeStopResultType.Error,
                                           ProviderId           = EVSPId,
                                           Description          = "Invalid token for given session identification!"
                                       });
                        }

                        #endregion

                    }

                    #region Invalid SessionId!

                    else
                    {
                        return new HTTPResponse<AuthStopResult>(
                                   new HTTPResponse(),
                                   new AuthStopResult(AuthorizatorId) {
                                       AuthorizationResult  = AuthorizeStopResultType.Error,
                                       ProviderId           = EVSPId,
                                       Description          = "Invalid session identification!"
                                   });
                    }

                    #endregion

                }

                #region Blocked

                else if (AuthenticationResult == TokenAuthorizationResultType.Blocked)
                    return new HTTPResponse<AuthStopResult>(
                               new HTTPResponse(),
                               new AuthStopResult(AuthorizatorId) {
                                   AuthorizationResult  = AuthorizeStopResultType.Error,
                                   ProviderId           = EVSPId,
                                   Description          = "Token is blocked!"
                               });

                #endregion

                #region ...fall through!

                else
                    return new HTTPResponse<AuthStopResult>(
                               new HTTPResponse(),
                               new AuthStopResult(AuthorizatorId) {
                                   AuthorizationResult  = AuthorizeStopResultType.Error,
                                   ProviderId           = EVSPId,
                               });

                #endregion

            }

            #region Unkown Token!

            else
                return new HTTPResponse<AuthStopResult>(
                           new HTTPResponse(),
                           new AuthStopResult(AuthorizatorId) {
                               AuthorizationResult  = AuthorizeStopResultType.Unspecified,
                               ProviderId           = EVSPId,
                               Description          = "Unkown token!"
                           });

            #endregion

        }

        #endregion

        #region SendChargeDetailRecord(EVSEId, SessionId, SessionStart, SessionEnd, PartnerProductId, ..., QueryTimeout = null)

        /// <summary>
        /// Create a SendChargeDetailRecord request.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification.</param>
        /// <param name="SessionId">The session identification from the Authorize Start request.</param>
        /// <param name="PartnerProductId"></param>
        /// <param name="SessionStart">The timestamp of the session start.</param>
        /// <param name="SessionEnd">The timestamp of the session end.</param>
        /// <param name="PartnerSessionId">An optional partner session identification.</param>
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
        public async Task<HTTPResponse<SendCDRResult>>

            SendChargeDetailRecord(EVSE_Id              EVSEId,
                                   ChargingSession_Id   SessionId,
                                   ChargingProduct_Id   PartnerProductId,
                                   DateTime             SessionStart,
                                   DateTime             SessionEnd,
                                   AuthInfo             AuthInfo, // REMOVE ME!
                                   ChargingSession_Id   PartnerSessionId      = null,
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
                return new HTTPResponse<SendCDRResult>(new HTTPResponse(),
                                                       SendCDRResult.Forwarded(AuthorizatorId));

            return new HTTPResponse<SendCDRResult>(new HTTPResponse(),
                                                   SendCDRResult.InvalidSessionId(AuthorizatorId));

        }

        #endregion


    }

}
