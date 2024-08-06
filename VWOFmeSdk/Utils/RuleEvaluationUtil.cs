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
using static VWOFmeSdk.Utils.DecisionUtil;
using static VWOFmeSdk.Utils.ImpressionUtil;

namespace VWOFmeSdk.Utils
{
    public static class RuleEvaluationUtil
    {
        /// <summary>
        /// This method checks if the user is whitelisted or not and if the user is part of the pre-segmentation.
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
        public static Dictionary<string, object> EvaluateRule(
            Settings settings,
            Feature feature,
            Campaign campaign,
            VWOContext context,
            Dictionary<string, object> evaluatedFeatureMap,
            Dictionary<int, int> megGroupWinnerCampaigns,
            StorageService storageService,
            Dictionary<string, object> decision)
        {
            try
            {
                var checkResult = CheckWhitelistingAndPreSeg(
                    settings,
                    feature,
                    campaign,
                    context,
                    evaluatedFeatureMap,
                    megGroupWinnerCampaigns,
                    storageService,
                    decision
                );

                bool preSegmentationResult = (bool)checkResult["preSegmentationResult"];
                var whitelistedObject = (Variation)checkResult["whitelistedObject"];

                if (preSegmentationResult && whitelistedObject != null && whitelistedObject.Id != null)
                {
                    decision["experimentId"] = campaign.Id;
                    decision["experimentKey"] = campaign.Key;
                    decision["experimentVariationId"] = whitelistedObject.Id;

                    ImpressionUtil.CreateAndSendImpressionForVariationShown(
                        settings,
                        campaign.Id,
                        whitelistedObject.Id,
                        context
                    );
                }

                return new Dictionary<string, object>
                {
                    { "preSegmentationResult", preSegmentationResult },
                    { "whitelistedObject", whitelistedObject },
                    { "updatedDecision", decision }
                };
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "Error occurred while evaluating rule: " + exception);
                return new Dictionary<string, object>();
            }
        }
    }
}
