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
using System.Globalization;
using System.Linq;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.Request;
using VWOFmeSdk.Models.Request.EventArchQueryParams;
using VWOFmeSdk.Models.Request.Visitor;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Packages.NetworkLayer.Models;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Services;
using Newtonsoft.Json;
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Packages.NetworkLayer.Manager;
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;
using Newtonsoft.Json;
using VWOFmeSdk.Interfaces.Batching;

namespace VWOFmeSdk.Utils
{
    public static class NetworkUtil
    {
        public static Dictionary<string, string> GetSettingsPath(string apiKey, int accountId)
        {
            var settingsQueryParams = new SettingsQueryParams(apiKey, GenerateRandom(), accountId.ToString());
            return settingsQueryParams.GetQueryParams();
        }

        /// <summary>
        /// Generates a random string
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="eventName"></param>
        /// <param name="visitorUserAgent"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetEventsBaseProperties(string eventName, string visitorUserAgent = "", string ipAddress = "")
        {
            string sdkKey = SettingsManager.GetInstance().SdkKey;
            var requestQueryParams = new RequestQueryParams(
                eventName,
                SettingsManager.GetInstance().AccountId.ToString(),
                sdkKey,
                visitorUserAgent,
                ipAddress,
                GenerateEventUrl()
            );
            return requestQueryParams.GetQueryParams();
        }

        /// <summary>
        /// Generates a random string
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="userId"></param>
        /// <param name="eventName"></param>
        /// <param name="visitorUserAgent"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static EventArchPayload GetEventBasePayload(Settings settings, string userId, string eventName, string visitorUserAgent, string ipAddress)
        {
            string uuid = UUIDUtils.GetUUID(userId, SettingsManager.GetInstance().AccountId.ToString());
            var eventArchData = new EventArchData
            {
                MsgId = GenerateMsgId(uuid),
                VisId = uuid,
                SessionId = GenerateSessionId()
            };
            SetOptionalVisitorData(eventArchData, visitorUserAgent, ipAddress);

            var @event = CreateEvent(eventName, settings);
            eventArchData.Event = @event;

            var visitor = CreateVisitor(settings);
            eventArchData.Visitor = visitor;

            var eventArchPayload = new EventArchPayload
            {
                D = eventArchData
            };
            return eventArchPayload;
        }

        /// <summary>
        /// Generates a random string
        /// </summary>
        /// <param name="eventArchData"></param>
        /// <param name="visitorUserAgent"></param>
        /// <param name="ipAddress"></param>
        private static void SetOptionalVisitorData(EventArchData eventArchData, string visitorUserAgent, string ipAddress)
        {
            if (!string.IsNullOrEmpty(visitorUserAgent))
            {
                eventArchData.VisitorUa = visitorUserAgent;
            }

            if (!string.IsNullOrEmpty(ipAddress))
            {
                eventArchData.VisitorIp = ipAddress;
            }
        }

        /// <summary>
        /// Generates a random string
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static Event CreateEvent(string eventName, Settings settings)
        {
            return new Event
            {
                Props = CreateProps(settings),
                Name = eventName,
                Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        /// <summary>
        /// Converts the EventArchPayload object to a dictionary
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static Props CreateProps(Settings settings)
        {
            return new Props
            {
                VwoSdkName = ConstantsNamespace.Constants.SDK_NAME,
                VwoSdkVersion = SDKMetaUtil.GetSdkVersion(),
                VwoEnvKey = SettingsManager.GetInstance().SdkKey
            };
        }

        /// <summary>
        /// Converts the EventArchPayload object to a dictionary
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static Visitor CreateVisitor(Settings settings)
        {
            return new Visitor
            {
                Props = new Dictionary<string, object>
                {
                    { ConstantsNamespace.Constants.VWO_FS_ENVIRONMENT, SettingsManager.GetInstance().SdkKey }
                }
            };
        }

        /// <summary>
        /// Generates a random string
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="userId"></param>
        /// <param name="eventName"></param>
        /// <param name="campaignId"></param>
        /// <param name="variationId"></param>
        /// <param name="visitorUserAgent"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static Dictionary<string, object> GetTrackUserPayloadData(Settings settings, string userId, string eventName, int campaignId, int variationId, string visitorUserAgent, string ipAddress)
        {
            var properties = GetEventBasePayload(settings, userId, eventName, visitorUserAgent, ipAddress);
            properties.D.Event.Props.Id = campaignId;
            properties.D.Event.Props.Variation = variationId.ToString(CultureInfo.InvariantCulture);
            properties.D.Event.Props.IsFirst = 1;
            
            if (UsageStatsUtil.GetInstance().GetUsageStats().Count > 0)
            {
                properties.D.Event.Props.VwoMeta = UsageStatsUtil.GetInstance().GetUsageStats();
            }

            LoggerService.Log(LogLevelEnum.DEBUG, "IMPRESSION_FOR_TRACK_USER", new Dictionary<string, string>
            {
                { "accountId", settings.AccountId.ToString() },
                { "userId", userId },
                { "campaignId", campaignId.ToString() }
            });

            var payload = ConvertEventArchPayloadToDictionary(properties);
            return RemoveNullValues(payload);
        }

        /// <summary>
        /// Generates a random string
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="userId"></param>
        /// <param name="eventName"></param>
        /// <param name="context"></param>
        /// <param name="eventProperties"></param>
        /// <returns></returns>
        public static Dictionary<string, object> GetTrackGoalPayloadData(Settings settings, string userId, string eventName, VWOContext context, Dictionary<string, object> eventProperties)
        {
            var properties = GetEventBasePayload(settings, userId, eventName, context.UserAgent, context.IpAddress);
            properties.D.Event.Props.IsCustomEvent = true;
            AddCustomEventProperties(properties, eventProperties);

            LoggerService.Log(LogLevelEnum.DEBUG, "IMPRESSION_FOR_TRACK_GOAL", new Dictionary<string, string>
            {
                { "eventName", eventName },
                { "accountId", settings.AccountId.ToString() },
                { "userId", userId }
            });

            var payload = ConvertEventArchPayloadToDictionary(properties);
            return RemoveNullValues(payload);
        }

        /// <summary>
        /// Adds custom event properties to the event payload
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="eventProperties"></param>
        private static void AddCustomEventProperties(EventArchPayload properties, Dictionary<string, object> eventProperties)
        {
            if (eventProperties != null)
            {
                properties.D.Event.Props.AdditionalProperties = eventProperties;
            }
        }

        /// <summary>
        /// Generates a payload string
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="userId"></param>
        /// <param name="eventName"></param>
        /// <param name="attributeKey"></param>
        /// <param name="attributeValue"></param>
        /// <returns></returns>
        public static Dictionary<string, object> GetAttributePayloadData(Settings settings, string userId, string eventName, string attributeKey, object attributeValue)
        {
            var properties = GetEventBasePayload(settings, userId, eventName, null, null);
            properties.D.Event.Props.IsCustomEvent = true;
            properties.D.Visitor.Props[attributeKey] = attributeValue;

            LoggerService.Log(LogLevelEnum.DEBUG, "IMPRESSION_FOR_SYNC_VISITOR_PROP", new Dictionary<string, string>
            {
                { "eventName", eventName },
                { "accountId", settings.AccountId.ToString() },
                { "userId", userId }
            });

            var payload = ConvertEventArchPayloadToDictionary(properties);
            return RemoveNullValues(payload);
        }

        /// <summary>
        /// Send the post request to the VWO server
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="payload"></param>
        /// <param name="userAgent"></param>
        /// <param name="ipAddress"></param>
        public static void SendPostApiRequest(Dictionary<string, string> properties, Dictionary<string, object> payload, string userAgent, string ipAddress)
        {
            try
            {
                NetworkManager.GetInstance().AttachClient();
                var headers = CreateHeaders(userAgent, ipAddress);
                var request = new RequestModel(UrlService.GetBaseUrl(), "POST", UrlEnum.EVENTS.GetUrl(), properties, payload, headers, SettingsManager.GetInstance().Protocol, SettingsManager.GetInstance().Port);
                NetworkManager.GetInstance().PostAsync(request);
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "NETWORK_CALL_FAILED", new Dictionary<string, string>
                {
                    { "method", "POST" },
                    { "err", exception.ToString() }
                });
            }
        }

        /// <summary>
        /// Sends a batch POST request to the VWO server with the specified payload and account details.
        /// </summary>
        /// <param name="payload">The payload data to be sent in the request body. This can include event-related information.</param>
        /// <param name="accountId">The account ID to associate with the request, used as a query parameter.</param>
        /// <param name="sdkKey">The API key to authenticate the request in the headers.</param>
        public static bool SendPostBatchRequest(List<Dictionary<string, object>> payload, int accountId, string sdkKey, IFlushInterface flushCallback)
        {
            try
            {
                var batchPayload = new Dictionary<string, object>
                {
                    { "ev", payload }
                };

                var query = new Dictionary<string, string>
                {
                    { "a", accountId.ToString()},
                    {"env", sdkKey}
                };


                // Create the RequestModel with necessary data
                var requestModel = new RequestModel(
                    UrlService.GetBaseUrl(),
                    "POST",
                    UrlEnum.BATCH_EVENTS.GetUrl(),
                    query,
                    batchPayload,
                    new Dictionary<string, string>
                    {
                        { "Authorization", sdkKey },
                        { "Content-Type", "application/json" }
                    },
                    ConstantsNamespace.Constants.HTTPS_PROTOCOL,
                    SettingsManager.GetInstance().Port
                );

                // Send the request using the Post method
                var response = NetworkManager.GetInstance().Post(requestModel, flushCallback);
                return response?.GetStatusCode() == 200;
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogLevelEnum.ERROR, $"Error occurred while sending batch events: {ex.Message}");

                // Call flush callback with error
                flushCallback?.OnFlush(ex.Message, payload);
                return false;
            }
        }


        /// <summary>
        /// Removes all the null values from the dictionary
        /// </summary>
        /// <param name="originalMap"></param>
        /// <returns></returns>
        public static Dictionary<string, object> RemoveNullValues(Dictionary<string, object> originalMap)
        {
            var cleanedMap = new Dictionary<string, object>();

            foreach (var entry in originalMap)
            {
                var value = entry.Value;
                if (value is Dictionary<string, object> valueMap)
                {
                    value = RemoveNullValues(valueMap);
                }
                if (value != null)
                {
                    cleanedMap[entry.Key] = value;
                }
            }

            return cleanedMap;
        }

        /// <summary>
        /// Converts the EventArchPayload object to a dictionary
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        private static Dictionary<string, object> ConvertEventArchPayloadToDictionary(EventArchPayload payload)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(payload));
        }

        private static string GenerateRandom()
        {
            return new Random().NextDouble().ToString(CultureInfo.InvariantCulture);
        }

        private static string GenerateEventUrl()
        {
            return ConstantsNamespace.Constants.HTTPS_PROTOCOL + UrlService.GetBaseUrl() + UrlEnum.EVENTS.GetUrl();
        }

        private static string GenerateMsgId(string uuid)
        {
            return uuid + "-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static long GenerateSessionId()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private static Dictionary<string, string> CreateHeaders(string userAgent, string ipAddress)
        {
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(userAgent)) headers[HeadersEnum.USER_AGENT.GetHeader()] = userAgent;
            if (!string.IsNullOrEmpty(ipAddress)) headers[HeadersEnum.IP.GetHeader()] = ipAddress;
            return headers;
        }

        public static Dictionary<string, object> GetMessagingEventPayload(string messageType, string message, string eventName)
        {
            var userId = $"{SettingsManager.GetInstance().AccountId}_{SettingsManager.GetInstance().SdkKey}";
            var properties = GetEventBasePayload(null, userId, eventName, null, null);

            properties.D.Event.Props.VwoEnvKey = SettingsManager.GetInstance().SdkKey;
            properties.D.Event.Props.Product = "fme"; // Assuming 'product' is a required field

            // Set the message data
            var data = new
            {
                type = messageType,
                content = new
                {
                    title = message,
                    dateTime = FunctionUtil.GetCurrentUnixTimestampInMillis()
                }
            };

            properties.D.Event.Props.Data = data; // Assign data to "data" property

            return ConvertEventArchPayloadToDictionary(properties);
        }

        public static object SendMessagingEvent(Dictionary<string, string> properties, Dictionary<string, object> payload)
        {
            NetworkManager.GetInstance().AttachClient();

            try
            {
                // Prepare the request model
                var request = new RequestModel(
                    ConstantsNamespace.Constants.HOST_NAME,
                    "POST",
                    UrlEnum.EVENTS.GetUrl(),
                    properties,
                    payload,
                    null,
                    ConstantsNamespace.Constants.HTTPS_PROTOCOL,
                    443
                );            

                NetworkManager.GetInstance().PostAsync(request);
                // Assuming the POST request was successful, return a success indicator
                return new { success = true, message = "Event sent successfully" };
            }
            catch (Exception ex)
            {
                // Log error and return false as fallback
                LoggerService.Log(LogLevelEnum.ERROR, "NETWORK_CALL_FAILED", new Dictionary<string, string>
                {
                    { "method", "POST" },
                    { "err", ex.ToString() }
                });

                return false;
            }
        }
    }
}