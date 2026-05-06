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

using System.Collections.Generic;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Services;

namespace VWOFmeSdk.Utils
{
    public static class UserIdUtil
    {
        /**
         * Resolves the canonical userId considering aliasing feature and gateway availability
         * @param userId
         * @param isAliasingEnabled
         * @return string
         */
        public static string GetUserId(string userId, bool isAliasingEnabled)
        {
            if (isAliasingEnabled)
            {
                if (SettingsManager.GetInstance().isGatewayServiceProvided)
                {
                    var aliasId = AliasingUtil.GetAlias(userId);
                    LoggerService.Log(LogLevelEnum.INFO, "ALIAS_ENABLED", new Dictionary<string, string> { { "userId", aliasId } });
                    return aliasId;
                }
                else
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "GATEWAY_URL_ERROR", null);
                    return userId;
                }
            }
            else
            {
                return userId;
            }
        }
    }
}