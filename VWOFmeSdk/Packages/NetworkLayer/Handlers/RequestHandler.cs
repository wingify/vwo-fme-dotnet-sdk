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
using VWOFmeSdk.Packages.NetworkLayer.Models;

namespace VWOFmeSdk.Packages.NetworkLayer.Handlers
{
    public class RequestHandler
    {
        /// <summary>
        /// Creates a new request by merging properties from a base request and a configuration model.
        /// If both the request URL and the base URL from the configuration are missing, it returns null.
        /// Otherwise, it merges the properties from the configuration into the request if they are not already set.
        /// </summary>
        /// <param name="request">The initial request model.</param>
        /// <param name="config">The global request configuration model.</param>
        /// <returns>The merged request model or null if both URLs are missing.</returns>
        public RequestModel CreateRequest(RequestModel request, GlobalRequestModel config)
        {
            // Check if both the request URL and the configuration base URL are missing
            if (string.IsNullOrEmpty(config.GetBaseUrl()) && string.IsNullOrEmpty(request.GetUrl()))
            {
                return null; // Return null if no URL is specified
            }

            // Set the request URL, defaulting to the configuration base URL if not set
            if (string.IsNullOrEmpty(request.GetUrl()))
            {
                request.SetUrl(config.GetBaseUrl());
            }

            // Set the request timeout, defaulting to the configuration timeout if not set
            if (request.GetTimeout() == -1)
            {
                request.SetTimeout(config.GetTimeout());
            }

            // Set the request body, defaulting to the configuration body if not set
            if (request.GetBody() == null)
            {
                request.SetBody(config.GetBody());
            }

            // Set the request headers, defaulting to the configuration headers if not set
            if (request.GetHeaders() == null)
            {
                request.SetHeaders(config.GetHeaders());
            }

            // Initialize request query parameters, defaulting to an empty dictionary if not set
            var requestQueryParams = request.GetQuery() ?? new Dictionary<string, string>();

            // Initialize configuration query parameters, defaulting to an empty dictionary if not set
            var configQueryParams = config.GetQuery() ?? new Dictionary<string, object>();

            // Merge configuration query parameters into the request query parameters if they don't exist
            foreach (var entry in configQueryParams)
            {
                if (!requestQueryParams.ContainsKey(entry.Key))
                {
                    requestQueryParams[entry.Key] = entry.Value.ToString();
                }
            }

            // Set the merged query parameters back to the request
            request.SetQuery(requestQueryParams);

            return request; // Return the modified request
        }
    }
}