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
        private string settings;
        private string originalSettings;
        private bool isSettingsFetchInProgress;

        public VWOBuilder(VWOInitOptions options)
        {
            this.options = options;
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
                networkInstance.AttachClient(this.options.NetworkClientInterface);
            }
            else
            {
                networkInstance.AttachClient();
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
                LoggerService.Log(LogLevelEnum.ERROR, "SETTINGS_FETCH_ERROR", new Dictionary<string, string>
                {
                    { "err", e.ToString() }
                });
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
                Storage.Instance.AttachConnector(options.Storage);
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
                LoggerService.Log(LogLevelEnum.ERROR, "INIT_OPTIONS_INVALID", new Dictionary<string, string>
                {
                    { "key", "pollInterval" },
                    { "correctType", "number" }
                });
                return this;
            }

            if (this.options.PollInterval != null && this.options.PollInterval < 1000)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "INIT_OPTIONS_INVALID", new Dictionary<string, string>
                {
                    { "key", "pollInterval" },
                    { "correctType", "number" }
                });
                return this;
            }

            new System.Threading.Thread(CheckAndPoll).Start();

            return this;
        }

        /// <summary>
        /// Checks and polls for settings updates at the provided interval.
        /// </summary>
        private void CheckAndPoll()
        {
            int pollingInterval = this.options.PollInterval.Value;

            while (true)
            {
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
                    LoggerService.Log(LogLevelEnum.ERROR, "POLLING_FAILED", new Dictionary<string, string>
                    {
                        { "err", ex.ToString() }
                    });
                }

                System.Threading.Thread.Sleep(pollingInterval);
            }
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
