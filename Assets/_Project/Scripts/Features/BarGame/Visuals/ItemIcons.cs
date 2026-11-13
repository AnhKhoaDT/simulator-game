using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Item thumbnails for the shop and inventory: each model is photographed once by a hidden camera into a
    // RenderTexture, so new furniture needs no hand-made icon.
    internal static class ItemIcons
    {
        private const int Size = 192;
        private const int IconLayer = 31;
        private const float FieldOfView = 28f;
        private const float Padding = 1.08f;
        private const float ViewPitch = 24f;
        private const float ViewYaw = 35f;
        private const float LightIntensity = 1.3f;
        private const float NeutralAmbient = 0.6f;

        private static readonly Vector3 Stage = new Vector3(0f, -500f, 0f);
        private static readonly Color Backdrop = new Color(0.2f, 0.24f, 0.3f, 1f);
        private static readonly Dictionary<string, RenderTexture> Cache = new Dictionary<string, RenderTexture>();

        private static Camera _camera;
        private static Light _light;

        // The icon for a model path, or null if the model is missing.
        public static RenderTexture Get(string modelPath)
        {
            if (Cache.TryGetValue(modelPath, out var cached) && cached != null)
            {
                return cached;
            }

            var icon = Render(modelPath);
            Cache[modelPath] = icon;
            return icon;
        }

        private static RenderTexture Render(string modelPath)
        {
            var prefab = Resources.Load<GameObject>(modelPath);
            if (prefab == null)
            {
                return null;
            }

            EnsureRig();
            var instance = Object.Instantiate(prefab, Stage, Quaternion.identity);
            SetLayer(instance, IconLayer);
            var bounds = MeasureWorld(instance);

            var texture = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32) { name = "Icon_" + prefab.name };
            var rotation = Quaternion.Euler(ViewPitch, ViewYaw, 0f);
            var radius = Mathf.Max(bounds.extents.magnitude, 0.05f);
            var distance = radius / Mathf.Sin(FieldOfView * 0.5f * Mathf.Deg2Rad) * Padding;
            _camera.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * distance, rotation);
            _camera.farClipPlane = distance + radius * 2f;
            _camera.targetTexture = texture;

            var ambient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = Color.white * NeutralAmbient;
            var others = DisableOtherLights();
            _light.enabled = true;

            _camera.Render();

            _light.enabled = false;
            foreach (var light in others)
            {
                light.enabled = true;
            }

            RenderSettings.ambientLight = ambient;
            _camera.targetTexture = null;
            Object.Destroy(instance);
            return texture;
        }

        private static void EnsureRig()
        {
            if (_camera != null && _light != null)
            {
                return;
            }

            var rig = new GameObject("ItemIconRig");
            rig.hideFlags = HideFlags.HideAndDontSave;

            _camera = rig.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Backdrop;
            _camera.cullingMask = 1 << IconLayer;
            _camera.fieldOfView = FieldOfView;
            _camera.nearClipPlane = 0.05f;

            var lightObject = new GameObject("ItemIconLight");
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.transform.rotation = Quaternion.Euler(45f, 20f, 0f);
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Directional;
            _light.intensity = LightIntensity;
            _light.shadows = LightShadows.None;
            _light.enabled = false;
        }

        // Scene lights would tint the icon differently by time of day, so they sleep for the shot.
        private static List<Light> DisableOtherLights()
        {
            var switched = new List<Light>();
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light != _light && light.enabled)
                {
                    light.enabled = false;
                    switched.Add(light);
                }
            }

            return switched;
        }

        private static void SetLayer(GameObject root, int layer)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        // Skinned meshes report no bounds before they have rendered, so use their rest-pose local bounds.
        private static Bounds MeasureWorld(GameObject root)
        {
            var found = false;
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                Bounds rendered;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    var local = skinned.localBounds;
                    rendered = new Bounds(skinned.transform.TransformPoint(local.center),
                        Vector3.Scale(local.size, skinned.transform.lossyScale));
                }
                else
                {
                    rendered = renderer.bounds;
                }

                if (found)
                {
                    bounds.Encapsulate(rendered);
                }
                else
                {
                    bounds = rendered;
                    found = true;
                }
            }

            return bounds;
        }
    }
}
