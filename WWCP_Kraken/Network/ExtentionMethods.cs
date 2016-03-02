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

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// Extention methods
    /// </summary>
    public static partial class ExtentionMethods
    {

        #region CreateNewRemoteStation(this ChargingPool, ChargingStationId = null, ChargingStationConfigurator = null, RemoteChargingStationConfigurator = null, OnSuccess = null, OnError = null)

        /// <summary>
        /// Create a new remote charging station.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="ChargingStationId">The charging station identification for the charging station to be created.</param>
        /// <param name="ChargingStationConfigurator">An optional delegate to configure the new (local) charging station.</param>
        /// <param name="RemoteChargingStationConfigurator">An optional delegate to configure the new (remote) charging station.</param>
        /// <param name="OnSuccess">An optional delegate for reporting success.</param>
        /// <param name="OnError">An optional delegate for reporting an error.</param>
        public static ChargingStation CreateNewRemoteStation(this ChargingPool                         ChargingPool,
                                                             ChargingStation_Id                        ChargingStationId                  = null,
                                                             UInt16                                    MaxStatusListSize                  = NetworkChargingStationStub.DefaultMaxStatusListSize,
                                                             UInt16                                    MaxAdminStatusListSize             = NetworkChargingStationStub.DefaultMaxAdminStatusListSize,
                                                             Action<ChargingStation>                   ChargingStationConfigurator        = null,
                                                             Action<NetworkChargingStationStub>        RemoteChargingStationConfigurator  = null,
                                                             Action<ChargingStation>                   OnSuccess                          = null,
                                                             Action<ChargingPool, ChargingStation_Id>  OnError                            = null)
        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException(nameof(ChargingPool), "The given charging pool must not be null!");

            #endregion

            return ChargingPool.CreateNewStation(ChargingStationId,
                                                 ChargingStationConfigurator,
                                                 OnSuccess,
                                                 OnError,
                                                 newstation => {

                                                     var remotestation = new NetworkChargingStationStub(newstation, MaxStatusListSize, MaxAdminStatusListSize);

                                                     if (RemoteChargingStationConfigurator != null)
                                                         RemoteChargingStationConfigurator(remotestation);

                                                     return remotestation;

                                                 }) ;

        }

        #endregion

    }

}
