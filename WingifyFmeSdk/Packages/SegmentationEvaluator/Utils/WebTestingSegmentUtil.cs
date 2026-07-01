#pragma warning disable 1587
/**
 * Copyright 2024-2026 Wingify Software Pvt. Ltd.
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
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WingifyFmeSdk.Models.User;
using WingifyFmeSdk.Enums;
using WingifyFmeSdk.Packages.Logger.Core;
using WingifyFmeSdk.Packages.Logger.Enums;

namespace WingifyFmeSdk.Packages.SegmentationEvaluator.Utils
{
    /// <summary>
    /// Utility class for evaluating and managing Web Testing pre-segmentation based on campaign variation assignments.
    /// </summary>
    public static class WebTestingSegmentUtil
    {
        /// <summary>
        /// Normalizes Web Testing campaign map keys and variation values to strings.
        /// </summary>
        /// <param name="rawAssignments">The raw assignments dictionary containing campaign IDs and variation values.</param>
        /// <returns>A dictionary with normalized string keys and values representing campaign IDs and their assigned variation IDs.</returns>
        public static Dictionary<string, string> NormalizeWebTestingCampaignsMap(Dictionary<string, object> rawAssignments)
        {
            // Turn the raw assignments map into a simple string map for regex matching.
            var campaignIdToVariationId = new Dictionary<string, string>();
            foreach (var kvp in rawAssignments)
            {
                var campaignId = kvp.Key;
                var assignedVariationId = kvp.Value;

                // Ignore empty keys; null/undefined variations mean nothing assigned for that id.
                if (assignedVariationId != null && !string.IsNullOrEmpty(campaignId))
                {
                    campaignIdToVariationId[campaignId] = assignedVariationId.ToString();
                }
            }
            return campaignIdToVariationId;
        }

        /// <summary>
        /// Parses `context.PlatformVariables["webTestingCampaigns"]` (JSON string or dictionary).
        /// </summary>
        /// <param name="context">The context object containing platform variables.</param>
        /// <returns>A dictionary of parsed and normalized web testing campaigns, or null if missing or invalid.</returns>
        public static Dictionary<string, string> ParseWebTestingCampaignsFromContext(WingifyContext context)
        {
            // No payload from the integration means empty assignments map.
            if (context.PlatformVariables == null || !context.PlatformVariables.ContainsKey("webTestingCampaigns"))
            {
                return null;
            }

            var webTestingCampaignsInput = context.PlatformVariables["webTestingCampaigns"];
            if (webTestingCampaignsInput == null)
            {
                return null;
            }

            // SDK already forwarded a plain campaignId -> variationId object.
            if (webTestingCampaignsInput is Dictionary<string, object> dictObj)
            {
                return NormalizeWebTestingCampaignsMap(dictObj);
            }
            else if (webTestingCampaignsInput is Dictionary<string, string> dictStr)
            {
                return dictStr;
            }
            else if (webTestingCampaignsInput is JObject jObject)
            {
                var dict = jObject.ToObject<Dictionary<string, object>>();
                return NormalizeWebTestingCampaignsMap(dict);
            }
            // Some stacks pass JSON text (cookie, SSR prop, tag); parse it only if it's an object.
            else if (webTestingCampaignsInput is string jsonString)
            {
                string trimmedWebTestingCampaignsJson = jsonString.Trim();
                // Empty JSON string is invalid.
                if (string.IsNullOrEmpty(trimmedWebTestingCampaignsJson))
                {
                    return null;
                }

                try
                {
                    // extract all "key": tokens and check for duplicates before parsing swallows them
                    var matches = Regex.Matches(trimmedWebTestingCampaignsJson, "\"([^\"]*)\"\\s*:");
                    if (matches.Count > 0)
                    {
                        var campaignIds = matches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                        bool hasDuplicateCampaignId = campaignIds.Count != campaignIds.Distinct().Count();

                        if (hasDuplicateCampaignId)
                        {
                            LogManager.GetInstance().ErrorLog(
                                "INVALID_WEB_TESTING_CAMPAIGNS_DUPLICATE_KEY",
                                new Dictionary<string, string>(),
                                new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } });
                        }
                    }

                    // Parse the JSON string into an object.
                    var parsedAssignments = JsonConvert.DeserializeObject<Dictionary<string, object>>(trimmedWebTestingCampaignsJson);
                    if (parsedAssignments != null)
                    {
                        return NormalizeWebTestingCampaignsMap(parsedAssignments);
                    }
                    
                    // Parsed fine but it's an array/string/etc. Invalid shape.
                    LogManager.GetInstance().ErrorLog(
                        "INVALID_WEB_TESTING_CAMPAIGNS_JSON",
                        new Dictionary<string, string>(),
                        new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } });
                }
                catch (Exception)
                {
                    // Malformed JSON; treat like missing assignments.
                    LogManager.GetInstance().ErrorLog(
                        "INVALID_WEB_TESTING_CAMPAIGNS_JSON",
                        new Dictionary<string, string>(),
                        new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } });
                }
                return null;
            }

            // Booleans/numbers/other odd types are invalid.
            string kind = webTestingCampaignsInput.GetType().IsArray ? "array" : webTestingCampaignsInput.GetType().Name.ToLower();
            LogManager.GetInstance().ErrorLog(
                "INVALID_WEB_TESTING_CAMPAIGNS_TYPE",
                new Dictionary<string, string> { { "kind", kind } },
                new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } });
            
            return null;
        }

        /// <summary>
        /// Evaluates campaignVariation operand encoding.
        /// </summary>
        /// <param name="campaignVariationOperand">The DSL operand specifying the campaign variation check.</param>
        /// <param name="assignedVariationsByCampaignId">The dictionary of assigned variations by campaign ID.</param>
        /// <returns>A tuple containing the evaluation result (true/false) and a boolean indicating if the operand format was invalid.</returns>
        public static (bool result, bool invalidFormat) EvaluateWebTestingCampaignVariation(string campaignVariationOperand, Dictionary<string, string> assignedVariationsByCampaignId)
        {
            // Null means empty assignments map.
            var assignments = assignedVariationsByCampaignId ?? new Dictionary<string, string>();

            // !123 — user should not be in campaign 123.
            var match1 = Regex.Match(campaignVariationOperand, "^!(\\d+)$");
            if (match1.Success)
            {
                var campaignId = match1.Groups[1].Value;
                return (!assignments.ContainsKey(campaignId), false);
            }

            // 123_!4 — in campaign 123 but not the variation 4.
            var match2 = Regex.Match(campaignVariationOperand, "^(\\d+)_!(\\d+)$");
            if (match2.Success)
            {
                var campaignId = match2.Groups[1].Value;
                var variationId = match2.Groups[2].Value;
                if (!assignments.ContainsKey(campaignId))
                {
                    return (false, false);
                }
                return (assignments[campaignId] != variationId, false);
            }

            // 123_4 — must be exactly that campaign and variation.
            var match3 = Regex.Match(campaignVariationOperand, "^(\\d+)_(\\d+)$");
            if (match3.Success)
            {
                var campaignId = match3.Groups[1].Value;
                var variationId = match3.Groups[2].Value;
                if (!assignments.ContainsKey(campaignId))
                {
                    return (false, false);
                }
                return (assignments[campaignId] == variationId, false);
            }

            // 123 — in the campaign, any variation counts.
            var match4 = Regex.Match(campaignVariationOperand, "^(\\d+)$");
            if (match4.Success)
            {
                var campaignId = match4.Groups[1].Value;
                return (assignments.ContainsKey(campaignId), false);
            }

            // Invalid format.
            return (false, true);
        }
    }
}
