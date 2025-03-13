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
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json; // Ensure the correct namespace for JsonSerializer is included
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Services;
using static VWOFmeSdk.Utils.CampaignUtil;

namespace VWOFmeSdk.Utils
{
    public static class SettingsUtil
    {
        /// <summary>
        /// This method processes the settings and sets the variation allocation for each campaign.
        /// </summary>
        /// <param name="settings"></param>
        public static void ProcessSettings(Settings settings)
        {
            try
            {
                var campaigns = settings.Campaigns;
                for (int i = 0; i < campaigns.Count; i++)
                {
                    var campaign = campaigns[i];
                    SetVariationAllocation(campaign);
                    campaigns[i] = campaign;
                }
                AddLinkedCampaignsToSettings(settings);
                AddIsGatewayServiceRequiredFlag(settings);
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "Exception occurred while processing settings " + exception.Message);
            }
        }

        /// <summary>
        /// This method adds linked campaigns to the settings.
        /// </summary>
        /// <param name="settings"></param>
        private static void AddLinkedCampaignsToSettings(Settings settings)
        {
            var campaignMap = settings.Campaigns.ToDictionary(campaign => campaign.Id, campaign => campaign);

            foreach (var feature in settings.Features)
            {
                var rulesLinkedCampaignModel = feature.Rules.Select(rule =>
                {
                    if (!campaignMap.TryGetValue(rule.CampaignId, out var originalCampaign))
                        return null;

                    originalCampaign.RuleKey = rule.RuleKey;
                    // Create a new campaign instance and initialize it
                    var campaign = new Campaign();
                    campaign.SetModelFromDictionary(originalCampaign);

                    // Preserve the original variations
                    var originalVariations = new List<Variation>(originalCampaign.Variations);
                    if (rule.VariationId != null && rule.VariationId != 0)
                    {
                        // Filter variations based on the rule's VariationId
                        var filteredVariations = originalVariations.Where(v => v.Id == rule.VariationId).ToList();

                        // Only update variations if a match is found
                        if (filteredVariations.Any())
                        {
                            campaign.Variations = filteredVariations;
                        }
                    }
                    return campaign;
                }).Where(campaign => campaign != null).ToList();

                feature.RulesLinkedCampaign = rulesLinkedCampaignModel;
            }
        }

        /// <summary>
        /// This method adds the IsGatewayServiceRequired flag to the settings.
        /// </summary>
        /// <param name="settings"></param>
        private static void AddIsGatewayServiceRequiredFlag(Settings settings)
        {
            var pattern = new Regex(@"\b(country|region|city|os|device_type|browser_string|ua)\b|""custom_variable""\s*:\s*{\s*""name""\s*:\s*""inlist\([^)]*\)""");

            foreach (var feature in settings.Features)
            {
                if (feature.RulesLinkedCampaign == null) 
                    continue;

                foreach (var rule in feature.RulesLinkedCampaign)
                {

                    if (rule.Variations == null || rule.Variations.Count == 0)
                    {
                        continue;
                    }

                    var segments = (rule.Type == CampaignTypeEnum.ROLLOUT.GetValue() || rule.Type == CampaignTypeEnum.PERSONALIZE.GetValue())
                        ? rule.Variations[0].Segments
                        : rule.Segments;

                    if (segments == null)
                        continue;

                    var jsonSegments = JsonConvert.SerializeObject(segments);
                    var matches = pattern.Matches(jsonSegments);
                    var foundMatch = false;

                    foreach (Match match in matches)
                    {
                        if (Regex.IsMatch(match.Value, @"\b(country|region|city|os|device_type|browser_string|ua)\b"))
                        {
                            if (!IsWithinCustomVariable(match.Index, jsonSegments))
                            {
                                foundMatch = true;
                                break;
                            }
                        }
                        else
                        {
                            foundMatch = true;
                            break;
                        }
                    }

                    if (foundMatch)
                    {
                        feature.IsGatewayServiceRequired = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// This method checks if the index is within a custom variable.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private static bool IsWithinCustomVariable(int startIndex, string input)
        {
            var index = input.LastIndexOf("\"custom_variable\"", startIndex, StringComparison.Ordinal);
            if (index == -1) return false;

            var closingBracketIndex = input.IndexOf("}", index, StringComparison.Ordinal);
            return closingBracketIndex != -1 && startIndex < closingBracketIndex;
        }
    }
}