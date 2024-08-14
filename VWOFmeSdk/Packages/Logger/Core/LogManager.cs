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
#pragma warning restore 1587

using System;
using System.Collections.Generic;
using System.Linq;
using VWOFmeSdk.Interfaces.Logger;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.Logger.Transports;

namespace VWOFmeSdk.Packages.Logger.Core
{
    public class LogManager : Logger, ILogManager
    {
        private static LogManager instance;
        private LogTransportManager transportManager;
        private Dictionary<string, object> config;
        private string name;
        private string requestId;
        private LogLevelEnum level;
        private string prefix;
        private string dateTimeFormat;
        private List<Dictionary<string, object>> transports = new List<Dictionary<string, object>>();

        /// <summary>
        ///  Private constructor to enforce singleton pattern
        /// </summary>
        /// <param name="config"></param>
        private LogManager(Dictionary<string, object> config)
        {
            this.config = config;
            this.name = config.ContainsKey("name") ? config["name"].ToString() : "VWO Logger";
            this.requestId = Guid.NewGuid().ToString();
            this.level = (LogLevelEnum)Enum.Parse(typeof(LogLevelEnum), config.ContainsKey("level") ? config["level"].ToString().ToUpper() : LogLevelEnum.ERROR.ToString());
            this.prefix = config.ContainsKey("prefix") ? config["prefix"].ToString() : "VWO-SDK";
            this.dateTimeFormat = config.ContainsKey("dateTimeFormat") ? config["dateTimeFormat"].ToString() : "yyyy-MM-dd'T'HH:mm:ss.SSSZ";

            this.transportManager = new LogTransportManager(config);
            HandleTransports();
            instance = this;
        }

        /// <summary>
        ///  Public static method to get the singleton instance
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static LogManager GetInstance(Dictionary<string, object> config = null)
        {
            if (instance == null)
            {
                if (config == null)
                {
                    throw new InvalidOperationException("LogManager is not initialized. Call GetInstance with a config first.");
                }
                instance = new LogManager(config);
            }
            return instance;
        }

        /// <summary>
        /// Method to handle transports
        /// </summary>
        private void HandleTransports()
        {
            if (config.ContainsKey("transports"))
            {
                var transportList = config["transports"] as List<Dictionary<string, object>>;
                if (transportList != null && transportList.Any())
                {
                    AddTransports(transportList);
                }
            } 
            else {
                    var defaultTransport = new ConsoleTransport(level);
                    var defaultTransportMap = new Dictionary<string, object>
                    {
                        { "defaultTransport", defaultTransport }
                    };
                    AddTransport(defaultTransportMap);
                }
        }

        /// <summary>
        /// Method to add a transport
        /// </summary>
        /// <param name="transport"></param>
        public void AddTransport(Dictionary<string, object> transport)
        {
            if (transport.ContainsKey("defaultTransport") && transport["defaultTransport"] is LogTransport)
            {
                transportManager.AddTransport((LogTransport)transport["defaultTransport"]);
            }
        }

        /// <summary>
        ///  Method to get debug info
        /// </summary>
        /// <returns></returns>
        public string GetDebugInfo()
        {
            return $"Name: {name}, RequestId: {requestId}, Level: {level}, Prefix: {prefix}, DateTimeFormat: {dateTimeFormat}";
        }

        /// <summary>
        /// Method to add multiple transports
        /// </summary>
        /// <param name="transportList"></param>
        public void AddTransports(List<Dictionary<string, object>> transportList)
        {
            foreach (var transport in transportList)
            {
                AddTransport(transport);
            }
        }

        public LogTransportManager GetTransportManager()
        {
            return transportManager;
        }

        public Dictionary<string, object> GetConfig()
        {
            return config;
        }

        public string GetName()
        {
            return name;
        }

        public string GetRequestId()
        {
            return requestId;
        }

        public LogLevelEnum GetLevel()
        {
            return level;
        }

        public string GetPrefix()
        {
            return prefix;
        }

        public string GetDateTimeFormat()
        {
            return dateTimeFormat;
        }

        public Dictionary<string, object> GetTransport()
        {
            // Implementing a method that returns a single transport if needed
            return transports.FirstOrDefault();
        }

        public List<Dictionary<string, object>> GetTransports()
        {
            return transports;
        }

        public override void Trace(string message)
        {
            transportManager.Trace(message);
        }

        public override void Debug(string message)
        {
            transportManager.Debug(message);
        }

        public override void Info(string message)
        {
            transportManager.Info(message);
        }

        public override void Warn(string message)
        {
            transportManager.Warn(message);
        }

        public override void Error(string message)
        {
            transportManager.Error(message);
        }
    }
}