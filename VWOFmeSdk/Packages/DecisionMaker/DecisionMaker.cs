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

using System;
using Murmur;
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Services;
using VWOFmeSdk.Packages.Logger.Enums;

namespace VWOFmeSdk.Packages.DecisionMaker
{
    public class DecisionMaker
    {
        private const int SEED_VALUE = 1; // Seed value for the hash function
        public const int MAX_TRAFFIC_VALUE = ConstantsNamespace.Constants.MAX_TRAFFIC_VALUE; // Maximum traffic value used as a default scale
        public const int MAX_CAMPAIGN_VALUE = 100;

        public int GenerateBucketValue(long hashValue, int maxValue, int multiplier)
        {
            double ratio = (double)hashValue / Math.Pow(2, 32); // Calculate the ratio of the hash value to the maximum hash value
            double multipliedValue = (maxValue * ratio + 1) * multiplier; // Apply the multiplier after scaling the hash value
            return (int)Math.Floor(multipliedValue); // Floor the value to get an integer bucket value
        }

        public int GenerateBucketValue(long hashValue, int maxValue)
        {
            int multiplier = 1;
            double ratio = (double)hashValue / Math.Pow(2, 32); // Calculate the ratio of the hash value to the maximum hash value
            double multipliedValue = (maxValue * ratio + 1) * multiplier; // Apply the multiplier after scaling the hash value
            return (int)Math.Floor(multipliedValue); // Floor the value to get an integer bucket value
        }

        public int GetBucketValueForUser(string userId, int maxValue)
        {
            if (string.IsNullOrEmpty(userId))
            {
                string template = LoggerService.ErrorMessages["USER_ID_NULL_OR_EMPTY"];
                string message = LogMessageUtil.BuildMessage(template, null);
                LoggerService.Log(LogLevelEnum.ERROR, "USER_ID_NULL_OR_EMPTY", null);
                throw new ArgumentException(message);
            }
            long hashValue = GenerateHashValue(userId); // Generate the hash value using murmurHash
            return GenerateBucketValue(hashValue, maxValue, 1); // Generate the bucket value using the hash value (default multiplier)
        }

        public int GetBucketValueForUser(string userId)
        {
            int maxValue = 100;
            if (string.IsNullOrEmpty(userId))
            {
                string template = LoggerService.ErrorMessages["USER_ID_NULL_OR_EMPTY"];
                string message = LogMessageUtil.BuildMessage(template, null);
                LoggerService.Log(LogLevelEnum.ERROR, "USER_ID_NULL_OR_EMPTY", null);
                throw new ArgumentException(message);
            }
            long hashValue = GenerateHashValue(userId); // Generate the hash value using murmurHash
            return GenerateBucketValue(hashValue, maxValue, 1); // Generate the bucket value using the hash value (default multiplier)
        }

        public int CalculateBucketValue(string str, int multiplier, int maxValue)
        {
            long hashValue = GenerateHashValue(str); // Generate the hash value for the string

            return GenerateBucketValue(hashValue, maxValue, multiplier); // Generate and return the bucket value
        }

        public int CalculateBucketValue(string str)
        {
            int multiplier = 1;
            int maxValue = MAX_TRAFFIC_VALUE;
            long hashValue = GenerateHashValue(str); // Generate the hash value for the string

            return GenerateBucketValue(hashValue, maxValue, multiplier); // Generate and return the bucket value
        }

        public long GenerateHashValue(string hashKey)
        {
            var murmur128 = MurmurHash.Create128(SEED_VALUE);
            byte[] hashBytes = murmur128.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashKey));
            long hashValue = BitConverter.ToUInt32(hashBytes, 0); // Convert the first 4 bytes to an unsigned long value
            return hashValue;
        }
    }
}
