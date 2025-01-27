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
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;

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
        public static void CreateAndSendImpressionForVariationShown(Settings settings, int campaignId, int variationId, VWOContext context)
        {
            var properties = NetworkUtil.GetEventsBaseProperties(settings, EventEnum.VWO_VARIATION_SHOWN.GetValue(), EncodeURIComponent(context.UserAgent), context.IpAddress);

            var payload = NetworkUtil.GetTrackUserPayloadData(settings, context.Id, EventEnum.VWO_VARIATION_SHOWN.GetValue(), campaignId, variationId, context.UserAgent, context.IpAddress);

            NetworkUtil.SendPostApiRequest(properties, payload, context.UserAgent, context.IpAddress);
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
