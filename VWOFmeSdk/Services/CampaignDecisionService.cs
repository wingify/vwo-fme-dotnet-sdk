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
using Newtonsoft.Json;
using VWOFmeSdk.Utils;

namespace VWOFmeSdk.Services
{
    public class CampaignDecisionService
    {
        /// <summary>
        /// Check if user is part of campaign
        /// </summary>
        /// <param name="context"></param>
        /// <param name="campaign"></param>
        public bool IsUserPartOfCampaign(VWOContext context, Campaign campaign)
        {
            var userId = context.Id;

            if (campaign == null || string.IsNullOrEmpty(userId))
            {
                return false;
            }
            
            var bucketingId = CampaignUtil.GetBucketingIdForUser(context);

            // Determine if the campaign is of type ROLLOUT or PERSONALIZE
            bool isRolloutOrPersonalize = campaign.Type == CampaignTypeEnum.ROLLOUT.GetValue() || campaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue();

            // Get the salt based on the campaign type
            string salt = isRolloutOrPersonalize ? campaign.Variations[0].Salt : campaign.Salt;

            // Get the traffic allocation based on the campaign type
            double trafficAllocation = isRolloutOrPersonalize ? campaign.Variations[0].Weight : campaign.PercentTraffic;

            // Build the bucket key
            // Generate bucket key using bucketing_id (custom seed or user_id) 
            string bucketKey = !string.IsNullOrEmpty(salt) ? $"{salt}_{bucketingId}" : $"{campaign.Id}_{bucketingId}";

            int valueAssignedToUser = new DecisionMaker().GetBucketValueForUser(bucketKey);
            bool isUserPart = valueAssignedToUser != 0 && valueAssignedToUser <= trafficAllocation;

            LoggerService.Log(LogLevelEnum.INFO, "USER_PART_OF_CAMPAIGN", new Dictionary<string, string>
            {
                {"userId", bucketingId != userId ? $"{userId} (Seed: {bucketingId})" : userId },
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
        /// <param name="context"></param>
        /// <param name="accountId"></param>
        /// <param name="campaign"></param>
        public Variation BucketUserToVariation(VWOContext context, string accountId, Campaign campaign)
        {
            var userId = context.Id;
            var bucketingId = CampaignUtil.GetBucketingIdForUser(context);

            if (campaign == null || string.IsNullOrEmpty(bucketingId))
            {
                return null;
            }

            int multiplier = campaign.PercentTraffic != 0 ? 1 : 0;
            int percentTraffic = campaign.PercentTraffic;

            // Get salt
            string salt = campaign.Salt;

            // Get bucket key
            string bucketKey = !string.IsNullOrEmpty(salt) ? $"{salt}_{accountId}_{bucketingId}" : $"{campaign.Id}_{accountId}_{bucketingId}";

            // Generate hash value
            long hashValue = new DecisionMaker().GenerateHashValue(bucketKey);
            int bucketValue = new DecisionMaker().GenerateBucketValue(hashValue, ConstantsNamespace.Constants.MAX_TRAFFIC_VALUE, multiplier);

            LoggerService.Log(LogLevelEnum.DEBUG, "USER_BUCKET_TO_VARIATION", new Dictionary<string, string>
            {
                {"userId", bucketingId != userId ? $"{userId} (Seed: {bucketingId})" : userId },
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
        /// <param name="context"></param>
        /// <param name="accountId"></param>
        /// <param name="campaign"></param>
        /// <returns></returns>
        public Variation GetVariationAllotted(VWOContext context, string accountId, Campaign campaign)
        {
            bool isUserPart = IsUserPartOfCampaign(context, campaign);
            if (campaign.Type == CampaignTypeEnum.ROLLOUT.GetValue() || campaign.Type == CampaignTypeEnum.PERSONALIZE.GetValue())
            {
                return isUserPart ? campaign.Variations[0] : null;
            }
            else
            {
                return isUserPart ? BucketUserToVariation(context, accountId, campaign) : null;
            }
        }
    }
}