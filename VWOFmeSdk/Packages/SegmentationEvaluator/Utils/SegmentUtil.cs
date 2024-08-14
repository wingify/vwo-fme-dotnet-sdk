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
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace VWOFmeSdk.Packages.SegmentationEvaluator.Utils
{
    public static class SegmentUtil
    {
        public static bool CheckValuePresent(Dictionary<string, List<string>> expectedMap, Dictionary<string, string> actualMap)
        {
            foreach (var key in actualMap.Keys)
            {
                if (expectedMap.ContainsKey(key))
                {
                    var expectedValues = expectedMap[key];
                    expectedValues = expectedValues.Select(value => value.ToLower()).ToList();
                    var actualValue = actualMap[key];

                    foreach (var val in expectedValues)
                    {
                        if (val.StartsWith("wildcard(") && val.EndsWith(")"))
                        {
                            var wildcardPattern = val.Substring(9, val.Length - 10);
                            var regex = new Regex(wildcardPattern.Replace("*", ".*"));
                            var matcher = regex.Match(actualValue);
                            if (matcher.Success)
                            {
                                return true;
                            }
                        }
                    }

                    if (expectedValues.Contains(actualValue.Trim().ToLower()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool ValuesMatch(Dictionary<string, object> expectedLocationMap, Dictionary<string, string> userLocation)
        {
            foreach (var entry in expectedLocationMap)
            {
                var key = entry.Key;
                var value = entry.Value;
                if (userLocation.ContainsKey(key))
                {
                    var normalizedValue1 = NormalizeValue(value);
                    var normalizedValue2 = NormalizeValue(userLocation[key]);
                    if (!normalizedValue1.Equals(normalizedValue2))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static string NormalizeValue(object value)
        {
            if (value == null)
            {
                return null;
            }
            return value.ToString().Replace("\"", "").Trim();
        }

        public static KeyValuePair<string, JToken>? GetKeyValue(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var fields = (JObject)token;
                var firstProperty = fields.Properties().FirstOrDefault();

                if (firstProperty != null)
                {
                    return new KeyValuePair<string, JToken>(firstProperty.Name, firstProperty.Value);
                }
            }
            return null;
        }

        public static bool MatchWithRegex(string str, string regex)
        {
            try
            {
                var pattern = new Regex(regex);
                var matcher = pattern.Match(str);
                return matcher.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
