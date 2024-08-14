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
using System.Collections.Generic;
using VWOFmeSdk.Interfaces.Logger;
using VWOFmeSdk.Packages.Logger.Enums;

namespace VWOFmeSdk.Packages.Logger
{
    public class LogMessageBuilder
    {
        private Dictionary<string, object> loggerConfig;
        private LogTransport transport;
        private string prefix;
        private string dateTimeFormat;

        public LogMessageBuilder(Dictionary<string, object> loggerConfig, LogTransport transport)
        {
            this.loggerConfig = loggerConfig;
            this.transport = transport;
            this.prefix = loggerConfig.ContainsKey("prefix") ? loggerConfig["prefix"].ToString() : "VWO-SDK";
            this.dateTimeFormat = loggerConfig.ContainsKey("dateTimeFormat") ? loggerConfig["dateTimeFormat"].ToString() : "yyyy-MM-dd'T'HH:mm:ss.SSSZ";
        }

        public string FormatMessage(LogLevelEnum level, string message)
        {
            return $"[{GetFormattedLevel(level.ToString())}]: {GetFormattedPrefix(prefix)} {GetFormattedDateTime()} {message}";
        }

        private string GetFormattedPrefix(string prefix)
        {
            return $"{AnsiColorEnum.Bold}{AnsiColorEnum.Green}{prefix}{AnsiColorEnum.Reset}";
        }

        private string GetFormattedLevel(string level)
        {
            string upperCaseLevel = level.ToUpper();
            switch (upperCaseLevel)
            {
                case "TRACE":
                    return $"{AnsiColorEnum.Bold}{AnsiColorEnum.White}{upperCaseLevel}{AnsiColorEnum.Reset}";
                case "DEBUG":
                    return $"{AnsiColorEnum.Bold}{AnsiColorEnum.LightBlue}{upperCaseLevel}{AnsiColorEnum.Reset}";
                case "INFO":
                    return $"{AnsiColorEnum.Bold}{AnsiColorEnum.Cyan}{upperCaseLevel}{AnsiColorEnum.Reset}";
                case "WARN":
                    return $"{AnsiColorEnum.Bold}{AnsiColorEnum.Yellow}{upperCaseLevel}{AnsiColorEnum.Reset}";
                case "ERROR":
                    return $"{AnsiColorEnum.Bold}{AnsiColorEnum.Red}{upperCaseLevel}{AnsiColorEnum.Reset}";
                default:
                    return level;
            }
        }

        private string GetFormattedDateTime()
        {
            return DateTime.Now.ToString(dateTimeFormat);
        }
    }
}
