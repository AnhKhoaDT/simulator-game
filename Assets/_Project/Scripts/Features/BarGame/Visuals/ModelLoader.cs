using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Loads imported models from Resources and fits them to a target size, so any source unit scale works.
    internal static class ModelLoader
    {
        // Below this a measured size is treated as invalid instead of being divided by.
        private const float MinValidExtent = 0.001f;

        // Reused for measuring skinned meshes, so creating characters does not allocate a mesh each time.
        private static readonly List<Vector3> BakeVertices = new List<Vector3>();
        private static Mesh _bakeMesh;

        // Returns a wrapper object holding the model, or null if the model is missing.
        public static GameObject Instantiate(string resourcePath, Transform parent, string name)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                return null;
            }

            var wrapper = new GameObject(name);
            wrapper.transform.SetParent(parent, false);

            var model = Object.Instantiate(prefab, wrapper.transform);
            model.name = prefab.name;
            return wrapper;
        }

        // The Animator on the model root, added if the importer did not create one.
        public static Animator EnsureAnimator(GameObject wrapper)
        {
            var model = wrapper.transform.GetChild(0).gameObject;
            var animator = model.GetComponent<Animator>();
            return animator != null ? animator : model.AddComponent<Animator>();
        }

        public static void ApplyTexture(GameObject wrapper, Texture texture)
        {
            if (texture == null)
            {
                return;
            }

            foreach (var renderer in wrapper.GetComponentsInChildren<Renderer>())
            {
                renderer.material.mainTexture = texture;
            }
        }

        public static void Tint(GameObject wrapper, Color color)
        {
            foreach (var renderer in wrapper.GetComponentsInChildren<Renderer>())
            {
                renderer.material.color = color;
            }
        }

        // Stretch to an exact box, centered on the wrapper in XZ and resting on its Y.
        public static void FitToBox(GameObject wrapper, Vector3 size)
        {
            if (!TryGetBounds(wrapper, out var bounds) || !IsValid(wrapper, bounds.size.x, bounds.size.y, bounds.size.z))
            {
                return;
            }

            wrapper.transform.localScale = new Vector3(
                size.x / bounds.size.x,
                size.y / bounds.size.y,
                size.z / bounds.size.z);
            Recenter(wrapper);
        }

        // Uniform scale to a target height, keeping the model's proportions.
        public static void FitToHeight(GameObject wrapper, float height)
        {
            if (!TryGetBounds(wrapper, out var bounds) || !IsValid(wrapper, bounds.size.y))
            {
                return;
            }

            wrapper.transform.localScale = Vector3.one * (height / bounds.size.y);
            Recenter(wrapper);
        }

        private static bool IsValid(GameObject wrapper, params float[] extents)
        {
            foreach (var extent in extents)
            {
                if (extent < MinValidExtent)
                {
                    Debug.LogWarning($"ModelLoader could not measure '{wrapper.name}'; leaving its scale unchanged.");
                    return false;
                }
            }

            return true;
        }

        private static void Recenter(GameObject wrapper)
        {
            if (!TryGetBounds(wrapper, out var bounds))
            {
                return;
            }

            var origin = wrapper.transform.position;
            var shift = new Vector3(origin.x - bounds.center.x, origin.y - bounds.min.y, origin.z - bounds.center.z);
            wrapper.transform.GetChild(0).position += shift;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var found = false;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var rendererBounds = renderer is SkinnedMeshRenderer skinned
                    ? SkinnedWorldBounds(skinned)
                    : renderer.bounds;

                if (found)
                {
                    bounds.Encapsulate(rendererBounds);
                }
                else
                {
                    bounds = rendererBounds;
                    found = true;
                }
            }

            return found;
        }

        // Skinned bounds are unreliable before the first rendered frame, so skin the mesh on the CPU
        // in its current pose and measure the real vertices in world space.
        private static Bounds SkinnedWorldBounds(SkinnedMeshRenderer skinned)
        {
            if (_bakeMesh == null)
            {
                _bakeMesh = new Mesh();
            }

            // Without scale, baked vertices are in the renderer's local space.
            skinned.BakeMesh(_bakeMesh, false);
            _bakeMesh.GetVertices(BakeVertices);

            var matrix = skinned.transform.localToWorldMatrix;
            if (BakeVertices.Count == 0)
            {
                return new Bounds(skinned.transform.position, Vector3.zero);
            }

            var result = new Bounds(matrix.MultiplyPoint3x4(BakeVertices[0]), Vector3.zero);
            for (var i = 1; i < BakeVertices.Count; i++)
            {
                result.Encapsulate(matrix.MultiplyPoint3x4(BakeVertices[i]));
            }

            return result;
        }
    }
}
