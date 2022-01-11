/*
 * Copyright (c) 2014-2022 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP Cloud <https://git.graphdefined.com/OpenChargingCloud/WWCP_Cloud>
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

using org.GraphDefined.WWCP.EMSP;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.WWCP
{

    public static class LocalEMobilityServiceExtentions
    {

        public static eMobilityProvider

            CreateEMobilityServiceProvider(this RoamingNetwork                           RoamingNetwork,
                                           eMobilityProvider_Id                          Id,
                                           I18NString                                    Name,
                                           I18NString                                    Description    = null,
                                           Action<eMobilityProvider>                     Configurator   = null,
                                           eMobilityProviderPriority                     Priority       = null,
                                           eMobilityProviderAdminStatusTypes             AdminStatus    = eMobilityProviderAdminStatusTypes.Operational,
                                           eMobilityProviderStatusTypes                  Status         = eMobilityProviderStatusTypes.Available,
                                           Action<eMobilityProvider>                     OnSuccess      = null,
                                           Action<RoamingNetwork, eMobilityProvider_Id>  OnError        = null)

            => RoamingNetwork.CreateEMobilityProvider(Id,
                                                      Name,
                                                      Description,
                                                      Priority,
                                                      Configurator,

                                                      // Remote EMP...
                                                      emp => new eMobilityServiceProvider(emp.Id,
                                                                                          emp.RoamingNetwork),

                                                      AdminStatus,
                                                      Status,
                                                      OnSuccess,
                                                      OnError);

    }

}
