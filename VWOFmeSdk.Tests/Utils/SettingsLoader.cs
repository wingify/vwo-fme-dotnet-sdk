using System.IO;
using Newtonsoft.Json.Linq;

namespace VWOFlagTesting.Utils
{
    public static class SettingsLoader
    {
        public static JObject LoadSettings(string fileName)
        {
            // Construct the absolute path to the Resources folder
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName);

            // Validate if the file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Settings file not found: {filePath}");
            }

            // Read and parse the JSON file
            return JObject.Parse(File.ReadAllText(filePath));
        }
    }
}
