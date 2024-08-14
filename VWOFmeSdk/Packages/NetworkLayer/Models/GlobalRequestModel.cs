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
#pragma warning disable 1587

using System.Collections.Generic;

namespace VWOFmeSdk.Packages.NetworkLayer.Models
{
    public class GlobalRequestModel
    {
        private string url;
        private int timeout = 3000;
        private Dictionary<string, object> query;
        private Dictionary<string, object> body;
        private Dictionary<string, string> headers;
        private bool isDevelopmentMode;

        public GlobalRequestModel(string url, Dictionary<string, object> query, Dictionary<string, object> body, Dictionary<string, string> headers)
        {
            this.url = url;
            this.query = query;
            this.body = body;
            this.headers = headers;
        }

        public void SetQuery(Dictionary<string, object> query)
        {
            this.query = query;
        }

        public Dictionary<string, object> GetQuery()
        {
            return query;
        }

        public void SetBody(Dictionary<string, object> body)
        {
            this.body = body;
        }

        public Dictionary<string, object> GetBody()
        {
            return body;
        }

        public void SetBaseUrl(string url)
        {
            this.url = url;
        }

        public string GetBaseUrl()
        {
            return url;
        }

        public void SetTimeout(int timeout)
        {
            this.timeout = timeout;
        }

        public int GetTimeout()
        {
            return timeout;
        }

        public void SetHeaders(Dictionary<string, string> headers)
        {
            this.headers = headers;
        }

        public Dictionary<string, string> GetHeaders()
        {
            return headers;
        }

        public void SetDevelopmentMode(bool isDevelopmentMode)
        {
            this.isDevelopmentMode = isDevelopmentMode;
        }

        public bool GetDevelopmentMode()
        {
            return isDevelopmentMode;
        }
    }
}
