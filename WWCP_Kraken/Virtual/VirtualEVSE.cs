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
using System.Collections.Concurrent;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// An Electric Vehicle Supply Equipment (EVSE) to charge an electric vehicle (EV).
    /// This is meant to be one electrical circuit which can charge a electric vehicle
    /// independently. Thus there could be multiple interdependent power sockets.
    /// </summary>
    public class VirtualEVSE : AEMobilityEntity<EVSE_Id>,
                               IEquatable<VirtualEVSE>, IComparable<VirtualEVSE>, IComparable,
                               IEnumerable<SocketOutlet>,
                               IStatus<EVSEStatusType>,
                               IRemoteEVSE
    {

        #region Data

        /// <summary>
        /// The default max size of the EVSE status history.
        /// </summary>
        public const UInt16 DefaultMaxEVSEStatusListSize = 50;

        /// <summary>
        /// The default max size of the EVSE admin status history.
        /// </summary>
        public const UInt16 DefaultMaxAdminStatusListSize = 50;

        /// <summary>
        /// The maximum time span for a reservation.
        /// </summary>
        public static readonly TimeSpan MaxReservationDuration = TimeSpan.FromMinutes(15);

        #endregion

        #region Properties

        #region Description

        private I18NString _Description;

        [Mandatory]
        public I18NString Description
        {

            get
            {

                return _Description != null
                    ? _Description
                    : _ChargingStation.Description;

            }

            set
            {

                if (value == _ChargingStation.Description)
                    value = null;

                if (_Description != value)
                    SetProperty<I18NString>(ref _Description, value);

            }

        }

        #endregion

        #region AverageVoltage

        private Double _AverageVoltage;

        /// <summary>
        /// Average voltage at the connector [Volt].
        /// </summary>
        [Mandatory]
        public Double AverageVoltage
        {

            get
            {
                return _AverageVoltage;
            }

            set
            {

                if (_AverageVoltage != value)
                    SetProperty(ref _AverageVoltage, value);

            }

        }

        #endregion

        #region MaxPower

        private Double _MaxPower;

        /// <summary>
        /// Max power at connector [Watt].
        /// </summary>
        [Mandatory]
        public Double MaxPower
        {

            get
            {
                return _MaxPower;
            }

            set
            {

                if (_MaxPower != value)
                    SetProperty(ref _MaxPower, value);

            }

        }

        #endregion

        #region RealTimePower

        private Double _RealTimePower;

        /// <summary>
        /// Real-time power at connector [Watt].
        /// </summary>
        [Mandatory]
        public Double RealTimePower
        {

            get
            {
                return _RealTimePower;
            }

            set
            {

                if (_RealTimePower != value)
                    SetProperty(ref _RealTimePower, value);

            }

        }

        #endregion

        #region GuranteedMinPower

        private Double _GuranteedMinPower;

        /// <summary>
        /// Guranteed min power at connector [Watt].
        /// </summary>
        [Mandatory]
        public Double GuranteedMinPower
        {

            get
            {
                return _GuranteedMinPower;
            }

            set
            {

                if (_MaxPower != value)
                    SetProperty(ref _GuranteedMinPower, value);

            }

        }

        #endregion

        #region MaxCapacity_kWh

        private Double? _MaxCapacity_kWh;

        /// <summary>
        /// Max power capacity at the connector [kWh].
        /// </summary>
        [Mandatory]
        public Double? MaxCapacity_kWh
        {

            get
            {
                return _MaxCapacity_kWh;
            }

            set
            {

                if (_MaxCapacity_kWh != value)
                    SetProperty(ref _MaxCapacity_kWh, value);

            }

        }

        #endregion

        #region ChargingModes

        private ReactiveSet<ChargingModes> _ChargingModes;

        [Mandatory]
        public ReactiveSet<ChargingModes> ChargingModes
        {

            get
            {
                return _ChargingModes;
            }

            set
            {

                if (_ChargingModes != value)
                    SetProperty(ref _ChargingModes, value);

            }

        }

        #endregion

        #region ChargingFacilities

        private ReactiveSet<ChargingFacilities> _ChargingFacilities;

        [Mandatory]
        public ReactiveSet<ChargingFacilities> ChargingFacilities
        {

            get
            {
                return _ChargingFacilities;
            }

            set
            {

                if (_ChargingFacilities != value)
                    SetProperty(ref _ChargingFacilities, value);

            }

        }

        #endregion

        #region SocketOutlets

        private ReactiveSet<SocketOutlet> _SocketOutlets;

        public ReactiveSet<SocketOutlet> SocketOutlets
        {

            get
            {
                return _SocketOutlets;
            }

            set
            {

                if (_SocketOutlets != value)
                    SetProperty(ref _SocketOutlets, value);

            }

        }

        #endregion


        #region PointOfDelivery // MeterId

        private String _PointOfDelivery;

        /// <summary>
        /// Point of delivery or meter identification.
        /// </summary>
        [Optional]
        public String PointOfDelivery
        {

            get
            {
                return _PointOfDelivery;
            }

            set
            {

                if (_PointOfDelivery != value)
                    SetProperty<String>(ref _PointOfDelivery, value);

            }

        }

        #endregion


        #region Reservation

        private ChargingReservation _Reservation;

        /// <summary>
        /// The charging reservation, if available.
        /// </summary>
        [InternalUseOnly]
        public ChargingReservation Reservation
        {

            get
            {
                return _Reservation;
            }

            set
            {

                if (_Reservation == value)
                    return;

                _Reservation = value;

                var OnNewReservationLocal = OnNewReservation;
                if (OnNewReservationLocal != null)
                    OnNewReservationLocal(DateTime.Now, Id, _Reservation);

                if (_Reservation != null)
                    SetStatus(EVSEStatusType.Reserved);
                else
                    SetStatus(EVSEStatusType.Available);

            }

        }

        #endregion

        #region ChargingSession

        private ChargingSession _ChargingSession;

        /// <summary>
        /// The current charging session, if available.
        /// </summary>
        [InternalUseOnly]
        public ChargingSession ChargingSession
        {

            get
            {
                return _ChargingSession;
            }

            set
            {

                if (_ChargingSession == value)
                    return;

                SetProperty(ref _ChargingSession, value);

                if (_ChargingSession != null)
                    SetStatus(EVSEStatusType.Charging);
                else
                    SetStatus(EVSEStatusType.Available);

            }

        }

        #endregion


        #region Status

        /// <summary>
        /// The current EVSE status.
        /// </summary>
        [InternalUseOnly]
        public Timestamped<EVSEStatusType> Status
        {

            get
            {
                return _StatusSchedule.CurrentStatus;
            }

            set
            {

                if (value == null)
                    return;

                if (_StatusSchedule.CurrentValue != value.Value)
                    SetStatus(value);

            }

        }

        #endregion

        #region StatusSchedule

        private StatusSchedule<EVSEStatusType> _StatusSchedule;

        /// <summary>
        /// The EVSE status schedule.
        /// </summary>
        public IEnumerable<Timestamped<EVSEStatusType>> StatusSchedule
        {
            get
            {
                return _StatusSchedule;
            }
        }

        #endregion

        #region AdminStatus

        /// <summary>
        /// The current EVSE admin status.
        /// </summary>
        [InternalUseOnly]
        public Timestamped<EVSEAdminStatusType> AdminStatus
        {

            get
            {
                return _AdminStatusSchedule.CurrentStatus;
            }

            set
            {

                if (value == null)
                    return;

                if (_AdminStatusSchedule.CurrentValue != value.Value)
                    SetAdminStatus(value);

            }

        }

        #endregion

        #region AdminStatusSchedule

        private StatusSchedule<EVSEAdminStatusType> _AdminStatusSchedule;

        /// <summary>
        /// The EVSE admin status schedule.
        /// </summary>
        public IEnumerable<Timestamped<EVSEAdminStatusType>> AdminStatusSchedule
        {
            get
            {
                return _AdminStatusSchedule;
            }
        }

        #endregion


        #region ChargingStation

        private readonly VirtualChargingStation _ChargingStation;

        /// <summary>
        /// The charging station of this EVSE.
        /// </summary>
        [InternalUseOnly]
        public IRemoteChargingStation ChargingStation
        {
            get
            {
                return _ChargingStation;
            }
        }

        #endregion

        #region Operator

        /// <summary>
        /// The operator of this EVSE.
        /// </summary>
        [InternalUseOnly]
        public EVSEOperator Operator
        {
            get
            {
                return null;// _ChargingStation.ChargingPool.EVSEOperator;
            }
        }

        #endregion

        #endregion

        #region Events

        #region OnStatusChanged

        /// <summary>
        /// An event fired whenever the dynamic status of the EVSE changed.
        /// </summary>
        public event OnStatusChangedDelegate OnStatusChanged;

        #endregion

        #region OnAdminStatusChanged

        /// <summary>
        /// An event fired whenever the admin status of the EVSE changed.
        /// </summary>
        public event OnAdminStatusChangedDelegate OnAdminStatusChanged;

        #endregion

        #region OnNewReservation

        /// <summary>
        /// An event fired whenever a new charging reservation was created.
        /// </summary>
        public event OnNewReservationDelegate OnNewReservation;

        #endregion

        #region OnReservationDeleted

        /// <summary>
        /// An event fired whenever a charging reservation was deleted.
        /// </summary>
        public event OnReservationDeletedDelegate OnReservationDeleted;

        #endregion

        #region OnNewChargingSession

        /// <summary>
        /// An event fired whenever a new charging session was created.
        /// </summary>
        public event OnNewChargingSessionDelegate OnNewChargingSession;

        #endregion

        #region SocketOutletAddition

        internal readonly IVotingNotificator<DateTime, IRemoteEVSE, SocketOutlet, Boolean> SocketOutletAddition;

        /// <summary>
        /// Called whenever a socket outlet will be or was added.
        /// </summary>
        public IVotingSender<DateTime, IRemoteEVSE, SocketOutlet, Boolean> OnSocketOutletAddition
        {
            get
            {
                return SocketOutletAddition;
            }
        }

        #endregion

        #region SocketOutletRemoval

        internal readonly IVotingNotificator<DateTime, IRemoteEVSE, SocketOutlet, Boolean> SocketOutletRemoval;

        /// <summary>
        /// Called whenever a socket outlet will be or was removed.
        /// </summary>
        public IVotingSender<DateTime, IRemoteEVSE, SocketOutlet, Boolean> OnSocketOutletRemoval
        {
            get
            {
                return SocketOutletRemoval;
            }
        }

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new Electric Vehicle Supply Equipment (EVSE) having the given EVSE identification.
        /// </summary>
        /// <param name="Id">The unique identification of this EVSE.</param>
        /// <param name="ChargingStation">The parent charging station.</param>
        /// <param name="MaxStatusListSize">The maximum size of the EVSE status list.</param>
        /// <param name="MaxAdminStatusListSize">The maximum size of the EVSE admin status list.</param>
        internal VirtualEVSE(EVSE_Id                 Id,
                             VirtualChargingStation  ChargingStation,
                             UInt16                  MaxStatusListSize       = DefaultMaxEVSEStatusListSize,
                             UInt16                  MaxAdminStatusListSize  = DefaultMaxAdminStatusListSize)

            : base(Id)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException("ChargingStation", "The charging station must not be null!");

            #endregion

            #region Init data and properties

            this._ChargingStation       = ChargingStation;

            this._Description           = new I18NString();
            this._ChargingModes         = new ReactiveSet<ChargingModes>();
            this._ChargingFacilities    = new ReactiveSet<ChargingFacilities>();
            this._SocketOutlets         = new ReactiveSet<SocketOutlet>();

            this._StatusSchedule        = new StatusSchedule<EVSEStatusType>(MaxStatusListSize);
            this._StatusSchedule.Insert(EVSEStatusType.Unspecified);

            this._AdminStatusSchedule   = new StatusSchedule<EVSEAdminStatusType>(MaxStatusListSize);
            this._AdminStatusSchedule.Insert(EVSEAdminStatusType.Unspecified);

            #endregion

            #region Init events

            this.SocketOutletAddition   = new VotingNotificator<DateTime, IRemoteEVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);
            this.SocketOutletRemoval    = new VotingNotificator<DateTime, IRemoteEVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);

            #endregion

            #region Link events

            this._StatusSchedule.     OnStatusChanged += (Timestamp, StatusSchedule, OldStatus, NewStatus)
                                                          => UpdateStatus(Timestamp, OldStatus, NewStatus);

            this._AdminStatusSchedule.OnStatusChanged += (Timestamp, StatusSchedule, OldStatus, NewStatus)
                                                          => UpdateAdminStatus(Timestamp, OldStatus, NewStatus);


            //this.SocketOutletAddition.OnVoting        += (timestamp, evse, outlet, vote)
            //                                              => ChargingStation.SocketOutletAddition.SendVoting      (timestamp, evse, outlet, vote);
            //
            //this.SocketOutletAddition.OnNotification  += (timestamp, evse, outlet)
            //                                              => ChargingStation.SocketOutletAddition.SendNotification(timestamp, evse, outlet);
            //
            //this.SocketOutletRemoval. OnVoting        += (timestamp, evse, outlet, vote)
            //                                              => ChargingStation.SocketOutletRemoval. SendVoting      (timestamp, evse, outlet, vote);
            //
            //this.SocketOutletRemoval. OnNotification  += (timestamp, evse, outlet)
            //                                              => ChargingStation.SocketOutletRemoval. SendNotification(timestamp, evse, outlet);

            #endregion

        }

        #endregion


        #region SetStatus(NewStatus)

        /// <summary>
        /// Set the current status.
        /// </summary>
        /// <param name="NewStatus">A new timestamped status.</param>
        public void SetStatus(Timestamped<EVSEStatusType>  NewStatus)
        {
            _StatusSchedule.Insert(NewStatus);
        }

        #endregion

        #region SetStatus(Timestamp, NewStatus)

        /// <summary>
        /// Set the status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="NewStatus">A new status.</param>
        public void SetStatus(DateTime        Timestamp,
                              EVSEStatusType  NewStatus)
        {
            _StatusSchedule.Insert(Timestamp, NewStatus);
        }

        #endregion

        #region SetStatus(NewStatusList, ChangeMethod = ChangeMethods.Replace)

        /// <summary>
        /// Set the timestamped status.
        /// </summary>
        /// <param name="NewStatusList">A list of new timestamped status.</param>
        /// <param name="ChangeMethod">The change mode.</param>
        public void SetStatus(IEnumerable<Timestamped<EVSEStatusType>>  NewStatusList,
                              ChangeMethods                             ChangeMethod = ChangeMethods.Replace)
        {
            _StatusSchedule.Insert(NewStatusList, ChangeMethod);
        }

        #endregion


        #region SetAdminStatus(NewAdminStatus)

        /// <summary>
        /// Set the admin status.
        /// </summary>
        /// <param name="NewAdminStatus">A new timestamped admin status.</param>
        public void SetAdminStatus(Timestamped<EVSEAdminStatusType> NewAdminStatus)
        {
            _AdminStatusSchedule.Insert(NewAdminStatus);
        }

        #endregion

        #region SetAdminStatus(Timestamp, NewAdminStatus)

        /// <summary>
        /// Set the admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="NewAdminStatus">A new admin status.</param>
        public void SetAdminStatus(DateTime             Timestamp,
                                   EVSEAdminStatusType  NewAdminStatus)
        {
            _AdminStatusSchedule.Insert(Timestamp, NewAdminStatus);
        }

        #endregion

        #region SetAdminStatus(NewAdminStatusList, ChangeMethod = ChangeMethods.Replace)

        /// <summary>
        /// Set the timestamped admin status.
        /// </summary>
        /// <param name="NewAdminStatusList">A list of new timestamped admin status.</param>
        /// <param name="ChangeMethod">The change mode.</param>
        public void SetAdminStatus(IEnumerable<Timestamped<EVSEAdminStatusType>>  NewAdminStatusList,
                                   ChangeMethods                                  ChangeMethod = ChangeMethods.Replace)
        {
            _AdminStatusSchedule.Insert(NewAdminStatusList, ChangeMethod);
        }

        #endregion


        #region Reserve(...ProviderId, ReservationId, StartTime, Duration, ...)

        public async Task<ReservationResult> Reserve(DateTime                 Timestamp,
                                                     CancellationToken        CancellationToken,
                                                     EventTracking_Id         EventTrackingId,
                                                     EVSP_Id                  ProviderId,
                                                     ChargingReservation_Id   ReservationId,
                                                     DateTime?                StartTime,
                                                     TimeSpan?                Duration,
                                                     ChargingProduct_Id       ChargingProductId  = null,
                                                     IEnumerable<Auth_Token>  RFIDIds            = null,
                                                     IEnumerable<eMA_Id>      eMAIds             = null,
                                                     IEnumerable<UInt32>      PINs               = null,
                                                     TimeSpan?                QueryTimeout       = null)

        {

            #region Check if this is a reservation update...

            if (_Reservation != null)
            {

                // Same ids => it's an update!
                if (_Reservation.Id == ReservationId)
                {

                    this._Reservation = new ChargingReservation(Timestamp,
                                                                StartTime.HasValue ? StartTime.Value : DateTime.Now,
                                                                Duration. HasValue ? Duration. Value : MaxReservationDuration,
                                                                ProviderId,
                                                                ChargingReservationLevel.EVSE,
                                                                null, //ChargingStation.ChargingPool.EVSEOperator.RoamingNetwork,
                                                                null, //ChargingStation.ChargingPool.Id,
                                                                ChargingStation.Id,
                                                                Id,
                                                                ChargingProductId,
                                                                RFIDIds,
                                                                eMAIds,
                                                                PINs);

                    return ReservationResult.Success(_Reservation);

                }

                return ReservationResult.AlreadyReserved;

            }

            #endregion


            switch (Status.Value)
            {

                case EVSEStatusType.OutOfService:
                    return ReservationResult.OutOfService;

                case EVSEStatusType.Charging:
                    return ReservationResult.AlreadyInUse;

                case EVSEStatusType.Reserved:
                    return ReservationResult.AlreadyReserved;

                case EVSEStatusType.Available:

                    this.Reservation = new ChargingReservation(Timestamp,
                                                               StartTime.HasValue ? StartTime.Value : DateTime.Now,
                                                               Duration. HasValue ? Duration. Value : MaxReservationDuration,
                                                               ProviderId,
                                                               ChargingReservationLevel.EVSE,
                                                               null, //ChargingStation.ChargingPool.EVSEOperator.RoamingNetwork,
                                                               null, //ChargingStation.ChargingPool.Id,
                                                               ChargingStation.Id,
                                                               Id,
                                                               ChargingProductId,
                                                               RFIDIds,
                                                               eMAIds,
                                                               PINs);

                    //SetStatus(EVSEStatusType.Reserved);

                    return ReservationResult.Success(_Reservation);

                default:
                    return ReservationResult.Error();

            }

        }

        #endregion

        #region RemoteStart(...EVSEId, ChargingProductId, ReservationId, SessionId, ProviderId, eMAId)

        /// <summary>
        /// Initiate a remote start of the given charging session at the given EVSE
        /// and for the given Provider/eMAId.
        /// </summary>
        /// <param name="ChargingProductId">The unique identification of the choosen charging product at the given EVSE.</param>
        /// <param name="ReservationId">The unique identification for a charging reservation.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// <returns>A RemoteStartResult task.</returns>
        public async Task<RemoteStartEVSEResult> RemoteStart(DateTime                Timestamp,
                                                             CancellationToken       CancellationToken,
                                                             EventTracking_Id        EventTrackingId,
                                                             ChargingProduct_Id      ChargingProductId,
                                                             ChargingReservation_Id  ReservationId,
                                                             ChargingSession_Id      SessionId,
                                                             EVSP_Id                 ProviderId,
                                                             eMA_Id                  eMAId,
                                                             TimeSpan?               QueryTimeout  = null)
        {

            #region Initial checks

            if (SessionId == null)
                SessionId = ChargingSession_Id.New;

            #endregion


            // SessionId_AlreadyInUse,
            // EVSE_NotReachable,
            // Start_Timeout

            #region Available

            if (Status.Value == EVSEStatusType.Available)
            {

                return RemoteStartEVSEResult.Success(new ChargingSession(SessionId) {
                                                         ProviderId         = ProviderId,
                                                         eMAId              = eMAId,
                                                         ChargingProductId  = ChargingProductId
                                                     });

            }

            #endregion

            #region Reserved

            else if (Status.Value == EVSEStatusType.Reserved)
            {

                if (Reservation    != null &&
                    Reservation.Id == ReservationId)
                {

                    return RemoteStartEVSEResult.Success(new ChargingSession(SessionId) {
                                                             Reservation        = Reservation,
                                                             ProviderId         = ProviderId,
                                                             eMAId              = eMAId,
                                                             ChargingProductId  = ChargingProductId
                                                         });

                }

                else
                    return RemoteStartEVSEResult.Reserved;

            }

            #endregion

            #region Charging

            else if (Status.Value == EVSEStatusType.Charging)
            {
                return RemoteStartEVSEResult.AlreadyInUse;
            }

            #endregion

            #region OutOfService

            else if (Status.Value == EVSEStatusType.OutOfService)
            {
                return RemoteStartEVSEResult.OutOfService;
            }

            #endregion

            #region Offline

            else if (Status.Value == EVSEStatusType.Offline)
            {
                return RemoteStartEVSEResult.Offline;
            }

            #endregion

            else
                return RemoteStartEVSEResult.Error();

        }

        #endregion

        #region RemoteStop(Timestamp, CancellationToken, EVSEId, SessionId, ReservationHandling)

        /// <summary>
        /// Initiate a remote stop of the given charging session at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ReservationHandling">Wether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <returns>A RemoteStopResult task.</returns>
        public async Task<RemoteStopEVSEResult> RemoteStop(DateTime             Timestamp,
                                                           CancellationToken    CancellationToken,
                                                           EventTracking_Id     EventTrackingId,
                                                           ChargingSession_Id   SessionId,
                                                           ReservationHandling  ReservationHandling,
                                                           EVSP_Id              ProviderId,
                                                           TimeSpan?            QueryTimeout  = null)
        {

            #region Available

            if (Status.Value == EVSEStatusType.Available)
            {
                return RemoteStopEVSEResult.InvalidSessionId(SessionId);
            }

            #endregion

            #region Reserved

            else if (Status.Value == EVSEStatusType.Reserved)
            {
                return RemoteStopEVSEResult.InvalidSessionId(SessionId);
            }

            #endregion

            #region Charging

            else if (Status.Value == EVSEStatusType.Charging)
            {

                if (ChargingSession.Id == SessionId)
                {
                    ChargingSession = null;
                    return RemoteStopEVSEResult.Success(SessionId, null, ReservationHandling);
                }

                else
                    return RemoteStopEVSEResult.InvalidSessionId(SessionId);

            }

            #endregion

            #region OutOfService

            else if (Status.Value == EVSEStatusType.OutOfService)
            {
                return RemoteStopEVSEResult.OutOfService(SessionId);
            }

            #endregion

            #region Offline

            else if (Status.Value == EVSEStatusType.Offline)
            {
                return RemoteStopEVSEResult.Offline(SessionId);
            }

            #endregion

            else
                return RemoteStopEVSEResult.Error(SessionId);

        }

        #endregion


        #region (internal) UpdateStatus(Timestamp, OldStatus, NewStatus)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal void UpdateStatus(DateTime                     Timestamp,
                                   Timestamped<EVSEStatusType>  OldStatus,
                                   Timestamped<EVSEStatusType>  NewStatus)
        {

            var OnStatusChangedLocal = OnStatusChanged;
            if (OnStatusChangedLocal != null)
                OnStatusChangedLocal(Timestamp, this, OldStatus, NewStatus);

        }

        #endregion

        #region (internal) UpdateAdminStatus(Timestamp, OldStatus, NewStatus)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="OldStatus">The old EVSE admin status.</param>
        /// <param name="NewStatus">The new EVSE admin status.</param>
        internal void UpdateAdminStatus(DateTime                          Timestamp,
                                        Timestamped<EVSEAdminStatusType>  OldStatus,
                                        Timestamped<EVSEAdminStatusType>  NewStatus)
        {

            var OnAdminStatusChangedLocal = OnAdminStatusChanged;
            if (OnAdminStatusChangedLocal != null)
                OnAdminStatusChangedLocal(Timestamp, this, OldStatus, NewStatus);

        }

        #endregion


        #region IEnumerable<SocketOutlet> Members

        /// <summary>
        /// Return a socket outlet enumerator.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _SocketOutlets.GetEnumerator();
        }

        /// <summary>
        /// Return a socket outlet enumerator.
        /// </summary>
        public IEnumerator<SocketOutlet> GetEnumerator()
        {
            return _SocketOutlets.GetEnumerator();
        }

        #endregion


        #region IComparable<VirtualEVSE> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object Object)
        {

            if (Object == null)
                throw new ArgumentNullException("The given object must not be null!");

            // Check if the given object is a virtual EVSE.
            var VirtualEVSE = Object as VirtualEVSE;
            if ((Object) VirtualEVSE == null)
                throw new ArgumentException("The given object is not a virtual EVSE!");

            return CompareTo(VirtualEVSE);

        }

        #endregion

        #region CompareTo(VirtualEVSE)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="VirtualEVSE">An virtual EVSE to compare with.</param>
        public Int32 CompareTo(VirtualEVSE VirtualEVSE)
        {

            if ((Object) VirtualEVSE == null)
                throw new ArgumentNullException("The given virtual EVSE must not be null!");

            return _Id.CompareTo(VirtualEVSE._Id);

        }

        #endregion

        #endregion

        #region IEquatable<VirtualEVSE> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>true|false</returns>
        public override Boolean Equals(Object Object)
        {

            if (Object == null)
                return false;

            // Check if the given object is a virtual EVSE.
            var VirtualEVSE = Object as VirtualEVSE;
            if ((Object) VirtualEVSE == null)
                return false;

            return this.Equals(VirtualEVSE);

        }

        #endregion

        #region Equals(VirtualEVSE)

        /// <summary>
        /// Compares two virtual EVSEs for equality.
        /// </summary>
        /// <param name="VirtualEVSE">A virtual EVSE to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(VirtualEVSE VirtualEVSE)
        {

            if ((Object) VirtualEVSE == null)
                return false;

            return _Id.Equals(VirtualEVSE._Id);

        }

        #endregion

        #endregion

        #region GetHashCode()

        /// <summary>
        /// Get the hashcode of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            return _Id.GetHashCode();
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a string representation of this object.
        /// </summary>
        public override String ToString()
        {
            return _Id.ToString();
        }

        #endregion

    }

}
