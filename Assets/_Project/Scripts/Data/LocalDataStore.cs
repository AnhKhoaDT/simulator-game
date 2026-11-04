using System;
using System.IO;
using UnityEngine;

namespace Startup.Data
{
    // Offline-first IDataStore: one JSON file per key under persistentDataPath.
    public class LocalDataStore : IDataStore
    {
        public void Save<T>(string key, T data)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("LocalDataStore.Save called with null or empty key.");
                return;
            }

            var wrapper = new Wrapper<T> { value = data };
            var json = JsonUtility.ToJson(wrapper);
            File.WriteAllText(GetPath(key), json);
        }

        public T Load<T>(string key, T defaultValue)
        {
            if (string.IsNullOrEmpty(key) || !Has(key))
            {
                return defaultValue;
            }

            try
            {
                var json = File.ReadAllText(GetPath(key));
                var wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
                return wrapper != null ? wrapper.value : defaultValue;
            }
            catch (Exception e)
            {
                Debug.LogError($"LocalDataStore.Load failed for key '{key}': {e.Message}");
                return defaultValue;
            }
        }

        public bool Has(string key)
        {
            return !string.IsNullOrEmpty(key) && File.Exists(GetPath(key));
        }

        public void Delete(string key)
        {
            if (Has(key))
            {
                File.Delete(GetPath(key));
            }
        }

        private static string GetPath(string key)
        {
            return Path.Combine(Application.persistentDataPath, key + ".json");
        }

        // JsonUtility cannot serialize a bare value/primitive, so wrap it in a Serializable class.
        [Serializable]
        private class Wrapper<T>
        {
            public T value;
        }
    }
}
