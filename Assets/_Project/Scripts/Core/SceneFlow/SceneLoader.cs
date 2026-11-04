using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Startup.Core.SceneFlow
{
    // The only allowed scene-transition entry point. Do not call SceneManager directly elsewhere.
    public static class SceneLoader
    {
        private static SceneLoaderRunner _runner;

        public static void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("SceneLoader.LoadScene called with null or empty scene name.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        // Async load with an optional post-load hook (a Phase 2 fade can subscribe here).
        public static void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("SceneLoader.LoadSceneAsync called with null or empty scene name.");
                return;
            }

            EnsureRunner();
            _runner.StartCoroutine(LoadRoutine(sceneName, onComplete));
        }

        private static void EnsureRunner()
        {
            if (_runner != null)
            {
                return;
            }

            // Persistent runner only needed to drive the load coroutine.
            var go = new GameObject("SceneLoaderRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<SceneLoaderRunner>();
        }

        private static IEnumerator LoadRoutine(string sceneName, Action onComplete)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            onComplete?.Invoke();
        }

        private sealed class SceneLoaderRunner : MonoBehaviour
        {
        }
    }
}
