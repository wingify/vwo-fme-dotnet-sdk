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

using System;
using Newtonsoft.Json;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.Schemas;
using VWOFlagTesting.Utils;
using Xunit;

namespace VWOFlagTesting.Tests
{
    public class SettingsSchemaValidationTests
    {
        private readonly SettingsSchema settingsSchemaValidation;

        public SettingsSchemaValidationTests()
        {
            settingsSchemaValidation = new SettingsSchema();
        }

        [Fact]
        public void SettingsWithWrongTypeForValues_ShouldFailValidation()
        {
            // Arrange
            var settings = SettingsLoader.LoadSettings("SETTINGS_WITH_WRONG_TYPE_FOR_VALUES.json");
            
            // Act & Assert
            // This test should fail during JSON deserialization because the JSON has wrong types
            // (accountId: true instead of integer, sdkKey: 123456 instead of string)
            Assert.Throws<JsonReaderException>(() => 
            {
                JsonConvert.DeserializeObject<Settings>(settings.ToString());
            });
        }

        [Fact]
        public void SettingsWithExtraKeyAtRootLevel_ShouldNotFailValidation()
        {
            // Arrange
            var settings = SettingsLoader.LoadSettings("SETTINGS_WITH_EXTRA_KEYS_AT_ROOT_LEVEL.json");
            var settingsObject = JsonConvert.DeserializeObject<Settings>(settings.ToString());

            // Act
            var result = settingsSchemaValidation.IsSettingsValid(settingsObject);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SettingsWithExtraKeyInsideObjects_ShouldNotFailValidation()
        {
            // Arrange
            var settings = SettingsLoader.LoadSettings("SETTINGS_WITH_EXTRA_KEYS_INSIDE_OBJECTS.json");
            var settingsObject = JsonConvert.DeserializeObject<Settings>(settings.ToString());

            // Act
            var result = settingsSchemaValidation.IsSettingsValid(settingsObject);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SettingsWithNoFeatureAndCampaign_ShouldNotFailValidation()
        {
            // Arrange
            var settings = SettingsLoader.LoadSettings("SETTINGS_WITH_NO_FEATURE_AND_CAMPAIGN.json");
            var settingsObject = JsonConvert.DeserializeObject<Settings>(settings.ToString());

            // Act
            var result = settingsSchemaValidation.IsSettingsValid(settingsObject);

            // Assert
            Assert.True(result);
        }
    }
}
