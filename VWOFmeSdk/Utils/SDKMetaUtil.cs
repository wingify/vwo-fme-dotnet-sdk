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
using System.Reflection;

namespace VWOFmeSdk.Utils
{
    /// <summary>
    /// This class contains utility methods to get SDK version.
    /// </summary>
    public static class SDKMetaUtil
    {
        private static readonly string sdkVersion;

        static SDKMetaUtil()
        {
            try
            {
                // Fetch the version as a System.Version object
                var version = Assembly.GetExecutingAssembly().GetName().Version;

                // Format the version string to include only Major, Minor, and Build components
                sdkVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            }
            catch (Exception e)
            {
                sdkVersion = "1.0.0-error";
                Console.WriteLine($"Exception occurred while retrieving version: {e.Message}");
            }
        }

        public static string GetSdkVersion()
        {
            return sdkVersion;
        }
    }
}
