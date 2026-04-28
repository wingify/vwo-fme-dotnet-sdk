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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VWOFmeSdk;
using VWOFmeSdk.Decorators;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Packages.DecisionMaker;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Packages.SegmentationEvaluator.Core;
using VWOFmeSdk.Services;
using VWOFmeSdk.Constants;
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Enums;

namespace VWOFmeSdk.Utils
{
    public static class HoldoutUtil
    {
        /// <summary>
        /// Gets the applicable holdouts for a given feature ID.
        /// </summary>
        /// <param name="settings">The settings object.</param>
        /// <param name="featureId">The feature ID.</param>
        /// <returns>The applicable holdouts.</returns>
        public static List<Holdout> GetApplicableHoldouts(Settings settings, int featureId)
        {
            var holdouts = settings.Holdouts ?? new List<Holdout>();
            // filter the holdouts to only include global holdouts and holdouts that have the given feature ID
            return holdouts.Where(holdout => holdout.IsGlobal || (holdout.FeatureIds != null && holdout.FeatureIds.Contains(featureId))).ToList();
        }

        /// <summary>
        /// Gets the matched holdout(s) for a given feature ID and context.
        /// Evaluates all applicable holdouts, creates batched impressions for all of them,
        /// and returns all matched holdouts (i.e. holdouts the user is part of).
        /// </summary>
        /// <param name="feature">The feature object.</param>
        /// <param name="context">The context object.</param>
        /// <param name="storedData">The stored data for the feature (from storage).</param>
        /// <returns>The matched holdouts (empty list if none are matched).</returns>
        public static Tuple<List<Holdout>, List<Holdout>, List<Dictionary<string, object>>> GetMatchedHoldouts(
            Settings settings,
            Feature feature,
            VWOContext context,
            Storage storedData)
        {
            if (settings == null) return null;

            // storedData has IsInHoldoutId and NotInHoldoutId, we need to use these to check if the holdout is already evaluated
            var isInHoldoutIds = storedData?.IsInHoldoutId ?? new List<int>();
            var notInHoldoutIds = storedData?.NotInHoldoutId ?? new List<int>();
            var alreadyEvaluatedHoldoutIds = new HashSet<int>(isInHoldoutIds.Concat(notInHoldoutIds));

            int featureId = feature.Id;
            string featureKey = feature.Key;

            // get the applicable holdouts for the given feature ID
            var applicableHoldouts = GetApplicableHoldouts(settings, featureId);

            // if there are no applicable holdouts, return empty list
            if (applicableHoldouts == null || applicableHoldouts.Count == 0)
            {
                return new Tuple<List<Holdout>, List<Holdout>, List<Dictionary<string, object>>>(new List<Holdout>(), new List<Holdout>(), new List<Dictionary<string, object>>());
            }

            var matchedHoldouts = new List<Holdout>();
            var notMatchedHoldouts = new List<Holdout>();
            var holdoutPayloads = new List<Dictionary<string, object>>();

            // iterate through the applicable holdouts
            // for each holdout, validate the segmentation and determine if user is IN or NOT IN
            foreach (var holdout in applicableHoldouts)
            {
                if (alreadyEvaluatedHoldoutIds.Contains(holdout.Id))
                {
                    LoggerService.Log(LogLevelEnum.DEBUG, "HOLDOUT_SKIP_EVALUATION", new Dictionary<string, string>
                    {
                        { "holdoutId", holdout.Id.ToString() },
                        {"reason", $"user {context.Id} was already evaluated for feature with id: {featureId}; SKIP decision making altogether."}
                    });
                    continue;
                }

                var segments = holdout.Segments ?? new Dictionary<string, object>();
                bool segmentPass = true;
                if (segments != null && segments.Count > 0)
                {
                    segmentPass = SegmentationManager.GetInstance().ValidateSegmentation(segments, context.CustomVariables);
                }
                else
                {
                    LoggerService.Log(LogLevelEnum.INFO, "HOLDOUT_SEGMENTATION_SKIP", new Dictionary<string, string>
                    {
                        { "holdoutId", holdout.Id.ToString() },
                        { "userId", context.Id }
                    });
                }

                // Determine variationId: 1 if IN holdout, 2 if NOT IN holdout
                int variationId;
                bool isInHoldout = false;

                // if the segmentation fails, user is NOT IN holdout (variationId = 2)
                if (!segmentPass)
                {
                    LoggerService.Log(LogLevelEnum.DEBUG, "HOLDOUT_SEGMENTATION_FAIL", new Dictionary<string, string>
                    {
                        { "userId", context.Id },
                        { "holdoutGroupName", holdout.Name }
                        
                    });
                    variationId = ConstantsNamespace.Constants.VARIATION_NOT_PART_OF_HOLDOUT; // NOT IN holdout
                    notMatchedHoldouts.Add(holdout);
                }
                else
                {
                    LoggerService.Log(LogLevelEnum.INFO, "SEGMENTATION_PASSED_HOLDOUT", new Dictionary<string, string>
                    {
                        { "holdoutId", holdout.Id.ToString() },
                        { "userId", context.Id }
                    });
                    // Check traffic allocation
                    string hashKey = $"{settings.AccountId}_{holdout.Id}_{context.Id}";
                    int bucket = new DecisionMaker().GetBucketValueForUser(hashKey, ConstantsNamespace.Constants.MAX_TRAFFIC_PERCENT);

                    // If bucket is within percentTraffic, user is IN holdout (variationId = 1)
                    // Otherwise, user is NOT IN holdout (variationId = 2)
                    isInHoldout = bucket != 0 && bucket <= holdout.PercentTraffic;
                    variationId = isInHoldout ? ConstantsNamespace.Constants.VARIATION_IS_PART_OF_HOLDOUT : ConstantsNamespace.Constants.VARIATION_NOT_PART_OF_HOLDOUT;

                    // Add all matched holdouts (user is IN)
                    if (isInHoldout)
                    {
                        LoggerService.Log(LogLevelEnum.INFO, "HOLDOUT_SHOULD_EXCLUDE_USER", new Dictionary<string, string>
                        {
                            { "userId", context.Id },
                            { "bucketValue", bucket.ToString() },
                            { "holdoutGroupName", holdout.Name },
                            { "percentTraffic", holdout.PercentTraffic.ToString() },
                            { "featureKey", featureKey }
                        });
                        matchedHoldouts.Add(holdout);
                    }
                    else
                    {
                        LoggerService.Log(LogLevelEnum.DEBUG, "HOLDOUT_SHOULD_NOT_EXCLUDE_USER", new Dictionary<string, string>
                        {
                            { "userId", context.Id },
                            { "holdoutGroupName", holdout.Name },
                            { "featureKey", featureKey }
                        });
                        notMatchedHoldouts.Add(holdout);
                    }
                }

                // Create holdout payload for ALL applicable holdouts (both IN and NOT IN)
                // campaignId is the holdoutId, variationId is 1 (IN) or 2 (NOT IN)
                var payload = NetworkUtil.CreateHoldoutPayload(
                    settings,
                    EventEnum.VWO_VARIATION_SHOWN.GetValue(),
                    holdout.Id,
                    variationId,
                    context,
                    featureId
                );

                
                holdoutPayloads.Add(payload);
            }

            return new Tuple<List<Holdout>, List<Holdout>, List<Dictionary<string, object>>>(matchedHoldouts, notMatchedHoldouts, holdoutPayloads);
        }

        /// <summary>
        /// Sends network calls for NOT IN holdouts that are applicable but not stored in storage.
        /// </summary>
        /// <param name="settings">The settings object.</param>
        /// <param name="feature">The feature model.</param>
        /// <param name="context">The context model.</param>
        /// <param name="decision">The decision dictionary to update (e.g. isHoldoutPresent).</param>
        /// <param name="storedData">The stored data for the feature (from storage).</param>
        /// <param name="storageService">The storage service.</param>
        public static List<int> SendNetworkCallsForNotInHoldouts(
            Settings settings,
            Feature feature,
            VWOContext context,
            Dictionary<string, object> decision,
            Storage storedData)
        {
            var applicableHoldouts = GetApplicableHoldouts(settings, feature.Id);
            var updatedNotInHoldoutIds = storedData?.NotInHoldoutId ?? new List<int>();
            var isInHoldoutIds = storedData?.IsInHoldoutId ?? new List<int>();
            var batchPayload = new List<Dictionary<string, object>>();
            int initialNotInHoldoutCount = updatedNotInHoldoutIds.Count;

            if (applicableHoldouts != null && applicableHoldouts.Count > 0)
            {
                decision["isHoldoutPresent"] = true;
            }


            //create payload for applicable holdouts that are not stored in storage
            foreach (var holdout in applicableHoldouts)
            {
                int holdoutId = holdout.Id;
                if (updatedNotInHoldoutIds.Contains(holdoutId) || isInHoldoutIds.Contains(holdoutId))
                {
                    continue;
                }

                updatedNotInHoldoutIds.Add(holdoutId);

                var payload = NetworkUtil.CreateHoldoutPayload(
                    settings,
                    EventEnum.VWO_VARIATION_SHOWN.GetValue(),
                    holdoutId,
                    ConstantsNamespace.Constants.VARIATION_NOT_PART_OF_HOLDOUT,
                    context,
                    feature.Id
                );
                if (SettingsManager.GetInstance().isGatewayServiceProvided) {
                    ImpressionUtil.SendImpressionForVariationShown(settings, holdoutId, ConstantsNamespace.Constants.VARIATION_NOT_PART_OF_HOLDOUT, context, feature.Key, payload);
                }
                else {
                    batchPayload.Add(payload);
                }
            }

            if (updatedNotInHoldoutIds.Count > initialNotInHoldoutCount)
            {
                new StorageDecorator().SetDataInStorage(
                    new Dictionary<string, object>
                    {
                        { "featureKey", feature.Key },
                        { "context", context },
                        { "userId", context.Id},
                        { "notInHoldoutId", updatedNotInHoldoutIds }
                    },
                    new StorageService()
                );
            }

            if (batchPayload.Count > 0)
            {
                var vwoInstance = VWO.GetInstance();
                if (vwoInstance.BatchEventQueue != null)
                {
                    foreach (var payload in batchPayload)
                    {
                        vwoInstance.BatchEventQueue.Enqueue(payload);
                    }
                }
                else
                {
                    NetworkUtil.SendPostBatchRequest(batchPayload, settings.AccountId, settings.SdkKey, null);
                }
            }

            return updatedNotInHoldoutIds;
        }
    }
}
