using AtariST.SerialDisk.Models;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;

namespace AtariST.SerialDisk.Utilities
{
    public static class ConfigurationHelper
    {
        public static ApplicationSettings GetDefaultApplicationSettings()
        {
            ApplicationSettings defaultAppSettings;

            var resourceFiles = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var defaultConfigName = $"Resources.default_config_{ OSHelper.OperatingSystemName.ToLower()}.json";
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
