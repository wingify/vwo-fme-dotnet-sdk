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

using Newtonsoft.Json;
using System.Collections.Generic;

namespace VWOFmeSdk.Models.Request
{
    public class Props
    {
        [JsonProperty("vwo_sdkName")]
        public string VwoSdkName { get; set; }

        [JsonProperty("vwo_sdkVersion")]
        public string VwoSdkVersion { get; set; }

        [JsonProperty("vwo_envKey")]
        public string VwoEnvKey { get; set; }

        [JsonProperty("variation")]
        public string Variation { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("isFirst")]
        public int IsFirst { get; set; }

        [JsonProperty("isCustomEvent")]
        public bool IsCustomEvent { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
    }
}
