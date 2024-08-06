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

namespace VWOFmeSdk.Models
{
    public class Campaign
    {
        private bool isAlwaysCheckSegment = false;
        private bool isUserListEnabled = false;
        private int id;
        private Dictionary<string, object> segments;
        private string ruleKey;
        private string status;
        private int percentTraffic;
        private string key;
        private string type;
        private string name;
        private bool isForcedVariationEnabled = false;
        private List<Variation> variations;
        private int startRangeVariation = 0;
        private int endRangeVariation = 0;
        private List<Variable> variables;
        private double weight;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public Dictionary<string, object> Segments
        {
            get { return segments; }
            set { segments = value; }
        }

        public string Status
        {
            get { return status; }
            set { status = value; }
        }

        public int PercentTraffic
        {
            get { return percentTraffic; }
            set { percentTraffic = value; }
        }

        public string Key
        {
            get { return key; }
            set { key = value; }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public bool IsForcedVariationEnabled
        {
            get { return isForcedVariationEnabled; }
            set { isForcedVariationEnabled = value; }
        }

        public List<Variation> Variations
        {
            get { return variations; }
            set { variations = value; }
        }

        public List<Variable> Variables
        {
            get { return variables; }
            set { variables = value; }
        }

        public string RuleKey
        {
            get { return ruleKey; }
            set { ruleKey = value; }
        }

        public bool IsAlwaysCheckSegment
        {
            get { return isAlwaysCheckSegment; }
            set { isAlwaysCheckSegment = value; }
        }

        public bool IsUserListEnabled
        {
            get { return isUserListEnabled; }
            set { isUserListEnabled = value; }
        }

        public double Weight
        {
            get { return weight; }
            set { weight = value; }
        }

        public int StartRangeVariation
        {
            get { return startRangeVariation; }
            set { startRangeVariation = value; }
        }

        public int EndRangeVariation
        {
            get { return endRangeVariation; }
            set { endRangeVariation = value; }
        }

        public void SetModelFromDictionary(Campaign model)
        {
            if (model.Id != 0)
                id = model.Id;

            if (model.Segments != null)
                segments = model.Segments;

            if (model.Status != null)
                status = model.Status;

            if (model.PercentTraffic != 0)
                percentTraffic = model.PercentTraffic;

            if (model.Key != null)
                key = model.Key;

            if (model.Type != null)
                type = model.Type;

            if (model.Name != null)
                name = model.Name;

            if (model.IsForcedVariationEnabled != false)
                isForcedVariationEnabled = model.IsForcedVariationEnabled;

            if (model.Variations != null)
                variations = model.Variations;

            if (model.Variables != null)
                variables = model.Variables;

            if (model.RuleKey != null)
                ruleKey = model.RuleKey;

            if (model.IsAlwaysCheckSegment != false)
                isAlwaysCheckSegment = model.IsAlwaysCheckSegment;

            if (model.IsUserListEnabled != false)
                isUserListEnabled = model.IsUserListEnabled;

            if (model.Weight != 0)
                weight = model.Weight;

            if (model.StartRangeVariation != 0)
                startRangeVariation = model.StartRangeVariation;

            if (model.EndRangeVariation != 0)
                endRangeVariation = model.EndRangeVariation;
        }
    }
}
