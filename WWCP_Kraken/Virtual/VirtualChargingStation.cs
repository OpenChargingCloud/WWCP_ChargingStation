﻿/*
 * Copyright (c) 2014-2017 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.WWCP.ChargingPools;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// A demo implementation of a virtual WWCP charging station.
    /// </summary>
    public class VirtualChargingStation : AEMobilityEntity<ChargingStation_Id>,
                                          IEquatable<VirtualChargingStation>, IComparable<VirtualChargingStation>, IComparable,
                                          IStatus<ChargingStationStatusTypes>,
                                          IRemoteChargingStation
    {

        #region Data

        /// <summary>
        /// The default max size of the status history.
        /// </summary>
        public const UInt16 DefaultMaxStatusListSize = 50;

        /// <summary>
        /// The default max size of the admin status history.
        /// </summary>
        public const UInt16 DefaultMaxAdminStatusListSize = 50;

        /// <summary>
        /// The maximum time span for a reservation.
        /// </summary>
        public static readonly TimeSpan MaxReservationDuration = TimeSpan.FromMinutes(15);

        /// <summary>
        /// The default time span between self checks.
        /// </summary>
        public static readonly TimeSpan DefaultSelfCheckTimeSpan = TimeSpan.FromSeconds(3);

        private static readonly Object SelfCheckLock = new Object();
        private Timer _SelfCheckTimer;


        public const String DefaultWhiteListName = "default";

        #endregion

        #region Properties

        #region Id

        /// <summary>
        /// The unique identification of this virtual charging station.
        /// </summary>
        public ChargingStation_Id Id { get; }

        #endregion

        #region Description

        internal I18NString _Description;

        /// <summary>
        /// An optional (multi-language) description of this charging station.
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


        #region AdminStatus

        /// <summary>
        /// The current charging station admin status.
        /// </summary>
        [InternalUseOnly]
        public Timestamped<ChargingStationAdminStatusTypes> AdminStatus
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

        private StatusSchedule<ChargingStationAdminStatusTypes> _AdminStatusSchedule;

        /// <summary>
        /// The charging station admin status schedule.
        /// </summary>
        public IEnumerable<Timestamped<ChargingStationAdminStatusTypes>> AdminStatusSchedule
        {
            get
            {
                return _AdminStatusSchedule;
            }
        }

        #endregion

        #region Status

        /// <summary>
        /// The current charging station status.
        /// </summary>
        [InternalUseOnly]
        public Timestamped<ChargingStationStatusTypes> Status
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

        private StatusSchedule<ChargingStationStatusTypes> _StatusSchedule;

        /// <summary>
        /// The charging station status schedule.
        /// </summary>
        public IEnumerable<Timestamped<ChargingStationStatusTypes>> StatusSchedule
        {
            get
            {
                return _StatusSchedule;
            }
        }

        #endregion


        #region UseWhiteLists

        private Boolean _UseWhiteLists;

        /// <summary>
        /// The authentication white lists.
        /// </summary>
        public Boolean UseWhiteLists
        {

            get
            {
                return _UseWhiteLists;
            }

            set
            {
                _UseWhiteLists = value;
            }

        }

        #endregion

        #region WhiteLists

        private readonly Dictionary<String, HashSet<AuthIdentification>> _WhiteLists;

        /// <summary>
        /// The authentication white lists.
        /// </summary>
        [InternalUseOnly]
        public Dictionary<String, HashSet<AuthIdentification>> WhiteLists
        {
            get
            {
                return _WhiteLists;
            }
        }

        #endregion

        #region DefaultWhiteList

        /// <summary>
        /// The authentication white lists.
        /// </summary>
        [InternalUseOnly]
        public HashSet<AuthIdentification> DefaultWhiteList
        {
            get
            {
                return _WhiteLists[DefaultWhiteListName];
            }
        }

        #endregion


        #region SelfCheckTimeSpan

        private readonly TimeSpan _SelfCheckTimeSpan;

        /// <summary>
        /// The time span between self checks.
        /// </summary>
        public TimeSpan SelfCheckTimeSpan
        {
            get
            {
                return _SelfCheckTimeSpan;
            }
        }

        #endregion

        #endregion

        #region Links

        /// <summary>
        /// The optional linked charging pool.
        /// </summary>
        public VirtualChargingPool  ChargingPool      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a virtual charging station.
        /// </summary>
        /// <param name="Id">The unique identification of this EVSE.</param>
        /// <param name="ChargingPool">The parent virtual charging pool.</param>
        /// <param name="SelfCheckTimeSpan">The time span between self checks.</param>
        /// <param name="MaxStatusListSize">The maximum size of the charging station status list.</param>
        /// <param name="MaxAdminStatusListSize">The maximum size of the charging station admin status list.</param>
        public VirtualChargingStation(ChargingStation_Id               Id,
                                      VirtualChargingPool              ChargingPool,
                                      ChargingStationAdminStatusTypes  InitialAdminStatus       = ChargingStationAdminStatusTypes.Operational,
                                      ChargingStationStatusTypes       InitialStatus            = ChargingStationStatusTypes.Available,
                                      UInt16                           MaxAdminStatusListSize   = DefaultMaxAdminStatusListSize,
                                      UInt16                           MaxStatusListSize        = DefaultMaxStatusListSize,
                                      TimeSpan?                        SelfCheckTimeSpan        = null)

            : base(Id)

        {

            #region Initial checks

            //if (ChargingPool == null)
            //    throw new ArgumentNullException(nameof(ChargingPool), "The given charging pool must not be null!");

            #endregion

            #region Init data and properties

            this.ChargingPool          = ChargingPool;
            this._EVSEs                = new HashSet<IRemoteEVSE>();

            this._AdminStatusSchedule  = new StatusSchedule<ChargingStationAdminStatusTypes>(MaxAdminStatusListSize);
            this._AdminStatusSchedule.Insert(InitialAdminStatus);

            this._StatusSchedule       = new StatusSchedule<ChargingStationStatusTypes>(MaxStatusListSize);
            this._StatusSchedule.Insert(InitialStatus);

            this._WhiteLists           = new Dictionary<String, HashSet<AuthIdentification>>();
            _WhiteLists.Add("default", new HashSet<AuthIdentification>());

            this._SelfCheckTimeSpan    = SelfCheckTimeSpan != null && SelfCheckTimeSpan.HasValue ? SelfCheckTimeSpan.Value : DefaultSelfCheckTimeSpan;
            this._SelfCheckTimer       = new Timer(SelfCheck, null, _SelfCheckTimeSpan, _SelfCheckTimeSpan);

            #endregion

            #region Link events

            this._AdminStatusSchedule.OnStatusChanged += (Timestamp, EventTrackingId, StatusSchedule, OldStatus, NewStatus)
                                                          => UpdateAdminStatus(Timestamp, EventTrackingId, OldStatus, NewStatus);

            this._StatusSchedule.     OnStatusChanged += (Timestamp, EventTrackingId, StatusSchedule, OldStatus, NewStatus)
                                                          => UpdateStatus(Timestamp, EventTrackingId, OldStatus, NewStatus);

            #endregion

        }

        #endregion


        public ChargingStation_Id     RemoteChargingStationId    { get; set; }

        public String                 RemoteEVSEIdPrefix         { get; set; }

        public void AddMapping(EVSE_Id LocalEVSEId,
                               EVSE_Id RemoteEVSEId)
        {
        }


        #region (Admin-)Status management

        #region OnData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data of the charging station changed.
        /// </summary>
        public event OnRemoteChargingStationDataChangedDelegate         OnDataChanged;

        /// <summary>
        /// An event fired whenever the admin status of the charging station changed.
        /// </summary>
        public event OnRemoteChargingStationAdminStatusChangedDelegate  OnAdminStatusChanged;

        /// <summary>
        /// An event fired whenever the dynamic status of the charging station changed.
        /// </summary>
        public event OnRemoteChargingStationStatusChangedDelegate       OnStatusChanged;

        #endregion


        #region SetAdminStatus(NewAdminStatus)

        /// <summary>
        /// Set the admin status.
        /// </summary>
        /// <param name="NewAdminStatus">A new timestamped admin status.</param>
        public void SetAdminStatus(ChargingStationAdminStatusTypes  NewAdminStatus)
        {
            _AdminStatusSchedule.Insert(NewAdminStatus);
        }

        #endregion

        #region SetAdminStatus(NewTimestampedAdminStatus)

        /// <summary>
        /// Set the admin status.
        /// </summary>
        /// <param name="NewTimestampedAdminStatus">A new timestamped admin status.</param>
        public void SetAdminStatus(Timestamped<ChargingStationAdminStatusTypes> NewTimestampedAdminStatus)
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
        public void SetAdminStatus(ChargingStationAdminStatusTypes  NewAdminStatus,
                                   DateTime                         Timestamp)
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
        public void SetAdminStatus(IEnumerable<Timestamped<ChargingStationAdminStatusTypes>>  NewAdminStatusList,
                                   ChangeMethods                                              ChangeMethod = ChangeMethods.Replace)
        {
            _AdminStatusSchedule.Insert(NewAdminStatusList, ChangeMethod);
        }

        #endregion


        #region SetStatus(NewStatus)

        /// <summary>
        /// Set the current status.
        /// </summary>
        /// <param name="NewStatus">A new status.</param>
        public void SetStatus(ChargingStationStatusTypes  NewStatus)
        {
            _StatusSchedule.Insert(NewStatus);
        }

        #endregion

        #region SetStatus(NewTimestampedStatus)

        /// <summary>
        /// Set the current status.
        /// </summary>
        /// <param name="NewTimestampedStatus">A new timestamped status.</param>
        public void SetStatus(Timestamped<ChargingStationStatusTypes> NewTimestampedStatus)
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
        public void SetStatus(ChargingStationStatusTypes  NewStatus,
                              DateTime                   Timestamp)
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
        public void SetStatus(IEnumerable<Timestamped<ChargingStationStatusTypes>>  NewStatusList,
                              ChangeMethods                                        ChangeMethod = ChangeMethods.Replace)
        {
            _StatusSchedule.Insert(NewStatusList, ChangeMethod);
        }

        #endregion


        #region (internal) UpdateAdminStatus(Timestamp, EventTrackingId, OldStatus, NewStatus)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="OldStatus">The old EVSE admin status.</param>
        /// <param name="NewStatus">The new EVSE admin status.</param>
        internal async Task UpdateAdminStatus(DateTime                                      Timestamp,
                                              EventTracking_Id                              EventTrackingId,
                                              Timestamped<ChargingStationAdminStatusTypes>  OldStatus,
                                              Timestamped<ChargingStationAdminStatusTypes>  NewStatus)
        {

            OnAdminStatusChanged?.Invoke(Timestamp,
                                         EventTrackingId,
                                         this,
                                         OldStatus,
                                         NewStatus);

        }

        #endregion

        #region (internal) UpdateStatus     (Timestamp, EventTrackingId, OldStatus, NewStatus)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal async Task UpdateStatus(DateTime                                 Timestamp,
                                         EventTracking_Id                         EventTrackingId,
                                         Timestamped<ChargingStationStatusTypes>  OldStatus,
                                         Timestamped<ChargingStationStatusTypes>  NewStatus)
        {

            OnStatusChanged?.Invoke(Timestamp,
                                    EventTrackingId,
                                    this,
                                    OldStatus,
                                    NewStatus);

        }

        #endregion

        #endregion

        #region (private) SelfCheck(Context)

        private void SelfCheck(Object Context)
        {

            if (Monitor.TryEnter(SelfCheckLock))
            {

                try
                {

                    foreach (var _EVSE in _EVSEs)
                        _EVSE.CheckIfReservationIsExpired().Wait();

                }
                catch (Exception e)
                {

                    while (e.InnerException != null)
                        e = e.InnerException;

                    DebugX.LogT("VirtualChargingStation SelfCheck() '" + Id + "' led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);

                }

                finally
                {
                    Monitor.Exit(SelfCheckLock);
                }

            }

        }

        #endregion


        #region EVSEs...

        #region EVSEs

        private readonly HashSet<IRemoteEVSE> _EVSEs;

        /// <summary>
        /// All registered EVSEs.
        /// </summary>
        public IEnumerable<IRemoteEVSE> EVSEs
            => _EVSEs;

        #endregion

        #region CreateVirtualEVSE(EVSEId, Configurator = null, OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new EVSE having the given
        /// unique EVSE identification.
        /// </summary>
        /// <param name="EVSEId">The unique identification of the new EVSE.</param>
        /// <param name="Configurator">An optional delegate to configure the new EVSE after its creation.</param>
        /// <param name="OnSuccess">An optional delegate called after successful creation of the EVSE.</param>
        /// <param name="OnError">An optional delegate for signaling errors.</param>
        public VirtualEVSE CreateVirtualEVSE(EVSE_Id                       EVSEId,
                                             EVSEAdminStatusTypes          InitialAdminStatus       = EVSEAdminStatusTypes.Operational,
                                             EVSEStatusTypes               InitialStatus            = EVSEStatusTypes.Available,
                                             UInt16                        MaxAdminStatusListSize   = DefaultMaxAdminStatusListSize,
                                             UInt16                        MaxStatusListSize        = DefaultMaxStatusListSize,
                                             Action<VirtualEVSE>           Configurator             = null,
                                             Action<VirtualEVSE>           OnSuccess                = null,
                                             Action<VirtualEVSE, EVSE_Id>  OnError                  = null)
        {

            #region Initial checks

            if (_EVSEs.Any(evse => evse.Id == EVSEId))
            {
                throw new Exception("EVSEAlreadyExistsInStation");
               // if (OnError == null)
               //     throw new EVSEAlreadyExistsInStation(this.ChargingStation, EVSEId);
               // else
               //     OnError?.Invoke(this, EVSEId);
            }

            #endregion

            var Now           = DateTime.Now;
            var _VirtualEVSE  = new VirtualEVSE(EVSEId,
                                                this,
                                                InitialAdminStatus,
                                                InitialStatus,
                                                MaxAdminStatusListSize,
                                                MaxStatusListSize);

            Configurator?.Invoke(_VirtualEVSE);

            if (_EVSEs.Add(_VirtualEVSE))
            {

                //_VirtualChargingStation.OnPropertyChanged        += (Timestamp, Sender, PropertyName, OldValue, NewValue)
                //                                           => UpdateEVSEData(Timestamp, Sender as VirtualChargingStation, PropertyName, OldValue, NewValue);
                //
                //_VirtualChargingStation.OnStatusChanged          += UpdateEVSEStatus;
                //_VirtualChargingStation.OnAdminStatusChanged     += UpdateEVSEAdminStatus;
                //_VirtualChargingStation.OnNewReservation         += SendNewReservation;
                //_VirtualChargingStation.OnNewChargingSession     += SendNewChargingSession;
                //_VirtualChargingStation.OnNewChargeDetailRecord  += SendNewChargeDetailRecord;
                //_VirtualChargingStation.OnReservationCancelled   += SendOnReservationCancelled;

                OnSuccess?.Invoke(_VirtualEVSE);

                return _VirtualEVSE;

            }

            return null;

        }

        #endregion

        public IRemoteEVSE AddEVSE(IRemoteEVSE                       EVSE,
                                   Action<EVSE>                      Configurator  = null,
                                   Action<EVSE>                      OnSuccess     = null,
                                   Action<ChargingStation, EVSE_Id>  OnError       = null)

        {

            _EVSEs.Add(EVSE);

            return EVSE;

        }


        #region ContainsEVSE(EVSE)

        /// <summary>
        /// Check if the given EVSE is already present within the charging station.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        public Boolean ContainsEVSE(EVSE EVSE)
            => _EVSEs.Any(evse => evse.Id == EVSE.Id);

        #endregion

        #region ContainsEVSE(EVSEId)

        /// <summary>
        /// Check if the given EVSE identification is already present within the charging station.
        /// </summary>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        public Boolean ContainsEVSE(EVSE_Id EVSEId)
            => _EVSEs.Any(evse => evse.Id == EVSEId);

        #endregion

        #region GetEVSEbyId(EVSEId)

        public IRemoteEVSE GetEVSEbyId(EVSE_Id EVSEId)
            => _EVSEs.FirstOrDefault(evse => evse.Id == EVSEId);

        #endregion

        #region TryGetEVSEbyId(EVSEId, out EVSE)

        public Boolean TryGetEVSEbyId(EVSE_Id EVSEId, out IRemoteEVSE EVSE)
        {

            EVSE = GetEVSEbyId(EVSEId);

            return EVSE != null;

        }

        #endregion


        #region OnRemoteEVSEData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data of any subordinated EVSE changed.
        /// </summary>
        public event OnRemoteEVSEDataChangedDelegate         OnEVSEDataChanged;

        /// <summary>
        /// An event fired whenever the dynamic status of any subordinated EVSE changed.
        /// </summary>
        public event OnRemoteEVSEStatusChangedDelegate       OnEVSEStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status of any subordinated EVSE changed.
        /// </summary>
        public event OnRemoteEVSEAdminStatusChangedDelegate  OnEVSEAdminStatusChanged;

        #endregion

        #region (internal) UpdateEVSEData(Timestamp, RemoteEVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update the data of a remote EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="RemoteEVSE">The remote EVSE.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        internal void UpdateEVSEData(DateTime     Timestamp,
                                     IRemoteEVSE  RemoteEVSE,
                                     String       PropertyName,
                                     Object       OldValue,
                                     Object       NewValue)
        {

            var OnEVSEDataChangedLocal = OnEVSEDataChanged;
            if (OnEVSEDataChangedLocal != null)
                OnEVSEDataChangedLocal(Timestamp, RemoteEVSE, PropertyName, OldValue, NewValue);

        }

        #endregion

        #region (internal) UpdateEVSEAdminStatus(Timestamp, EventTrackingId, RemoteEVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update the current charging station status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An event tracking identification for correlating this request with other events.</param>
        /// <param name="RemoteEVSE">The updated remote EVSE.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal async Task UpdateEVSEAdminStatus(DateTime                          Timestamp,
                                                  EventTracking_Id                  EventTrackingId,
                                                  IRemoteEVSE                       RemoteEVSE,
                                                  Timestamped<EVSEAdminStatusTypes>  OldStatus,
                                                  Timestamped<EVSEAdminStatusTypes>  NewStatus)
        {

            var OnEVSEAdminStatusChangedLocal = OnEVSEAdminStatusChanged;
            if (OnEVSEAdminStatusChangedLocal != null)
                await OnEVSEAdminStatusChangedLocal(Timestamp,
                                                    EventTrackingId,
                                                    RemoteEVSE,
                                                    OldStatus,
                                                    NewStatus);

        }

        #endregion

        #region (internal) UpdateEVSEStatus     (Timestamp, EventTrackingId, RemoteEVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update the remote EVSE station status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An event tracking identification for correlating this request with other events.</param>
        /// <param name="RemoteEVSE">The updated EVSE.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal async Task UpdateEVSEStatus(DateTime                     Timestamp,
                                             EventTracking_Id             EventTrackingId,
                                             IRemoteEVSE                  RemoteEVSE,
                                             Timestamped<EVSEStatusTypes>  OldStatus,
                                             Timestamped<EVSEStatusTypes>  NewStatus)
        {

            var OnEVSEStatusChangedLocal = OnEVSEStatusChanged;
            if (OnEVSEStatusChangedLocal != null)
                await OnEVSEStatusChangedLocal(Timestamp,
                                               EventTrackingId,
                                               RemoteEVSE,
                                               OldStatus,
                                               NewStatus);

        }

        #endregion


        IRemoteEVSE IRemoteChargingStation.CreateNewEVSE(EVSE_Id EVSEId, Action<EVSE> Configurator = null, Action<EVSE> OnSuccess = null, Action<ChargingStation, EVSE_Id> OnError = null)
        {
            throw new NotImplementedException();
        }


        // Socket events

        #region SocketOutletAddition

        internal readonly IVotingNotificator<DateTime, VirtualChargingStation, SocketOutlet, Boolean> SocketOutletAddition;

        /// <summary>
        /// Called whenever a socket outlet will be or was added.
        /// </summary>
        public IVotingSender<DateTime, VirtualChargingStation, SocketOutlet, Boolean> OnSocketOutletAddition
        {
            get
            {
                return SocketOutletAddition;
            }
        }

        #endregion

        #region SocketOutletRemoval

        internal readonly IVotingNotificator<DateTime, VirtualChargingStation, SocketOutlet, Boolean> SocketOutletRemoval;

        /// <summary>
        /// Called whenever a socket outlet will be or was removed.
        /// </summary>
        public IVotingSender<DateTime, VirtualChargingStation, SocketOutlet, Boolean> OnSocketOutletRemoval
        {
            get
            {
                return SocketOutletRemoval;
            }
        }

        #endregion


        #region GetEVSEStatus(...)

        public async Task<IEnumerable<EVSEStatus>> GetEVSEStatus(DateTime           Timestamp,
                                                                 CancellationToken  CancellationToken,
                                                                 EventTracking_Id   EventTrackingId,
                                                                 TimeSpan?          RequestTimeout  = null)

            => _EVSEs.Select(evse => new EVSEStatus(evse.Id,
                                                    evse.Status.Value,
                                                    evse.Status.Timestamp));

        #endregion

        #endregion

        #region Reservations...

        #region Reserve(...        StartTime, Duration, ReservationId = null, ProviderId = null, eMAId = null,...)

        /// <summary>
        /// Reserve the possibility to charge at the given EVSE.
        /// </summary>
        /// <param name="StartTime">The starting time of the reservation.</param>
        /// <param name="Duration">The duration of the reservation.</param>
        /// <param name="ReservationId">An optional unique identification of the reservation. Mandatory for updates.</param>
        /// <param name="ProviderId">An optional unique identification of e-Mobility service provider.</param>
        /// <param name="Identification">An optional unique identification of e-Mobility account/customer requesting this reservation.</param>
        /// <param name="ChargingProduct">The charging product to be reserved.</param>
        /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
        /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
        /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<ReservationResult>

            Reserve(DateTime?                         StartTime,
                    TimeSpan?                         Duration,
                    ChargingReservation_Id?           ReservationId       = null,
                    eMobilityProvider_Id?             ProviderId          = null,
                    AuthIdentification                Identification      = null,
                    ChargingProduct                   ChargingProduct     = null,
                    IEnumerable<Auth_Token>           AuthTokens          = null,
                    IEnumerable<eMobilityAccount_Id>  eMAIds              = null,
                    IEnumerable<UInt32>               PINs                = null,

                    DateTime?                         Timestamp           = null,
                    CancellationToken?                CancellationToken   = null,
                    EventTracking_Id                  EventTrackingId     = null,
                    TimeSpan?                         RequestTimeout      = null)

        {

            #region Initial checks

            if (!Timestamp.HasValue)
                Timestamp = DateTime.Now;

            if (!CancellationToken.HasValue)
                CancellationToken = new CancellationTokenSource().Token;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;

            #endregion

            if (AdminStatus.Value == ChargingStationAdminStatusTypes.Operational ||
                AdminStatus.Value == ChargingStationAdminStatusTypes.InternalUse)
            {

                #region Check if the eMAId is on the white list

                if (_UseWhiteLists &&
                   !_WhiteLists["default"].Contains(Identification))
                    return ReservationResult.InvalidCredentials;

                #endregion

                // ReserveNow!
                // Later this could also be a delayed reservation!

                var _EVSE = _EVSEs.FirstOrDefault(evse => evse.Status.Value == EVSEStatusTypes.Available);

                if (_EVSE == null)
                    return ReservationResult.NoEVSEsAvailable;


                return await _EVSE.Reserve(ChargingReservationLevel.ChargingStation,
                                           StartTime,
                                           Duration,
                                           ReservationId,
                                           ProviderId,
                                           Identification,
                                           ChargingProduct,
                                           AuthTokens,
                                           eMAIds,
                                           PINs,

                                           Timestamp,
                                           CancellationToken,
                                           EventTrackingId,
                                           RequestTimeout);

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

        #region Reserve(...EVSEId, StartTime, Duration, ReservationId = null, ProviderId = null, eMAId = null,...)

        /// <summary>
        /// Reserve the possibility to charge at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of the EVSE to be reserved.</param>
        /// <param name="StartTime">The starting time of the reservation.</param>
        /// <param name="Duration">The duration of the reservation.</param>
        /// <param name="ReservationId">An optional unique identification of the reservation. Mandatory for updates.</param>
        /// <param name="ProviderId">An optional unique identification of e-Mobility service provider.</param>
        /// <param name="Identification">An optional unique identification of e-Mobility account/customer requesting this reservation.</param>
        /// <param name="ChargingProduct">The charging product to be reserved.</param>
        /// <param name="AuthTokens">A list of authentication tokens, who can use this reservation.</param>
        /// <param name="eMAIds">A list of eMobility account identifications, who can use this reservation.</param>
        /// <param name="PINs">A list of PINs, who can be entered into a pinpad to use this reservation.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<ReservationResult>

            Reserve(EVSE_Id                           EVSEId,
                    DateTime?                         StartTime,
                    TimeSpan?                         Duration,
                    ChargingReservation_Id?           ReservationId       = null,
                    eMobilityProvider_Id?             ProviderId          = null,
                    AuthIdentification                Identification      = null,
                    ChargingProduct                   ChargingProduct     = null,
                    IEnumerable<Auth_Token>           AuthTokens          = null,
                    IEnumerable<eMobilityAccount_Id>  eMAIds              = null,
                    IEnumerable<UInt32>               PINs                = null,

                    DateTime?                         Timestamp           = null,
                    CancellationToken?                CancellationToken   = null,
                    EventTracking_Id                  EventTrackingId     = null,
                    TimeSpan?                         RequestTimeout      = null)

        {

            #region Initial checks

            if (!Timestamp.HasValue)
                Timestamp = DateTime.Now;

            if (!CancellationToken.HasValue)
                CancellationToken = new CancellationTokenSource().Token;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;


            ReservationResult result = null;

            #endregion

            if (AdminStatus.Value == ChargingStationAdminStatusTypes.Operational ||
                AdminStatus.Value == ChargingStationAdminStatusTypes.InternalUse)
            {

                #region Check if the eMAId is on the white list

                if (_UseWhiteLists &&
                   !_WhiteLists["default"].Contains(Identification))
                    return ReservationResult.InvalidCredentials;

                #endregion

                var _VirtualChargingStation = _EVSEs.FirstOrDefault(evse => evse.Id == EVSEId);

                if (_VirtualChargingStation != null)
                {

                    result = await _VirtualChargingStation.Reserve(StartTime,
                                                        Duration,
                                                        ReservationId,
                                                        ProviderId,
                                                        Identification,
                                                        ChargingProduct,
                                                        AuthTokens,
                                                        eMAIds,
                                                        PINs,

                                                        Timestamp,
                                                        CancellationToken,
                                                        EventTrackingId,
                                                        RequestTimeout);

                }

                else
                    result = ReservationResult.UnknownEVSE;


                return result;

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

        #region ChargingReservations

        /// <summary>
        /// All current charging reservations.
        /// </summary>

        public IEnumerable<ChargingReservation> ChargingReservations

            => _EVSEs.
                   Select(evse => evse.Reservation).
                   Where(reservation => reservation != null);

        #endregion

        #region OnNewReservation

        /// <summary>
        /// An event fired whenever a new charging reservation was created.
        /// </summary>
        public event OnNewReservationDelegate OnNewReservation;


        internal void SendNewReservation(DateTime             Timestamp,
                                         Object               Sender,
                                         ChargingReservation  Reservation)
        {

            OnNewReservation?.Invoke(Timestamp, Sender, Reservation);

        }

        #endregion


        #region TryGetReservationById(ReservationId, out Reservation)

        /// <summary>
        /// Return the charging reservation specified by its unique identification.
        /// </summary>
        /// <param name="ReservationId">The charging reservation identification.</param>
        /// <param name="Reservation">The charging reservation identification.</param>
        /// <returns>True when successful, false otherwise.</returns>
        public Boolean TryGetReservationById(ChargingReservation_Id ReservationId, out ChargingReservation Reservation)
        {

            Reservation = _EVSEs.Where (evse => evse.Reservation != null &&
                                                evse.Reservation.Id == ReservationId).
                                 Select(evse => evse.Reservation).
                                 FirstOrDefault();

            return Reservation != null;

        }

        #endregion


        #region CancelReservation(...ReservationId, Reason, ProviderId = null, EVSEId = null, ...)

        /// <summary>
        /// Try to remove the given charging reservation.
        /// </summary>
        /// <param name="ReservationId">The unique charging reservation identification.</param>
        /// <param name="Reason">A reason for this cancellation.</param>
        /// <param name="ProviderId">An optional unique identification of e-Mobility service provider.</param>
        /// <param name="EVSEId">An optional identification of the EVSE.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<CancelReservationResult>

            CancelReservation(ChargingReservation_Id                 ReservationId,
                              ChargingReservationCancellationReason  Reason,
                              eMobilityProvider_Id?                  ProviderId          = null,
                              EVSE_Id?                               EVSEId              = null,

                              DateTime?                              Timestamp           = null,
                              CancellationToken?                     CancellationToken   = null,
                              EventTracking_Id                       EventTrackingId     = null,
                              TimeSpan?                              RequestTimeout      = null)

        {

            #region Initial checks

            if (!Timestamp.HasValue)
                Timestamp = DateTime.Now;

            if (!CancellationToken.HasValue)
                CancellationToken = new CancellationTokenSource().Token;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;


            CancelReservationResult result = null;

            #endregion

            #region Check admin status

            if (AdminStatus.Value != ChargingStationAdminStatusTypes.Operational &&
                AdminStatus.Value != ChargingStationAdminStatusTypes.InternalUse)
                return CancelReservationResult.OutOfService(ReservationId,
                                                            Reason);

            #endregion


            var _Reservation = ChargingReservations.FirstOrDefault(reservation => reservation.Id == ReservationId);

            if (_Reservation        != null &&
                _Reservation.EVSEId.HasValue)
            {

                result = await GetEVSEbyId(_Reservation.EVSEId.Value).
                                   CancelReservation(ReservationId,
                                                     Reason,
                                                     ProviderId,

                                                     Timestamp,
                                                     CancellationToken,
                                                     EventTrackingId,
                                                     RequestTimeout);

                if (result.Result != CancelReservationResults.UnknownReservationId)
                    return result;

            }

            foreach (var _EVSE in _EVSEs)
            {

                result = await _EVSE.CancelReservation(ReservationId,
                                                       Reason,
                                                       ProviderId,

                                                       Timestamp,
                                                       CancellationToken,
                                                       EventTrackingId,
                                                       RequestTimeout);

                if (result.Result != CancelReservationResults.UnknownReservationId)
                    return result;

            }

            return CancelReservationResult.UnknownReservationId(ReservationId,
                                                                Reason);

        }

        #endregion

        #region OnReservationCancelled

        /// <summary>
        /// An event fired whenever a charging reservation was deleted.
        /// </summary>
        public event OnCancelReservationResponseDelegate OnReservationCancelled;


        internal void SendOnReservationCancelled(DateTime                               LogTimestamp,
                                                 DateTime                               RequestTimestamp,
                                                 Object                                 Sender,
                                                 EventTracking_Id                       EventTrackingId,

                                                 RoamingNetwork_Id?                     RoamingNetworkId,
                                                 eMobilityProvider_Id?                  ProviderId,
                                                 ChargingReservation_Id                 ReservationId,
                                                 ChargingReservation                    Reservation,
                                                 ChargingReservationCancellationReason  Reason,
                                                 CancelReservationResult                Result,
                                                 TimeSpan                               Runtime,
                                                 TimeSpan?                              RequestTimeout)
        {

            OnReservationCancelled?.Invoke(LogTimestamp,
                                           RequestTimestamp,
                                           Sender,
                                           EventTrackingId,
                                           RoamingNetworkId,
                                           ProviderId,
                                           ReservationId,
                                           Reservation,
                                           Reason,
                                           Result,
                                           Runtime,
                                           RequestTimeout);

        }

        #endregion

        #endregion

        #region RemoteStart/-Stop and Sessions

        #region RemoteStart(...EVSEId, ChargingProduct = null, ReservationId = null, SessionId = null, ProviderId = null, eMAId = null, ...)

        /// <summary>
        /// Start a charging session at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of the EVSE to be started.</param>
        /// <param name="ChargingProduct">The choosen charging product.</param>
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

            RemoteStart(EVSE_Id                  EVSEId,
                        ChargingProduct          ChargingProduct     = null,
                        ChargingReservation_Id?  ReservationId       = null,
                        ChargingSession_Id?      SessionId           = null,
                        eMobilityProvider_Id?    ProviderId          = null,
                        eMobilityAccount_Id?     eMAId               = null,

                        DateTime?                Timestamp           = null,
                        CancellationToken?       CancellationToken   = null,
                        EventTracking_Id         EventTrackingId     = null,
                        TimeSpan?                RequestTimeout      = null)
        {

            #region Initial checks

            if (!Timestamp.HasValue)
                Timestamp = DateTime.Now;

            if (!CancellationToken.HasValue)
                CancellationToken = new CancellationTokenSource().Token;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;


            RemoteStartEVSEResult result = null;

            #endregion

            if (AdminStatus.Value == ChargingStationAdminStatusTypes.Operational ||
                AdminStatus.Value == ChargingStationAdminStatusTypes.InternalUse)
            {

                #region Check if the eMAId is on the white list

                if (_UseWhiteLists &&
                   !_WhiteLists["default"].Contains(AuthIdentification.FromRemoteIdentification(eMAId.Value)))
                    return RemoteStartEVSEResult.InvalidCredentials;

                #endregion

                var _VirtualChargingStation = GetEVSEbyId(EVSEId);

                if (_VirtualChargingStation == null)
                    result = RemoteStartEVSEResult.UnknownEVSE;


                return await _VirtualChargingStation.
                                 RemoteStart(ChargingProduct,
                                             ReservationId,
                                             SessionId,
                                             ProviderId,
                                             eMAId,

                                             Timestamp,
                                             CancellationToken,
                                             EventTrackingId,
                                             RequestTimeout);

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

        #region RemoteStart(...        ChargingProduct = null, ReservationId = null, SessionId = null, ProviderId = null, eMAId = null, ...)

        /// <summary>
        /// Start a charging session at the given charging station.
        /// </summary>
        /// <param name="ChargingProduct">The choosen charging product.</param>
        /// <param name="ReservationId">The unique identification for a charging reservation.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider for the case it is different from the current message sender.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStartChargingStationResult>

            RemoteStart(ChargingProduct          ChargingProduct     = null,
                        ChargingReservation_Id?  ReservationId       = null,
                        ChargingSession_Id?      SessionId           = null,
                        eMobilityProvider_Id?    ProviderId          = null,
                        eMobilityAccount_Id?     eMAId               = null,

                        DateTime?                Timestamp           = null,
                        CancellationToken?       CancellationToken   = null,
                        EventTracking_Id         EventTrackingId     = null,
                        TimeSpan?                RequestTimeout      = null)

        {

            #region Initial checks

            if (!Timestamp.HasValue)
                Timestamp = DateTime.Now;

            if (!CancellationToken.HasValue)
                CancellationToken = new CancellationTokenSource().Token;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;

            #endregion


            if (AdminStatus.Value == ChargingStationAdminStatusTypes.Operational ||
                AdminStatus.Value == ChargingStationAdminStatusTypes.InternalUse)
            {

                #region Check if the eMAId is on the white list

                if (_UseWhiteLists &&
                   !_WhiteLists["default"].Contains(AuthIdentification.FromRemoteIdentification(eMAId.Value)))
                    return RemoteStartChargingStationResult.InvalidCredentials;

                #endregion

                var _VirtualChargingStation = _EVSEs.FirstOrDefault(evse => evse.Status.Value == EVSEStatusTypes.Available);

                if (_VirtualChargingStation == null)
                    return RemoteStartChargingStationResult.NoEVSEsAvailable;


                var result = await _VirtualChargingStation.
                                       RemoteStart(ChargingProduct,
                                                   ReservationId,
                                                   SessionId,
                                                   ProviderId,
                                                   eMAId,

                                                   Timestamp,
                                                   CancellationToken,
                                                   EventTrackingId,
                                                   RequestTimeout);


                switch (result.Result)
                {

                    case RemoteStartEVSEResultType.Error:
                        return RemoteStartChargingStationResult.Error(result.Message);

                    case RemoteStartEVSEResultType.InternalUse:
                        return RemoteStartChargingStationResult.InternalUse;

                    case RemoteStartEVSEResultType.InvalidCredentials:
                        return RemoteStartChargingStationResult.InvalidCredentials;

                    case RemoteStartEVSEResultType.InvalidSessionId:
                        return RemoteStartChargingStationResult.InvalidSessionId;

                    case RemoteStartEVSEResultType.Offline:
                        return RemoteStartChargingStationResult.Offline;

                    case RemoteStartEVSEResultType.OutOfService:
                        return RemoteStartChargingStationResult.OutOfService;

                    case RemoteStartEVSEResultType.Reserved:
                        return RemoteStartChargingStationResult.Reserved;

                    case RemoteStartEVSEResultType.Success:
                        if (result.Session != null)
                            return RemoteStartChargingStationResult.Success(result.Session);
                        else
                            return RemoteStartChargingStationResult.Success();

                    case RemoteStartEVSEResultType.Timeout:
                        return RemoteStartChargingStationResult.Timeout;

                    case RemoteStartEVSEResultType.UnknownOperator:
                        return RemoteStartChargingStationResult.UnknownOperator;

                    case RemoteStartEVSEResultType.Unspecified:
                        return RemoteStartChargingStationResult.Unspecified;

                }

                return RemoteStartChargingStationResult.Error("Could not start charging!");

            }
            else
            {

                switch (AdminStatus.Value)
                {

                    default:
                        return RemoteStartChargingStationResult.OutOfService;

                }

            }

        }

        #endregion

        #region ChargingSessions

        /// <summary>
        /// All current charging sessions.
        /// </summary>

        public IEnumerable<ChargingSession> ChargingSessions

            => _EVSEs.
                   Select(evse => evse.ChargingSession).
                   Where(session => session != null);

        #endregion

        #region OnNewChargingSession

        /// <summary>
        /// An event fired whenever a new charging session was created.
        /// </summary>
        public event OnNewChargingSessionDelegate OnNewChargingSession;


        internal void SendNewChargingSession(DateTime         Timestamp,
                                             Object           Sender,
                                             ChargingSession  ChargingSession)
        {

            OnNewChargingSession?.Invoke(Timestamp, Sender, ChargingSession);

        }

        #endregion


        #region RemoteStop(...                   SessionId, ReservationHandling = null, ProviderId = null, eMAId = null, ...)

        /// <summary>
        /// Stop the given charging session.
        /// </summary>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ReservationHandling">Whether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStopResult>

            RemoteStop(ChargingSession_Id     SessionId,
                       ReservationHandling?   ReservationHandling   = null,
                       eMobilityProvider_Id?  ProviderId            = null,
                       eMobilityAccount_Id?   eMAId                 = null,

                       DateTime?              Timestamp             = null,
                       CancellationToken?     CancellationToken     = null,
                       EventTracking_Id       EventTrackingId       = null,
                       TimeSpan?              RequestTimeout        = null)

        {

            #region Initial checks

            if (!Timestamp.HasValue)
                Timestamp = DateTime.Now;

            if (!CancellationToken.HasValue)
                CancellationToken = new CancellationTokenSource().Token;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;

            #endregion


            if (AdminStatus.Value == ChargingStationAdminStatusTypes.Operational ||
                AdminStatus.Value == ChargingStationAdminStatusTypes.InternalUse)
            {

                #region Check if the eMAId is on the white list

                if (_UseWhiteLists &&
                   !_WhiteLists["default"].Contains(AuthIdentification.FromRemoteIdentification(eMAId.Value)))
                    return RemoteStopResult.InvalidCredentials(SessionId);

                #endregion

                var _VirtualChargingStation = _EVSEs.FirstOrDefault(evse => evse.ChargingSession    != null &&
                                                                 evse.ChargingSession.Id == SessionId);

                if (_VirtualChargingStation == null)
                    return RemoteStopResult.InvalidSessionId(SessionId);


                var result = await _VirtualChargingStation.RemoteStop(SessionId,
                                                           ReservationHandling,
                                                           ProviderId,
                                                           eMAId,

                                                           Timestamp,
                                                           CancellationToken,
                                                           EventTrackingId,
                                                           RequestTimeout);

                switch (result.Result)
                {

                    case RemoteStopEVSEResultType.Error:
                        return RemoteStopResult.Error(SessionId, result.Message);

                    case RemoteStopEVSEResultType.InternalUse:
                        return RemoteStopResult.InternalUse(SessionId);

                    case RemoteStopEVSEResultType.InvalidSessionId:
                        return RemoteStopResult.InvalidSessionId(SessionId);

                    case RemoteStopEVSEResultType.Offline:
                        return RemoteStopResult.Offline(SessionId);

                    case RemoteStopEVSEResultType.OutOfService:
                        return RemoteStopResult.OutOfService(SessionId);

                    case RemoteStopEVSEResultType.Success:
                        if (result.ChargeDetailRecord != null)
                            return RemoteStopResult.Success(result.ChargeDetailRecord, result.ReservationId, result.ReservationHandling);
                        else
                            return RemoteStopResult.Success(result.SessionId, result.ReservationId, result.ReservationHandling);

                    case RemoteStopEVSEResultType.Timeout:
                        return RemoteStopResult.Timeout(SessionId);

                    case RemoteStopEVSEResultType.UnknownOperator:
                        return RemoteStopResult.UnknownOperator(SessionId);

                    case RemoteStopEVSEResultType.Unspecified:
                        return RemoteStopResult.Unspecified(SessionId);

                }

                return RemoteStopResult.Error(SessionId);

            }
            else
            {

                switch (AdminStatus.Value)
                {

                    default:
                        return RemoteStopResult.OutOfService(SessionId);

                }

            }

        }

        #endregion

        #region RemoteStop(...EVSEId,            SessionId, ReservationHandling = null, ProviderId = null, eMAId = null, ...)

        /// <summary>
        /// Stop the given charging session at the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The unique identification of the EVSE to be stopped.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ReservationHandling">Whether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStopEVSEResult>

            RemoteStop(EVSE_Id                EVSEId,
                       ChargingSession_Id     SessionId,
                       ReservationHandling?   ReservationHandling   = null,
                       eMobilityProvider_Id?  ProviderId            = null,
                       eMobilityAccount_Id?   eMAId                 = null,

                       DateTime?              Timestamp             = null,
                       CancellationToken?     CancellationToken     = null,
                       EventTracking_Id       EventTrackingId       = null,
                       TimeSpan?              RequestTimeout        = null)

        {

            #region Initial checks

            if (!Timestamp.HasValue)
                Timestamp = DateTime.Now;

            if (!CancellationToken.HasValue)
                CancellationToken = new CancellationTokenSource().Token;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;


            RemoteStopEVSEResult result = null;

            #endregion


            if (AdminStatus.Value == ChargingStationAdminStatusTypes.Operational ||
                AdminStatus.Value == ChargingStationAdminStatusTypes.InternalUse)
            {

                #region Check if the eMAId is on the white list

                if (_UseWhiteLists &&
                   !_WhiteLists["default"].Contains(AuthIdentification.FromRemoteIdentification(eMAId.Value)))
                    return RemoteStopEVSEResult.InvalidCredentials(SessionId);

                #endregion

                var _VirtualChargingStation = GetEVSEbyId(EVSEId);

                if (_VirtualChargingStation != null)
                {

                    result = await _VirtualChargingStation.RemoteStop(SessionId,
                                                           ReservationHandling,
                                                           ProviderId,
                                                           eMAId,

                                                           Timestamp,
                                                           CancellationToken,
                                                           EventTrackingId,
                                                           RequestTimeout);

                }

                else
                    result = RemoteStopEVSEResult.UnknownEVSE(SessionId);


                return result;

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

        #region RemoteStop(...ChargingStationId, SessionId, ReservationHandling = null, ProviderId = null, eMAId = null, ...)

        /// <summary>
        /// Stop the given charging session at the given charging station.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of the charging station to be stopped.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="ReservationHandling">Whether to remove the reservation after session end, or to keep it open for some more time.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public async Task<RemoteStopChargingStationResult>

            RemoteStop(ChargingStation_Id     ChargingStationId,
                       ChargingSession_Id     SessionId,
                       ReservationHandling?   ReservationHandling   = null,
                       eMobilityProvider_Id?  ProviderId            = null,
                       eMobilityAccount_Id?   eMAId                 = null,

                       DateTime?              Timestamp             = null,
                       CancellationToken?     CancellationToken     = null,
                       EventTracking_Id       EventTrackingId       = null,
                       TimeSpan?              RequestTimeout        = null)

        {

            #region Initial checks

            if (!Timestamp.HasValue)
                Timestamp = DateTime.Now;

            if (!CancellationToken.HasValue)
                CancellationToken = new CancellationTokenSource().Token;

            if (EventTrackingId == null)
                EventTrackingId = EventTracking_Id.New;

            #endregion


            if (AdminStatus.Value == ChargingStationAdminStatusTypes.Operational ||
                AdminStatus.Value == ChargingStationAdminStatusTypes.InternalUse)
            {

                #region Check if the eMAId is on the white list

                if (_UseWhiteLists &&
                   !_WhiteLists["default"].Contains(AuthIdentification.FromRemoteIdentification(eMAId.Value)))
                    return RemoteStopChargingStationResult.InvalidCredentials(SessionId);

                #endregion

                var _VirtualChargingStation = _EVSEs.FirstOrDefault(evse => evse.ChargingSession    != null &&
                                                                 evse.ChargingSession.Id == SessionId);

                if (_VirtualChargingStation == null)
                    return RemoteStopChargingStationResult.InvalidSessionId(SessionId);


                var result = await _VirtualChargingStation.RemoteStop(SessionId,
                                                           ReservationHandling,
                                                           ProviderId,
                                                           eMAId,

                                                           Timestamp,
                                                           CancellationToken,
                                                           EventTrackingId,
                                                           RequestTimeout);

                switch (result.Result)
                {

                    case RemoteStopEVSEResultType.Error:
                        return RemoteStopChargingStationResult.Error(SessionId, result.Message);

                    case RemoteStopEVSEResultType.InternalUse:
                        return RemoteStopChargingStationResult.InternalUse(SessionId);

                    case RemoteStopEVSEResultType.InvalidSessionId:
                        return RemoteStopChargingStationResult.InvalidSessionId(SessionId);

                    case RemoteStopEVSEResultType.Offline:
                        return RemoteStopChargingStationResult.Offline(SessionId);

                    case RemoteStopEVSEResultType.OutOfService:
                        return RemoteStopChargingStationResult.OutOfService(SessionId);

                    case RemoteStopEVSEResultType.Success:
                        if (result.ChargeDetailRecord != null)
                            return RemoteStopChargingStationResult.Success(result.ChargeDetailRecord, result.ReservationId, result.ReservationHandling);
                        else
                            return RemoteStopChargingStationResult.Success(result.SessionId, result.ReservationId, result.ReservationHandling);

                    case RemoteStopEVSEResultType.Timeout:
                        return RemoteStopChargingStationResult.Timeout(SessionId);

                    case RemoteStopEVSEResultType.UnknownOperator:
                        return RemoteStopChargingStationResult.UnknownOperator(SessionId);

                    case RemoteStopEVSEResultType.Unspecified:
                        return RemoteStopChargingStationResult.Unspecified(SessionId);

                }

                return RemoteStopChargingStationResult.Error(SessionId);

            }
            else
            {

                switch (AdminStatus.Value)
                {

                    default:
                        return RemoteStopChargingStationResult.OutOfService(SessionId);

                }

            }

        }

        #endregion

        #region OnNewChargeDetailRecord

        /// <summary>
        /// An event fired whenever a new charge detail record was created.
        /// </summary>
        public event OnNewChargeDetailRecordDelegate OnNewChargeDetailRecord;


        internal void SendNewChargeDetailRecord(DateTime            Timestamp,
                                                Object              Sender,
                                                ChargeDetailRecord  ChargeDetailRecord)
        {

            OnNewChargeDetailRecord?.Invoke(Timestamp, Sender, ChargeDetailRecord);

        }

        #endregion

        #endregion

        #region WhiteLists

        #region GetWhiteList(Name)

        public HashSet<AuthIdentification> GetWhiteList(String Name)

            => _WhiteLists[Name];

        #endregion

        #endregion


        //-- Client-side methods -----------------------------------------

        #region AuthenticateToken(AuthToken)

        public Boolean AuthenticateToken(Auth_Token AuthToken)
        {
            return false;
        }

        #endregion



        #region IComparable<VirtualChargingStation> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object Object)
        {

            if (Object == null)
                throw new ArgumentNullException(nameof(Object), "The given object must not be null!");

            var VirtualChargingStation = Object as VirtualChargingStation;
            if ((Object) VirtualChargingStation == null)
                throw new ArgumentException("The given object is not a virtual charging station!");

            return CompareTo(VirtualChargingStation);

        }

        #endregion

        #region CompareTo(VirtualChargingStation)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="VirtualChargingStation">An virtual charging station to compare with.</param>
        public Int32 CompareTo(VirtualChargingStation VirtualChargingStation)
        {

            if ((Object) VirtualChargingStation == null)
                throw new ArgumentNullException(nameof(VirtualChargingStation),  "The given virtual charging station must not be null!");

            return Id.CompareTo(VirtualChargingStation.Id);

        }

        #endregion

        #endregion

        #region IEquatable<VirtualChargingStation> Members

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

            var VirtualChargingStation = Object as VirtualChargingStation;
            if ((Object) VirtualChargingStation == null)
                return false;

            return Equals(VirtualChargingStation);

        }

        #endregion

        #region Equals(VirtualChargingStation)

        /// <summary>
        /// Compares two virtual charging stations for equality.
        /// </summary>
        /// <param name="VirtualChargingStation">A virtual charging station to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(VirtualChargingStation VirtualChargingStation)
        {

            if ((Object) VirtualChargingStation == null)
                return false;

            return Id.Equals(VirtualChargingStation.Id);

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
