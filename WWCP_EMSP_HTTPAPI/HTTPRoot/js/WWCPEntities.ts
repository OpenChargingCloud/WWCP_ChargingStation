/*
 * Copyright (c) 2014-2017, GaphDefined GmbH <achim.friedland@graphdefined.com>
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

module WWCP {

    export class RoamingNetwork
    { }

    export class EVSEOperator
    { }

    export class ChargingPool {

        private _ChargingPoolId:   string;
        private _Name:             I18NString;
        private _Description:      I18NString;
        private _GeoLocation:      GeoCoordinate;
        private _ChargingStations: ChargingStation[];

        get ChargingPoolId()   { return this._ChargingPoolId; }
        get Name()             { return this._Name; }
        get Description()      { return this._Description; }
        get GeoLocation()      { return this._GeoLocation; }
        get ChargingStations() { return this._ChargingStations; }

        constructor(JSON: any) {

            if (JSON !== undefined) {
                this._ChargingPoolId    =  JSON.hasOwnProperty("ChargingPoolId")        ? JSON.ChargingPoolId                    : null;
                this._Name              =  JSON.hasOwnProperty("Name")                  ? new I18NString(JSON.Name)              : null;
                this._Description       =  JSON.hasOwnProperty("Description")           ? new I18NString(JSON.Description)       : null;
                this._GeoLocation       =  JSON.hasOwnProperty("GeoLocation")           ? GeoCoordinate.Parse(JSON.GeoLocation)  : null;
                this._ChargingStations  = (JSON.hasOwnProperty("ChargingStations") &&
                                                JSON.ChargingStations instanceof Array) ? JSON.ChargingStations.map((station, index, array) =>
                                                                                              new ChargingStation(station))      : null;
            }

        }

    }

    export class ChargingStation {

        private _ChargingStationId:  string;
        private _Name:               I18NString;
        private _Description:        I18NString;
        private _EVSEs:              EVSE[];

        get ChargingStationId() { return this._ChargingStationId; }
        get Name()              { return this._Name; }
        get Description()       { return this._Description; }
        get EVSEs()             { return this._EVSEs; }

        constructor(JSON: any) {

            if (JSON !== undefined) {
                this._ChargingStationId  =  JSON.hasOwnProperty("ChargingStationId") ? JSON.ChargingStationId           : null;
                this._Name               =  JSON.hasOwnProperty("Name")              ? new I18NString(JSON.Name)        : null;
                this._Description        =  JSON.hasOwnProperty("Description")       ? new I18NString(JSON.Description) : null;
                this._EVSEs              = (JSON.hasOwnProperty("EVSEs") &&
                                                JSON.EVSEs instanceof Array)         ? JSON.EVSEs.map((evse, index, array) =>
                                                                                           new EVSE(evse))              : null;
            }

        }

    }

    export class EVSE {

        private _EVSEId:             string;
        private _Description:        I18NString;
        private _MaxPower:           Number;
        private _SocketOutlets:      SocketOutlet[];

        get EVSEId()        { return this._EVSEId; }
        get Description()   { return this._Description; }
        get MaxPower()      { return this._MaxPower; }
        get SocketOutlets() { return this._SocketOutlets; }

        constructor(JSON: any) {

            this._EVSEId         = JSON.EVSEId;
            this._Description    = new I18NString(JSON.Description);
            this._MaxPower       = JSON.MaxPower;
            this._SocketOutlets  = JSON.SocketOutlets.map((socketOutlet, index, array) =>
                                        new SocketOutlet(socketOutlet));

        }

    }

    export class SocketOutlet {

        private _Plug:               SocketTypes;
        private _PlugImage:          string;

        get Plug()      { return this._Plug; }
        get PlugImage() { return this._PlugImage; }

        constructor(JSON: any) {

            const prefix = "images/Ladestecker/";

            switch (JSON.Plug) {

                case "TypeFSchuko":
                    this._Plug       = SocketTypes.TypeFSchuko;
                    this._PlugImage  = prefix + "Schuko.svg";
                    break;

                case "Type2Outlet":
                    this._Plug       = SocketTypes.Type2Outlet;
                    this._PlugImage  = prefix + "IEC_Typ_2.svg";
                    break;

                case "Type2Connector_CableAttached":
                    this._Plug       = SocketTypes.Type2Outlet;
                    this._PlugImage  = prefix + "IEC_Typ_2_Cable.svg";
                    break;

                case "CHAdeMO":
                    this._Plug       = SocketTypes.CHAdeMO;
                    this._PlugImage  = prefix + "CHAdeMO.svg";
                    break;

                case "CCSCombo2Plug_CableAttached":
                    this._Plug       = SocketTypes.CCSCombo2Plug_CableAttached;
                    this._PlugImage  = prefix + "CCS_Typ_2.svg";
                    break;

                default:
                    this._Plug       = SocketTypes.unknown;
                    this._PlugImage  = "";
                    break;

            }

        }

    }

    export class EVSEStatusRecord {

        private _EVSEId:      string;
        private _EVSEStatus:  EVSEStatusTypes;

        get EVSEId()     { return this._EVSEId;     }
        get EVSEStatus() { return this._EVSEStatus; }

        static Parse(EVSEId: any, JSON: any): EVSEStatusRecord {

            var status: EVSEStatusTypes;

            for (var timestamp in JSON) {
                status = JSON[timestamp];
                break;
            }

            if (JSON !== undefined) {
                return new EVSEStatusRecord(
                    <string>          EVSEId,
                    status
                );
            }

        }

        constructor(EVSEId:      string,
                    EVSEStatus:  EVSEStatusTypes) {

            this._EVSEId     = EVSEId;
            this._EVSEStatus = EVSEStatus;

        }

    }


}