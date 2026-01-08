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
using VWOFmeSdk;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Utils;
using ConstantsNamespace = VWOFmeSdk.Constants;

namespace VWOFmeSdk.Utils
{
    public static class ImpressionUtil
    {
        /// <summary>
        /// This method creates and sends an impression event for the campaign and variation shown to the user.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="campaignId"></param>
        /// <param name="variationId"></param>
        /// <param name="context"></param>
        public static void CreateAndSendImpressionForVariationShown(Settings settings, int campaignId, int variationId, VWOContext context, string featureKey)
        {
            var payload = NetworkUtil.GetTrackUserPayloadData(settings, EventEnum.VWO_VARIATION_SHOWN.GetValue(), campaignId, variationId, context);
            var vwoInstance = VWO.GetInstance();

            // Get the campaign key with feature name
            string campaignKeyWithFeatureName = CampaignUtil.GetCampaignKeyFromCampaignId(settings, campaignId);
            // Get the variation name for the campaignId and variationId
            string variationName = CampaignUtil.GetVariationNameFromCampaignIdAndVariationId(settings, campaignId, variationId);
            string campaignKey = string.Empty;
            // If featureKey is equal to the campaignKeyWithFeatureName, set campaignKey to IMPACT_ANALYSIS constant
            if (featureKey == campaignKeyWithFeatureName)
            {
                campaignKey = ConstantsNamespace.Constants.IMPACT_ANALYSIS;
            }
            else
            {
                // Otherwise, split the campaignKeyWithFeatureName and get the part after featureKey + "_"
                var prefix = featureKey + "_";
                if (!string.IsNullOrEmpty(campaignKeyWithFeatureName) && campaignKeyWithFeatureName.StartsWith(prefix))
                {
                    campaignKey = campaignKeyWithFeatureName.Substring(prefix.Length);
                }
                else
                {
                    campaignKey = campaignKeyWithFeatureName;
                }
            }
            // Get the campaign type from campaignId
            string campaignType = CampaignUtil.GetCampaignTypeFromCampaignId(settings, campaignId);
            

            Dictionary<string, object> campaignInfo = new Dictionary<string, object>
            {
                { "campaignKey", campaignKey },
                { "variationName", variationName },
                { "featureKey", featureKey },
                { "campaignType", campaignType }
            };
            // Check if batch events are enabled
            if (vwoInstance.BatchEventQueue != null)
            {
                // Enqueue the event to the batch queue
                vwoInstance.BatchEventQueue.Enqueue(payload);
            }
            else
            {
                var properties = NetworkUtil.GetEventsBaseProperties(EventEnum.VWO_VARIATION_SHOWN.GetValue(), EncodeURIComponent(context.UserAgent), context.IpAddress);
                NetworkUtil.SendPostApiRequest(properties, payload, context.UserAgent, context.IpAddress, campaignInfo);
            }
        }

        public static string EncodeURIComponent(string value)
        {
            try
            {
                return Uri.EscapeDataString(value);
            }
            catch (Exception e)
            {
                throw new Exception("Error encoding URI component: " + e.Message);
            }
        }
    }
}
