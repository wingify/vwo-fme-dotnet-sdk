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
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Utils;

namespace VWOFmeSdk.Services
{
    public class LoggerService
    {
        public static Dictionary<string, string> DebugMessages { get; private set; }
        public static Dictionary<string, string> ErrorMessages { get; private set; }
        public static Dictionary<string, string> InfoMessages { get; private set; }
        public static Dictionary<string, string> WarningMessages { get; private set; }
        public static Dictionary<string, string> TraceMessages { get; private set; }

        public LoggerService(Dictionary<string, object> config)
        {
            // Initialize LogManager
            LogManager.GetInstance(config);

            // Load messages from embedded resources
            DebugMessages = LoadEmbeddedResource("debug-messages.json");
            InfoMessages = LoadEmbeddedResource("info-messages.json");
            ErrorMessages = LoadEmbeddedResource("error-messages.json");
        }

        /**
         * Loads the specified embedded JSON file and returns its contents as a dictionary.
         * Replaces the old ReadLogFiles method.
         */
        private Dictionary<string, string> LoadEmbeddedResource(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();    
            string resourcePath = $"VWOFmeSdk.Packages.Logger.Messages.{resourceFileName}";

            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream == null)
                    {
                        return new Dictionary<string, string>();
                    }

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string fileContent = reader.ReadToEnd();
                        return JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent);
                    }
                }
            }
            catch (Exception ex)
            {
                return new Dictionary<string, string>();
            }
        }

        public static void Log(LogLevelEnum level, string key, Dictionary<string, string> map)
        {
            var logManager = LogManager.GetInstance();

            string messageTemplate;
            switch (level)
            {
                case LogLevelEnum.DEBUG:
                    messageTemplate = DebugMessages.ContainsKey(key) ? DebugMessages[key] : key;
                    logManager.Debug(LogMessageUtil.BuildMessage(messageTemplate, map));
                    break;
                case LogLevelEnum.INFO:
                    messageTemplate = InfoMessages.ContainsKey(key) ? InfoMessages[key] : key;
                    logManager.Info(LogMessageUtil.BuildMessage(messageTemplate, map));
                    break;
                case LogLevelEnum.TRACE:
                    messageTemplate = TraceMessages.ContainsKey(key) ? TraceMessages[key] : key;
                    logManager.Trace(LogMessageUtil.BuildMessage(messageTemplate, map));
                    break;
                case LogLevelEnum.WARN:
                    messageTemplate = WarningMessages.ContainsKey(key) ? WarningMessages[key] : key;
                    logManager.Warn(LogMessageUtil.BuildMessage(messageTemplate, map));
                    break;
                default:
                    messageTemplate = ErrorMessages.ContainsKey(key) ? ErrorMessages[key] : key;
                    logManager.Error(LogMessageUtil.BuildMessage(messageTemplate, map));
                    break;
            }
        }

        public static void Log(LogLevelEnum level, string message)
        {
            var logManager = LogManager.GetInstance();
            switch (level)
            {
                case LogLevelEnum.DEBUG:
                    logManager.Debug(message);
                    break;
                case LogLevelEnum.INFO:
                    logManager.Info(message);
                    break;
                case LogLevelEnum.TRACE:
                    logManager.Trace(message);
                    break;
                case LogLevelEnum.WARN:
                    logManager.Warn(message);
                    break;
                default:
                    logManager.Error(message);
                    break;
            }
        }
    }
}
