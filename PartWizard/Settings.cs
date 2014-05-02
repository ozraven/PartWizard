using System;
using System.IO;

namespace PartWizard
{
    internal static class Configuration
    {
        // TODO: Refactor this to avoid static classes and wonky path manipulation.

        private const string CONFIGURATION_FILE = "GameData/PartWizard/partwizard.cfg";
        private static string ConfigurationPath = Path.Combine(KSPUtil.ApplicationRootPath, CONFIGURATION_FILE);
        private static ConfigNode Settings = ConfigNode.Load(Configuration.ConfigurationPath) ?? new ConfigNode();
        
        public static void Save()
        {
            Configuration.Settings.Save(Configuration.ConfigurationPath);
        }

        public static void SetValue(string key, float value)
        {
            if(Configuration.Settings.HasValue(key))
            {
                Configuration.Settings.RemoveValue(key);
            }

            Configuration.Settings.AddValue(key, value);
        }

        public static float GetValue(string key, float defaultValue)
        {
            float result = default(float);

            if(!float.TryParse(Configuration.Settings.GetValue(key), out result))
            {
                result = defaultValue;
            }

            return result;
        }
    }
}
