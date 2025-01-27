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
using System.Linq;
using VWOFmeSdk.Constants;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Services;
using System.Linq;
using ConstantsNamespace = VWOFmeSdk.Constants;

namespace VWOFmeSdk.Utils
{
    public static class CampaignUtil
    {
        /// <summary>
        /// Sets the variation allocation for a given campaign based on its type.
        /// If the campaign type is ROLLOUT or PERSONALIZE, it handles the campaign using HandleRolloutCampaign.
        /// Otherwise, it assigns range values to each variation in the campaign.
        /// </summary>
        /// <param name="campaign">The campaign for which to set the variation allocation.</param>
        public static void SetVariationAllocation(Campaign campaign)
        {
            if (campaign.Type == CampaignTypeEnum.ROLLOUT.GetValue() || campaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue())
            {
                HandleRolloutCampaign(campaign);
            }
            else
            {
                int currentAllocation = 0;
                foreach (var variation in campaign.Variations)
                {
                    int stepFactor = AssignRangeValues(variation, currentAllocation);
                    currentAllocation += stepFactor;

                    // Log the range allocation for debugging purposes
                    LoggerService.Log(LogLevelEnum.INFO, "VARIATION_RANGE_ALLOCATION", new Dictionary<string, string>
                    {
                        {"variationKey", variation.Key},
                        {"campaignKey", campaign.Type == CampaignTypeEnum.AB.GetValue() ? campaign.Key : campaign.Name + "_" + campaign.RuleKey},
                        {"variationWeight", variation.Weight.ToString()},
                        {"startRange", variation.StartRangeVariation.ToString()},
                        {"endRange", variation.EndRangeVariation.ToString()}
                    });
                }
            }
        }

        /// <summary>
        /// Assigns start and end range values to a variation based on its weight.
        /// </summary>
        /// <param name="data">The variation model to assign range values.</param>
        /// <param name="currentAllocation">The current allocation value before this variation.</param>
        /// <returns>The step factor calculated from the variation's weight.</returns>
        public static int AssignRangeValues(Variation data, int currentAllocation)
        {
            int stepFactor = GetVariationBucketRange(data.Weight);

            if (stepFactor > 0)
            {
                data.StartRangeVariation = currentAllocation + 1;
                data.EndRangeVariation = currentAllocation + stepFactor;
            }
            else
            {
                data.StartRangeVariation = -1;
                data.EndRangeVariation = -1;
            }

            return stepFactor;
        }

        /// <summary>
        /// Scales the weights of variations to sum up to 100%.
        /// </summary>
        /// <param name="variations">The list of variations to scale.</param>
        public static void ScaleVariationWeights(List<Variation> variations)
        {
            double totalWeight = variations.Sum(variation => variation.Weight);

            if (totalWeight == 0)
            {
                double equalWeight = 100.0 / variations.Count;
                variations.ForEach(variation => variation.Weight = equalWeight);
            }
            else
            {
                variations.ForEach(variation => variation.Weight = (variation.Weight / totalWeight) * 100);
            }
        }

        /// <summary>
        /// Generates a bucketing seed based on user ID, campaign, and optional group ID.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="campaign">The campaign object.</param>
        /// <param name="groupId">The optional group ID.</param>
        /// <returns>The bucketing seed.</returns>
        public static string GetBucketingSeed(string userId, Campaign campaign, int? groupId)
        {
            return groupId.HasValue ? $"{groupId}_{userId}" : $"{campaign.Id}_{userId}";
        }

        /// <summary>
        /// Retrieves a variation by its ID within a specific campaign identified by its key.
        /// </summary>
        /// <param name="settings">The settings model containing all campaigns.</param>
        /// <param name="campaignKey">The key of the campaign.</param>
        /// <param name="variationId">The ID of the variation to retrieve.</param>
        /// <returns>The found variation model or null if not found.</returns>
        public static Variation GetVariationFromCampaignKey(Settings settings, string campaignKey, int variationId)
        {
            var campaign = settings.Campaigns.FirstOrDefault(c => c.Key == campaignKey);
            return campaign?.Variations.FirstOrDefault(v => v.Id == variationId);
        }

        /// <summary>
        /// Sets the allocation ranges for a list of campaigns.
        /// </summary>
        /// <param name="campaigns">The list of campaigns to set allocations for.</param>
        public static void SetCampaignAllocation(List<Variation> campaigns)
        {
            int currentAllocation = 0;
            foreach (var campaign in campaigns)
            {
                int stepFactor = AssignRangeValuesMEG(campaign, currentAllocation);
                currentAllocation += stepFactor;
            }
        }

        /// <summary>
        /// Determines if a campaign is part of a group.
        /// </summary>
        /// <param name="settings">The settings model containing group associations.</param>
        /// <param name="campaignId">The ID of the campaign to check.</param>
        /// <param name="variationId">The optional variation ID.</param>
        /// <returns>A dictionary containing the group ID and name if the campaign is part of a group, otherwise an empty dictionary.</returns>
        public static Dictionary<string, string> GetGroupDetailsIfCampaignPartOfIt(Settings settings, int campaignId, int? variationId = null)
        {
            var groupDetails = new Dictionary<string, string>();

            string campaignToCheck = variationId.HasValue ? $"{campaignId}_{variationId}" : campaignId.ToString();

            if (settings.CampaignGroups != null && settings.CampaignGroups.ContainsKey(campaignToCheck))
            {
                int groupId = settings.CampaignGroups[campaignToCheck];
                if (settings.Groups.ContainsKey(groupId.ToString()))
                {
                    string groupName = settings.Groups[groupId.ToString()].Name;
                    groupDetails["groupId"] = groupId.ToString();
                    groupDetails["groupName"] = groupName;
                }
            }
            return groupDetails;
        }

        /// <summary>
        /// Finds all groups associated with a feature specified by its key.
        /// </summary>
        /// <param name="settings">The settings model containing all features and groups.</param>
        /// <param name="featureKey">The key of the feature to find groups for.</param>
        /// <returns>A list of dictionaries containing the group ID and name associated with the feature.</returns>
        public static List<Dictionary<string, string>> FindGroupsFeaturePartOf(Settings settings, string featureKey)
        {
            var ruleList = new List<Rule>();

            foreach (var feature in settings.Features)
            {
                if (feature.Key == featureKey)
                {
                    foreach (var rule in feature.Rules)
                    {
                        if (!ruleList.Contains(rule))
                        {
                            ruleList.Add(rule);
                        }
                    }
                }
            }

            var groups = new List<Dictionary<string, string>>();
            foreach (var rule in ruleList)
            {
                var group = GetGroupDetailsIfCampaignPartOfIt(
                    settings,
                    rule.CampaignId,
                    rule.Type == CampaignTypeEnum.PERSONALIZE.GetValue() ? (int?)rule.VariationId : null
                );

                if (group.Count > 0 && !groups.Any(g => g["groupId"] == group["groupId"]))
                {
                    groups.Add(group);
                }
            }
            return groups;
        }

        /// <summary>
        /// Retrieves campaigns by a specific group ID.
        /// </summary>
        /// <param name="settings">The settings model containing all groups.</param>
        /// <param name="groupId">The ID of the group.</param>
        /// <returns>A list of campaign IDs associated with the specified group ID.</returns>
        public static List<string> GetCampaignsByGroupId(Settings settings, int groupId)
        {
            var group = settings.Groups[groupId.ToString()];
            return group?.Campaigns.Select(campaignId => campaignId.ToString()).ToList() ?? new List<string>();
        }

        /// <summary>
        /// Retrieves feature keys from a list of campaign IDs.
        /// </summary>
        /// <param name="settings">The settings model containing all features.</param>
        /// <param name="campaignIdWithVariation">A list of campaign IDs and variation IDs in format campaignId_variationId.</param>
        /// <returns>A list of feature keys associated with the provided campaign IDs.</returns>
        public static List<string> GetFeatureKeysFromCampaignIds(Settings settings, List<string> campaignIdWithVariation)
        {
            var featureKeys = new List<string>();

            foreach (var campaign in campaignIdWithVariation)
            {
                var parts = campaign.Split('_');
                int campaignId = int.Parse(parts[0]);
                int? variationId = parts.Length > 1 ? int.Parse(parts[1]) : (int?)null;

                foreach (var feature in settings.Features)
                {
                    if(featureKeys.Contains(feature.Key))
                    {
                        continue;
                    }
                    foreach (var rule in feature.Rules)
                    {
                        if (rule.CampaignId == campaignId)
                        {
                            if (variationId.HasValue)
                            {
                                if (rule.VariationId == variationId.Value)
                                {
                                    featureKeys.Add(feature.Key);
                                }
                            }
                            else
                            {
                                featureKeys.Add(feature.Key);
                            }
                        }
                    }
                }
            }

            return featureKeys;
        }

        /// <summary>
        /// Retrieves campaign IDs from a specific feature key.
        /// </summary>
        /// <param name="settings">The settings model containing all features.</param>
        /// <param name="featureKey">The key of the feature.</param>
        /// <returns>A list of campaign IDs associated with the specified feature key.</returns>
        public static List<int> GetCampaignIdsFromFeatureKey(Settings settings, string featureKey)
        {
            var campaignIds = new List<int>();
            foreach (var feature in settings.Features)
            {
                if (feature.Key == featureKey)
                {
                    campaignIds.AddRange(feature.Rules.Select(rule => rule.CampaignId));
                }
            }
            return campaignIds;
        }

        /// <summary>
        /// Assigns range values to a campaign based on its weight.
        /// </summary>
        /// <param name="data">The campaign data containing weight.</param>
        /// <param name="currentAllocation">The current allocation value before this campaign.</param>
        /// <returns>The step factor calculated from the campaign's weight.</returns>
        public static int AssignRangeValuesMEG(Variation data, int currentAllocation)
        {
            int stepFactor = GetVariationBucketRange(data.Weight);
            if (stepFactor > 0)
            {
                data.StartRangeVariation = currentAllocation + 1;
                data.EndRangeVariation = currentAllocation + stepFactor;
            }
            else
            {
                data.StartRangeVariation = -1;
                data.EndRangeVariation = -1;
            }
            return stepFactor;
        }

        /// <summary>
        /// Retrieves the rule type using a campaign ID from a specific feature.
        /// </summary>
        /// <param name="feature">The feature containing rules.</param>
        /// <param name="campaignId">The campaign ID to find the rule type for.</param>
        /// <returns>The rule type if found, otherwise an empty string.</returns>
        public static string GetRuleTypeUsingCampaignIdFromFeature(Feature feature, int campaignId)
        {
            var rule = feature.Rules.FirstOrDefault(r => r.CampaignId == campaignId);
            return rule?.Type ?? string.Empty;
        }

        /// <summary>
        /// Calculates the bucket range for a variation based on its weight.
        /// </summary>
        /// <param name="variationWeight">The weight of the variation.</param>
        /// <returns>The calculated bucket range.</returns>
        private static int GetVariationBucketRange(double variationWeight)
        {
            if (variationWeight <= 0)
            {
                return 0;
            }
            int startRange = (int)Math.Ceiling(variationWeight * 100);
            return Math.Min(startRange, ConstantsNamespace.Constants.MAX_TRAFFIC_VALUE);
        }

        /// <summary>
        /// Handles the rollout campaign by setting start and end ranges for all variations.
        /// </summary>
        /// <param name="campaign">The campaign to handle.</param>
        private static void HandleRolloutCampaign(Campaign campaign)
        {
            foreach (var variation in campaign.Variations)
            {
                int endRange = (int)(variation.Weight * 100);
                variation.StartRangeVariation = 1;
                variation.EndRangeVariation = endRange;
                LoggerService.Log(LogLevelEnum.INFO, "VARIATION_RANGE_ALLOCATION", new Dictionary<string, string>
                {
                    {"variationKey", variation.Key},
                    {"campaignKey", campaign.Type == CampaignTypeEnum.AB.GetValue() ? campaign.Key : campaign.Name + "_" + campaign.RuleKey},
                    {"variationWeight", variation.Weight.ToString()},
                    {"startRange", variation.StartRangeVariation.ToString()},
                    {"endRange", variation.EndRangeVariation.ToString()}
                });
            }
        }
    }
}
