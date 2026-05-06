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
using Newtonsoft.Json.Linq;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.NetworkLayer.Manager;
using VWOFmeSdk.Packages.NetworkLayer.Models;
using VWOFmeSdk.Services;
using ConstantsNamespace = VWOFmeSdk.Constants;

namespace VWOFmeSdk.Utils
{
    /**
     * Utility class for handling alias operations through network calls to gateway
     */
    public static class AliasingUtil
    {

        /**
         * Retrieves alias for a given user ID
         * @param userId The user identifier
         * @return Returns alias string on success, or the original userId as fallback
         */
        public static string GetAlias(string userId)
        {
            try
            {
                var queryParams = new Dictionary<string, string>();
                queryParams["accountId"] = SettingsManager.GetInstance().AccountId.ToString();
                queryParams["sdkKey"] = SettingsManager.GetInstance().SdkKey;
                // Backend expects userId as JSON array
                queryParams[ConstantsNamespace.Constants.KEY_USER_ID] = "[\"" + userId + "\"]";

                var request = new RequestModel(
                    SettingsManager.GetInstance().hostname,
                    "GET",
                    UrlService.GetEndpointWithCollectionPrefix(UrlEnum.GET_ALIAS.GetUrl()),
                    queryParams,
                    null,
                    null,
                    SettingsManager.GetInstance().Protocol,
                    SettingsManager.GetInstance().Port
                );

                var response = NetworkManager.GetInstance().Get(request);
                if (response == null || response.GetStatusCode() != ConstantsNamespace.Constants.HTTP_OK)
                {
                    return userId;
                }

                var data = response.GetData();
                if (string.IsNullOrEmpty(data))
                {
                    return userId;
                }

                // Expecting JSON array of objects and alias is at index 0's userId
                try
                {
                    var token = JToken.Parse(data);
                    if (token is JArray arr && arr.Count > 0)
                    {
                        var aliasIdFromResponse = arr[0]?[ConstantsNamespace.Constants.KEY_USER_ID]?.ToString();
                        if (!string.IsNullOrEmpty(aliasIdFromResponse))
                        {
                            return aliasIdFromResponse;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "ALIAS_RESPONSE_PARSE_FAILED", new Dictionary<string, string> { { "err", ex.ToString() } });
                }

                return userId;
            }
            catch (Exception err)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "GATEWAY_SERVICE_ERROR", new Dictionary<string, string> { { "err", err.ToString() } });
                return userId;
            }
        }

        /**
         * Sets alias for a given user ID
         * @param userId The user identifier
         * @param aliasId The alias identifier to set
         * @return Returns response data string on success, or null on failure
         */
        public static string SetAlias(string userId, string aliasId)
        {
            try
            {
                var queryParams = new Dictionary<string, string>();
                queryParams["accountId"] = SettingsManager.GetInstance().AccountId.ToString();
                queryParams["sdkKey"] = SettingsManager.GetInstance().SdkKey;
                queryParams[ConstantsNamespace.Constants.KEY_USER_ID] = userId;
                queryParams[ConstantsNamespace.Constants.KEY_ALIAS_ID] = aliasId;

                var requestBody = new Dictionary<string, object>
                {
                    { ConstantsNamespace.Constants.KEY_USER_ID, userId },
                    { ConstantsNamespace.Constants.KEY_ALIAS_ID, aliasId }
                };

                var request = new RequestModel(
                    SettingsManager.GetInstance().hostname,
                    "POST",
                    UrlService.GetEndpointWithCollectionPrefix(UrlEnum.SET_ALIAS.GetUrl()),
                    queryParams,
                    requestBody,
                    null,
                    SettingsManager.GetInstance().Protocol,
                    SettingsManager.GetInstance().Port
                );

                var response = NetworkManager.GetInstance().PostAsync(request, null);
                if (response == null || response.GetStatusCode() != ConstantsNamespace.Constants.HTTP_OK)
                {
                    return null;
                }
        
                return response.GetData()?.ToString();
            }
            catch (Exception err)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "GATEWAY_SERVICE_ERROR", new Dictionary<string, string> { { "err", err.ToString() } });
                return null;
            }
        }
    }
}