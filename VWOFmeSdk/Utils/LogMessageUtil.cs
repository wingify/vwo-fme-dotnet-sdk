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
using System.Text.RegularExpressions;

namespace VWOFmeSdk.Utils
{
    public static class LogMessageUtil
    {
        private static readonly Regex NARGS = new Regex("\\{([0-9a-zA-Z_]+)\\}", RegexOptions.Compiled);

        public static string BuildMessage(string template, Dictionary<string, string> data)
        {
            if (template == null || data == null)
            {
                return template;
            }

            try
            {
                var result = new System.Text.StringBuilder();
                var matcher = NARGS.Matches(template);
                int lastEnd = 0;

                foreach (Match match in matcher)
                {
                    var key = match.Groups[1].Value;
                    if (data.TryGetValue(key, out var value))
                    {
                        result.Append(template.Substring(lastEnd, match.Index - lastEnd));
                        result.Append(value);
                        lastEnd = match.Index + match.Length;
                    }
                }

                result.Append(template.Substring(lastEnd));
                return result.ToString();
            }
            catch (Exception e)
            {
                return template;
            }
        }
    }
}
