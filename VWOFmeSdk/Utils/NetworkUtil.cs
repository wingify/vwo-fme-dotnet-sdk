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
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Packages.NetworkLayer.Manager;
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Interfaces.Batching;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Packages.Logger.Core;

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
        /// Generates base properties for events with conditional SDK key and account ID handling
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="visitorUserAgent">Visitor user agent string</param>
        /// <param name="ipAddress">Visitor IP address</param>
        /// <param name="isUsageStatsEvent">Whether this is a usage stats event</param>
        /// <param name="usageStatsAccountId">Account ID to use for usage stats events</param>
        /// <returns>Dictionary containing the event base properties</returns>
        public static Dictionary<string, string> GetEventsBaseProperties(string eventName, string visitorUserAgent = "", string ipAddress = "", bool isUsageStatsEvent = false, int? usageStatsAccountId = null)
        {
            string sdkKey = SettingsManager.GetInstance().SdkKey;
            string accountId = isUsageStatsEvent && usageStatsAccountId.HasValue 
                ? usageStatsAccountId.Value.ToString() 
                : SettingsManager.GetInstance().AccountId.ToString();
            
            var requestQueryParams = new RequestQueryParams(
                eventName,
                accountId,
                isUsageStatsEvent ? null : sdkKey, // Only set env if not usage stats event
                visitorUserAgent,
                ipAddress,
                GenerateEventUrl(),
                ConstantsNamespace.Constants.SDK_NAME,
                SDKMetaUtil.GetSdkVersion()
            );
            return requestQueryParams.GetQueryParams();
        }

        /// <summary>
        /// Generates base payload for events with conditional SDK key and visitor handling
        /// </summary>
        /// <param name="settings">Settings object</param>
        /// <param name="userId">User ID</param>
        /// <param name="eventName">Event name</param>
        /// <param name="visitorUserAgent">Visitor user agent string</param>
        /// <param name="ipAddress">Visitor IP address</param>
        /// <param name="isUsageStatsEvent">Whether this is a usage stats event</param>
        /// <param name="usageStatsAccountId">Account ID to use for usage stats events</param>
        /// <returns>EventArchPayload object</returns>
        public static EventArchPayload GetEventBasePayload(Settings settings, string userId, string eventName, string visitorUserAgent = "", string ipAddress = "", bool isUsageStatsEvent = false, int? usageStatsAccountId = null, bool shouldGenerateUUID = true)
        {
            string accountId = isUsageStatsEvent && usageStatsAccountId.HasValue 
                ? usageStatsAccountId.Value.ToString() 
                : SettingsManager.GetInstance().AccountId.ToString();

            string uuid;
            if (shouldGenerateUUID)
            {
                uuid = UUIDUtils.GetUUID(userId.ToString(), accountId.ToString());
            }
            else
            {
                uuid = userId.ToString();
            }
            
            var eventArchData = new EventArchData
            {
                MsgId = GenerateMsgId(uuid),
                VisId = uuid,
                SessionId = GenerateSessionId()
            };
            SetOptionalVisitorData(eventArchData, visitorUserAgent, ipAddress);

            var @event = CreateEvent(eventName, settings, isUsageStatsEvent);
            eventArchData.Event = @event;

            if (!isUsageStatsEvent)
            {
                var visitor = CreateVisitor(settings);
                eventArchData.Visitor = visitor;
            }

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
        private static Event CreateEvent(string eventName, Settings settings, bool isUsageStatsEvent)
        {
            return new Event
            {
                Props = CreateProps(settings, isUsageStatsEvent),
                Name = eventName,
                Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        /// <summary>
        /// Converts the EventArchPayload object to a dictionary
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static Props CreateProps(Settings settings, bool isUsageStatsEvent)
        {
            var props = new Props
            {
                VwoSdkName = ConstantsNamespace.Constants.SDK_NAME,
                VwoSdkVersion = SDKMetaUtil.GetSdkVersion()
            };

            // Only set env key for standard SDK events
            if (!isUsageStatsEvent)
            {
                props.VwoEnvKey = SettingsManager.GetInstance().SdkKey;
            }

            return props;
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
        /// Generates track user payload data with post-segmentation variables and user agent information
        /// </summary>
        /// <param name="settings">The settings object</param>
        /// <param name="eventName">The event name</param>
        /// <param name="campaignId">The campaign ID</param>
        /// <param name="variationId">The variation ID</param>
        /// <param name="context">The VWO context containing user information</param>
        /// <returns>Array containing the track user payload data</returns>
        public static Dictionary<string, object> GetTrackUserPayloadData(Settings settings, string eventName, int campaignId, int variationId, VWOContext context)
        {
            var userId = context.Id;
            var visitorUserAgent = context.UserAgent;
            var ipAddress = context.IpAddress;
            var postSegmentationVariables = context.PostSegmentationVariables;
            var customVariables = context.CustomVariables;

            var properties = GetEventBasePayload(settings, userId, eventName, visitorUserAgent, ipAddress);
            properties.D.Event.Props.Id = campaignId;
            properties.D.Event.Props.Variation = variationId.ToString(CultureInfo.InvariantCulture);
            properties.D.Event.Props.IsFirst = 1;
            
            if(context.VwoSessionId != null)
            {
                properties.D.SessionId = context.VwoSessionId.Value;
            }
            

            // Add post-segmentation variables if they exist in custom variables
            if (postSegmentationVariables != null && customVariables != null)
            {
                foreach (var key in postSegmentationVariables)
                {
                    if (customVariables.ContainsKey(key))
                    {
                        properties.D.Visitor.Props[key] = customVariables[key];
                    }
                }
            }

            // Add IP address as a standard attribute if available
            if (!string.IsNullOrEmpty(ipAddress))
            {
                properties.D.Visitor.Props["ip"] = ipAddress;
            }

            // If userAgent is passed, add os_version and browser_version
            if (!string.IsNullOrEmpty(visitorUserAgent))
            {
                var uaInfo = context.Vwo?.UserAgent;
                if (uaInfo != null && uaInfo.Count > 0)
                {
                    if (uaInfo.ContainsKey("os_version"))
                    {
                        properties.D.Visitor.Props["vwo_osv"] = uaInfo["os_version"];
                    }
                    if (uaInfo.ContainsKey("browser_version"))
                    {
                        properties.D.Visitor.Props["vwo_bv"] = uaInfo["browser_version"];
                    }
                }
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
        public static Dictionary<string, object> GetTrackGoalPayloadData(Settings settings, string userId, string eventName, VWOContext context, Dictionary<string, object> eventProperties, long? sessionId)
        {
            var properties = GetEventBasePayload(settings, userId, eventName, context.UserAgent, context.IpAddress);
            if (sessionId != null)
            {
                properties.D.SessionId = sessionId.Value;
            }
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
       public static Dictionary<string, object> GetAttributePayloadData(Settings settings, string userId, string eventName, Dictionary<string, object> attributes, long? sessionId)
        {
            var properties = GetEventBasePayload(settings, userId, eventName, null, null);
            if (sessionId != null)
            {
                properties.D.SessionId = sessionId.Value;
            }
            properties.D.Event.Props.IsCustomEvent = true;

            foreach (var attribute in attributes)
            {
                properties.D.Visitor.Props[attribute.Key] = attribute.Value;
            }

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
        /// Send the post request to the VWO server using enhanced queue system
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="payload"></param>
        /// <param name="userAgent"></param>
        /// <param name="ipAddress"></param>
        public static void SendPostApiRequest(Dictionary<string, string> properties, Dictionary<string, object> payload, string userAgent, string ipAddress, Dictionary<string, object> campaignInfo = null)
        {
            try
            {
                NetworkManager.GetInstance().AttachClient(null,NetworkManager.GetInstance().GetRetryConfig());
                var headers = CreateHeaders(userAgent, ipAddress);
                var request = new RequestModel(UrlService.GetBaseUrl(), "POST", UrlEnum.EVENTS.GetUrl(), properties, payload, headers, SettingsManager.GetInstance().Protocol, SettingsManager.GetInstance().Port, NetworkManager.GetInstance().GetRetryConfig());

                // Derive event name from query properties (en key)
                string eventName = null;
                if (properties != null && properties.ContainsKey("en"))
                {
                    eventName = properties["en"];
                    request.SetEventName(eventName);
                }

                // Derive uuid from payload (d.visId)
                Dictionary<string, object> dDict = null;

                if (payload != null && payload.TryGetValue("d", out var dObj) && dObj != null)
                {
                    try
                    {
                        var dJson = dObj.ToString();
                        using (var doc = System.Text.Json.JsonDocument.Parse(dJson))
                        {
                            if (doc.RootElement.TryGetProperty("visId", out var visIdElement))
                            {
                                var visId = visIdElement.GetString();
                                if (!string.IsNullOrEmpty(visId))
                                {
                                    request.SetUuid(visId);
                                }
                            }
                        }
                    }
                    catch
                    {
                        LoggerService.Log(LogLevelEnum.DEBUG, "UUID_EXTRACTION_FAILED", new Dictionary<string, string> { { "err", "Failed to extract UUID from payload" } });
                    }

                    // Preserve dDict for later campaignId extraction if possible
                    if (dObj is Dictionary<string, object> dict)
                    {
                        dDict = dict;
                    }
                }

                // Determine apiName and extraDataForMessage similar to Node implementation
                string apiName = null;
                string extraDataForMessage = null;

                if (eventName == EventEnum.VWO_VARIATION_SHOWN.GetValue())
                {
                    apiName = ApiEnum.GET_FLAG.GetValue();

                    if (campaignInfo != null &&
                        campaignInfo.TryGetValue("campaignType", out var campaignTypeObj) &&
                        campaignTypeObj != null &&
                        (campaignTypeObj.ToString() == CampaignTypeEnum.ROLLOUT.GetValue() ||
                         campaignTypeObj.ToString() == CampaignTypeEnum.PERSONALIZE.GetValue()))
                    {
                        extraDataForMessage =
                            "feature: " + (campaignInfo.ContainsKey("featureKey") ? campaignInfo["featureKey"] : null) +
                            ", rule: " + (campaignInfo.ContainsKey("variationName") ? campaignInfo["variationName"] : null);
                    }
                    else
                    {
                        extraDataForMessage =
                            "feature: " + (campaignInfo != null && campaignInfo.ContainsKey("featureKey") ? campaignInfo["featureKey"] : null) +
                            ", rule: " + (campaignInfo != null && campaignInfo.ContainsKey("campaignKey") ? campaignInfo["campaignKey"] : null) +
                            " and variation: " + (campaignInfo != null && campaignInfo.ContainsKey("variationName") ? campaignInfo["variationName"] : null);
                    }

                    // Set campaignId on request if available in payload
                    try
                    {
                        if (dDict != null &&
                            dDict.TryGetValue("event", out var eventObj) && eventObj is Dictionary<string, object> eventDict &&
                            eventDict.TryGetValue("props", out var propsObj) && propsObj is Dictionary<string, object> propsDict &&
                            propsDict.TryGetValue("id", out var idObj) &&
                            int.TryParse(idObj?.ToString(), out var campaignId))
                        {
                            request.SetCampaignId(campaignId.ToString());
                        }
                    }
                    catch
                    {
                        // ignore campaignId extraction failures
                    }
                }
                else if (eventName == EventEnum.VWO_SYNC_VISITOR_PROP.GetValue())
                {
                    apiName = ApiEnum.SET_ATTRIBUTE.GetValue();
                    extraDataForMessage = apiName;
                }
                else if (eventName != EventEnum.VWO_DEBUGGER_EVENT.GetValue() &&
                         eventName != EventEnum.VWO_LOG_EVENT.GetValue() &&
                         eventName != EventEnum.VWO_SDK_INIT_EVENT.GetValue())
                {
                    apiName = ApiEnum.TRACK.GetValue();
                    extraDataForMessage = "event: " + (eventName ?? string.Empty);
                }

                // Perform synchronous POST and build debug event with extraData if retries happened
                var response = NetworkManager.GetInstance().Post(request);

                if (response != null && response.GetTotalAttempts() > 0)
                {
                    var debugEventProps = CreateNetworkAndRetryDebugEvent(
                        response,
                        payload,
                        apiName,
                        extraDataForMessage
                    );
                    debugEventProps["uuid"] = request.GetUuid();
                    DebuggerServiceUtil.SendDebugEventToVWO(debugEventProps);

                    LoggerService.Log(LogLevelEnum.INFO, "NETWORK_CALL_SUCCESS_WITH_RETRIES", new Dictionary<string, string>
                    {
                        { "extraData", $"POST {UrlService.GetBaseUrl() + UrlEnum.EVENTS.GetUrl()}" },
                        { "attempts", response.GetTotalAttempts().ToString() },
                        { "err", FunctionUtil.GetFormattedErrorMessage(response.GetError() as Exception) }
                    });
                }

                // Log error if status code is not 2xx (regardless of retries)
                if (response == null || response.GetStatusCode() < 200 || response.GetStatusCode() > 299)
                {
                    var responseModel = new ResponseModel();
                    responseModel.SetError(new Exception("Network request failed: response is null"));
                    responseModel.SetStatusCode(0);
                    //responseModel.SetTotalAttempts(0);

                    var debugEventProps = CreateNetworkAndRetryDebugEvent(responseModel, payload, apiName, extraDataForMessage);
                    debugEventProps["uuid"] = request.GetUuid();
                    DebuggerServiceUtil.SendDebugEventToVWO(debugEventProps);
                    LogManager.GetInstance().ErrorLog("NETWORK_CALL_FAILED", new Dictionary<string, string> { { "method", "POST" }, { "err", FunctionUtil.GetFormattedErrorMessage(response?.GetError() as Exception) ?? "No response" } }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } }, false);
                }
            }
            catch (Exception exception)
            {
                LogManager.GetInstance().ErrorLog("NETWORK_CALL_FAILED", new Dictionary<string, string> { { "method", "POST" }, { "err", FunctionUtil.GetFormattedErrorMessage(exception) } }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } }, false);
            }
        }
        

        /// <summary>
        /// Sends a batch POST request to the VWO server with the specified payload and account details using enhanced queue system.
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
                    {"env", sdkKey},
                    {"sn", ConstantsNamespace.Constants.SDK_NAME},
                    {"sv", SDKMetaUtil.GetSdkVersion()}
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
                    SettingsManager.GetInstance().Port,
                    NetworkManager.GetInstance().GetRetryConfig()
                );

                // Send the request using the enhanced Post method with queue system
                var response = NetworkManager.GetInstance().Post(requestModel, flushCallback);
                return response?.GetStatusCode() == 200;
            }
            catch (Exception ex)
            {
                LogManager.GetInstance().ErrorLog("BATCH_EVENTS_ERROR", new Dictionary<string, string> { { "err", FunctionUtil.GetFormattedErrorMessage(ex) } }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } });

                // Call flush callback with error
                flushCallback?.OnFlush(ex.Message, payload);
                return false;
            }
        }

        /// <summary>
        /// Generates a payload for the SDK init called event
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="settingsFetchTime"></param>
        /// <param name="sdkInitTime"></param>
        /// <returns></returns>
        public static Dictionary<string, object> GetSdkInitEventPayload(string eventName, int? settingsFetchTime = null, int? sdkInitTime = null)
        {
            // Get user ID and properties
            var userId = SettingsManager.GetInstance().AccountId.ToString() + "_" + SettingsManager.GetInstance().SdkKey;
            var properties = GetEventBasePayload(null, userId, eventName, null, null);

            // Set the required fields as specified
            
            properties.D.Event.Props.VwoEnvKey = SettingsManager.GetInstance().SdkKey;
            properties.D.Event.Props.Product = ConstantsNamespace.Constants.FME;

            // Create the data object
            var data = new Dictionary<string, object>
            {
                { "isSDKInitialized", true },
                { "settingsFetchTime", settingsFetchTime },
                { "sdkInitTime", sdkInitTime }
            };

            properties.D.Event.Props.Data = data;

            return ConvertEventArchPayloadToDictionary(properties);
        }

        /// <summary>
        /// Constructs the payload for SDK usage stats event.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="usageStatsAccountId">The account ID for usage statistics.</param>
        /// <returns>The constructed payload with required fields.</returns>
        public static Dictionary<string, object> GetSDKUsageStatsEventPayload(string eventName, int usageStatsAccountId)
        {
            // Build userId as accountId_sdkKey (not usageStatsAccountId_sdkKey)
            var userId = SettingsManager.GetInstance().AccountId.ToString() + "_" + SettingsManager.GetInstance().SdkKey;

            // Pass usageStatsAccountId as the last argument to GetEventBasePayload, with isUsageStatsEvent = true
            var properties = GetEventBasePayload(
                null,
                userId,
                eventName,
                null,
                null,
                true,
                usageStatsAccountId
            );

            // Set the required fields as specified
            properties.D.Event.Props.Product = ConstantsNamespace.Constants.FME;
            properties.D.Event.Props.VwoMeta = UsageStatsUtil.GetInstance().GetUsageStats();

            return ConvertEventArchPayloadToDictionary(properties);
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
            var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
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

        /// <summary>
        /// Generates a payload for the debugger event
        /// </summary>
        /// <param name="eventProps">The properties for the debugger event</param>
        /// <returns>Dictionary containing the debugger event payload</returns>
        public static Dictionary<string, object> GetDebuggerEventPayload(Dictionary<string, object> eventProps = null)
        {
            if (eventProps == null)
            {
                eventProps = new Dictionary<string, object>();
            }

            var accountId = SettingsManager.GetInstance().AccountId.ToString();
            var sdkKey = SettingsManager.GetInstance().SdkKey;

            // Generate uuid if not present
            string uuid;
            if (!eventProps.ContainsKey("uuid") || eventProps["uuid"] == null || string.IsNullOrEmpty(eventProps["uuid"].ToString()))
            {
                uuid = UUIDUtils.GetUUID(accountId + "_" + sdkKey, accountId);
                eventProps["uuid"] = uuid;
            }
            else
            {
                uuid = eventProps["uuid"].ToString();
            }

            // Create standard event payload; do not generate a new UUID inside
            var properties = GetEventBasePayload(
                null,
                uuid,
                EventEnum.VWO_DEBUGGER_EVENT.GetValue(),
                null,
                null,
                false,
                null,
                false
            );

            // Reset props to ensure a clean debugger payload container
            if (properties.D.Event.Props == null)
            {
                properties.D.Event.Props = new Props();
            }
            if (properties.D.Event.Props.VwoMeta == null)
            {
                properties.D.Event.Props.VwoMeta = new Dictionary<string, object>();
            }

            // Add session id to the event props if not present
            if (eventProps.ContainsKey("sId") && eventProps["sId"] != null)
            {
                if (long.TryParse(eventProps["sId"].ToString(), out var sessionId))
                {
                    properties.D.SessionId = sessionId;
                }
            }
            else
            {
                // Use the generated sessionId from payload
                eventProps["sId"] = properties.D.SessionId;
            }

            // Build vwoMeta object
            var vwoMeta = new Dictionary<string, object>(eventProps)
            {
                ["a"] = SettingsManager.GetInstance().AccountId,
                ["product"] = ConstantsNamespace.Constants.FME,
                ["sn"] = ConstantsNamespace.Constants.SDK_NAME,
                ["sv"] = SDKMetaUtil.GetSdkVersion(),
                ["eventId"] = UUIDUtils.GetRandomUUID(SettingsManager.GetInstance().SdkKey)
            };

            properties.D.Event.Props.VwoMeta = vwoMeta;

            return ConvertEventArchPayloadToDictionary(properties);
        }

        /// <summary>
        /// Creates network and retry debug event properties
        /// </summary>
        /// <param name="response">The response model</param>
        /// <param name="payload">The payload dictionary (can be null)</param>
        /// <param name="apiName">The API name</param>
        /// <param name="extraData">Extra data for the message</param>
        /// <returns>Dictionary containing debug event properties</returns>
        public static Dictionary<string, object> CreateNetworkAndRetryDebugEvent(
            ResponseModel response,
            object payload,
            string apiName,
            string extraData)
        {
            try
            {
                // Set category, if call got success then category is retry, otherwise network
                string category = DebuggerCategoryEnum.RETRY.GetValue();
                string msg_t = ConstantsNamespace.Constants.NETWORK_CALL_SUCCESS_WITH_RETRIES;

                var infoMessages = LoggerService.InfoMessages;
                string infoTemplate = infoMessages.ContainsKey("NETWORK_CALL_SUCCESS_WITH_RETRIES")
                    ? infoMessages["NETWORK_CALL_SUCCESS_WITH_RETRIES"]
                    : "NETWORK_CALL_SUCCESS_WITH_RETRIES";

                string msg = LogMessageUtil.BuildMessage(
                    infoTemplate,
                    new Dictionary<string, string>
                    {
                        { "extraData", extraData ?? string.Empty },
                        { "attempts", response.GetTotalAttempts().ToString() },
                        { "err", FunctionUtil.GetFormattedErrorMessage(response.GetError() as Exception) }
                    }
                );

                string lt = LogLevelEnum.INFO.ToString().ToLower();

                if (response.GetStatusCode() != 200)
                {
                    category = DebuggerCategoryEnum.NETWORK.GetValue();
                    msg_t = ConstantsNamespace.Constants.NETWORK_CALL_FAILURE_AFTER_MAX_RETRIES;

                    var errorMessages = LoggerService.ErrorMessages;
                    string errorTemplate = errorMessages.ContainsKey("NETWORK_CALL_FAILURE_AFTER_MAX_RETRIES")
                        ? errorMessages["NETWORK_CALL_FAILURE_AFTER_MAX_RETRIES"]
                        : "NETWORK_CALL_FAILURE_AFTER_MAX_RETRIES";

                    msg = LogMessageUtil.BuildMessage(
                        errorTemplate,
                        new Dictionary<string, string>
                        {
                            { "extraData", extraData ?? string.Empty },
                            { "attempts", response.GetTotalAttempts().ToString() },
                            { "err", FunctionUtil.GetFormattedErrorMessage(response.GetError() as Exception) }
                        }
                    );
                    lt = LogLevelEnum.ERROR.ToString().ToLower();
                }

                var debugEventProps = new Dictionary<string, object>
                {
                    { "cg", category },
                    { "msg_t", msg_t },
                    { "msg", msg },
                    { "lt", lt }
                };

                if (!string.IsNullOrEmpty(apiName))
                {
                    debugEventProps["an"] = apiName;
                }

                // Extract sessionId from payload.d.sessionId
                if (payload is Dictionary<string, object> payloadDict)
                {
                    if (payloadDict.ContainsKey("d") && payloadDict["d"] is Dictionary<string, object> dObj)
                    {
                        if (dObj.ContainsKey("sessionId"))
                        {
                            debugEventProps["sId"] = dObj["sessionId"];
                        }
                        else
                        {
                            debugEventProps["sId"] = FunctionUtil.GetCurrentUnixTimestamp();
                        }
                    }
                    else
                    {
                        debugEventProps["sId"] = FunctionUtil.GetCurrentUnixTimestamp();
                    }
                }
                else
                {
                    debugEventProps["sId"] = FunctionUtil.GetCurrentUnixTimestamp();
                }

                return debugEventProps;
            }
            catch (Exception err)
            {
                return new Dictionary<string, object>
                {
                    { "cg", DebuggerCategoryEnum.NETWORK.GetValue() },
                    { "an", apiName },
                    { "msg_t", "NETWORK_CALL_FAILED" },
                    { "msg", LogMessageUtil.BuildMessage(
                        LoggerService.ErrorMessages.ContainsKey("NETWORK_CALL_FAILED")
                            ? LoggerService.ErrorMessages["NETWORK_CALL_FAILED"]
                            : "NETWORK_CALL_FAILED",
                        new Dictionary<string, string>
                        {
                            { "method", extraData ?? string.Empty },
                            { "err", FunctionUtil.GetFormattedErrorMessage(err) }
                        })
                    },
                    { "lt", LogLevelEnum.ERROR.ToString().ToLower() },
                    { "sId", FunctionUtil.GetCurrentUnixTimestamp() }
                };
            }
        }

        /// <summary>
        /// Sends an event to the VWO server using enhanced queue system
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="payload"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static object SendEvent(Dictionary<string, string> properties, Dictionary<string, object> payload, string eventName)
        {
            NetworkManager.GetInstance().AttachClient(null, NetworkManager.GetInstance().GetRetryConfig()); // Use enhanced concurrent connections
            var baseUrl = UrlService.GetBaseUrl();
            var port = SettingsManager.GetInstance().Port;
            var protocol = SettingsManager.GetInstance().Protocol;
            var retryConfig = new Dictionary<string, object>(NetworkManager.GetInstance().GetRetryConfig());

            if(eventName == EventEnum.VWO_DEBUGGER_EVENT.GetValue())
            {
                retryConfig[ConstantsNamespace.Constants.RETRY_SHOULD_RETRY] = false;
            }

            if(eventName == EventEnum.VWO_LOG_EVENT.GetValue() || eventName == EventEnum.VWO_USAGE_STATS_EVENT.GetValue() || eventName == EventEnum.VWO_DEBUGGER_EVENT.GetValue())
            {
                baseUrl = ConstantsNamespace.Constants.HOST_NAME;
                protocol = ConstantsNamespace.Constants.HTTPS_PROTOCOL;
                port = 443;
            }

            try
            {
                // Prepare the request model
                var request = new RequestModel(
                    baseUrl,
                    "POST",
                    UrlEnum.EVENTS.GetUrl(),
                    properties,
                    payload,
                    null,
                    protocol,
                    port,
                    retryConfig
                );            

                NetworkManager.GetInstance().PostAsync(request);
                // Assuming the POST request was successful, return a success indicator
                return new { success = true, message = "Event sent successfully" };
            }
            catch (Exception ex)
            {
                // Log error and return false as fallback
                LogManager.GetInstance().ErrorLog("NETWORK_CALL_FAILED", new Dictionary<string, string> { { "method", "POST" }, { "err", FunctionUtil.GetFormattedErrorMessage(ex) } }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } }, false );

                return false;
            }
        }
    }
}