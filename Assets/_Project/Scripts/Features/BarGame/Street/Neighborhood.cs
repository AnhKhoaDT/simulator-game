using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Decorative neighbor houses: a row beside the bar and a row across the road.
    // A fixed seed keeps the street the same every time the scene loads.
    internal static class Neighborhood
    {
        private const int Seed = 1977;
        private const float MinWidth = 4f;
        private const float MaxWidth = 7f;
        private const float GapFromBar = 0.4f;
        private const float HouseGap = 0.15f;
        private const float MinLastWidth = 2.5f;
        private const float Depth = 9f;
        private const float FloorHeight = 3f;
        private const int MaxFloors = 3;
        private const float WindowWidth = 0.9f;
        private const float WindowHeight = 1.1f;
        private const float WindowSill = 1f;
        private const float WindowDepth = 0.06f;
        private const float DoorWidth = 1f;
        private const float DoorHeight = 2.1f;
        private const float RoofHeight = 1.6f;
        private const float RoofOverhang = 0.3f;
        private const float ParapetHeight = 0.5f;
        private const float ParapetThickness = 0.15f;
        private const float GableChance = 0.55f;
        private const float AwningChance = 0.5f;
        private const float AirConChance = 0.35f;
        private const float LitWindowChance = 0.6f;
        private const float ShopChance = 0.6f;
        private const float SkylineNearGap = 14f;
        private const float SkylineFarGap = 32f;
        private const float SkylineHalfLength = 55f;
        private const float SkylineMaxGap = 3f;
        private const float SkylineWidthMin = 8f;
        private const float SkylineWidthMax = 13f;
        private const float TowerWidthMin = 10f;
        private const float TowerWidthMax = 14f;
        private const float StreetEndGap = 10f;

        private static readonly Color[] WallColors =
        {
            new Color(0.95f, 0.85f, 0.55f),
            new Color(0.7f, 0.85f, 0.92f),
            new Color(0.93f, 0.72f, 0.72f),
            new Color(0.72f, 0.88f, 0.75f),
            new Color(0.92f, 0.9f, 0.85f),
            new Color(0.85f, 0.75f, 0.9f)
        };

        private static readonly Color[] AwningColors =
        {
            new Color(0.8f, 0.2f, 0.2f),
            new Color(0.2f, 0.45f, 0.8f),
            new Color(0.2f, 0.6f, 0.3f),
            new Color(0.95f, 0.6f, 0.15f)
        };

        private static readonly Color RoofColor = new Color(0.62f, 0.25f, 0.18f);
        private static readonly Color DoorColor = new Color(0.35f, 0.24f, 0.16f);
        private static readonly Color AirConColor = new Color(0.82f, 0.83f, 0.85f);

        public static readonly Color WindowDayColor = new Color(0.2f, 0.3f, 0.38f);
        public static readonly Color WindowNightColor = new Color(1f, 0.85f, 0.45f);

        // Fills 'litWindows' with the windows that light up at night.
        public static void Build(Transform parent, BarLayout layout, List<Renderer> litWindows)
        {
            var random = new System.Random(Seed);
            var root = new GameObject("Neighborhood").transform;
            root.SetParent(parent, false);

            var half = BarLayout.StreetHalfLength;
            var barEdge = layout.HalfX + GapFromBar;

            // Beside the bar: fronts on the bar's front line, facing the street (-Z).
            BuildRow(root, random, -half, -barEdge, layout.FrontZ, false, litWindows);
            BuildRow(root, random, barEdge, half, layout.FrontZ, false, litWindows);
            // Across the road: fronts face back toward the bar (+Z).
            BuildRow(root, random, -half, half, layout.OppositeFrontZ, true, litWindows);
            BuildSkyline(root, random, layout);
        }

        private static void BuildRow(Transform parent, System.Random random, float fromX, float toX,
            float frontZ, bool facesBar, List<Renderer> litWindows)
        {
            var x = fromX;
            while (toX - x >= MinLastWidth)
            {
                var width = Mathf.Min(Range(random, MinWidth, MaxWidth), toX - x);
                // Avoid a sliver house at the end of the row.
                if (toX - x - width < MinLastWidth)
                {
                    width = toX - x;
                }

                var house = new GameObject("House").transform;
                house.SetParent(parent, false);
                house.localPosition = new Vector3(x + width * 0.5f, 0f, frontZ);
                house.localRotation = facesBar ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
                if (!TryBuildModelHouse(house, random, width - HouseGap))
                {
                    BuildHouse(house, random, width - HouseGap, litWindows);
                }

                x += width;
            }
        }

        // A Kenney building fitted to the lot width, its front on the street line. Mostly shophouses, some homes.
        private static bool TryBuildModelHouse(Transform house, System.Random random, float width)
        {
            var isShop = random.NextDouble() < ShopChance;
            var name = isShop ? Pick(random, ModelCatalog.Shops) : Pick(random, ModelCatalog.Houses);
            var folder = isShop ? ModelCatalog.Commercial : ModelCatalog.Suburban;
            var model = ModelLoader.Instantiate(folder + name, house, name);
            if (model == null)
            {
                return false;
            }

            model.transform.localRotation = Quaternion.Euler(0f, ModelCatalog.BuildingYawOffset, 0f);
            ModelLoader.FitToWidth(model, width);
            // Fitting centers the model on the lot; push it back so its front sits on the street line.
            model.transform.localPosition += new Vector3(0f, 0f, ModelLoader.MeasureSize(model).z * 0.5f);
            return true;
        }

        // Far-away buildings behind both rows, plus towers closing off both ends of the street.
        private static void BuildSkyline(Transform parent, System.Random random, BarLayout layout)
        {
            var root = new GameObject("Skyline").transform;
            root.SetParent(parent, false);

            BuildSkylineRow(root, random, layout.HalfZ + SkylineNearGap, false, ModelCatalog.Skyline, SkylineWidthMin, SkylineWidthMax);
            BuildSkylineRow(root, random, layout.OppositeFrontZ - SkylineNearGap, true, ModelCatalog.Skyline, SkylineWidthMin, SkylineWidthMax);
            BuildSkylineRow(root, random, layout.HalfZ + SkylineFarGap, false, ModelCatalog.Skyscrapers, TowerWidthMin, TowerWidthMax);
            BuildSkylineRow(root, random, layout.OppositeFrontZ - SkylineFarGap, true, ModelCatalog.Skyscrapers, TowerWidthMin, TowerWidthMax);

            for (var side = -1; side <= 1; side += 2)
            {
                var x = side * (BarLayout.StreetHalfLength + StreetEndGap);
                PlaceBuilding(root, ModelCatalog.Commercial + Pick(random, ModelCatalog.Skyscrapers),
                    new Vector3(x, 0f, layout.RoadZ), Quaternion.Euler(0f, side * -90f + ModelCatalog.BuildingYawOffset, 0f),
                    TowerWidthMax);
            }
        }

        private static void BuildSkylineRow(Transform parent, System.Random random, float z, bool facesBar,
            string[] names, float minWidth, float maxWidth)
        {
            var rotation = Quaternion.Euler(0f, (facesBar ? 180f : 0f) + ModelCatalog.BuildingYawOffset, 0f);
            for (var x = -SkylineHalfLength; x < SkylineHalfLength;)
            {
                var width = Range(random, minWidth, maxWidth);
                PlaceBuilding(parent, ModelCatalog.Commercial + Pick(random, names),
                    new Vector3(x + width * 0.5f, 0f, z), rotation, width);
                x += width + Range(random, 0f, SkylineMaxGap);
            }
        }

        private static void PlaceBuilding(Transform parent, string path, Vector3 position, Quaternion rotation, float width)
        {
            var model = ModelLoader.Instantiate(path, parent, System.IO.Path.GetFileName(path));
            if (model == null)
            {
                return;
            }

            model.transform.SetPositionAndRotation(position, rotation);
            ModelLoader.FitToWidth(model, width);
        }

        private static string Pick(System.Random random, string[] names)
        {
            return names[random.Next(names.Length)];
        }

        // Built in local space with the front at z = 0 facing -Z and the body extending toward +Z.
        private static void BuildHouse(Transform house, System.Random random, float width, List<Renderer> litWindows)
        {
            var floors = random.Next(1, MaxFloors + 1);
            var height = floors * FloorHeight;
            var wallColor = WallColors[random.Next(WallColors.Length)];

            PrimitiveFactory.Box("Body", house, new Vector3(0f, height * 0.5f, Depth * 0.5f),
                new Vector3(width, height, Depth), wallColor);

            for (var floor = 0; floor < floors; floor++)
            {
                var baseY = floor * FloorHeight;
                for (var side = -1; side <= 1; side += 2)
                {
                    var window = PrimitiveFactory.Box("Window", house,
                        new Vector3(side * width * 0.25f, baseY + WindowSill + WindowHeight * 0.5f, -WindowDepth * 0.5f),
                        new Vector3(WindowWidth, WindowHeight, WindowDepth), WindowDayColor);
                    if (random.NextDouble() < LitWindowChance)
                    {
                        litWindows.Add(window.GetComponent<Renderer>());
                    }
                }

                if (floor > 0 && random.NextDouble() < AirConChance)
                {
                    PrimitiveFactory.Box("AirCon", house, new Vector3(width * 0.25f, baseY + 0.5f, -0.25f),
                        new Vector3(0.8f, 0.5f, 0.5f), AirConColor);
                }
            }

            PrimitiveFactory.Box("Door", house, new Vector3(0f, DoorHeight * 0.5f, -WindowDepth * 0.5f),
                new Vector3(DoorWidth, DoorHeight, WindowDepth), DoorColor);

            if (random.NextDouble() < AwningChance)
            {
                var awning = PrimitiveFactory.Box("Awning", house, new Vector3(0f, 2.6f, -0.5f),
                    new Vector3(width * 0.85f, 0.08f, 1f), AwningColors[random.Next(AwningColors.Length)]);
                awning.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            }

            if (random.NextDouble() < GableChance)
            {
                MeshShapes.GableRoof("Roof", house, new Vector3(0f, height, Depth * 0.5f),
                    new Vector3(width + RoofOverhang, RoofHeight, Depth + RoofOverhang), RoofColor);
            }
            else
            {
                BuildParapet(house, width, height, wallColor);
            }
        }

        // Flat-roof houses get a low wall around the roof edge.
        private static void BuildParapet(Transform house, float width, float height, Color color)
        {
            var y = height + ParapetHeight * 0.5f;
            var t = ParapetThickness;
            PrimitiveFactory.Box("Parapet", house, new Vector3(0f, y, t * 0.5f), new Vector3(width, ParapetHeight, t), color);
            PrimitiveFactory.Box("Parapet", house, new Vector3(0f, y, Depth - t * 0.5f), new Vector3(width, ParapetHeight, t), color);
            PrimitiveFactory.Box("Parapet", house, new Vector3(-width * 0.5f + t * 0.5f, y, Depth * 0.5f),
                new Vector3(t, ParapetHeight, Depth), color);
            PrimitiveFactory.Box("Parapet", house, new Vector3(width * 0.5f - t * 0.5f, y, Depth * 0.5f),
                new Vector3(t, ParapetHeight, Depth), color);
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
