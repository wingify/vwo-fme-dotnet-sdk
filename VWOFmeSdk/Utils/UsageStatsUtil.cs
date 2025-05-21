using System;
using System.Collections.Generic;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Packages.Logger.Enums;

namespace VWOFmeSdk.Utils
{
    /// <summary>
    /// Manages usage statistics for the SDK.
    /// Tracks various features and configurations being used by the client.
    /// Implements Singleton pattern to ensure a single instance.
    /// </summary>
    public class UsageStatsUtil
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static UsageStatsUtil _instance;

        /// <summary>
        /// Internal storage for usage statistics data
        /// </summary>
        private Dictionary<string, object> _usageStatsData;

        /// <summary>
        /// Private constructor to prevent direct instantiation
        /// </summary>
        private UsageStatsUtil()
        {
            _usageStatsData = new Dictionary<string, object>();
        }

        /// <summary>
        /// Provides access to the singleton instance of UsageStatsUtil
        /// </summary>
        /// <returns>The single instance of UsageStatsUtil</returns>
        public static UsageStatsUtil GetInstance()
        {
            if (_instance == null)
            {
                _instance = new UsageStatsUtil();
            }
            return _instance;
        }

        /// <summary>
        /// Sets usage statistics based on provided options
        /// </summary>
        /// <param name="options">Configuration options for the SDK</param>
        public void SetUsageStats(VWOInitOptions options)
        {
            var data = new Dictionary<string, object>();

            // Map configuration options to usage stats flags
            if (options.Integrations != null) data["ig"] = 1;

            // Check if the logger has transports in it
            if (options.Logger != null && options.Logger is Dictionary<string, object> loggerDict)
            {
                if (loggerDict.ContainsKey("transport") || loggerDict.ContainsKey("transports"))
                {
                    data["cl"] = 1;
                }
            }

            // Check the logger level
            if (options.Logger != null && options.Logger is Dictionary<string, object> loggerMap)
            {
                if (loggerMap.ContainsKey("level"))
                {
                    string level = loggerMap["level"].ToString();
                    try
                    {
                        if (Enum.TryParse<LogLevelNumberEnum>(level.ToUpper(), out var logLevelEnum))
                        {
                            data["ll"] = (int)logLevelEnum;
                        }
                        else
                        {
                            data["ll"] = -1;
                        }
                    }
                    catch
                    {
                        data["ll"] = -1;
                    }
                }
            }

            if (options.Storage != null) data["ss"] = 1;
            if (options.GatewayService != null) data["gs"] = 1;
            if (options.PollInterval != null) data["pi"] = 1;

            // Handle _vwo_meta
            if (options.VwoMetaData != null && options.VwoMetaData is Dictionary<string, object> vwoMetaMap)
            {
                if (vwoMetaMap.ContainsKey("ea"))
                {
                    data["_ea"] = 1;
                }
            }

            // Get .NET version
            data["lv"] = Environment.Version.ToString();

            _usageStatsData = data;
        }

        /// <summary>
        /// Retrieves the current usage statistics
        /// </summary>
        /// <returns>Dictionary containing flags for various SDK features in use</returns>
        public Dictionary<string, object> GetUsageStats()
        {
            return new Dictionary<string, object>(_usageStatsData);
        }

        /// <summary>
        /// Clears the usage statistics data
        /// </summary>
        public void ClearUsageStats()
        {
            _usageStatsData.Clear();
        }
    }
}
