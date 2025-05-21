using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace VWOFlagTesting.Utils
{
    public static class TestCaseLoader
    {
        public static List<dynamic> LoadTestCases(string fileName)
        {
            // Construct the relative path to the Expectations folder
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Expectations", fileName);

            // Read and deserialize the JSON test case file
            return JsonConvert.DeserializeObject<List<dynamic>>(File.ReadAllText(filePath));
        }
    }
}
