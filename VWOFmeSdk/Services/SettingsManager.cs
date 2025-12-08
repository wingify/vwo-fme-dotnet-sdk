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
using Newtonsoft.Json;
using System.Collections.Generic;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Constants;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.NetworkLayer.Manager;
using VWOFmeSdk.Packages.NetworkLayer.Models;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Models.Schemas;
using ConstantsNamespace = VWOFmeSdk.Constants;

namespace VWOFmeSdk.Services
{
    public class SettingsManager
    {
        private string sdkKey;
        private int? accountId;
        private int expiry;
        private int networkTimeout;
        public string hostname;
        public int port;
        public string protocol = "https";
        public bool isGatewayServiceProvided = false;
        private static SettingsManager instance;
        private int settingsFetchTime;
        private bool isSettingsValid = false;

        public bool IsSettingsValid
        {
            get { return isSettingsValid; }
        }

        public int SettingsFetchTime
        {
            get { return settingsFetchTime; }
        }

        public string Protocol
        {
            get { return protocol; }
        }

        public int Port
        {
            get { return port; }
        }

        public string SdkKey
        {
            get { return sdkKey; }
        }

        public int? AccountId
        {
            get { return accountId; }
        }


        /// <summary>
        /// Get the instance of SettingsManager
        /// </summary>
        /// <param name="options"></param>
        public SettingsManager(VWOInitOptions options)
        {
            this.sdkKey = options.SdkKey;
            this.accountId = options.AccountId;
            this.expiry = (int)ConstantsNamespace.Constants.SETTINGS_EXPIRY;
            this.networkTimeout = (int)ConstantsNamespace.Constants.SETTINGS_TIMEOUT;

            if (options.GatewayService != null && options.GatewayService.Count > 0)
            {
                isGatewayServiceProvided = true;
                try
                {
                    Uri parsedUrl;
                    string gatewayServiceUrl = options.GatewayService["url"].ToString();
                    object gatewayServiceProtocol = options.GatewayService.ContainsKey("protocol") ? options.GatewayService["protocol"] : null;
                    object gatewayServicePort = options.GatewayService.ContainsKey("port") ? options.GatewayService["port"] : null;

                    if (gatewayServiceUrl.StartsWith("http://") || gatewayServiceUrl.StartsWith("https://"))
                    {
                        parsedUrl = new Uri(gatewayServiceUrl);
                    }
                    else if (gatewayServiceProtocol != null && !string.IsNullOrEmpty(gatewayServiceProtocol.ToString()))
                    {
                        parsedUrl = new Uri($"{gatewayServiceProtocol}://{gatewayServiceUrl}");
                    }
                    else
                    {
                        parsedUrl = new Uri($"https://{gatewayServiceUrl}");
                    }

                    this.hostname = parsedUrl.Host;
                    this.protocol = parsedUrl.Scheme;
                    this.port = parsedUrl.Port != -1 ? parsedUrl.Port : (gatewayServicePort != null ? Convert.ToInt32(gatewayServicePort) : 443);
                }
                catch (Exception e)
                {
                    LoggerService.Log(LogLevelEnum.ERROR, $"Error occurred while parsing gateway service URL: {e.Message}");
                    this.hostname = ConstantsNamespace.Constants.HOST_NAME;
                }
            }
            else
            {
                this.hostname = ConstantsNamespace.Constants.HOST_NAME;
            }

            SettingsManager.instance = this;
        }

        public static SettingsManager GetInstance()
        {
            return instance;
        }

        /// <summary>
        /// Fetches settings from the server and caches it in storage
        /// </summary>
        /// <returns></returns>
        private string FetchSettingsAndCacheInStorage()
        {
            try
            {
                return FetchSettings();
            }
            catch (Exception e)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "SETTINGS_FETCH_ERROR", new Dictionary<string, string> { { "err", e.ToString() } });
            }
            return null;
        }

        /**
         * Fetches settings from the server
         * @return settings
         */
        public string FetchSettings(bool isViaWebhook = false)
        {
            if (sdkKey == null || accountId == null)
            {
                throw new ArgumentException("SDK Key and Account ID are required to fetch settings. Aborting!");
            }

            NetworkManager networkInstance = NetworkManager.GetInstance();
            Dictionary<string, string> options = NetworkUtil.GetSettingsPath(sdkKey, accountId.Value);
            options.Add("api-version", "3");

            Dictionary<string, object> retryConfig = NetworkManager.GetInstance().GetRetryConfig();
            if (!networkInstance.GetConfig().GetDevelopmentMode())
            {
                options.Add("s", "prod");
            }

            string path = isViaWebhook 
                ? ConstantsNamespace.Constants.WEBHOOK_SETTINGS_ENDPOINT 
                : ConstantsNamespace.Constants.SETTINGS_ENDPOINT;

            try
            {
                RequestModel request = new RequestModel(hostname, "GET", path, options, null, null, this.protocol, port, retryConfig);
                request.SetTimeout(networkTimeout);

                // start timer for settings fetch
                var settingsFetchStartTime = DateTime.UtcNow;
                ResponseModel response = networkInstance.Get(request);
                if (response.GetStatusCode() != 200)
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "SETTINGS_FETCH_ERROR", new Dictionary<string, string> { { "err", response.GetError().ToString() } });
                    return null;
                }

                // stop timer for settings fetch               
                var settingsFetchTime = (int)(DateTime.UtcNow - settingsFetchStartTime).TotalMilliseconds;

                this.settingsFetchTime = settingsFetchTime;

                return response.GetData();
            }
            catch (Exception e)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "SETTINGS_FETCH_ERROR", new Dictionary<string, string> { { "err", e.ToString() } });
                return null;
            }
        }

        /**
         * Fetches settings from the server
         * @param forceFetch forceFetch, if pooling - true, else - false
         * @return settings
         */
        public string GetSettings(bool forceFetch)
        {
            if (forceFetch)
            {
                return FetchSettingsAndCacheInStorage();
            }
            else
            {
                try
                {
                    string settings = FetchSettingsAndCacheInStorage();
                    if (settings == null)
                    {
                        LoggerService.Log(LogLevelEnum.ERROR, "SETTINGS_SCHEMA_INVALID", null);
                        return null;
                    }

                    bool settingsValid = new SettingsSchema().IsSettingsValid(JsonConvert.DeserializeObject<Settings>(settings));
                    if (settingsValid)
                    {
                        LoggerService.Log(LogLevelEnum.INFO, "SETTINGS_FETCH_SUCCESS", null);
                        this.isSettingsValid = true;
                        return settings;
                    }
                    else
                    {
                        LoggerService.Log(LogLevelEnum.ERROR, "SETTINGS_SCHEMA_INVALID", null);
                        return null;
                    }
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }
    }
}
