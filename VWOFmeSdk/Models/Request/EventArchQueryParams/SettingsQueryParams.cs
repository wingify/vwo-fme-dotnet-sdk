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

namespace VWOFmeSdk.Models.Request.EventArchQueryParams
{
    public class SettingsQueryParams
    {
        private string i;
        private string r;
        private string a;

        public SettingsQueryParams(string i, string r, string a)
        {
            this.i = i;
            this.r = r;
            this.a = a;
        }

        public Dictionary<string, string> GetQueryParams()
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            queryParams.Add("i", this.i);
            queryParams.Add("r", this.r);
            queryParams.Add("a", this.a);
            return queryParams;
        }

        public string I
        {
            get { return i; }
            set { i = value; }
        }

        public string R
        {
            get { return r; }
            set { r = value; }
        }

        public string A
        {
            get { return a; }
            set { a = value; }
        }
    }
}
