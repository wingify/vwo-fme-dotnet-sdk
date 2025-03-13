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
using Newtonsoft.Json;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Services;
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Packages.DecisionMaker;
using VWOFmeSdk.Decorators;
using VWOFmeSdk.Utils;

namespace VWOFmeSdk.Utils
{
    public static class MegUtil
    {
        /// <summary>
        /// Evaluates the feature rollout rules and determines the winner campaign.
        /// </summary>
        /// <param name="settings">Settings object containing feature and campaign information.</param>
        /// <param name="feature">Feature object being evaluated.</param>
        /// <param name="groupId">Group ID of the campaigns.</param>
        /// <param name="evaluatedFeatureMap">Dictionary of evaluated features.</param>
        /// <param name="context">Context object containing user information.</param>
        /// <param name="storageService">Storage service for data persistence.</param>
        /// <returns>Winner variation if any.</returns>
        public static Variation EvaluateGroups(Settings settings, Feature feature, int groupId, 
                                       Dictionary<string, object> evaluatedFeatureMap, VWOContext context, StorageService storageService)
        {
            var featureToSkip = new List<string>();
            var campaignMap = new Dictionary<string, List<Campaign>>();

            // Get all feature keys and all campaignIds from the groupId
            var featureKeysAndGroupCampaignIds = GetFeatureKeysFromGroup(settings, groupId);
            var featureKeys = ((List<object>)featureKeysAndGroupCampaignIds["featureKeys"]).Cast<string>().ToList();
            var groupCampaignIds = ((List<object>)featureKeysAndGroupCampaignIds["groupCampaignIds"]).Cast<string>().ToList();

            foreach (var featureKey in featureKeys)
            {
                var currentFeature = FunctionUtil.GetFeatureFromKey(settings, featureKey);

                // Check if the feature is already evaluated
                if (featureToSkip.Contains(featureKey))
                {
                    continue;
                }

                // Evaluate the feature rollout rules
                bool isRolloutRulePassed = IsRolloutRuleForFeaturePassed(settings, currentFeature, evaluatedFeatureMap, featureToSkip, context, storageService);
                if (isRolloutRulePassed)
                {
                    foreach (var feat in settings.Features)
                    {
                        if (feat.Key == featureKey)
                        {
                            foreach (var campaign in feat.RulesLinkedCampaign)
                            {
                                if (groupCampaignIds.Contains(campaign.Id.ToString()) || groupCampaignIds.Contains($"{campaign.Id}_{campaign.Variations[0].Id}"))
                                {
                                    if (!campaignMap.ContainsKey(featureKey))
                                    {
                                        campaignMap[featureKey] = new List<Campaign>();
                                    }
                                    // Check if the campaign is already present in the campaignMap for the feature
                                    if (!campaignMap[featureKey].Any(c => c.RuleKey == campaign.RuleKey))
                                    {
                                        campaignMap[featureKey].Add(campaign);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Get eligible campaigns
            var eligibleCampaignsMap = GetEligibleCampaigns(settings, campaignMap, context, storageService);
            
            var eligibleCampaigns = eligibleCampaignsMap.ContainsKey("eligibleCampaigns") ? (List<Campaign>)eligibleCampaignsMap["eligibleCampaigns"] : new List<Campaign>();
            var eligibleCampaignsWithStorage = eligibleCampaignsMap.ContainsKey("eligibleCampaignsWithStorage") ? (List<Campaign>)eligibleCampaignsMap["eligibleCampaignsWithStorage"] : new List<Campaign>();
            // Check if eligible campaigns exist before proceeding
            if (eligibleCampaigns == null || eligibleCampaigns.Count == 0)
            {
                LoggerService.Log(LogLevelEnum.DEBUG, "No eligible campaigns found for feature key: " + feature.Key);
                return null;
            }

            return FindWinnerCampaignAmongEligibleCampaigns(settings, feature.Key, eligibleCampaigns, eligibleCampaignsWithStorage, groupId, context, storageService);
        }

        /// <summary>
        /// Get all feature keys and all campaignIds from the groupId    
        /// </summary>
        /// <param name="settings">Settings object containing feature and campaign information.</param>
        /// <param name="groupId">Group ID of the campaigns.</param>
        /// <returns>Dictionary with feature keys and campaign IDs.</returns>
        public static Dictionary<string, List<object>> GetFeatureKeysFromGroup(Settings settings, int groupId)
        {
            var groupCampaignIds = CampaignUtil.GetCampaignsByGroupId(settings, groupId);
            var featureKeys = CampaignUtil.GetFeatureKeysFromCampaignIds(settings, groupCampaignIds);

            return new Dictionary<string, List<object>>
            {
                {"featureKeys", featureKeys.Cast<object>().ToList()},
                {"groupCampaignIds", groupCampaignIds.Cast<object>().ToList()}
            };
        }

        /// <summary>
        /// Evaluates the feature rollout rules for a given feature.
        /// </summary>
        /// <param name="settings">Settings object containing feature and campaign information.</param>
        /// <param name="feature">Feature object being evaluated.</param>
        /// <param name="evaluatedFeatureMap">Dictionary of evaluated features.</param>
        /// <param name="featureToSkip">List of features to skip.</param>
        /// <param name="context">Context object containing user information.</param>
        /// <param name="storageService">Storage service for data persistence.</param>
        /// <returns>Boolean indicating if the rollout rule passed.</returns>
        private static bool IsRolloutRuleForFeaturePassed(
            Settings settings,
            Feature feature,
            Dictionary<string, object> evaluatedFeatureMap,
            List<string> featureToSkip,
            VWOContext context,
            StorageService storageService)
        {
            if (evaluatedFeatureMap.ContainsKey(feature.Key) &&
                ((Dictionary<string, object>)evaluatedFeatureMap[feature.Key]).ContainsKey("rolloutId"))
            {
                return true;
            }

            var rollOutRules = FunctionUtil.GetSpecificRulesBasedOnType(feature, CampaignTypeEnum.ROLLOUT);
            if (rollOutRules.Any())
            {
                Campaign ruleToTestForTraffic = null;

                foreach (var rule in rollOutRules)
                {
                    var preSegmentationResult = RuleEvaluationUtil.EvaluateRule(settings, feature, rule, context, evaluatedFeatureMap, null, storageService, new Dictionary<string, object>());
                    if ((bool)preSegmentationResult["preSegmentationResult"])
                    {
                        ruleToTestForTraffic = rule;
                        break;
                    }
                }

                if (ruleToTestForTraffic != null)
                {
                    var variation = DecisionUtil.EvaluateTrafficAndGetVariation(settings, ruleToTestForTraffic, context.Id);
                    if (variation != null)
                    {
                        var rollOutInformation = new Dictionary<string, object>
                        {
                            {"rolloutId", variation.Id},
                            {"rolloutKey", variation.Name},
                            {"rolloutVariationId", variation.Id}
                        };
                        evaluatedFeatureMap[feature.Key] = rollOutInformation;
                        return true;
                    }
                }

                featureToSkip.Add(feature.Key);
                return false;
            }

            LoggerService.Log(LogLevelEnum.INFO, "MEG_SKIP_ROLLOUT_EVALUATE_EXPERIMENTS", new Dictionary<string, string> {{"featureKey", feature.Key}});
            return true;
        }

        /// <summary>
        /// Retrieves eligible campaigns based on the provided campaign map and context.
        /// </summary>
        /// <param name="settings">Settings object containing feature and campaign information.</param>
        /// <param name="campaignMap">Dictionary mapping features to their campaigns.</param>
        /// <param name="context">Context object containing user information.</param>
        /// <param name="storageService">Storage service for data persistence.</param>
        /// <returns>Dictionary with eligible campaigns, campaigns with storage, and ineligible campaigns.</returns>
        private static Dictionary<string, object> GetEligibleCampaigns(
            Settings settings,
            Dictionary<string, List<Campaign>> campaignMap,
            VWOContext context,
            StorageService storageService)
        {
            var eligibleCampaigns = new List<Campaign>();
            var eligibleCampaignsWithStorage = new List<Campaign>();
            var inEligibleCampaigns = new List<Campaign>();

            foreach (var entry in campaignMap)
            {
                var featureKey = entry.Key;
                var campaigns = entry.Value;

                foreach (var campaign in campaigns)
                {
                    var storedDataMap = new StorageDecorator().GetFeatureFromStorage(featureKey, context, storageService);
                    try
                    {
                        var storageMapAsString = JsonConvert.SerializeObject(storedDataMap);
                        var storedData = JsonConvert.DeserializeObject<Storage>(storageMapAsString);
                        if (storedData != null && storedData.ExperimentVariationId != null && !string.IsNullOrEmpty(storedData.ExperimentVariationId.ToString()))
                        {
                            if (!string.IsNullOrEmpty(storedData.ExperimentKey) && storedData.ExperimentKey == campaign.Key)
                            {
                                var variation = CampaignUtil.GetVariationFromCampaignKey(settings, storedData.ExperimentKey, storedData.ExperimentVariationId);
                                if (variation != null)
                                {
                                    LoggerService.Log(LogLevelEnum.INFO, "MEG_CAMPAIGN_FOUND_IN_STORAGE", new Dictionary<string, string>
                                    {
                                        {"campaignKey", storedData.ExperimentKey},
                                        {"userId", context.Id}
                                    });
                                    if (!eligibleCampaignsWithStorage.Any(c => c.Key == campaign.Key))
                                    {
                                        eligibleCampaignsWithStorage.Add(campaign);
                                    }
                                    continue;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Error processing storage data: " + e.Message);
                    }

                    if (new CampaignDecisionService().GetPreSegmentationDecision(campaign, context) &&
                        new CampaignDecisionService().IsUserPartOfCampaign(context.Id, campaign))
                    {
                        LoggerService.Log(LogLevelEnum.INFO, "MEG_CAMPAIGN_ELIGIBLE", new Dictionary<string, string>
                        {
                            {"campaignKey", campaign.Key},
                            {"userId", context.Id}
                        });
                        eligibleCampaigns.Add(campaign);
                        continue;
                    }

                    inEligibleCampaigns.Add(campaign);
                }
            }

            return new Dictionary<string, object>
            {
                {"eligibleCampaigns", eligibleCampaigns},
                {"eligibleCampaignsWithStorage", eligibleCampaignsWithStorage},
                {"inEligibleCampaigns", inEligibleCampaigns}
            };
        }

        /// <summary>
        /// Find the winner campaign among the eligible campaigns.
        /// </summary>
        /// <param name="settings">Settings object containing feature and campaign information.</param>
        /// <param name="featureKey">Key of the feature being evaluated.</param>
        /// <param name="eligibleCampaigns">List of eligible campaigns.</param>
        /// <param name="eligibleCampaignsWithStorage">List of eligible campaigns with storage.</param>
        /// <param name="groupId">Group ID of the campaigns.</param>
        /// <param name="context">Context object containing user information.</param>
        /// <param name="storageService">Storage service for data persistence.</param>
        /// <returns>Winner variation if any.</returns>
        private static Variation FindWinnerCampaignAmongEligibleCampaigns(
            Settings settings,
            string featureKey,
            List<Campaign> eligibleCampaigns,
            List<Campaign> eligibleCampaignsWithStorage,
            int groupId,
            VWOContext context,
            StorageService storageService)
        {
            var campaignIds = CampaignUtil.GetCampaignIdsFromFeatureKey(settings, featureKey);
            Variation winnerCampaign = null;

            try
            {
                var group = settings.Groups[groupId.ToString()];
                int megAlgoNumber = group != null && group.Et.HasValue && group.Et.Value != 0 ? group.Et.Value : ConstantsNamespace.Constants.RANDOM_ALGO;
                if (eligibleCampaignsWithStorage.Count == 1)
                {
                    var campaignModel = JsonConvert.SerializeObject(eligibleCampaignsWithStorage[0]);
                    winnerCampaign = JsonConvert.DeserializeObject<Variation>(campaignModel);
                    LoggerService.Log(LogLevelEnum.INFO, "MEG_WINNER_CAMPAIGN", new Dictionary<string, string>
                    {
                        {"campaignKey", winnerCampaign.Type == CampaignTypeEnum.AB.GetValue() ? winnerCampaign.Key : winnerCampaign.Name + "_" + winnerCampaign.RuleKey},
                        {"groupId", groupId.ToString()},
                        {"userId", context.Id}
                    });
                }
                else if (eligibleCampaignsWithStorage.Count > 1 && megAlgoNumber == ConstantsNamespace.Constants.RANDOM_ALGO)
                {
                    winnerCampaign = NormalizeWeightsAndFindWinningCampaign(eligibleCampaignsWithStorage, context, campaignIds, groupId, storageService);
                }
                else if (eligibleCampaignsWithStorage.Count > 1)
                {
                    winnerCampaign = GetCampaignUsingAdvancedAlgo(settings, eligibleCampaignsWithStorage, context, campaignIds, groupId, storageService);
                }

                if (!eligibleCampaignsWithStorage.Any())
                {
                    if (eligibleCampaigns.Count == 1)
                    {
                        var campaignModel = JsonConvert.SerializeObject(eligibleCampaigns[0]);
                        winnerCampaign = JsonConvert.DeserializeObject<Variation>(campaignModel);
                        LoggerService.Log(LogLevelEnum.INFO, "MEG_WINNER_CAMPAIGN", new Dictionary<string, string>
                        {
                            {"campaignKey", winnerCampaign.Type == CampaignTypeEnum.AB.GetValue() ? winnerCampaign.Key : winnerCampaign.Name + "_" + winnerCampaign.RuleKey},
                            {"groupId", groupId.ToString()},
                            {"userId", context.Id},
                            {"algo", ""}
                        });
                    }
                    else if (eligibleCampaigns.Count > 1 && megAlgoNumber == ConstantsNamespace.Constants.RANDOM_ALGO)
                    {
                        winnerCampaign = NormalizeWeightsAndFindWinningCampaign(eligibleCampaigns, context, campaignIds, groupId, storageService);
                    }
                    else if (eligibleCampaigns.Count > 1)
                    {
                        winnerCampaign = GetCampaignUsingAdvancedAlgo(settings, eligibleCampaigns, context, campaignIds, groupId, storageService);
                    }
                }
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "MEG: error inside FindWinnerCampaignAmongEligibleCampaigns " + exception.Message);
            }

            return winnerCampaign;
        }

        /// <summary>
        /// Normalize the weights of all the shortlisted campaigns and find the winning campaign.
        /// </summary>
        /// <param name="shortlistedCampaigns">List of shortlisted campaigns.</param>
        /// <param name="context">Context object containing user information.</param>
        /// <param name="calledCampaignIds">List of called campaign IDs.</param>
        /// <param name="groupId">Group ID of the campaigns.</param>
        /// <param name="storageService">Storage service for data persistence.</param>
        /// <returns>Winner variation if any.</returns>
        private static Variation NormalizeWeightsAndFindWinningCampaign(
            List<Campaign> shortlistedCampaigns,
            VWOContext context,
            List<int> calledCampaignIds,
            int groupId,
            StorageService storageService)
        {
            try
            {
                // Normalize the weights of all the shortlisted campaigns
                shortlistedCampaigns.ForEach(campaign => campaign.Weight = Math.Floor(100.0 / shortlistedCampaigns.Count));

                // Convert shortlistedCampaigns to list of VariationModel
                var variations = shortlistedCampaigns
                    .Select(campaign => JsonConvert.DeserializeObject<Variation>(JsonConvert.SerializeObject(campaign)))
                    .ToList();

                // Re-distribute the traffic for each campaign
                CampaignUtil.SetCampaignAllocation(variations);

                // Get the winning variation
                var winnerCampaign = new CampaignDecisionService().GetVariation(
                    variations, new DecisionMaker().CalculateBucketValue(CampaignUtil.GetBucketingSeed(context.Id, null, groupId))
                );
        
                LoggerService.Log(LogLevelEnum.INFO, "MEG_WINNER_CAMPAIGN", new Dictionary<string, string>
                {
                    {"campaignKey", winnerCampaign.Type == CampaignTypeEnum.AB.GetValue() ? winnerCampaign.Key : winnerCampaign.Key + "_" + winnerCampaign.RuleKey},
                    {"groupId", groupId.ToString()},
                    {"userId", context.Id},
                    {"algo", "using random algorithm"}
                });

                if (winnerCampaign != null && calledCampaignIds.Contains(winnerCampaign.Id))
                {
                    return winnerCampaign;
                }
                else if (winnerCampaign != null)
                {
                    // Winner campaign is not in the called feature, store it in storage
                    new StorageDecorator().SetDataInStorage(new Dictionary<string, object>
                    {
                        { "featureKey", $"{ConstantsNamespace.Constants.VWO_META_MEG_KEY}{groupId}" },
                        { "userId", context.Id },
                        { "experimentId", winnerCampaign.Id },
                        { "experimentKey", winnerCampaign.Key },
                        { "experimentVariationId", winnerCampaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue() ? winnerCampaign.Variations[0].Id : -1 }
                    }, storageService);
                }
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "MEG: error inside NormalizeWeightsAndFindWinningCampaign " + exception.Message);
            }

            return null;
        }
        /// <summary>
        /// Get the winning campaign using advanced algorithm.
        /// </summary>
        /// <param name="settings">Settings object containing feature and campaign information.</param>
        /// <param name="shortlistedCampaigns">List of shortlisted campaigns.</param>
        /// <param name="context">Context object containing user information.</param>
        /// <param name="calledCampaignIds">List of called campaign IDs.</param>
        /// <param name="groupId">Group ID of the campaigns.</param>
        /// <param name="storageService">Storage service for data persistence.</param>
        /// <returns>Winner variation if any.</returns>
        private static Variation GetCampaignUsingAdvancedAlgo(
            Settings settings,
            List<Campaign> shortlistedCampaigns,
            VWOContext context,
            List<int> calledCampaignIds,
            int groupId,
            StorageService storageService)
        {
            Variation winnerCampaign = null;
            bool found = false; // Flag to check whether winnerCampaign has been found or not
            
            try {
                //var group = settings.Groups.TryGetValue(groupId, out var groupData) ? groupData : null;
                var groupIdString = groupId.ToString();
                var group = settings.Groups.TryGetValue(groupIdString, out var groupData) ? groupData : null;

                var priorityOrder = group?.P ?? new List<string>();
                var wt = group?.Wt ?? new Dictionary<string, double>();

                for (int i = 0; i < priorityOrder.Count; i++)
                {
                    for (int j = 0; j < shortlistedCampaigns.Count; j++)
                    {
                        if (shortlistedCampaigns[j].Id.ToString() == priorityOrder[i])
                        {
                            var campaignModel = JsonConvert.SerializeObject(FunctionUtil.CloneObject(shortlistedCampaigns[j]));
                            winnerCampaign = JsonConvert.DeserializeObject<Variation>(campaignModel);
                            //winnerCampaign = FunctionUtil.CloneObject(shortlistedCampaigns[j]);
                            found = true;
                            break;
                        }
                        else if (shortlistedCampaigns[j].Id + "_" + shortlistedCampaigns[j].Variations[0].Id == priorityOrder[i])
                        {
                            var campaignModel = JsonConvert.SerializeObject(FunctionUtil.CloneObject(shortlistedCampaigns[j]));
                            winnerCampaign = JsonConvert.DeserializeObject<Variation>(campaignModel);
                            //winnerCampaign = FunctionUtil.CloneObject(shortlistedCampaigns[j]);
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        break;
                }

                // If winnerCampaign not found through Priority, then go for weighted random distribution
                if (winnerCampaign == null)
                {
                    var participatingCampaignList = new List<Campaign>();

                    foreach (var campaign in shortlistedCampaigns)
                    {
                        var campaignId = campaign.Id.ToString();
                        if (wt.ContainsKey(campaignId))
                        {
                            var clonedCampaign = FunctionUtil.CloneObject(campaign);
                            clonedCampaign.Weight = (int)Math.Floor(wt[campaignId]);
                            participatingCampaignList.Add(clonedCampaign);
                        }
                        else if (wt.ContainsKey(campaignId + "_" + campaign.Variations[0].Id.ToString()))
                        {
                            var clonedCampaign = FunctionUtil.CloneObject(campaign);
                            clonedCampaign.Weight = (int)Math.Floor(wt[campaignId + "_" + campaign.Variations[0].Id.ToString()]);
                            participatingCampaignList.Add(clonedCampaign);
                        }
                    }

                    var variations = participatingCampaignList
                        .Select(campaign => JsonConvert.DeserializeObject<Variation>(JsonConvert.SerializeObject(campaign)))
                        .ToList();

                    CampaignUtil.SetCampaignAllocation(variations);
                    winnerCampaign = new CampaignDecisionService().GetVariation(
                        variations, new DecisionMaker().CalculateBucketValue(CampaignUtil.GetBucketingSeed(context.Id, null, groupId))
                    );
                }

                // Logging winner campaign or if no winner is found
                if (winnerCampaign != null)
                {
                    LoggerService.Log(LogLevelEnum.INFO, "MEG_WINNER_CAMPAIGN", new Dictionary<string, string>
                    {
                        {"campaignKey", winnerCampaign.Type == CampaignTypeEnum.AB.GetValue() ? winnerCampaign.Key : winnerCampaign.Key + "_" + winnerCampaign.RuleKey},
                        {"groupId", groupId.ToString()},
                        {"userId", context.Id},
                        {"algo", "using advanced algorithm"}
                    });

                    // Check if the winner is among the called campaign IDs
                    if (winnerCampaign != null && calledCampaignIds.Contains(winnerCampaign.Id))
                    {
                        return winnerCampaign;
                    }
                    else if (winnerCampaign != null)
                    {
                        // Store the winner campaign in storage if not already part of called feature
                        new StorageDecorator().SetDataInStorage(new Dictionary<string, object>
                        {
                            { "featureKey", $"{ConstantsNamespace.Constants.VWO_META_MEG_KEY}{groupId}" },
                            { "userId", context.Id },
                            { "experimentId", winnerCampaign.Id },
                            { "experimentKey", winnerCampaign.Key },
                            { "experimentVariationId", winnerCampaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue() ? winnerCampaign.Variations[0].Id : -1 }
                        }, storageService);
                    }
                }
                else
                {
                    LoggerService.Log(LogLevelEnum.INFO, $"No winner campaign found for MEG group: {groupId}");
                }
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "MEG: error inside GetCampaignUsingAdvancedAlgo " + exception.Message);
            }

            return winnerCampaign;
        }



        private static Dictionary<int, int> ConvertWtToMap(Dictionary<string, int> wt)
        {
            var wtToReturn = new Dictionary<int, int>();

            foreach (var entry in wt)
            {
                wtToReturn[int.Parse(entry.Key)] = entry.Value;
            }

            return wtToReturn;
        }
    }
}