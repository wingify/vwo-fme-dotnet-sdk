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
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;

namespace VWOFmeSdk.Models.User
{
    public class VWOContext
    {
        private string id;
        private string userAgent = "";
        private string ipAddress = "";
        private Dictionary<string, object> customVariables = new Dictionary<string, object>();
        private Dictionary<string, object> variationTargetingVariables = new Dictionary<string, object>();
        private List<string> postSegmentationVariables = new List<string>();
        private GatewayService _vwo;
        private string _vwo_uuid;
        private long? _vwo_sessionId;

        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        public string VwoUuid
        {
            get { return _vwo_uuid; }
            set { _vwo_uuid = value; }
        }

        public long? VwoSessionId
        {
            get { return _vwo_sessionId; }
            set { _vwo_sessionId = value; }
        }

        /// <summary>
        /// Initializes _vwo_uuid and _vwo_sessionId for the current context
        /// This should be called after setting the Id property
        /// </summary>
        public void InitializeVwoIdentifiers()
        {
            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    _vwo_uuid = UUIDUtils.GetUUID(id, SettingsManager.GetInstance().AccountId.ToString());
                    _vwo_sessionId = FunctionUtil.GetCurrentUnixTimestamp();
                }
                catch (Exception)
                {
                    // If SettingsManager is not initialized, use empty values
                    _vwo_uuid = null;
                    _vwo_sessionId = null;
                }
            }
        }

        public string UserAgent
        {
            get { return userAgent; }
            set { userAgent = value; }
        }

        public string IpAddress
        {
            get { return ipAddress; }
            set { ipAddress = value; }
        }

        public Dictionary<string, object> CustomVariables
        {
            get { return customVariables; }
            set { customVariables = value; }
        }

        public Dictionary<string, object> VariationTargetingVariables
        {
            get { return variationTargetingVariables; }
            set { variationTargetingVariables = value; }
        }

        public List<string> PostSegmentationVariables
        {
            get { return postSegmentationVariables; }
            set { postSegmentationVariables = value; }
        }

        public GatewayService Vwo
        {
            get { return _vwo; }
            set { _vwo = value; }
        }

        public string GetUuid()
        {
            // Lazy initialization if UUID is not set
            if (_vwo_uuid == null && !string.IsNullOrEmpty(id))
            {
                InitializeVwoIdentifiers();
            }
            return _vwo_uuid;
        }

        public long? GetSessionId()
        {
            // Lazy initialization if session ID is not set
            if (_vwo_sessionId == null && !string.IsNullOrEmpty(id))
            {
                InitializeVwoIdentifiers();
            }
            return _vwo_sessionId;
        }

        
    }
}
