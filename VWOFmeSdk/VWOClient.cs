#pragma warning disable 1587
/**
 * Copyright 2024 Wingify Software Pvt. Ltd.
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

namespace VWOFmeSdk
{
    public class VWOClient
    {
        private Settings processedSettings;
        public string Settings { get; private set; }
        private VWOInitOptions options;
        public static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public static ObjectMapper ObjectMapper { get; } = new ObjectMapper();

        /// <summary>
        /// Constructor to initialize the VWOClient
        /// </summary>
        /// <param name="settings">Settings JSON string</param>
        /// <param name="options">VWOInitOptions object</param>
        /// <returns> VWOClient object</returns>
        public VWOClient(string settings, VWOInitOptions options)
        {
            try
            {
                this.options = options;
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
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "Exception occurred while parsing settings: " + exception.Message);
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
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "Exception occurred while updating settings: " + exception.Message);
            }
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
                    LoggerService.Log(LogLevelEnum.ERROR, "SETTINGS_SCHEMA_INVALID", null);
                    getFlag.SetIsEnabled(false);
                    return getFlag;
                }

                return GetFlagAPI.GetFlag(featureKey, this.processedSettings, context, hooksManager);
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "API_THROW_ERROR", new Dictionary<string, string>
                {
                    { "apiName", apiName },
                    { "err", exception.ToString() }
                });
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
                    LoggerService.Log(LogLevelEnum.ERROR, "API_INVALID_PARAM", new Dictionary<string, string>
                    {
                        { "apiName", apiName },
                        { "key", "eventName" },
                        { "type", DataTypeUtil.GetType(eventName) },
                        { "correctType", "String" }
                    });
                    throw new ArgumentException("TypeError: Event-name should be a string");
                }

                if (context == null || string.IsNullOrEmpty(context.Id))
                {
                    throw new ArgumentException("User ID is required");
                }

                if (this.processedSettings == null || !new SettingsSchema().IsSettingsValid(this.processedSettings))
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "SETTINGS_SCHEMA_INVALID", null);
                    resultMap[eventName] = false;
                    return resultMap;
                }

                bool result = TrackEventAPI.Track(this.processedSettings, eventName, context, eventProperties, hooksManager);
                resultMap[eventName] = result;
                return resultMap;
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "API_THROW_ERROR", new Dictionary<string, string>
                {
                    { "apiName", apiName },
                    { "err", exception.ToString() }
                });
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
        /// Overloaded function for no event properties
        /// calls track method to track the event
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
        public void SetAttribute(string attributeKey, string attributeValue, VWOContext context)
        {
            string apiName = "setAttribute";
            try
            {
                LoggerService.Log(LogLevelEnum.DEBUG, "API_CALLED", new Dictionary<string, string> { { "apiName", apiName } });
                if (!DataTypeUtil.IsString(attributeKey))
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "API_INVALID_PARAM", new Dictionary<string, string>
                    {
                        { "apiName", apiName },
                        { "key", "attributeKey" },
                        { "type", DataTypeUtil.GetType(attributeKey) },
                        { "correctType", "String" }
                    });
                    throw new ArgumentException("TypeError: attributeKey should be a string");
                }

                if (!DataTypeUtil.IsString(attributeValue))
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "API_INVALID_PARAM", new Dictionary<string, string>
                    {
                        { "apiName", apiName },
                        { "key", "attributeValue" },
                        { "type", DataTypeUtil.GetType(attributeValue) },
                        { "correctType", "String" }
                    });
                    throw new ArgumentException("TypeError: attributeValue should be a string");
                }

                if (context == null || string.IsNullOrEmpty(context.Id))
                {
                    throw new ArgumentException("User ID is required");
                }

                if (this.processedSettings == null || !new SettingsSchema().IsSettingsValid(this.processedSettings))
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "SETTINGS_SCHEMA_INVALID", null);
                    return;
                }

                SetAttributeAPI.SetAttribute(this.processedSettings, attributeKey, attributeValue, context);
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "API_THROW_ERROR", new Dictionary<string, string>
                {
                    { "apiName", apiName },
                    { "err", exception.ToString() }
                });
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