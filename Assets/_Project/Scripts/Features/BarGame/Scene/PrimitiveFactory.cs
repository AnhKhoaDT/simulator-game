using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Builds colored primitives; materials are cached per color to keep draw calls and memory low.
    internal static class PrimitiveFactory
    {
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
        private static Material _template;

        public static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            return Shape(PrimitiveType.Cube, name, parent, localPosition, localScale, color);
        }

        public static GameObject Shape(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Color color)
        {
            var shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localScale = localScale;
            shape.GetComponent<Renderer>().sharedMaterial = GetMaterial(color);
            return shape;
        }

        public static Material GetMaterial(Color color)
        {
            if (Materials.TryGetValue(color, out var cached) && cached != null)
            {
                return cached;
            }

            var material = new Material(GetTemplate()) { color = color };
            Materials[color] = material;
            return material;
        }

        // Reuse the render pipeline's default primitive material so this works under URP without Shader.Find.
        private static Material GetTemplate()
        {
            if (_template == null)
            {
                var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _template = probe.GetComponent<Renderer>().sharedMaterial;
                Object.Destroy(probe);
            }

            return _template;
        }
    }
}
