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

namespace VWOFmeSdk.Enums
{
    public enum EventEnum
    {
        VWO_VARIATION_SHOWN,
        VWO_SYNC_VISITOR_PROP,
        VWO_ERROR,
        VWO_SDK_INIT_EVENT
    }

    public static class EventEnumExtensions
    {
        public static string GetValue(this EventEnum eventEnum)
        {
            switch (eventEnum)
            {
                case EventEnum.VWO_VARIATION_SHOWN:
                    return "vwo_variationShown";
                case EventEnum.VWO_SYNC_VISITOR_PROP:
                    return "vwo_syncVisitorProp";
                case EventEnum.VWO_ERROR:
                    return "vwo_log";
                case EventEnum.VWO_SDK_INIT_EVENT:
                    return "vwo_fmeSdkInit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventEnum), eventEnum, null);
            }
        }
    }
}
