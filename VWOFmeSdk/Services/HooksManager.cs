#pragma warning disable 1587
/**
 * Copyright 2024 Wingify Software Pvt. Ltd.
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
using VWOFmeSdk.Services;

namespace VWOFmeSdk.Services
{
    public class HooksManager
    {
        private readonly IntegrationCallback _callback;
        private Dictionary<string, object> _decision;

        /// <summary>
        /// Constructor for HooksManager
        /// </summary>
        /// <param name="callback"></param>
        public HooksManager(IntegrationCallback callback)
        {
            _callback = callback;
        }

        /// <summary>
        /// Executes the callback
        /// </summary>
        /// <param name="properties"></param>
        public void Execute(Dictionary<string, object> properties)
        {
            _callback?.Execute(properties);
        }

        /// <summary>
        /// Sets the decision
        /// </summary>
        /// <param name="properties"></param>
        public void Set(Dictionary<string, object> properties)
        {
            if (_callback != null)
            {
                _decision = properties;
            }
        }

        public Dictionary<string, object> Get()
        {
            return _decision;
        }
    }
}
