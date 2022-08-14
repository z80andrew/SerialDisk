using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using Z80andrew.SerialDisk.Models;

namespace Z80andrew.SerialDisk.Utilities
{
    public static class ConfigurationHelper
    {
        public static string ApplicationVersion => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        // used to denote non-release versions
        public const string VERSION_TYPE = "beta 2";

        public const string VERSION_TAG = "3.0-beta2";

        public static bool IsNewVersionAvailable(string gitHubAPIResponse)
        {
            var json = JsonDocument.Parse(gitHubAPIResponse);
            var latestVersionTag = json.RootElement.GetProperty("tag_name").GetString();
            return !latestVersionTag.Equals(VERSION_TAG, System.StringComparison.InvariantCultureIgnoreCase);
        }

        public static string GetLatestVersionUrl(string gitHubAPIResponse)
        {
            var json = JsonDocument.Parse(gitHubAPIResponse);
            var latestVersionUrl = json.RootElement.GetProperty("html_url").GetString();
            return latestVersionUrl;
        }

        public static ApplicationSettings GetDefaultApplicationSettings()
        {
            ApplicationSettings defaultAppSettings;

            var resourceFiles = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var defaultConfigName = $"Resources.default_config_{OSHelper.OperatingSystemName.ToLower()}.json";
            var defaultConfigResourceName = resourceFiles.Where(res => res.Contains(defaultConfigName)).Single();

            using (var defaultConfigStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(defaultConfigResourceName))
            {
                DataContractJsonSerializer appSettingsSerializer = new DataContractJsonSerializer(typeof(ApplicationSettings));
                defaultAppSettings = (ApplicationSettings)appSettingsSerializer.ReadObject(defaultConfigStream);
            }

            return defaultAppSettings;
        }
    }
}
