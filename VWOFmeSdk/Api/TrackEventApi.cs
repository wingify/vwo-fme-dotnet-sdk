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
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Models.User;
using Newtonsoft.Json;

namespace VWOFmeSdk.Api
{
    public class TrackEventAPI
    {
        public static bool Track(Settings settings, string eventName, VWOContext context, Dictionary<string, object> eventProperties, HooksManager hooksManager)
        {
            ApiEnum apiEnum = ApiEnum.TRACK;
            string apiValue = apiEnum.GetValue();
            try
            {
                if (FunctionUtil.DoesEventBelongToAnyFeature(eventName, settings))
                {
                    CreateAndSendImpressionForTrack(settings, eventName, context, eventProperties);
                    Dictionary<string, object> objectToReturn = new Dictionary<string, object>
                    {
                        { "eventName", eventName },
                        { "api", apiValue }
                    };
                    hooksManager.Set(objectToReturn);
                    hooksManager.Execute(hooksManager.Get());
                    return true;
                }
                else
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "EVENT_NOT_FOUND", new Dictionary<string, string>
                    {
                        { "eventName", eventName }
                    });
                    return false;
                }
            }
            catch (Exception e)
            {
                LoggerService.Log(LogLevelEnum.ERROR, $"Error in tracking event: {eventName}");
                return false;
            }
        }

        private static void CreateAndSendImpressionForTrack(
            Settings settings,
            string eventName,
            VWOContext context,
            Dictionary<string, object> eventProperties
        )
        {
            Dictionary<string, string> properties = NetworkUtil.GetEventsBaseProperties(
                eventName,
                ImpressionUtil.EncodeURIComponent(context.UserAgent),
                context.IpAddress
            );

            Dictionary<string, object> payload = NetworkUtil.GetTrackGoalPayloadData(
                settings,
                context.Id,
                eventName,
                context,
                eventProperties
            );

            NetworkUtil.SendPostApiRequest(properties, payload, context.UserAgent, context.IpAddress);
        }
    }
}
