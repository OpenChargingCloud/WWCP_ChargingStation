/*
 * Copyright (c) 2014-2016 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// A remote Charging Station Operator.
    /// </summary>
    public class RemoteEVSEOperator : IRemoteChargingStationOperator
    {

        #region Properties

        #region ChargingReservations

        private readonly Dictionary<ChargingReservation_Id, ChargingReservation> _ChargingReservations;

        public IEnumerable<ChargingReservation> ChargingReservations
        {
            get
            {
                return _ChargingReservations.Select(_ => _.Value);
            }
        }

        #endregion

        #region ChargingSessions

        private readonly Dictionary<ChargingSession_Id, ChargingSession> _ChargingSessions;

        public IEnumerable<ChargingSession> ChargingSessions
        {
            get
            {
                return _ChargingSessions.Select(_ => _.Value);
            }
        }

        #endregion

        #endregion

        #region Events

        // Events towards the remote Charging Station Operator

        /// <summary>
        /// An event fired whenever an EVSE is being reserved.
        /// </summary>
        public event OnReserveEVSEDelegate                 OnReserveEVSE;

        /// <summary>
        /// An event sent whenever an EVSE should start charging.
        /// </summary>
        public event OnRemoteStartEVSEDelegate             OnRemoteStartEVSE;

        /// <summary>
        /// An event sent whenever a charging session should stop.
        /// </summary>
        public event OnRemoteStopEVSEDelegate              OnRemoteStopEVSE;

        #endregion

        #region Constructor(s)

        public RemoteEVSEOperator(OnReserveEVSEDelegate      OnReserveEVSE,
                                  OnRemoteStartEVSEDelegate  OnRemoteStartEVSE,
                                  OnRemoteStopEVSEDelegate   OnRemoteStopEVSE)
        {

            this.OnReserveEVSE     += OnReserveEVSE;
            this.OnRemoteStartEVSE += OnRemoteStartEVSE;
            this.OnRemoteStopEVSE  += OnRemoteStopEVSE;

            this._ChargingSessions = new Dictionary<ChargingSession_Id, ChargingSession>();

        }

        #endregion


        #region Reserve(...)

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
        /// <param name="ChargingProductId">An optional unique identification of the charging product to be reserved.</param>
        /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
        /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
        /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<ReservationResult>

            Reserve(DateTime                 Timestamp,
                    CancellationToken        CancellationToken,
                    EventTracking_Id         EventTrackingId,
                    EVSE_Id                  EVSEId,
                    DateTime?                StartTime,
                    TimeSpan?                Duration,
                    ChargingReservation_Id   ReservationId      = null,
                    EMobilityProvider_Id                  ProviderId         = null,
                    eMA_Id                   eMAId              = null,
                    ChargingProduct_Id       ChargingProductId  = null,
                    IEnumerable<Auth_Token>  AuthTokens         = null,
                    IEnumerable<eMA_Id>      eMAIds             = null,
                    IEnumerable<UInt32>      PINs               = null,
                    TimeSpan?                QueryTimeout       = null)

        {

            return ReservationResult.UnknownEVSE;

        }


        #endregion

        #region RemoteStart(...)

        /// <summary>
        /// Start a charging session at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of the EVSE to be started.</param>
        /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
        /// <param name="ReservationId">The unique identification for a charging reservation.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider for the case it is different from the current message sender.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStartEVSEResult>

            RemoteStart(EVSE_Id                 EVSEId,
                        ChargingProduct_Id      ChargingProductId,
                        ChargingReservation_Id  ReservationId,
                        ChargingSession_Id      SessionId,
                        EMobilityProvider_Id    ProviderId,
                        eMA_Id                  eMAId,

                        DateTime?               Timestamp          = null,
                        CancellationToken?      CancellationToken  = null,
                        EventTracking_Id        EventTrackingId    = null,
                        TimeSpan?               RequestTimeout     = null)

        {


            var OnRemoteStartEVSELocal = OnRemoteStartEVSE;
            if (OnRemoteStartEVSELocal == null)
                return RemoteStartEVSEResult.Error("");

            var results = await Task.WhenAll(OnRemoteStartEVSELocal.
                                                 GetInvocationList().
                                                 Select(subscriber => (subscriber as OnRemoteStartEVSEDelegate)
                                                     (Timestamp.Value,
                                                      CancellationToken.Value,
                                                      EventTrackingId,
                                                      EVSEId,
                                                      ChargingProductId,
                                                      ReservationId,
                                                      SessionId,
                                                      ProviderId,
                                                      eMAId,
                                                      RequestTimeout)));

            var result = results.
                             Where(_result => _result.Result != RemoteStartEVSEResultType.Unspecified).
                             FirstOrDefault();

            if (result        != null &&
                result.Result == RemoteStartEVSEResultType.Success)
            {

                _ChargingSessions.Add(result.Session.Id, result.Session);

            }

            return result;

        }


        /// <summary>
        /// Start a charging session at the given charging stations.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of the charging station to be started.</param>
        /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
        /// <param name="ReservationId">The unique identification for a charging reservation.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider for the case it is different from the current message sender.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStartChargingStationResult>

            RemoteStart(ChargingStation_Id      ChargingStationId,
                        ChargingProduct_Id      ChargingProductId,
                        ChargingReservation_Id  ReservationId,
                        ChargingSession_Id      SessionId,
                        EMobilityProvider_Id    ProviderId,
                        eMA_Id                  eMAId,

                        DateTime?               Timestamp          = null,
                        CancellationToken?      CancellationToken  = null,
                        EventTracking_Id        EventTrackingId    = null,
                        TimeSpan?               RequestTimeout     = null)

            => RemoteStartChargingStationResult.UnknownChargingStation;

        #endregion

        #region RemoteStop(...)

        /// <summary>
        /// Stop the given charging session.
        /// </summary>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStopResult>

            RemoteStop(ChargingSession_Id    SessionId,
                       ReservationHandling   ReservationHandling,
                       EMobilityProvider_Id  ProviderId         = null,
                       eMA_Id                eMAId              = null,

                       DateTime?             Timestamp          = null,
                       CancellationToken?    CancellationToken  = null,
                       EventTracking_Id      EventTrackingId    = null,
                       TimeSpan?             RequestTimeout     = null)

        {

            throw new NotImplementedException();

        }

        /// <summary>
        /// Stop the given charging session at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of the EVSE to be stopped.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStopEVSEResult>

            RemoteStop(EVSE_Id               EVSEId,
                       ChargingSession_Id    SessionId,
                       ReservationHandling   ReservationHandling,
                       EMobilityProvider_Id  ProviderId         = null,
                       eMA_Id                eMAId              = null,

                       DateTime?             Timestamp          = null,
                       CancellationToken?    CancellationToken  = null,
                       EventTracking_Id      EventTrackingId    = null,
                       TimeSpan?             RequestTimeout     = null)

        {

            var OnRemoteStopEVSELocal = OnRemoteStopEVSE;
            if (OnRemoteStopEVSELocal == null)
                return RemoteStopEVSEResult.Error(SessionId);

            var results = await Task.WhenAll(OnRemoteStopEVSELocal.
                                                 GetInvocationList().
                                                 Select(subscriber => (subscriber as OnRemoteStopEVSEDelegate)
                                                     (Timestamp.Value,
                                                      CancellationToken.Value,
                                                      EventTrackingId,
                                                      ReservationHandling,
                                                      SessionId,
                                                      ProviderId,
                                                      eMAId,
                                                      EVSEId,
                                                      RequestTimeout)));

            var result = results.
                             Where(_result => _result.Result != RemoteStopEVSEResultType.Unspecified).
                             FirstOrDefault();

            if (result        != null &&
                result.Result == RemoteStopEVSEResultType.Success)
            {

                if (_ChargingSessions.ContainsKey(result.SessionId))
                    _ChargingSessions.Remove(result.SessionId);

            }

            return result;

        }

        public async Task<RemoteStopChargingStationResult>

            RemoteStop(ChargingStation_Id    ChargingStationId,
                       ChargingSession_Id    SessionId,
                       ReservationHandling   ReservationHandling,
                       EMobilityProvider_Id  ProviderId         = null,
                       eMA_Id                eMAId              = null,

                       DateTime?             Timestamp          = null,
                       CancellationToken?    CancellationToken  = null,
                       EventTracking_Id      EventTrackingId    = null,
                       TimeSpan?             RequestTimeout     = null)

        {

            throw new NotImplementedException();

        }

        #endregion

    }

}