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
using VWOFmeSdk.Utils;
using ConstantsNamespace = VWOFmeSdk.Constants;

namespace VWOFmeSdk.Utils
{
    public static class DecisionUtil
    {
        /// <summary>
        /// This method checks for whitelisting and pre-segmentation for a user in a campaign.
        /// </summary>
        /// <param name="settings">The settings object containing the account details.</param>
        /// <param name="feature">The feature associated with the campaign.</param>
        /// <param name="campaign">The campaign being evaluated.</param>
        /// <param name="context">The user context including user ID and custom variables.</param>
        /// <param name="evaluatedFeatureMap">A dictionary containing evaluated features for tracking.</param>
        /// <param name="megGroupWinnerCampaigns">A dictionary to track winning campaigns in mutually exclusive groups (MEG).</param>
        /// <param name="storageService">The storage service used for retrieving stored data.</param>
        /// <param name="decision">A dictionary to store decision-related variables.</param>
        /// <returns>A dictionary indicating whether pre-segmentation was successful and the whitelisted object if any.</returns>
        public static Dictionary<string, object> CheckWhitelistingAndPreSeg(
            Settings settings,
            Feature feature,
            Campaign campaign,
            VWOContext context,
            Dictionary<string, object> evaluatedFeatureMap,
            Dictionary<int, string> megGroupWinnerCampaigns,
            StorageService storageService,
            Dictionary<string, object> decision)
        {
            // Generate a unique user ID for VWO
            string vwoUserId = UUIDUtils.GetUUID(context.Id, settings.AccountId.ToString());
            int campaignId = campaign.Id;

            // Check if the campaign type is AB
            if (campaign.Type == CampaignTypeEnum.AB.GetValue())
            {
                // Set _vwoUserId for variation targeting variables
                context.VariationTargetingVariables["_vwoUserId"] = campaign.IsUserListEnabled ? vwoUserId : context.Id;
                decision["variationTargetingVariables"] = context.VariationTargetingVariables;

                // Check if the campaign satisfies the whitelisting criteria
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
                    // Log that whitelisting was skipped
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

            var variationId = campaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue()? (int?)campaign.Variations[0].Id : null;

            // Check if the campaign is part of a mutually exclusive group (MEG)
            var groupDetails = CampaignUtil.GetGroupDetailsIfCampaignPartOfIt(
                settings,
                campaignId,
                variationId);
            string groupId = groupDetails.ContainsKey("groupId") ? groupDetails["groupId"] : null;

            // Handle MEG (Mutually Exclusive Group) logic
            if (!string.IsNullOrEmpty(groupId))
            {
                int groupIdInt = int.Parse(groupId);
                if (megGroupWinnerCampaigns.TryGetValue(groupIdInt, out string groupWinnerCampaignId))
                {
                    if (campaign.Type == CampaignTypeEnum.AB.GetValue())
                    {
                        // Check if the campaign is the winner of the group
                        if (groupWinnerCampaignId == campaignId.ToString())
                        {
                            return new Dictionary<string, object>
                            {
                                {"preSegmentationResult", true},
                                {"whitelistedObject", null}
                            };
                        }
                    }
                    else if (campaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue())
                    {
                        // Check if the campaign is the winner of the group
                        if (groupWinnerCampaignId == campaignId.ToString() + "_" + campaign.Variations[0].Id.ToString())
                        {
                            return new Dictionary<string, object>
                            {
                                {"preSegmentationResult", true},
                                {"whitelistedObject", null}
                            };
                        }
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
                    var storedData = new StorageDecorator().GetFeatureFromStorage(
                        $"{ConstantsNamespace.Constants.VWO_META_MEG_KEY}{groupId}",
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
                        megGroupWinnerCampaigns[groupIdInt] = storedData["experimentId"].ToString();
                        return new Dictionary<string, object>
                        {
                            {"preSegmentationResult", false},
                            {"whitelistedObject", null}
                        };
                    }
                }
            }

            // If Whitelisting is skipped/failed and the campaign is not part of any MEG Groups
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
                    if (winnerCampaign.Type == CampaignTypeEnum.AB.GetValue())
                    {
                        return new Dictionary<string, object>
                        {
                            { "preSegmentationResult", true },
                            { "whitelistedObject", null }
                        };
                    }
                    else if (winnerCampaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue() && winnerCampaign.Variations[0].Id == campaign.Variations[0].Id)
                    {
                        return new Dictionary<string, object>
                        {
                            { "preSegmentationResult", true },
                            { "whitelistedObject", null }
                        };
                    }
                    else
                    {
                        // Update the MEG group winner campaigns map for Personalize
                        megGroupWinnerCampaigns[int.Parse(groupId)] = $"{winnerCampaign.Id}_{winnerCampaign.Variations[0].Id}";
                        return new Dictionary<string, object>
                        {
                            { "preSegmentationResult", false },
                            { "whitelistedObject", null }
                        };
                    }
                }
                else if (winnerCampaign != null)
                {
                    // Update the MEG group winner campaigns map
                    if (winnerCampaign.Type == CampaignTypeEnum.AB.GetValue())
                    {
                        megGroupWinnerCampaigns[int.Parse(groupId)] = winnerCampaign.Id.ToString();
                    }
                    else if (winnerCampaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue())
                    {
                        megGroupWinnerCampaigns[int.Parse(groupId)] = $"{winnerCampaign.Id}_{winnerCampaign.Variations[0].Id}";
                    }
                    return new Dictionary<string, object>
                    {
                        { "preSegmentationResult", false },
                        { "whitelistedObject", null }
                    };
                }

                // No winner found, mark group as evaluated with no winner
                megGroupWinnerCampaigns[int.Parse(groupId)] = "-1";
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
        /// This method evaluates the traffic for a user and returns the allotted variation.
        /// </summary>
        /// <param name="settings">The settings object containing the account details.</param>
        /// <param name="campaign">The campaign being evaluated.</param>
        /// <param name="userId">The unique ID assigned to the user.</param>
        /// <returns>The variation allotted to the user, or null if no variation is allotted.</returns>
        public static Variation EvaluateTrafficAndGetVariation(Settings settings, Campaign campaign, string userId)
        {
            // Get the variation allotted to the user
            var variation = new CampaignDecisionService().GetVariationAllotted(userId, settings.AccountId.ToString(), campaign);
            if (variation == null)
            {
                // Log that the user did not get any variation
                LoggerService.Log(LogLevelEnum.INFO, "USER_CAMPAIGN_BUCKET_INFO", new Dictionary<string, string>
                {
                    { "userId", userId },
                    { "campaignKey", campaign.RuleKey },
                    { "status", "did not get any variation" }
                });
                return null;
            }

            // Log that the user got a specific variation
            LoggerService.Log(LogLevelEnum.INFO, "USER_CAMPAIGN_BUCKET_INFO", new Dictionary<string, string>
            {
                { "userId", userId },
                { "campaignKey", campaign.RuleKey },
                { "status", $"got variation: {variation.Name}" }
            });
            return variation;
        }

        /// <summary>
        /// This method checks if the user is whitelisted for the campaign.
        /// </summary>
        /// <param name="campaign">The campaign being evaluated.</param>
        /// <param name="context">The user context including user ID and custom variables.</param>
        /// <returns>A dictionary containing the whitelisted variation and its details if the user is whitelisted, otherwise null.</returns>
        private static Dictionary<string, object> CheckCampaignWhitelisting(Campaign campaign, VWOContext context)
        {
            var whitelistingResult = EvaluateWhitelisting(campaign, context);
            var status = whitelistingResult != null ? StatusEnum.PASSED : StatusEnum.FAILED;
            var variationString = whitelistingResult?["variationName"]?.ToString() ?? string.Empty;

            // Log the result of whitelisting
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
        /// This method evaluates the whitelisting criteria for a user in a campaign.
        /// </summary>
        /// <param name="campaign">The campaign being evaluated.</param>
        /// <param name="context">The user context including user ID and custom variables.</param>
        /// <returns>A dictionary containing the whitelisted variation and its details if the user meets the whitelisting criteria, otherwise null.</returns>
        private static Dictionary<string, object> EvaluateWhitelisting(Campaign campaign, VWOContext context)
        {
            var targetedVariations = new List<Variation>();

            // Loop through each variation in the campaign
            foreach (var variation in campaign.Variations)
            {
                // Skip if the variation has no segments defined
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

                // Evaluate segmentation for each variation
                if (variation.Segments != null)
                {
                    var segmentationResult = SegmentationManager.GetInstance().ValidateSegmentation(
                        variation.Segments,
                        (Dictionary<string, object>)context.VariationTargetingVariables);
                    if (segmentationResult)
                    {
                        targetedVariations.Add(FunctionUtil.CloneObject(variation));
                    }
                }
            }

            Variation whitelistedVariation = null;

            // If multiple variations are targeted, scale the weights and get the variation
            if (targetedVariations.Count > 1)
            {
                CampaignUtil.ScaleVariationWeights(targetedVariations);
                int currentAllocation = 0;
                foreach (var variation in targetedVariations)
                {
                    int stepFactor = CampaignUtil.AssignRangeValues(variation, currentAllocation);
                    currentAllocation += stepFactor;
                }

                whitelistedVariation = new CampaignDecisionService().GetVariation(targetedVariations, new DecisionMaker().CalculateBucketValue(CampaignUtil.GetBucketingSeed(context.Id, campaign, null)));
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
