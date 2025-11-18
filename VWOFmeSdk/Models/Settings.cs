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
using Newtonsoft.Json;
using VWOFmeSdk.Utils;

namespace VWOFmeSdk.Models
{
    public class Settings
    {
        private List<Feature> features;
        private int accountId;
        private Dictionary<string, Groups> groups;
        private Dictionary<string, int> campaignGroups;
        private bool isNBv2 = false;
        private List<Campaign> campaigns;
        private bool isNB = false;
        private string sdkKey;
        private int version;
        private string collectionPrefix;

        [JsonConverter(typeof(EmptyObjectToArrayConverter<Feature>))]
        public List<Feature> Features
        {
            get { return features; }
            set { features = value; }
        }

        public int AccountId
        {
            get { return accountId; }
            set { accountId = value; }
        }

        public Dictionary<string, Groups> Groups
        {
            get { return groups; }
            set { groups = value; }
        }

        public Dictionary<string, int> CampaignGroups
        {
            get { return campaignGroups; }
            set { campaignGroups = value; }
        }

        public bool IsNBv2
        {
            get { return isNBv2; }
            set { isNBv2 = value; }
        }

        [JsonConverter(typeof(EmptyObjectToArrayConverter<Campaign>))]
        public List<Campaign> Campaigns
        {
            get { return campaigns; }
            set { campaigns = value; }
        }

        public bool IsNB
        {
            get { return isNB; }
            set { isNB = value; }
        }

        public string SdkKey
        {
            get { return sdkKey; }
            set { sdkKey = value; }
        }

        public int Version
        {
            get { return version; }
            set { version = value; }
        }

        public string CollectionPrefix
        {
            get { return collectionPrefix; }
            set { collectionPrefix = value; }
        }
    }
}
