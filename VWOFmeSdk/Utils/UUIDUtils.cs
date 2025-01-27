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
using System.Linq;

namespace VWOFmeSdk.Utils
{
    public static class UUIDUtils
    {
        /// <summary>
        /// This method generates a random UUID.
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static string GetRandomUUID(string apiKey)
        {
            var namespaceUUID = new Guid("00000000-0000-0000-0000-000000000000");
            var randomUUID = Guid.NewGuid();
            return new Guid(namespaceUUID.ToByteArray()).ToString();
        }

        /// <summary>
        /// This method generates a UUID for a given userId and accountId.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public static string GetUUID(string userId, string accountId)
        {
            var vwoNamespace = Guid.Parse("00000000-0000-0000-0000-000000000000");
            var userIdNamespace = GenerateUUID(accountId, vwoNamespace.ToString());
            var uuidForUserIdAccountId = GenerateUUID(userId, userIdNamespace.ToString());
            return uuidForUserIdAccountId.ToString().Replace("-", "").ToUpper();
        }

        /// <summary>
        /// This method generates a UUID for a given name and namespaceId.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="namespaceId"></param>
        /// <returns></returns>
        private static Guid GenerateUUID(string name, string namespaceId)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(namespaceId))
            {
                return default;
            }

            var namespaceBytes = new Guid(namespaceId).ToByteArray();
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            var hash = System.Security.Cryptography.SHA1.Create().ComputeHash(namespaceBytes.Concat(nameBytes).ToArray());
            hash[6] = (byte)(0x50 | (hash[6] & 0xf));
            hash[8] = (byte)(0x80 | (hash[8] & 0x3f));
            return new Guid(hash.Take(16).ToArray());
        }
    }
}
