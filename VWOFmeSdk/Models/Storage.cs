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
    public class Storage
    {
        private string featureKey;
        private string user;
        private int rolloutId;
        private string rolloutKey;
        private int rolloutVariationId;
        private int experimentId;
        private string experimentKey;
        private int experimentVariationId;

        public string FeatureKey
        {
            get { return featureKey; }
            set { featureKey = value; }
        }

        public string User
        {
            get { return user; }
            set { user = value; }
        }

        public int RolloutId
        {
            get { return rolloutId; }
            set { rolloutId = value; }
        }

        public string RolloutKey
        {
            get { return rolloutKey; }
            set { rolloutKey = value; }
        }

        public int RolloutVariationId
        {
            get { return rolloutVariationId; }
            set { rolloutVariationId = value; }
        }

        public int ExperimentId
        {
            get { return experimentId; }
            set { experimentId = value; }
        }

        public string ExperimentKey
        {
            get { return experimentKey; }
            set { experimentKey = value; }
        }

        public int ExperimentVariationId
        {
            get { return experimentVariationId; }
            set { experimentVariationId = value; }
        }
    }
}
