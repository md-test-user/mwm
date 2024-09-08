using System;
using System.IO;
using System.Xml.Linq;

namespace mwm
{
    public static class ConfigManager
    {
        // Path for the configuration file
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "mwm",
            "mwm.config");

        public static void Initialize()
        {
            EnsureConfigFileExists();  // Ensure the config file exists
        }

        // Ensure the configuration file exists, and if not, create it with default settings
        private static void EnsureConfigFileExists()
        {
            string configDir = Path.GetDirectoryName(ConfigFilePath);
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            // Only create the config file if it doesn't already exist
            if (!File.Exists(ConfigFilePath))
            {
                CreateDefaultConfigFile();
            }
        }

        // Create a default config file
        private static void CreateDefaultConfigFile()
        {

            XDocument config = new XDocument(
                new XElement("configuration",
                    new XElement("appSettings",
                        new XElement("add", new XAttribute("key", "DefaultFolder"), new XAttribute("value", "")),
                        new XElement("add", new XAttribute("key", "ShowHiddenFiles"), new XAttribute("value", "false")),
                        new XElement("add", new XAttribute("key", "BlacklistExtensions"), new XAttribute("value", ".lnk,.exe,.url,.bat,.cmd,.ps1"))
                    )
                )
            );

            // Save the config file
            config.Save(ConfigFilePath);
        }

        // Reads a configuration setting by key and expands environment variables
        public static string ReadSetting(string key)
        {
            if (!File.Exists(ConfigFilePath)) return null;

            var config = XDocument.Load(ConfigFilePath);
            foreach (var element in config.Root.Element("appSettings").Elements("add"))
            {
                if (element.Attribute("key")?.Value == key)
                {
                    string value = element.Attribute("value")?.Value;

                    // Expand any environment variables present in the value
                    if (!string.IsNullOrEmpty(value))
                    {
                        return Environment.ExpandEnvironmentVariables(value);
                    }

                    return value;
                }
            }
            return null;
        }

        // Writes a configuration setting by key
        public static void WriteSetting(string key, string value)
        {
            if (!File.Exists(ConfigFilePath)) return;

            var config = XDocument.Load(ConfigFilePath);
            var appSettings = config.Root.Element("appSettings");

            var existingSetting = appSettings.Elements("add")
                .FirstOrDefault(e => e.Attribute("key")?.Value == key);

            if (existingSetting != null)
            {
                existingSetting.SetAttributeValue("value", value);
            }
            else
            {
                appSettings.Add(new XElement("add", new XAttribute("key", key), new XAttribute("value", value)));
            }

            config.Save(ConfigFilePath);
        }
    }
}
