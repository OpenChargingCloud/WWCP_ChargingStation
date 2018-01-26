﻿/*
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
using System.Net.Security;
using System.Threading.Tasks;
using System.Collections.Generic;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using System.Security.Cryptography.X509Certificates;

#endregion

namespace org.GraphDefined.WWCP.ChargingStations
{

    /// <summary>
    /// A remote charging station attached via a computer network (TCP/IP).
    /// </summary>
    public class NetworkChargingStationStub : INetworkChargingStation
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

        public static readonly TimeSpan  DefaultRequestTimeout  = TimeSpan.FromSeconds(180);


        public static readonly IPPort DefaultTCPPort = IPPort.Parse(2348);

        /// <summary>
        /// The default time span between self checks.
        /// </summary>
        public static readonly TimeSpan DefaultSelfCheckTimeSpan = TimeSpan.FromSeconds(5);

        private Timer _SelfCheckTimer;

        #endregion

        #region Properties

        #region Id

        private ChargingStation_Id _Id;

        /// <summary>
        /// The unique identification of this network charging station.
        /// </summary>
        public ChargingStation_Id Id
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


        #region IPTransport

        private readonly IPTransport _IPTransport;

        public IPTransport IPTransport
        {
            get
            {
                return _IPTransport;
            }
        }

        #endregion

        #region DNSClient

        private readonly DNSClient _DNSClient;

        public DNSClient DNSClient
        {
            get
            {
                return _DNSClient;
            }
        }

        #endregion

        #region Hostname

        private readonly String _Hostname;

        public String Hostname
        {
            get
            {
                return _Hostname;
            }
        }

        #endregion

        #region TCPPort

        private readonly IPPort _TCPPort;

        public IPPort TCPPort
        {
            get
            {
                return _TCPPort;
            }
        }

        #endregion

        #region Service

        private readonly String _Service;

        public String Service
        {
            get
            {
                return _Service;
            }
        }

        #endregion

        /// <summary>
        /// A delegate to verify the remote TLS certificate.
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidator { get; }

        public LocalCertificateSelectionCallback LocalCertificateSelector { get; }

        public X509Certificate ClientCert { get; }

        #region VirtualHost

        private readonly String _VirtualHost;

        public String VirtualHost
        {
            get
            {
                return _VirtualHost;
            }
        }

        #endregion

        #region URIPrefix

        private readonly String _URIPrefix;

        public String URIPrefix
        {
            get
            {
                return _URIPrefix;
            }
        }

        #endregion

        #region RequestTimeout

        private readonly TimeSpan _RequestTimeout;

        public TimeSpan RequestTimeout
        {
            get
            {
                return _RequestTimeout;
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


        public ChargingStation_Id RemoteChargingStationId { get; set; }
        public String             RemoteEVSEIdPrefix      { get; set; }


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


        #region EVSEIdMapping

        private readonly Dictionary<EVSE_Id, EVSE_Id> MapOutgoing;
        private readonly Dictionary<EVSE_Id, EVSE_Id> MapIncoming;

        #endregion

        #endregion

        #region Links

        #region ChargingStation

        protected readonly ChargingStation _ChargingStation;

        public ChargingStation ChargingStation
        {
            get
            {
                return _ChargingStation;
            }
        }

        #endregion

        #region EVSEs

        protected readonly HashSet<IRemoteEVSE> _EVSEs;

        /// <summary>
        /// All registered EVSEs.
        /// </summary>
        public IEnumerable<IRemoteEVSE> EVSEs
        {
            get
            {
                return _EVSEs;
            }
        }

        #endregion

        #endregion

        #region Events

        #region OnNewReservation

        /// <summary>
        /// An event fired whenever a new charging reservation was created.
        /// </summary>
        public event OnNewReservationDelegate OnNewReservation;

        #endregion

        #region OnNewChargingSession/-ChargeDetailRecord

        /// <summary>
        /// An event fired whenever a new charging session was created.
        /// </summary>
        public event OnNewChargingSessionDelegate OnNewChargingSession;

        /// <summary>
        /// An event fired whenever a new charge detail record was created.
        /// </summary>
        public event OnNewChargeDetailRecordDelegate OnNewChargeDetailRecord;

        #endregion

        public event OnCancelReservationResponseDelegate OnReservationCancelled;


        // EVSE events

        #region EVSEAddition

        internal readonly IVotingNotificator<DateTime, IRemoteChargingStation, IRemoteEVSE, Boolean> EVSEAddition;

        /// <summary>
        /// Called whenever an EVSE will be or was added.
        /// </summary>
        public IVotingSender<DateTime, IRemoteChargingStation, IRemoteEVSE, Boolean> OnEVSEAddition
        {
            get
            {
                return EVSEAddition;
            }
        }

        #endregion

        #region OnEVSEData/(Admin)StatusChanged

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

        #region OnReserveEVSE / OnReservedEVSE

        /// <summary>
        /// An event fired whenever a reserve EVSE command was received.
        /// </summary>
        public event OnReserveEVSERequestDelegate   OnReserveEVSE;

        /// <summary>
        /// An event fired whenever a reserve EVSE command completed.
        /// </summary>
        public event OnReserveEVSEResponseDelegate  OnEVSEReserved;

        #endregion

        #region OnRemoteEVSEStart / OnRemoteEVSEStarted

        /// <summary>
        /// An event fired whenever a remote start EVSE command was received.
        /// </summary>
        public event OnRemoteStartEVSERequestDelegate   OnRemoteEVSEStart;

        /// <summary>
        /// An event fired whenever a remote start EVSE command completed.
        /// </summary>
        public event OnRemoteStartEVSEResponseDelegate  OnRemoteEVSEStarted;

        #endregion

        #region OnRemoteEVSEStop / OnRemoteEVSEStopped

        /// <summary>
        /// An event fired whenever a remote stop EVSE command was received.
        /// </summary>
        public event OnRemoteStopEVSERequestDelegate   OnRemoteEVSEStop;

        /// <summary>
        /// An event fired whenever a remote stop EVSE command completed.
        /// </summary>
        public event OnRemoteStopEVSEResponseDelegate  OnRemoteEVSEStopped;

        #endregion


        // Socket events

        #region SocketOutletAddition

        internal readonly IVotingNotificator<DateTime, VirtualEVSE, SocketOutlet, Boolean> SocketOutletAddition;

        /// <summary>
        /// Called whenever a socket outlet will be or was added.
        /// </summary>
        public IVotingSender<DateTime, VirtualEVSE, SocketOutlet, Boolean> OnSocketOutletAddition
        {
            get
            {
                return SocketOutletAddition;
            }
        }

        #endregion

        #region SocketOutletRemoval

        internal readonly IVotingNotificator<DateTime, VirtualEVSE, SocketOutlet, Boolean> SocketOutletRemoval;

        /// <summary>
        /// Called whenever a socket outlet will be or was removed.
        /// </summary>
        public IVotingSender<DateTime, VirtualEVSE, SocketOutlet, Boolean> OnSocketOutletRemoval
        {
            get
            {
                return SocketOutletRemoval;
            }
        }

        #endregion


        #region OnData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data of the charging station changed.
        /// </summary>
        public event OnRemoteChargingStationDataChangedDelegate         OnChargingStationDataChanged;

        /// <summary>
        /// An event fired whenever the dynamic status of the charging station changed.
        /// </summary>
        public event OnRemoteChargingStationStatusChangedDelegate       OnStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status of the charging station changed.
        /// </summary>
        public event OnRemoteChargingStationAdminStatusChangedDelegate  OnAdminStatusChanged;
        public event OnRemoteChargingStationDataChangedDelegate OnDataChanged;

        #endregion

        #endregion

        #region Constructor(s)

        #region NetworkChargingStationStub(ChargingStation)

        /// <summary>
        /// A charging station.
        /// </summary>
        /// <param name="ChargingStation">A local charging station.</param>
        public NetworkChargingStationStub(ChargingStation  ChargingStation,
                                          UInt16           MaxStatusListSize       = DefaultMaxStatusListSize,
                                          UInt16           MaxAdminStatusListSize  = DefaultMaxAdminStatusListSize)
        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException("ChargingStation", "The given charging station parameter must not be null!");

            #endregion

            this._Id                    = ChargingStation.Id;
            this._ChargingStation       = ChargingStation;
            this._EVSEs                 = new HashSet<IRemoteEVSE>();

            this._StatusSchedule        = new StatusSchedule<ChargingStationStatusTypes>(MaxStatusListSize);
            this._StatusSchedule.Insert(ChargingStationStatusTypes.OutOfService);

            this._AdminStatusSchedule   = new StatusSchedule<ChargingStationAdminStatusTypes>(MaxStatusListSize);
            this._AdminStatusSchedule.Insert(ChargingStationAdminStatusTypes.OutOfService);


            #region Init events

            // ChargingStation events
            this.EVSEAddition               = new VotingNotificator<DateTime, IRemoteChargingStation, IRemoteEVSE, Boolean>(() => new VetoVote(), true);
          //  this.EVSERemoval                = new VotingNotificator<DateTime, ChargingStation, EVSE, Boolean>(() => new VetoVote(), true);

          //  // EVSE events
          //  this.SocketOutletAddition       = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);
          //  this.SocketOutletRemoval        = new VotingNotificator<DateTime, EVSE, SocketOutlet, Boolean>(() => new VetoVote(), true);

            #endregion

        }

        #endregion

        #region NetworkChargingStationStub(Id, IPTransport = IPv4only, DNSClient = null, Hostname = DefaultHostname, TCPPort = null, ...)

        /// <summary>
        /// A virtual WWCP charging station.
        /// </summary>
        /// <param name="ChargingStation">A local charging station.</param>
        /// <param name="DNSClient">An optional DNS client used to resolve DNS names.</param>
        public NetworkChargingStationStub(ChargingStation                      ChargingStation,
                                          TimeSpan?                            SelfCheckTimeSpan            = null,
                                          UInt16                               MaxStatusListSize            = DefaultMaxStatusListSize,
                                          UInt16                               MaxAdminStatusListSize       = DefaultMaxAdminStatusListSize,
                                          IPTransport                          IPTransport                  = IPTransport.IPv4only,
                                          DNSClient                            DNSClient                    = null,
                                          String                               Hostname                     = null,
                                          IPPort?                              TCPPort                      = null,
                                          String                               Service                      = null,
                                          RemoteCertificateValidationCallback  RemoteCertificateValidator   = null,
                                          LocalCertificateSelectionCallback    LocalCertificateSelector     = null,
                                          X509Certificate                      ClientCert                   = null,
                                          String                               VirtualHost                  = null,
                                          String                               URIPrefix                    = null,
                                          TimeSpan?                            RequestTimeout               = null)

            : this(ChargingStation, MaxStatusListSize, MaxAdminStatusListSize)

        {

            this._IPTransport                 = IPTransport;
            this._DNSClient                   = DNSClient ?? new DNSClient(SearchForIPv4DNSServers: true,
                                                                           SearchForIPv6DNSServers: false);
            this._Hostname                    = Hostname;
            this._TCPPort                     = TCPPort ?? DefaultTCPPort;
            this._Service                     = Service;
            this.RemoteCertificateValidator   = RemoteCertificateValidator;
            this.LocalCertificateSelector     = LocalCertificateSelector;
            this.ClientCert                   = ClientCert;
            this._VirtualHost                 = VirtualHost.IsNotNullOrEmpty() ? VirtualHost        : Hostname;
            this._URIPrefix                   = URIPrefix;
            this._RequestTimeout                = RequestTimeout.HasValue          ? RequestTimeout.Value : DefaultRequestTimeout;

            this._SelfCheckTimeSpan           = SelfCheckTimeSpan != null && SelfCheckTimeSpan.HasValue ? SelfCheckTimeSpan.Value : DefaultSelfCheckTimeSpan;
            this._SelfCheckTimer              = new Timer(SelfCheck, null, _SelfCheckTimeSpan, _SelfCheckTimeSpan);

            this.MapOutgoing                  = new Dictionary<EVSE_Id, EVSE_Id>();
            this.MapIncoming                  = new Dictionary<EVSE_Id, EVSE_Id>();

        }

        #endregion

        #endregion


        #region (Admin-)Status management

        #region SetStatus(NewStatus)

        /// <summary>
        /// Set the current status.
        /// </summary>
        /// <param name="NewStatus">A new timestamped status.</param>
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


        #region (internal) UpdateAdminStatus(Timestamp, EventTrackingId, OldStatus, NewStatus)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="OldStatus">The old EVSE admin status.</param>
        /// <param name="NewStatus">The new EVSE admin status.</param>
        internal void UpdateAdminStatus(DateTime                                      Timestamp,
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

        #region (internal) UpdateStatus(Timestamp, EventTrackingId, OldStatus, NewStatus)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal void UpdateStatus(DateTime                                 Timestamp,
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

            //foreach (var _EVSE in _EVSEs)
            //    _EVSE.CheckReservationTime().Wait();

        }

        #endregion


        public void AddMapping(EVSE_Id LocalEVSEId,
                               EVSE_Id RemoteEVSEId)
        {

            MapOutgoing.Add(LocalEVSEId,  RemoteEVSEId);
            MapIncoming.Add(RemoteEVSEId, LocalEVSEId);

        }

        public EVSE_Id MapOutgoingId(EVSE_Id EVSEIdOut)
        {

            EVSE_Id EVSEIdIn;

            if (MapOutgoing.TryGetValue(EVSEIdOut, out EVSEIdIn))
                return EVSEIdIn;

            return EVSEIdOut;

        }

        public EVSE_Id MapIncomingId(EVSE_Id EVSEIdIn)
        {

            EVSE_Id EVSEIdOut;

            if (MapIncoming.TryGetValue(EVSEIdIn, out EVSEIdOut))
                return EVSEIdOut;

            return EVSEIdIn;

        }


        IEnumerable<IRemoteEVSE> IRemoteChargingStation.EVSEs
            => null;

        #region CreateNewEVSE(EVSEId, Configurator = null, OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new EVSE having the given
        /// unique EVSE identification.
        /// </summary>
        /// <param name="EVSEId">The unique identification of the new EVSE.</param>
        /// <param name="Configurator">An optional delegate to configure the new EVSE after its creation.</param>
        /// <param name="OnSuccess">An optional delegate called after successful creation of the EVSE.</param>
        /// <param name="OnError">An optional delegate for signaling errors.</param>
        public NetworkEVSEStub CreateNewEVSE(EVSE_Id                                 EVSEId,
                                        Action<NetworkEVSEStub>                      Configurator  = null,
                                        Action<NetworkEVSEStub>                      OnSuccess     = null,
                                        Action<NetworkChargingStationStub, EVSE_Id>  OnError       = null)
        {

            #region Initial checks

            if (EVSEId == null)
                throw new ArgumentNullException(nameof(EVSEId), "The given EVSE identification must not be null!");

            if (_EVSEs.Any(evse => evse.Id == EVSEId))
            {
                if (OnError == null)
                    throw new EVSEAlreadyExistsInStation(this.ChargingStation, EVSEId);
                else
                    OnError?.Invoke(this, EVSEId);
            }

            #endregion

            var Now           = DateTime.Now;
            var _NetworkEVSE  = new NetworkEVSEStub(EVSEId, this);

            Configurator?.Invoke(_NetworkEVSE);

            if (EVSEAddition.SendVoting(Now, this, _NetworkEVSE))
            {
                if (_EVSEs.Add(_NetworkEVSE))
                {

               //     _EVSE.OnPropertyChanged     += (Timestamp, Sender, PropertyName, OldValue, NewValue)
               //                                     => UpdateEVSEData       (Timestamp, Sender as EVSE, PropertyName, OldValue, NewValue);
               //
               //     _EVSE.OnStatusChanged       += (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus)
               //                                     => UpdateEVSEStatus     (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus);
               //
               //     _EVSE.OnAdminStatusChanged  += (Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus)
               //                                     => UpdateEVSEAdminStatus(Timestamp, EVSE, OldEVSEStatus, NewEVSEStatus);

                    OnSuccess?.Invoke(_NetworkEVSE);
                    EVSEAddition.SendNotification(Now, this, _NetworkEVSE);
               //     UpdateEVSEStatus(Now, _EVSE, new Timestamped<EVSEStatusType>(Now, EVSEStatusType.Unspecified), _EVSE.Status);

                    return _NetworkEVSE;

                }
            }

            //Debug.WriteLine("EVSE '" + EVSEId + "' was not created!");
            return null;

        }

        #endregion

        IRemoteEVSE IRemoteChargingStation.CreateNewEVSE(EVSE_Id EVSEId, Action<EVSE> Configurator = null, Action<EVSE> OnSuccess = null, Action<ChargingStation, EVSE_Id> OnError = null)
            => this.CreateNewEVSE(EVSEId);

        public IRemoteEVSE AddEVSE(IRemoteEVSE                       EVSE,
                                   Action<EVSE>                      Configurator  = null,
                                   Action<EVSE>                      OnSuccess     = null,
                                   Action<ChargingStation, EVSE_Id>  OnError       = null)

        {

            _EVSEs.Add(EVSE);

            return EVSE;

        }


        public virtual async Task<IEnumerable<EVSEStatus>> GetEVSEStatus(DateTime           Timestamp,
                                                                         CancellationToken  CancellationToken,
                                                                         EventTracking_Id   EventTrackingId,
                                                                         TimeSpan?          RequestTimeout = null)

            => new EVSEStatus[] {
                   new EVSEStatus(EVSE_Id.Parse("DE*822*E222*1"), new Timestamped<EVSEStatusTypes>(EVSEStatusTypes.Charging))
               };

        #region Reservations

        public IEnumerable<ChargingReservation> ChargingReservations
            => new ChargingReservation[0];


        #region Reserve(...StartTime, Duration, ReservationId = null, ProviderId = null, ...)

        /// <summary>
        /// Reserve the possibility to charge at the given charging station.
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
        public virtual async Task<ReservationResult>

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

            => ReservationResult.OutOfService;

        #endregion

        #region Reserve(...EVSEId, StartTime, Duration, ReservationId = null, ProviderId = null, ...)

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
        public virtual async Task<ReservationResult>

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

            => ReservationResult.OutOfService;

        #endregion


        #region TryGetReservationById(ReservationId, out Reservation)

        public Boolean TryGetReservationById(ChargingReservation_Id ReservationId, out ChargingReservation Reservation)
        {

            Reservation = _EVSEs.Where (evse => evse.Reservation != null &&
                                                evse.Reservation.Id == ReservationId).
                                 Select(evse => evse.Reservation).
                                 FirstOrDefault();

            return Reservation != null;

        }

        #endregion

        protected internal void SendNewReservation(ChargingReservation Reservation)
        {

            OnNewReservation?.Invoke(DateTime.Now,
                                     this,
                                     Reservation);

        }


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
        public virtual async Task<CancelReservationResult>

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

            if (ReservationId == null)
                throw new ArgumentNullException(nameof(ReservationId), "The given charging reservation identification must not be null!");

            #endregion


            return await _EVSEs.Where   (evse => evse.Reservation    != null &&
                                                 evse.Reservation.Id == ReservationId).
                                MapFirst(evse => evse.CancelReservation(ReservationId,
                                                                        Reason,
                                                                        ProviderId,

                                                                        Timestamp,
                                                                        CancellationToken,
                                                                        EventTrackingId,
                                                                        RequestTimeout),
                                         Task.FromResult(CancelReservationResult.Error(ReservationId,
                                                                                       Reason,
                                                                                       "The charging reservation could not be cancelled!")));

        }

        #endregion

        #endregion

        #region RemoteStart/-Stop

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
        public virtual async Task<RemoteStartEVSEResult>

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

            => RemoteStartEVSEResult.OutOfService;

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

            => RemoteStartChargingStationResult.OutOfService;

        #endregion


        protected internal void SendNewChargingSession(ChargingSession ChargingSession)
        {

            OnNewChargingSession?.Invoke(DateTime.Now,
                                         this,
                                         ChargingSession);

        }


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

            => RemoteStopResult.OutOfService(SessionId);

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
        public virtual async Task<RemoteStopEVSEResult>

            RemoteStop(EVSE_Id                EVSEId,
                       ChargingSession_Id     SessionId,
                       ReservationHandling?   ReservationHandling   = null,
                       eMobilityProvider_Id?  ProviderId            = null,
                       eMobilityAccount_Id?   eMAId                 = null,

                       DateTime?              Timestamp             = null,
                       CancellationToken?     CancellationToken     = null,
                       EventTracking_Id       EventTrackingId       = null,
                       TimeSpan?              RequestTimeout        = null)

            => RemoteStopEVSEResult.OutOfService(SessionId);

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

            => RemoteStopChargingStationResult.OutOfService(SessionId);

        #endregion

        #endregion


    }

}
