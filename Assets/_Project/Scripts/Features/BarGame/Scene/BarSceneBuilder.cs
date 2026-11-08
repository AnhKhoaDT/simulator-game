using System.Collections.Generic;
using Startup.Core.Localization;
using Startup.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace Startup.Features.BarGame
{
    // Builds the bar from primitives on start: floor, walls, tables, tycoon camera and light.
    public class BarSceneBuilder : MonoBehaviour
    {
        private const float FloorThickness = 0.2f;
        private const float WallThickness = 0.2f;
        private const float TableSpacing = 4f;
        private const int TableColumns = 2;
        private const float CameraPitch = 50f;
        private const float CameraYaw = 45f;
        private const float CameraDistance = 16f;
        private const float CameraFieldOfView = 40f;
        private const float PanLimitRatio = 0.4f;
        private const float DoorOffsetRatio = 0.375f;
        private const float SpawnOutsideDistance = 2f;
        private const float WaitInsideDistance = 1f;
        private const float WaiterStationInset = 1.2f;
        private const float DecorWallInset = 0.8f;
        private const float DecorRugLift = 0.005f;

        private static readonly Color RugColor = new Color(0.42f, 0.2f, 0.18f);
        private static readonly Color FloorColor = new Color(0.35f, 0.3f, 0.28f);
        private static readonly Color WallColor = new Color(0.6f, 0.55f, 0.5f);
        private static readonly Color BackgroundColor = new Color(0.06f, 0.08f, 0.1f);
        private static readonly Color AmbientColor = new Color(0.5f, 0.5f, 0.55f);
        private static readonly Color SunColor = new Color(1f, 0.96f, 0.88f);

        [Header("Room")]
        [SerializeField] private Vector2 _roomSize = new Vector2(12f, 10f);
        [SerializeField, Min(1f)] private float _wallHeight = 3f;

        [Header("Tables")]
        [SerializeField, Range(1, 8)] private int _tableCount = 4;
        [Tooltip("Optional. Leave empty to use the built-in primitive table.")]
        [SerializeField] private BarTable _tablePrefab;

        [Header("Customers")]
        [SerializeField] private CustomerSettings _customers = new CustomerSettings();

        [Header("Orders")]
        [SerializeField] private OrderSettings _orderSettings = new OrderSettings();

        [Header("Upgrades")]
        [SerializeField] private UpgradeSettings _upgradeSettings = new UpgradeSettings();

        [Header("Camera")]
        [Tooltip("Optional. Falls back to Camera.main.")]
        [SerializeField] private Camera _camera;

        private readonly List<BarTable> _tables = new List<BarTable>();
        private readonly BarSeating _seating = new BarSeating();

        private Transform _room;
        private BarUpgrades _upgrades;

        public IReadOnlyList<BarTable> Tables => _tables;

        private void Start()
        {
            LocalizationService.Initialize(new LanguagePreferenceStore(new LocalDataStore()));

            var saveStore = new BarSaveStore(new LocalDataStore());
            var economy = new EconomyManager();
            _upgrades = new BarUpgrades(_upgradeSettings);
            saveStore.LoadInto(economy, _upgrades);

            var orders = new OrderSystem(_orderSettings, economy.TotalRevenue);
            orders.Completed += item => economy.Earn(item.Price);
            economy.Changed += () => orders.RefreshUnlocks(economy.TotalRevenue);

            _room = new GameObject("BarRoom").transform;
            BuildFloor(_room);
            BuildWalls(_room);
            SyncTables();
            _upgrades.Changed += SyncTables;

            BuildDecor(_room);
            var spawner = BuildCustomers(_room, orders);
            BuildWaiter(_room, spawner);
            BuildLight(_room);
            ConfigureCamera();
            BuildInteraction(_room);
            BuildHud(_room, economy, orders, _upgrades);
            BuildAudio(_room, orders);
            BuildAutoSave(_room, economy, _upgrades, saveStore);
        }

        private void OnDestroy()
        {
            if (_upgrades != null)
            {
                _upgrades.Changed -= SyncTables;
            }
        }

        // Optional decoration from imported models; silently skipped if a model is missing.
        private void BuildDecor(Transform parent)
        {
            var cornerX = _roomSize.x * 0.5f - DecorWallInset;
            var cornerZ = _roomSize.y * 0.5f - DecorWallInset;

            PlaceDecor(parent, "Models/Furniture/rugRectangle", new Vector3(0f, DecorRugLift, 0f),
                rugSize: new Vector3(7f, 0.03f, 6f), tint: RugColor);
            PlaceDecor(parent, "Models/Furniture/pottedPlant", new Vector3(cornerX, 0f, cornerZ), height: 1f);
            PlaceDecor(parent, "Models/Furniture/lampRoundFloor", new Vector3(cornerX, 0f, cornerZ - 1.4f), height: 1.6f);
        }

        private static void PlaceDecor(Transform parent, string path, Vector3 position,
            float height = 0f, Vector3? rugSize = null, Color? tint = null)
        {
            var decor = ModelLoader.Instantiate(path, parent, System.IO.Path.GetFileName(path));
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

        private void BuildAudio(Transform parent, OrderSystem orders)
        {
            var audio = new GameObject("BarAudio").AddComponent<BarAudio>();
            audio.transform.SetParent(parent, false);
            audio.Setup(orders, _upgrades);
        }

        private static void BuildAutoSave(Transform parent, EconomyManager economy, BarUpgrades upgrades, BarSaveStore saveStore)
        {
            var autoSave = new GameObject("BarAutoSave").AddComponent<BarAutoSave>();
            autoSave.transform.SetParent(parent, false);
            autoSave.Setup(economy, upgrades, saveStore);
        }

        private void BuildWaiter(Transform parent, CustomerSpawner spawner)
        {
            var waiter = new GameObject("WaiterService").AddComponent<WaiterService>();
            waiter.transform.SetParent(parent, false);
            var station = new Vector3(_roomSize.x * 0.5f - WaiterStationInset, 0f, 0f);
            waiter.Setup(spawner, _seating, _upgrades, _upgradeSettings.WaiterWalkSpeed, station);
        }

        private void BuildFloor(Transform parent)
        {
            var size = new Vector3(_roomSize.x, FloorThickness, _roomSize.y);
            PrimitiveFactory.Box("Floor", parent, new Vector3(0f, -FloorThickness * 0.5f, 0f), size, FloorColor);
        }

        // Walls only on the far sides so the camera sees into the room.
        private void BuildWalls(Transform parent)
        {
            var y = _wallHeight * 0.5f;

            var backSize = new Vector3(_roomSize.x, _wallHeight, WallThickness);
            PrimitiveFactory.Box("Wall_Back", parent, new Vector3(0f, y, _roomSize.y * 0.5f), backSize, WallColor);

            var sideSize = new Vector3(WallThickness, _wallHeight, _roomSize.y);
            PrimitiveFactory.Box("Wall_Side", parent, new Vector3(_roomSize.x * 0.5f, y, 0f), sideSize, WallColor);
        }

        // Creates tables up to the number owned; called at start and whenever a table is bought.
        private void SyncTables()
        {
            var target = Mathf.Min(_upgrades.TablesOwned, _tableCount);
            while (_tables.Count < target)
            {
                var table = CreateTable(_room, SlotPosition(_tables.Count));
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

        private BarTable CreateTable(Transform parent, Vector3 position)
        {
            if (_tablePrefab == null)
            {
                return TableBuilder.Build(parent, position);
            }

            var table = Instantiate(_tablePrefab, parent);
            table.transform.localPosition = position;
            return table;
        }

        // The door is an open gap on the camera-facing edge; customers spawn outside and walk in.
        private CustomerSpawner BuildCustomers(Transform parent, OrderSystem orders)
        {
            var doorX = -_roomSize.x * DoorOffsetRatio;
            var frontZ = -_roomSize.y * 0.5f;
            var spawnPoint = new Vector3(doorX, 0f, frontZ - SpawnOutsideDistance);
            var waitPoint = new Vector3(doorX, 0f, frontZ + WaitInsideDistance);

            var spawner = new GameObject("CustomerSpawner").AddComponent<CustomerSpawner>();
            spawner.transform.SetParent(parent, false);
            // Bubbles face the fixed camera direction.
            var bubbleRotation = Quaternion.Euler(CameraPitch, CameraYaw, 0f);
            spawner.Setup(_seating, orders, _customers, bubbleRotation, spawnPoint, waitPoint);
            return spawner;
        }

        private void BuildInteraction(Transform parent)
        {
            var cam = _camera != null ? _camera : Camera.main;
            var picker = new GameObject("CustomerPicker").AddComponent<CustomerPicker>();
            picker.transform.SetParent(parent, false);
            picker.Setup(cam);
        }

        private static void BuildHud(Transform parent, EconomyManager economy, OrderSystem orders, BarUpgrades upgrades)
        {
            var hud = new GameObject("BarHud").AddComponent<BarHud>();
            hud.transform.SetParent(parent, false);
            hud.Setup(economy, orders, upgrades);
        }

        private static void BuildLight(Transform parent)
        {
            var go = new GameObject("Directional Light");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = SunColor;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.None;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;
        }

        private void ConfigureCamera()
        {
            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null)
            {
                Debug.LogError("BarSceneBuilder found no camera to configure.");
                return;
            }

            var target = new Vector3(0f, 0.5f, 0f);
            var rotation = Quaternion.Euler(CameraPitch, CameraYaw, 0f);
            cam.transform.SetPositionAndRotation(target - rotation * Vector3.forward * CameraDistance, rotation);
            cam.orthographic = false;
            cam.fieldOfView = CameraFieldOfView;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;

            var pan = cam.GetComponent<BarCameraPan>();
            if (pan == null)
            {
                pan = cam.gameObject.AddComponent<BarCameraPan>();
            }

            pan.Setup(cam, _roomSize * PanLimitRatio);
        }
    }
}
