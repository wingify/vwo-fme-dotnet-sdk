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
using VWOFmeSdk.Models;
using VWOFmeSdk.Services;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Enums;

namespace VWOFmeSdk.Models.Schemas
{
    public class SettingsSchema
    {
        public bool IsSettingsValid(Settings settings)
        {
            if (settings == null)
            {
                return false;
            }

            // Validate Settings fields - check required fields and log specific errors
            if (settings.Version == null)
            {
                return false;
            }

            // Validate AccountId - check for null (if nullable) or 0 (if missing from JSON)
            if (settings.AccountId == null || settings.AccountId == 0)
            {
                LogManager.GetInstance().ErrorLog("SETTINGS_ACCOUNT_ID_MISSING", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.INIT.GetValue() } });
                return false;
            }

            // Validate SdkKey - check for null or empty string
            if (string.IsNullOrEmpty(settings.SdkKey))
            {
                LogManager.GetInstance().ErrorLog("SETTINGS_SDK_KEY_MISSING", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } });
                return false;
            }

            // Allow empty campaigns - it's valid to have no campaigns set up
            // if campaingns in empty, then  normalize to empty list
            if (settings.Campaigns == null)
            {
                settings.Campaigns = new List<Campaign>();
            }

            if (settings.Campaigns.Count > 0)
            {
                foreach (var campaign in settings.Campaigns)
                {
                    if (!IsValidCampaign(campaign))
                    {
                        return false;
                    }
                }
            }

            if(settings.Features == null)
            {
                settings.Features = new List<Feature>();
            }

            if (settings.Features != null)
            {
                foreach (var feature in settings.Features)
                {
                    if (!IsValidFeature(feature))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsValidCampaign(Campaign campaign)
        {
            if (campaign.Id == null || campaign.Type == null || campaign.Key == null || campaign.Status == null || campaign.Name == null)
            {
                return false;
            }

            if (campaign.Variations == null || campaign.Variations.Count == 0)
            {
                return false;
            }

            foreach (var variation in campaign.Variations)
            {
                if (!IsValidCampaignVariation(variation))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsValidCampaignVariation(Variation variation)
        {
            if (variation.Id == null || variation.Name == null || string.IsNullOrEmpty(variation.Weight.ToString()))
            {
                return false;
            }

            if (variation.Variables != null)
            {
                foreach (var variable in variation.Variables)
                {
                    if (!IsValidVariableObject(variable))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsValidVariableObject(Variable variable)
        {
            return variable.Id != null && variable.Type != null && variable.Key != null && variable.Value != null;
        }

        private bool IsValidFeature(Feature feature)
        {
            if (feature.Id == null || feature.Key == null || feature.Status == null || feature.Name == null || feature.Type == null)
            {
                return false;
            }

            if (feature.Metrics == null || feature.Metrics.Count == 0)
            {
                return false;
            }

            foreach (var metric in feature.Metrics)
            {
                if (!IsValidCampaignMetric(metric))
                {
                    return false;
                }
            }

            if (feature.Rules != null)
            {
                foreach (var rule in feature.Rules)
                {
                    if (!IsValidRule(rule))
                    {
                        return false;
                    }
                }
            }

            if (feature.Variables != null)
            {
                foreach (var variable in feature.Variables)
                {
                    if (!IsValidVariableObject(variable))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsValidCampaignMetric(Metric metric)
        {
            return metric.Id != null && metric.Type != null && metric.Identifier != null;
        }

        private bool IsValidRule(Rule rule)
        {
            return rule.Type != null && rule.RuleKey != null && rule.CampaignId != null;
        }
    }
}
