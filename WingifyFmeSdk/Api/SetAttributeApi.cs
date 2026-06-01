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

using System.Collections.Generic;
using WingifyFmeSdk.Enums;
using WingifyFmeSdk.Models;
using WingifyFmeSdk.Utils;
using WingifyFmeSdk.Models.User;

namespace WingifyFmeSdk.Api
{
    public class SetAttributeAPI
    {
        public static void SetAttribute(Settings settings, Dictionary<string, dynamic> attributes, WingifyContext context)
        {
            CreateAndSendImpressionForSetAttribute(settings, attributes, context);
        }

        public static void SetAttribute(Settings settings, string attributeKey, string attributeValue, WingifyContext context)
        {
            Dictionary<string, dynamic> attributes = new Dictionary<string, dynamic>
            {
                { attributeKey, attributeValue }
            };
            CreateAndSendImpressionForSetAttribute(settings, attributes, context);
        }

        private static void CreateAndSendImpressionForSetAttribute(
            Settings settings,
            Dictionary<string, dynamic> attributes,
            WingifyContext context
        )
        {
            Dictionary<string, string> properties = NetworkUtil.GetEventsBaseProperties(
                EventEnum.SYNC_VISITOR_PROP.GetValue(),
                context.UserAgent,
                context.IpAddress
            );

            Dictionary<string, object> payload = NetworkUtil.GetAttributePayloadData(
                settings,
                context.Id,
                EventEnum.SYNC_VISITOR_PROP.GetValue(),
                attributes,
                context.VwoSessionId
            );

            var vwoInstance = WingifyClient.GetInstance();

            // Check if batch events are enabled
            if (vwoInstance.BatchEventQueue != null)
            {
                // Enqueue the event to the batch queue if batching is enabled
                vwoInstance.BatchEventQueue.Enqueue(payload);
            }
            else
            {
                // Otherwise, send the event immediately using SendPostApiRequest
                NetworkUtil.SendPostApiRequest(properties, payload, context.UserAgent, context.IpAddress);
            }
        }
    }
}
