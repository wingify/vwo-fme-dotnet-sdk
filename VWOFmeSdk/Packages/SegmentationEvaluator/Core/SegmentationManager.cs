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
#pragma warning disable 1587

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VWOFmeSdk.Constants;
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.SegmentationEvaluator.Evaluators;
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;

namespace VWOFmeSdk.Packages.SegmentationEvaluator.Core
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
        public async Task SetContextualData(Settings settings, Feature feature, VWOContext context)
        {
            this.AttachEvaluator();
            this.evaluator.context = context;
            this.evaluator.settings = settings;
            this.evaluator.feature = feature;

            if (string.IsNullOrEmpty(context.UserAgent) && string.IsNullOrEmpty(context.IpAddress))
            {
                return;
            }
            
            if (feature.IsGatewayServiceRequired && !UrlService.GetBaseUrl().Contains(ConstantsNamespace.Constants.HOST_NAME) && (context.Vwo == null))
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
                    var response = GatewayServiceUtil.GetFromGatewayService(queryParamsEncoded, UrlEnum.GET_USER_DATA.GetUrl());
                    var gatewayServiceModel = JsonConvert.DeserializeObject<GatewayService>(response);
                    context.Vwo = gatewayServiceModel;
                }
                catch (Exception err)
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "Error in setting contextual data for segmentation. Got error: " + err);
                }
            }
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
                return evaluator.IsSegmentationValid(dslNodes, properties);
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "Exception occurred validate segmentation " + exception.Message);
                return false;
            }
        }
    }
}
