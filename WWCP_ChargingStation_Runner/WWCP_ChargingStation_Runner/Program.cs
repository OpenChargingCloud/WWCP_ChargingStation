/*
 * Copyright (c) 2014-2023 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ChargingStation <https://github.com/OpenChargingCloud/WWCP_ChargingStation>
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

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.ChargingStations;

#endregion

namespace org.GraphDefined
{

    public class Program
    {

        #region Event Sinks

        #region ChargingStation_Connected(ChargingStation)

        private static void ChargingStation_Connected(RemoteChargingStation ChargingStation)
        {
            Console.WriteLine("'" + ChargingStation.Id + "' connected to '" + ChargingStation.EVSEOperatorDNS + "'");
        }

        #endregion

        #region ChargingStation_Disconnected(ChargingStation)

        private static void ChargingStation_Disconnected(RemoteChargingStation ChargingStation)
        {
            Console.WriteLine("'" + ChargingStation.Id + "' disconnected from '" + ChargingStation.EVSEOperatorDNS + "'");
        }

        #endregion

        #region ChargingStation_StateChanged(ChargingStation, OldState, NewState)

        private static void ChargingStation_StateChanged(RemoteChargingStation  ChargingStation,
                                                         ChargingStationStatus  OldState,
                                                         ChargingStationStatus  NewState)
        {
            Console.WriteLine("'" + ChargingStation.Id + "' changed from " + OldState + " to " + NewState);
        }

        #endregion

        #endregion

        public static void Main(String[] Arguments)
        {

            var ChargingStation01 = new RemoteChargingStation(Id:               ChargingStation_Id.Parse("DE*GEF*S12345*"),
                                                              EVSEOperatorDNS:  "backend.ev.graphdefined.org");

            //ChargingStation01.Connected     += ChargingStation_Connected;
            //ChargingStation01.Disconnected  += ChargingStation_Disconnected;
            //ChargingStation01.StateChanged  += ChargingStation_StateChanged;

            ChargingStation01.Connect();


        }

    }

}
