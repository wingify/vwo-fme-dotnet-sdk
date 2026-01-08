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
using Identifiable;

namespace VWOFmeSdk.Utils
{
    public static class UUIDUtils
    {
        private static readonly Guid UrlNamespace = new Guid("6ba7b811-9dad-11d1-80b4-00c04fd430c8"); // The namespace for URLs
        private static readonly Guid DnsNamespace = new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8"); // The namespace for DNS
        private static readonly string VWO_NAMESPACE_URL = "https://vwo.com";

        /// <summary>
        /// This method generates a UUID for a given userId and accountId.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public static string GetUUID(string userId, string accountId)
        {
            var accountIdAsString = accountId.ToString();

            // Compute the UUID using NamedGuid.Compute from the Identifiable library
            var vwoNamespaceGuid = NamedGuid.Compute(NamedGuidAlgorithm.SHA1, UrlNamespace, VWO_NAMESPACE_URL);
            var accountIdGuid = NamedGuid.Compute(NamedGuidAlgorithm.SHA1, vwoNamespaceGuid, accountIdAsString);
            var userIdGuid = NamedGuid.Compute(NamedGuidAlgorithm.SHA1, accountIdGuid, userId);

            var uuid = userIdGuid.ToString("N").ToUpper(); // Format as a UUID string (no hyphens)
            return uuid;
        }

        /// <summary>
        /// This method generates a random UUID based on an API key.
        /// Generates a namespace based on the API key using DNS namespace,
        /// then generates a random UUID using the namespace derived from the API key.
        /// </summary>
        /// <param name="apiKey">The API key used to generate a namespace for the UUID.</param>
        /// <returns>A random UUID string.</returns>
        public static string GetRandomUUID(string apiKey)
        {
            // Generate a namespace based on the API key using DNS namespace
            var namespaceGuid = NamedGuid.Compute(NamedGuidAlgorithm.SHA1, DnsNamespace, apiKey);
            // Generate a random UUID v4
            var randomUUIDv4 = Guid.NewGuid();
            // Generate a random UUID using the namespace derived from the API key
            var randomUUID = NamedGuid.Compute(NamedGuidAlgorithm.SHA1, namespaceGuid, randomUUIDv4.ToString());

            return randomUUID.ToString();
        }
    }
}
