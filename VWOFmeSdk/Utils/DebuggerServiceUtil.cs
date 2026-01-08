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
using System.Threading.Tasks;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Enums;
using ConstantsNamespace = VWOFmeSdk.Constants;
using Newtonsoft.Json;

namespace VWOFmeSdk.Utils
{
    /// <summary>
    /// Utility functions for handling debugger service operations including
    /// filtering sensitive properties and extracting decision keys.
    /// </summary>
    public static class DebuggerServiceUtil
    {
        /// <summary>
        /// Extracts only the required fields from a decision object.
        /// </summary>
        /// <param name="decisionObj">The decision object to extract fields from</param>
        /// <returns>An object containing only rolloutKey and experimentKey if they exist</returns>
        public static Dictionary<string, object> ExtractDecisionKeys(Dictionary<string, object> decisionObj = null)
        {
            var extractedKeys = new Dictionary<string, object>();

            if (decisionObj == null)
            {
                return extractedKeys;
            }

            // Extract rolloutId if present
            if (decisionObj.ContainsKey("rolloutId") && decisionObj["rolloutId"] != null)
            {
                extractedKeys["rId"] = decisionObj["rolloutId"];
            }

            // Extract rolloutVariationId if present
            if (decisionObj.ContainsKey("rolloutVariationId") && decisionObj["rolloutVariationId"] != null)
            {
                extractedKeys["rvId"] = decisionObj["rolloutVariationId"];
            }

            // Extract experimentId if present
            if (decisionObj.ContainsKey("experimentId") && decisionObj["experimentId"] != null)
            {
                extractedKeys["eId"] = decisionObj["experimentId"];
            }

            // Extract experimentVariationId if present
            if (decisionObj.ContainsKey("experimentVariationId") && decisionObj["experimentVariationId"] != null)
            {
                extractedKeys["evId"] = decisionObj["experimentVariationId"];
            }

            return extractedKeys;
        }

        /// <summary>
        /// Sends a debug event to VWO.
        /// </summary>
        /// <param name="eventProps">The properties for the event.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static void SendDebugEventToVWO(Dictionary<string, object> eventProps = null)
        {
            // Create query parameters
            var properties = NetworkUtil.GetEventsBaseProperties(EventEnum.VWO_DEBUGGER_EVENT.GetValue());

            // Create payload
            var payload = NetworkUtil.GetDebuggerEventPayload(eventProps);

            // Send event
            NetworkUtil.SendEvent(properties, payload, EventEnum.VWO_DEBUGGER_EVENT.GetValue());
        }

    }
}

