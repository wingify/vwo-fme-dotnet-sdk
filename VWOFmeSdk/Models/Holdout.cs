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

namespace VWOFmeSdk.Models
{
    public class Holdout
    {
        private int id;
        private Dictionary<string, object> segments;
        private int? percentTraffic;
        private bool isGlobal;
        private List<int> featureIds;
        private List<Metric> metrics;
        private bool isGatewayServiceRequired = false;
        private string name;

        [JsonProperty("id")]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        [JsonProperty("segments")]
        public Dictionary<string, object> Segments
        {
            get { return segments; }
            set { segments = value; }
        }

        [JsonProperty("percentTraffic")]
        public int? PercentTraffic
        {
            get { return percentTraffic; }
            set { percentTraffic = value; }
        }

        [JsonProperty("isGlobal")]
        public bool IsGlobal
        {
            get { return isGlobal; }
            set { isGlobal = value; }
        }

        [JsonProperty("featureIds")]
        public List<int> FeatureIds
        {
            get { return featureIds; }
            set { featureIds = value; }
        }

        [JsonProperty("metrics")]
        public List<Metric> Metrics
        {
            get { return metrics; }
            set { metrics = value; }
        }

        [JsonProperty("isGatewayServiceRequired")]
        public bool IsGatewayServiceRequired
        {
            get { return isGatewayServiceRequired; }
            set { isGatewayServiceRequired = value; }
        }

        [JsonProperty("name")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}
