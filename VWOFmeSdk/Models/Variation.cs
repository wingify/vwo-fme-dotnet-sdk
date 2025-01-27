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

namespace VWOFmeSdk.Models
{
    public class Variation
    {
        private int id;
        private string key;
        private string name;
        private double weight;
        private int startRangeVariation = 0;
        private int endRangeVariation = 0;
        private List<Variable> variables = new List<Variable>();
        private List<Variation> variations = new List<Variation>();
        private Dictionary<string, object> segments = new Dictionary<string, object>();
        private string ruleKey;
        private string type;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Key
        {
            get { return key; }
            set { key = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
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

        public List<Variable> Variables
        {
            get { return variables; }
            set { variables = value; }
        }

        public List<Variation> Variations
        {
            get { return variations; }
            set { variations = value; }
        }

        public Dictionary<string, object> Segments
        {
            get { return segments; }
            set { segments = value; }
        }

        public string RuleKey
        {
            get { return ruleKey; }
            set { ruleKey = value; }
        }

         public string Type
        {
            get { return type; }
            set { type = value; }
        }
    }
}