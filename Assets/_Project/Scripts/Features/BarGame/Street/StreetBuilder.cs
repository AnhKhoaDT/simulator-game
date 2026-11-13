using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Ground, both sidewalks, the road with lane marks, trees, street lamps and the shop sign over the door.
    internal static class StreetBuilder
    {
        private const float GroundMargin = 60f;
        private const float GroundDepth = 160f;
        private const float GroundTop = -0.1f;
        private const float SidewalkThickness = 0.1f;
        private const float RoadDrop = 0.04f;
        private const float CurbWidth = 0.15f;
        private const float DashLength = 1.2f;
        private const float DashGap = 1f;
        private const float DashWidth = 0.15f;

        private const float PropSpacing = 9f;
        private const float PropCurbInset = 0.5f;
        private const float DoorClearance = 3f;
        private const float TrunkHeight = 1.6f;
        private const float TrunkRadius = 0.12f;
        private const float CrownSize = 1.8f;
        private const float LampHeight = 4f;
        private const float LampPoleWidth = 0.1f;
        private const float BulbSize = 0.35f;
        private const float PropHalfFootprint = 0.25f;
        private const float PropMaxFootprint = 1.6f;
        private const float TreeHeight = 4.5f;
        private const float PoleHeight = 7f;
        private const float DumpsterHeight = 1.3f;
        private const float DumpsterOffset = 1.4f;
        private const float DumpsterInset = 0.5f;

        private const float SignWidth = 2.6f;
        private const float SignHeight = 0.8f;
        private const float SignBottomGap = 0.15f;

        private static readonly Color GroundColor = new Color(0.2f, 0.24f, 0.18f);
        private static readonly Color SidewalkColor = new Color(0.62f, 0.6f, 0.56f);
        private static readonly Color CurbColor = new Color(0.78f, 0.77f, 0.74f);
        private static readonly Color RoadColor = new Color(0.2f, 0.21f, 0.23f);
        private static readonly Color DashColor = new Color(0.9f, 0.8f, 0.3f);
        private static readonly Color TrunkColor = new Color(0.4f, 0.27f, 0.16f);
        private static readonly Color CrownColor = new Color(0.25f, 0.55f, 0.25f);
        private static readonly Color PoleColor = new Color(0.25f, 0.26f, 0.28f);
        private static readonly Color BulbDayColor = new Color(0.85f, 0.85f, 0.8f);
        private static readonly Color BulbNightColor = new Color(1f, 0.9f, 0.55f);
        private static readonly Color BoardColor = new Color(0.6f, 0.12f, 0.1f);
        private static readonly Color BoardFrameColor = new Color(1f, 0.72f, 0.28f);
        private static readonly Color StarColor = new Color(1f, 0.9f, 0.4f);

        public static readonly Color LampDayColor = BulbDayColor;
        public static readonly Color LampNightColor = BulbNightColor;

        // Adds tree and lamp footprints to 'obstacles' (so the owner walks around them) and lamp bulbs to 'bulbs'.
        public static void Build(Transform parent, BarLayout layout, List<Rect> obstacles, List<Renderer> bulbs)
        {
            var half = BarLayout.StreetHalfLength;
            var length = half * 2f;

            PrimitiveFactory.Box("Ground", parent, new Vector3(0f, GroundTop - 0.05f, layout.RoadZ),
                new Vector3(length + GroundMargin * 2f, 0.1f, GroundDepth), GroundColor);

            BuildSidewalk(parent, layout.FrontZ - BarLayout.SidewalkWidth * 0.5f, length);
            BuildSidewalk(parent, layout.OppositeSidewalkZ, length);

            PrimitiveFactory.Box("Road", parent,
                new Vector3(0f, -SidewalkThickness * 0.5f - RoadDrop, layout.RoadZ),
                new Vector3(length, SidewalkThickness, BarLayout.RoadWidth), RoadColor);
            BuildLaneMarks(parent, layout.RoadZ, half);

            // Trees and lamps along the curb of each sidewalk, alternating, skipping the bar's doorway.
            var nearCurbZ = layout.FrontZ - BarLayout.SidewalkWidth + PropCurbInset;
            var farCurbZ = layout.OppositeFrontZ + BarLayout.SidewalkWidth - PropCurbInset;
            BuildProps(parent, layout, nearCurbZ, true, obstacles, bulbs);
            BuildProps(parent, layout, farCurbZ, false, obstacles, bulbs);
            BuildPoles(parent, layout, nearCurbZ, obstacles);
            BuildPoles(parent, layout, farCurbZ, obstacles);

            // A dumpster on the sidewalk just right of the bar.
            PlaceProp(parent, ModelCatalog.Roads + "dumpster",
                new Vector3(layout.HalfX + DumpsterOffset, 0f, layout.FrontZ - DumpsterInset), DumpsterHeight, 0f, obstacles);

            BuildSign(parent, layout);
        }

        // Electricity poles halfway between trees and lamps, like a typical Vietnamese street.
        private static void BuildPoles(Transform parent, BarLayout layout, float z, List<Rect> obstacles)
        {
            var half = BarLayout.StreetHalfLength;
            for (var x = -half + PropSpacing; x < half; x += PropSpacing * 2f)
            {
                if (Mathf.Abs(x - layout.DoorX) < DoorClearance)
                {
                    continue;
                }

                PlaceProp(parent, ModelCatalog.Roads + "electricity-pole", new Vector3(x, 0f, z), PoleHeight, 90f, obstacles);
            }
        }

        // A decorative model on the street; also registers its footprint so the owner walks around it.
        private static void PlaceProp(Transform parent, string path, Vector3 position, float height, float yaw,
            List<Rect> obstacles)
        {
            var model = ModelLoader.Instantiate(path, parent, System.IO.Path.GetFileName(path));
            if (model == null)
            {
                return;
            }

            model.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            ModelLoader.FitToHeight(model, height);
            var size = ModelLoader.MeasureSize(model);
            var half = new Vector2(Mathf.Min(size.x, PropMaxFootprint), Mathf.Min(size.z, PropMaxFootprint)) * 0.5f;
            obstacles.Add(new Rect(position.x - half.x, position.z - half.y, half.x * 2f, half.y * 2f));
        }

        private static void BuildSidewalk(Transform parent, float centerZ, float length)
        {
            PrimitiveFactory.Box("Sidewalk", parent, new Vector3(0f, -SidewalkThickness * 0.5f, centerZ),
                new Vector3(length, SidewalkThickness, BarLayout.SidewalkWidth), SidewalkColor);

            for (var side = -1; side <= 1; side += 2)
            {
                var curbZ = centerZ + side * (BarLayout.SidewalkWidth * 0.5f - CurbWidth * 0.5f);
                PrimitiveFactory.Box("Curb", parent, new Vector3(0f, -SidewalkThickness * 0.5f + 0.005f, curbZ),
                    new Vector3(length, SidewalkThickness, CurbWidth), CurbColor);
            }
        }

        private static void BuildLaneMarks(Transform parent, float roadZ, float half)
        {
            var y = -RoadDrop + 0.002f;
            for (var x = -half; x < half; x += DashLength + DashGap)
            {
                PrimitiveFactory.Box("LaneMark", parent, new Vector3(x + DashLength * 0.5f, y, roadZ),
                    new Vector3(DashLength, 0.01f, DashWidth), DashColor);
            }
        }

        private static void BuildProps(Transform parent, BarLayout layout, float z, bool nearSide,
            List<Rect> obstacles, List<Renderer> bulbs)
        {
            var half = BarLayout.StreetHalfLength;
            var index = 0;
            for (var x = -half + PropSpacing * 0.5f; x < half; x += PropSpacing, index++)
            {
                if (nearSide && Mathf.Abs(x - layout.DoorX) < DoorClearance)
                {
                    continue;
                }

                var position = new Vector3(x, 0f, z);
                if (index % 2 == 0)
                {
                    BuildTree(parent, position, index / 2 + (nearSide ? 0 : 3));
                }
                else
                {
                    bulbs.Add(BuildLamp(parent, position));
                }

                obstacles.Add(new Rect(x - PropHalfFootprint, z - PropHalfFootprint,
                    PropHalfFootprint * 2f, PropHalfFootprint * 2f));
            }
        }

        private static void BuildTree(Transform parent, Vector3 position, int variant)
        {
            var trees = ModelCatalog.Trees;
            var model = ModelLoader.Instantiate(trees[variant % trees.Length], parent, "Tree");
            if (model != null)
            {
                model.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, variant * 47f, 0f));
                ModelLoader.FitToHeight(model, TreeHeight);
                return;
            }

            PrimitiveFactory.Shape(PrimitiveType.Cylinder, "Trunk", parent,
                position + Vector3.up * (TrunkHeight * 0.5f),
                new Vector3(TrunkRadius * 2f, TrunkHeight * 0.5f, TrunkRadius * 2f), TrunkColor);
            PrimitiveFactory.Shape(PrimitiveType.Sphere, "Crown", parent,
                position + Vector3.up * (TrunkHeight + CrownSize * 0.35f), Vector3.one * CrownSize, CrownColor);
        }

        private static Renderer BuildLamp(Transform parent, Vector3 position)
        {
            PrimitiveFactory.Box("LampPole", parent, position + Vector3.up * (LampHeight * 0.5f),
                new Vector3(LampPoleWidth, LampHeight, LampPoleWidth), PoleColor);
            var bulb = PrimitiveFactory.Shape(PrimitiveType.Sphere, "LampBulb", parent,
                position + Vector3.up * LampHeight, Vector3.one * BulbSize, BulbDayColor);
            Object.Destroy(bulb.GetComponent<Collider>());
            return bulb.GetComponent<Renderer>();
        }

        // A pictogram sign (beer mug) on the facade above the door, so it reads in every language.
        private static void BuildSign(Transform parent, BarLayout layout)
        {
            var root = new GameObject("ShopSign").transform;
            root.SetParent(parent, false);
            var faceZ = layout.FrontZ - BarLayout.WallThickness * 0.5f;
            root.localPosition = new Vector3(layout.DoorX, BarLayout.DoorHeight + SignBottomGap + SignHeight * 0.5f, faceZ);

            PrimitiveFactory.Box("BoardFrame", root, new Vector3(0f, 0f, -0.05f),
                new Vector3(SignWidth + 0.15f, SignHeight + 0.15f, 0.1f), BoardFrameColor);
            PrimitiveFactory.Box("Board", root, new Vector3(0f, 0f, -0.08f),
                new Vector3(SignWidth, SignHeight, 0.1f), BoardColor);

            var mug = ItemVisuals.BuildMug(root, 0.55f);
            mug.transform.localPosition = new Vector3(0f, -0.2f, -0.25f);

            for (var side = -1; side <= 1; side += 2)
            {
                var star = PrimitiveFactory.Shape(PrimitiveType.Sphere, "Light", root,
                    new Vector3(side * 0.85f, 0f, -0.15f), Vector3.one * 0.14f, StarColor);
                Object.Destroy(star.GetComponent<Collider>());
            }
        }
    }
}
