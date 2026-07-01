#pragma warning disable 1587
/**
 * Copyright 2024-2026 Wingify Software Pvt. Ltd.
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
#pragma warning disable 1587

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WingifyFmeSdk.Constants;
using ConstantsNamespace = WingifyFmeSdk.Constants;
using WingifyFmeSdk.Enums;
using WingifyFmeSdk.Models;
using WingifyFmeSdk.Models.User;
using WingifyFmeSdk.Packages.Logger.Enums;
using WingifyFmeSdk.Packages.SegmentationEvaluator.Evaluators;
using WingifyFmeSdk.Services;
using WingifyFmeSdk.Utils;
using WingifyFmeSdk.Packages.Logger.Core;
using WingifyFmeSdk.Enums;

namespace WingifyFmeSdk.Packages.SegmentationEvaluator.Core
{
    public class SegmentationManager
    {
        private static SegmentationManager instance;
        private SegmentEvaluator evaluator;

        public static SegmentationManager GetInstance()
        {
            if (instance == null)
            {
                instance = new SegmentationManager();
            }
            return instance;
        }

        public void AttachEvaluator(SegmentEvaluator segmentEvaluator)
        {
            this.evaluator = segmentEvaluator;
        }

        public void AttachEvaluator()
        {
            this.evaluator = new SegmentEvaluator();
        }

        /// <summary>
        /// This method sets the contextual data for the evaluator
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="feature"></param>
        /// <param name="context"></param>
        /// <returns></returns> 
        public async Task SetContextualData(Settings settings, Feature feature, WingifyContext context)
        {
            this.AttachEvaluator();
            this.evaluator.context = context;
            this.evaluator.settings = settings;
            this.evaluator.feature = feature;

            if (string.IsNullOrEmpty(context.UserAgent) && string.IsNullOrEmpty(context.IpAddress))
            {
                return;
            }

            var holdouts = settings.Holdouts ?? new List<Holdout>();
            bool isGatewayServiceRequiredForHoldouts = holdouts.Any(holdout => holdout.IsGatewayServiceRequired);

            bool segmentationRequiresGateway = feature.IsGatewayServiceRequired || isGatewayServiceRequiredForHoldouts;

            // Log an error if segmentation requires gateway but it is not configured; flow continues and segmentation will be skipped
            if (segmentationRequiresGateway && !SettingsManager.GetInstance().isGatewayServiceProvided)
            {
                LogManager.GetInstance().ErrorLog("GATEWAY_SERVICE_REQUIRED_BUT_NOT_CONFIGURED", new Dictionary<string, string>(), new Dictionary<string, object>(), false);
            }

            // get-user-details must only be called when:
            // 1. GatewayService is explicitly configured (never ProxyUrl)
            // 2. AND segmentation on this feature or a holdout actually requires it
            bool shouldCallGatewayService = SettingsManager.GetInstance().isGatewayServiceProvided && segmentationRequiresGateway;

            if (shouldCallGatewayService)
            {
                var queryParams = new Dictionary<string, string>();
                if (string.IsNullOrEmpty(context.UserAgent) && string.IsNullOrEmpty(context.IpAddress))
                {
                    return;
                }
                if (!string.IsNullOrEmpty(context.UserAgent))
                {
                    queryParams["userAgent"] = context.UserAgent;
                }

                if (!string.IsNullOrEmpty(context.IpAddress))
                {
                    queryParams["ipAddress"] = context.IpAddress;
                }

                try
                {
                    var queryParamsEncoded = GatewayServiceUtil.GetQueryParams(queryParams);
                    var response = GatewayServiceUtil.GetFromGatewayService(queryParamsEncoded, UrlEnum.GET_USER_DATA.GetUrl(), context);
                    var gatewayServiceModel = JsonConvert.DeserializeObject<GatewayService>(response);
                    context.Vwo = gatewayServiceModel;
                }
                catch (Exception err)
                {
                    LogManager.GetInstance().ErrorLog("ERROR_SETTING_SEGMENTATION_CONTEXT", 
                        new Dictionary<string, string> { { "err", FunctionUtil.GetFormattedErrorMessage(err as Exception) } },
                        new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue()}, { "uuid", context.VwoUuid }, { "sId", context.VwoSessionId } });
                }
            }
        }

        /// <summary>
        /// Recursively checks if any node in the DSL tree is a campaignVariation operand.
        /// </summary>
        /// <param name="dsl">The DSL JSON token to check.</param>
        /// <returns>True if a campaignVariation node is found, otherwise false.</returns>
        private bool HasCampaignVariationNode(JToken dsl)
        {
            // Base case: if it's not a JSON object, it cannot contain operators as keys.
            if (dsl == null || dsl.Type != JTokenType.Object)
            {
                return false;
            }

            foreach (var property in dsl.Children<JProperty>())
            {
                // If the current node's key is the target operand, return true immediately.
                if (property.Name == "web_campaign_variation" || property.Name == "campaignVariation")
                {
                    return true;
                }

                // If the value is an array (e.g. wrapper operators like "and": [ ... ]), recursively check each item.
                if (property.Value.Type == JTokenType.Array)
                {
                    foreach (var subDsl in property.Value.Children())
                    {
                        if (HasCampaignVariationNode(subDsl))
                        {
                            return true;
                        }
                    }
                }
                // If the value is another object, recurse into it.
                else if (property.Value.Type == JTokenType.Object)
                {
                    if (HasCampaignVariationNode(property.Value))
                    {
                        return true;
                    }
                }
            }
            
            // Reached the end of the tree without finding the target operand.
            return false;
        }

        /// <summary>
        /// This method evaluates the segmentation for the user
        /// </summary>
        /// <param name="dsl"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public bool ValidateSegmentation(object dsl, Dictionary<string, object> properties)
        {
            try
            {
                JToken dslNodes = dsl is string ? JToken.Parse(dsl.ToString()) : JToken.FromObject(dsl);

                // If the DSL contains any campaignVariation node but no webTestingCampaigns was provided, fail immediately.
                // This covers NOT/OR/AND wrappers too — there is no web testing data to evaluate against.
                if (HasCampaignVariationNode(dslNodes))
                {
                    var platformVariables = evaluator?.context?.PlatformVariables;
                    if (platformVariables == null || !platformVariables.ContainsKey("webTestingCampaigns") || platformVariables["webTestingCampaigns"] == null)
                    {
                        return false;
                    }
                }

                return evaluator.IsSegmentationValid(dslNodes, properties);
            }
           catch (Exception exception)
            {
                return false;
            }
        }
    }
}
