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
#pragma warning disable 1587

using System;

namespace VWOFmeSdk.Packages.Logger.Enums
{
    public enum LogLevelEnum
    {
        TRACE,
        DEBUG,
        INFO,
        WARN,
        ERROR
    }

    public static class LogLevelEnumExtensions
    {
        public static string GetLevel(this LogLevelEnum logLevelEnum)
        {
            switch (logLevelEnum)
            {
                case LogLevelEnum.TRACE:
                    return "trace";
                case LogLevelEnum.DEBUG:
                    return "debug";
                case LogLevelEnum.INFO:
                    return "info";
                case LogLevelEnum.WARN:
                    return "warn";
                case LogLevelEnum.ERROR:
                    return "error";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevelEnum), logLevelEnum, null);
            }
        }
    }
}
