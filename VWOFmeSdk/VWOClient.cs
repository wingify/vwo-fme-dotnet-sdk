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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VWOFmeSdk.Api;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Models.Schemas;
using System.Linq;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Enums;

namespace VWOFmeSdk
{
    public class VWOClient
    {
        private static VWOClient vwoClientInstance;
        private Settings processedSettings;
        public string Settings { get; private set; }
        private VWOInitOptions options;
        public static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore
        };
        private BatchEventQueue batchEventQueue;
        private VWOBuilder vwoBuilder;
        
        public BatchEventQueue BatchEventQueue
        {
            get { return batchEventQueue; }
            set { batchEventQueue = value; }
        }

        /// <summary>
        /// Constructor to initialize the VWOClient
        /// </summary>
        /// <param name="settings">Settings JSON string</param>
        /// <param name="options">VWOInitOptions object</param>
        /// <returns> VWOClient object</returns>
        public VWOClient(string settings, VWOInitOptions options, VWOBuilder vwoBuilder)
        {
            try
            {
                this.options = options;
                this.vwoBuilder = vwoBuilder;
                if (settings == null)
                {
                    return;
                }
                this.Settings = settings;
                this.processedSettings = JsonConvert.DeserializeObject<Settings>(settings, JsonSerializerSettings);
                SettingsUtil.ProcessSettings(this.processedSettings);
                // Init URL version with collection prefix
                UrlService.Init(this.processedSettings.CollectionPrefix);
                // Init SDKMetaUtil and set SDK version
                LoggerService.Log(LogLevelEnum.INFO, "CLIENT_INITIALIZED", null);

                vwoClientInstance = this;
            }
            catch (Exception exception)
            {
                LogManager.GetInstance().ErrorLog("ERROR_PARSING_SETTINGS", new Dictionary<string, string> { { "err", FunctionUtil.GetFormattedErrorMessage(exception) } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
            }
        }

        /// <summary>
        /// This method is used to update the settings
        /// </summary>
        /// <param name="newSettings">New settings to be updated</param>
        /// <returns>void</returns>
        public void UpdateSettings(string newSettings)
        {
            try
            {
                this.processedSettings = JsonConvert.DeserializeObject<Settings>(newSettings, JsonSerializerSettings);
                SettingsUtil.ProcessSettings(this.processedSettings);
                LoggerService.Log(LogLevelEnum.INFO, "SETTINGS_UPDATED_SUCCESSFULLY", null);
            }
            catch (Exception exception)
            {
                LogManager.GetInstance().ErrorLog("ERROR_UPDATING_SETTINGS", new Dictionary<string, string> { { "err", FunctionUtil.GetFormattedErrorMessage(exception) } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } }, false);
            }
        }

        /// <summary>
        /// This method is used to update the settings.
        /// </summary>
        /// <param name="isViaWebhook">Boolean value to indicate if the settings are being fetched via webhook</param>
        /// <returns>Updated settings in JSON string format</returns>
        public string UpdateSettings(bool isViaWebhook)
        {
            string apiName = "updateSettings";
            try
            {
                // Fetch the new settings from the server based on the webhook flag
                this.Settings = SettingsManager.GetInstance().FetchSettings(isViaWebhook);
                UpdateSettings(this.Settings);  // Call the method that takes a settings string
                return this.Settings;
            }
            catch (Exception exception)
            {
                LogManager.GetInstance().ErrorLog("SETTINGS_FETCH_FAILED", new Dictionary<string, string> { { "apiName", apiName }, { "isViaWebhook", isViaWebhook.ToString() }, { "err", FunctionUtil.GetFormattedErrorMessage(exception) } }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                return null;
            }
        }

        /// <summary>
        /// Static method to get the VWOClient instance
        /// </summary>
        /// <returns>VWOClient instance</returns>
        public static VWOClient GetInstance()
        {
            return vwoClientInstance;
        }

        /// <summary>
        /// This method is used to get the flag value for the given feature key
        /// </summary>
        /// <param name="featureKey">Feature key for which the flag value is to be fetched</param>
        /// <param name="context">User context</param>
        /// <returns>GetFlag object containing the flag values</returns>
        public GetFlag GetFlag(string featureKey, VWOContext context)
        {
            string apiName = "getFlag";
            var getFlag = new GetFlag();
            try
            {
                LoggerService.Log(LogLevelEnum.DEBUG, "API_CALLED", new Dictionary<string, string> { { "apiName", apiName } });
                var hooksManager = new HooksManager(this.options.Integrations);
                if (context == null || string.IsNullOrEmpty(context.Id))
                {
                    getFlag.SetIsEnabled(false);
                    throw new ArgumentException("User ID is required");
                }

                if (string.IsNullOrEmpty(featureKey))
                {
                    getFlag.SetIsEnabled(false);
                    throw new ArgumentException("Feature Key is required");
                }

                if (this.processedSettings == null || !new SettingsSchema().IsSettingsValid(this.processedSettings))
                {
                    LogManager.GetInstance().ErrorLog("SETTINGS_SCHEMA_INVALID", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } }, false);
                    getFlag.SetIsEnabled(false);
                    return getFlag;
                }

                return GetFlagAPI.GetFlag(featureKey, this.processedSettings, context, hooksManager);
            }
            catch (Exception exception)
            {
                LogManager.GetInstance().ErrorLog("API_THROW_ERROR", new Dictionary<string, string> { { "apiName", apiName }, { "err", FunctionUtil.GetFormattedErrorMessage(exception) } }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } });
                getFlag.SetIsEnabled(false);
                return getFlag;
            }
        }

        /// <summary>
        /// This method is used to track the event
        /// </summary>
        /// <param name="eventName">Event name to be tracked</param>
        /// <param name="context">User context</param>
        /// <param name="eventProperties">Event properties to be sent for the event</param>
        /// <returns>Map containing the event name and its status</returns>
        private Dictionary<string, bool> Track(string eventName, VWOContext context, Dictionary<string, object> eventProperties)
        {
            string apiName = "trackEvent";
            var resultMap = new Dictionary<string, bool>();
            try
            {
                LoggerService.Log(LogLevelEnum.DEBUG, "API_CALLED", new Dictionary<string, string> { { "apiName", apiName } });
                var hooksManager = new HooksManager(this.options.Integrations);
                if (!DataTypeUtil.IsString(eventName))
                {
                    LogManager.GetInstance().ErrorLog("API_INVALID_PARAM", new Dictionary<string, string> { { "apiName", apiName }, { "key", "eventName" }, { "type", DataTypeUtil.GetType(eventName) }, { "correctType", "String" } }, new Dictionary<string, object> { { "an", ApiEnum.TRACK.GetValue() } }, false);
                    throw new ArgumentException("TypeError: Event-name should be a string");
                }

                if (context == null || string.IsNullOrEmpty(context.Id))
                {
                    LogManager.GetInstance().ErrorLog("API_CONTEXT_INVALID", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.TRACK.GetValue() } }, false);
                    throw new ArgumentException("User ID is required");
                }

                if (this.processedSettings == null || !new SettingsSchema().IsSettingsValid(this.processedSettings))
                {
                    LogManager.GetInstance().ErrorLog("SETTINGS_SCHEMA_INVALID", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.TRACK.GetValue() } }, false);
                    resultMap[eventName] = false;
                    return resultMap;
                }

                bool result = TrackEventAPI.Track(this.processedSettings, eventName, context, eventProperties, hooksManager);
                resultMap[eventName] = result;
                return resultMap;
            }
            catch (Exception exception)
            {
                LogManager.GetInstance().ErrorLog("API_THROW_ERROR", new Dictionary<string, string> { { "apiName", apiName }, { "err", FunctionUtil.GetFormattedErrorMessage(exception) } }, new Dictionary<string, object> { { "an", ApiEnum.TRACK.GetValue() } });
                resultMap[eventName] = false;
                return resultMap;
            }
        }

        /// <summary>
        /// Overloaded function if event properties need to be passed
        /// calls track method to track the event
        /// </summary>
        /// <param name="eventName">Event name to be tracked</param>
        /// <param name="context">User context</param>
        /// <param name="eventProperties">Event properties to be sent for the event</param>
        /// <returns>Map containing the event name and its status</returns>
        public Dictionary<string, bool> TrackEvent(string eventName, VWOContext context, Dictionary<string, object> eventProperties)
        {
            return Track(eventName, context, eventProperties);
        }

        /// <summary>
        /// Overloaded function when no event properties are required
        /// </summary>
        /// <param name="eventName">Event name to be tracked</param>
        /// <param name="context">User context</param>
        /// <returns>Map containing the event name and its status</returns>
        public Dictionary<string, bool> TrackEvent(string eventName, VWOContext context)
        {
            return Track(eventName, context, new Dictionary<string, object>());
        }

        /// <summary>
        /// Sets an attribute for a user in the context provided.
        /// This method validates the types of the inputs before proceeding with the API call.
        /// </summary>
        /// <param name="attributeKey">The key of the attribute to set</param>
        /// <param name="attributeValue">The value of the attribute to set</param>
        /// <param name="context">User context</param>
        public void SetAttribute(string attributeKey, string attributeValue, VWOContext context = null)
        {
            string apiName = "setAttribute";
            try
            {
                LoggerService.Log(LogLevelEnum.DEBUG, "API_CALLED", new Dictionary<string, string> { { "apiName", apiName } });
                if (!DataTypeUtil.IsString(attributeKey))
                {
                    LogManager.GetInstance().ErrorLog("API_INVALID_PARAM", new Dictionary<string, string> { { "apiName", apiName }, { "key", "attributeKey" }, { "type", DataTypeUtil.GetType(attributeKey) }, { "correctType", "String" } }, new Dictionary<string, object> { { "an", ApiEnum.SET_ATTRIBUTE.GetValue() } }, false);
                    throw new ArgumentException("TypeError: attributeKey should be a string");
                }

                if (!DataTypeUtil.IsString(attributeValue))
                {
                    LogManager.GetInstance().ErrorLog("API_INVALID_PARAM", new Dictionary<string, string> { { "apiName", apiName }, { "key", "attributeValue" }, { "type", DataTypeUtil.GetType(attributeValue) }, { "correctType", "String" } }, new Dictionary<string, object> { { "an", ApiEnum.SET_ATTRIBUTE.GetValue() } }, false);
                    throw new ArgumentException("TypeError: attributeValue should be a string");
                }

                if (context == null || string.IsNullOrEmpty(context.Id))
                {
                    LogManager.GetInstance().ErrorLog("API_CONTEXT_INVALID", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.SET_ATTRIBUTE.GetValue() } }, false);
                    throw new ArgumentException("Invalid Context");
                }

                if (this.processedSettings == null || !new SettingsSchema().IsSettingsValid(this.processedSettings))
                {
                    LogManager.GetInstance().ErrorLog("SETTINGS_SCHEMA_INVALID", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.SET_ATTRIBUTE.GetValue() } }, false);
                    return;
                }

                SetAttributeAPI.SetAttribute(this.processedSettings, attributeKey, attributeValue, context);
            }
            catch (Exception exception)
            {
                LogManager.GetInstance().ErrorLog("API_THROW_ERROR", new Dictionary<string, string> { { "apiName", apiName }, { "err", FunctionUtil.GetFormattedErrorMessage(exception) } }, new Dictionary<string, object> { { "an", ApiEnum.SET_ATTRIBUTE.GetValue() } });
            }
        }

        /// <summary>
        /// Sets multiple attributes for a user in the context provided.
        /// This method validates the types of the inputs before proceeding with the API call.
        /// </summary>
        /// <param name="attributes">A dictionary of attributes</param>
        /// <param name="context">User context</param>
        public void SetAttribute(Dictionary<string, dynamic> attributes, VWOContext context)
        {
            string apiName = "setAttribute";
            try
            {
                LoggerService.Log(LogLevelEnum.DEBUG, "API_CALLED", new Dictionary<string, string> { { "apiName", apiName } });

                // Validate input parameters
                if (attributes == null || attributes.Count == 0)
                {
                    LogManager.GetInstance().ErrorLog("API_INVALID_PARAM", new Dictionary<string, string> { { "apiName", apiName }, { "key", "attributes" }, { "type", "a null or empty attributes dictionary" }, { "correctType", "a non-empty dictionary" } }, new Dictionary<string, object> { { "an", ApiEnum.SET_ATTRIBUTE.GetValue() } }, false);
                }


                // Iterate over each attribute and validate its type
                foreach (var attribute in attributes)
                {
                    string key = attribute.Key;
                    var value = attribute.Value;

                    // Allow only primitive types: bool, string, int, float, double
                    if (!(value is bool || value is string || value is int || value is float || value is double))
                    {
                        LogManager.GetInstance().ErrorLog("API_INVALID_PARAM", new Dictionary<string, string> { { "apiName", apiName }, { "key", key }, { "type", value?.GetType().ToString() ?? "null" }, { "correctType", "bool, string, int, float, double" } }, new Dictionary<string, object> { { "an", ApiEnum.SET_ATTRIBUTE.GetValue() } }, false);

                        throw new ArgumentException($"Invalid attribute type for key \"{key}\". Expected bool, string, int, float, or double, but got {value?.GetType()}");
                    }

                    // Reject arrays and complex objects explicitly
                    if (value is Array || (value is object && !(value is string || value is bool || value is int || value is float || value is double)))
                    {
                        LogManager.GetInstance().ErrorLog("API_INVALID_PARAM", new Dictionary<string, string> { { "apiName", apiName }, { "key", key }, { "type", value?.GetType().ToString() ?? "null" }, { "correctType", "bool, string, int, float, double" } }, new Dictionary<string, object> { { "an", ApiEnum.SET_ATTRIBUTE.GetValue() } }, false);

                        throw new ArgumentException($"Invalid attribute value for key \"{key}\". Arrays and complex objects are not supported.");
                    }
                }

                if (context == null || string.IsNullOrEmpty(context.Id))
                {
                    LogManager.GetInstance().ErrorLog("API_CONTEXT_INVALID", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.SET_ATTRIBUTE.GetValue() } }, false);
                    throw new ArgumentException("Invalid Context");
                }

                if (this.processedSettings == null || !new SettingsSchema().IsSettingsValid(this.processedSettings))
                {
                    LogManager.GetInstance().ErrorLog("SETTINGS_SCHEMA_INVALID", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.SET_ATTRIBUTE.GetValue() } }, false);
                    return;
                }

                SetAttributeAPI.SetAttribute(this.processedSettings, attributes, context);
            }
            catch (Exception exception)
            {
                LogManager.GetInstance().ErrorLog("API_THROW_ERROR", new Dictionary<string, string> { { "apiName", apiName }, { "err", FunctionUtil.GetFormattedErrorMessage(exception) } }, new Dictionary<string, object> { { "an", ApiEnum.SET_ATTRIBUTE.GetValue() } });
            }
        }

        /// <summary>
        /// Stops SDK background activities (e.g. polling and batching).
        /// </summary>
        public void Shutdown()
        {
            try {
                LoggerService.Log(LogLevelEnum.DEBUG, "API_CALLED", new Dictionary<string, string> { { "apiName", ApiEnum.SHUTDOWN.GetValue() } });
                this.vwoBuilder?.StopPolling();

                //if batching enabled, call the flushEvents method
                if (this.vwoBuilder.IsBatchingUsed)
                {
                    this.batchEventQueue.FlushAndClearTimer(shouldWaitForFlush: true);
                    LoggerService.Log(LogLevelEnum.INFO, "Successfully flushed BatchQueue, removed timers and cleared polling");
                    return;
                }
                LoggerService.Log(LogLevelEnum.INFO, "Successfully cleared polling");
            } 
            catch (Exception exception)
            {
                LogManager.GetInstance().ErrorLog("API_THROW_ERROR", new Dictionary<string, string> { { "apiName", ApiEnum.SHUTDOWN.GetValue() }, { "err", FunctionUtil.GetFormattedErrorMessage(exception) } }, new Dictionary<string, object> { { "an", ApiEnum.SHUTDOWN.GetValue() } });
            }
        }

        /// <summary>
        /// Flushes the events manually from the batch events queue
        /// </summary>
        /// <returns>True if flush was successful, false otherwise</returns>
        public bool FlushEvents()
        {
            string apiName = "FlushEvents";
            try
            {
                LoggerService.Log(LogLevelEnum.DEBUG, "API_CALLED", new Dictionary<string, string> { { "apiName", apiName } });
                
                if (this.batchEventQueue != null)
                {
                    return this.batchEventQueue.FlushAndClearTimer();
                }
                else
                {
                    LogManager.GetInstance().ErrorLog("BATCHING_NOT_ENABLED", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.FLUSH_EVENTS.GetValue() } });
                    return false;
                }
            }
            catch (Exception exception)
            {
                LogManager.GetInstance().ErrorLog("API_THROW_ERROR", new Dictionary<string, string> { { "apiName", apiName }, { "err", FunctionUtil.GetFormattedErrorMessage(exception) } }, new Dictionary<string, object> { { "an", ApiEnum.FLUSH_EVENTS.GetValue() } });
                return false;
            }
        }
    }

    public class ObjectMapper
    {
        public Dictionary<string, object> ConvertValue<T>(T value)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(value));
        }
    }
}
