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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using org.GraphDefined.Vanaheimr.Illias;

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

        #region CreateNewStation(ChargingStation, Configurator = null, OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new charging station having the given
        /// unique charging station identification.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="Configurator">An optional delegate to configure the new charging station after its creation.</param>
        /// <param name="OnSuccess">An optional delegate called after successful creation of the charging station.</param>
        /// <param name="OnError">An optional delegate for signaling errors.</param>
        public VirtualChargingStation CreateNewStation(ChargingStation                                  ChargingStation,
                                                       Action<VirtualChargingStation>                   Configurator  = null,
                                                       Action<VirtualChargingStation>                   OnSuccess     = null,
                                                       Action<VirtualChargingPool, ChargingStation_Id>  OnError       = null)
        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation), "The given charging station must not be null!");

            if (_Stations.Any(station => station.Id == ChargingStation.Id))
            {
                if (OnError == null)
                    throw new ChargingStationAlreadyExistsInPool(this.ChargingPool, ChargingStation.Id);
                else
                    OnError?.Invoke(this, ChargingStation.Id);
            }

            #endregion

            var Now              = DateTime.Now;
            var _VirtualStation  = new VirtualChargingStation(ChargingStation, this);

            Configurator?.Invoke(_VirtualStation);

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

                OnSuccess?.Invoke(_VirtualStation);

                return _VirtualStation;

            }

            return null;

        }

        #endregion

        #endregion



        public IEnumerable<ChargingReservation> ChargingReservations
        {
            get
            {
                return new ChargingReservation[0];
            }
        }

        public Task<ReservationResult> Reserve(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, EVSE_Id EVSEId, DateTime? StartTime, TimeSpan? Duration, ChargingReservation_Id ReservationId = null, eMobilityProvider_Id ProviderId = null, ChargingProduct_Id ChargingProductId = null, IEnumerable<Auth_Token> AuthTokens = null, IEnumerable<eMobilityAccount_Id> eMAIds = null, IEnumerable<uint> PINs = null, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public Task<ReservationResult> Reserve(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, DateTime? StartTime, TimeSpan? Duration, ChargingReservation_Id ReservationId = null, eMobilityProvider_Id ProviderId = null, ChargingProduct_Id ChargingProductId = null, IEnumerable<Auth_Token> AuthTokens = null, IEnumerable<eMobilityAccount_Id> eMAIds = null, IEnumerable<uint> PINs = null, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public bool TryGetReservationById(ChargingReservation_Id ReservationId, out ChargingReservation Reservation)
        {
            throw new NotImplementedException();
        }

        #region CancelReservation(...ReservationId, Reason, ...)

        /// <summary>
        /// Try to remove the given charging reservation.
        /// </summary>
        /// <param name="Timestamp">The timestamp of this request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="ReservationId">The unique charging reservation identification.</param>
        /// <param name="Reason">A reason for this cancellation.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<CancelReservationResult> CancelReservation(DateTime                               Timestamp,
                                                                     CancellationToken                      CancellationToken,
                                                                     EventTracking_Id                       EventTrackingId,
                                                                     ChargingReservation_Id                 ReservationId,
                                                                     ChargingReservationCancellationReason  Reason,
                                                                     TimeSpan?                              QueryTimeout  = null)
        {
            throw new NotImplementedException();
        }

        #endregion

        public Task<RemoteStartChargingStationResult> RemoteStart(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, ChargingProduct_Id ChargingProductId, ChargingReservation_Id ReservationId, ChargingSession_Id SessionId, eMobilityProvider_Id ProviderId, eMobilityAccount_Id eMAId, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public Task<RemoteStartEVSEResult> RemoteStart(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, EVSE_Id EVSEId, ChargingProduct_Id ChargingProductId, ChargingReservation_Id ReservationId, ChargingSession_Id SessionId, eMobilityProvider_Id ProviderId, eMobilityAccount_Id eMAId, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public Task<RemoteStopResult> RemoteStop(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, ChargingSession_Id SessionId, ReservationHandling ReservationHandling, eMobilityProvider_Id ProviderId, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public Task<RemoteStopEVSEResult> RemoteStop(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, EVSE_Id EVSEId, ChargingSession_Id SessionId, ReservationHandling ReservationHandling, eMobilityProvider_Id ProviderId, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }

        public Task<RemoteStopChargingStationResult> RemoteStop(DateTime Timestamp, CancellationToken CancellationToken, EventTracking_Id EventTrackingId, ChargingStation_Id ChargingStationId, ChargingSession_Id SessionId, ReservationHandling ReservationHandling, eMobilityProvider_Id ProviderId, TimeSpan? QueryTimeout = default(TimeSpan?))
        {
            throw new NotImplementedException();
        }


    }

}
