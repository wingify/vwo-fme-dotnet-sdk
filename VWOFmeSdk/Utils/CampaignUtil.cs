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
        /// This method sets the range allocation for the variations of a campaign
        /// </summary>
        /// <param name="campaign"></param>
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
                    LoggerService.Log(LogLevelEnum.INFO, "VARIATION_RANGE_ALLOCATION", new Dictionary<string, string>
                    {
                        {"campaignKey", campaign.Key},
                        {"variationKey", variation.Name},
                        {"variationWeight", variation.Weight.ToString()},
                        {"startRange", variation.StartRangeVariation.ToString()},
                        {"endRange", variation.EndRangeVariation.ToString()}
                    });
                }
            }
        }

        /// <summary>
        ///  This method sets the range allocation for the variations of a campaign
        /// </summary>
        /// <param name="data"></param>
        /// <param name="currentAllocation"></param>
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
        /// This method sets the range allocation for the variations of a campaign
        /// </summary>
        /// <param name="variations"></param>
        public static void ScaleVariationWeights(List<Variation> variations)
        {
            double totalWeight = 0;
            foreach (var variation in variations)
            {
                totalWeight += variation.Weight;
            }

            if (totalWeight == 0)
            {
                double equalWeight = 100.0 / variations.Count;
                foreach (var variation in variations)
                {
                    variation.Weight = equalWeight;
                }
            }
            else
            {
                foreach (var variation in variations)
                {
                    variation.Weight = (variation.Weight / totalWeight) * 100;
                }
            }
        }

        /// <summary>
        /// This method sets the range allocation for the variations of a campaign
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="campaign"></param>
        /// <param name="groupId"></param>
        public static string GetBucketingSeed(string userId, Campaign campaign, int? groupId)
        {
            return groupId.HasValue ? $"{groupId}_{userId}" : $"{campaign.Id}_{userId}";
        }

        public static Variation GetVariationFromCampaignKey(Settings settings, string campaignKey, int variationId)
        {
            var campaign = settings.Campaigns.Find(c => c.Key == campaignKey);
            return campaign?.Variations.Find(v => v.Id == variationId);
        }

        public static void SetCampaignAllocation(List<Variation> campaigns)
        {
            int currentAllocation = 0;
            foreach (var campaign in campaigns)
            {
                int stepFactor = AssignRangeValuesMEG(campaign, currentAllocation);
                currentAllocation += stepFactor;
            }
        }

        /**
        * Determines if a campaign is part of a group.
        * @param settings - The settings model containing group associations.
        * @param campaignId - The ID of the campaign to check.
        * @param variationId - The optional variation ID.
        * @returns A dictionary containing the group ID and name if the campaign is part of a group, otherwise an empty dictionary.
        */
        public static Dictionary<string, string> GetGroupDetailsIfCampaignPartOfIt(Settings settings, int campaignId, int? variationId = null)
        {
            var groupDetails = new Dictionary<string, string>();

            // If variationId is null, that means that campaign is testing campaign
            // If variationId is not null, that means that campaign is personalization campaign and we need to append variationId to campaignId using _
            string campaignToCheck = campaignId.ToString();
            if (variationId.HasValue)
            {
                campaignToCheck = $"{campaignId}_{variationId.Value}";
            }

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

        public static List<Dictionary<string, string>> FindGroupsFeaturePartOf(Settings settings, string featureKey)
        {
            // Initialize a list to store all rules for the given feature to fetch campaignId and variationId later
            var ruleList = new List<Rule>();

            // Loop over all rules inside the feature where the feature key matches and collect all rules
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

            // Loop over all campaigns and find the group for each campaign
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

        public static List<string> GetCampaignsByGroupId(Settings settings, int groupId)
        {
            var group = settings.Groups[groupId.ToString()];
            return group.Campaigns.Select(campaignId => campaignId.ToString()).ToList();
        }

        public static List<string> GetFeatureKeysFromCampaignIds(Settings settings, List<string> campaignIdWithVariation)
        {
            var featureKeys = new List<string>();

            foreach (var campaign in campaignIdWithVariation)
            {
                // Split key with _ to separate campaignId and variationId
                var parts = campaign.Split('_');
                int campaignId = int.Parse(parts[0]);
                int? variationId = parts.Length > 1 ? int.Parse(parts[1]) : (int?)null;

                foreach (var feature in settings.Features)
                {
                    foreach (var rule in feature.Rules)
                    {
                        if (rule.CampaignId == campaignId)
                        {
                            // Check if variationId is provided and matches the rule's variationId
                            if (variationId.HasValue)
                            {
                                // Add feature key if variationId matches
                                if (rule.VariationId == variationId.Value)
                                {
                                    featureKeys.Add(feature.Key);
                                }
                            }
                            else
                            {
                                // Add feature key if no variationId is provided
                                featureKeys.Add(feature.Key);
                            }
                        }
                    }
                }
            }

            return featureKeys;
        }

        public static List<int> GetCampaignIdsFromFeatureKey(Settings settings, string featureKey)
        {
            var campaignIds = new List<int>();
            foreach (var feature in settings.Features)
            {
                if (feature.Key == featureKey)
                {
                    foreach (var rule in feature.Rules)
                    {
                        campaignIds.Add(rule.CampaignId);
                    }
                }
            }
            return campaignIds;
        }

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

        public static string GetRuleTypeUsingCampaignIdFromFeature(Feature feature, int campaignId)
        {
            var rule = feature.Rules.Find(r => r.CampaignId == campaignId);
            return rule?.Type ?? string.Empty;
        }

        private static int GetVariationBucketRange(double variationWeight)
        {
            if (variationWeight <= 0)
            {
                return 0;
            }
            int startRange = (int)Math.Ceiling(variationWeight * 100);
            return Math.Min(startRange, ConstantsNamespace.Constants.MAX_TRAFFIC_VALUE);
        }

        private static void HandleRolloutCampaign(Campaign campaign)
        {
            foreach (var variation in campaign.Variations)
            {
                int endRange = (int)(variation.Weight * 100);
                variation.StartRangeVariation = 1;
                variation.EndRangeVariation = endRange;
                LoggerService.Log(LogLevelEnum.INFO, "VARIATION_RANGE_ALLOCATION", new Dictionary<string, string>
                {
                    {"campaignKey", campaign.Key},
                    {"variationKey", variation.Name},
                    {"variationWeight", variation.Weight.ToString()},
                    {"startRange", variation.StartRangeVariation.ToString()},
                    {"endRange", variation.EndRangeVariation.ToString()}
                });
            }
        }
    }
}
