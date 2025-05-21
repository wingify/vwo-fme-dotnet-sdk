using Newtonsoft.Json.Linq;
using VWOFlagTesting.Utils;
using VWOFmeSdk;
using VWOFmeSdk.Models.User;
using Xunit;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VWOFlagTesting.Tests
{
    public class FeatureFlagTests
    {
        [Theory]
        [InlineData("GetFlagWithoutStorage.json")]
        [InlineData("GetFlagWithStorage.json")]
        [InlineData("GetFlagMegRandom.json")]
        [InlineData("GetFlagMegAdvance.json")]
        public void ValidateFeatureFlags(string testCaseFile)
        {
            // Load test cases from the specified JSON file
            var testCases = TestCaseLoader.LoadTestCases(testCaseFile);

            foreach (var testCase in testCases)
            {
                // Load settings for the test case
                var settings = SettingsLoader.LoadSettings((string)testCase.settings);
                string settingsJson = settings.ToString();
                var logger = new Dictionary<string, object>
                {
                    { "level", "DEBUG" }
                };

                // Initialize the VWO builder
                var options = new VWOInitOptions
                {
                    SdkKey = "your-sdk-key",  // Replace with your actual SDK key
                    AccountId = 123456,       // Replace with your actual account ID
                    Logger = logger
                };
    
                var vwoBuilder = new VWOBuilder(options);
                
                
                vwoBuilder.SetLogger();         // Set the logger
                //vwoBuilder.SetSettings(settings); // Set the settings for the test case
                vwoBuilder.SetSettings(settingsJson); // Pass the JSON string to SetSettings

                var opt = new VWOInitOptions
                {
                    SdkKey = "your-sdk-key",  // Replace with your actual SDK key
                    AccountId = 123456,       // Replace with your actual account ID
                    VwoBuilder = vwoBuilder  // Pass the VWOBuilder instance
                };

                // Initialize the VWO client
                var vwoClient = VWO.Init(opt);

                // Ensure the VWO client is initialized successfully
                Assert.NotNull(vwoClient);

                // Parse the context from the test case
                var context = new VWOContext
                {
                    Id = testCase.context["id"].ToString(), // Ensure "id" is present in the test case
                    UserAgent = testCase.context.ContainsKey("userAgent") ? testCase.context["userAgent"].ToString() : "",
                    IpAddress = testCase.context.ContainsKey("ipAddress") ? testCase.context["ipAddress"].ToString() : "",
                    CustomVariables = testCase.context.ContainsKey("customVariables")
                        ? JsonConvert.DeserializeObject<Dictionary<string, object>>(testCase.context["customVariables"].ToString())
                        : new Dictionary<string, object>(),
                    VariationTargetingVariables = testCase.context.ContainsKey("variationTargetingVariables")
                        ? JsonConvert.DeserializeObject<Dictionary<string, object>>(testCase.context["variationTargetingVariables"].ToString())
                        : new Dictionary<string, object>()
                };

                // System.Console.WriteLine("testCase.featureKey - ");
                // System.Console.WriteLine(testCase.featureKey.ToString());

                // Fetch the flag result
                var result = vwoClient.GetFlag(testCase.featureKey.ToString(), context);

                // Validate the expected results
                Assert.Equal((bool)testCase.expectation["isEnabled"], result.IsEnabled());
                Assert.Equal((int)result.GetVariable("int", 1), (int)testCase.expectation["intVariable"]);
                Assert.Equal((string)result.GetVariable("string", "VWO"), (string)testCase.expectation["stringVariable"]);
                Assert.Equal((float)(double)result.GetVariable("float", 1.1), (float)(double)testCase.expectation["floatVariable"]);
                Assert.Equal((bool)result.GetVariable("boolean", false), (bool)testCase.expectation["booleanVariable"]);



                // Validate JSON object comparison
                // var expectedJsonVariable = JsonConvert.SerializeObject(testCase.expectation["jsonVariable"]);
                // var actualJsonVariable = JsonConvert.SerializeObject(result.GetVariable("json", new Dictionary<string, object>()));
                // Assert.Equal(expectedJsonVariable, actualJsonVariable);

                
                // Additional validation for storage data, if present
                if (testCase.expectation.storageData != null)
                {
                    Assert.Equal(testCase.expectation.storageData.rolloutKey, result.Storage.RolloutKey);
                    Assert.Equal(testCase.expectation.storageData.rolloutVariationId, result.Storage.RolloutVariationId);

                    if (testCase.expectation.storageData.experimentKey != null)
                    {
                        Assert.Equal(testCase.expectation.storageData.experimentKey, result.Storage.ExperimentKey);
                        Assert.Equal(testCase.expectation.storageData.experimentVariationId, result.Storage.ExperimentVariationId);
                    }
                }
            }
        }
    }
}
