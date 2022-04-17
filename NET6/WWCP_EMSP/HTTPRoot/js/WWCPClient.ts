/*
 * Copyright (c) 2014-2019, GaphDefined GmbH <achim.friedland@graphdefined.com>
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

    export interface OnNewDataDelegate {
        (ChargingPoolData: WWCP.ChargingPool[],     Run: number): void;
    }

    export interface OnNewStatusDelegate {
        (EVSEStatus: any, Run: number): void;
    }

    export class Client {

        private _URI:             string;
        get URI()             { return this._URI;           }


        private _DataRun:         number;
        get DataRun()         { return this._DataRun;       }

        private _DataIntervall:   number;
        get DataIntervall()   { return this._DataIntervall; }

        private _OnNewData:       OnNewDataDelegate;
        get OnNewData()       { return this._OnNewData;     }


        private _StatusRun:       number;
        get StatusRun()       { return this._StatusRun;   }

        private _StatusIntervall: number;
        get StatusIntervall() { return this._StatusIntervall; }

        private _OnNewStatus:     OnNewStatusDelegate;
        get OnNewStatus()     { return this._OnNewStatus; }


        private _OnError:         string;
        get OnError()         { return this._OnError;     }


        constructor(URI:              string,
                    OnNewData:        OnNewDataDelegate,
                    DataIntervall:    number,
                    OnNewStatus:      OnNewStatusDelegate,
                    StatusIntervall:  number) {

            if (URI !== undefined) {
                this._URI = URI;
            }

            this._DataIntervall    = DataIntervall;
            this._OnNewData        = OnNewData;
            this._DataRun          = 1;

            this._StatusIntervall  = StatusIntervall;
            this._OnNewStatus      = OnNewStatus;
            this._StatusRun        = 1;

            this.DownloadChargingPools(this._URI, this._DataRun,   this._DataIntervall);
            this.DownloadEVSEStatus   (this._URI, this._StatusRun, this._StatusIntervall);

        }

        private DownloadChargingPools(URI: string, Run: number, Intervall: number) {

            Download(URI + '/ChargingPools',
                     (NewData) => {

                         window.localStorage.setItem('ChargingPools', NewData);

                         const DataArray = JSON.parse(NewData);

                         const CPs : WWCP.ChargingPool[] = DataArray.map(chargingpool => new WWCP.ChargingPool(chargingpool));

                         this._OnNewData(CPs, Run);

                     },
                     this._OnError);

            window.setTimeout(() => this.DownloadChargingPools(this._URI, Run + 1, Intervall), Intervall);

        }

        private DownloadEVSEStatus(URI: string, Run: number, Intervall: number) {

            DownloadStatus(URI + '/EVSEs',
                (NewData) => {

                    window.localStorage.setItem('EVSEStatus', NewData);

                    this._OnNewStatus(JSON.parse(NewData), Run);

                },
                this._OnError);

            window.setTimeout(() => this.DownloadEVSEStatus(this._URI, Run + 1, Intervall), Intervall);

        }

    }

}