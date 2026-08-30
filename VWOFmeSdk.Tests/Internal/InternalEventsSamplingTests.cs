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
using WingifyFmeSdk.Services;
using WingifyFmeSdk.Utils;
using Xunit;

namespace VWOFlagTesting.Tests
{
    public class InternalEventsSamplingTests
    {
        private static Dictionary<string, object> ParseSettings(string json)
        {
            return InternalEventsSamplingUtil.ParseSettingsDocument(json);
        }

        [Fact]
        public void ParseSettingsDocument_ShouldReturnEmptyDictionaryForNullOrEmptyInput()
        {
            Assert.Empty(InternalEventsSamplingUtil.ParseSettingsDocument(null));
            Assert.Empty(InternalEventsSamplingUtil.ParseSettingsDocument(string.Empty));
        }

        [Fact]
        public void ShouldApplyEventSampling_ShouldDefaultToFalseWhenMissing()
        {
            var settings = ParseSettings("{}");

            Assert.False(InternalEventsSamplingUtil.ShouldApplyEventSampling(settings));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ShouldApplyEventSampling_ShouldReadServerRuntimeFlag(bool alwaysApply)
        {
            var settings = ParseSettings($@"{{
                ""alwaysApplySampling"": {{ ""server"": {alwaysApply.ToString().ToLower()} }}
            }}");

            Assert.Equal(alwaysApply, InternalEventsSamplingUtil.ShouldApplyEventSampling(settings));
        }

        [Fact]
        public void GetUsageStatsSamplingPercent_ShouldDefaultToTenWhenMissing()
        {
            var settings = ParseSettings("{}");

            Assert.Equal(10, InternalEventsSamplingUtil.GetUsageStatsSamplingPercent(settings));
        }

        [Fact]
        public void GetDebugEventSamplingPercent_ShouldReadConfiguredServerValue()
        {
            var settings = ParseSettings(@"{ ""sampling"": { ""debug"": { ""server"": 25 } } }");

            Assert.Equal(25, InternalEventsSamplingUtil.GetDebugEventSamplingPercent(settings));
        }

        [Fact]
        public void NormalizeSamplingPercent_ShouldFallbackToDefaultForInvalidValues()
        {
            Assert.Equal(10, InternalEventsSamplingUtil.NormalizeSamplingPercent(null));
            Assert.Equal(10, InternalEventsSamplingUtil.NormalizeSamplingPercent("invalid"));
            Assert.Equal(10, InternalEventsSamplingUtil.NormalizeSamplingPercent(-1));
            Assert.Equal(10, InternalEventsSamplingUtil.NormalizeSamplingPercent(101));
        }

        [Theory]
        [InlineData(0.0, 100, true)]
        [InlineData(0.0, 0, true)]
        [InlineData(0.5, 50, true)]
        [InlineData(0.5, 49, false)]
        [InlineData(0.999, 100, true)]
        public void PassesSamplingPercent_ShouldEvaluateThreshold(double randomValue, int samplingPercent, bool expected)
        {
            Assert.Equal(expected, InternalEventsSamplingUtil.PassesSamplingPercent(samplingPercent, randomValue));
        }

        [Theory]
        [InlineData("EVENT_NOT_FOUND", true)]
        [InlineData("FEATURE_NOT_FOUND", true)]
        [InlineData("FEATURE_NOT_FOUND_WITH_ID", true)]
        [InlineData("NETWORK_CALL_FAILED", false)]
        [InlineData("UNKNOWN_ERROR_KEY", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        public void IsSampledDebugErrorTemplateKey_ShouldIdentifySampledKeys(string messageTemplateKey, bool expected)
        {
            Assert.Equal(expected, InternalEventsSamplingUtil.IsSampledDebugErrorTemplateKey(messageTemplateKey));
        }

        [Fact]
        public void GetUsageStatsAccountId_ShouldReturnNullWhenMissing()
        {
            Assert.Null(InternalEventsSamplingUtil.GetUsageStatsAccountId(ParseSettings("{}")));
        }

        [Fact]
        public void GetUsageStatsAccountId_ShouldReturnConfiguredValue()
        {
            var settings = ParseSettings(@"{ ""usageStatsAccountId"": 12345 }");

            Assert.Equal(12345L, InternalEventsSamplingUtil.GetUsageStatsAccountId(settings));
        }

        [Fact]
        public void ShouldSendUsageStatsEvent_ShouldAlwaysSendWhenAlwaysApplySamplingIsFalse()
        {
            var settings = ParseSettings(@"{ ""alwaysApplySampling"": { ""server"": false } }");
            var service = new InternalEventsThrottleService(() => 1.0);

            Assert.True(service.ShouldSendUsageStatsEvent(settings));
        }

        [Fact]
        public void ShouldSendUsageStatsEvent_ShouldApplySamplingWhenAlwaysApplySamplingIsTrue()
        {
            var settings = ParseSettings(@"{{
                ""alwaysApplySampling"": {{ ""server"": true }},
                ""sampling"": {{ ""usage"": {{ ""server"": 50 }} }}
            }}");

            var passingService = new InternalEventsThrottleService(() => 0.0);
            var failingService = new InternalEventsThrottleService(() => 1.0);

            Assert.True(passingService.ShouldSendUsageStatsEvent(settings));
            Assert.False(failingService.ShouldSendUsageStatsEvent(settings));
        }

        [Fact]
        public void ShouldSendSampledDebugEvent_ShouldAlwaysSendWhenAlwaysApplySamplingIsFalse()
        {
            var settings = ParseSettings(@"{ ""alwaysApplySampling"": { ""server"": false } }");
            var service = new InternalEventsThrottleService(() => 1.0);

            Assert.True(service.ShouldSendSampledDebugEvent(settings));
        }

        [Fact]
        public void ShouldSendSampledDebugEvent_ShouldApplyDebugSamplingWhenAlwaysApplySamplingIsTrue()
        {
            var settings = ParseSettings(@"{{
                ""alwaysApplySampling"": {{ ""server"": true }},
                ""sampling"": {{ ""debug"": {{ ""server"": 10 }} }}
            }}");

            var passingService = new InternalEventsThrottleService(() => 0.0);
            var failingService = new InternalEventsThrottleService(() => 0.5);

            Assert.True(passingService.ShouldSendSampledDebugEvent(settings));
            Assert.False(failingService.ShouldSendSampledDebugEvent(settings));
        }
    }
}
