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
        /// The default max size of the status history.
        /// </summary>
        public const UInt16 DefaultMaxStatusListSize        = 50;

        /// <summary>
        /// The default max size of the admin status history.
        /// </summary>
        public const UInt16 DefaultMaxAdminStatusListSize   = 50;

        /// <summary>
        /// The maximum time span for a reservation.
        /// </summary>
        public  static readonly TimeSpan MaxReservationDuration  = TimeSpan.FromMinutes(15);

        private static readonly Random   _random                 = new Random(DateTime.Now.Millisecond);

        #endregion

        #region Properties

        #region Description

        private I18NString _Description;

        /// <summary>
        /// An description of this EVSE.
        /// </summary>
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


        #region ChargingModes

        private ReactiveSet<ChargingModes> _ChargingModes;

        /// <summary>
        /// Charging modes.
        /// </summary>
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

        #region AverageVoltage

        private Double _AverageVoltage;

        /// <summary>
        /// The average voltage.
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

        #region CurrentType

        private CurrentTypes _CurrentType;

        /// <summary>
        /// The type of the current.
        /// </summary>
        [Mandatory]
        public CurrentTypes CurrentType
        {

            get
            {
                return _CurrentType;
            }

            set
            {

                if (_CurrentType != value)
                    SetProperty(ref _CurrentType, value);

            }

        }

        #endregion

        #region MaxCurrent

        private Double _MaxCurrent;

        /// <summary>
        /// The maximum current [Ampere].
        /// </summary>
        [Mandatory]
        public Double MaxCurrent
        {

            get
            {
                return _MaxCurrent;
            }

            set
            {

                if (_MaxCurrent != value)
                    SetProperty(ref _MaxCurrent, value);

            }

        }

        #endregion

        #region MaxPower

        private Double _MaxPower;

        /// <summary>
        /// The maximum power [kWatt].
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
        /// The current real-time power delivery [Watt].
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

        #region MaxCapacity

        private Double? _MaxCapacity;

        /// <summary>
        /// The maximum capacity [kWh].
        /// </summary>
        [Mandatory]
        public Double? MaxCapacity
        {

            get
            {
                return _MaxCapacity;
            }

            set
            {

                if (_MaxCapacity != value)
                    SetProperty(ref _MaxCapacity, value);

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


        #region Status

        /// <summary>
        /// The current EVSE status.
        /// </summary>
        [InternalUseOnly]
        public Timestamped<EVSEStatusType> Status
        {

            get
            {

                if (AdminStatus.Value == EVSEAdminStatusType.Operational ||
                    AdminStatus.Value == EVSEAdminStatusType.InternalUse)
                {
                    return _StatusSchedule.CurrentStatus;
                }

                else
                {

                    switch (AdminStatus.Value)
                    {

                        default:
                            return new Timestamped<EVSEStatusType>(AdminStatus.Timestamp, EVSEStatusType.OutOfService);

                    }

                }

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

        #endregion

        #region Links

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
        public ChargingStationOperator Operator
        {
            get
            {
                return null;// _ChargingStation.ChargingPool.EVSEOperator;
            }
        }

        #endregion

        #endregion

        #region Events

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
                             UInt16                  MaxStatusListSize       = DefaultMaxStatusListSize,
                             UInt16                  MaxAdminStatusListSize  = DefaultMaxAdminStatusListSize)

            : base(Id)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation), "The charging station must not be null!");

            #endregion

            #region Init data and properties

            this._ChargingStation       = ChargingStation;

            this._Description           = new I18NString();
            this._ChargingModes         = new ReactiveSet<ChargingModes>();
            this._SocketOutlets         = new ReactiveSet<SocketOutlet>();

            this._StatusSchedule        = new StatusSchedule<EVSEStatusType>(MaxStatusListSize);
            this._StatusSchedule.     Insert(EVSEStatusType.OutOfService);

            this._AdminStatusSchedule   = new StatusSchedule<EVSEAdminStatusType>(MaxStatusListSize);
            this._AdminStatusSchedule.Insert(EVSEAdminStatusType.OutOfService);

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


        #region Data/(Admin-)Status management

        #region OnData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data of the EVSE changed.
        /// </summary>
        public event OnRemoteEVSEDataChangedDelegate         OnDataChanged;

        /// <summary>
        /// An event fired whenever the dynamic status of the EVSE changed.
        /// </summary>
        public event OnRemoteEVSEStatusChangedDelegate       OnStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status of the EVSE changed.
        /// </summary>
        public event OnRemoteEVSEAdminStatusChangedDelegate  OnAdminStatusChanged;

        #endregion


        #region SetStatus(NewStatus)

        /// <summary>
        /// Set the current status.
        /// </summary>
        /// <param name="NewStatus">A new status.</param>
        public void SetStatus(EVSEStatusType  NewStatus)
        {
            _StatusSchedule.Insert(NewStatus);
        }

        #endregion

        #region SetStatus(NewTimestampedStatus)

        /// <summary>
        /// Set the current status.
        /// </summary>
        /// <param name="NewTimestampedStatus">A new timestamped status.</param>
        public void SetStatus(Timestamped<EVSEStatusType> NewTimestampedStatus)
        {
            _StatusSchedule.Insert(NewTimestampedStatus);
        }

        #endregion

        #region SetStatus(NewStatus, Timestamp)

        /// <summary>
        /// Set the status.
        /// </summary>
        /// <param name="NewStatus">A new status.</param>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        public void SetStatus(EVSEStatusType  NewStatus,
                              DateTime        Timestamp)
        {
            _StatusSchedule.Insert(NewStatus, Timestamp);
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
        public void SetAdminStatus(EVSEAdminStatusType NewAdminStatus)
        {
            _AdminStatusSchedule.Insert(NewAdminStatus);
        }

        #endregion

        #region SetAdminStatus(NewTimestampedAdminStatus)

        /// <summary>
        /// Set the admin status.
        /// </summary>
        /// <param name="NewTimestampedAdminStatus">A new timestamped admin status.</param>
        public void SetAdminStatus(Timestamped<EVSEAdminStatusType> NewTimestampedAdminStatus)
        {
            _AdminStatusSchedule.Insert(NewTimestampedAdminStatus);
        }

        #endregion

        #region SetAdminStatus(NewAdminStatus, Timestamp)

        /// <summary>
        /// Set the admin status.
        /// </summary>
        /// <param name="NewAdminStatus">A new admin status.</param>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        public void SetAdminStatus(EVSEAdminStatusType  NewAdminStatus,
                                   DateTime             Timestamp)
        {
            _AdminStatusSchedule.Insert(NewAdminStatus, Timestamp);
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

        #endregion

        #region Reservations...

        #region (internal) Reserve(...StartTime, Duration, ReservationId = null, ProviderId = null, ...)

        /// <summary>
        /// Reserve the possibility to charge at the given EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp of this request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="ReservationLevel">The level of the reservation to create (EVSE, charging station, ...).</param>
        /// <param name="StartTime">The starting time of the reservation.</param>
        /// <param name="Duration">The duration of the reservation.</param>
        /// <param name="ReservationId">An optional unique identification of the reservation. Mandatory for updates.</param>
        /// <param name="ProviderId">An optional unique identification of e-Mobility service provider.</param>
        /// <param name="eMAId">An optional unique identification of e-Mobility account/customer requesting this reservation.</param>
        /// <param name="ChargingProductId">An optional unique identification of the charging product to be reserved.</param>
        /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
        /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
        /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        internal async Task<ReservationResult> Reserve(DateTime                  Timestamp,
                                                       CancellationToken         CancellationToken,
                                                       EventTracking_Id          EventTrackingId,
                                                       ChargingReservationLevel  ReservationLevel,
                                                       DateTime?                 StartTime          = null,
                                                       TimeSpan?                 Duration           = null,
                                                       ChargingReservation_Id    ReservationId      = null,
                                                       EMobilityProvider_Id                   ProviderId         = null,
                                                       eMA_Id                    eMAId              = null,
                                                       ChargingProduct_Id        ChargingProductId  = null,
                                                       IEnumerable<Auth_Token>   AuthTokens         = null,
                                                       IEnumerable<eMA_Id>       eMAIds             = null,
                                                       IEnumerable<UInt32>       PINs               = null,
                                                       TimeSpan?                 QueryTimeout       = null)

        {

            if (AdminStatus.Value == EVSEAdminStatusType.Operational ||
                AdminStatus.Value == EVSEAdminStatusType.InternalUse)
            {


                #region Check if this is a reservation update...

                if (_Reservation != null)
                {

                    // Same ids => it's an update!
                    if (_Reservation.Id == ReservationId)
                    {

                        var OldReservation = _Reservation; // Store already consumed reservation time!

                        this._Reservation = new ChargingReservation(OldReservation.Id,
                                                                    Timestamp,
                                                                    OldReservation.StartTime,
                                                                    Duration. HasValue  ? Duration. Value : MaxReservationDuration,
                                                                    (StartTime.HasValue ? StartTime.Value : DateTime.Now) + (Duration.HasValue ? Duration.Value : MaxReservationDuration),
                                                                    OldReservation.ConsumedReservationTime + OldReservation.Duration - OldReservation.TimeLeft,
                                                                    ReservationLevel,
                                                                    ProviderId,
                                                                    eMAId,
                                                                    null, //ChargingStation.ChargingPool.EVSEOperator.RoamingNetwork,
                                                                    null, //ChargingStation.ChargingPool.Id,
                                                                    ChargingStation.Id,
                                                                    Id,
                                                                    ChargingProductId,
                                                                    AuthTokens,
                                                                    eMAIds,
                                                                    PINs);

                        var OnNewReservationLocal = OnNewReservation;
                        if (OnNewReservationLocal != null)
                            OnNewReservationLocal(DateTime.Now, this, _Reservation);

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

                        // Will do: Status = EVSEStatusType.Reserved
                        // Will do: Send OnNewReservation event!
                        this.Reservation = new ChargingReservation(ReservationId:           ReservationId ?? ChargingReservation_Id.New,
                                                                   Timestamp:               Timestamp,
                                                                   StartTime:               StartTime. HasValue ? StartTime.Value : DateTime.Now,
                                                                   Duration:                Duration.  HasValue ? Duration. Value : MaxReservationDuration,
                                                                   EndTime:                 (StartTime.HasValue ? StartTime.Value : DateTime.Now) + (Duration.HasValue ? Duration.Value : MaxReservationDuration),
                                                                   ConsumedReservationTime: TimeSpan.FromSeconds(0),
                                                                   ReservationLevel:        ReservationLevel,
                                                                   ProviderId:              ProviderId,
                                                                   eMAId:                   eMAId,
                                                                   RoamingNetwork:          null,
                                                                   ChargingPoolId:          null,
                                                                   ChargingStationId:       ChargingStation.Id,
                                                                   EVSEId:                  Id,
                                                                   ChargingProductId:       ChargingProductId,
                                                                   AuthTokens:              AuthTokens,
                                                                   eMAIds:                  eMAIds,
                                                                   PINs:                    PINs != null ? PINs : new UInt32[] { (UInt32)(_random.Next(1000000) + 100000) });

                        return ReservationResult.Success(_Reservation);

                    default:
                        return ReservationResult.Error();

                }

            }
            else
            {

                switch (AdminStatus.Value)
                {

                    default:
                        return ReservationResult.OutOfService;

                }

            }

        }

        #endregion

        #region Reserve(...StartTime, Duration, ReservationId = null, ProviderId = null, ...)

        /// <summary>
        /// Reserve the possibility to charge at the given EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp of this request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="StartTime">The starting time of the reservation.</param>
        /// <param name="Duration">The duration of the reservation.</param>
        /// <param name="ReservationId">An optional unique identification of the reservation. Mandatory for updates.</param>
        /// <param name="ProviderId">An optional unique identification of e-Mobility service provider.</param>
        /// <param name="eMAId">An optional unique identification of e-Mobility account/customer requesting this reservation.</param>
        /// <param name="ChargingProductId">An optional unique identification of the charging product to be reserved.</param>
        /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
        /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
        /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<ReservationResult> Reserve(DateTime                 Timestamp,
                                                     CancellationToken        CancellationToken,
                                                     EventTracking_Id         EventTrackingId,
                                                     DateTime?                StartTime          = null,
                                                     TimeSpan?                Duration           = null,
                                                     ChargingReservation_Id   ReservationId      = null,
                                                     EMobilityProvider_Id                  ProviderId         = null,
                                                     eMA_Id                   eMAId              = null,
                                                     ChargingProduct_Id       ChargingProductId  = null,
                                                     IEnumerable<Auth_Token>  AuthTokens         = null,
                                                     IEnumerable<eMA_Id>      eMAIds             = null,
                                                     IEnumerable<UInt32>      PINs               = null,
                                                     TimeSpan?                QueryTimeout       = null)

        {

            return await Reserve(Timestamp,
                                 CancellationToken,
                                 EventTrackingId,
                                 ChargingReservationLevel.EVSE,
                                 StartTime,
                                 Duration,
                                 ReservationId,
                                 ProviderId,
                                 eMAId,
                                 ChargingProductId,
                                 AuthTokens,
                                 eMAIds,
                                 PINs,
                                 QueryTimeout);

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

                // Skip, if the reservation is already known... 
                if (_Reservation != value)
                {

                    if (value != null)
                    {

                        _Reservation = value;

                        SetStatus(EVSEStatusType.Reserved);

                        OnNewReservation?.Invoke(DateTime.Now, this, _Reservation);

                    }

                    else
                    {

                        SetStatus(EVSEStatusType.Available);

                        OnReservationCancelled?.Invoke(DateTime.Now,
                                                       this,
                                                       EventTracking_Id.New,
                                                       _Reservation.Id,
                                                       _Reservation,
                                                       ChargingReservationCancellationReason.EndOfProcess);

                        _Reservation = null;

                    }

                }

            }

        }

        #endregion

        #region OnNewReservation

        /// <summary>
        /// An event fired whenever a new charging reservation was created.
        /// </summary>
        public event OnNewReservationDelegate OnNewReservation;

        #endregion


        #region (internal) CheckIfReservationIsExpired()

        /// <summary>
        /// Check if the reservation is expired.
        /// </summary>
        internal async Task CheckIfReservationIsExpired()
        {

            if (_Reservation != null &&
                Status.Value == EVSEStatusType.Reserved &&
                _Reservation.IsExpired())
            {

                await __CancelReservation(DateTime.Now,
                                          new CancellationTokenSource().Token,
                                          EventTracking_Id.New,
                                          _Reservation.Id,
                                          ChargingReservationCancellationReason.Expired);

            }

        }

        #endregion

        #region (private) __CancelReservation(...ReservationId, Reason, ...)

        /// <summary>
        /// Try to remove the given charging reservation.
        /// </summary>
        /// <param name="Timestamp">The timestamp of this request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="ReservationId">The unique charging reservation identification.</param>
        /// <param name="Reason">A reason for this cancellation.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        private async Task<CancelReservationResult> __CancelReservation(DateTime                               Timestamp,
                                                                        CancellationToken                      CancellationToken,
                                                                        EventTracking_Id                       EventTrackingId,
                                                                        ChargingReservation_Id                 ReservationId,
                                                                        ChargingReservationCancellationReason  Reason,
                                                                        TimeSpan?                              QueryTimeout  = null)
        {

            if (_Reservation == null || _Reservation.Id != ReservationId)
                return CancelReservationResult.UnknownReservationId(ReservationId);


            var SavedReservation = _Reservation;

            _Reservation = null;

            OnReservationCancelled?.Invoke(DateTime.Now,
                                           this,
                                           EventTracking_Id.New,
                                           SavedReservation.Id,
                                           SavedReservation,
                                           Reason);

            // Will send events!
            SetStatus(EVSEStatusType.Available);

            return CancelReservationResult.Success(ReservationId);

        }

        #endregion

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

            #region Initial checks

            if (ReservationId == null)
                throw new ArgumentNullException(nameof(ReservationId),  "The given charging reservation identification must not be null!");

            #endregion


            if (AdminStatus.Value == EVSEAdminStatusType.Operational ||
                AdminStatus.Value == EVSEAdminStatusType.InternalUse)
            {

                return await __CancelReservation(Timestamp,
                                                 CancellationToken,
                                                 EventTrackingId,
                                                 ReservationId,
                                                 Reason);

            }
            else
            {

                switch (AdminStatus.Value)
                {

                    default:
                        return CancelReservationResult.OutOfService;

                }

            }

        }

        #endregion

        #region OnReservationCancelled

        /// <summary>
        /// An event fired whenever a charging reservation was deleted.
        /// </summary>
        public event OnReservationCancelledInternalDelegate OnReservationCancelled;

        #endregion

        #endregion

        #region RemoteStart/-Stop and Sessions...

        #region RemoteStart(...ChargingProductId = null, ReservationId = null, SessionId = null, ProviderId = null, eMAId = null, ...)

        /// <summary>
        /// Start a charging session.
        /// </summary>
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

            RemoteStart(ChargingProduct_Id      ChargingProductId  = null,
                        ChargingReservation_Id  ReservationId      = null,
                        ChargingSession_Id      SessionId          = null,
                        EMobilityProvider_Id    ProviderId         = null,
                        eMA_Id                  eMAId              = null,

                        DateTime?               Timestamp          = null,
                        CancellationToken?      CancellationToken  = null,
                        EventTracking_Id        EventTrackingId    = null,
                        TimeSpan?               RequestTimeout     = null)

        {

            #region Initial checks

            if (SessionId == null)
                SessionId = ChargingSession_Id.New;

            #endregion


            if (AdminStatus.Value == EVSEAdminStatusType.Operational ||
                AdminStatus.Value == EVSEAdminStatusType.InternalUse)
            {

                switch (Status.Value)
                {

                    #region Available

                    case EVSEStatusType.Available:
                    case EVSEStatusType.AVAILABLE_DOOR_NOT_CLOSED:

                        // Will also set the status -> EVSEStatusType.Charging;
                        ChargingSession = new ChargingSession(SessionId) {
                                                                 EventTrackingId    = EventTrackingId,
                                                                 Reservation        = Reservation != null && Reservation.Id == ReservationId ? Reservation : null,
                                                                 ReservationId      = ReservationId,
                                                                 EVSEId             = Id,
                                                                 ChargingProductId  = ChargingProductId != null ? ChargingProductId : ChargingProduct_Id.Parse("AC1"),
                                                                 ProviderId         = ProviderId,
                                                                 eMAIdStart         = eMAId
                                                             };

                        return RemoteStartEVSEResult.Success(_ChargingSession);

                    #endregion

                    #region Reserved

                    case EVSEStatusType.Reserved:

                        #region Not matching reservation identifications...

                        if (Reservation != null && ReservationId == null)
                            return RemoteStartEVSEResult.Reserved("Missing reservation identification!");

                        if (Reservation != null && ReservationId != Reservation.Id)
                            return RemoteStartEVSEResult.Reserved("Invalid reservation identification!");

                        #endregion

                        #region ...or a matching reservation identification!

                        // Check if this remote start is allowed!
                        if (!Reservation.eMAIds.Contains(eMAId))
                            return RemoteStartEVSEResult.InvalidCredentials;


                        Reservation.AddToConsumedReservationTime(Reservation.Duration - Reservation.TimeLeft);

                        // Will also set the status -> EVSEStatusType.Charging;
                        ChargingSession = new ChargingSession(SessionId) {
                                                                 EventTrackingId    = EventTrackingId,
                                                                 Reservation        = Reservation,
                                                                 ProviderId         = ProviderId,
                                                                 eMAIdStart         = eMAId,
                                                                 EVSEId             = Id,
                                                                 ChargingProductId  = ChargingProductId != null ? ChargingProductId : ChargingProduct_Id.Parse("AC1"),
                        };


                        Reservation.ChargingSession = ChargingSession;


                        return RemoteStartEVSEResult.Success(_ChargingSession);

                        #endregion

                    #endregion

                    #region Charging

                    case EVSEStatusType.Charging:
                        return RemoteStartEVSEResult.AlreadyInUse;

                    #endregion

                    #region OutOfService

                    case EVSEStatusType.OutOfService:
                        return RemoteStartEVSEResult.OutOfService;

                    #endregion

                    #region Offline

                    case EVSEStatusType.Offline:
                        return RemoteStartEVSEResult.Offline;

                    #endregion

                }

                return RemoteStartEVSEResult.Error("Could not start charging!");

            }
            else
            {

                switch (AdminStatus.Value)
                {

                    default:
                        return RemoteStartEVSEResult.OutOfService;

                }

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

                // Skip, if the charging session is already known... 
                if (_ChargingSession != value)
                {

                    _ChargingSession = value;

                    if (_ChargingSession != null)
                    {

                        SetStatus(EVSEStatusType.Charging);

                        var OnNewChargingSessionLocal = OnNewChargingSession;
                        if (OnNewChargingSessionLocal != null)
                            OnNewChargingSessionLocal(DateTime.Now, this, _ChargingSession);

                    }

                    else
                        SetStatus(EVSEStatusType.Available);

                }

            }

        }

        #endregion

        #region OnNewChargingSession

        /// <summary>
        /// An event fired whenever a new charging session was created.
        /// </summary>
        public event OnNewChargingSessionDelegate OnNewChargingSession;

        #endregion

        #region RemoteStop(...SessionId, ReservationHandling, ProviderId = null, eMAId = null, ...)

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
        public async Task<RemoteStopEVSEResult>

            RemoteStop(ChargingSession_Id    SessionId,
                       ReservationHandling   ReservationHandling,
                       EMobilityProvider_Id  ProviderId         = null,
                       eMA_Id                eMAId              = null,

                       DateTime?             Timestamp          = null,
                       CancellationToken?    CancellationToken  = null,
                       EventTracking_Id      EventTrackingId    = null,
                       TimeSpan?             RequestTimeout     = null)

        {

            if (AdminStatus.Value == EVSEAdminStatusType.Operational ||
                AdminStatus.Value == EVSEAdminStatusType.InternalUse)
            {

                switch (Status.Value)
                {

                    #region Available

                    case EVSEStatusType.Available:
                        return RemoteStopEVSEResult.InvalidSessionId(SessionId);

                    #endregion

                    #region Reserved

                    case EVSEStatusType.Reserved:
                        return RemoteStopEVSEResult.InvalidSessionId(SessionId);

                    #endregion

                    #region Charging

                    case EVSEStatusType.Charging:

                        #region Matching session identification...

                        if (ChargingSession.Id == SessionId)
                        {

                            var Now = DateTime.Now;

                            var _ChargeDetailRecord = new ChargeDetailRecord(SessionId:              _ChargingSession.Id,
                                                                             Reservation:            _ChargingSession.Reservation,
                                                                             EVSE:                   _ChargingSession.EVSE,
                                                                             ChargingStation:        _ChargingSession.EVSE.ChargingStation,
                                                                             ChargingPool:           _ChargingSession.EVSE.ChargingStation.ChargingPool,
                                                                             EVSEOperator:           _ChargingSession.EVSE.Operator,
                                                                             ProviderId:             _ChargingSession.ProviderId,
                                                                             ChargingProductId:      _ChargingSession.ChargingProductId,
                                                                             SessionTime:            new StartEndDateTime(_ChargingSession.SessionTime.Value.StartTime, Now),

                                                                             IdentificationStart:    _ChargingSession.eMAIdStart != null
                                                                                                         ? AuthInfo.FromRemoteIdentification(_ChargingSession.eMAIdStart)
                                                                                                         : _ChargingSession.AuthTokenStart != null
                                                                                                             ? AuthInfo.FromAuthToken(_ChargingSession.AuthTokenStart)
                                                                                                             : null,

                                                                             IdentificationStop:     eMAId != null ? AuthInfo.FromRemoteIdentification(eMAId) : null,

                                                                             EnergyMeterId:          EnergyMeter_Id.Parse("default"),
                                                                             EnergyMeteringValues:   new List<Timestamped<Double>>() {
                                                                                                         new Timestamped<Double>(_ChargingSession.SessionTime.Value.StartTime,   0),
                                                                                                         new Timestamped<Double>(Now,                                          100)
                                                                                                     }

                                                                            );

                            // Will do: Status = EVSEStatusType.Available
                            ChargingSession = null;

                            if (!ReservationHandling.IsKeepAlive)
                            {

                                // Will do: Status = EVSEStatusType.Available
                                Reservation = null;

                            }

                            else
                            {
                                //ToDo: Reservation will live on!
                            }


                            OnNewChargeDetailRecord?.Invoke(DateTime.Now, this, _ChargeDetailRecord);

                            return RemoteStopEVSEResult.Success(_ChargeDetailRecord, null, ReservationHandling);

                        }

                        #endregion

                        #region ...or unknown session identification!

                        else
                            return RemoteStopEVSEResult.InvalidSessionId(SessionId);

                        #endregion

                    #endregion

                    #region OutOfService

                    case EVSEStatusType.OutOfService:
                        return RemoteStopEVSEResult.OutOfService(SessionId);

                    #endregion

                    #region Offline

                    case EVSEStatusType.Offline:
                        return RemoteStopEVSEResult.Offline(SessionId);

                    #endregion

                }

                return RemoteStopEVSEResult.Error(SessionId);

            }
            else
            {

                switch (AdminStatus.Value)
                {

                    default:
                        return RemoteStopEVSEResult.OutOfService(SessionId);

                }

            }

        }

        #endregion

        #region OnNewChargeDetailRecord

        /// <summary>
        /// An event fired whenever a new charge detail record was created.
        /// </summary>
        public event OnNewChargeDetailRecordDelegate OnNewChargeDetailRecord;

        #endregion

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
                throw new ArgumentNullException(nameof(VirtualEVSE),  "The given virtual EVSE must not be null!");

            return Id.CompareTo(VirtualEVSE.Id);

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

            return Id.Equals(VirtualEVSE.Id);

        }

        #endregion

        #endregion

        #region GetHashCode()

        /// <summary>
        /// Get the hashcode of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a string representation of this object.
        /// </summary>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
