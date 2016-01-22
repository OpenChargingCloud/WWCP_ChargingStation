﻿/*
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

        #region CreateNewRemoteStation(this ChargingPool, ChargingStationId = null, Configurator = null, OnSuccess = null, OnError = null)

        /// <summary>
        /// Create a new virtual charging station.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="ChargingStationId">The charging station identification for the charging station to create.</param>
        /// <param name="Configurator"></param>
        /// <param name="OnSuccess"></param>
        /// <param name="OnError"></param>
        public static ChargingStation CreateNewRemoteStation(this ChargingPool                         ChargingPool,
                                                             ChargingStation_Id                        ChargingStationId  = null,
                                                             Action<ChargingStation>                   Configurator       = null,
                                                             Action<ChargingStation>                   OnSuccess          = null,
                                                             Action<ChargingPool, ChargingStation_Id>  OnError            = null)
        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException("ChargingPool", "The given charging pool must not be null!");

            #endregion

            var _ChargingStation  = ChargingPool.CreateNewStation(ChargingStationId,
                                                                  Configurator,
                                                                  OnSuccess,
                                                                  OnError);

            _ChargingStation.RemoteChargingStation  = new RemoteChargingStation(_ChargingStation);

            return _ChargingStation;

        }

        #endregion

    }

}
