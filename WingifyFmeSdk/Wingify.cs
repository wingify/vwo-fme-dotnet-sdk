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
using WingifyFmeSdk.Models.User;
using WingifyFmeSdk.Utils;
using WingifyFmeSdk.Packages.Logger.Enums;
using WingifyFmeSdk.Services;


namespace WingifyFmeSdk
{
    public class Wingify : WingifyClient
    {
        private static WingifyBuilder vwoBuilder;
        private static Wingify instance;

        /// <summary>
        /// Constructor for the Wingify class.
        /// Initializes a new instance of Wingify with the provided options.
        /// </summary>
        /// <param name="settings">Configuration settings for the Wingify instance.</param>
        /// <param name="options">Configuration options for the Wingify instance.</param>
        public Wingify(string settings, WingifyInitOptions options, WingifyBuilder vwoBuilder) : base(settings, options, vwoBuilder) { }

        /// <summary>
        /// Sets the singleton instance of VWO.
        /// Configures and builds the Wingify instance using the provided options.
        /// </summary>
        /// <param name="options">Configuration options for setting up VWO.</param>
        /// <returns>A configured Wingify instance.</returns>
        private static Wingify SetInstance(WingifyInitOptions options)
        {
            if (options.VwoBuilder != null)
            {
                vwoBuilder = options.VwoBuilder;
            }
            else
            {
                vwoBuilder = new WingifyBuilder(options);
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
            // Create the Wingify instance
            Wingify vwoInstance = new Wingify(settings, options, vwoBuilder);

            // Set WingifyClient instance in WingifyBuilder
            vwoBuilder.SetWingifyClient(vwoInstance);
            vwoBuilder.InitBatching();
            return vwoInstance;
        }

        /// <summary>
        /// Gets the singleton instance of VWO.
        /// </summary>
        /// <returns>The singleton instance of VWO.</returns>
        public static Wingify GetInstance()
        {
            return instance;
        }

        public static Wingify Init(WingifyInitOptions options)
        {
            // Start timer for total init time
            var initStartTime = DateTime.UtcNow;
            
            if (options == null || string.IsNullOrEmpty(options.SdkKey))
            {
                string message = LogMessageUtil.BuildMessage("SDK key is required to initialize VWO. Please provide the sdkKey in the options.", null);
                Console.Error.WriteLine(message);
                return null;
            }

            if (options == null || options.AccountId == null || string.IsNullOrEmpty(options.AccountId.ToString()))
            {    
                string message = LogMessageUtil.BuildMessage("Account ID is required to initialize VWO. Please provide the accountId in the options.", null);
                Console.Error.WriteLine(message);
                return null;
            }

            if (options.IsAliasingEnabled && (options.GatewayService == null || string.IsNullOrEmpty(options.GatewayService["url"].ToString())))
            {
                string message = LogMessageUtil.BuildMessage("Please provide the gatewayService URL in the options if aliasing is enabled", null);
                Console.Error.WriteLine(message);
                return null;
            }

            instance = SetInstance(options);

            // Stop timer for total init time
            var initEndTime = DateTime.UtcNow;
            var sdkInitTime = (int)(initEndTime - initStartTime).TotalMilliseconds;


            // wasInitialized is used to check if the SDK was initialized earlier
            bool wasInitialized = false;
            var originalSettingsString = vwoBuilder.GetOriginalSettings();
            if (!string.IsNullOrEmpty(originalSettingsString))
            {
                // originalSettings is a string, so we need to parse it to a dictionary 
                var originalSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(originalSettingsString);
                // Check if sdkMetaInfo exists and contains wasInitializedEarlier
                if (originalSettings.ContainsKey("sdkMetaInfo") && originalSettings["sdkMetaInfo"] != null)
                {
                    var sdkMetaInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(originalSettings["sdkMetaInfo"].ToString());
                    // Check if wasInitializedEarlier exists in sdkMetaInfo
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
                EventUtil.SendSdkInitEvent(settingsManager.SettingsFetchTime, sdkInitTime);
            }

            var internalEventsThrottleService = InternalEventsThrottleService.Default;
            var settingsDocument = InternalEventsSamplingUtil.ParseSettingsDocument(originalSettingsString);

            long? usageStatsAccountId = InternalEventsSamplingUtil.GetUsageStatsAccountId(settingsDocument);
            if (usageStatsAccountId.HasValue
                && internalEventsThrottleService.ShouldSendUsageStatsEvent(settingsDocument))
            {
                EventUtil.SendSDKUsageStatsEvent((int)usageStatsAccountId.Value);
            }

            return instance;
        }
    }
}
