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
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Packages.NetworkLayer.Manager;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.SegmentationEvaluator.Core;
using VWOFmeSdk.Packages.Storage;
using Newtonsoft.Json;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.Schemas;
using VWOFmeSdk.Services;
using VWOFmeSdk.Constants;
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Enums;

namespace VWOFmeSdk
{
    /// <summary>
    /// VWOBuilder class to build the VWO instance.
    /// </summary>
    public class VWOBuilder
    {
        private VWOClient vwoClient;
        private readonly VWOInitOptions options;
        private SettingsManager settingsManager;
        private Settings settings;
        private string originalSettings;
        private bool isSettingsFetchInProgress;
        public bool settingsSetManually = false;
        public bool stopPolling = false;

        public bool IsBatchingUsed { get; private set; }
        private BatchEventQueue batchEventQueue;

        public VWOBuilder(VWOInitOptions options)
        {
            this.options = options;
        }

        /// <summary>
        /// Gets the settings manager.
        /// </summary>
        /// <returns>The settings manager.</returns>
        public SettingsManager GetSettingsManager()
        {
            return settingsManager;
        }

        // Set VWOClient instance
        public void SetVWOClient(VWOClient vwoClient)
        {
            this.vwoClient = vwoClient;
        }

        /// <summary>
        /// Sets the network manager with the provided client and development mode options.
        /// </summary>
        /// <returns>The VWOBuilder instance.</returns>
        public VWOBuilder SetNetworkManager()
        {
            NetworkManager networkInstance = NetworkManager.GetInstance();
            if (this.options != null && this.options.NetworkClientInterface != null)
            {
                networkInstance.AttachClient(this.options.NetworkClientInterface, this.options.RetryConfig ?? ConstantsNamespace.Constants.DEFAULT_RETRY_CONFIG);
            }
            else
            {
                networkInstance.AttachClient(null, this.options.RetryConfig ?? ConstantsNamespace.Constants.DEFAULT_RETRY_CONFIG    );
            }
            networkInstance.GetConfig().SetDevelopmentMode(false);
            LoggerService.Log(LogLevelEnum.DEBUG, "SERVICE_INITIALIZED", new Dictionary<string, string>
            {
                { "service", "Network Layer" }
            });
            return this;
        }

        /// <summary>
        /// Sets the segmentation evaluator with the provided segmentation options.
        /// </summary>
        /// <returns>The instance of this builder.</returns>
        public VWOBuilder SetSegmentation()
        {
            if (options != null && options.SegmentEvaluator != null)
            {
                SegmentationManager.GetInstance().AttachEvaluator(options.SegmentEvaluator);
            }
            LoggerService.Log(LogLevelEnum.DEBUG, "SERVICE_INITIALIZED", new Dictionary<string, string>
            {
                { "service", "Segmentation Evaluator" }
            });
            return this;
        }

         /// <summary>
        /// Sets the settings manually for the SDK.
        /// </summary>
        /// <param name="settings">Settings as a JSON string.</param>
        public void SetSettings(string settings)
        {
            LoggerService.Log(LogLevelEnum.DEBUG, "API - SetSettings called");

            if (string.IsNullOrEmpty(settings))
            {
                LogManager.GetInstance().ErrorLog("SETTINGS_NULL_OR_EMPTY", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                return;
            }
            // Store the original settings as a string
            originalSettings = settings;

            try
            {
                // Deserialize the JSON string into a Settings object
                Settings settingsObject;
                try
                {
                    settingsObject = JsonConvert.DeserializeObject<Settings>(settings);
                }
                catch (JsonReaderException jsonEx)
                {
                    string path = jsonEx.Path ?? "unknown";
                    LogManager.GetInstance().ErrorLog("SETTINGS_JSON_PARSE_ERROR", new Dictionary<string, string> { { "path", path }, { "error", jsonEx.Message } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                    return;
                }

                if (settingsObject == null)
                {
                    LogManager.GetInstance().ErrorLog("SETTINGS_SCHEMA_INVALID", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                    return;
                }

                // Process the settings using SettingsUtil
                SettingsUtil.ProcessSettings(settingsObject);

                // Assign the processed settings back to this.settings as a Settings object
                this.settings = settingsObject;

                // Mark settings as manually set
                settingsSetManually = true;

                LoggerService.Log(LogLevelEnum.INFO, "Settings have been manually set");
            }
            catch (Exception ex)
            {
                LogManager.GetInstance().ErrorLog("ERROR_SETTING_SETTINGS_MANUALLY", new Dictionary<string, string> { { "err", FunctionUtil.GetFormattedErrorMessage(ex) } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
            }
        }

        public string GetOriginalSettings()
        {
            return originalSettings;
        }


        /// <summary>
        /// Fetches settings asynchronously, ensuring no parallel fetches.
        /// </summary>
        /// <param name="forceFetch">Force fetch ignoring cache.</param>
        /// <returns>The fetched settings.</returns>
        public string FetchSettings(bool forceFetch)
        {
            if (isSettingsFetchInProgress || settingsManager == null)
            {
                return null;
            }

            isSettingsFetchInProgress = true;

            try
            {
                string settings = settingsManager.GetSettings(forceFetch);

                if (!forceFetch)
                {
                    originalSettings = settings;
                }

                isSettingsFetchInProgress = false;
                return settings;
            }
            catch (Exception e)
            {
                LogManager.GetInstance().ErrorLog("ERROR_FETCHING_SETTINGS", new Dictionary<string, string> { { "err", FunctionUtil.GetFormattedErrorMessage(e) } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                isSettingsFetchInProgress = false;
                return null;
            }
        }

        /// <summary>
        /// Gets the settings, fetching them if not cached or if forced.
        /// </summary>
        /// <param name="forceFetch">Force fetch ignoring cache.</param>
        /// <returns>The fetched settings.</returns>
        public string GetSettings(bool forceFetch)
        {
            return FetchSettings(forceFetch);
        }

        /// <summary>
        /// Sets the storage connector for the VWO instance.
        /// </summary>
        /// <returns>The instance of this builder.</returns>
        public VWOBuilder SetStorage()
        {
            if (options != null && options.Storage != null)
            {
                VWOFmeSdk.Packages.Storage.Storage.Instance.AttachConnector(options.Storage);
            }
            return this;
        }

        /// <summary>
        /// Sets the settings manager for the VWO instance.
        /// </summary>
        /// <returns>The instance of this builder.</returns>
        public VWOBuilder SetSettingsManager()
        {
            if (options == null)
            {
                return this;
            }
            settingsManager = new SettingsManager(options);
            return this;
        }

        /// <summary>
        /// Sets the logger for the VWO instance.
        /// </summary>
        /// <returns>The instance of this builder.</returns>
        public VWOBuilder SetLogger()
        {
            try
            {
                if (this.options == null || this.options.Logger == null || this.options.Logger.Count == 0)
                {
                    new LoggerService(new Dictionary<string, object>());
                }
                else
                {
                    new LoggerService(ConvertLoggerOptions(this.options.Logger));
                }
                LoggerService.Log(LogLevelEnum.DEBUG, "SERVICE_INITIALIZED", new Dictionary<string, string>
                {
                    { "service", "Logger" }
                });
            }
            catch (Exception e)
            {
                string message = LogMessageUtil.BuildMessage("Error occurred while initializing Logger : " + e.Message, null);
                Console.Error.WriteLine(message);
            }
            return this;
        }

        /// <summary>
        /// Initializes the polling with the provided poll interval.
        /// </summary>
        /// <returns>The instance of this builder.</returns>
        public VWOBuilder InitPolling()
        {
            if (this.options.PollInterval == null)
            {
                return this;
            }

            if (this.options.PollInterval != null && !DataTypeUtil.IsInteger(this.options.PollInterval))
            {
                LogManager.GetInstance().ErrorLog("INIT_OPTIONS_INVALID", new Dictionary<string, string> { { "key", "pollInterval" }, { "correctType", "number" } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                return this;
            }

            if (this.options.PollInterval != null && this.options.PollInterval < 1000)
            {
                LogManager.GetInstance().ErrorLog("INIT_OPTIONS_INVALID", new Dictionary<string, string> { { "key", "pollInterval" }, { "correctType", "number" } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                return this;
            }
            stopPolling = false;

            var pollingThread = new System.Threading.Thread(CheckAndPoll)
            {
                IsBackground = true
            };
            pollingThread.Start();


            return this;
        }

        /// <summary>
        /// Initializes batching based on options.
        /// </summary>
        /// <returns>The instance of this builder.</returns>
        public VWOBuilder InitBatching()
        {
            // Check if gateway service is provided and skip SDK batching if so
            if (SettingsManager.GetInstance().isGatewayServiceProvided)
            {
                LoggerService.Log(LogLevelEnum.INFO, "GATEWAY_SERVICE_CONFIGURED", null);
                return this;
            }

            // Check if batch event data is provided in options
            if (this.options?.BatchEventData != null)
            {
                int eventsPerRequest = this.options.BatchEventData.EventsPerRequest;
                int requestTimeInterval = this.options.BatchEventData.RequestTimeInterval;

                bool isEventsPerRequestValid = DataTypeUtil.IsInteger(eventsPerRequest) && 
                    eventsPerRequest > 0 && 
                    eventsPerRequest <= ConstantsNamespace.Constants.MAX_EVENTS_PER_REQUEST;
                
                bool isRequestTimeIntervalValid = DataTypeUtil.IsInteger(requestTimeInterval) && 
                    requestTimeInterval > 0;

                // Check data type and values for eventsPerRequest and requestTimeInterval
                if (!isEventsPerRequestValid && !isRequestTimeIntervalValid)
                {
                    LogManager.GetInstance().ErrorLog("INVALID_BATCH_EVENTS_CONFIG", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                    return this;
                }

                // Handle invalid data types for individual parameters
                if (!isEventsPerRequestValid)
                {
                    LogManager.GetInstance().ErrorLog("INVALID_EVENTS_PER_REQUEST", new Dictionary<string, string> { { "key", "events_per_request" }, { "correctType", "number" } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                    eventsPerRequest = ConstantsNamespace.Constants.DEFAULT_EVENTS_PER_REQUEST; // Use default if invalid
                }
                if (!isRequestTimeIntervalValid)
                {
                    LogManager.GetInstance().ErrorLog("INVALID_REQUEST_TIME_INTERVAL", new Dictionary<string, string> { { "key", "request_time_interval" }, { "correctType", "number" } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                    requestTimeInterval = ConstantsNamespace.Constants.DEFAULT_REQUEST_TIME_INTERVAL; // Use default if invalid
                }
                // Initialize BatchEventQueue for batching
                batchEventQueue = new BatchEventQueue(
                    eventsPerRequest,
                    requestTimeInterval,
                    this.options.AccountId ?? 0,
                    this.options.SdkKey,
                    this.options.BatchEventData.FlushCallback
                );

                vwoClient.BatchEventQueue = batchEventQueue; // Link the BatchEventQueue to the vwoClient
                IsBatchingUsed = true;

                LoggerService.Log(LogLevelEnum.DEBUG,"Event Batching initialized successfully in SDK.");
            }
            else
            {
                LoggerService.Log(LogLevelEnum.DEBUG,"Event Batching functionality not initialized. SDK batching is disabled.");
                IsBatchingUsed = false;
            }

            return this;
        }

        /// <summary>
        /// Initializes the usage stats for the VWO instance.
        /// </summary>
        /// <returns>The instance of this builder.</returns>
        public VWOBuilder InitUsageStats()
        {
            // if usageStatsDisabled is not null and is true, then return
            if (this.options.IsUsageStatsDisabled)
            {
                return this;
            }
            
            UsageStatsUtil.GetInstance().SetUsageStats(this.options);
            
            return this;
        } 

        /// <summary>
        /// Checks and polls for settings updates at the provided interval.
        /// </summary>
        private void CheckAndPoll()
        {
            int pollingInterval = this.options.PollInterval.Value;

            while (!stopPolling)
            {
                System.Threading.Thread.Sleep(pollingInterval);
                if (stopPolling)
                {
                    break;
                }
                try
                {
                    string latestSettings = GetSettings(true);
                    if (originalSettings != null && latestSettings != null)
                    {
                        var latestSettingJsonNode = Newtonsoft.Json.JsonConvert.DeserializeObject(latestSettings);
                        var originalSettingsJsonNode = Newtonsoft.Json.JsonConvert.DeserializeObject(originalSettings);
                        if (!latestSettingJsonNode.Equals(originalSettingsJsonNode))
                        {
                            originalSettings = latestSettings;
                            LoggerService.Log(LogLevelEnum.INFO, "POLLING_SET_SETTINGS", null);
                            if (vwoClient != null)
                            {
                                vwoClient.UpdateSettings(originalSettings);
                            }
                        }
                        else
                        {
                            LoggerService.Log(LogLevelEnum.INFO, "POLLING_NO_CHANGE", null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.GetInstance().ErrorLog("POLLING_FAILED", new Dictionary<string, string> { { "err", FunctionUtil.GetFormattedErrorMessage(ex) } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                }
            }
        }

        /// <summary>
        /// Stops background activities such as polling and batching timers.
        /// </summary>
        public void StopPolling()
        {
            stopPolling = true;
        }
        
        /// <summary>
        /// Converts logger options from Dictionary<string, string> to Dictionary<string, object>.
        /// </summary>
        /// <param name="loggerOptions">The logger options as Dictionary<string, string>.</param>
        /// <returns>The logger options as Dictionary<string, object>.</returns>
        private static Dictionary<string, object> ConvertLoggerOptions(Dictionary<string, object> loggerOptions)
        {
            var convertedLoggerOptions = new Dictionary<string, object>();
            foreach (var option in loggerOptions)
            {
                convertedLoggerOptions[option.Key] = option.Value;
            }
            return convertedLoggerOptions;
        }
    }
}