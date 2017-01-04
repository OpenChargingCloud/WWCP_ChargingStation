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

self.addEventListener('message', e => {

    var data   = e.data;
    var worker = <Worker> <any> self;

    switch (data.cmd) {

        case 'start':
            worker.postMessage('WORKER STARTED: ' + data.msg);
            break;

        case 'stop':
            worker.postMessage('WORKER STOPPED: ' + data.msg);
           // aa.close();
            break;

        case 'RemoteStart':
            worker.postMessage('Remote start of: ' + data.EVSEId);
            break;

        default:
            worker.postMessage('Unkown command: ' + data.msg);

    };

}, false);
