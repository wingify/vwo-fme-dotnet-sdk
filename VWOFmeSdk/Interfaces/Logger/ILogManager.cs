
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
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Packages.Logger.Enums;

namespace VWOFmeSdk.Interfaces.Logger
{
    /// <summary>
    /// Interface for LogManager
    /// LogManager is responsible for managing the loggers and their configurations
    /// </summary>
    public interface ILogManager
    {
        LogTransportManager GetTransportManager();
        Dictionary<string, object> GetConfig();
        string GetName();
        string GetRequestId();
        LogLevelEnum GetLevel();
        string GetPrefix();
        string GetDateTimeFormat();

        Dictionary<string, object> GetTransport();
        List<Dictionary<string, object>> GetTransports();

        void AddTransport(Dictionary<string, object> transport);
        void AddTransports(List<Dictionary<string, object>> transports);
    }
}
