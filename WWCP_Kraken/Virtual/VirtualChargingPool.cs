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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// A demo implementation of a virtual WWCP charging pool.
    /// </summary>
    public class VirtualChargingPool : IRemoteChargingPool
    {

        #region Properties

        #region Id

        private ChargingPool_Id _Id;

        /// <summary>
        /// The unique identification of this virtual charging pool.
        /// </summary>
        public ChargingPool_Id Id
        {
            get
            {
                return _Id;
            }
        }

        #endregion

        #region Description

        internal I18NString _Description;

        /// <summary>
        /// An optional (multi-language) description of this charging pool.
        /// </summary>
        [Optional]
        public I18NString Description
        {

            get
            {

                return _Description;

            }

            set
            {

                if (value == _Description)
                    return;

                _Description = value;

            }

        }

        #endregion

        #region Status

        private ChargingPoolStatusType _Status;

        public ChargingPoolStatusType Status
        {
            get
            {
                return _Status;
            }
        }

        #endregion

        #endregion

        #region Links

        #region ChargingPool

        private readonly ChargingPool _ChargingPool;

        public ChargingPool ChargingPool
        {
            get
            {
                return _ChargingPool;
            }
        }

        #endregion

        #endregion

        #region Events



        #endregion

        #region Constructor(s)

        /// <summary>
        /// A virtual WWCP charging pool.
        /// </summary>
        /// <param name="ChargingPool">A local charging pool.</param>
        public VirtualChargingPool(ChargingPool ChargingPool)
        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException(nameof(ChargingPool),  "The given charging pool parameter must not be null!");

            #endregion

            this._Id            = ChargingPool.Id;
            this._ChargingPool  = ChargingPool;
            this._Status        = ChargingPoolStatusType.Available;
            this._Stations      = new HashSet<VirtualChargingStation>();

        }

        #endregion


        #region Charging stations...

        #region Stations

        private readonly HashSet<VirtualChargingStation> _Stations;

        /// <summary>
        /// All registered charging stations.
        /// </summary>
        public IEnumerable<VirtualChargingStation> Stations
        {
            get
            {
                return _Stations;
            }
        }

        #endregion

        #region CreateNewStation(ChargingStationId, Configurator = null, OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new charging station having the given
        /// unique charging station identification.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of the new charging station.</param>
        /// <param name="Configurator">An optional delegate to configure the new charging station after its creation.</param>
        /// <param name="OnSuccess">An optional delegate called after successful creation of the charging station.</param>
        /// <param name="OnError">An optional delegate for signaling errors.</param>
        public VirtualChargingStation CreateNewStation(ChargingStation_Id                               ChargingStationId,
                                                       Action<VirtualChargingStation>                   Configurator  = null,
                                                       Action<VirtualChargingStation>                   OnSuccess     = null,
                                                       Action<VirtualChargingPool, ChargingStation_Id>  OnError       = null)
        {

            #region Initial checks

            if (ChargingStationId == null)
                throw new ArgumentNullException(nameof(ChargingStationId), "The given charging station identification must not be null!");

            if (_Stations.Any(station => station.Id == ChargingStationId))
            {
                if (OnError == null)
                    throw new ChargingStationAlreadyExistsInPool(ChargingStationId, this.Id);
                else
                    OnError.FailSafeInvoke(this, ChargingStationId);
            }

            #endregion

            var Now              = DateTime.Now;
            var _VirtualStation  = new VirtualChargingStation(ChargingStationId, this);

            Configurator.FailSafeInvoke(_VirtualStation);

            if (_Stations.Add(_VirtualStation))
            {

                //_VirtualEVSE.OnPropertyChanged        += (Timestamp, Sender, PropertyName, OldValue, NewValue)
                //                                           => UpdateEVSEData(Timestamp, Sender as VirtualEVSE, PropertyName, OldValue, NewValue);
                //
                //_VirtualEVSE.OnStatusChanged          += UpdateEVSEStatus;
                //_VirtualEVSE.OnAdminStatusChanged     += UpdateEVSEAdminStatus;
                //_VirtualEVSE.OnNewReservation         += SendNewReservation;
                //_VirtualEVSE.OnNewChargingSession     += SendNewChargingSession;
                //_VirtualEVSE.OnNewChargeDetailRecord  += SendNewChargeDetailRecord;

                OnSuccess.FailSafeInvoke(_VirtualStation);

                return _VirtualStation;

            }

            return null;

        }

        #endregion


        #endregion



        public Task<ReservationResult> Reserve(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, EVSE_Id EVSEId, DateTime? StartTime, TimeSpan? Duration, ChargingReservation_Id ReservationId = null, EVSP_Id ProviderId = null, ChargingProduct_Id ChargingProductId = null, IEnumerable<Auth_Token> AuthTokens = null, IEnumerable<eMA_Id> eMAIds = null, IEnumerable<uint> PINs = null, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public Task<ReservationResult> Reserve(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, DateTime? StartTime, TimeSpan? Duration, ChargingReservation_Id ReservationId = null, EVSP_Id ProviderId = null, ChargingProduct_Id ChargingProductId = null, IEnumerable<Auth_Token> AuthTokens = null, IEnumerable<eMA_Id> eMAIds = null, IEnumerable<uint> PINs = null, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public bool TryGetReservationById(ChargingReservation_Id ReservationId, out ChargingReservation Reservation)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CancelReservation(ChargingReservation_Id ReservationId)
        {
            throw new NotImplementedException();
        }

        public Task<RemoteStartChargingStationResult> RemoteStart(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, ChargingProduct_Id ChargingProductId, ChargingReservation_Id ReservationId, ChargingSession_Id SessionId, EVSP_Id ProviderId, eMA_Id eMAId, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public Task<RemoteStartEVSEResult> RemoteStart(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, EVSE_Id EVSEId, ChargingProduct_Id ChargingProductId, ChargingReservation_Id ReservationId, ChargingSession_Id SessionId, EVSP_Id ProviderId, eMA_Id eMAId, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public Task<RemoteStopResult> RemoteStop(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, ChargingSession_Id SessionId, ReservationHandling ReservationHandling, EVSP_Id ProviderId, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public Task<RemoteStopEVSEResult> RemoteStop(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, EVSE_Id EVSEId, ChargingSession_Id SessionId, ReservationHandling ReservationHandling, EVSP_Id ProviderId, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public Task<RemoteStopChargingStationResult> RemoteStop(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, ChargingStation_Id ChargingStationId, ChargingSession_Id SessionId, ReservationHandling ReservationHandling, EVSP_Id ProviderId, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }


    }

}
