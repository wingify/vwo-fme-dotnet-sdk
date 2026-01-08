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
    public class Feature
    {
        private string key;
        private List<Metric> metrics;
        private string status;
        private int id;
        private List<Rule> rules;
        private ImpactCampaign impactCampaign = new ImpactCampaign();
        private string name;
        private string type;
        private List<Campaign> rulesLinkedCampaign = new List<Campaign>();
        private bool isGatewayServiceRequired = false;
        private List<Variable> variables;
        private bool isDebuggerEnabled = false;

        public string Key
        {
            get { return key; }
            set { key = value; }
        }

        public List<Metric> Metrics
        {
            get { return metrics; }
            set { metrics = value; }
        }

        public string Status
        {
            get { return status; }
            set { status = value; }
        }

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public List<Rule> Rules
        {
            get { return rules; }
            set { rules = value; }
        }

        public ImpactCampaign ImpactCampaign
        {
            get { return impactCampaign; }
            set { impactCampaign = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        public List<Campaign> RulesLinkedCampaign
        {
            get { return rulesLinkedCampaign; }
            set { rulesLinkedCampaign = value; }
        }

        public bool IsGatewayServiceRequired
        {
            get { return isGatewayServiceRequired; }
            set { isGatewayServiceRequired = value; }
        }

        public List<Variable> Variables
        {
            get { return variables; }
            set { variables = value; }
        }

        public bool IsDebuggerEnabled
        {
            get { return isDebuggerEnabled; }
            set { isDebuggerEnabled = value; }
        }
    }
}
