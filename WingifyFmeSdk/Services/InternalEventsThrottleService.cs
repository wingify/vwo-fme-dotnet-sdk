#pragma warning disable 1587
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
#pragma warning restore 1587

using System;
using System.Collections.Generic;
using WingifyFmeSdk.Utils;

namespace WingifyFmeSdk.Services
{
    /// <summary>
    /// Applies gating and sampling rules for internal SDK events
    /// (<c>vwo_fmeSdkInit</c>, <c>vwo_sdkUsageStats</c>, and sampled <c>vwo_sdkDebug</c>).
    /// </summary>
    public class InternalEventsThrottleService
    {
        private readonly Func<double> randomValueProvider;

        /// <summary>
        /// Default throttle service instance used during SDK runtime.
        /// </summary>
        public static InternalEventsThrottleService Default { get; } = new InternalEventsThrottleService();

        /// <summary>
        /// Creates an internal-events throttle service.
        /// </summary>
        /// <param name="randomValueProvider">Optional provider for sampling randomness (used in tests).</param>
        public InternalEventsThrottleService(Func<double> randomValueProvider = null)
        {
            this.randomValueProvider = randomValueProvider ?? (() => new Random().NextDouble());
        }


        /// <summary>
        /// Determines whether the SDK usage-stats internal event should be sent.
        /// </summary>
        /// <param name="settings">The parsed settings document.</param>
        /// <returns><c>true</c> when the usage-stats event qualifies to be sent.</returns>
        public bool ShouldSendUsageStatsEvent(Dictionary<string, object> settings)
        {
            settings = settings ?? new Dictionary<string, object>();

            // When alwaysApplySampling is false, send directly without percentage sampling.
            if (!InternalEventsSamplingUtil.ShouldApplyEventSampling(settings))
            {
                return true;
            }

            int usageStatsSamplingPercent = InternalEventsSamplingUtil.GetUsageStatsSamplingPercent(settings);
            return InternalEventsSamplingUtil.PassesSamplingPercent(usageStatsSamplingPercent, randomValueProvider());
        }

        /// <summary>
        /// Determines whether a sampled debug internal event should be sent.
        /// Always-send debug events bypass this method entirely.
        /// </summary>
        /// <param name="settings">The parsed settings document.</param>
        /// <returns><c>true</c> when the sampled debug event qualifies to be sent.</returns>
        public bool ShouldSendSampledDebugEvent(Dictionary<string, object> settings)
        {
            settings = settings ?? new Dictionary<string, object>();

            // When alwaysApplySampling is false, send directly without percentage sampling.
            if (!InternalEventsSamplingUtil.ShouldApplyEventSampling(settings))
            {
                return true;
            }

            int debugSamplingPercent = InternalEventsSamplingUtil.GetDebugEventSamplingPercent(settings);
            return InternalEventsSamplingUtil.PassesSamplingPercent(debugSamplingPercent, randomValueProvider());
        }
    }
}
