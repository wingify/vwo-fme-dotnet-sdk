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

namespace VWOFmeSdk.Utils
{
    public static class MegUtil
    {
        /// <summary>
        /// Evaluate the feature rollout rules
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="feature"></param>
        /// <param name="groupId"></param>
        /// <param name="evaluatedFeatureMap"></param>
        /// <param name="context"></param>
        /// <param name="storageService"></param>
        /// <returns></returns>
        public static Variation EvaluateGroups(Settings settings, Feature feature, int groupId,
                                               Dictionary<string, object> evaluatedFeatureMap, VWOContext context, StorageService storageService)
        {
            var featureToSkip = new List<string>();
            var campaignMap = new Dictionary<string, List<Campaign>>();

            // Get all feature keys and all campaignIds from the groupId
            var featureKeysAndGroupCampaignIds = GetFeatureKeysFromGroup(settings, groupId);
            var featureKeys = featureKeysAndGroupCampaignIds["featureKeys"].Cast<string>().ToList();
            var groupCampaignIds = featureKeysAndGroupCampaignIds["groupCampaignIds"].Cast<string>().ToList();

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
                            foreach (var rule in feat.RulesLinkedCampaign)
                            {
                                if (groupCampaignIds.Contains(rule.Id.ToString()) || groupCampaignIds.Contains($"{rule.Id}_{rule.Variations[0].Id}"))
                                {
                                    if (!campaignMap.ContainsKey(featureKey))
                                    {
                                        campaignMap[featureKey] = new List<Campaign>();
                                    }
                                    // Check if the campaign is already present in the campaignMap for the feature
                                    if (!campaignMap[featureKey].Any(item => item.Key == rule.Key))
                                    {
                                        campaignMap[featureKey].Add(rule);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var eligibleCampaignsMap = GetEligibleCampaigns(settings, campaignMap, context, storageService);
            var eligibleCampaigns = (List<Campaign>)eligibleCampaignsMap["eligibleCampaigns"];
            var eligibleCampaignsWithStorage = (List<Campaign>)eligibleCampaignsMap["eligibleCampaignsWithStorage"];

            return FindWinnerCampaignAmongEligibleCampaigns(settings, feature.Key, eligibleCampaigns, eligibleCampaignsWithStorage, groupId, context, storageService);
        }

        /// <summary>
        /// Get all feature keys and all campaignIds from the groupId    
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
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
        /// Get all eligible campaigns for the feature
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="feature"></param>
        /// <param name="evaluatedFeatureMap"></param>
        /// <param name="featureToSkip"></param>
        /// <param name="context"></param>
        /// <param name="storageService"></param>
        /// <returns></returns>
        private static bool IsRolloutRuleForFeaturePassed(Settings settings, Feature feature, Dictionary<string, object> evaluatedFeatureMap,
                                                          List<string> featureToSkip, VWOContext context, StorageService storageService)
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
        /// Get all eligible campaigns for the feature
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="campaignMap"></param>
        /// <param name="context"></param>
        /// <param name="storageService"></param>
        /// <returns></returns>
        private static Dictionary<string, object> GetEligibleCampaigns(Settings settings, Dictionary<string, List<Campaign>> campaignMap,
                                                                       VWOContext context, StorageService storageService)
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
        /// Find the winner campaign among the eligible campaigns
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="featureKey"></param>
        /// <param name="eligibleCampaigns"></param>
        /// <param name="eligibleCampaignsWithStorage"></param>
        /// <param name="groupId"></param>
        /// <param name="context"></param>
        /// <param name="storageService"></param>
        /// <returns></returns>
        private static Variation FindWinnerCampaignAmongEligibleCampaigns(Settings settings, string featureKey,
                                                                          List<Campaign> eligibleCampaigns,
                                                                          List<Campaign> eligibleCampaignsWithStorage,
                                                                          int groupId, VWOContext context, StorageService storageService)
        {
            var campaignIds = CampaignUtil.GetCampaignIdsFromFeatureKey(settings, featureKey);
            Variation winnerCampaign = null;

            try
            {
                var group = settings.Groups[groupId.ToString()];
                int megAlgoNumber = group != null && group.Et != 0 ? group.Et : ConstantsNamespace.Constants.RANDOM_ALGO;
                if (eligibleCampaignsWithStorage.Count == 1)
                {
                    var campaignModel = JsonConvert.SerializeObject(eligibleCampaignsWithStorage[0]);
                    winnerCampaign = JsonConvert.DeserializeObject<Variation>(campaignModel);
                    LoggerService.Log(LogLevelEnum.INFO, "MEG_WINNER_CAMPAIGN", new Dictionary<string, string>
                    {
                        {"campaignKey", winnerCampaign.Key},
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
                            {"campaignKey", winnerCampaign.Key},
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
    /// Normalize the weights of all the shortlisted campaigns and find the winning campaign
    /// </summary>
    /// <param name="shortlistedCampaigns"></param>
    /// <param name="context"></param>
    /// <param name="calledCampaignIds"></param>
    /// <param name="groupId"></param>
    /// <param name="storageService"></param>
    /// <returns></returns>
        private static Variation NormalizeWeightsAndFindWinningCampaign(List<Campaign> shortlistedCampaigns, VWOContext context, List<int> calledCampaignIds, int groupId, StorageService storageService)
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

                // Log the winner campaign
                LoggerService.Log(LogLevelEnum.INFO, "MEG_WINNER_CAMPAIGN", new Dictionary<string, string>
                {
                    {"campaignKey", winnerCampaign.Key},
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
                        { "featureKey", $"_vwo_meta_meg_{groupId}" },
                        { "context", context },
                        { "experimentId", winnerCampaign.Id },
                        { "experimentKey", winnerCampaign.Key },
                        { "experimentVariationId", -1 }
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
        /// Get the winning campaign using advanced algo
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="shortlistedCampaigns"></param>
        /// <param name="context"></param>
        /// <param name="calledCampaignIds"></param>
        /// <param name="groupId"></param>
        /// <param name="storageService"></param>
        /// <returns></returns>
        private static Variation GetCampaignUsingAdvancedAlgo(Settings settings, List<Campaign> shortlistedCampaigns,
                                                              VWOContext context, List<int> calledCampaignIds, int groupId, StorageService storageService)
        {
            Variation winnerCampaign = null;

            try
            {

                if (!settings.Groups.TryGetValue(groupId.ToString(), out var group))
                {
                    LoggerService.Log(LogLevelEnum.INFO,$"Group with ID {groupId} not found in settings.");
                }

                var priorityOrder = group?.P?.Any() == true ? group.P : new List<int>();
                var wt = group?.Wt?.Any() == true ? ConvertWtToMap(group.Wt) : new Dictionary<int, int>();

                foreach (var integer in priorityOrder)
                {
                    foreach (var shortlistedCampaign in shortlistedCampaigns)
                    {
                        if (shortlistedCampaign.Id == integer)
                        {
                            var campaignModel = JsonConvert.SerializeObject(FunctionUtil.CloneObject(shortlistedCampaign));
                            winnerCampaign = JsonConvert.DeserializeObject<Variation>(campaignModel);
                            break;
                        }
                    }

                    if (winnerCampaign != null)
                    {
                        break;
                    }
                }

                if (winnerCampaign == null)
                {
                    var participatingCampaignList = new List<Campaign>();

                    foreach (var campaign in shortlistedCampaigns)
                    {
                        if (wt.TryGetValue(campaign.Id, out var weight))
                        {
                            var clonedCampaign = FunctionUtil.CloneObject(campaign);
                            clonedCampaign.Weight = weight;
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

                LoggerService.Log(LogLevelEnum.INFO, "MEG_WINNER_CAMPAIGN", new Dictionary<string, string>
                {
                    {"campaignKey", winnerCampaign.Name},
                    {"groupId", groupId.ToString()},
                    {"userId", context.Id},
                    {"algo", "using advanced algorithm"}
                });

                if (calledCampaignIds.Contains(winnerCampaign.Id))
                {
                    return winnerCampaign;
                }
                else if (winnerCampaign != null)
                {
                    // Winner campaign is not in the called feature, store it in storage
                    new StorageDecorator().SetDataInStorage(new Dictionary<string, object>
                    {
                        { "featureKey", $"_vwo_meta_meg_{groupId}" },
                        { "context", context },
                        { "experimentId", winnerCampaign.Id },
                        { "experimentKey", winnerCampaign.Key },
                        { "experimentVariationId", -1 }
                    }, storageService);
                }
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "MEG: error inside GetCampaignUsingAdvancedAlgo " + exception.Message);
            }

            return null;
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