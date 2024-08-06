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

namespace VWOFmeSdk.Enums
{
    public enum CampaignTypeEnum
    {
        ROLLOUT,
        AB,
        PERSONALIZE
    }

    public static class CampaignTypeEnumExtensions
    {
        public static string GetValue(this CampaignTypeEnum campaignTypeEnum)
        {
            switch (campaignTypeEnum)
            {
                case CampaignTypeEnum.ROLLOUT:
                    return "FLAG_ROLLOUT";
                case CampaignTypeEnum.AB:
                    return "FLAG_TESTING";
                case CampaignTypeEnum.PERSONALIZE:
                    return "FLAG_PERSONALIZE";
                default:
                    throw new ArgumentOutOfRangeException(nameof(campaignTypeEnum), campaignTypeEnum, null);
            }
        }
    }
}
