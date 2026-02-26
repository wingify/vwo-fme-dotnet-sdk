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
#pragma warning disable 1587

using System;
using System.Collections.Generic;
using VWOFmeSdk.Interfaces.Logger;
using VWOFmeSdk.Packages.Logger.Enums;

namespace VWOFmeSdk.Packages.Logger.Core
{
    public class LogTransportManager : Logger, LogTransport
    {
        private List<Dictionary<string, object>> transports = new List<Dictionary<string, object>>();
        private Dictionary<string, object> config;

        public LogTransportManager(Dictionary<string, object> config)
        {
            this.config = config;
        }

        public void AddTransport(Dictionary<string, object> transport)
        {
            transports.Add(transport);
        }

        public bool ShouldLog(string transportLevel, object configLevel)
        {
            string level;
            if (configLevel == null || string.IsNullOrEmpty(configLevel.ToString()))
            {
                level = config.ContainsKey("level") ? config["level"].ToString() : LogLevelEnum.ERROR.ToString();
            }
            else
            {
                level = configLevel.ToString();
            }
            if (string.IsNullOrEmpty(level))
            {
                level = config.ContainsKey("level") ? config["level"].ToString() : LogLevelEnum.ERROR.ToString();
            }
            int targetLevel = (int)Enum.Parse(typeof(LogLevelEnum), transportLevel.ToUpper());
            int desiredLevel = (int)Enum.Parse(typeof(LogLevelEnum), level.ToUpper());
            return targetLevel >= desiredLevel;
        }

        public override void Trace(string message)
        {
            Log(LogLevelEnum.TRACE, message);
        }

        public override void Debug(string message)
        {
            Log(LogLevelEnum.DEBUG, message);
        }

        public override void Info(string message)
        {
            Log(LogLevelEnum.INFO, message);
        }

        public override void Warn(string message)
        {
            Log(LogLevelEnum.WARN, message);
        }

        public override void Error(string message)
        {
            Log(LogLevelEnum.ERROR, message);
        }

        public void Log(LogLevelEnum level, string message)
        {
            foreach (var transport in transports)
            {
                // get the log message builder
                LogMessageBuilder logMessageBuilder = new LogMessageBuilder(config, transport);
                // format the message
                string formattedMessage = logMessageBuilder.FormatMessage(level, message);
                // get the transport level
                object trasportLevel = transport.ContainsKey("level") ? transport["level"] : null;
                // check if the transport level should log the message
                if (ShouldLog(level.ToString(), trasportLevel))
                {
                    object logHandler = transport.ContainsKey("log") ? transport["log"] : null;
                    // if the log handler is a custom transport, log the message
                    if (logHandler is LogTransport customTransport)
                    {
                        customTransport.Log(level, formattedMessage);
                    }
                    else
                    {
                        // if the log handler is a default transport, log the message
                        object defaultLogHandler = transport.ContainsKey("defaultTransport") ? transport["defaultTransport"] : null;
                        if (defaultLogHandler is LogTransport defaultTransport)
                        {
                            defaultTransport.Log(level, formattedMessage);
                        }
                    }
                }
            }
        }
    }
}