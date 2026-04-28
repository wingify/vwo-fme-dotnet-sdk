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
using VWOFmeSdk.Packages.Logger.Core;
using ConstantsNamespace = VWOFmeSdk.Constants;
using Newtonsoft.Json.Linq;

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
            List<int> notInHoldoutIds = new List<int>();

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
                { "api", apiValue },
                { "holdoutIDs", new List<int>() },
                { "isPartOfHoldout", false },
                { "isHoldoutPresent", false },
                { "isUserPartOfCampaign", false }
            };

              // create debug event props
            Dictionary<string, object> debugEventProps = new Dictionary<string, object>
            {
                { "an", ApiEnum.GET_FLAG.GetValue() },
                { "uuid", context?.GetUuid() },
                { "fk", feature?.Key },
                { "sId", context?.GetSessionId() }
            };


            StorageService storageService = new StorageService();
            Dictionary<string, object> storedDataMap = new StorageDecorator().GetFeatureFromStorage(featureKey, context, storageService);

            // If feature is found in the storage, return the stored variation
            try
            {
                string storageMapAsString = JsonConvert.SerializeObject(storedDataMap);
                Storage storedData = JsonConvert.DeserializeObject<Storage>(storageMapAsString);

                // if storedData has isInHoldoutId, then check if the settings stil contain atleast 1 holdoutGroup that is present in the storedData
                List<int> storedIsInHoldoutId = storedData?.IsInHoldoutId ?? new List<int>();
                List<int> storedNotInHoldoutId = storedData?.NotInHoldoutId ?? new List<int>();

                if (storedIsInHoldoutId != null && storedIsInHoldoutId.Count > 0) {
                    List<Holdout> applicableHoldouts = HoldoutUtil.GetApplicableHoldouts(settings, feature.Id);

                    if (applicableHoldouts.Count > 0) {
                        foreach (var holdout in applicableHoldouts) {
                            // if the holdout id is present in the storedData, then return the disabled flag
                            if (storedIsInHoldoutId.Contains(holdout.Id)) {
                                LoggerService.Log(LogLevelEnum.INFO, "STORED_HOLDOUT_DECISION_FOUND", new Dictionary<string, string> { { "featureKey", featureKey }, { "userId", context.Id }, { "holdoutId", holdout.Id.ToString() } });

                                // evaluate the new holdouts in settings file and send the impression for them
                                // destructuring the result to get the matched holdouts, not matched holdouts and holdout payloads
                                Tuple<List<Holdout>, List<Holdout>, List<Dictionary<string, object>>> matchedHoldoutsResult = HoldoutUtil.GetMatchedHoldouts(settings, feature, context, storedData);
                                List<Holdout> matchedHoldouts = matchedHoldoutsResult.Item1 ?? new List<Holdout>();
                                List<Holdout> notMatchedHoldouts = matchedHoldoutsResult.Item2 ?? new List<Holdout>();
                                List<Dictionary<string, object>> holdoutPayloads = matchedHoldoutsResult.Item3 ?? new List<Dictionary<string, object>>();
                                
                                // updatedHoldoutIds is the array of holdout ids for which user became part of the holdouts
                                List<int> updatedHoldoutIds = storedIsInHoldoutId.Concat(matchedHoldouts.Select(matchedHoldout => matchedHoldout.Id)).ToList();
                                // updatedNotInHoldoutIds is the array of holdout ids for which user became not part of the holdouts
                                List<int> updatedNotInHoldoutIds = storedNotInHoldoutId.Concat(notMatchedHoldouts.Select(notMatchedHoldout => notMatchedHoldout.Id)).ToList();

                                // store the updated holdout ids in storage and push the updated not in holdout ids to the notInHoldoutIds array
                                new StorageDecorator().SetDataInStorage(
                                new Dictionary<string, object>
                                {
                                    { "featureKey", featureKey },
                                    { "context", context },
                                    { "userId", context.Id},
                                    { "isInHoldoutId", updatedHoldoutIds },
                                    { "notInHoldoutId", updatedNotInHoldoutIds },
                                }, storageService );

                                if (SettingsManager.GetInstance().isGatewayServiceProvided) {
                                    foreach (var payload in holdoutPayloads) {
                                        if (payload != null &&
                                            payload.TryGetValue("d", out var dObj) && dObj is Dictionary<string, object> dDict &&
                                            dDict.TryGetValue("event", out var eventObj) && eventObj is Dictionary<string, object> eventDict &&
                                            eventDict.TryGetValue("props", out var propsObj) && propsObj is Dictionary<string, object> propsDict &&
                                            propsDict.TryGetValue("id", out var idObj) && int.TryParse(idObj?.ToString(), out var campaignId) &&
                                            propsDict.TryGetValue("variation", out var variationObj) && int.TryParse(variationObj?.ToString(), out var variationId)) {
                                            ImpressionUtil.SendImpressionForVariationShown(settings, campaignId, variationId, context, feature.Key, payload);
                                        }
                                    }
                                } else{
                                    var vwoInstance = VWO.GetInstance();
                                    if (vwoInstance.BatchEventQueue != null) {
                                        foreach (var payload in holdoutPayloads) {
                                            vwoInstance.BatchEventQueue.Enqueue(payload);
                                        }
                                    }
                                    else {
                                        NetworkUtil.SendPostBatchRequest(holdoutPayloads, settings.AccountId, settings.SdkKey, null);
                                    }
                                }

                                getFlag.SetIsEnabled(false);
                                return getFlag;
                            }
                        }
                    }
                    
                } 
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
                            decision["isUserPartOfCampaign"] = true;

                            // network calls for holdouts that are newly added in settings and are not present in storage
                            HoldoutUtil.SendNetworkCallsForNotInHoldouts(
                                settings,
                                feature,
                                context,
                                decision,
                                storedData
                            );
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
                        decision["isUserPartOfCampaign"] = true;

                        LoggerService.Log(LogLevelEnum.DEBUG, "EXPERIMENTS_EVALUATION_WHEN_ROLLOUT_PASSED", new Dictionary<string, string>
                        {
                            { "userId", context.Id }
                        });
                        // network calls for holdouts that are newly added in settings and are not present in storage
                        List<int> updatedNotInHoldoutIds = HoldoutUtil.SendNetworkCallsForNotInHoldouts(
                            settings,
                            feature,
                            context,
                            decision,
                            storedData
                        );
                        
                        // push/append the updated not in holdout ids to the notInHoldoutIds array
                        notInHoldoutIds.AddRange(updatedNotInHoldoutIds);

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
                LogManager.GetInstance().ErrorLog("ERROR_PARSING_STORED_DATA", new Dictionary<string, string> { { "err", e.Message } }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } });
            }

            // If feature is not found, return false
            if (feature == null)
            {
                LogManager.GetInstance().ErrorLog("FEATURE_NOT_FOUND", new Dictionary<string, string> { { "featureKey", featureKey } }, debugEventProps);
                getFlag.SetIsEnabled(false);
                return getFlag;
            }

            SegmentationManager.GetInstance().SetContextualData(settings, feature, context);

            if(!getFlag.IsEnabled()) {
                // Holdout group exclusion: if user falls into any holdout group for this feature, return disabled
                string storageMapAsString = JsonConvert.SerializeObject(storedDataMap);
                Storage storedData = JsonConvert.DeserializeObject<Storage>(storageMapAsString);
                Tuple<List<Holdout>, List<Holdout>, List<Dictionary<string, object>>> matchedHoldoutsResult = HoldoutUtil.GetMatchedHoldouts(settings, feature, context, storedData);
                List<Holdout> matchedHoldouts = matchedHoldoutsResult.Item1 ?? new List<Holdout>();
                List<Holdout> notMatchedHoldouts = matchedHoldoutsResult.Item2 ?? new List<Holdout>();
                List<Dictionary<string, object>> holdoutPayloads = matchedHoldoutsResult.Item3 ?? new List<Dictionary<string, object>>();

                decision["isPartOfHoldout"] = matchedHoldouts != null && matchedHoldouts.Count > 0;
                if ( (matchedHoldouts != null && matchedHoldouts.Count > 0) || (notMatchedHoldouts != null && notMatchedHoldouts.Count > 0)) {
                    decision["isHoldoutPresent"] = true;
                }

                if (matchedHoldouts != null && matchedHoldouts.Count > 0)
                {
                    string qualifiedHoldoutNames = string.Join(",", matchedHoldouts.Select(holdout => holdout.Name));
                    decision["holdoutIDs"] = matchedHoldouts.Select(holdout => holdout.Id).ToList();

                    LoggerService.Log(LogLevelEnum.INFO, "USER_IN_HOLDOUT_GROUP", new Dictionary<string, string>
                    {
                        { "userId", context.Id },
                        { "holdoutGroupName", qualifiedHoldoutNames },
                        { "featureKey", featureKey }
                    });

                    new StorageDecorator().SetDataInStorage(new Dictionary<string, object>
                    {
                        { "featureKey", featureKey },
                        { "context", context },
                        { "userId", context.Id},
                        { "isInHoldoutId", matchedHoldouts.Select(holdout => holdout.Id).ToList() },
                        { "notInHoldoutId", notMatchedHoldouts.Select(holdout => holdout.Id).ToList() }
                    }, storageService);


                    hookManager.Set(decision);
                    hookManager.Execute(hookManager.Get());

                    if (SettingsManager.GetInstance().isGatewayServiceProvided) {
                        foreach (var payload in holdoutPayloads)
                        {
                            if (payload == null) continue;

                            var payloadObject = JObject.FromObject(payload);

                            var propsObject = payloadObject["d"]?["event"]?["props"];
                            if (propsObject == null) continue;

                            var campaignIdToken = propsObject["id"];
                            var variationIdToken = propsObject["variation"];

                            if (campaignIdToken == null || variationIdToken == null) continue;

                            if (int.TryParse(campaignIdToken.ToString(), out var campaignId) &&
                                int.TryParse(variationIdToken.ToString(), out var variationId))
                            {

                                ImpressionUtil.SendImpressionForVariationShown( settings, campaignId, variationId, context, feature.Key, payload);
                            }
                        }
                    } else {
                        var vwoInstance = VWO.GetInstance();
                        if (vwoInstance.BatchEventQueue != null) {
                            foreach (var payload in holdoutPayloads) {
                                vwoInstance.BatchEventQueue.Enqueue(payload);
                            }
                        }
                        else {
                            NetworkUtil.SendPostBatchRequest(holdoutPayloads, settings.AccountId, settings.SdkKey, null);
                        }
                    }

                    getFlag.SetIsEnabled(false);
                    return getFlag;
                }
                else
                {
                    LoggerService.Log(LogLevelEnum.INFO, "USER_NOT_EXCLUDED_DUE_TO_HOLDOUT", new Dictionary<string, string>
                    {
                        { "featureKey", featureKey },
                        { "userId", context.Id }
                    });
                    notInHoldoutIds.AddRange(notMatchedHoldouts.Select(holdout => holdout.Id).ToList());
                    // send impression for the not in holdout ids
                    if (SettingsManager.GetInstance().isGatewayServiceProvided) {
                        foreach (var payload in holdoutPayloads) {
                            if (payload == null) continue;

                            var payloadObject = JObject.FromObject(payload);
                            var propsObject = payloadObject["d"]?["event"]?["props"];
                            if (propsObject == null) continue;

                            var campaignIdToken = propsObject["id"];
                            var variationIdToken = propsObject["variation"];

                            if (campaignIdToken == null || variationIdToken == null) continue;

                            if (int.TryParse(campaignIdToken.ToString(), out var campaignId) &&
                                int.TryParse(variationIdToken.ToString(), out var variationId)) {
                                ImpressionUtil.SendImpressionForVariationShown(settings, campaignId, variationId, context, feature.Key, payload);
                            }
                        }
                    } else {
                        var vwoInstance = VWO.GetInstance();
                        if (vwoInstance.BatchEventQueue != null) {
                            foreach (var payload in holdoutPayloads) {
                                vwoInstance.BatchEventQueue.Enqueue(payload);
                            }
                        }
                        else {
                            NetworkUtil.SendPostBatchRequest(holdoutPayloads, settings.AccountId, settings.SdkKey, null);
                        }
                    }
                }
            }

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
                    Variation variation = DecisionUtil.EvaluateTrafficAndGetVariation(settings, passedRolloutCampaign, context);
                    if (variation != null)
                    {
                        getFlag.SetIsEnabled(true);
                        decision["isUserPartOfCampaign"] = true;
                        getFlag.Variables = variation.Variables;
                        shouldCheckForExperimentsRules = true;
                        UpdateIntegrationsDecisionObject(passedRolloutCampaign, variation, passedRulesInformation, decision);
                        ImpressionUtil.CreateAndSendImpressionForVariationShown(settings, passedRolloutCampaign.Id, variation.Id, context, featureKey);
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
                            decision["isUserPartOfCampaign"] = true;
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
                    Variation variation = DecisionUtil.EvaluateTrafficAndGetVariation(settings, campaign, context);
                    if (variation != null)
                    {
                        getFlag.SetIsEnabled(true);
                        decision["isUserPartOfCampaign"] = true;
                        getFlag.Variables = variation.Variables;
                        UpdateIntegrationsDecisionObject(campaign, variation, passedRulesInformation, decision);
                        ImpressionUtil.CreateAndSendImpressionForVariationShown(settings, campaign.Id, variation.Id, context, featureKey);
                    }
                }
            }

            if (getFlag.IsEnabled())
            {
                Dictionary<string, object> storageMap = new Dictionary<string, object>
                {
                    { "featureKey", feature.Key },
                    { "context", context },
                    { "userId", context.Id },
                    { "notInHoldoutId", notInHoldoutIds },
                };
                foreach (var item in passedRulesInformation)
                {
                    storageMap[item.Key] = item.Value;
                }
                new StorageDecorator().SetDataInStorage(storageMap, storageService);
            } else {
                new StorageDecorator().SetDataInStorage(
                    new Dictionary<string, object>
                    {
                        { "featureKey", feature.Key },
                        { "context", context },
                        { "userId", context.Id},
                        { "notInHoldoutId", notInHoldoutIds }
                    },
                    storageService
                );
                
            }

            // Execute the integrations
            hookManager.Set(decision);
            hookManager.Execute(hookManager.Get());


            // Send debug event, if debugger is enabled
            if (feature != null && feature.IsDebuggerEnabled)
            {
                debugEventProps["cg"] = DebuggerCategoryEnum.DECISION.GetValue();
                debugEventProps["lt"] = LogLevelEnum.INFO.ToString().ToLower();
                debugEventProps["msg_t"] = ConstantsNamespace.Constants.FLAG_DECISION_GIVEN;
                
                // Update debug event props with decision keys
                UpdateDebugEventProps(debugEventProps, decision);

                // Send debug event
                DebuggerServiceUtil.SendDebugEventToVWO(debugEventProps);
            }

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
                    context,
                    featureKey
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

        /// <summary>
        /// Update debug event props with decision keys
        /// </summary>
        /// <param name="debugEventProps">Debug event props</param>
        /// <param name="decision">Decision</param>
        private static void UpdateDebugEventProps(Dictionary<string, object> debugEventProps, Dictionary<string, object> decision)
        {
            Dictionary<string, object> decisionKeys = DebuggerServiceUtil.ExtractDecisionKeys(decision);
            string message = $"Flag decision given for feature:{decision["featureKey"]}.";
            
            if (decision.ContainsKey("rolloutKey") && decision["rolloutKey"] != null && 
                decision.ContainsKey("rolloutVariationId") && decision["rolloutVariationId"] != null)
            {
                string rolloutKey = decision["rolloutKey"].ToString();
                string featureKey = decision["featureKey"].ToString();
                string rolloutKeySuffix = rolloutKey.Substring((featureKey + "_").Length);
                message += $" Got rollout:{rolloutKeySuffix} with variation:{decision["rolloutVariationId"]}";
            }
            
            if (decision.ContainsKey("experimentKey") && decision["experimentKey"] != null && 
                decision.ContainsKey("experimentVariationId") && decision["experimentVariationId"] != null)
            {
                string experimentKey = decision["experimentKey"].ToString();
                string featureKey = decision["featureKey"].ToString();
                string experimentKeySuffix = experimentKey.Substring((featureKey + "_").Length);
                message += $" and experiment:{experimentKeySuffix} with variation:{decision["experimentVariationId"]}";
            }
            
            debugEventProps["msg"] = message;
            
            foreach (var kvp in decisionKeys)
            {
                debugEventProps[kvp.Key] = kvp.Value;
            }
        }
    }
}