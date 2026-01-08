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
using System.Linq;
using Newtonsoft.Json;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;

namespace VWOFmeSdk.Utils
{
    public static class FunctionUtil
    {
        public static T CloneObject<T>(T obj)
        {
            if (obj == null)
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
        }

        public static long GetCurrentUnixTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static long GetCurrentUnixTimestampInMillis()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static double GetRandomNumber()
        {
            return new Random().NextDouble();
        }

        /// <summary>
        /// This method returns the list of all the campaigns linked to a feature.
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<Campaign> GetSpecificRulesBasedOnType(Feature feature, CampaignTypeEnum type)
        {
            if (feature == null || feature.RulesLinkedCampaign == null)
            {
                return new List<Campaign>();
            }
            if (type != null)
            {
                return feature.RulesLinkedCampaign.Where(rule => rule.Type == type.GetValue()).ToList();
            }
            return feature.RulesLinkedCampaign;
        }

        /// <summary>
        /// This method returns the list of all the campaigns linked to a feature.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        public static List<Campaign> GetAllExperimentRules(Feature feature)
        {
            if (feature == null || feature.RulesLinkedCampaign == null)
            {
                return new List<Campaign>();
            }
            var filteredRules = feature.RulesLinkedCampaign
                .Where(rule => rule.Type == CampaignTypeEnum.AB.GetValue() || rule.Type == CampaignTypeEnum.PERSONALIZE.GetValue())
                .ToList();

            return filteredRules;
        }

        /// <summary>
        /// This method returns the feature object from the feature key.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="featureKey"></param>
        /// <returns></returns>
        public static Feature GetFeatureFromKey(Settings settings, string featureKey)
        {
            if (settings == null || settings.Features == null)
            {
                return null;
            }
            return settings.Features.FirstOrDefault(feature => feature.Key == featureKey);
        }

        /// <summary>
        /// This method returns the campaign object from the campaign key.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static bool DoesEventBelongToAnyFeature(string eventName, Settings settings)
        {
            return settings.Features.Any(feature => feature.Metrics.Any(metric => metric.Identifier == eventName));
        }

         /// <summary>
        /// Formats error message from exception
        /// </summary>
        /// <param name="err">Exception object</param>
        /// <returns>Formatted error message</returns>
        public static string GetFormattedErrorMessage(Exception err)
        {
            if (err == null)
            {
                return "Unknown error";
            }

            try
            {
                return err.Message ?? err.ToString();
            }
            catch
            {
                return err.ToString();
            }
        }
    }
}
