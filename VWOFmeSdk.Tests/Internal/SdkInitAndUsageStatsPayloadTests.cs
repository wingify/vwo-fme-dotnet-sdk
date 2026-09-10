/**
 * Copyright 2024-2026 Wingify Software Pvt. Ltd.
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

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WingifyFmeSdk.Enums;
using WingifyFmeSdk.Models;
using WingifyFmeSdk.Models.User;
using WingifyFmeSdk.Services;
using WingifyFmeSdk.Utils;
using Xunit;

namespace VWOFlagTesting.Tests
{
    public class SdkInitAndUsageStatsPayloadTests
    {
        private const int AccountId = 111111;
        private const string SdkKey = "test-sdk-key";
        private const int UsageStatsAccountId = 99999;

        public SdkInitAndUsageStatsPayloadTests()
        {
            new SettingsManager(new WingifyInitOptions
            {
                AccountId = AccountId,
                SdkKey = SdkKey
            });
        }

        [Fact]
        public void GetSdkInitEventPayload_ShouldOnlyIncludeIsSDKInitializedInData()
        {
            var payload = NetworkUtil.GetSdkInitEventPayload(EventEnum.SDK_INIT_EVENT.GetValue());
            var data = GetEventData(payload);

            Assert.True(data["isSDKInitialized"].Value<bool>());
            Assert.False(data.ContainsKey("settingsFetchTime"));
            Assert.False(data.ContainsKey("sdkInitTime"));
            Assert.False(data.ContainsKey("initConfig"));
        }

        [Fact]
        public void GetSDKUsageStatsEventPayload_ShouldIncludeTimingsAndInitConfigInData()
        {
            var options = CreateSampleInitOptions();
            var payload = NetworkUtil.GetSDKUsageStatsEventPayload(
                EventEnum.USAGE_STATS_EVENT.GetValue(),
                UsageStatsAccountId,
                150,
                320,
                options);

            var data = GetEventData(payload);

            Assert.Equal(150, data["settingsFetchTime"].Value<int>());
            Assert.Equal(320, data["sdkInitTime"].Value<int>());

            var initConfig = data["initConfig"] as JObject;
            Assert.NotNull(initConfig);
            Assert.Equal(SdkKey, initConfig["sdkKey"]?.Value<string>());
            Assert.Equal(AccountId, initConfig["accountId"]?.Value<int>());
            Assert.Equal(60, initConfig["pollInterval"]?.Value<int>());
            Assert.True(initConfig["isAliasingEnabled"]?.Value<bool>());
            Assert.Equal("https://proxy.example.com", initConfig["proxyUrl"]?.Value<string>());
            Assert.False(initConfig["integrations"]?.Value<bool>());
            Assert.False(initConfig["networkClientInterface"]?.Value<bool>());
            Assert.False(initConfig["segmentEvaluator"]?.Value<bool>());
            Assert.False(initConfig["storage"]?.Value<bool>());
        }

        [Fact]
        public void GetSDKUsageStatsEventPayload_ShouldFlattenRetryAndBatchConfigInInitConfig()
        {
            var options = CreateSampleInitOptions();
            options.RetryConfig = new Dictionary<string, object>
            {
                { "shouldRetry", true },
                { "maxRetries", 3 },
                { "initialDelay", 1 },
                { "backoffMultiplier", 2 }
            };
            options.BatchEventData = new BatchEventData
            {
                EventsPerRequest = 25,
                RequestTimeInterval = 120
            };

            var payload = NetworkUtil.GetSDKUsageStatsEventPayload(
                EventEnum.USAGE_STATS_EVENT.GetValue(),
                UsageStatsAccountId,
                100,
                200,
                options);

            var initConfig = GetEventData(payload)["initConfig"] as JObject;
            Assert.NotNull(initConfig);

            var retryConfig = initConfig["retryConfig"] as JObject;
            Assert.NotNull(retryConfig);
            Assert.True(retryConfig["shouldRetry"]?.Value<bool>());
            Assert.Equal(3, retryConfig["maxRetries"]?.Value<int>());

            var batchConfig = initConfig["batchEventData"] as JObject;
            Assert.NotNull(batchConfig);
            Assert.Equal(25, batchConfig["eventsPerRequest"]?.Value<int>());
            Assert.Equal(120, batchConfig["requestTimeInterval"]?.Value<int>());
            Assert.False(batchConfig["flushCallback"]?.Value<bool>());
        }

        [Fact]
        public void GetSDKUsageStatsEventPayload_ShouldIncludeVwoMetaOnProps()
        {
            UsageStatsUtil.GetInstance().SetUsageStats(CreateSampleInitOptions());

            var payload = NetworkUtil.GetSDKUsageStatsEventPayload(
                EventEnum.USAGE_STATS_EVENT.GetValue(),
                UsageStatsAccountId,
                50,
                75,
                CreateSampleInitOptions());

            var props = GetEventProps(payload);
            var vwoMeta = props["vwoMeta"] as JObject;

            Assert.NotNull(vwoMeta);
            Assert.Equal(AccountId, vwoMeta["a"]?.Value<int>());
            Assert.Equal(SdkKey, vwoMeta["env"]?.Value<string>());
        }

        [Fact]
        public void GetEventsBaseProperties_ForUsageStats_ShouldIncludeEnvAndUsageStatsAccountId()
        {
            var queryParams = NetworkUtil.GetEventsBaseProperties(
                EventEnum.USAGE_STATS_EVENT.GetValue(),
                visitorUserAgent: null,
                ipAddress: null,
                isUsageStatsEvent: true,
                usageStatsAccountId: UsageStatsAccountId);

            Assert.Equal(EventEnum.USAGE_STATS_EVENT.GetValue(), queryParams["en"]);
            Assert.Equal(UsageStatsAccountId.ToString(), queryParams["a"]);
            Assert.Equal(SdkKey, queryParams["env"]);
            Assert.True(queryParams.ContainsKey("sn"));
            Assert.True(queryParams.ContainsKey("sv"));
        }

        private static WingifyInitOptions CreateSampleInitOptions()
        {
            return new WingifyInitOptions
            {
                AccountId = AccountId,
                SdkKey = SdkKey,
                PollInterval = 60,
                IsAliasingEnabled = true,
                ProxyUrl = "https://proxy.example.com"
            };
        }

        private static JObject GetEventData(Dictionary<string, object> payload)
        {
            return GetEventProps(payload)["data"] as JObject;
        }

        private static JObject GetEventProps(Dictionary<string, object> payload)
        {
            var root = JObject.FromObject(payload);
            return root["d"]?["event"]?["props"] as JObject;
        }
    }
}
