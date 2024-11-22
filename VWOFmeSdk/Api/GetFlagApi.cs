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
using Newtonsoft.Json;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.SegmentationEvaluator.Core;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Decorators;
using static VWOFmeSdk.Utils.FunctionUtil;
using VWOFmeSdk.Utils; // Ensure this namespace is included

namespace VWOFmeSdk.Api
{
    public class GetFlagAPI
    {
        public static GetFlag GetFlag(string featureKey, Settings settings, VWOContext context, HooksManager hookManager)
        {
            GetFlag getFlag = new GetFlag();
            bool shouldCheckForExperimentsRules = false;

            Dictionary<string, object> passedRulesInformation = new Dictionary<string, object>();
            Dictionary<string, object> evaluatedFeatureMap = new Dictionary<string, object>();

            // Get feature object from feature key
            Feature feature = GetFeatureFromKey(settings, featureKey);

            ApiEnum apiEnum = ApiEnum.GET_FLAG;
            string apiValue = apiEnum.GetValue();
            // Decision object to be sent for the integrations
            Dictionary<string, object> decision = new Dictionary<string, object>
            {
                { "featureName", feature?.Name },
                { "featureId", feature?.Id },
                { "featureKey", feature?.Key },
                { "userId", context?.Id },
                { "api", apiValue }
            };

            StorageService storageService = new StorageService();
            Dictionary<string, object> storedDataMap = new StorageDecorator().GetFeatureFromStorage(featureKey, context, storageService);

            // If feature is found in the storage, return the stored variation
            try
            {
                string storageMapAsString = JsonConvert.SerializeObject(storedDataMap);
                Storage storedData = JsonConvert.DeserializeObject<Storage>(storageMapAsString);
                if (storedData != null && storedData.ExperimentVariationId != null && !string.IsNullOrEmpty(storedData.ExperimentVariationId.ToString()))
                {
                    if (!string.IsNullOrEmpty(storedData.ExperimentKey))
                    {
                        Variation variation = CampaignUtil.GetVariationFromCampaignKey(settings, storedData.ExperimentKey, storedData.ExperimentVariationId);
                        if (variation != null)
                        {
                            LoggerService.Log(LogLevelEnum.INFO, "STORED_VARIATION_FOUND", new Dictionary<string, string>
                            {
                                { "variationKey", variation.Name },
                                { "userId", context.Id },
                                { "experimentType", "experiment" },
                                { "experimentKey", storedData.ExperimentKey }
                            });
                            getFlag.SetIsEnabled(true);
                            getFlag.Variables = variation.Variables;
                            return getFlag;
                        }
                    }
                }
                else if (storedData != null && storedData.RolloutKey != null && storedData.RolloutId != null && !string.IsNullOrEmpty(storedData.RolloutId.ToString()))
                {
                    Variation variation = CampaignUtil.GetVariationFromCampaignKey(settings, storedData.RolloutKey, storedData.RolloutVariationId);
                    if (variation != null)
                    {
                        LoggerService.Log(LogLevelEnum.INFO, "STORED_VARIATION_FOUND", new Dictionary<string, string>
                        {
                            { "variationKey", variation.Name },
                            { "userId", context.Id },
                            { "experimentType", "rollout" },
                            { "experimentKey", storedData.RolloutKey }
                        });

                        LoggerService.Log(LogLevelEnum.DEBUG, "EXPERIMENTS_EVALUATION_WHEN_ROLLOUT_PASSED", new Dictionary<string, string>
                        {
                            { "userId", context.Id }
                        });

                        getFlag.SetIsEnabled(true);
                        shouldCheckForExperimentsRules = true;
                        Dictionary<string, object> featureInfo = new Dictionary<string, object>
                        {
                            { "rolloutId", storedData.RolloutId },
                            { "rolloutKey", storedData.RolloutKey },
                            { "rolloutVariationId", storedData.RolloutVariationId }
                        };
                        evaluatedFeatureMap[featureKey] = featureInfo;
                        passedRulesInformation = featureInfo;
                    }
                }
            }
            catch (Exception e)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "Error parsing stored data: " + e.Message);
            }

            // If feature is not found, return false
            if (feature == null)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "FEATURE_NOT_FOUND", new Dictionary<string, string>
                {
                    { "featureKey", featureKey }
                });
                getFlag.SetIsEnabled(false);
                return getFlag;
            }

            SegmentationManager.GetInstance().SetContextualData(settings, feature, context);

            // Get all the rollout rules for the feature and evaluate them
            List<Campaign> rollOutRules = GetSpecificRulesBasedOnType(feature, CampaignTypeEnum.ROLLOUT);
            if (rollOutRules.Count > 0 && !getFlag.IsEnabled())
            {
                List<Campaign> rolloutRulesToEvaluate = new List<Campaign>();
                foreach (var rule in rollOutRules)
                {
                    Dictionary<string, object> evaluateRuleResult = RuleEvaluationUtil.EvaluateRule(settings, feature, rule, context, evaluatedFeatureMap, new Dictionary<int, string>(), storageService, decision);
                    bool preSegmentationResult = (bool)evaluateRuleResult["preSegmentationResult"];
                    if (preSegmentationResult)
                    {
                        rolloutRulesToEvaluate.Add(rule);
                        Dictionary<string, object> featureMap = new Dictionary<string, object>
                        {
                            { "rolloutId", rule.Id },
                            { "rolloutKey", rule.Key },
                            { "rolloutVariationId", rule.Variations[0].Id }
                        };
                        evaluatedFeatureMap[featureKey] = featureMap;
                        break;
                    }
                }

                // Evaluate the passed rollout rule traffic and get the variation
                if (rolloutRulesToEvaluate.Count > 0)
                {
                    Campaign passedRolloutCampaign = rolloutRulesToEvaluate[0];
                    Variation variation = DecisionUtil.EvaluateTrafficAndGetVariation(settings, passedRolloutCampaign, context.Id);
                    if (variation != null)
                    {
                        getFlag.SetIsEnabled(true);
                        getFlag.Variables = variation.Variables;
                        shouldCheckForExperimentsRules = true;
                        UpdateIntegrationsDecisionObject(passedRolloutCampaign, variation, passedRulesInformation, decision);
                        ImpressionUtil.CreateAndSendImpressionForVariationShown(settings, passedRolloutCampaign.Id, variation.Id, context);
                    }
                }
            }
            else
            {
                LoggerService.Log(LogLevelEnum.DEBUG, "EXPERIMENTS_EVALUATION_WHEN_NO_ROLLOUT_PRESENT", null);
                shouldCheckForExperimentsRules = true;
            }

            // If any rollout rule passed pre-segmentation and traffic evaluation, check for experiment rules
            if (shouldCheckForExperimentsRules)
            {
                List<Campaign> experimentRulesToEvaluate = new List<Campaign>();
                List<Campaign> experimentRules = GetAllExperimentRules(feature);
                Dictionary<int, string> megGroupWinnerCampaigns = new Dictionary<int, string>();

                foreach (var rule in experimentRules)
                {
                    // Evaluate the rule here
                    Dictionary<string, object> evaluateRuleResult = RuleEvaluationUtil.EvaluateRule(settings, feature, rule, context, evaluatedFeatureMap, megGroupWinnerCampaigns, storageService, decision);
                    bool preSegmentationResult = (bool)evaluateRuleResult["preSegmentationResult"];
                    if (preSegmentationResult)
                    {
                        Variation whitelistedObject = (Variation)evaluateRuleResult["whitelistedObject"];
                        if (whitelistedObject == null)
                        {
                            experimentRulesToEvaluate.Add(rule);
                        }
                        else
                        {
                            getFlag.SetIsEnabled(true);
                            getFlag.Variables = whitelistedObject.Variables;
                            passedRulesInformation["experimentId"] = rule.Id;
                            passedRulesInformation["experimentKey"] = rule.Key;
                            passedRulesInformation["experimentVariationId"] = whitelistedObject.Id;
                        }
                        break;
                    }
                }

                // Evaluate the passed experiment rule traffic and get the variation
                if (experimentRulesToEvaluate.Count > 0)
                {
                    Campaign campaign = experimentRulesToEvaluate[0];
                    Variation variation = DecisionUtil.EvaluateTrafficAndGetVariation(settings, campaign, context.Id);
                    if (variation != null)
                    {
                        getFlag.SetIsEnabled(true);
                        getFlag.Variables = variation.Variables;
                        UpdateIntegrationsDecisionObject(campaign, variation, passedRulesInformation, decision);
                        ImpressionUtil.CreateAndSendImpressionForVariationShown(settings, campaign.Id, variation.Id, context);
                    }
                }
            }

            if (getFlag.IsEnabled())
            {
                Dictionary<string, object> storageMap = new Dictionary<string, object>
                {
                    { "featureKey", feature.Key },
                    { "userId", context.Id }
                };
                foreach (var item in passedRulesInformation)
                {
                    storageMap[item.Key] = item.Value;
                }
                new StorageDecorator().SetDataInStorage(storageMap, storageService);
            }

            // Execute the integrations
            hookManager.Set(decision);
            hookManager.Execute(hookManager.Get());

            // If the feature has an impact campaign, send an impression for the variation shown
            if (feature.ImpactCampaign != null && feature.ImpactCampaign.CampaignId != null && feature.ImpactCampaign.CampaignId != 0)
            {
                LoggerService.Log(LogLevelEnum.INFO, "IMPACT_ANALYSIS", new Dictionary<string, string>
                {
                    { "userId", context.Id },
                    { "featureKey", featureKey },
                    { "status", getFlag.IsEnabled() ? "enabled" : "disabled" }
                });
                ImpressionUtil.CreateAndSendImpressionForVariationShown(
                    settings,
                    (int)feature.ImpactCampaign.CampaignId,
                    getFlag.IsEnabled() ? 2 : 1,
                    context
                );
            }
            return getFlag;
        }

        private static void UpdateIntegrationsDecisionObject(Campaign campaign, Variation variation, Dictionary<string, object> passedRulesInformation, Dictionary<string, object> decision)
        {
            if (campaign.Type == CampaignTypeEnum.ROLLOUT.ToString())
            {
                passedRulesInformation["rolloutId"] = campaign.Id;
                passedRulesInformation["rolloutKey"] = campaign.Name;
                passedRulesInformation["rolloutVariationId"] = variation.Id;
            }
            else
            {
                passedRulesInformation["experimentId"] = campaign.Id;
                passedRulesInformation["experimentKey"] = campaign.Key;
                passedRulesInformation["experimentVariationId"] = variation.Id;
            }
            foreach (var item in passedRulesInformation)
            {
                decision[item.Key] = item.Value;
            }
        }
    }
}