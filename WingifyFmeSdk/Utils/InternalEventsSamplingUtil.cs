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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WingifyFmeSdk.Constants;

namespace WingifyFmeSdk.Utils
{
    /// <summary>
    /// Utility methods for reading internal-event sampling configuration from settings.
    /// The .NET SDK always uses the <c>server</c> runtime key.
    /// </summary>
    public static class InternalEventsSamplingUtil
    {
        /// <summary>
        /// Parses the raw settings JSON into a dictionary for sampling lookups.
        /// </summary>
        /// <param name="settingsJson">The original settings document as JSON.</param>
        /// <returns>A settings dictionary, or an empty dictionary when input is invalid.</returns>
        public static Dictionary<string, object> ParseSettingsDocument(string settingsJson)
        {
            // Return an empty dictionary if the input JSON string is null or empty
            if (string.IsNullOrEmpty(settingsJson))
            {
                return new Dictionary<string, object>();
            }

            return JsonConvert.DeserializeObject<Dictionary<string, object>>(settingsJson)
                ?? new Dictionary<string, object>();
        }


        /// <summary>
        /// Reads <c>usageStatsAccountId</c> from the settings document.
        /// </summary>
        /// <param name="settings">The parsed settings document.</param>
        /// <returns>The usage-stats account ID when present; otherwise <c>null</c>.</returns>
        public static long? GetUsageStatsAccountId(Dictionary<string, object> settings)
        {
            // If the settings object is null or doesn't contain a valid usageStatsAccountId, return null
            if (settings == null || !settings.ContainsKey("usageStatsAccountId") || settings["usageStatsAccountId"] == null)
            {
                return null;
            }

            // Safely convert the extracted account ID object into a 64-bit integer
            return Convert.ToInt64(settings["usageStatsAccountId"]);
        }

        /// <summary>
        /// Indicates whether sampling must be evaluated for the server runtime.
        /// Defaults to <c>false</c> when the flag is absent.
        /// </summary>
        /// <param name="settings">The parsed settings document.</param>
        /// <returns><c>true</c> when sampling rules should be applied before sending.</returns>
        public static bool ShouldApplyEventSampling(Dictionary<string, object> settings)
        {
            // Extract the 'internalEventsAlwaysApplySampling' configuration block from settings
            JObject alwaysApplyConfig = GetNestedObject(settings, Constants.Constants.INTERNAL_EVENTS_ALWAYS_APPLY_SAMPLING_KEY);
            
            // Check if the block exists, contains a runtime flag for the current platform, and if it's a valid boolean
            if (alwaysApplyConfig != null
                && alwaysApplyConfig.TryGetValue(Constants.Constants.PLATFORM, out JToken runtimeFlag)
                && runtimeFlag.Type == JTokenType.Boolean)
            {
                return runtimeFlag.Value<bool>();
            }

            // Fallback to the default sampling behavior if the flag is missing or invalid
            return Constants.Constants.INTERNAL_EVENTS_DEFAULT_ALWAYS_APPLY_SAMPLING;
        }

        /// <summary>
        /// Reads the usage-stats sampling percentage for the server runtime.
        /// </summary>
        /// <param name="settings">The parsed settings document.</param>
        /// <returns>The configured sampling percentage (0–100).</returns>
        public static int GetUsageStatsSamplingPercent(Dictionary<string, object> settings)
        {
            // Delegate to the common method to fetch the usage-stats sampling percentage
            return GetServerSamplingPercent(settings, Constants.Constants.INTERNAL_EVENTS_USAGE_SAMPLING_KEY);
        }

        /// <summary>
        /// Reads the debug-event sampling percentage for the server runtime.
        /// </summary>
        /// <param name="settings">The parsed settings document.</param>
        /// <returns>The configured sampling percentage (0–100).</returns>
        public static int GetDebugEventSamplingPercent(Dictionary<string, object> settings)
        {
            return GetServerSamplingPercent(settings, Constants.Constants.INTERNAL_EVENTS_DEBUG_SAMPLING_KEY);
        }

        /// <summary>
        /// Evaluates whether an event qualifies under the configured sampling percentage.
        /// </summary>
        /// <param name="samplingPercent">The configured sampling percentage (0–100).</param>
        /// <param name="randomValue">A random value in the range [0, 1). Defaults to a new random draw.</param>
        /// <returns><c>true</c> when the generated value falls within the sampling threshold.</returns>
        public static bool PassesSamplingPercent(int samplingPercent, double? randomValue = null)
        {
            double random = randomValue ?? new Random().NextDouble();
            // Scale the random value to a range of 0-100 to compare against the sampling percentage
            int normalizedRandomPercent = (int)Math.Floor(random * 101);
            // The event passes if the random threshold is less than or equal to the configured limit
            return normalizedRandomPercent <= samplingPercent;
        }

        /// <summary>
        /// Indicates whether a debug error template key is categorized as sampled.
        /// </summary>
        /// <param name="messageTemplateKey">The error log template key (<c>msg_t</c>).</param>
        /// <returns><c>true</c> when the key is in the sampled debug set.</returns>
        public static bool IsSampledDebugErrorTemplateKey(string messageTemplateKey)
        {
            // Ensure the key is valid and exists within the predefined list of sampled keys
            return !string.IsNullOrEmpty(messageTemplateKey)
                && SampledDebugErrorTemplateKeys.SampledKeys.Contains(messageTemplateKey);
        }

        /// <summary>
        /// Normalizes a sampling percentage to the inclusive range [0, 100].
        /// </summary>
        /// <param name="samplingValue">The raw value from settings.</param>
        /// <returns>A valid sampling percentage, or the server default when invalid.</returns>
        public static int NormalizeSamplingPercent(object samplingValue)
        {
            if (samplingValue != null && double.TryParse(samplingValue.ToString(), out double parsedValue)
                && parsedValue >= 0 && parsedValue <= 100)
            {
                return (int)parsedValue;
            }

            // If the value is invalid or missing, fallback to the default server sampling percentage
            return Constants.Constants.INTERNAL_EVENTS_DEFAULT_SERVER_SAMPLING_PERCENT;
        }

        /// <summary>
        /// Retrieves the sampling percentage for a specific category (e.g., usage stats, debug) from settings.
        /// </summary>
        /// <param name="settings">The parsed settings document.</param>
        /// <param name="samplingCategoryKey">The category key to lookup within the sampling configuration.</param>
        /// <returns>The extracted sampling percentage.</returns>
        private static int GetServerSamplingPercent(Dictionary<string, object> settings, string samplingCategoryKey)
        {
            JObject samplingConfig = GetNestedObject(settings, Constants.Constants.INTERNAL_EVENTS_SAMPLING_KEY);
            JObject categoryConfig = samplingConfig?[samplingCategoryKey] as JObject;
            // Extract the platform-specific (server) sampling value
            object serverValue = categoryConfig?[Constants.Constants.PLATFORM]?.ToObject<object>();
            // Normalize and return the final percentage
            return NormalizeSamplingPercent(serverValue);
        }

        /// <summary>
        /// Safely extracts a nested JSON object from the settings dictionary by its key.
        /// </summary>
        /// <param name="settings">The parsed settings document.</param>
        /// <param name="key">The key identifying the nested object to retrieve.</param>
        /// <returns>A JObject representation of the nested object, or null if it cannot be found or parsed.</returns>
        private static JObject GetNestedObject(Dictionary<string, object> settings, string key)
        {
            // Return null if the settings dictionary is missing or doesn't contain the requested key
            if (settings == null || !settings.ContainsKey(key) || settings[key] == null)
            {
                return null;
            }
            // If the value is already a JObject, return it directly
            if (settings[key] is JObject jObject)
            {
                return jObject;
            }

            // If the value is a standard dictionary, convert it into a JObject
            if (settings[key] is Dictionary<string, object> dictionaryValue)
            {
                return JObject.FromObject(dictionaryValue);
            }

            try
            {
                // As a fallback, try parsing the string representation of the value into a JObject
                return JObject.Parse(settings[key].ToString());
            }
            catch (JsonReaderException)
            {
                // Suppress parsing errors and return null to gracefully handle malformed structures
                return null;
            }
        }
    }
}
