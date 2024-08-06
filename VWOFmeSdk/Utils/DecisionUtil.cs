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
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Services;
using VWOFmeSdk.Packages.DecisionMaker;
using static VWOFmeSdk.Utils.CampaignUtil;
using VWOFmeSdk.Packages.SegmentationEvaluator.Core;
using VWOFmeSdk.Decorators;

namespace VWOFmeSdk.Utils
{
    public static class DecisionUtil
    {
        /// <summary>
        /// Check if user is part of campaign
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="feature"></param>
        /// <param name="campaign"></param>
        /// <param name="context"></param>
        /// <param name="evaluatedFeatureMap"></param>
        /// <param name="megGroupWinnerCampaigns"></param>
        /// <param name="storageService"></param>
        /// <param name="decision"></param>
        /// <returns></returns>
        public static Dictionary<string, object> CheckWhitelistingAndPreSeg(
            Settings settings,
            Feature feature,
            Campaign campaign,
            VWOContext context,
            Dictionary<string, object> evaluatedFeatureMap,
            Dictionary<int, int> megGroupWinnerCampaigns,
            StorageService storageService,
            Dictionary<string, object> decision)
        {
            string vwoUserId = UUIDUtils.GetUUID(context.Id, settings.AccountId.ToString());
            int campaignId = campaign.Id;

            if (campaign.Type == CampaignTypeEnum.AB.GetValue())
            {
                // Set _vwoUserId for variation targeting variables
                context.VariationTargetingVariables["_vwoUserId"] = campaign.IsUserListEnabled ? vwoUserId : context.Id;
                decision["variationTargetingVariables"] = context.VariationTargetingVariables;

                // Check if the campaign satisfies the whitelisting
                if (campaign.IsForcedVariationEnabled)
                {
                    var whitelistedVariation = CheckCampaignWhitelisting(campaign, context);
                    if (whitelistedVariation != null)
                    {
                        return new Dictionary<string, object>
                        {
                            { "preSegmentationResult", true },
                            { "whitelistedObject", whitelistedVariation["variation"] }
                        };
                    }
                }
                else
                {
                    LoggerService.Log(LogLevelEnum.INFO, "WHITELISTING_SKIP", new Dictionary<string, string>
                    {
                        { "userId", context.Id },
                        { "campaignKey", campaign.RuleKey }
                    });
                }
            }

            // Set custom variables for pre-segmentation
            context.CustomVariables["_vwoUserId"] = campaign.IsUserListEnabled ? vwoUserId : context.Id;
            decision["customVariables"] = context.CustomVariables;

            // Check if the campaign is part of a mutually exclusive group (MEG)
            var variationId = campaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue()? (int?)campaign.Variations[0].Id : null;
            var groupDetails = CampaignUtil.GetGroupDetailsIfCampaignPartOfIt(settings, campaignId, variationId);
            string groupId = groupDetails.ContainsKey("groupId") ? groupDetails["groupId"] : null;

            if (!string.IsNullOrEmpty(groupId))
            {
                int groupIdInt = int.Parse(groupId);
                if (megGroupWinnerCampaigns.TryGetValue(groupIdInt, out int groupWinnerCampaignId))
                {
                    if (!string.IsNullOrEmpty(groupWinnerCampaignId.ToString()) && groupWinnerCampaignId == campaignId)
                    {
                        return new Dictionary<string, object>
                        {
                            {"preSegmentationResult", true},
                            {"whitelistedObject", null}
                        };
                    }
                    // As group is already evaluated, no need to check again, return false directly
                    return new Dictionary<string, object>
                    {
                        { "preSegmentationResult", false },
                        { "whitelistedObject", null }
                    };
                }
                else
                {
                    // Check in storage if the group is already evaluated for the user
                    var storedData = new StorageDecorator().GetFeatureFromStorage(
                        $"_vwo_meta_meg_{groupId}",
                        context,
                        storageService
                    );
                    if (storedData != null && storedData.ContainsKey("experimentKey") && storedData.ContainsKey("experimentId"))
                    {
                        LoggerService.Log(LogLevelEnum.INFO, "MEG_CAMPAIGN_FOUND_IN_STORAGE", new Dictionary<string, string>
                        {
                            { "campaignKey", storedData["experimentKey"].ToString() },
                            { "userId", context.Id }
                        });

                        if (storedData["experimentId"].ToString() == campaignId.ToString())
                        {
                            // Return the campaign if the called campaignId matches
                            return new Dictionary<string, object>
                            {
                                { "preSegmentationResult", true },
                                { "whitelistedObject", null }
                            };
                        }
                        megGroupWinnerCampaigns[groupIdInt] = int.Parse(storedData["experimentId"].ToString());
                        return new Dictionary<string, object>
                        {
                            {"preSegmentationResult", false},
                            {"whitelistedObject", null}
                        };
                    }
                }
            }

            // Check campaign's pre-segmentation if whitelisting is skipped/failed and campaign not part of any MEG groups
            bool isPreSegmentationPassed = new CampaignDecisionService().GetPreSegmentationDecision(campaign, context);

            if (isPreSegmentationPassed && !string.IsNullOrEmpty(groupId))
            {
                var winnerCampaign = MegUtil.EvaluateGroups(
                    settings,
                    feature,
                    int.Parse(groupId),
                    evaluatedFeatureMap,
                    context,
                    storageService
                );

                if (winnerCampaign != null && winnerCampaign.Id == campaignId)
                {
                    return new Dictionary<string, object>
                    {
                        { "preSegmentationResult", true },
                        { "whitelistedObject", null }
                    };
                }

                megGroupWinnerCampaigns[int.Parse(groupId)] = winnerCampaign?.Id ?? 0;
                return new Dictionary<string, object>
                {
                    { "preSegmentationResult", false },
                    { "whitelistedObject", null }
                };
            }

            return new Dictionary<string, object>
            {
                { "preSegmentationResult", isPreSegmentationPassed },
                { "whitelistedObject", null }
            };
        }

        /// <summary>
        /// Evaluate the traffic for the user and get the allotted variation
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="campaign"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static Variation EvaluateTrafficAndGetVariation(Settings settings, Campaign campaign, string userId)
        {
            var variation = new CampaignDecisionService().GetVariationAllotted(userId, settings.AccountId.ToString(), campaign);
            if (variation == null)
            {
                LoggerService.Log(LogLevelEnum.INFO, "USER_CAMPAIGN_BUCKET_INFO", new Dictionary<string, string>
                {
                    { "userId", userId },
                    { "campaignKey", campaign.RuleKey },
                    { "status", "did not get any variation" }
                });
                return null;
            }

            LoggerService.Log(LogLevelEnum.INFO, "USER_CAMPAIGN_BUCKET_INFO", new Dictionary<string, string>
            {
                { "userId", userId },
                { "campaignKey", campaign.RuleKey },
                { "status", $"got variation: {variation.Name}" }
            });
            return variation;
        }

        /// <summary>
        /// Check if the user is whitelisted for the campaign
        /// </summary>
        /// <param name="campaign"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static Dictionary<string, object> CheckCampaignWhitelisting(Campaign campaign, VWOContext context)
        {
            var whitelistingResult = EvaluateWhitelisting(campaign, context);
            var status = whitelistingResult != null ? StatusEnum.PASSED : StatusEnum.FAILED;
            var variationString = whitelistingResult?["variationName"]?.ToString() ?? string.Empty;

            LoggerService.Log(LogLevelEnum.INFO, "WHITELISTING_STATUS", new Dictionary<string, string>
            {
                { "userId", context.Id },
                { "campaignKey", campaign.RuleKey },
                { "status", status.GetStatus() },
                { "variationString", variationString }
            });

            return whitelistingResult;
        }

        /// <summary>
        /// Evaluate the whitelisting for the user
        /// </summary>
        /// <param name="campaign"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static Dictionary<string, object> EvaluateWhitelisting(Campaign campaign, VWOContext context)
        {
            var targetedVariations = new List<Variation>();

            foreach (var variation in campaign.Variations)
            {
                if (variation.Segments != null && variation.Segments.Count == 0)
                {
                    LoggerService.Log(LogLevelEnum.INFO, "WHITELISTING_SKIP", new Dictionary<string, string>
                    {
                        { "userId", context.Id },
                        { "campaignKey", campaign.RuleKey },
                        { "variation", !string.IsNullOrEmpty(variation.Name) ? $"for variation: {variation.Name}" : "" }
                    });
                    continue;
                }

                if (variation.Segments != null)
                {
                    var segmentationResult = SegmentationManager.GetInstance().ValidateSegmentation(variation.Segments, (Dictionary<string, object>)context.VariationTargetingVariables);
                    if (segmentationResult)
                    {
                        targetedVariations.Add(FunctionUtil.CloneObject(variation));
                    }
                }
            }

            Variation whitelistedVariation = null;

            if (targetedVariations.Count > 1)
            {
                ScaleVariationWeights(targetedVariations);
                int currentAllocation = 0;
                foreach (var variation in targetedVariations)
                {
                    int stepFactor = AssignRangeValues(variation, currentAllocation);
                    currentAllocation += stepFactor;
                }

                whitelistedVariation = new CampaignDecisionService().GetVariation(targetedVariations, new DecisionMaker().CalculateBucketValue(GetBucketingSeed(context.Id, campaign, null)));
            }
            else if (targetedVariations.Count == 1)
            {
                whitelistedVariation = targetedVariations[0];
            }

            if (whitelistedVariation != null)
            {
                return new Dictionary<string, object>
                {
                    { "variation", whitelistedVariation },
                    { "variationName", whitelistedVariation.Name },
                    { "variationId", whitelistedVariation.Id }
                };
            }

            return null;
        }
    }
}
