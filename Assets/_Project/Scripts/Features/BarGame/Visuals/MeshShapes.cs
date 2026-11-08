using UnityEngine;

namespace Startup.Features.BarGame
{
    // Procedural meshes for shapes Unity has no primitive for. Meshes are cached and shared.
    internal static class MeshShapes
    {
        private static Mesh _gableRoof;

        // A gable roof on its parent's origin: ridge along X, slopes facing -Z and +Z, gable triangles at the X ends.
        public static GameObject GableRoof(string name, Transform parent, Vector3 localPosition, Vector3 size, Color color)
        {
            var roof = new GameObject(name);
            roof.transform.SetParent(parent, false);
            roof.transform.localPosition = localPosition;
            roof.transform.localScale = size;
            roof.AddComponent<MeshFilter>().sharedMesh = GableRoofMesh();
            roof.AddComponent<MeshRenderer>().sharedMaterial = PrimitiveFactory.GetMaterial(color);
            return roof;
        }

        // Unit prism: 1 wide (X), 1 tall (Y), 1 deep (Z), base centered on the origin. Separate vertices per face
        // keep the shading flat; triangles wind clockwise when seen from outside (Unity's front face).
        private static Mesh GableRoofMesh()
        {
            if (_gableRoof != null)
            {
                return _gableRoof;
            }

            var fl = new Vector3(-0.5f, 0f, -0.5f);
            var fr = new Vector3(0.5f, 0f, -0.5f);
            var bl = new Vector3(-0.5f, 0f, 0.5f);
            var br = new Vector3(0.5f, 0f, 0.5f);
            var rl = new Vector3(-0.5f, 1f, 0f);
            var rr = new Vector3(0.5f, 1f, 0f);

            var vertices = new[]
            {
                fl, rl, rr, fr, // front slope
                br, rr, rl, bl, // back slope
                bl, rl, fl,     // left gable
                fr, rr, br,     // right gable
                fl, fr, br, bl  // underside, seen from below
            };

            var triangles = new[]
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10,
                11, 12, 13,
                14, 15, 16, 14, 16, 17
            };

            _gableRoof = new Mesh { name = "GableRoof", vertices = vertices, triangles = triangles };
            _gableRoof.RecalculateNormals();
            _gableRoof.RecalculateBounds();
            return _gableRoof;
        }
    }
}
