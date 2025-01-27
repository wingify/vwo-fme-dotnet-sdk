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

namespace VWOFmeSdk.Models.Request.EventArchQueryParams
{
    public class RequestQueryParams
    {
        private string en;
        private string a;
        private string env;
        private long eTime;
        private double random;
        private string p;
        private string visitorUa;
        private string visitorIp;
        private string url;

        public RequestQueryParams(string eventName, string accountId, string sdkKey, string visitorUserAgent, string ipAddress, string url)
        {
            this.en = eventName;
            this.a = accountId;
            this.env = sdkKey;
            this.eTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            this.random = new Random().NextDouble();
            this.p = "FS";
            this.visitorUa = visitorUserAgent;
            this.visitorIp = ipAddress;
            this.url = url;
        }

        public Dictionary<string, string> GetQueryParams()
        {
            var queryParams = new Dictionary<string, string>
            {
                { "en", this.en },
                { "a", this.a },
                { "env", this.env },
                { "eTime", this.eTime.ToString() },
                { "random", this.random.ToString() },
                { "p", this.p },
                { "visitor_ua", this.visitorUa },
                { "visitor_ip", this.visitorIp }
            };
            return queryParams;
        }

        public string En
        {
            get { return en; }
            set { en = value; }
        }

        public string A
        {
            get { return a; }
            set { a = value; }
        }

        public string Env
        {
            get { return env; }
            set { env = value; }
        }

        public long ETime
        {
            get { return eTime; }
            set { eTime = value; }
        }

        public double Random
        {
            get { return random; }
            set { random = value; }
        }

        public string P
        {
            get { return p; }
            set { p = value; }
        }

        public string VisitorUa
        {
            get { return visitorUa; }
            set { visitorUa = value; }
        }

        public string VisitorIp
        {
            get { return visitorIp; }
            set { visitorIp = value; }
        }

        public string Url
        {
            get { return url; }
            set { url = value; }
        }
    }
}