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

using System.Collections.Generic;
using Newtonsoft.Json;
using VWOFlagTesting.Utils;
using VWOFmeSdk;
using VWOFmeSdk.Models.User;
using Xunit;

namespace VWOFlagTesting.Tests
{
    public class CustomBucketingSeedTests
    {
        private const string SdkKey = "abcdef";
        private const int AccountId = 123456;
        private const string SettingsFile = "CUSTOM_BUCKETING_SEED_SETTINGS.json";
        private const string FeatureKey = "featureOne";
        private const string FeatureTwoKey = "featureTwo";
        private const string SameSaltSettingsFile = "SETTINGS_WITH_SAME_SALT.JSON";
        private const string SameSaltFeatureOneKey = "feature1";
        private const string SameSaltFeatureTwoKey = "feature2";
        private const string SameSaltSdkKey = "000000000000_MASKED_000000000000";
        private const int SameSaltAccountId = 12345;

        private VWOClient InitializeVwoClient(string settingsFile = SettingsFile, string sdkKey = SdkKey, int accountId = AccountId)
        {
            var settings = SettingsLoader.LoadSettings(settingsFile);
            string settingsJson = settings.ToString();

            var logger = new Dictionary<string, object>
            {
                { "level", "ERROR" }
            };

            var vwoBuilder = new VWOBuilder(new VWOInitOptions
            {
                SdkKey = sdkKey,
                AccountId = accountId,
                Logger = logger
            });

            vwoBuilder.SetLogger();
            vwoBuilder.SetSettings(settingsJson);

            var options = new VWOInitOptions
            {
                SdkKey = sdkKey,
                AccountId = accountId,
                VwoBuilder = vwoBuilder
            };

            return VWO.Init(options);
        }

        private string SerializeVariables(GetFlag flag)
        {
            return JsonConvert.SerializeObject(flag.GetVariables());
        }

        /// <summary>
        /// Case 1: Standard bucketing (no custom seed)
        /// Two different users ('WingifyVWO', 'RandomUserVWO') with NO bucketing seed.
        /// They should be bucketed into different variations based on their User IDs.
        /// </summary>
        [Fact]
        public void ShouldAssignDifferentVariationsToUsersWithDifferentUserIds()
        {
            var vwoClient = InitializeVwoClient();
            Assert.NotNull(vwoClient);

            var user1Flag = vwoClient.GetFlag(FeatureKey, new VWOContext { Id = "WingifyVWO" });
            var user2Flag = vwoClient.GetFlag(FeatureKey, new VWOContext { Id = "RandomUserVWO" });

            Assert.NotEqual(SerializeVariables(user1Flag), SerializeVariables(user2Flag));
        }

        /// <summary>
        /// Case 2: Bucketing Seed Provided
        /// Two different users ('WingifyVWO', 'RandomUserVWO') are provided with the SAME bucketingSeed.
        /// Since the seed is identical, they MUST get the same variation.
        /// </summary>
        [Fact]
        public void ShouldAssignSameVariationToDifferentUsersWithSameBucketingSeed()
        {
            var vwoClient = InitializeVwoClient();
            Assert.NotNull(vwoClient);

            string sameBucketingSeed = "common-seed-123";

            var user1Flag = vwoClient.GetFlag(FeatureKey, new VWOContext
            {
                Id = "WingifyVWO",
                BucketingSeed = sameBucketingSeed
            });

            var user2Flag = vwoClient.GetFlag(FeatureKey, new VWOContext
            {
                Id = "RandomUserVWO",
                BucketingSeed = sameBucketingSeed
            });

            Assert.Equal(SerializeVariables(user1Flag), SerializeVariables(user2Flag));
        }


        /// <summary>
        /// Case 3: Different Seeds
        /// The SAME User ID is used, but with DIFFERENT bucketing seeds.
        /// The SDK should bucket based on the seed. Since we use seeds known
        /// to produce different results ('WingifyVWO' vs 'RandomUserVWO'), the outcomes should differ.
        /// </summary>
        [Fact]
        public void ShouldAssignDifferentVariationsToUsersWithDifferentBucketingSeeds()
        {
            var vwoClient = InitializeVwoClient();
            Assert.NotNull(vwoClient);

            var user1Flag = vwoClient.GetFlag(FeatureKey, new VWOContext
            {
                Id = "sameId",
                BucketingSeed = "WingifyVWO"
            });

            var user2Flag = vwoClient.GetFlag(FeatureKey, new VWOContext
            {
                Id = "sameId",
                BucketingSeed = "RandomUserVWO"
            });

            Assert.NotEqual(SerializeVariables(user1Flag), SerializeVariables(user2Flag));
        }

        /// <summary>
        /// Case 4: Empty String Seed
        /// bucketingSeed is provided but it's an empty string.
        /// Empty string is falsy, so it should fall back to userId.
        /// Different users should get different variations.
        /// </summary>
        [Fact]
        public void ShouldFallbackToUserIdWhenBucketingSeedIsEmptyString()
        {
            var vwoClient = InitializeVwoClient();
            Assert.NotNull(vwoClient);

            var user1Flag = vwoClient.GetFlag(FeatureKey, new VWOContext
            {
                Id = "WingifyVWO",
                BucketingSeed = ""
            });

            var user2Flag = vwoClient.GetFlag(FeatureKey, new VWOContext
            {
                Id = "RandomUserVWO",
                BucketingSeed = ""
            });

            Assert.NotEqual(SerializeVariables(user1Flag), SerializeVariables(user2Flag));
        }

        /// <summary>
        /// Case 6: Forced variation via segment should remain stable for the same user.
        /// A whitelisted user should receive the same variation for featureTwo even if a
        /// bucketingSeed is added on a later getFlag call.
        /// </summary>
        [Fact]
        public void ShouldReturnSameVariationForWhitelistedUserWithOrWithoutBucketingSeed()
        {
            var vwoClient = InitializeVwoClient();
            Assert.NotNull(vwoClient);

            const string userId = "whitelistedUser1";

            var flagWithoutSeed = vwoClient.GetFlag(FeatureTwoKey, new VWOContext
            {
                Id = userId
            });

            var flagWithSeed = vwoClient.GetFlag(FeatureTwoKey, new VWOContext
            {
                Id = userId,
                BucketingSeed = "randomSeed"
            });

            Assert.True(flagWithoutSeed.IsEnabled());
            Assert.True(flagWithSeed.IsEnabled());
            Assert.NotEmpty(flagWithoutSeed.GetVariables());
            Assert.Equal(SerializeVariables(flagWithoutSeed), SerializeVariables(flagWithSeed));
            Assert.Equal("abc", flagWithoutSeed.GetVariable("v1", null));
            Assert.Equal("xyz", flagWithoutSeed.GetVariable("v2", null));
        }

        /// <summary>
        /// Case 7: No bucketing seed, same custom salt in both testing rules.
        /// For the same user, both flags should resolve to the same variation
        /// because the testing campaigns use the same salt.
        /// </summary>
        [Fact]
        public void ShouldAssignSameVariationAcrossFlagsForSameUserWhenCustomSaltIsPresentAndSeedIsMissing()
        {
            var vwoClient = InitializeVwoClient(SameSaltSettingsFile, SameSaltSdkKey, SameSaltAccountId);
            Assert.NotNull(vwoClient);

            for (int i = 1; i <= 10; i++)
            {
                string userId = $"user{i}";
                var flag1 = vwoClient.GetFlag(SameSaltFeatureOneKey, new VWOContext { Id = userId });
                var flag2 = vwoClient.GetFlag(SameSaltFeatureTwoKey, new VWOContext { Id = userId });

                Assert.True(flag1.IsEnabled());
                Assert.True(flag2.IsEnabled());
                Assert.Equal(SerializeVariables(flag1), SerializeVariables(flag2));
            }
        }

        /// <summary>
        /// Case 8: Same bucketing seed and same custom salt in both testing rules.
        /// All users should receive the same variation, and both flags should resolve
        /// to that same variation for each user.
        /// </summary>
        [Fact]
        public void ShouldAssignSameVariationAcrossAllUsersWhenCustomSaltAndCommonBucketingSeedArePresent()
        {
            var vwoClient = InitializeVwoClient(SameSaltSettingsFile, SameSaltSdkKey, SameSaltAccountId);
            Assert.NotNull(vwoClient);

            const string commonBucketingSeed = "common_seed_456";
            var variationsAssigned = new HashSet<string>();

            for (int i = 1; i <= 10; i++)
            {
                string userId = $"user{i}";
                var flag1 = vwoClient.GetFlag(SameSaltFeatureOneKey, new VWOContext
                {
                    Id = userId,
                    BucketingSeed = commonBucketingSeed
                });
                var flag2 = vwoClient.GetFlag(SameSaltFeatureTwoKey, new VWOContext
                {
                    Id = userId,
                    BucketingSeed = commonBucketingSeed
                });

                Assert.True(flag1.IsEnabled());
                Assert.True(flag2.IsEnabled());

                string serializedVariables = SerializeVariables(flag1);
                Assert.Equal(serializedVariables, SerializeVariables(flag2));
                variationsAssigned.Add(serializedVariables);
            }

            Assert.Single(variationsAssigned);
        }
    }
}
