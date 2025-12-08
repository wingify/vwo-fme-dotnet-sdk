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
using VWOFmeSdk.Interfaces.Integration;
using VWOFmeSdk.Interfaces.Networking;
using VWOFmeSdk.Packages.SegmentationEvaluator.Evaluators;
using VWOFmeSdk.Packages.Storage;
using VWOFmeSdk.Models;
using ConstantsNamespace = VWOFmeSdk.Constants;
namespace VWOFmeSdk.Models.User
{
    public class VWOInitOptions
    {
        private string sdkKey;
        private int? accountId;
        private IntegrationCallback integrations;
        private Dictionary<string, object> logger = new Dictionary<string, object>();
        private NetworkClientInterface networkClientInterface;
        private SegmentEvaluator segmentEvaluator;
        private Connector storage;
        private int? pollInterval;
        private VWOBuilder vwoBuilder;
        private Dictionary<string, object> gatewayService;
        private BatchEventData batchEventData;
        private bool isUsageStatsDisabled;
        private Dictionary<string, object> vwoMetaData = new Dictionary<string, object>();
        private Dictionary<string, object> retryConfig;

        public Dictionary<string, object> RetryConfig
        {
            get { return retryConfig; }
            set { retryConfig = value ?? ConstantsNamespace.Constants.DEFAULT_RETRY_CONFIG; }
        }

        private string settings;

        public string SdkKey
        {
            get { return sdkKey; }
            set { sdkKey = value; }
        }

        public int? AccountId
        {
            get { return accountId; }
            set { accountId = value; }
        }

        public IntegrationCallback Integrations
        {
            get { return integrations; }
            set { integrations = value; }
        }

        public Dictionary<string, object> Logger
        {
            get { return logger; }
            set { logger = value; }
        }

        public Dictionary<string, object> GatewayService
        {
            get { return gatewayService; }
            set { gatewayService = value; }
        }

        public NetworkClientInterface NetworkClientInterface
        {
            get { return networkClientInterface; }
            set { networkClientInterface = value; }
        }

        public SegmentEvaluator SegmentEvaluator
        {
            get { return segmentEvaluator; }
            set { segmentEvaluator = value; }
        }

        public Connector Storage
        {
            get { return storage; }
            set { storage = value; }
        }

        public int? PollInterval
        {
            get { return pollInterval; }
            set { pollInterval = value; }
        }

        public VWOBuilder VwoBuilder
        {
            get { return vwoBuilder; }
            set { vwoBuilder = value; }
        }

        public string Settings
        {
            get { return settings; }
            set { settings = value; }
        }

        public BatchEventData BatchEventData
        {
            get { return batchEventData; }
            set { batchEventData = value; }
        }
        
        public bool IsUsageStatsDisabled
        {
            get { return isUsageStatsDisabled; }
            set { isUsageStatsDisabled = value; }
        }

        public Dictionary<string, object> VwoMetaData
        {
            get { return vwoMetaData; }
            set { vwoMetaData = value; }
        }
    }
}