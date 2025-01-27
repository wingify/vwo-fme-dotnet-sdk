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
using VWOFmeSdk.Interfaces.Storage;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Models;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Services;
using VWOFmeSdk.Packages.Logger.Enums;


namespace VWOFmeSdk.Decorators
{
    public class StorageDecorator : IStorageDecorator
    {
        /// <summary>
        ///  Get feature from storage
        /// </summary>
        /// <param name="featureKey"></param>
        /// <param name="context"></param>
        /// <param name="storageService"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetFeatureFromStorage(string featureKey, VWOContext context, StorageService storageService)
        {
            return storageService.GetDataInStorage(featureKey, context);
        }

        /// <summary>
        /// Set data in storage
        /// </summary>
        /// <param name="data"></param>
        /// <param name="storageService"></param>
        /// <returns></returns>
        public Variation SetDataInStorage(Dictionary<string, object> data, StorageService storageService)
        {
            string featureKey = data["featureKey"] as string;
            string userId = data["userId"].ToString();

            if (string.IsNullOrEmpty(featureKey))
            {
                LoggerService.Log(LogLevelEnum.ERROR, "STORING_DATA_ERROR", new Dictionary<string, string>
                {
                    { "key", "featureKey" }
                });
                return null;
            }

            if (string.IsNullOrEmpty(userId))
            {
                LoggerService.Log(LogLevelEnum.ERROR, "STORING_DATA_ERROR", new Dictionary<string, string>
                {
                    { "key", "Context or Context.id" }
                });
                return null;
            }

            string rolloutKey = data.ContainsKey("rolloutKey") ? data["rolloutKey"] as string : null;
            string experimentKey = data.ContainsKey("experimentKey") ? data["experimentKey"] as string : null;
            int? rolloutVariationId = data.ContainsKey("rolloutVariationId") ? (int?)data["rolloutVariationId"] : null;
            int? experimentVariationId = data.ContainsKey("experimentVariationId") ? (int?)data["experimentVariationId"] : null;

            if (!string.IsNullOrEmpty(rolloutKey) && string.IsNullOrEmpty(experimentKey) && rolloutVariationId == null)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "STORING_DATA_ERROR", new Dictionary<string, string>
                {
                    { "key", "Variation:(rolloutKey, experimentKey or rolloutVariationId)" }
                });
                return null;
            }

            if (!string.IsNullOrEmpty(experimentKey) && experimentVariationId == null)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "STORING_DATA_ERROR", new Dictionary<string, string>
                {
                    { "key", "Variation:(experimentKey or rolloutVariationId)" }
                });
                return null;
            }

            storageService.SetDataInStorage(data);

            return new Variation(); // Assuming you need to return a new Variation instance.
        }
    }
}
