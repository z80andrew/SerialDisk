using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using Z80andrew.SerialDisk.Models;
using static Z80andrew.SerialDisk.Common.Constants;

namespace Z80andrew.SerialDisk.Utilities
{
    public static class ConfigurationHelper
    {
        public static string ApplicationVersion => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        public static readonly ReleaseType RELEASE_TYPE = ReleaseType.Beta;
        public static readonly string RELEASE_NAME = "beta 2";
        public static readonly string RELEASE_TAG = "3.0-beta2";

        public static bool IsNewVersionAvailable(string gitHubAPIResponse)
        {
            bool isNewVersionAvailable = false;
            var json = JsonDocument.Parse(gitHubAPIResponse);
            var versions = json.RootElement.EnumerateArray();

            if (RELEASE_TYPE == ReleaseType.Release)
            {
                var releaseVersions = versions.Where(ver => ver.GetProperty("prerelease").GetBoolean() == false);
                if (!String.Equals(releaseVersions.First().GetProperty("tag_name").GetString(), RELEASE_TAG, StringComparison.InvariantCultureIgnoreCase)) isNewVersionAvailable = true;
            }

            else
            {
                if (!String.Equals(versions.First().GetProperty("tag_name").GetString(), RELEASE_TAG, StringComparison.InvariantCultureIgnoreCase)) isNewVersionAvailable = true;
            }

            return isNewVersionAvailable;
        }

        public static string GetLatestVersionUrl(string gitHubAPIResponse)
        {
            string latestVersionUrl = string.Empty;
            var json = JsonDocument.Parse(gitHubAPIResponse);
            var versions = json.RootElement.EnumerateArray();

            if (RELEASE_TYPE == ReleaseType.Release)
            {
                var releaseVersions = versions.Where(ver => ver.GetProperty("prerelease").GetBoolean() == false);
                latestVersionUrl = releaseVersions.First().GetProperty("html_url").GetString();
            }

            else
            {
                latestVersionUrl = versions.First().GetProperty("html_url").GetString();
            }
            
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
