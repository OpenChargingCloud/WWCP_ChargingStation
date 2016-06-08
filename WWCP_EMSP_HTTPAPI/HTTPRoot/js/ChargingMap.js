var DataURI = "https://e-mobility.belectric-data.com/RNs/Prod";
var __EVSEStatus = {};
var __UserPositon = null;
var worker = null;
function Startup(L) {
    var QueryParameters = { "RN": "Prod" };
    window.location.search.
        substring(1).
        split("&").
        map(function (item, index, array) { return item.split("=").
        map(function (element, index, array) { return decodeURIComponent(element); }); }).
        forEach(function (ding, index, array) { return QueryParameters[ding[0]] = ding[1]; });
    if (QueryParameters.hasOwnProperty("RN")) {
        switch (QueryParameters.RN) {
            case "QA":
                DataURI = "https://e-mobility.belectric-data.com/RNs/QA";
                break;
            case "Prod":
                DataURI = "https://e-mobility.belectric-data.com/RNs/Prod";
                break;
            case "QALocal":
                DataURI = "http://127.0.0.1:3004/RNs/QA";
                break;
            case "ProdLocal":
                DataURI = "http://127.0.0.1:3004/RNs/Prod";
                break;
        }
    }
    var map = L.map('map');
    var ACCESS_TOKEN = "pk.eyJ1IjoiYWh6ZiIsImEiOiJOdEQtTkcwIn0.Cn0iGqUYyA6KPS8iVjN68w";
    L.tileLayer('https://{s}.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=' + ACCESS_TOKEN, {
        maxZoom: 18,
        attribution: '<a href="http://openstreetmap.org">OSM</a> contr., ' +
            '<a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, ' +
            'Imagery © <a href="http://mapbox.com">Mapbox</a>',
        id: 'ahzf.nc811hb2'
    }).addTo(map);
    L.control.locate().addTo(map);
    var LeafIcon = L.Icon.extend({
        options: {
            iconSize: [32, 37],
            iconAnchor: [16, 37],
            popupAnchor: [0, -30]
        }
    });
    var EChargingIcon = new LeafIcon({ iconUrl: 'leaflet/images/e-charging-red.png' });
    var markers = L.markerClusterGroup({
        spiderfyOnMaxZoom: true,
        showCoverageOnHover: false,
        zoomToBoundsOnClick: true
    });
    var customCircleMarker = L.Marker.extend({
        options: {
            ChargingPool: '',
            CurrentStatus: ''
        }
    });
    map.on("click", function (e) {
        var InfoText = document.getElementById('InfoText');
        InfoText.style.display = "none";
    });
    var StreamFilterPattern = document.getElementById('searchslotinput');
    StreamFilterPattern.onchange = function () {
    };
    var WWCPClient = new WWCP.Client(DataURI, function (ChargingPools, Run) { return DrawMap(ChargingPools, Run, StreamFilterPattern.value, map, markers, customCircleMarker, EChargingIcon); }, 10000, function (EVSEStatus, Run) { return DrawMap2(EVSEStatus, Run, StreamFilterPattern.value, map, markers, customCircleMarker, EChargingIcon); }, 2000);
    worker = new Worker('HTTPWorker/HTTPWorker.js');
    worker.addEventListener('message', function (e) { return alert('HTTPWorker said: ' + e.data); }, false);
    if (navigator.geolocation)
        navigator.geolocation.getCurrentPosition(showPosition);
}
function showPosition(position) {
    __UserPositon = new WWCP.GeoCoordinate(position.coords.latitude, position.coords.longitude);
}
function DrawMap(ChargingPools, Run, filterpattern, map, markers, customCircleMarker, EChargingIcon) {
    var count = 0;
    markers.clearLayers();
    for (var i = 0; i < ChargingPools.length; i++) {
        if (ChargingPools[i].ChargingPoolId.indexOf(filterpattern) > -1) {
            var Marker = new customCircleMarker([ChargingPools[i].GeoLocation.lat,
                ChargingPools[i].GeoLocation.lng], {
                icon: EChargingIcon,
                ChargingPool: ChargingPools[i]
            });
            Marker.on('click', function (m) { OpenInfoText(m); });
            markers.addLayer(Marker);
            count++;
        }
    }
    map.addLayer(markers);
    if (Run === 1 && count > 0) {
        map.fitBounds(markers.getBounds());
    }
}
function DrawMap2(EVSEStatus, Run, filterpattern, map, markers, customCircleMarker, EChargingIcon) {
    var CurrentEVSEStatus = {};
    for (var evseid in EVSEStatus) {
        var status = WWCP.EVSEStatusTypes.unknown;
        var timestamped = EVSEStatus[evseid];
        for (var timestamp in timestamped) {
            status = timestamped[timestamp];
            break;
        }
        CurrentEVSEStatus[evseid] = status;
    }
    __EVSEStatus = CurrentEVSEStatus;
}
function Reserve(Button, EVSEId) {
    var SVGDoc = Button.parentElement.parentElement.getElementsByClassName('SVGSockets')[0].contentDocument.childNodes[0];
    SVGDoc.style.fill = 'red';
    Button.blur();
}
function RemoteStart(Button, EVSEId) {
    worker.postMessage({
        'cmd': 'RemoteStart',
        'EVSEId': EVSEId
    });
    Button.blur();
}
function sss(EVSEId) {
    if (__EVSEStatus[EVSEId] !== "Available") {
        var ss = document.getElementById('SVG_' + EVSEId.replace(/\*/g, ''));
        ss.contentDocument.childNodes[0].style.fill = 'rgba(167, 167, 167, 0.7)';
    }
}
function OpenInfoText(m) {
    var InfoTextDiv = document.getElementById('InfoText');
    var pool = m.target.options.ChargingPool;
    var distance = __UserPositon.DistanceTo(pool.GeoLocation, 1);
    InfoTextDiv.style.display = "block";
    InfoTextDiv.innerHTML = '<div id="TextGroup">' +
        '<div id="ChargingStationName">' + pool.Name.de + "</div>" +
        '<div id="ChargingStationDescription">' + pool.Description.de + ' (' + distance + " km)</div>" +
        '<div><button onclick="Reserve(this, \'' + pool.ChargingPoolId + '\')" class="ReserveButton">Reservieren</button></div>' +
        '</div>' +
        '<div id="ChargingStationGroup">' + pool.ChargingStations.map(function (station, index, array) {
        return '<div id="' + station.ChargingStationId + '" class="ChargingStations">' +
            '<div class="ChargingStationInfo">' + station.Description.de + '</div>' +
            station.EVSEs.map(function (evse, index, array) {
                return '<div id="DIV_' + evse.EVSEId.replace(/\*/g, '') + '" class="EVSEs EVSEStatus' + __EVSEStatus[evse.EVSEId] + '">' +
                    '<div class="PlugInfos">' +
                    '<object onload="sss(\'' + evse.EVSEId + '\')" type="image/svg+xml" data="' + evse.SocketOutlets.
                    map(function (socketOutlet, index, array) { return socketOutlet.PlugImage; }).
                    reduce(function (prev, curr, index, array) { return prev + ' ' + curr; }) +
                    '" id="SVG_' + evse.EVSEId.replace(/\*/g, '') + '" class="SVGSockets"></object>' +
                    '</div>' +
                    '<div class="EVSEInfos">' +
                    'max ' +
                    '<div class="ElectricalInfos">' + evse.MaxPower + ' kW</div>' +
                    '<div class="EVSEStatusInfos">' + __EVSEStatus[evse.EVSEId] + '</div>' +
                    '</div>' +
                    '<div class="EVSEButtons">' +
                    '<button onclick="Reserve(this, \'' + evse.EVSEId + '\')" class="ReserveButton" ' + (__EVSEStatus[evse.EVSEId] !== "Available" ? 'disabled=true ' : '') + '>Reservieren</button>' +
                    '<button onclick="RemoteStart(this, \'' + evse.EVSEId + '\')" class="RemoteStartButton" ' + (__EVSEStatus[evse.EVSEId] !== "Available" ? 'disabled=true ' : '') + '>Laden</button>' +
                    '</div>' +
                    '</div>';
            }).
                reduce(function (prev, curr, index, array) { return prev + curr; }) + '</div>';
    }).
        reduce(function (prev, curr, index, array) { return prev + curr; }) +
        '</div>';
    setTimeout(function () {
    }, 3000);
}
function Download(URI, OnSuccess, OnError) {
    var ajax = new XMLHttpRequest();
    ajax.open("GET", URI, true);
    ajax.setRequestHeader("Accept", "application/json; charset=UTF-8");
    ajax.onreadystatechange = function () {
        if (this.readyState === 4) {
            if (this.status === 200) {
                if (OnSuccess && typeof OnSuccess === 'function')
                    OnSuccess(ajax.responseText);
            }
            else if (OnError && typeof OnError === 'function')
                OnError(this.status, this.statusText);
        }
    };
    ajax.send();
}
function DownloadBlob(URI, OnSuccess, OnError) {
    var _this = this;
    var ajax = new XMLHttpRequest();
    ajax.open("GET", URI, true);
    ajax.setRequestHeader("Accept", "application/json; charset=UTF-8");
    ajax.responseType = 'blob';
    ajax.onload = function (e) {
        if (_this.status == 200) {
            var blob = new Blob([_this.response], { type: 'image/png' });
        }
    };
    ajax.send();
}
function sendForm(form) {
    var formData = new FormData(form);
    formData.append('secret_token', '1234567890');
    var xhr = new XMLHttpRequest();
    xhr.open('POST', form.action, true);
    xhr.onload = function (e) {
    };
    xhr.send(formData);
    return false;
}
function uploadFiles(url, files) {
    var formData = new FormData();
    for (var i = 0, file; file = files[i]; ++i) {
        formData.append(file.name, file);
    }
    var xhr = new XMLHttpRequest();
    xhr.open('POST', url, true);
    xhr.onload = function (e) { };
    xhr.send(formData);
}
function upload(blobOrFile) {
    var xhr = new XMLHttpRequest();
    xhr.open('POST', '/server', true);
    xhr.onload = function (e) { };
    var progressBar = document.querySelector('progress');
    xhr.upload.onprogress = function (e) {
        if (e.lengthComputable) {
            progressBar.value = (e.loaded / e.total) * 100;
        }
    };
    xhr.send(blobOrFile);
}
var WWCP;
(function (WWCP) {
    var Client = (function () {
        function Client(URI, OnNewData, DataIntervall, OnNewStatus, StatusIntervall) {
            if (URI !== undefined) {
                this._URI = URI;
            }
            this._DataIntervall = DataIntervall;
            this._OnNewData = OnNewData;
            this._DataRun = 1;
            this._StatusIntervall = StatusIntervall;
            this._OnNewStatus = OnNewStatus;
            this._StatusRun = 1;
            this.DownloadChargingPools(this._URI, this._DataRun, this._DataIntervall);
            this.DownloadEVSEStatus(this._URI, this._StatusRun, this._StatusIntervall);
        }
        Object.defineProperty(Client.prototype, "URI", {
            get: function () { return this._URI; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(Client.prototype, "DataRun", {
            get: function () { return this._DataRun; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(Client.prototype, "DataIntervall", {
            get: function () { return this._DataIntervall; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(Client.prototype, "OnNewData", {
            get: function () { return this._OnNewData; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(Client.prototype, "StatusRun", {
            get: function () { return this._StatusRun; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(Client.prototype, "StatusIntervall", {
            get: function () { return this._StatusIntervall; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(Client.prototype, "OnNewStatus", {
            get: function () { return this._OnNewStatus; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(Client.prototype, "OnError", {
            get: function () { return this._OnError; },
            enumerable: true,
            configurable: true
        });
        Client.prototype.DownloadChargingPools = function (URI, Run, Intervall) {
            var _this = this;
            Download(URI + '/ChargingPools', function (NewData) {
                window.localStorage.setItem('ChargingPools', NewData);
                var DataArray = JSON.parse(NewData);
                var CPs = DataArray.map(function (chargingpool) { return new WWCP.ChargingPool(chargingpool); });
                _this._OnNewData(CPs, Run);
            }, this._OnError);
            window.setTimeout(function () { return _this.DownloadChargingPools(_this._URI, Run + 1, Intervall); }, Intervall);
        };
        Client.prototype.DownloadEVSEStatus = function (URI, Run, Intervall) {
            var _this = this;
            Download(URI + '/EVSEStatus', function (NewData) {
                window.localStorage.setItem('EVSEStatus', NewData);
                _this._OnNewStatus(JSON.parse(NewData), Run);
            }, this._OnError);
            window.setTimeout(function () { return _this.DownloadEVSEStatus(_this._URI, Run + 1, Intervall); }, Intervall);
        };
        return Client;
    }());
    WWCP.Client = Client;
})(WWCP || (WWCP = {}));
if (typeof (Number.prototype.toRad) === "undefined") {
    Number.prototype.toRad = function () {
        return this * Math.PI / 180;
    };
}
var WWCP;
(function (WWCP) {
    (function (SocketTypes) {
        SocketTypes[SocketTypes["unknown"] = 0] = "unknown";
        SocketTypes[SocketTypes["TypeFSchuko"] = 1] = "TypeFSchuko";
        SocketTypes[SocketTypes["Type2Outlet"] = 2] = "Type2Outlet";
        SocketTypes[SocketTypes["CHAdeMO"] = 3] = "CHAdeMO";
        SocketTypes[SocketTypes["CCSCombo2Plug_CableAttached"] = 4] = "CCSCombo2Plug_CableAttached";
    })(WWCP.SocketTypes || (WWCP.SocketTypes = {}));
    var SocketTypes = WWCP.SocketTypes;
    (function (EVSEStatusTypes) {
        EVSEStatusTypes[EVSEStatusTypes["unknown"] = 0] = "unknown";
        EVSEStatusTypes[EVSEStatusTypes["available"] = 1] = "available";
        EVSEStatusTypes[EVSEStatusTypes["reserved"] = 2] = "reserved";
        EVSEStatusTypes[EVSEStatusTypes["charging"] = 3] = "charging";
    })(WWCP.EVSEStatusTypes || (WWCP.EVSEStatusTypes = {}));
    var EVSEStatusTypes = WWCP.EVSEStatusTypes;
    var I18NString = (function () {
        function I18NString(JSON) {
            if (JSON !== undefined) {
                this._de = JSON.hasOwnProperty("de") ? JSON.de : "";
                this._en = JSON.hasOwnProperty("en") ? JSON.en : "";
                this._fr = JSON.hasOwnProperty("fr") ? JSON.fr : "";
            }
        }
        Object.defineProperty(I18NString.prototype, "de", {
            get: function () { return this._de; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(I18NString.prototype, "en", {
            get: function () { return this._en; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(I18NString.prototype, "fr", {
            get: function () { return this._fr; },
            enumerable: true,
            configurable: true
        });
        return I18NString;
    }());
    WWCP.I18NString = I18NString;
    var GeoCoordinate = (function () {
        function GeoCoordinate(Latitude, Longitude) {
            this._lat = Latitude;
            this._lng = Longitude;
        }
        Object.defineProperty(GeoCoordinate.prototype, "lat", {
            get: function () { return this._lat; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(GeoCoordinate.prototype, "lng", {
            get: function () { return this._lng; },
            enumerable: true,
            configurable: true
        });
        GeoCoordinate.Parse = function (JSON) {
            if (JSON !== undefined) {
                return new GeoCoordinate(JSON.hasOwnProperty("lat") ? JSON.lat : 0, JSON.hasOwnProperty("lng") ? JSON.lng : 0);
            }
        };
        GeoCoordinate.prototype.DistanceTo = function (Target, Decimals) {
            Decimals = Decimals || 8;
            var earthRadius = 6371;
            var dLat = (Target.lat - this._lat).toRad();
            var dLon = (Target.lng - this._lng).toRad();
            var a = Math.sin(dLat / 2) * Math.sin(dLat / 2) + Math.sin(dLon / 2) * Math.sin(dLon / 2) * Math.cos(this._lat.toRad()) * Math.cos(Target.lat.toRad());
            var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
            var d = earthRadius * c;
            return Math.round(d * Math.pow(10, Decimals)) / Math.pow(10, Decimals);
        };
        return GeoCoordinate;
    }());
    WWCP.GeoCoordinate = GeoCoordinate;
})(WWCP || (WWCP = {}));
var WWCP;
(function (WWCP) {
    var RoamingNetwork = (function () {
        function RoamingNetwork() {
        }
        return RoamingNetwork;
    }());
    WWCP.RoamingNetwork = RoamingNetwork;
    var EVSEOperator = (function () {
        function EVSEOperator() {
        }
        return EVSEOperator;
    }());
    WWCP.EVSEOperator = EVSEOperator;
    var ChargingPool = (function () {
        function ChargingPool(JSON) {
            if (JSON !== undefined) {
                this._ChargingPoolId = JSON.hasOwnProperty("ChargingPoolId") ? JSON.ChargingPoolId : null;
                this._Name = JSON.hasOwnProperty("Name") ? new WWCP.I18NString(JSON.Name) : null;
                this._Description = JSON.hasOwnProperty("Description") ? new WWCP.I18NString(JSON.Description) : null;
                this._GeoLocation = JSON.hasOwnProperty("GeoLocation") ? WWCP.GeoCoordinate.Parse(JSON.GeoLocation) : null;
                this._ChargingStations = (JSON.hasOwnProperty("ChargingStations") &&
                    JSON.ChargingStations instanceof Array) ? JSON.ChargingStations.map(function (station, index, array) {
                    return new ChargingStation(station);
                }) : null;
            }
        }
        Object.defineProperty(ChargingPool.prototype, "ChargingPoolId", {
            get: function () { return this._ChargingPoolId; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(ChargingPool.prototype, "Name", {
            get: function () { return this._Name; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(ChargingPool.prototype, "Description", {
            get: function () { return this._Description; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(ChargingPool.prototype, "GeoLocation", {
            get: function () { return this._GeoLocation; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(ChargingPool.prototype, "ChargingStations", {
            get: function () { return this._ChargingStations; },
            enumerable: true,
            configurable: true
        });
        return ChargingPool;
    }());
    WWCP.ChargingPool = ChargingPool;
    var ChargingStation = (function () {
        function ChargingStation(JSON) {
            if (JSON !== undefined) {
                this._ChargingStationId = JSON.hasOwnProperty("ChargingStationId") ? JSON.ChargingStationId : null;
                this._Name = JSON.hasOwnProperty("Name") ? new WWCP.I18NString(JSON.Name) : null;
                this._Description = JSON.hasOwnProperty("Description") ? new WWCP.I18NString(JSON.Description) : null;
                this._EVSEs = (JSON.hasOwnProperty("EVSEs") &&
                    JSON.EVSEs instanceof Array) ? JSON.EVSEs.map(function (evse, index, array) {
                    return new EVSE(evse);
                }) : null;
            }
        }
        Object.defineProperty(ChargingStation.prototype, "ChargingStationId", {
            get: function () { return this._ChargingStationId; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(ChargingStation.prototype, "Name", {
            get: function () { return this._Name; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(ChargingStation.prototype, "Description", {
            get: function () { return this._Description; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(ChargingStation.prototype, "EVSEs", {
            get: function () { return this._EVSEs; },
            enumerable: true,
            configurable: true
        });
        return ChargingStation;
    }());
    WWCP.ChargingStation = ChargingStation;
    var EVSE = (function () {
        function EVSE(JSON) {
            this._EVSEId = JSON.EVSEId;
            this._Description = new WWCP.I18NString(JSON.Description);
            this._MaxPower = JSON.MaxPower;
            this._SocketOutlets = JSON.SocketOutlets.map(function (socketOutlet, index, array) {
                return new SocketOutlet(socketOutlet);
            });
        }
        Object.defineProperty(EVSE.prototype, "EVSEId", {
            get: function () { return this._EVSEId; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(EVSE.prototype, "Description", {
            get: function () { return this._Description; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(EVSE.prototype, "MaxPower", {
            get: function () { return this._MaxPower; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(EVSE.prototype, "SocketOutlets", {
            get: function () { return this._SocketOutlets; },
            enumerable: true,
            configurable: true
        });
        return EVSE;
    }());
    WWCP.EVSE = EVSE;
    var SocketOutlet = (function () {
        function SocketOutlet(JSON) {
            var prefix = "images/Ladestecker/";
            switch (JSON.Plug) {
                case "TypeFSchuko":
                    this._Plug = WWCP.SocketTypes.TypeFSchuko;
                    this._PlugImage = prefix + "Schuko.svg";
                    break;
                case "Type2Outlet":
                    this._Plug = WWCP.SocketTypes.Type2Outlet;
                    this._PlugImage = prefix + "IEC_Typ_2.svg";
                    break;
                case "Type2Connector_CableAttached":
                    this._Plug = WWCP.SocketTypes.Type2Outlet;
                    this._PlugImage = prefix + "IEC_Typ_2_Cable.svg";
                    break;
                case "CHAdeMO":
                    this._Plug = WWCP.SocketTypes.CHAdeMO;
                    this._PlugImage = prefix + "CHAdeMO.svg";
                    break;
                case "CCSCombo2Plug_CableAttached":
                    this._Plug = WWCP.SocketTypes.CCSCombo2Plug_CableAttached;
                    this._PlugImage = prefix + "CCS_Typ_2.svg";
                    break;
                default:
                    this._Plug = WWCP.SocketTypes.unknown;
                    this._PlugImage = "";
                    break;
            }
        }
        Object.defineProperty(SocketOutlet.prototype, "Plug", {
            get: function () { return this._Plug; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(SocketOutlet.prototype, "PlugImage", {
            get: function () { return this._PlugImage; },
            enumerable: true,
            configurable: true
        });
        return SocketOutlet;
    }());
    WWCP.SocketOutlet = SocketOutlet;
    var EVSEStatusRecord = (function () {
        function EVSEStatusRecord(EVSEId, EVSEStatus) {
            this._EVSEId = EVSEId;
            this._EVSEStatus = EVSEStatus;
        }
        Object.defineProperty(EVSEStatusRecord.prototype, "EVSEId", {
            get: function () { return this._EVSEId; },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(EVSEStatusRecord.prototype, "EVSEStatus", {
            get: function () { return this._EVSEStatus; },
            enumerable: true,
            configurable: true
        });
        EVSEStatusRecord.Parse = function (EVSEId, JSON) {
            var status;
            for (var timestamp in JSON) {
                status = JSON[timestamp];
                break;
            }
            if (JSON !== undefined) {
                return new EVSEStatusRecord(EVSEId, status);
            }
        };
        return EVSEStatusRecord;
    }());
    WWCP.EVSEStatusRecord = EVSEStatusRecord;
})(WWCP || (WWCP = {}));
//# sourceMappingURL=ChargingMap.js.map