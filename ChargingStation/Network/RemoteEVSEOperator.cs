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

using org.GraphDefined.Vanaheimr.Illias;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    public class RemoteEVSEOperator : IRemoteEVSEOperator
    {


        /// <summary>
        /// An event sent whenever an EVSE should start charging.
        /// </summary>
        public event OnRemoteStartEVSEDelegate OnRemoteStartEVSE;

        /// <summary>
        /// An event sent whenever an charging station should start charging.
        /// </summary>
        public event OnRemoteStartChargingStationDelegate OnRemoteStartChargingStation;

        /// <summary>
        /// An event sent whenever a charging session should stop.
        /// </summary>
        public event OnRemoteStopEVSEDelegate OnRemoteStopEVSE;




        public RemoteEVSEOperator(OnRemoteStartEVSEDelegate  OnRemoteStartEVSE,
                                  OnRemoteStopEVSEDelegate   OnRemoteStopEVSE)
        {




        }


        public async Task<RemoteStartEVSEResult>

            RemoteStart(DateTime                Timestamp,
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


            var OnRemoteStartEVSELocal = OnRemoteStartEVSE;
            if (OnRemoteStartEVSELocal == null)
                return RemoteStartEVSEResult.Error("");

            var results = await Task.WhenAll(OnRemoteStartEVSELocal.
                                                 GetInvocationList().
                                                 Select(subscriber => (subscriber as OnRemoteStartEVSEDelegate)
                                                     (Timestamp,
                                                      this,
                                                      CancellationToken,
                                                      EventTrackingId,
                                                      EVSEId,
                                                      ChargingProductId,
                                                      ReservationId,
                                                      SessionId,
                                                      ProviderId,
                                                      eMAId,
                                                      QueryTimeout)));

            return results.
                       Where(result => result.Result != RemoteStartEVSEResultType.Unspecified).
                       First();

        }




        public Task<RemoteStopEVSEResult> RemoteStop(DateTime             Timestamp,
                                                     CancellationToken    CancellationToken,
                                                     EventTracking_Id     EventTrackingId,
                                                     EVSE_Id              EVSEId,
                                                     ReservationHandling  ReservationHandling,
                                                     ChargingSession_Id   SessionId,
                                                     TimeSpan?            QueryTimeout  = null)
        {

            throw new NotImplementedException();

        }

    }

}