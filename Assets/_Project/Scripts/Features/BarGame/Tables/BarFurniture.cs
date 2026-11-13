using System;
using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Why a table or chair cannot go where the player is pointing. Ok means it fits.
    public enum PlaceCheck
    {
        Ok,
        Wall,
        Table,
        Chair,
        Door,
        Queue,
        Staff,
        Decor,
        Counter,
        Owner,
        UnderTable,
        Item
    }

    // Owns every table, chair and decoration in the bar. Chairs are independent: each one belongs to the nearest table within
    // BarTable.SeatRange (and then faces it), otherwise it stands alone as decoration. Picking up a table brings
    // its chairs along. Saved as (x, yaw degrees, z) per table and per chair.
    public sealed class BarFurniture
    {
        public const float GridStep = 0.25f;
        public const float RotateStep = 90f;
        public const int ChairsPerNewTable = 4;

        // Pick colliders for furniture use TransparentFX, which nothing else in the scene uses.
        public const int FurnitureLayer = 1;

        private const float WallMargin = 0.05f;
        private const float QueueDepth = 4f;
        private const float QueueHalfWidth = 0.25f;
        private const float DoorZoneDepth = 1.3f;
        private const float StationHalfSize = 0.5f;
        private const float OverlapShrink = 0.01f;
        private const float SearchStep = 0.5f;
        private const float PickHeight = 0.8f;
        private const float LooseChairMargin = 0.15f;
        private const float NewTableMargin = 0.3f;

        private readonly List<BarTable> _tables = new List<BarTable>();
        private readonly List<BarChair> _chairs = new List<BarChair>();
        private readonly List<PlacedDecor> _decor = new List<PlacedDecor>();
        private readonly List<DecorSave> _savedDecor = new List<DecorSave>();
        private readonly List<Zone> _keepClear = new List<Zone>();
        private readonly List<Vector3> _savedTables = new List<Vector3>();
        private readonly List<Vector3> _savedChairs = new List<Vector3>();
        private readonly List<BarChair> _group = new List<BarChair>();
        private readonly List<Rect> _looseAreas = new List<Rect>();
        private readonly BarSeating _seating;
        private readonly BarLayout _layout;
        private readonly Transform _root;
        private readonly Func<Vector3, BarTable> _createTable;
        private readonly Func<int, Vector3> _defaultSlot;

        private bool _layoutSaved;
        private bool _decorSaved;
        // True while the carried piece came out of storage, so cancelling throws it away instead of putting it back.
        private bool _movingIsNew;
        private PlacedDecor _movingDecor;
        private Vector3 _decorFrom;
        private Quaternion _decorFromRotation;
        private BarTable _movingTable;
        private Vector3 _tableFrom;
        private Quaternion _tableFromRotation;
        private BarChair _movingChair;
        private Vector3 _chairFrom;
        private Quaternion _chairFromRotation;

        public BarFurniture(BarLayout layout, BarSeating seating, Transform root, Rect counter,
            Func<Vector3, BarTable> createTable, Func<int, Vector3> defaultSlot)
        {
            _layout = layout;
            _seating = seating;
            _root = root;
            _createTable = createTable;
            _defaultSlot = defaultSlot;
            BuildKeepClearZones(counter);
        }

        public IReadOnlyList<BarTable> Tables => _tables;

        // Areas that must stay free, shown on the floor while carrying so the player sees why a spot is blocked.
        public IEnumerable<Rect> KeepClearAreas
        {
            get
            {
                foreach (var zone in _keepClear)
                {
                    yield return zone.Area;
                }
            }
        }

        // Raised after a table or chair is placed somewhere new, so the game can save.
        public event Action Changed;

        // An extra area furniture may not be placed on, such as a decoration corner.
        public void KeepClear(Rect area)
        {
            _keepClear.Add(new Zone(area, PlaceCheck.Decor));
        }

        // 'layoutSaved' means the lists are the complete layout, even when empty (everything may be in storage).
        public void Restore(List<Vector3> tables, List<Vector3> chairs, List<DecorSave> decor, bool layoutSaved,
            bool decorSaved)
        {
            _layoutSaved = layoutSaved;
            _decorSaved = decorSaved;
            _savedTables.Clear();
            _savedChairs.Clear();
            _savedDecor.Clear();
            if (decor != null)
            {
                _savedDecor.AddRange(decor);
            }

            if (tables != null)
            {
                _savedTables.AddRange(tables);
            }

            if (chairs != null)
            {
                _savedChairs.AddRange(chairs);
            }
        }

        // Builds the saved layout. A brand-new game gets 'defaultTables' with four chairs each; old saves without
        // chairs get four per table.
        public void Build(int defaultTables, IReadOnlyList<DecorSave> defaultDecor, List<string> overflow)
        {
            var hasChairs = _layoutSaved || _savedChairs.Count > 0;
            var count = _layoutSaved || _savedTables.Count > 0 ? _savedTables.Count : defaultTables;
            for (var index = 0; index < count; index++)
            {
                var table = AddTable(index);
                if (!hasChairs)
                {
                    AddDefaultChairs(table);
                }
            }

            foreach (var saved in _savedChairs)
            {
                _chairs.Add(TableBuilder.BuildChair(_root, new Vector3(saved.x, 0f, saved.z), saved.y));
            }

            foreach (var saved in _savedDecor)
            {
                var def = FurnitureCatalog.Find(saved.Id);
                if (def != null)
                {
                    _decor.Add(DecorBuilder.Build(_root, def, new Vector3(saved.X, 0f, saved.Z), saved.Yaw));
                }
            }

            if (!_decorSaved)
            {
                AddDefaultDecor(defaultDecor, overflow);
            }

            Regroup();
        }

        // The starting plant, lamp and sofa. One that no longer fits (the player has since filled the spot) is
        // reported in 'overflow' so it can go to storage instead of being lost.
        private void AddDefaultDecor(IReadOnlyList<DecorSave> defaults, List<string> overflow)
        {
            foreach (var entry in defaults)
            {
                var def = FurnitureCatalog.Find(entry.Id);
                if (def == null)
                {
                    continue;
                }

                var position = new Vector3(entry.X, 0f, entry.Z);
                var item = DecorBuilder.Build(_root, def, position, entry.Yaw);
                if (CheckDecor(item, position, Vector3.zero, -1f) == PlaceCheck.Ok)
                {
                    _decor.Add(item);
                }
                else
                {
                    UnityEngine.Object.Destroy(item.gameObject);
                    overflow.Add(def.Id);
                }
            }
        }

        // The nearest unused seat on a bench, sofa or armchair, for the owner or a resting waiter.
        public StaffSeat FindFreeStaffSeat(Vector3 near)
        {
            StaffSeat best = null;
            var bestDistance = float.MaxValue;
            foreach (var item in _decor)
            {
                if (item == _movingDecor)
                {
                    continue;
                }

                foreach (var seat in item.Seats)
                {
                    var distance = FlatDistance(seat.transform.position, near);
                    if (seat.IsFree && distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = seat;
                    }
                }
            }

            return best;
        }

        public List<Vector3> SnapshotTables()
        {
            var result = new List<Vector3>(_tables.Count);
            foreach (var table in _tables)
            {
                if (table == _movingTable && _movingIsNew)
                {
                    continue;
                }

                var moving = table == _movingTable;
                var position = moving ? _tableFrom : table.transform.position;
                var rotation = moving ? _tableFromRotation : table.transform.rotation;
                result.Add(new Vector3(position.x, rotation.eulerAngles.y, position.z));
            }

            return result;
        }

        // Anything being carried is saved where it was picked up from.
        public List<Vector3> SnapshotChairs()
        {
            var result = new List<Vector3>(_chairs.Count);
            foreach (var chair in _chairs)
            {
                if (chair == _movingChair && _movingIsNew)
                {
                    continue;
                }

                Vector3 position;
                Quaternion rotation;
                if (chair == _movingChair)
                {
                    position = _chairFrom;
                    rotation = _chairFromRotation;
                }
                else if (_movingTable != null && chair.Table == _movingTable)
                {
                    // Carried along with its table: undo the table's offset and turn.
                    var local = _movingTable.transform.InverseTransformPoint(chair.transform.position);
                    position = _tableFrom + _tableFromRotation * local;
                    rotation = _tableFromRotation * Quaternion.Inverse(_movingTable.transform.rotation) * chair.transform.rotation;
                }
                else
                {
                    position = chair.transform.position;
                    rotation = chair.transform.rotation;
                }

                result.Add(new Vector3(position.x, rotation.eulerAngles.y, position.z));
            }

            return result;
        }

        public List<DecorSave> SnapshotDecor()
        {
            var result = new List<DecorSave>(_decor.Count);
            foreach (var item in _decor)
            {
                if (item == _movingDecor && _movingIsNew)
                {
                    continue;
                }

                var moving = item == _movingDecor;
                var position = moving ? _decorFrom : item.transform.position;
                var rotation = moving ? _decorFromRotation : item.transform.rotation;
                result.Add(new DecorSave { Id = item.Def.Id, X = position.x, Yaw = rotation.eulerAngles.y, Z = position.z });
            }

            return result;
        }

        // ---------- Storage: new pieces in, old pieces out ----------

        // Starts carrying a brand-new piece from storage at 'at'. The caller takes it out of the inventory only
        // once TryEndMove* succeeds, so a cancel or a quit mid-carry loses nothing.
        public bool BeginPlaceNew(FurnitureDef def, Vector3 at)
        {
            if (IsBusy)
            {
                return false;
            }

            bool started;
            switch (def.Kind)
            {
                case FurnitureKind.Table:
                    started = BeginMove(CreateLooseTable(at));
                    break;
                case FurnitureKind.Chair:
                    var chair = TableBuilder.BuildChair(_root, at, 0f);
                    _chairs.Add(chair);
                    started = BeginMoveChair(chair);
                    break;
                default:
                    var item = DecorBuilder.Build(_root, def, at, 0f);
                    _decor.Add(item);
                    started = BeginMoveDecor(item);
                    break;
            }

            _movingIsNew = started;
            return started;
        }

        public bool StoreTable(BarTable table)
        {
            if (IsBusy || table == null || table.HasOccupants)
            {
                return false;
            }

            RemoveTable(table);
            Changed?.Invoke();
            return true;
        }

        public bool StoreChair(BarChair chair)
        {
            if (IsBusy || chair == null || chair.IsTaken)
            {
                return false;
            }

            RemoveChair(chair);
            Changed?.Invoke();
            return true;
        }

        public bool StoreDecor(PlacedDecor item)
        {
            if (IsBusy || item == null)
            {
                return false;
            }

            RemoveDecor(item);
            Changed?.Invoke();
            return true;
        }

        // ---------- Decorations ----------

        public bool BeginMoveDecor(PlacedDecor item)
        {
            if (IsBusy || item == null)
            {
                return false;
            }

            _movingDecor = item;
            _decorFrom = item.transform.position;
            _decorFromRotation = item.transform.rotation;
            Regroup();
            return true;
        }

        public void RotateCarriedDecor()
        {
            _movingDecor?.transform.Rotate(0f, RotateStep, 0f, Space.World);
        }

        // Rugs only need to stay inside the room; standing items need a free floor like a table does.
        public PlaceCheck CheckDecor(PlacedDecor item, Vector3 floor, Vector3 owner, float ownerRadius)
        {
            var rect = item.FootprintAt(floor);
            if (item.IsFlat)
            {
                return InsideRoom(rect) ? PlaceCheck.Ok : PlaceCheck.Wall;
            }

            return CheckArea(null, rect, owner, ownerRadius);
        }

        public bool TryEndMoveDecor(Vector3 floor, Vector3 owner, float ownerRadius)
        {
            if (_movingDecor == null || CheckDecor(_movingDecor, floor, owner, ownerRadius) != PlaceCheck.Ok)
            {
                return false;
            }

            _movingDecor.transform.position = floor;
            _movingDecor = null;
            _movingIsNew = false;
            Regroup();
            Changed?.Invoke();
            return true;
        }

        // ---------- Tables (carried together with their chairs) ----------

        public bool BeginMove(BarTable table)
        {
            if (IsBusy || table == null || table.HasOccupants)
            {
                return false;
            }

            _movingTable = table;
            _tableFrom = table.transform.position;
            _tableFromRotation = table.transform.rotation;
            foreach (var chair in table.Chairs)
            {
                chair.transform.SetParent(table.transform, true);
            }

            table.IsBeingMoved = true;
            _seating.Refresh();
            return true;
        }

        // Puts the carried table down where it is now; false (still carried) if that spot is not allowed.
        public bool TryEndMove(Vector3 owner, float ownerRadius)
        {
            if (_movingTable == null || CheckTable(_movingTable, _movingTable.transform.position, owner, ownerRadius) != PlaceCheck.Ok)
            {
                return false;
            }

            _movingIsNew = false;
            ReleaseTable();
            Changed?.Invoke();
            return true;
        }

        // Puts a carried piece back where it came from; a piece fresh from storage is thrown away (it was never
        // taken out of the inventory).
        public void CancelMove()
        {
            var discard = _movingIsNew;
            _movingIsNew = false;

            if (_movingTable != null)
            {
                var table = _movingTable;
                table.transform.SetPositionAndRotation(_tableFrom, _tableFromRotation);
                ReleaseTable();
                if (discard)
                {
                    RemoveTable(table);
                }
            }

            if (_movingChair != null)
            {
                var chair = _movingChair;
                chair.transform.SetPositionAndRotation(_chairFrom, _chairFromRotation);
                ReleaseChair();
                if (discard)
                {
                    RemoveChair(chair);
                }
            }

            if (_movingDecor != null)
            {
                var item = _movingDecor;
                item.transform.SetPositionAndRotation(_decorFrom, _decorFromRotation);
                _movingDecor = null;
                if (discard)
                {
                    RemoveDecor(item);
                }
                else
                {
                    Regroup();
                }
            }
        }

        // The table and its chairs must fit inside the room, off other furniture, and clear of the no-go areas.
        // 'owner' is where the player stands; the table must not cover them or they would be stuck inside it.
        public PlaceCheck CheckTable(BarTable table, Vector3 center, Vector3 owner, float ownerRadius)
        {
            return CheckArea(table, table.FootprintAt(center), owner, ownerRadius);
        }

        // ---------- Chairs ----------

        public bool BeginMoveChair(BarChair chair)
        {
            if (IsBusy || chair == null || chair.IsTaken)
            {
                return false;
            }

            _movingChair = chair;
            _chairFrom = chair.transform.position;
            _chairFromRotation = chair.transform.rotation;
            // Reserved while carried, so no guest picks it, and taken out of its table's group.
            chair.Reserve();
            Regroup();
            return true;
        }

        // Turns a carried chair; only matters for chairs standing alone (at a table they face it).
        public void RotateCarriedChair()
        {
            _movingChair?.transform.Rotate(0f, RotateStep, 0f, Space.World);
        }

        // 'position' is on the floor. The chair is set down at sitting height above it.
        public bool TryEndMoveChair(Vector3 position, Vector3 owner, float ownerRadius)
        {
            if (_movingChair == null || CheckChair(_movingChair, position, owner, ownerRadius) != PlaceCheck.Ok)
            {
                return false;
            }

            _movingChair.transform.position = position + Vector3.up * TableBuilder.SeatHeight;
            _movingIsNew = false;
            ReleaseChair();
            Changed?.Invoke();
            return true;
        }

        // A chair may stand anywhere free. If it lands near a table it joins it, and that table must still fit.
        public PlaceCheck CheckChair(BarChair chair, Vector3 position, Vector3 owner, float ownerRadius)
        {
            var c = TableBuilder.ChairHalfSize;
            var rect = Rect.MinMaxRect(position.x - c, position.z - c, position.x + c, position.z + c);
            var near = Rect.MinMaxRect(owner.x - ownerRadius, owner.z - ownerRadius, owner.x + ownerRadius, owner.z + ownerRadius);
            if (rect.Overlaps(near))
            {
                return PlaceCheck.Owner;
            }

            foreach (var other in _chairs)
            {
                if (other != chair && FlatDistance(position, other.transform.position) < BarTable.MinSeatGap)
                {
                    return PlaceCheck.Chair;
                }
            }

            var table = NearestTable(position, out var distance);
            if (table != null && distance < BarTable.MinSeatDistance)
            {
                return PlaceCheck.UnderTable;
            }

            if (table != null && distance <= BarTable.SeatRange)
            {
                // Joining this table: its grown footprint must still fit. The owner may stand inside the margin.
                return CheckArea(table, table.FootprintWithChair(chair, position), owner, -1f, chair);
            }

            return CheckArea(null, rect, owner, -1f, chair);
        }

        private bool IsBusy => _movingTable != null || _movingChair != null || _movingDecor != null;

        public BarTable CarriedTable => _movingTable;

        public BarChair CarriedChair => _movingChair;

        public PlacedDecor CarriedDecor => _movingDecor;

        // True while the carried piece came from storage.
        public bool CarryingNew => _movingIsNew;

        private void ReleaseTable()
        {
            foreach (var chair in _movingTable.Chairs)
            {
                chair.transform.SetParent(_root, true);
            }

            _movingTable.IsBeingMoved = false;
            _movingTable = null;
            Regroup();
        }

        private void ReleaseChair()
        {
            _movingChair.Release();
            _movingChair = null;
            Regroup();
        }

        private void RemoveTable(BarTable table)
        {
            _tables.Remove(table);
            _seating.RemoveTable(table);
            UnityEngine.Object.Destroy(table.gameObject);
            Regroup();
        }

        private void RemoveChair(BarChair chair)
        {
            _chairs.Remove(chair);
            UnityEngine.Object.Destroy(chair.gameObject);
            Regroup();
        }

        private void RemoveDecor(PlacedDecor item)
        {
            _decor.Remove(item);
            UnityEngine.Object.Destroy(item.gameObject);
            Regroup();
        }

        // Every chair joins the nearest table in range (chairs with a guest stay where they are), the rest stand
        // alone; then seats and walk-around obstacles are rebuilt.
        private void Regroup()
        {
            foreach (var table in _tables)
            {
                _group.Clear();
                foreach (var chair in _chairs)
                {
                    if (TargetTable(chair) == table)
                    {
                        _group.Add(chair);
                    }
                }

                table.SetChairs(_group);
            }

            _looseAreas.Clear();
            foreach (var chair in _chairs)
            {
                var table = TargetTable(chair);
                chair.AssignTo(table);
                if (table == null && chair != _movingChair)
                {
                    var p = chair.transform.position;
                    var half = TableBuilder.ChairHalfSize + LooseChairMargin;
                    _looseAreas.Add(Rect.MinMaxRect(p.x - half, p.z - half, p.x + half, p.z + half));
                }
            }

            foreach (var item in _decor)
            {
                if (item == _movingDecor || item.IsFlat)
                {
                    continue;
                }

                var r = item.Footprint;
                _looseAreas.Add(Rect.MinMaxRect(r.xMin - LooseChairMargin, r.yMin - LooseChairMargin,
                    r.xMax + LooseChairMargin, r.yMax + LooseChairMargin));
            }

            _seating.SetLooseChairAreas(_looseAreas);
        }

        // A carried chair belongs to nobody (so no table's footprint stretches to follow it); a chair with a guest
        // keeps its table; any other chair joins the nearest table in range.
        private BarTable TargetTable(BarChair chair)
        {
            if (chair == _movingChair)
            {
                return null;
            }

            return chair.IsTaken ? chair.Table : NearestTableInRange(chair.transform.position);
        }

        private BarTable NearestTableInRange(Vector3 position)
        {
            var table = NearestTable(position, out var distance);
            return table != null && distance <= BarTable.SeatRange ? table : null;
        }

        private BarTable NearestTable(Vector3 position, out float distance)
        {
            BarTable best = null;
            distance = float.MaxValue;
            foreach (var table in _tables)
            {
                var d = FlatDistance(position, table.transform.position);
                if (d < distance)
                {
                    distance = d;
                    best = table;
                }
            }

            return best;
        }

        // 'self' is the table being placed (its own footprint is ignored); 'ignoreChair' is a chair being placed.
        private PlaceCheck CheckArea(BarTable self, Rect rect, Vector3 owner, float ownerRadius, BarChair ignoreChair = null)
        {
            if (!InsideRoom(rect))
            {
                return PlaceCheck.Wall;
            }

            var shrunk = Shrink(rect);
            foreach (var zone in _keepClear)
            {
                if (shrunk.Overlaps(zone.Area))
                {
                    return zone.Reason;
                }
            }

            foreach (var other in _tables)
            {
                if (other != self && other != _movingTable && shrunk.Overlaps(other.Footprint))
                {
                    return PlaceCheck.Table;
                }
            }

            foreach (var item in _decor)
            {
                if (item != _movingDecor && !item.IsFlat && shrunk.Overlaps(item.Footprint))
                {
                    return PlaceCheck.Item;
                }
            }

            // Lone chairs (not ours, not the one being placed) must not end up inside this footprint.
            var c = TableBuilder.ChairHalfSize;
            foreach (var chair in _chairs)
            {
                if (chair == ignoreChair || (self != null && chair.Table == self) || chair.Table != null)
                {
                    continue;
                }

                var p = chair.transform.position;
                if (shrunk.Overlaps(Rect.MinMaxRect(p.x - c, p.z - c, p.x + c, p.z + c)))
                {
                    return PlaceCheck.Chair;
                }
            }

            // A negative radius means "do not check the owner" (used on load, and for chairs).
            if (ownerRadius >= 0f)
            {
                var near = Rect.MinMaxRect(owner.x - ownerRadius, owner.z - ownerRadius,
                    owner.x + ownerRadius, owner.z + ownerRadius);
                if (rect.Overlaps(near))
                {
                    return PlaceCheck.Owner;
                }
            }

            return PlaceCheck.Ok;
        }

        private bool InsideRoom(Rect rect)
        {
            var inner = BarLayout.WallThickness * 0.5f + WallMargin;
            return rect.xMin >= -_layout.HalfX + inner && rect.xMax <= _layout.HalfX - inner
                && rect.yMin >= _layout.FrontZ + inner && rect.yMax <= _layout.HalfZ - inner;
        }

        // A table with no chairs, registered and ready to be carried.
        private BarTable CreateLooseTable(Vector3 position)
        {
            var table = _createTable(position);
            AddPickVolume(table);
            _tables.Add(table);
            _seating.AddTable(table);
            return table;
        }

        private BarTable AddTable(int index)
        {
            Vector3 position;
            var yaw = 0f;
            var saved = index < _savedTables.Count;
            if (saved)
            {
                position = new Vector3(_savedTables[index].x, 0f, _savedTables[index].z);
                yaw = _savedTables[index].y;
            }
            else
            {
                position = _defaultSlot(index);
            }

            var table = _createTable(position);
            table.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            AddPickVolume(table);

            // A newly bought table may land where the player rearranged things; find the nearest free spot.
            // Its four chairs are not built yet, so check the area they will take.
            if (!saved && CheckNewTable(position) != PlaceCheck.Ok)
            {
                table.transform.position = FindFreeSpot(position);
            }

            _tables.Add(table);
            _seating.AddTable(table);
            return table;
        }

        private void AddDefaultChairs(BarTable table)
        {
            var center = table.transform.position;
            foreach (var direction in TableBuilder.DefaultSeatDirections)
            {
                var position = center + table.transform.rotation * direction * TableBuilder.DefaultSeatDistance;
                _chairs.Add(TableBuilder.BuildChair(_root, position, 0f));
            }
        }

        private PlaceCheck CheckNewTable(Vector3 center)
        {
            var half = TableBuilder.DefaultSeatDistance + TableBuilder.ChairHalfSize + NewTableMargin;
            return CheckArea(null, Rect.MinMaxRect(center.x - half, center.z - half, center.x + half, center.z + half),
                Vector3.zero, -1f);
        }

        // Nearest grid spot to 'preferred' where a new table and its chairs fit, searching outward ring by ring.
        private Vector3 FindFreeSpot(Vector3 preferred)
        {
            var maxRings = Mathf.CeilToInt(Mathf.Max(_layout.HalfX, _layout.HalfZ) * 2f / SearchStep);
            for (var ring = 1; ring <= maxRings; ring++)
            {
                for (var dx = -ring; dx <= ring; dx++)
                {
                    for (var dz = -ring; dz <= ring; dz++)
                    {
                        if (Mathf.Abs(dx) != ring && Mathf.Abs(dz) != ring)
                        {
                            continue;
                        }

                        var candidate = preferred + new Vector3(dx * SearchStep, 0f, dz * SearchStep);
                        if (CheckNewTable(candidate) == PlaceCheck.Ok)
                        {
                            return candidate;
                        }
                    }
                }
            }

            return preferred;
        }

        // A box over the table top the owner can aim at (models have no colliders); chairs have their own.
        private static void AddPickVolume(BarTable table)
        {
            var pick = new GameObject("PickVolume") { layer = FurnitureLayer };
            pick.transform.SetParent(table.transform, false);
            var box = pick.AddComponent<BoxCollider>();
            var size = TableBuilder.TopHalfSize * 2f;
            box.size = new Vector3(size, PickHeight, size);
            box.center = new Vector3(0f, PickHeight * 0.5f, 0f);
        }

        // Where guests walk in and wait, where they queue to pay, and where staff stand.
        private void BuildKeepClearZones(Rect counter)
        {
            _keepClear.Add(new Zone(counter, PlaceCheck.Counter));

            var door = _layout.DoorX;
            var half = BarLayout.DoorHalfWidth;
            _keepClear.Add(new Zone(Rect.MinMaxRect(door - half, _layout.FrontZ, door + half,
                _layout.DoorInside.z + DoorZoneDepth), PlaceCheck.Door));

            // The first guest stands level with the counter's middle; the line grows toward the back wall.
            var queue = _layout.QueueFirstSpot;
            var cashier = _layout.CashierSpot;
            _keepClear.Add(new Zone(Rect.MinMaxRect(queue.x - QueueHalfWidth, queue.z - QueueHalfWidth,
                cashier.x + QueueHalfWidth, queue.z + QueueDepth), PlaceCheck.Queue));

            var station = _layout.WaiterStation;
            _keepClear.Add(new Zone(new Rect(station.x - StationHalfSize, station.z - StationHalfSize,
                StationHalfSize * 2f, StationHalfSize * 2f), PlaceCheck.Staff));
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            var d = a - b;
            d.y = 0f;
            return d.magnitude;
        }

        private static Rect Shrink(Rect rect)
        {
            return Rect.MinMaxRect(rect.xMin + OverlapShrink, rect.yMin + OverlapShrink,
                rect.xMax - OverlapShrink, rect.yMax - OverlapShrink);
        }

        private readonly struct Zone
        {
            public readonly Rect Area;
            public readonly PlaceCheck Reason;

            public Zone(Rect area, PlaceCheck reason)
            {
                Area = area;
                Reason = reason;
            }
        }
    }
}
