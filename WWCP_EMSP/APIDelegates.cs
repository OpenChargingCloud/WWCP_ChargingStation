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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using org.GraphDefined.WWCP;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.WWCP.EMSP
{

    /// <summary>
    /// Initiate a remote start of the given charging session at the given EVSE
    /// and for the given Provider/eMAId.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="CancellationToken">A token to cancel this task.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="EVSEId">The unique identification of an EVSE.</param>
    /// <param name="ReservationId">The unique identification for a charging reservation.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="eMAId">The unique identification of the e-mobility account.</param>
    /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
    /// <param name="RequestTimeout">An optional timeout for this request.</param>
    public delegate Task<RemoteStartResult> OnRemoteStartDelegate(DateTime                Timestamp,
                                                                  ProviderAPI             Sender,
                                                                  CancellationToken       CancellationToken,
                                                                  EventTracking_Id        EventTrackingId,
                                                                  EVSE_Id                 EVSEId,
                                                                  ChargingProduct_Id      ChargingProductId,
                                                                  ChargingReservation_Id  ReservationId,
                                                                  ChargingSession_Id      SessionId,
                                                                  RemoteAuthentication    RemoteAuthentication,
                                                                  TimeSpan?               RequestTimeout  = null);


    /// <summary>
    /// Stop the given charging session at the given EVSE.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="CancellationToken">A token to cancel this task.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="EVSEId">The unique identification of an EVSE.</param>
    /// <param name="ReservationHandling">Whether to remove the reservation after session end, or to keep it open for some more time.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="eMAId">The unique identification of the e-mobility account.</param>
    /// <param name="RequestTimeout">An optional timeout for this request.</param>
    public delegate Task<RemoteStopResult> OnRemoteStopDelegate(DateTime              Timestamp,
                                                                ProviderAPI           Sender,
                                                                CancellationToken     CancellationToken,
                                                                EventTracking_Id      EventTrackingId,
                                                                EVSE_Id               EVSEId,
                                                                ReservationHandling   ReservationHandling,
                                                                ChargingSession_Id    SessionId,
                                                                RemoteAuthentication  RemoteAuthentication,
                                                                TimeSpan?             RequestTimeout  = null);


    /// <summary>
    /// Reserve the possibility to charge at the given EVSE.
    /// </summary>
    /// <param name="Sender">The sender of this event.</param>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="CancellationToken">A token to cancel this task.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this event with other events.</param>
    /// <param name="EVSEId">The unique identification of the EVSE to be reserved.</param>
    /// <param name="ReservationId">The unique identification for this charging reservation.</param>
    /// <param name="eMAId">An optional unique identification of e-Mobility account/customer requesting this reservation.</param>
    /// <param name="StartTime">The starting time of the reservation.</param>
    /// <param name="Duration">The duration of the reservation.</param>
    /// <param name="ChargingProductId">An optional unique identification of the charging product to be reserved.</param>
    /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
    /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
    /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
    /// <param name="RequestTimeout">An optional timeout for this request.</param>
    public delegate Task<ReservationResult> OnReserveEVSEDelegate(DateTime                          Timestamp,
                                                                  ProviderAPI                       Sender,
                                                                  CancellationToken                 CancellationToken,
                                                                  EventTracking_Id                  EventTrackingId,
                                                                  EVSE_Id                           EVSEId,
                                                                  DateTime?                         StartTime,
                                                                  TimeSpan?                         Duration,
                                                                  ChargingReservation_Id            ReservationId,
                                                                  RemoteAuthentication              RemoteAuthentication,
                                                                  ChargingProduct_Id                ChargingProductId,
                                                                  IEnumerable<Auth_Token>           AuthTokens,
                                                                  IEnumerable<eMobilityAccount_Id>  eMAIds,
                                                                  IEnumerable<UInt32>               PINs,
                                                                  TimeSpan?                         RequestTimeout  = null);


    public delegate Task<CancelReservationResult> OnCancelReservationDelegate(DateTime                               Timestamp,
                                                                              ProviderAPI                            Sender,
                                                                              CancellationToken                      CancellationToken,
                                                                              EventTracking_Id                       EventTrackingId,
                                                                              ChargingReservation_Id                 ChargingReservationId,
                                                                              ChargingReservationCancellationReason  Reason,
                                                                              TimeSpan?                              RequestTimeout  = null);

    public delegate Task<CancelReservationResult> OnDeleteReservationDelegate(DateTime                               Timestamp,
                                                                              ProviderAPI                            Sender,
                                                                              CancellationToken                      CancellationToken,
                                                                              EventTracking_Id                       EventTrackingId,
                                                                              ChargingReservation_Id                 ChargingReservationId,
                                                                              eMobilityAccount_Id                    eMAId,
                                                                              TimeSpan?                              RequestTimeout  = null);

}
