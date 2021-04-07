/*
 * Copyright (c) 2014-2018 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// An Electric Vehicle Supply Equipment (EVSE) to charge an electric vehicle (EV).
    /// This is meant to be one electrical circuit which can charge a electric vehicle
    /// independently. Thus there could be multiple interdependent power sockets.
    /// </summary>
    public class RemoteEVSE : AEMobilityEntity<EVSE_Id>,
                                   IEquatable<RemoteEVSE>, IComparable<RemoteEVSE>, IComparable,
                                   IEnumerable<SocketOutlet>,
                                   IStatus<EVSEStatusTypes>
                     //              IRemoteEVSE
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
                    : ChargingStation.Description;

            }

            set
            {

                if (value == ChargingStation.Description)
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


        #region ReservationId

        /// <summary>
        /// The charging reservation identification.
        /// </summary>
        [InternalUseOnly]
        public ChargingReservation_Id ReservationId
        {
            get
            {
                return _Reservation.Id;
            }
        }

        #endregion

        #region Reservation

        private ChargingReservation _Reservation;

        /// <summary>
        /// The charging reservation.
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

                if (_Reservation != null)
                    SetStatus(EVSEStatusTypes.Reserved);
                else
                    SetStatus(EVSEStatusTypes.Available);

            }

        }

        #endregion

        #region CurrentChargingSession

        private ChargingSession_Id _CurrentChargingSession;

        /// <summary>
        /// The current charging session at this EVSE.
        /// </summary>
        [InternalUseOnly]
        public ChargingSession_Id CurrentChargingSession
        {

            get
            {
                return _CurrentChargingSession;
            }

            set
            {

                if (_CurrentChargingSession != value)
                    SetProperty(ref _CurrentChargingSession, value);

                if (_CurrentChargingSession != null)
                    SetStatus(EVSEStatusTypes.Charging);
                else
                    SetStatus(EVSEStatusTypes.Available);

            }

        }

        #endregion


        #region Status

        /// <summary>
        /// The current EVSE status.
        /// </summary>
        [InternalUseOnly]
        public Timestamped<EVSEStatusTypes> Status
        {

            get
            {
                return _StatusSchedule.CurrentStatus;
            }

            set
            {
                SetStatus(value);
            }

        }

        #endregion

        #region StatusSchedule

        private StatusSchedule<EVSEStatusTypes> _StatusSchedule;

        /// <summary>
        /// The EVSE status schedule.
        /// </summary>
        public IEnumerable<Timestamped<EVSEStatusTypes>> StatusSchedule
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
        public Timestamped<EVSEAdminStatusTypes> AdminStatus
        {

            get
            {
                return _AdminStatusSchedule.CurrentStatus;
            }

            set
            {
                SetAdminStatus(value);
            }

        }

        #endregion

        #region AdminStatusSchedule

        private StatusSchedule<EVSEAdminStatusTypes> _AdminStatusSchedule;

        /// <summary>
        /// The EVSE admin status schedule.
        /// </summary>
        public IEnumerable<Timestamped<EVSEAdminStatusTypes>> AdminStatusSchedule
        {
            get
            {
                return _AdminStatusSchedule;
            }
        }

        #endregion


        #region ChargingStation

        private readonly RemoteChargingStation _ChargingStation;

        /// <summary>
        /// The charging station of this EVSE.
        /// </summary>
        [InternalUseOnly]
        public RemoteChargingStation ChargingStation
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

        #region OnAdminStatusChanged

        /// <summary>
        /// A delegate called whenever the admin status of the EVSE changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSE">The EVSE.</param>
        /// <param name="OldEVSEStatus">The old timestamped status of the EVSE.</param>
        /// <param name="NewEVSEStatus">The new timestamped status of the EVSE.</param>
        public delegate void OnAdminStatusChangedDelegate(DateTime Timestamp, EventTracking_Id EventTrackingId, RemoteEVSE EVSE, Timestamped<EVSEAdminStatusTypes> OldEVSEStatus, Timestamped<EVSEAdminStatusTypes> NewEVSEStatus);

        /// <summary>
        /// An event fired whenever the admin status of the EVSE changed.
        /// </summary>
        public event OnAdminStatusChangedDelegate OnAdminStatusChanged;

        #endregion

        #region OnStatusChanged

        /// <summary>
        /// A delegate called whenever the dynamic status of the EVSE changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSE">The EVSE.</param>
        /// <param name="OldEVSEStatus">The old timestamped status of the EVSE.</param>
        /// <param name="NewEVSEStatus">The new timestamped status of the EVSE.</param>
        public delegate void OnStatusChangedDelegate(DateTime Timestamp, EventTracking_Id EventTrackingId, RemoteEVSE EVSE, Timestamped<EVSEStatusTypes> OldEVSEStatus, Timestamped<EVSEStatusTypes> NewEVSEStatus);

        /// <summary>
        /// An event fired whenever the dynamic status of the EVSE changed.
        /// </summary>
        public event OnStatusChangedDelegate OnStatusChanged;

        #endregion

        #region OnNewReservation

        /// <summary>
        /// A delegate called whenever a reservation was created.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSE">The EVSE.</param>
        /// <param name="Reservation">The new charging reservation.</param>
        public delegate void OnNewReservationDelegate(DateTime Timestamp, EVSE EVSE, ChargingReservation Reservation);

        /// <summary>
        /// An event fired whenever reservation was created.
        /// </summary>
        public event OnNewReservationDelegate OnNewReservation;

        #endregion

        #region OnReservationDeleted

        /// <summary>
        /// A delegate called whenever a reservation was deleted.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSE">The EVSE.</param>
        /// <param name="Reservation">The deleted charging reservation.</param>
        public delegate void OnReservationDeletedDelegate(DateTime Timestamp, EVSE EVSE, ChargingReservation Reservation);

        /// <summary>
        /// An event fired whenever reservation was deleted.
        /// </summary>
        public event OnReservationDeletedDelegate OnReservationDeleted;

        #endregion

        #region SocketOutletAddition

        internal readonly IVotingNotificator<DateTime, EVSE, SocketOutlet, Boolean> SocketOutletAddition;

        /// <summary>
        /// Called whenever a socket outlet will be or was added.
        /// </summary>
        public IVotingSender<DateTime, EVSE, SocketOutlet, Boolean> OnSocketOutletAddition
        {
            get
            {
                return SocketOutletAddition;
            }
        }

        #endregion

        #region SocketOutletRemoval

        internal readonly IVotingNotificator<DateTime, EVSE, SocketOutlet, Boolean> SocketOutletRemoval;

        /// <summary>
        /// Called whenever a socket outlet will be or was removed.
        /// </summary>
        public IVotingSender<DateTime, EVSE, SocketOutlet, Boolean> OnSocketOutletRemoval
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
        internal RemoteEVSE(EVSE_Id                     Id,
                            RemoteChargingStation  ChargingStation,
                            UInt16                      MaxStatusListSize       = DefaultMaxEVSEStatusListSize,
                            UInt16                      MaxAdminStatusListSize  = DefaultMaxAdminStatusListSize)

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
            this._SocketOutlets         = new ReactiveSet<SocketOutlet>();

            this._StatusSchedule        = new StatusSchedule<EVSEStatusTypes>(MaxStatusListSize);
            this._StatusSchedule.Insert(EVSEStatusTypes.Unspecified);

            this._AdminStatusSchedule   = new StatusSchedule<EVSEAdminStatusTypes>(MaxStatusListSize);
            this._AdminStatusSchedule.Insert(EVSEAdminStatusTypes.Unspecified);

            #endregion

            #region Init events

            this.SocketOutletAddition   = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);
            this.SocketOutletRemoval    = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);

            #endregion

            #region Link events

            this._StatusSchedule.     OnStatusChanged += (Timestamp, EventTrackingId, StatusSchedule, OldStatus, NewStatus)
                                                          => UpdateStatus(Timestamp, EventTrackingId, OldStatus, NewStatus);

            this._AdminStatusSchedule.OnStatusChanged += (Timestamp, EventTrackingId, StatusSchedule, OldStatus, NewStatus)
                                                          => UpdateAdminStatus(Timestamp, EventTrackingId, OldStatus, NewStatus);


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
        /// <param name="NewStatus">A new status.</param>
        public void SetStatus(EVSEStatusTypes  NewStatus)
        {
            _StatusSchedule.Insert(NewStatus);
        }

        #endregion

        #region SetStatus(NewTimestampedStatus)

        /// <summary>
        /// Set the current status.
        /// </summary>
        /// <param name="NewTimestampedStatus">A new timestamped status.</param>
        public void SetStatus(Timestamped<EVSEStatusTypes> NewTimestampedStatus)
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
        public void SetStatus(EVSEStatusTypes  NewStatus,
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
        public void SetStatus(IEnumerable<Timestamped<EVSEStatusTypes>>  NewStatusList,
                              ChangeMethods                              ChangeMethod = ChangeMethods.Replace)
        {
            _StatusSchedule.Insert(NewStatusList);//, ChangeMethod);
        }

        #endregion


        #region SetAdminStatus(NewAdminStatus)

        /// <summary>
        /// Set the admin status.
        /// </summary>
        /// <param name="NewAdminStatus">A new timestamped admin status.</param>
        public void SetAdminStatus(EVSEAdminStatusTypes NewAdminStatus)
        {
            _AdminStatusSchedule.Insert(NewAdminStatus);
        }

        #endregion

        #region SetAdminStatus(NewTimestampedAdminStatus)

        /// <summary>
        /// Set the admin status.
        /// </summary>
        /// <param name="NewTimestampedAdminStatus">A new timestamped admin status.</param>
        public void SetAdminStatus(Timestamped<EVSEAdminStatusTypes> NewTimestampedAdminStatus)
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
        public void SetAdminStatus(EVSEAdminStatusTypes  NewAdminStatus,
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
        public void SetAdminStatus(IEnumerable<Timestamped<EVSEAdminStatusTypes>>  NewAdminStatusList,
                                   ChangeMethods                                   ChangeMethod = ChangeMethods.Replace)
        {
            _AdminStatusSchedule.Insert(NewAdminStatusList);//, ChangeMethod);
        }

        #endregion


        #region Reserve(...)

        public async Task<ReservationResult> Reserve(DateTime                          Timestamp,
                                                     CancellationToken                 CancellationToken,
                                                     EventTracking_Id                  EventTrackingId,
                                                     eMobilityProvider_Id              ProviderId,
                                                     RemoteAuthentication              RemoteAuthentication,
                                                     ChargingReservation_Id            ReservationId,
                                                     DateTime?                         StartTime,
                                                     TimeSpan?                         Duration,
                                                     ChargingProduct                   ChargingProduct  = null,
                                                     IEnumerable<Auth_Token>           AuthTokens       = null,
                                                     IEnumerable<eMobilityAccount_Id>  eMAIds           = null,
                                                     IEnumerable<UInt32>               PINs             = null,
                                                     TimeSpan?                         RequestTimeout   = null)
        {

            #region Try to remove an existing reservation if this is an update!

            if (ReservationId != null && _Reservation.Id != ReservationId)
            {

                return ReservationResult.UnknownChargingReservationId;

                // Send DeleteReservation event!

            }

            #endregion

            switch (Status.Value)
            {

                case EVSEStatusTypes.OutOfService:
                    return ReservationResult.OutOfService;

                case EVSEStatusTypes.Charging:
                    return ReservationResult.AlreadyInUse;

                case EVSEStatusTypes.Reserved:
                    return ReservationResult.AlreadyReserved;

                case EVSEStatusTypes.Available:

                    this._Reservation = new ChargingReservation(ReservationId,
                                                                Timestamp,
                                                                StartTime ?? DateTime.Now,
                                                                Duration  ?? MaxReservationDuration,
                                                                (StartTime ?? DateTime.Now) + (Duration ?? MaxReservationDuration),
                                                                TimeSpan.FromSeconds(0),
                                                                ChargingReservationLevel.EVSE,
                                                                ProviderId,
                                                                RemoteAuthentication,
                                                                null, //RoamingNetwork.Id,
                                                                null, //ChargingStation.ChargingPool.EVSEOperator.RoamingNetwork,
                                                                null, //ChargingStation.ChargingPool.Id,
                                                                ChargingStation.Id,
                                                                Id,
                                                                ChargingProduct,
                                                                AuthTokens,
                                                                eMAIds,
                                                                PINs);

                    SetStatus(EVSEStatusTypes.Reserved);

                    return ReservationResult.Success(_Reservation);

                default:
                    return ReservationResult.Error();

            }

        }

        #endregion

        #region RemoteStart => (...)

        /// <summary>
        /// Initiate a remote start of the given charging session at the given EVSE
        /// and for the given Provider/eMAId.
        /// </summary>
        /// <param name="ChargingProductId">The unique identification of the choosen charging product at the given EVSE.</param>
        /// <param name="ReservationId">The unique identification for a charging reservation.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="RemoteAuthentication">The unique identification of the e-mobility account.</param>
        /// <returns>A RemoteStartResult task.</returns>
        public async Task<RemoteStartResult> RemoteStart(DateTime                Timestamp,
                                                         CancellationToken       CancellationToken,
                                                         EventTracking_Id        EventTrackingId,
                                                         ChargingProduct_Id      ChargingProductId,
                                                         ChargingReservation_Id  ReservationId,
                                                         ChargingSession_Id      SessionId,
                                                         eMobilityProvider_Id    ProviderId,
                                                         RemoteAuthentication    RemoteAuthentication,
                                                         TimeSpan?               RequestTimeout  = null)
        {

            return RemoteStartResult.Offline();

            //if (_ChargingStation == null)
            //    return RemoteStartEVSEResult.Offline;

            //return await _ChargingStation.
            //                 RemoteStart(Timestamp,
            //                             CancellationToken,
            //                             EventTrackingId,
            //                             Id,
            //                             ChargingProductId,
            //                             ReservationId,
            //                             SessionId,
            //                             ProviderId,
            //                             eMAId,
            //                             RequestTimeout);

        }

        #endregion

        #region RemoteStop => (...)

        /// <summary>
        /// Initiate a remote stop of the given charging session at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ReservationHandling">Whether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <returns>A RemoteStopResult task.</returns>
        public async Task<RemoteStopResult> RemoteStop(DateTime              Timestamp,
                                                       CancellationToken     CancellationToken,
                                                       EventTracking_Id      EventTrackingId,
                                                       ChargingSession_Id    SessionId,
                                                       ReservationHandling   ReservationHandling,
                                                       eMobilityProvider_Id  ProviderId,
                                                       TimeSpan?             RequestTimeout  = null)
        {

            return RemoteStopResult.Offline(SessionId);

            //if (_ChargingStation == null)
            //    return RemoteStopEVSEResult.Offline(SessionId);

            //RemoteStopEVSEResult result = null;

            //var result2 = await _ChargingStation.
            //                        RemoteStop(Timestamp,
            //                                   CancellationToken,
            //                                   EventTrackingId,
            //                                   SessionId,
            //                                   ReservationHandling,
            //                                   ProviderId,
            //                                   RequestTimeout);

            //switch (result2.Result)
            //{

            //    case RemoteStopResultType.Error:
            //        result = RemoteStopEVSEResult.Error(SessionId, result2.ErrorMessage);
            //        break;


            //}

            //return result;

        }

        #endregion


        #region (internal) UpdateStatus(Timestamp, OldStatus, NewStatus)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal async Task UpdateStatus(DateTime                      Timestamp,
                                         EventTracking_Id              EventTrackingId,
                                         Timestamped<EVSEStatusTypes>  OldStatus,
                                         Timestamped<EVSEStatusTypes>  NewStatus)
        {

            OnStatusChanged?.Invoke(Timestamp, EventTrackingId, this, OldStatus, NewStatus);

        }

        #endregion

        #region (internal) UpdateAdminStatus(Timestamp, OldStatus, NewStatus)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="OldStatus">The old EVSE admin status.</param>
        /// <param name="NewStatus">The new EVSE admin status.</param>
        internal async Task UpdateAdminStatus(DateTime                           Timestamp,
                                              EventTracking_Id                   EventTrackingId,
                                              Timestamped<EVSEAdminStatusTypes>  OldStatus,
                                              Timestamped<EVSEAdminStatusTypes>  NewStatus)
        {

            OnAdminStatusChanged?.Invoke(Timestamp, EventTrackingId, this, OldStatus, NewStatus);

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


        #region IComparable<RemoteEVSE> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public override Int32 CompareTo(Object Object)
        {

            if (Object == null)
                throw new ArgumentNullException("The given object must not be null!");

            // Check if the given object is a virtual EVSE.
            var RemoteEVSE = Object as RemoteEVSE;
            if (RemoteEVSE is null)
                throw new ArgumentException("The given object is not a virtual EVSE!");

            return CompareTo(RemoteEVSE);

        }

        #endregion

        #region CompareTo(RemoteEVSE)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RemoteEVSE">An virtual EVSE to compare with.</param>
        public Int32 CompareTo(RemoteEVSE RemoteEVSE)
        {

            if (RemoteEVSE is null)
                throw new ArgumentNullException(nameof(RemoteEVSE),  "The given virtual EVSE must not be null!");

            return Id.CompareTo(RemoteEVSE.Id);

        }

        #endregion

        #endregion

        #region IEquatable<RemoteEVSE> Members

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
            var RemoteEVSE = Object as RemoteEVSE;
            if ((Object) RemoteEVSE == null)
                return false;

            return this.Equals(RemoteEVSE);

        }

        #endregion

        #region Equals(RemoteEVSE)

        /// <summary>
        /// Compares two virtual EVSEs for equality.
        /// </summary>
        /// <param name="RemoteEVSE">A virtual EVSE to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(RemoteEVSE RemoteEVSE)
        {

            if ((Object) RemoteEVSE == null)
                return false;

            return Id.Equals(RemoteEVSE.Id);

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
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
