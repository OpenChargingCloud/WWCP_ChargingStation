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


interface Number {
    toRad(): number;
}

if (typeof (Number.prototype.toRad) === "undefined") {
    Number.prototype.toRad = function () {
        return this * Math.PI / 180;
    }
}

module WWCP {

    //#region Enums

    export enum SocketTypes {
        unknown,
        TypeFSchuko,
        Type2Outlet,
        CHAdeMO,
        CCSCombo2Plug_CableAttached
    }

    export enum EVSEStatusTypes {
        unknown,
        available,
        reserved,
        charging
    }

    //#endregion

    //#region General data types...

    export class I18NString {

        //ToDo: Refactor /me for TypeScript v2.0
        private _de:                 string;
        private _en:                 string;
        private _fr:                 string;

        get de() { return this._de; }
        get en() { return this._en; }
        get fr() { return this._fr; }

        constructor(JSON: any) {

            if (JSON !== undefined) {
                this._de = JSON.hasOwnProperty("de") ? JSON.de : "";
                this._en = JSON.hasOwnProperty("en") ? JSON.en : "";
                this._fr = JSON.hasOwnProperty("fr") ? JSON.fr : "";
            }

        }

    }

    /**
    * A geo coordinate
    * @class WWCP.GeoCoordinate
    */
    export class GeoCoordinate {

        private _lat: number;
        private _lng: number;

        get lat() { return this._lat; }
        get lng() { return this._lng; }


        /**
        * Create a new geo coordinate.
        * @param {number} Latitude A geo latitude.
        * @param {number} Longitude A geo longitude.
        */
        constructor(Latitude: number, Longitude: number) {
            this._lat = Latitude;
            this._lng = Longitude;
        }

        static Parse(JSON: any): GeoCoordinate {

            if (JSON !== undefined) {
                return new GeoCoordinate(JSON.hasOwnProperty("lat") ? JSON.lat : 0,
                                         JSON.hasOwnProperty("lng") ? JSON.lng : 0);
            }

        }

        /**
        * Returns the distance to the given geo coordinate in km.
        * @param  {GeoCoordinate} Target A geo coordinate.
        * @param  {number} Decimals Number of decimals of the result.
        * @returns {number} the distance to the given geo coordinate in km.
        */
        DistanceTo(Target: GeoCoordinate, Decimals?: number)
        {

                  Decimals     = Decimals || 8;
            const earthRadius  = 6371; // km

            const dLat         = (Target.lat - this._lat).toRad();
            const dLon         = (Target.lng - this._lng).toRad();

            const a            = Math.sin(dLat / 2) * Math.sin(dLat / 2) + Math.sin(dLon / 2) * Math.sin(dLon / 2) * Math.cos(this._lat.toRad()) * Math.cos(Target.lat.toRad());
            const c            = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
            const d            = earthRadius * c;

            return Math.round(d * Math.pow(10, Decimals)) / Math.pow(10, Decimals);

        }

    }

    //#endregion

}