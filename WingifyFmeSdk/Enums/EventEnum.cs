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

namespace WingifyFmeSdk.Enums
{
    public enum EventEnum
    {
        VARIATION_SHOWN,
        SYNC_VISITOR_PROP,
        LOG_EVENT,
        SDK_INIT_EVENT,
        USAGE_STATS_EVENT,
        DEBUGGER_EVENT
    }

    public static class EventEnumExtensions
    {
        public static string GetValue(this EventEnum eventEnum)
        {
            switch (eventEnum)
            {
                case EventEnum.VARIATION_SHOWN:
                    return "vwo_variationShown";
                case EventEnum.SYNC_VISITOR_PROP:
                    return "vwo_syncVisitorProp";
                case EventEnum.LOG_EVENT:
                    return "vwo_log";
                case EventEnum.SDK_INIT_EVENT:
                    return "vwo_fmeSdkInit";
                case EventEnum.USAGE_STATS_EVENT:
                    return "vwo_sdkUsageStats";
                case EventEnum.DEBUGGER_EVENT:
                    return "vwo_sdkDebug";
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventEnum), eventEnum, null);
            }
        }
    }
}
