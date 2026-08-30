#pragma warning disable 1587
/**
 * Copyright 2024-2026 Wingify Software Pvt. Ltd.
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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using VWOFmeSdk.Models.User;
using WingifyFmeSdk;
using WingifyFmeSdk.Utils;
using WingifyFmeSdk.Services;

namespace VWOFmeSdk
{
    public class VWO : VWOClient
    {
        private static VWOBuilder vwoBuilder;
        private static VWO instance;

        public VWO(string settings, VWOInitOptions options, VWOBuilder vwoBuilder) : base(settings, options, vwoBuilder) { }

        private static VWO SetInstance(VWOInitOptions options)
        {
            if (options.VwoBuilder != null)
            {
                vwoBuilder = (VWOBuilder)options.VwoBuilder;
            }
            else
            {
                vwoBuilder = new VWOBuilder(options);
            }

            vwoBuilder
                .SetMaxRequestQueueCapacity()
                .SetLogger()
                .SetSettingsManager()
                .SetStorage()
                .SetNetworkManager()
                .SetSegmentation()
                .InitPolling()
                .InitUsageStats();

            string settings;
            if (!string.IsNullOrEmpty(options.Settings))
            {
                settings = options.Settings;
                vwoBuilder.SetSettings(settings);
            }
            else if (vwoBuilder.settingsSetManually)
            {
                settings = vwoBuilder.GetOriginalSettings();
            }
            else
            {
                settings = vwoBuilder.GetSettings(false);
            }

            VWO vwoInstance = new VWO(settings, options, vwoBuilder);

            vwoBuilder.SetWingifyClient(vwoInstance);
            vwoBuilder.InitBatching();
            return vwoInstance;
        }

        public static new VWO GetInstance()
        {
            return instance;
        }

        public static VWO Init(VWOInitOptions options)
        {
            // Set flag before initializing Wingify core
            BrandContext.SetIsViaVwo(true);
            
            var initStartTime = DateTime.UtcNow;
            
            if (options == null || string.IsNullOrEmpty(options.SdkKey))
            {
                string message = WingifyFmeSdk.Utils.LogMessageUtil.BuildMessage("SDK key is required to initialize VWO. Please provide the sdkKey in the options.", null);
                Console.Error.WriteLine(message);
                return null;
            }

            if (options == null || options.AccountId == null || string.IsNullOrEmpty(options.AccountId.ToString()))
            {    
                string message = WingifyFmeSdk.Utils.LogMessageUtil.BuildMessage("Account ID is required to initialize VWO. Please provide the accountId in the options.", null);
                Console.Error.WriteLine(message);
                return null;
            }

            if (options.IsAliasingEnabled && (options.GatewayService == null || string.IsNullOrEmpty(options.GatewayService["url"].ToString())))
            {
                string message = WingifyFmeSdk.Utils.LogMessageUtil.BuildMessage("Please provide the gatewayService URL in the options if aliasing is enabled", null);
                Console.Error.WriteLine(message);
                return null;
            }

            instance = SetInstance(options);

            var initEndTime = DateTime.UtcNow;
            var sdkInitTime = (int)(initEndTime - initStartTime).TotalMilliseconds;

            bool wasInitialized = false;
            var originalSettingsString = vwoBuilder.GetOriginalSettings();
            if (!string.IsNullOrEmpty(originalSettingsString))
            {
                var originalSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(originalSettingsString);
                if (originalSettings.ContainsKey("sdkMetaInfo") && originalSettings["sdkMetaInfo"] != null)
                {
                    var sdkMetaInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(originalSettings["sdkMetaInfo"].ToString());
                    wasInitialized = sdkMetaInfo.ContainsKey("wasInitializedEarlier") ? (bool)sdkMetaInfo["wasInitializedEarlier"] : false;
                }
                else
                {
                    wasInitialized = false;
                }
            }
            else
            {
                wasInitialized = false;
            }

            var settingsManager = vwoBuilder.GetSettingsManager();
            if (!wasInitialized && settingsManager != null && settingsManager.IsSettingsValid)
            {
                WingifyFmeSdk.Utils.EventUtil.SendSdkInitEvent(settingsManager.SettingsFetchTime, sdkInitTime);
            }

            var internalEventsThrottleService = InternalEventsThrottleService.Default;
            var settingsDocument = InternalEventsSamplingUtil.ParseSettingsDocument(originalSettingsString);
            long? usageStatsAccountId = InternalEventsSamplingUtil.GetUsageStatsAccountId(settingsDocument);
            if (usageStatsAccountId.HasValue
                && internalEventsThrottleService.ShouldSendUsageStatsEvent(settingsDocument))
            {
                WingifyFmeSdk.Utils.EventUtil.SendSDKUsageStatsEvent((int)usageStatsAccountId.Value);
            }

            return instance;
        }
    }
}
