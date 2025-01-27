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
using VWOFmeSdk.Constants;
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Packages.DecisionMaker;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.SegmentationEvaluator.Core;

namespace VWOFmeSdk.Services
{
    public class CampaignDecisionService
    {
        /// <summary>
        /// Check if user is part of campaign
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="campaign"></param>
        public bool IsUserPartOfCampaign(string userId, Campaign campaign)
        {
            if (campaign == null || userId == null)
            {
                return false;
            }

            double trafficAllocation;
            if (campaign.Type == CampaignTypeEnum.ROLLOUT.GetValue() || campaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue())
            {
                trafficAllocation = campaign.Variations[0].Weight;
            }
            else
            {
                trafficAllocation = campaign.PercentTraffic;
            }

            int valueAssignedToUser = new DecisionMaker().GetBucketValueForUser(campaign.Id + "_" + userId);
            bool isUserPart = valueAssignedToUser != 0 && valueAssignedToUser <= trafficAllocation;

            LoggerService.Log(LogLevelEnum.INFO, "USER_PART_OF_CAMPAIGN", new Dictionary<string, string>
            {
                {"userId", userId},
                {"notPart", isUserPart ? "" : "not"},
                { "campaignKey", 
                    campaign.Type == CampaignTypeEnum.AB.GetValue() 
                    ? campaign.Key 
                    : campaign.Name + "_" + campaign.RuleKey 
                },
            });

            return isUserPart;
        }

        /// <summary>
        /// Get variation for user
        /// </summary>
        /// <param name="variations"></param>
        /// <param name="bucketValue"></param>
        public Variation GetVariation(List<Variation> variations, int bucketValue)
        {
            foreach (var variation in variations)
            {
                if (bucketValue >= variation.StartRangeVariation && bucketValue <= variation.EndRangeVariation)
                {
                    return variation;
                }
            }
            return null;
        }

        /// <summary>
        /// Check if user is in range of variation
        /// </summary>
        /// <param name="variation"></param>
        /// <param name="bucketValue"></param>
        public Variation CheckInRange(Variation variation, int bucketValue)
        {
            if (bucketValue >= variation.StartRangeVariation && bucketValue <= variation.EndRangeVariation)
            {
                return variation;
            }
            return null;
        }

        /// <summary>
        /// Bucket the user to a variation
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="accountId"></param>
        /// <param name="campaign"></param>
        public Variation BucketUserToVariation(string userId, string accountId, Campaign campaign)
        {
            if (campaign == null || userId == null)
            {
                return null;
            }

            int multiplier = campaign.PercentTraffic != 0 ? 1 : 0;
            int percentTraffic = campaign.PercentTraffic;
            long hashValue = new DecisionMaker().GenerateHashValue(campaign.Id + "_" + accountId + "_" + userId);
            int bucketValue = new DecisionMaker().GenerateBucketValue(hashValue, ConstantsNamespace.Constants.MAX_TRAFFIC_VALUE, multiplier);

            LoggerService.Log(LogLevelEnum.DEBUG, "USER_BUCKET_TO_VARIATION", new Dictionary<string, string>
            {
                {"userId", userId},
                { "campaignKey", 
                    campaign.Type == CampaignTypeEnum.AB.GetValue()
                    ? campaign.Key 
                    : campaign.Name + "_" + campaign.RuleKey 
                },
                {"percentTraffic", percentTraffic.ToString()},
                {"bucketValue", bucketValue.ToString()},
                {"hashValue", hashValue.ToString()}
            });

            return GetVariation(campaign.Variations, bucketValue);
        }

        /// <summary>
        /// Get the variation allotted to the user
        /// </summary>
        /// <param name="campaign"></param>
        /// <param name="context"></param>
        public bool GetPreSegmentationDecision(Campaign campaign, VWOContext context)
        {
            string campaignType = campaign.Type;
            Dictionary<string, object> segments;

            if (campaignType == CampaignTypeEnum.ROLLOUT.GetValue() || campaignType == CampaignTypeEnum.PERSONALIZE.GetValue())
            {
                segments = campaign.Variations[0].Segments;
            }
            else if (campaignType == CampaignTypeEnum.AB.GetValue())
            {
                segments = campaign.Segments;
            }
            else
            {
                segments = new Dictionary<string, object>();
            }

            if (segments.Count == 0)
            {
                LoggerService.Log(LogLevelEnum.INFO, "SEGMENTATION_SKIP", new Dictionary<string, string>
                {
                    {"userId", context.Id},
                    { "campaignKey", 
                        campaign.Type == CampaignTypeEnum.AB.GetValue() 
                        ? campaign.Key 
                        : campaign.Name + "_" + campaign.RuleKey 
                    }
                });
                return true;
            }
            else
            {
                bool preSegmentationResult = SegmentationManager.GetInstance().ValidateSegmentation(segments, context.CustomVariables as Dictionary<string, object>);
                LoggerService.Log(LogLevelEnum.INFO, "SEGMENTATION_STATUS", new Dictionary<string, string>
                {
                    {"userId", context.Id},
                    { "campaignKey", 
                        campaign.Type == CampaignTypeEnum.AB.GetValue() 
                        ? campaign.Key 
                        : campaign.Name + "_" + campaign.RuleKey 
                    },
                    {"status", preSegmentationResult ? "passed" : "failed"}
                });
                return preSegmentationResult;
            }
        }

        /// <summary>
        /// Get the variation allotted to the user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="accountId"></param>
        /// <param name="campaign"></param>
        /// <returns></returns>
        public Variation GetVariationAllotted(string userId, string accountId, Campaign campaign)
        {
            bool isUserPart = IsUserPartOfCampaign(userId, campaign);
            if (campaign.Type == CampaignTypeEnum.ROLLOUT.GetValue() || campaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue())
            {
                return isUserPart ? campaign.Variations[0] : null;
            }
            else
            {
                return isUserPart ? BucketUserToVariation(userId, accountId, campaign) : null;
            }
        }
    }
}