#pragma warning disable 1587
/**
 * Copyright 2024-2025 Wingify Software Pvt. Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#pragma warning restore 1587

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.Schemas;
using VWOFmeSdk.Models.User;

namespace VWOFmeSdk.Utils
{
    /// <summary>
    /// Utility class for sending SDK init events to VWO.
    /// </summary>
    public static class EventUtil
    {
        /// <summary>
        /// Sends an init called event to VWO.
        /// This event is triggered when the init function is called.
        /// </summary>
        /// <param name="settingsFetchTime">Time taken to fetch settings in milliseconds.</param>
        /// <param name="sdkInitTime">Time taken to initialize the SDK in milliseconds.</param>
        public static void SendSdkInitEvent(int? settingsFetchTime = null, int? sdkInitTime = null)
        {
            try
            {
                // Create the query parameters
                var properties = NetworkUtil.GetEventsBaseProperties(EventEnum.VWO_SDK_INIT_EVENT.GetValue());

                // Create the payload with required fields
                var payload = NetworkUtil.GetSdkInitEventPayload(EventEnum.VWO_SDK_INIT_EVENT.GetValue(), settingsFetchTime, sdkInitTime);

                // Check if batching is available through VWO instance
                var vwoInstance = VWO.GetInstance();
                if (vwoInstance?.BatchEventQueue != null)
                {
                    vwoInstance.BatchEventQueue.Enqueue(payload);
                }
                else
                {
                    NetworkUtil.SendEvent(properties, payload, EventEnum.VWO_SDK_INIT_EVENT.GetValue());
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "SDK_INIT_EVENT_FAILED", new Dictionary<string, string>
                {
                    { "err", ex.ToString() }
                });
            }
        }

    }
} 