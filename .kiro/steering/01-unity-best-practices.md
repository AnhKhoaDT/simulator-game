# Unity Best Practices — Khởi Nghiệp

> Companion to 00-game-dev-rules. Concrete Unity patterns to apply when writing code.
> Sourced asmdeffrom Unity's official architecture & naming guides and the Localization docs.

## Performance (mobile matters most)

- **Nothing heavy in `Update()`.** Avoid `GetComponent`, `Find`, `Camera.main`, allocations, LINQ, or string building per frame. Cache references in `Awake`/`Start`.
- Use **object pooling** for frequently spawned/destroyed objects (customers, coins, popups). Do not `Instantiate`/`Destroy` in hot paths.
- Prefer events/coroutines/timers over polling flags every frame.
- Cache `transform`, components, and WaitForSeconds instances; reuse them.
- Keep draw calls low: share materials, use URP mobile renderer, batch where possible.
- Avoid per-frame garbage: no `new` in `Update`, no boxing, no `foreach` over structs that allocate.

## ScriptableObjects (data-driven design)

- Use ScriptableObjects for shared/tunable data: prices, spawn rates, menu items, customer patience, locale config, upgrade costs.
- Benefits: tune without recompiling, reuse across scenes, decouple data from behaviour, enable designer/dev parallel work.
- Consider ScriptableObject-based **event channels** for cross-system messaging (e.g. `MoneyChangedEvent`) to keep systems decoupled.
- Consider **runtime sets** (SO holding a live collection) for tracking active customers/tables without hard references.

## Code organization

- Favor composition over inheritance. Small focused components over god-classes.
- Program to interfaces for swappable systems (`IDataStore`, `ISpawner`), especially where a server/local or real/fake split is expected.
- Serialize private fields with `[SerializeField]` instead of making them public.
- Use `[RequireComponent]`, `[Tooltip]`, and sensible `[Header]`/`[Range]` to make inspectors clear and safe.
- Null-check serialized references early; fail loud in the Editor, not silently at runtime.

## Scene & lifecycle

- `Awake`: self-setup and caching. `Start`: cross-object wiring. `OnEnable`/`OnDisable`: subscribe/unsubscribe events (always unsubscribe to avoid leaks).
- One bootstrap entry point per scene; do not rely on script execution order hacks.
- Each scene loads clean; persistent services live in a dedicated bootstrap/persistent context, not duplicated per scene.

## UI Toolkit

- Structure in UXML, style in USS, behaviour in a C# controller that queries the `VisualElement` tree.
- Reuse shared USS variables (colors, spacing, fonts) from `UI/Shared` — theme in one place.
- Use flexbox and percentage units for responsive layout; handle safe area for notches.
- Bind localized text via `LocalizedString` / localization bindings — do not set literal strings on labels in code.

## Localization implementation notes

- Package: `com.unity.localization`. Create Locales, then String Tables (keys + values per locale).
- `LocalizedString` auto-updates when the active Locale changes — support runtime language switching, no restart.
- Use Smart Strings for placeholders and plurals instead of string concatenation.
- Optionally drive translation via CSV/Google Sheets import for scale; keep keys stable.
- Preload String Tables used at startup (Splash/Menu) to avoid first-frame pop-in.

## Git hygiene

- Respect `.gitignore` (Library/, Temp/, Build/ are generated — never commit).
- Commit `.meta` files alongside their assets; never delete a `.meta` without its asset.
- Keep commits scoped to one logical change.
