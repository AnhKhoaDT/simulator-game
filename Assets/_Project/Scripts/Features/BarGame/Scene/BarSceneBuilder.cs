using System.Collections.Generic;
using Startup.Core.Localization;
using Startup.Data;
using Startup.Shared;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Builds the whole bar scene on start: street, room, furniture, people, clock, HUD, and wires them together.
    public class BarSceneBuilder : MonoBehaviour
    {
        private const float FloorThickness = 0.2f;
        private const float TableSpacing = 4f;
        private const int TableColumns = 2;
        private const float WindowSill = 0.9f;
        private const float WindowTop = 2.2f;
        private const float FrontWindowStart = 0.5f;
        private const float FrontWindowEnd = 3.5f;
        private const float SideWindowHalfLength = 1.5f;
        private const float CeilingThickness = 0.1f;
        private const float RoofHeight = 1.8f;
        private const float RoofOverhang = 0.8f;
        private const float DecorWallInset = 0.8f;
        private const float NameTagHeight = 1.95f;
        private const float DecorRugLift = 0.005f;
        private const float SunPitch = 50f;
        private const float SunYaw = -30f;

        private static readonly Color RugColor = new Color(0.42f, 0.2f, 0.18f);
        private static readonly Color FloorColor = new Color(0.35f, 0.3f, 0.28f);
        private static readonly Color WallColor = new Color(0.86f, 0.78f, 0.62f);
        private static readonly Color CeilingColor = new Color(0.92f, 0.9f, 0.85f);
        private static readonly Color RoofColor = new Color(0.55f, 0.2f, 0.15f);

        [Header("Room")]
        [SerializeField] private Vector2 _roomSize = new Vector2(12f, 10f);

        [Header("Tables")]
        [SerializeField, Range(1, 8)] private int _tableCount = 4;
        [Tooltip("Optional. Leave empty to use the built-in table.")]
        [SerializeField] private BarTable _tablePrefab;

        [Header("Customers")]
        [SerializeField] private CustomerSettings _customers = new CustomerSettings();
        [SerializeField, Range(0, 20)] private int _pedestrians = 6;
        [SerializeField, Range(0, 20)] private int _cars = 6;

        [Header("Orders")]
        [SerializeField] private OrderSettings _orderSettings = new OrderSettings();

        [Header("Upgrades")]
        [SerializeField] private UpgradeSettings _upgradeSettings = new UpgradeSettings();

        [Header("Shift")]
        [SerializeField] private ShiftSettings _shift = new ShiftSettings();

        [Header("Owner")]
        [SerializeField, Min(1f)] private float _ownerSpeed = 4.5f;

        [Header("Camera")]
        [Tooltip("Optional. Falls back to Camera.main.")]
        [SerializeField] private Camera _camera;

        private readonly List<BarTable> _tables = new List<BarTable>();
        private readonly BarSeating _seating = new BarSeating();

        private BarLayout _layout;
        private Transform _room;
        private BarUpgrades _upgrades;

        public IReadOnlyList<BarTable> Tables => _tables;

        private void Start()
        {
            LocalizationService.Initialize(new LanguagePreferenceStore(new LocalDataStore()));
            _layout = new BarLayout(_roomSize);
            var cam = _camera != null ? _camera : Camera.main;

            var saveStore = new BarSaveStore(new LocalDataStore());
            var economy = new EconomyManager();
            _upgrades = new BarUpgrades(_upgradeSettings);
            var saved = saveStore.LoadInto(economy, _upgrades);

            var orders = new OrderSystem(_orderSettings, economy.TotalRevenue);
            orders.Completed += item => economy.Earn(item.Price);
            economy.Changed += () => orders.RefreshUnlocks(economy.TotalRevenue);

            _room = new GameObject("BarRoom").transform;
            var clock = Child<ShiftClock>("ShiftClock");
            clock.Setup(_shift, saved.ClockMinutes);

            var props = new List<Rect>();
            var lamps = new List<Renderer>();
            var windows = new List<Renderer>();
            StreetBuilder.Build(_room, _layout, props, lamps);
            Neighborhood.Build(_room, _layout, windows);

            BuildFloor();
            BuildWalls();
            BuildRoof();
            var counter = BuildCounter();
            SyncTables();
            _upgrades.Changed += SyncTables;
            BuildDecor();

            var spawner = Child<CustomerSpawner>("CustomerSpawner");
            spawner.Setup(new CustomerContext(_layout, _seating, orders, counter, _customers), clock);
            Child<AmbientPedestrians>("Pedestrians").Setup(_layout, _pedestrians);
            Child<TrafficCars>("Traffic").Setup(_layout, _cars);
            var waiter = Child<WaiterService>("WaiterService");
            waiter.Setup(spawner, _seating, _upgrades, _upgradeSettings.WaiterWalkSpeed, _layout.WaiterStation);

            var profile = new PlayerProfileStore(new LocalDataStore());
            var hud = Child<BarHud>("BarHud");
            hud.Setup(economy, orders, _upgrades, clock, profile);

            BuildOwner(cam, spawner, counter, hud, props, profile);
            hud.NameTags.Add(waiter.Figure, NameTagHeight, LocalizationService.GetText(LocKeys.StaffWaiterDefault));
            hud.ShowTutorialOnce();

            var lighting = Child<DayLighting>("DayLighting");
            lighting.Setup(clock, BuildSun(), cam);
            foreach (var lamp in lamps)
            {
                lighting.AddNightLight(lamp, StreetBuilder.LampDayColor, StreetBuilder.LampNightColor);
            }

            foreach (var window in windows)
            {
                lighting.AddNightLight(window, Neighborhood.WindowDayColor, Neighborhood.WindowNightColor);
            }

            Child<BarAudio>("BarAudio").Setup(orders, _upgrades);
            Child<BarAutoSave>("BarAutoSave").Setup(economy, _upgrades, clock, saveStore);
        }

        // The owner starts on the sidewalk looking at the bar's front; the camera sees through their eyes.
        private void BuildOwner(Camera cam, CustomerSpawner spawner, CashierCounter counter, BarHud hud,
            List<Rect> props, IPlayerProfile profile)
        {
            if (cam == null)
            {
                Debug.LogError("BarSceneBuilder found no camera for the owner's view.");
                return;
            }

            var view = cam.gameObject.AddComponent<PlayerCamera>();
            var owner = Child<PlayerController>("Owner");
            owner.Setup(_layout, _seating.Obstacles, props, spawner, view, hud.Touch, _ownerSpeed);
            view.Setup(cam, owner.transform, owner.BodyRenderers, hud.Touch, hud.IsOverUi, 0f);
            counter.AddAttendant(owner.transform);

            // Your own tag only shows in third person; in first person you are the camera.
            var tag = hud.NameTags.Add(owner.transform, NameTagHeight, OwnerDisplayName(profile.LoadOwnerName()),
                () => !view.IsFirstPerson);
            hud.OwnerNameChanged += name => tag.text = OwnerDisplayName(name);
        }

        private static string OwnerDisplayName(string saved)
        {
            return string.IsNullOrEmpty(saved) ? LocalizationService.GetText(LocKeys.CommonOwnerDefault) : saved;
        }

        private void OnDestroy()
        {
            if (_upgrades != null)
            {
                _upgrades.Changed -= SyncTables;
            }
        }

        private T Child<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(_room, false);
            return go.AddComponent<T>();
        }

        private void BuildFloor()
        {
            var size = new Vector3(_roomSize.x, FloorThickness, _roomSize.y);
            PrimitiveFactory.Box("Floor", _room, new Vector3(0f, -FloorThickness * 0.5f + 0.001f, 0f), size, FloorColor);
        }

        // A normal closed building: four full walls, a door and a big window at the front, a window on the left.
        private void BuildWalls()
        {
            var t = BarLayout.WallThickness;
            var h = BarLayout.WallHeight;
            var halfX = _layout.HalfX;
            var halfZ = _layout.HalfZ;
            var spanX = halfX + t * 0.5f;
            var spanZ = halfZ + t * 0.5f;

            var door = new WallOpening(_layout.DoorX - BarLayout.DoorHalfWidth, _layout.DoorX + BarLayout.DoorHalfWidth,
                0f, BarLayout.DoorHeight);
            var frontWindow = new WallOpening(FrontWindowStart, FrontWindowEnd, WindowSill, WindowTop);
            var sideWindow = new WallOpening(-SideWindowHalfLength, SideWindowHalfLength, WindowSill, WindowTop);

            WallBuilder.Build("Wall_Front", _room, true, _layout.FrontZ, -spanX, spanX, h, t, WallColor, door, frontWindow);
            WallBuilder.Build("Wall_Back", _room, true, halfZ, -spanX, spanX, h, t, WallColor);
            WallBuilder.Build("Wall_Left", _room, false, -halfX, -spanZ, spanZ, h, t, WallColor, sideWindow);
            WallBuilder.Build("Wall_Right", _room, false, halfX, -spanZ, spanZ, h, t, WallColor);
        }

        // A ceiling for the inside view, and a tiled gable roof above it for the outside view.
        private void BuildRoof()
        {
            var top = BarLayout.WallHeight;
            var t = BarLayout.WallThickness;
            PrimitiveFactory.Box("Ceiling", _room, new Vector3(0f, top - CeilingThickness * 0.5f, 0f),
                new Vector3(_roomSize.x + t, CeilingThickness, _roomSize.y + t), CeilingColor);
            MeshShapes.GableRoof("Roof", _room, new Vector3(0f, top, 0f),
                new Vector3(_roomSize.x + RoofOverhang, RoofHeight, _roomSize.y + RoofOverhang), RoofColor);
        }

        private CashierCounter BuildCounter()
        {
            var counter = Child<CashierCounter>("CashierCounter");
            counter.Setup(_layout);
            _seating.AddObstacle(counter.Footprint);
            return counter;
        }

        // Creates tables up to the number owned; called at start and whenever a table is bought.
        private void SyncTables()
        {
            var target = Mathf.Min(_upgrades.TablesOwned, _tableCount);
            while (_tables.Count < target)
            {
                var table = CreateTable(SlotPosition(_tables.Count));
                _tables.Add(table);
                _seating.AddTable(table);
            }
        }

        // Fixed grid of table slots; _tableCount is the number of slots, not the number owned.
        private Vector3 SlotPosition(int index)
        {
            var rows = Mathf.CeilToInt(_tableCount / (float)TableColumns);
            var column = index % TableColumns;
            var row = index / TableColumns;
            var x = (column - (TableColumns - 1) * 0.5f) * TableSpacing;
            var z = (row - (rows - 1) * 0.5f) * TableSpacing;
            return new Vector3(x, 0f, z);
        }

        private BarTable CreateTable(Vector3 position)
        {
            if (_tablePrefab == null)
            {
                return TableBuilder.Build(_room, position);
            }

            var table = Instantiate(_tablePrefab, _room);
            table.transform.localPosition = position;
            return table;
        }

        // Optional decoration from imported models; silently skipped if a model is missing.
        private void BuildDecor()
        {
            var cornerX = _layout.HalfX - DecorWallInset;
            var cornerZ = _layout.HalfZ - DecorWallInset;

            PlaceDecor("Models/Furniture/rugRectangle", new Vector3(0f, DecorRugLift, 0f),
                rugSize: new Vector3(7f, 0.03f, 6f), tint: RugColor);
            PlaceDecor("Models/Furniture/pottedPlant", new Vector3(cornerX, 0f, cornerZ), height: 1f);
            PlaceDecor("Models/Furniture/lampRoundFloor", new Vector3(cornerX, 0f, cornerZ - 1.4f), height: 1.6f);
        }

        private void PlaceDecor(string path, Vector3 position, float height = 0f, Vector3? rugSize = null, Color? tint = null)
        {
            var decor = ModelLoader.Instantiate(path, _room, System.IO.Path.GetFileName(path));
            if (decor == null)
            {
                return;
            }

            decor.transform.position = position;
            if (tint.HasValue)
            {
                ModelLoader.Tint(decor, tint.Value);
            }

            if (rugSize.HasValue)
            {
                ModelLoader.FitToBox(decor, rugSize.Value);
            }
            else
            {
                ModelLoader.FitToHeight(decor, height);
            }
        }

        // DayLighting drives intensity and color from the clock.
        private Light BuildSun()
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(_room, false);
            go.transform.rotation = Quaternion.Euler(SunPitch, SunYaw, 0f);

            var sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.None;
            return sun;
        }
    }
}
