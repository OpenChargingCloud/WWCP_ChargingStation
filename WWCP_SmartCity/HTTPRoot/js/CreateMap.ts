/*
 * Copyright (c) 2014-2016, GaphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP TypeScript Client <http://www.github.com/OpenCharingCloud/WWCP_TypedClient>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

var DataURI       = "https://e-mobility.belectric-data.com/RNs/Prod";
var __EVSEStatus  = { };
var __UserPositon: WWCP.GeoCoordinate = null;
var worker : Worker = null;

function Startup(L) {

    let QueryParameters = { "RN": "Prod" };
    window.location.search.
           substring(1).
           split("&").
           map((item,    index, array) => item.split("=").
               map((element, index, array) => decodeURIComponent(element))).
           forEach((ding, index, array) => QueryParameters[ding[0]] = ding[1]);

    if (QueryParameters.hasOwnProperty("RN")) {
        switch (QueryParameters.RN)
        {

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

    const map                 = L.map('map');
    const ACCESS_TOKEN        = "pk.eyJ1IjoiYWh6ZiIsImEiOiJOdEQtTkcwIn0.Cn0iGqUYyA6KPS8iVjN68w";

    L.tileLayer('https://{s}.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=' + ACCESS_TOKEN, {
        maxZoom: 18,
        attribution: '<a href="http://openstreetmap.org">OSM</a> contr., ' +
        '<a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, ' +
        'Imagery © <a href="http://mapbox.com">Mapbox</a>',
        id: 'ahzf.nc811hb2'
    }).addTo(map);

    L.control.locate().addTo(map);

    const LeafIcon            = L.Icon.extend({
                                    options: {
                                        iconSize:    [32,  37],
                                        iconAnchor:  [16,  37],
                                        popupAnchor: [ 0, -30]
                                    }
                                });

    const EChargingIcon       = new LeafIcon({ iconUrl: 'leaflet/images/e-charging-red.png' });

    const markers             = L.markerClusterGroup({
                                    spiderfyOnMaxZoom:    true,
                                    showCoverageOnHover:  false,
                                    zoomToBoundsOnClick:  true
                                });

    const customCircleMarker  = L.Marker.extend({
                                    options: {
                                        ChargingPool:  '',
                                        CurrentStatus: ''
                                    }
                                });

    map.on("click", function (e) {

        const InfoText = document.getElementById('InfoText');

        InfoText.style.display = "none";

    });


    const StreamFilterPattern = <HTMLInputElement> document.getElementById('searchslotinput');

    StreamFilterPattern.onchange = function () {

  //      LoadChargingPools(StreamFilterPattern.value, map, markers, customCircleMarker, EChargingIcon);

    }

    const WWCPClient = new WWCP.Client(DataURI,
                                       (ChargingPools, Run) => DrawMap(ChargingPools, Run, StreamFilterPattern.value, map, markers, customCircleMarker, EChargingIcon),
                                       10000,
                                       (EVSEStatus,    Run) => DrawMap2(EVSEStatus,   Run, StreamFilterPattern.value, map, markers, customCircleMarker, EChargingIcon),
                                       2000);

    worker = new Worker('HTTPWorker/HTTPWorker.js');

    worker.addEventListener('message',
                            e => alert('HTTPWorker said: ' + e.data),
                            false);

    if (navigator.geolocation)
        navigator.geolocation.getCurrentPosition(showPosition);

}


function showPosition(position) {
    __UserPositon = new WWCP.GeoCoordinate(position.coords.latitude,
                                           position.coords.longitude);
}


function DrawMap(ChargingPools:  WWCP.ChargingPool[],
                 Run:            number,
                 filterpattern,
                 map,
                 markers,
                 customCircleMarker,
                 EChargingIcon) {

    let   count      = 0;

    markers.clearLayers();

    //if (Run % 2 == 0)
    //    filterpattern = '55';
    //else
    //    filterpattern = '';

    for (let i = 0; i < ChargingPools.length; i++) {

        if (ChargingPools[i].ChargingPoolId.indexOf(filterpattern) > -1) {

            let Marker = new customCircleMarker([ChargingPools[i].GeoLocation.lat,
                                                 ChargingPools[i].GeoLocation.lng],
                                                {
                                                    icon:          EChargingIcon,
                                                    ChargingPool:  ChargingPools[i]
                                                });

            Marker.on('click', function (m) { OpenInfoText(m) });

            markers.addLayer(Marker);

            count++;

        }

    }

    map.addLayer(markers);

    if (Run === 1 && count > 0) {
        map.fitBounds(markers.getBounds());
    }

}

function DrawMap2(EVSEStatus:     any,
                  Run:            number,
                  filterpattern,
                  map,
                  markers,
                  customCircleMarker,
                  EChargingIcon) {

    let CurrentEVSEStatus = {};

    for (var evseid in EVSEStatus) {

        var status       = WWCP.EVSEStatusTypes.unknown;
        var timestamped  = EVSEStatus[evseid];

        for (var timestamp in timestamped) {
            status = timestamped[timestamp];
            break;
        }

        CurrentEVSEStatus[evseid] = status;
        //.replace('+49*822*', 'DE*822*E').replace('+33*822*', 'FR*822*E')

    }

    __EVSEStatus = CurrentEVSEStatus;

}

function Reserve(Button, EVSEId)
{

    //const SVGSockets = document.getElementsByClassName('SVGSockets');
    //
    //for (var i = 0; i < SVGSockets.length; i++) {
    //    var SVGDoc = (SVGSockets[i] as any).contentDocument.childNodes[1];//.childNodes[1];
    //    SVGDoc.style.fill = 'red';
    //}

    let SVGDoc = Button.parentElement.parentElement.getElementsByClassName('SVGSockets')[0].contentDocument.childNodes[0]
    SVGDoc.style.fill = 'red';
  //  SVGDoc.stroke.fill = 'red';

    Button.blur();

//    alert("Reserve!");

}

function RemoteStart(Button, EVSEId) {

    worker.postMessage({
        'cmd':     'RemoteStart',
        'EVSEId':  EVSEId
    });

    Button.blur();

}


function sss(EVSEId) {

    if (__EVSEStatus[EVSEId] !== "Available") {
        const ss = document.getElementById('SVG_' + EVSEId.replace(/\*/g, '')) as any;
        ss.contentDocument.childNodes[0].style.fill = 'rgba(167, 167, 167, 0.7)';
    }

}





function OpenInfoText(m) {

    const InfoTextDiv          = document.getElementById('InfoText');
    const pool                 = m.target.options.ChargingPool as WWCP.ChargingPool;
    const distance             = __UserPositon.DistanceTo(pool.GeoLocation, 1);

    //var aa = new WWCP.GeoCoordinate(50.9323824, 11.6259274).DistanceTo(new WWCP.GeoCoordinate(50.9323573, 11.6246802), 8); // == 0.08744731

    InfoTextDiv.style.display  = "block";
    InfoTextDiv.innerHTML      = '<div id="TextGroup">' +
                                     '<div id="ChargingStationName">' +        pool.Name.       de + "</div>" +
                                     '<div id="ChargingStationDescription">' + pool.Description.de + ' (' + distance + " km)</div>" +
                                     '<div><button onclick="Reserve(this, \'' + pool.ChargingPoolId + '\')" class="ReserveButton">Reservieren</button></div>' +
                                 '</div>' +

                                 '<div id="ChargingStationGroup">' + pool.ChargingStations.map((station, index, array) =>

                                     '<div id="' + station.ChargingStationId + '" class="ChargingStations">' +
                                     '<div class="ChargingStationInfo">' + station.Description.de + '</div>' +
                                     station.EVSEs.map((evse, index, array) =>

                                         '<div id="DIV_' + evse.EVSEId.replace(/\*/g, '') + '" class="EVSEs EVSEStatus' + __EVSEStatus[evse.EVSEId] + '">' +

                                             '<div class="PlugInfos">' +

                                                 //'<img src="' + evse.SocketOutlets.
                                                 //                        map   ((socketOutlet, index, array)  => socketOutlet.PlugImage).
                                                 //                        reduce((prev, curr,   index, array)  => prev + ' ' + curr) +
                                                 //                        '" class="PlugImage">' +

                                                 '<object onload="sss(\'' + evse.EVSEId + '\')" type="image/svg+xml" data="' + evse.SocketOutlets.
                                                                                              map   ((socketOutlet, index, array)  => socketOutlet.PlugImage).
                                                                                              reduce((prev, curr,   index, array)  => prev + ' ' + curr) +
                                                                                              '" id="SVG_' + evse.EVSEId.replace(/\*/g, '') + '" class="SVGSockets"></object>' +

                                                 //'<object type="image/svg+xml" data="/images/Ladestecker/IEC_Typ_2.svg" id="SVG_' + evse.EVSEId.replace('*', '') + '" class="SVGSockets"></object>' +

                                             '</div>' +

                                             '<div class="EVSEInfos">' +

                                                 'max ' +
                                                 '<div class="ElectricalInfos">' + evse.MaxPower + ' kW</div>' +
                                                 '<div class="EVSEStatusInfos">' + __EVSEStatus[evse.EVSEId] + '</div>' +

                                             '</div>' +

                                             '<div class="EVSEButtons">' +
                                                 '<button onclick="Reserve(this, \''     + evse.EVSEId + '\')" class="ReserveButton" '     + (__EVSEStatus[evse.EVSEId] !== "Available" ? 'disabled=true ' : '') + '>Reservieren</button>' +
                                                 '<button onclick="RemoteStart(this, \'' + evse.EVSEId + '\')" class="RemoteStartButton" ' + (__EVSEStatus[evse.EVSEId] !== "Available" ? 'disabled=true ' : '') + '>Laden</button>' +
                                             '</div>' +

                                         '</div>').

                                         reduce((prev, curr, index, array) => prev + curr) + '</div>').
                                         reduce((prev, curr, index, array) => prev + curr) +

                                     '</div>';


    setTimeout(function () {



    }, 3000);

}
