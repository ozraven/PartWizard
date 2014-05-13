using System;
using System.IO;

using UnityEngine;

namespace PartWizard
{
    internal static class Configuration
    {
        // TODO: Refactor this to avoid static classes and wonky path manipulation.

        private const string File = "GameData/PartWizard/partwizard.cfg";
        private const string SettingsNodeName = "PART_WIZARD_SETTINGS";
        private const string KeyRectX = "x";
        private const string KeyRectY = "y";
        private const string KeyRectWidth = "width";
        private const string KeyRectHeight = "height";

        private static string Path = System.IO.Path.Combine(KSPUtil.ApplicationRootPath, File);
        private static ConfigNode Root = ConfigNode.Load(Configuration.Path) ?? new ConfigNode();
       
        static Configuration()
        {
            if(Configuration.Root.GetNode(Configuration.SettingsNodeName) == null)
            {
                Configuration.Root.AddNode(Configuration.SettingsNodeName);
            }            
        }
        
        public static void Save()
        {
            Configuration.Root.Save(Configuration.Path);
        }

        private static void ValidateKeyName(string key)
        {
            if(key == null)
                throw new ArgumentNullException("key");

            if(string.IsNullOrEmpty(key))
                throw new ArgumentException("Invalid key name.", "key");
        }

        //public static void SetValue(string key, float value)
        //{
        //    Configuration.ValidateKeyName(key);

        //    ConfigNode settingsNode = Configuration.Root.GetNode(Configuration.SettingsNodeName);

        //    Configuration.SetValue(settingsNode, key, value);
        //}

        //public static void SetValue(string key, int value)
        //{
        //    Configuration.ValidateKeyName(key);

        //    ConfigNode settingsNode = Configuration.Root.GetNode(Configuration.SettingsNodeName);

        //    Configuration.SetValue(settingsNode, key, value);
        //}

        private static void SetValue(ConfigNode node, string key, float value)
        {
            if(node == null)
                throw new ArgumentNullException("node");

            // key is validated by public facing methods.

            if(node.HasValue(key))
            {
                node.RemoveValue(key);
            }

            node.AddValue(key, value);
        }

        public static void SetValue(string key, Rect value)
        {
            Configuration.ValidateKeyName(key);

            ConfigNode settingsNode = Configuration.Root.GetNode(Configuration.SettingsNodeName);

            if(settingsNode.HasNode(key))
            {
                settingsNode.RemoveNode(key);
            }

            ConfigNode rectNode = settingsNode.AddNode(key);

            Configuration.SetValue(rectNode, KeyRectX, value.x);
            Configuration.SetValue(rectNode, KeyRectY, value.y);
            Configuration.SetValue(rectNode, KeyRectWidth, value.width);
            Configuration.SetValue(rectNode, KeyRectHeight, value.height);
        }

        private static float GetValue(ConfigNode node, string key, float defaultValue)
        {
            if(node == null)
                throw new ArgumentNullException("node");

            // key is validated by public facing methods.

            float result = default(float);

            if(!float.TryParse(node.GetValue(key), out result))
            {
                result = defaultValue;
            }

            return result;
        }

        //public static float GetValue(string key, float defaultValue)
        //{
        //    return Configuration.GetValue(Configuration.Root.GetNode(Configuration.SettingsNodeName), key, defaultValue);
        //}

        //private static int GetValue(ConfigNode node, string key, int defaultValue)
        //{
        //    if(node == null)
        //        throw new ArgumentNullException("node");

        //    // key is validated by public facing methods.

        //    int result = default(int);

        //    if(!int.TryParse(node.GetValue(key), out result))
        //    {
        //        result = defaultValue;
        //    }

        //    return result;
        //}

        //public static int GetValue(string key, int defaultValue)
        //{
        //    return Configuration.GetValue(Configuration.Root.GetNode(Configuration.SettingsNodeName), key, defaultValue);
        //}

        public static Rect GetValue(string key, Rect defaultValue)
        {
            Configuration.ValidateKeyName(key);

            Rect result = defaultValue;

            ConfigNode settingsNode = Configuration.Root.GetNode(Configuration.SettingsNodeName);

            if(settingsNode.HasNode(key))
            {
                ConfigNode rectNode = settingsNode.GetNode(key);

                result.x = Configuration.GetValue(rectNode, KeyRectX, defaultValue.x);
                result.y = Configuration.GetValue(rectNode, KeyRectY, defaultValue.y);
                result.width = Configuration.GetValue(rectNode, KeyRectWidth, defaultValue.width);
                result.height = Configuration.GetValue(rectNode, KeyRectHeight, defaultValue.height);
            }

            return result;
        }
    }
}
