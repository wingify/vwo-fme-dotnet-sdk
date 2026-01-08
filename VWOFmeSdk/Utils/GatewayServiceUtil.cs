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
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.NetworkLayer.Manager;
using VWOFmeSdk.Packages.NetworkLayer.Models;
using VWOFmeSdk.Services;
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Enums;

namespace VWOFmeSdk.Utils
{
    public static class GatewayServiceUtil
    {
        /**
         * Fetches data from the gateway service
         * @param queryParams The query parameters to send with the request
         * @param endpoint The endpoint to send the request to
         * @return The response data from the gateway service
         */
        public static string GetFromGatewayService(Dictionary<string, string> queryParams, string endpoint, VWOContext context)
        {
            NetworkManager networkInstance = NetworkManager.GetInstance();
            if (UrlService.GetBaseUrl().Contains(ConstantsNamespace.Constants.HOST_NAME))
            {
                LogManager.GetInstance().ErrorLog(
                    "INVALID_GATEWAY_URL",
                    new Dictionary<string, string> { },
                    new Dictionary<string, object>
                    {
                        { "an", ApiEnum.GET_FLAG.GetValue() },
                        { "uuid", context.VwoUuid },
                        { "sId", context.VwoSessionId }
                    }
                );
                return null;
            }

            try
            {
                RequestModel request = new RequestModel(
                    UrlService.GetBaseUrl(),
                    "GET",
                    endpoint,
                    queryParams,
                    null,
                    null,
                    SettingsManager.GetInstance().Protocol,
                    SettingsManager.GetInstance().Port
                );

                ResponseModel response = networkInstance.Get(request);
                if (response.GetStatusCode() != 200)
                {
                    LogManager.GetInstance().ErrorLog(
                        "ERROR_SETTING_SEGMENTATION_CONTEXT", 
                        new Dictionary<string, string> { { "err", response.GetError().ToString() } },
                        new Dictionary<string, object>
                        {
                            { "an", ApiEnum.GET_FLAG.GetValue() },
                            { "uuid", context.VwoUuid },
                            { "sId", context.VwoSessionId }
                        }
                    );
                    return null;
                }

                return response.GetData();
            }
            catch (Exception e)
            {
                LogManager.GetInstance().ErrorLog(
                    "ERROR_SETTING_SEGMENTATION_CONTEXT", 
                    new Dictionary<string, string> { { "err", FunctionUtil.GetFormattedErrorMessage(e) } },
                    new Dictionary<string, object>
                    {
                        { "an", ApiEnum.GET_FLAG.GetValue() },
                        { "uuid", context.VwoUuid },
                        { "sId", context.VwoSessionId }
                    }
                );
                return null;
            }
        }

        /**
         * Encodes the query parameters to ensure they are URL-safe
         * @param queryParams The query parameters to encode
         * @return The encoded query parameters
         */
        public static Dictionary<string, string> GetQueryParams(Dictionary<string, string> queryParams)
        {
            var encodedParams = new Dictionary<string, string>();

            foreach (var entry in queryParams)
            {
                var encodedValue = Uri.EscapeDataString(entry.Value);
                encodedParams.Add(entry.Key, encodedValue);
            }

            return encodedParams;
        }

        private static string GetQueryString(Dictionary<string, string> parameters)
        {
            var queryString = string.Empty;

            foreach (var parameter in parameters)
            {
                if (!string.IsNullOrEmpty(queryString))
                {
                    queryString += "&";
                }
                queryString += $"{parameter.Key}={parameter.Value}";
            }

            return queryString;
        }
    }
}
