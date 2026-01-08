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
using VWOFmeSdk.Interfaces.Networking;
using VWOFmeSdk.Packages.NetworkLayer.Client;
using VWOFmeSdk.Packages.NetworkLayer.Handlers;
using VWOFmeSdk.Packages.NetworkLayer.Models;
using VWOFmeSdk.Services;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Interfaces.Batching;
using VWOFmeSdk.Utils;
using Newtonsoft.Json;
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Enums;

namespace VWOFmeSdk.Packages.NetworkLayer.Manager
{
    public class NetworkManager
    {
        private static NetworkManager instance;

        private GlobalRequestModel config;
        private NetworkClientInterface client;
        private TaskFactory executorService;
        private Dictionary<string, object> retryConfig = ConstantsNamespace.Constants.DEFAULT_RETRY_CONFIG;

        private NetworkManager()
        {
            this.executorService = new TaskFactory(TaskScheduler.Default);
            this.retryConfig = ConstantsNamespace.Constants.DEFAULT_RETRY_CONFIG;
            this.config = new GlobalRequestModel(null, null, null, null);
        }

        public static NetworkManager GetInstance()
        {
            if (instance == null)
            {
                lock (typeof(NetworkManager))
                {
                    if (instance == null)
                    {
                        instance = new NetworkManager();
                    }
                }
            }
            return instance;
        }

        public void AttachClient(NetworkClientInterface client = null, Dictionary<string, object> retryConfig = null)
        {
            this.config = new GlobalRequestModel(null, null, null, null);
            
            // Normalize retry config from parameter (if any)
            Dictionary<string, object> providedRetry = retryConfig ?? new Dictionary<string, object>();
            this.retryConfig = ValidateRetryConfig(providedRetry);
            
            this.client = client ?? new NetworkClient();
        }

        public void AttachClient()
        {
            AttachClient(null, null);
        }

        public void SetConfig(GlobalRequestModel config)
        {
            this.config = config;
        }

        public GlobalRequestModel GetConfig()
        {
            return this.config;
        }

        public RequestModel CreateRequest(RequestModel request)
        {
            var handler = new RequestHandler();
            return handler.CreateRequest(request, this.config); // Merge and create request
        }

        public ResponseModel Get(RequestModel request)
        {
            try
            {
                var networkOptions = CreateRequest(request);
                if (networkOptions == null)
                {
                    return null;
                }
                else
                {
                    return client.GET(request);
                }
            }
            catch (Exception error)
            {
                return null;
            }
        }

        /// <summary>
        /// Synchronously sends a POST request to the server.
        /// </summary>
        /// <param name="request">The RequestModel containing the URL, headers, and body of the POST request.</param>
        /// <returns></returns>
        public ResponseModel Post(RequestModel request, IFlushInterface flushCallback = null)
        {
            try
            {
                var networkOptions = CreateRequest(request);
                if (networkOptions == null)
                {
                    return null;
                }

                return client.POST(request, flushCallback);
            }
            catch (Exception error)
            {
                if (flushCallback != null)
                {
                    flushCallback.OnFlush($"Error occurred while sending batch events: {error.Message}", null);
                }
                return null;
            }
        }

        /// <summary>
        /// Asynchronously sends a POST request to the server.
        /// </summary>
        /// <param name="request">The RequestModel containing the URL, headers, and body of the POST request.</param>
        public ResponseModel PostAsync(RequestModel request, Dictionary<string, string> properties = null, Dictionary<string, object> campaignInfo = null)
        {
            executorService.StartNew(() => 
            {
                try
                {
                    var response = Post(request);
                    if (response != null && response.GetStatusCode() >= 200 && response.GetStatusCode() <= 299)
                    {
                        if (UsageStatsUtil.GetInstance().GetUsageStats().Count > 0)
                        {
                            UsageStatsUtil.GetInstance().ClearUsageStats();
                        }
                    }
                    return response;
                }
                catch (Exception ex)
                {
                    return null;
                }
            });
            return null;
        }


        private Dictionary<string, object> ValidateRetryConfig(Dictionary<string, object> retryConfig)
        {
            Dictionary<string, object> validatedConfig = new Dictionary<string, object>(ConstantsNamespace.Constants.DEFAULT_RETRY_CONFIG);
            bool isInvalidConfig = false;

            if (retryConfig == null || retryConfig.Count == 0)
            {
                return validatedConfig;
            }

            if (retryConfig.ContainsKey(ConstantsNamespace.Constants.RETRY_SHOULD_RETRY))
            {
                object shouldRetryValue = retryConfig[ConstantsNamespace.Constants.RETRY_SHOULD_RETRY];
                if (shouldRetryValue is bool)
                {
                    validatedConfig[ConstantsNamespace.Constants.RETRY_SHOULD_RETRY] = shouldRetryValue;
                }
                else
                {
                    isInvalidConfig = true;
                }
            }

            if (retryConfig.ContainsKey(ConstantsNamespace.Constants.RETRY_MAX_RETRIES))
            {
                object maxRetriesValue = retryConfig[ConstantsNamespace.Constants.RETRY_MAX_RETRIES];
                if (maxRetriesValue is int maxRetriesInt && maxRetriesInt >= 1)
                {
                    validatedConfig[ConstantsNamespace.Constants.RETRY_MAX_RETRIES] = maxRetriesInt;
                }
                else if (maxRetriesValue is long maxRetriesLong && maxRetriesLong >= 1 && maxRetriesLong <= int.MaxValue)
                {
                    validatedConfig[ConstantsNamespace.Constants.RETRY_MAX_RETRIES] = (int)maxRetriesLong;
                }
                else
                {
                    isInvalidConfig = true;
                }
            }

            if (retryConfig.ContainsKey(ConstantsNamespace.Constants.RETRY_INITIAL_DELAY))
            {
                object initialDelayValue = retryConfig[ConstantsNamespace.Constants.RETRY_INITIAL_DELAY];
                if (initialDelayValue is int initialDelayInt && initialDelayInt >= 1)
                {
                    validatedConfig[ConstantsNamespace.Constants.RETRY_INITIAL_DELAY] = initialDelayInt;
                }
                else if (initialDelayValue is long initialDelayLong && initialDelayLong >= 1 && initialDelayLong <= int.MaxValue)
                {
                    validatedConfig[ConstantsNamespace.Constants.RETRY_INITIAL_DELAY] = (int)initialDelayLong;
                }
                else
                {
                    isInvalidConfig = true;
                }
            }

            if (retryConfig.ContainsKey(ConstantsNamespace.Constants.RETRY_BACKOFF_MULTIPLIER))
            {
                object backoffMultiplierValue = retryConfig[ConstantsNamespace.Constants.RETRY_BACKOFF_MULTIPLIER];
                if (backoffMultiplierValue is int backoffMultiplierInt && backoffMultiplierInt >= 2)
                {
                    validatedConfig[ConstantsNamespace.Constants.RETRY_BACKOFF_MULTIPLIER] = backoffMultiplierInt;
                }
                else if (backoffMultiplierValue is long backoffMultiplierLong && backoffMultiplierLong >= 2 && backoffMultiplierLong <= int.MaxValue)
                {
                    validatedConfig[ConstantsNamespace.Constants.RETRY_BACKOFF_MULTIPLIER] = (int)backoffMultiplierLong;
                }
                else
                {
                    isInvalidConfig = true;
                }
            }

            if (isInvalidConfig)
            {
                LogManager.GetInstance().ErrorLog("INVALID_RETRY_CONFIG", new Dictionary<string, string> { { "retryConfig", JsonConvert.SerializeObject(retryConfig) } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
            }

            return validatedConfig;
        }

        public Dictionary<string, object> GetRetryConfig()
        {
            return retryConfig;
        }
    }
}