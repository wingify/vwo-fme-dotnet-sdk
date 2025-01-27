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
#pragma warning disable 1587

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VWOFmeSdk.Packages.NetworkLayer.Models
{
    public class RequestModel
    {
        private string url;
        private string method;
        private string scheme;
        private int port;
        private string path;
        private Dictionary<string, string> query;
        private int timeout;
        private Dictionary<string, object> body;
        private Dictionary<string, string> headers;

        public RequestModel(string url, string method, string path, Dictionary<string, string> query, Dictionary<string, object> body, Dictionary<string, string> headers, string scheme, int port)
        {
            this.url = url;
            this.method = method ?? "GET";
            this.path = path;
            this.query = query;
            this.body = body;
            this.headers = headers;
            this.scheme = scheme ?? "http";

            if (port != 0)
            {
                this.port = port;
            }
        }

        public string GetMethod()
        {
            return method;
        }

        public void SetMethod(string method)
        {
            this.method = method;
        }

        public Dictionary<string, object> GetBody()
        {
            return body;
        }

        public void SetBody(Dictionary<string, object> body)
        {
            this.body = body;
        }

        public Dictionary<string, string> GetQuery()
        {
            return query;
        }

        public void SetQuery(Dictionary<string, string> query)
        {
            this.query = query;
        }

        public Dictionary<string, string> GetHeaders()
        {
            return headers;
        }

        public void SetHeaders(Dictionary<string, string> headers)
        {
            this.headers = headers;
        }

        public int GetTimeout()
        {
            return timeout;
        }

        public void SetTimeout(int timeout)
        {
            this.timeout = timeout;
        }

        public string GetUrl()
        {
            return url;
        }

        public void SetUrl(string url)
        {
            this.url = url;
        }

        public string GetScheme()
        {
            return scheme;
        }

        public void SetScheme(string scheme)
        {
            this.scheme = scheme;
        }

        public int GetPort()
        {
            return port;
        }

        public void SetPort(int port)
        {
            this.port = port;
        }

        public Dictionary<string, object> GetOptions()
        {
            var queryParams = new System.Text.StringBuilder();
            foreach (var key in query.Keys)
            {
                queryParams.Append(key).Append('=').Append(query[key]).Append('&');
            }

            var options = new Dictionary<string, object>();
            options["hostname"] = url;
            options["agent"] = false;

            if (scheme != null)
            {
                options["scheme"] = scheme;
            }

            if (port != 80)
            {
                options["port"] = port;
            }

            if (headers != null)
            {
                options["headers"] = headers;
            }

            if (method != null)
            {
                options["method"] = method;
            }

            if (body != null)
            {
                var postBody = JsonConvert.SerializeObject(body);
                headers["Content-Type"] = "application/json";
                headers["Content-Length"] = postBody.Length.ToString();
                options["headers"] = headers;
                options["body"] = body;
            }

            if (path != null)
            {
                var combinedPath = path;
                if (queryParams.Length > 0)
                {
                    combinedPath += "?" + queryParams.ToString(0, queryParams.Length - 1);
                }
                options["path"] = combinedPath;
            }

            if (timeout > 0)
            {
                options["timeout"] = timeout;
            }

            return options;
        }
    }
}
