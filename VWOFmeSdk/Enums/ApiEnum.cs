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
    public enum ApiEnum
    {
        INIT,
        ON_INIT,
        GET_FLAG,
        TRACK,
        SET_ATTRIBUTE,
        FLUSH_EVENTS,
        BATCH_FLUSH
    }

    public static class ApiEnumExtensions
    {
        public static string GetValue(this ApiEnum apiEnum)
        {
            switch (apiEnum)
            {
                case ApiEnum.INIT:
                    return "init";
                case ApiEnum.ON_INIT:
                    return "onInit";
                case ApiEnum.GET_FLAG:
                    return "getFlag";
                case ApiEnum.TRACK:
                    return "trackEvent";
                case ApiEnum.SET_ATTRIBUTE:
                    return "setAttribute";
                case ApiEnum.FLUSH_EVENTS:
                    return "flushEvents";
                case ApiEnum.BATCH_FLUSH:
                    return "batchFlush";
                default:
                    throw new ArgumentOutOfRangeException(nameof(apiEnum), apiEnum, null);
            }
        }
    }
}
