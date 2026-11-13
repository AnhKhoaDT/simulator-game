using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Startup.Features.BarGame
{
    // What the decor prompt near the crosshair should say. Order after None matches the HUD labels.
    public enum DecorPrompt
    {
        None,
        PickTable,
        PickChair,
        PickItem,
        PickSeat,
        PickWaiter,
        Busy,
        CarryTable,
        CarryChair,
        CarryItem,
        Blocked,
        Sitting
    }

    // Rearranging the bar. Look at a table (it brings its chairs), a chair or any other piece and press E to pick it
    // up, walk, E to put it down, R to turn it, Q to cancel, or X to send it to storage. F sits on a chair, bench
    // or sofa; E on a waiter opens their staff card. Pieces from the inventory
    // are carried the same way. While carrying, a floor marker shows the real footprint in green (fits) or red
    // (blocked, with the reason), and orange plates show the areas that must stay free.
    public class DecorController : MonoBehaviour
    {
        private const float Reach = 4.5f;
        private const float TableCarryDistance = 2.4f;
        private const float ChairCarryDistance = 1.2f;
        private const float ItemCarryDistance = 2f;
        private const float CarryLift = 0.12f;
        private const float OwnerClearance = 0.35f;
        // Above the rug (whose top is about 4 cm), so the marker is never hidden.
        private const float MarkerY = 0.06f;
        private const float ZoneY = 0.05f;
        private const float PlateThickness = 0.01f;
        private const int HitBufferSize = 8;

        private static readonly Color AllowedColor = new Color(0.3f, 0.85f, 0.4f);
        private static readonly Color BlockedColor = new Color(0.9f, 0.25f, 0.2f);
        private static readonly Color ZoneColor = new Color(0.95f, 0.6f, 0.15f);

        private enum Carry
        {
            None,
            Table,
            Chair,
            Item
        }

        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];
        private readonly List<GameObject> _zonePlates = new List<GameObject>();

        private BarFurniture _furniture;
        private BarInventory _inventory;
        private Func<bool> _isBlocked;
        private Camera _camera;
        private Transform _owner;
        private PlayerController _player;
        private Transform _marker;
        private Renderer _markerRenderer;
        private BarTable _table;
        private BarChair _chair;
        private PlacedDecor _item;
        private Carry _carry;
        // Set while carrying something that came from the inventory; it is taken out only once it is put down.
        private FurnitureDef _fromStorage;
        private bool? _markerAllowed;
        private DecorPrompt _prompt;
        private PlaceCheck _reason;
        private PlaceCheck _shownReason;

        // Prompt plus, for Blocked, the reason.
        public event Action<DecorPrompt, PlaceCheck> PromptChanged;

        // The owner pressed E on a waiter.
        public event Action<WaiterRecord> StaffPicked;

        public bool IsCarrying => _carry != Carry.None;

        // 'isBlocked' lets another system (the cashier) claim the E key while it has a prompt up.
        public void Setup(BarFurniture furniture, BarInventory inventory, Camera cam, PlayerController owner, Func<bool> isBlocked)
        {
            _furniture = furniture;
            _inventory = inventory;
            _camera = cam;
            _player = owner;
            _owner = owner.transform;
            _isBlocked = isBlocked;
            _marker = Plate("PlacementMarker", AllowedColor).transform;
            _markerRenderer = _marker.GetComponent<Renderer>();
            _marker.gameObject.SetActive(false);
        }

        // Starts carrying one item out of the inventory; false if the owner is already carrying something.
        public bool BeginPlaceFromStorage(FurnitureDef def)
        {
            if (IsCarrying || _inventory.Count(def.Id) <= 0)
            {
                return false;
            }

            var at = CarryPoint(CarryDistanceFor(def.Kind));
            if (!_furniture.BeginPlaceNew(def, at))
            {
                return false;
            }

            _fromStorage = def;
            switch (def.Kind)
            {
                case FurnitureKind.Table:
                    _table = _furniture.CarriedTable;
                    _carry = Carry.Table;
                    break;
                case FurnitureKind.Chair:
                    _chair = _furniture.CarriedChair;
                    _carry = Carry.Chair;
                    break;
                default:
                    _item = _furniture.CarriedDecor;
                    _carry = Carry.Item;
                    break;
            }

            StartCarrying();
            return true;
        }

        private void Update()
        {
            if (_camera == null || Time.timeScale == 0f)
            {
                return;
            }

            var keyboard = Keyboard.current;
            switch (_carry)
            {
                case Carry.None:
                    UpdateLooking(keyboard);
                    break;
                case Carry.Chair:
                    UpdateCarryingChair(keyboard);
                    break;
                case Carry.Item:
                    UpdateCarryingItem(keyboard);
                    break;
                default:
                    UpdateCarryingTable(keyboard);
                    break;
            }
        }

        // Never leave furniture hanging in the air if the scene ends mid-carry.
        private void OnDestroy()
        {
            if (IsCarrying)
            {
                _furniture.CancelMove();
            }
        }

        private void UpdateLooking(Keyboard keyboard)
        {
            if (_isBlocked != null && _isBlocked())
            {
                SetPrompt(DecorPrompt.None);
                return;
            }

            if (_player.IsSitting)
            {
                SetPrompt(DecorPrompt.Sitting);
                return;
            }

            LookTarget(out var table, out var chair, out var item, out var waiter);
            if (waiter != null)
            {
                SetPrompt(DecorPrompt.PickWaiter);
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    StaffPicked?.Invoke(waiter.Record);
                }

                return;
            }

            if (table == null && chair == null && item == null)
            {
                SetPrompt(DecorPrompt.None);
                return;
            }

            var busy = chair != null ? chair.IsTaken : table != null ? table.HasOccupants : item.HasSitter;
            if (busy)
            {
                SetPrompt(DecorPrompt.Busy);
                return;
            }

            SetPrompt(chair != null ? DecorPrompt.PickChair
                : table != null ? DecorPrompt.PickTable
                : item.Seats.Count > 0 ? DecorPrompt.PickSeat : DecorPrompt.PickItem);
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.fKey.wasPressedThisFrame && (chair != null || item != null))
            {
                ISeat seat = chair != null ? chair : FirstFreeSeat(item);
                if (seat != null)
                {
                    _player.TrySit(seat);
                }
            }
            else if (keyboard.xKey.wasPressedThisFrame)
            {
                StoreTarget(table, chair, item);
            }
            else if (keyboard.eKey.wasPressedThisFrame)
            {
                PickUp(table, chair, item);
            }
        }

        private static ISeat FirstFreeSeat(PlacedDecor item)
        {
            foreach (var seat in item.Seats)
            {
                if (seat.IsFree)
                {
                    return seat;
                }
            }

            return null;
        }

        private void PickUp(BarTable table, BarChair chair, PlacedDecor item)
        {
            if (chair != null && _furniture.BeginMoveChair(chair))
            {
                _chair = chair;
                _carry = Carry.Chair;
            }
            else if (table != null && _furniture.BeginMove(table))
            {
                _table = table;
                _carry = Carry.Table;
            }
            else if (item != null && _furniture.BeginMoveDecor(item))
            {
                _item = item;
                _carry = Carry.Item;
            }
            else
            {
                return;
            }

            StartCarrying();
        }

        private void StoreTarget(BarTable table, BarChair chair, PlacedDecor item)
        {
            string id;
            bool stored;
            if (chair != null)
            {
                id = FurnitureCatalog.ChairId;
                stored = _furniture.StoreChair(chair);
            }
            else if (table != null)
            {
                id = FurnitureCatalog.TableId;
                stored = _furniture.StoreTable(table);
            }
            else
            {
                id = item.Def.Id;
                stored = _furniture.StoreDecor(item);
            }

            if (stored)
            {
                _inventory.Add(id);
                SetPrompt(DecorPrompt.None);
            }
        }

        private void StartCarrying()
        {
            _markerAllowed = null;
            ShowZones(true);
            _marker.gameObject.SetActive(true);
        }

        private void UpdateCarryingTable(Keyboard keyboard)
        {
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                _table.transform.Rotate(0f, BarFurniture.RotateStep, 0f, Space.World);
            }

            var center = CarryPoint(TableCarryDistance);
            var check = _furniture.CheckTable(_table, center, _owner.position, OwnerClearance);
            _table.transform.position = center + Vector3.up * CarryLift;
            ShowMarker(_table.FootprintAt(center), check, DecorPrompt.CarryTable);

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                Cancel();
            }
            else if (keyboard.eKey.wasPressedThisFrame && check == PlaceCheck.Ok)
            {
                // Lower it first: the move is judged where the table will actually stand.
                _table.transform.position = center;
                if (_furniture.TryEndMove(_owner.position, OwnerClearance))
                {
                    Finish();
                }
            }
        }

        private void UpdateCarryingChair(Keyboard keyboard)
        {
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                _furniture.RotateCarriedChair();
            }

            var floor = CarryPoint(ChairCarryDistance);
            var check = _furniture.CheckChair(_chair, floor, _owner.position, OwnerClearance);
            _chair.transform.position = floor + Vector3.up * (TableBuilder.SeatHeight + CarryLift);

            var c = TableBuilder.ChairHalfSize;
            ShowMarker(Rect.MinMaxRect(floor.x - c, floor.z - c, floor.x + c, floor.z + c), check, DecorPrompt.CarryChair);

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                Cancel();
            }
            else if (keyboard.eKey.wasPressedThisFrame && check == PlaceCheck.Ok
                     && _furniture.TryEndMoveChair(floor, _owner.position, OwnerClearance))
            {
                Finish();
            }
        }

        private void UpdateCarryingItem(Keyboard keyboard)
        {
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                _furniture.RotateCarriedDecor();
            }

            var floor = CarryPoint(ItemCarryDistance);
            var check = _furniture.CheckDecor(_item, floor, _owner.position, OwnerClearance);
            _item.transform.position = floor + Vector3.up * CarryLift;
            ShowMarker(_item.FootprintAt(floor), check, DecorPrompt.CarryItem);

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                Cancel();
            }
            else if (keyboard.eKey.wasPressedThisFrame && check == PlaceCheck.Ok
                     && _furniture.TryEndMoveDecor(floor, _owner.position, OwnerClearance))
            {
                Finish();
            }
        }

        // A grid point on the floor a little in front of the owner, in the direction the camera looks.
        private Vector3 CarryPoint(float distance)
        {
            var forward = _camera.transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : _owner.forward;

            var point = _owner.position + forward * distance;
            return new Vector3(Snap(point.x), 0f, Snap(point.z));
        }

        private static float CarryDistanceFor(FurnitureKind kind)
        {
            switch (kind)
            {
                case FurnitureKind.Table: return TableCarryDistance;
                case FurnitureKind.Chair: return ChairCarryDistance;
                default: return ItemCarryDistance;
            }
        }

        private void ShowMarker(Rect area, PlaceCheck check, DecorPrompt carryPrompt)
        {
            var allowed = check == PlaceCheck.Ok;
            _marker.position = new Vector3(area.center.x, MarkerY, area.center.y);
            _marker.localScale = new Vector3(area.width, PlateThickness, area.height);

            // Repaint on the first frame of every carry and whenever the verdict flips.
            if (_markerAllowed != allowed)
            {
                _markerAllowed = allowed;
                _markerRenderer.sharedMaterial = PrimitiveFactory.GetMaterial(allowed ? AllowedColor : BlockedColor);
            }

            _reason = check;
            SetPrompt(allowed ? carryPrompt : DecorPrompt.Blocked);
        }

        private void Cancel()
        {
            _furniture.CancelMove();
            Finish(false);
        }

        // 'placed' is false after a cancel. A piece from storage leaves the inventory only when it was put down.
        private void Finish(bool placed = true)
        {
            if (placed && _fromStorage != null)
            {
                _inventory.TryTake(_fromStorage.Id);
            }

            _fromStorage = null;
            _carry = Carry.None;
            _table = null;
            _chair = null;
            _item = null;
            _marker.gameObject.SetActive(false);
            ShowZones(false);
            SetPrompt(DecorPrompt.None);
        }

        // The no-go areas (doorway, pay queue, staff spot, decoration) appear only while carrying.
        private void ShowZones(bool show)
        {
            if (show && _zonePlates.Count == 0)
            {
                foreach (var area in _furniture.KeepClearAreas)
                {
                    var plate = Plate("KeepClear", ZoneColor);
                    plate.transform.position = new Vector3(area.center.x, ZoneY, area.center.y);
                    plate.transform.localScale = new Vector3(area.width, PlateThickness, area.height);
                    _zonePlates.Add(plate);
                }
            }

            foreach (var plate in _zonePlates)
            {
                plate.SetActive(show);
            }
        }

        // The nearest table top, chair or decoration under the crosshair, within reach.
        private void LookTarget(out BarTable table, out BarChair chair, out PlacedDecor item, out StaffHandle waiter)
        {
            table = null;
            chair = null;
            item = null;
            waiter = null;
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            var count = Physics.RaycastNonAlloc(ray, _hits, Reach, 1 << BarFurniture.FurnitureLayer,
                QueryTriggerInteraction.Collide);

            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.distance >= bestDistance)
                {
                    continue;
                }

                // Chairs are checked first: a chair is never inside a table object, but a table may hold carried chairs.
                var hitWaiter = hit.collider.GetComponent<StaffHandle>();
                var hitChair = hit.collider.GetComponentInParent<BarChair>();
                var hitItem = hitChair == null ? hit.collider.GetComponentInParent<PlacedDecor>() : null;
                var hitTable = hitChair == null && hitItem == null ? hit.collider.GetComponentInParent<BarTable>() : null;
                if (hitWaiter == null && hitChair == null && hitItem == null && hitTable == null)
                {
                    continue;
                }

                bestDistance = hit.distance;
                waiter = hitWaiter;
                chair = hitChair;
                item = hitItem;
                table = hitTable;
            }
        }

        private void SetPrompt(DecorPrompt prompt)
        {
            if (prompt == _prompt && (prompt != DecorPrompt.Blocked || _reason == _shownReason))
            {
                return;
            }

            _prompt = prompt;
            _shownReason = _reason;
            PromptChanged?.Invoke(prompt, _reason);
        }

        private GameObject Plate(string name, Color color)
        {
            var plate = PrimitiveFactory.Box(name, transform, Vector3.zero, Vector3.one, color);
            Destroy(plate.GetComponent<Collider>());
            return plate;
        }

        private static float Snap(float value)
        {
            return Mathf.Round(value / BarFurniture.GridStep) * BarFurniture.GridStep;
        }
    }
}
