# Startup — Game Development Rules

> READ THIS FIRST. These rules apply to every piece of work on this project.
> This is a Unity 3D business-simulation game ("Startup"). First mode: bar/pub tycoon.

## Project facts (do not re-ask)

- **Engine / Language:** Unity + URP, C# only.
- **UI:** UI Toolkit (UXML + USS). Not UGUI.
- **Target:** Mobile landscape first, also runs on PC. Develop/test on PC (Editor Play), build Android to verify touch & performance.
- **Input:** Unity Input System, unified pointer (touch + mouse share one code path).
- **Data:** Offline-first via an `IDataStore` interface. A server backend comes later behind the same interface — never couple gameplay to a concrete store.
- **Localization:** Multi-language from day one. See "Localization rules" below. NEVER hardcode user-facing text.
- **Architecture:** Feature-based folders under `Assets/_Project/`, split by Assembly Definitions (asmdef).
- **Visuals:** Primitive shapes now (cubes/capsules), swap to real models later WITHOUT changing logic.

## Code style

- Write **clean, self-documenting code**. Prefer clear names over clever tricks.
- **Comments in English, short, under two lines.** Comment the *why*, not the *what*. No comment is better than a redundant one.
- Follow Unity/C# naming conventions:
  - `PascalCase` for classes, methods, properties, public fields, enums.
  - `camelCase` for local variables and parameters.
  - `_camelCase` for private fields (leading underscore).
  - `UPPER_CASE` or `PascalCase` for constants (`const` / `static readonly`).
  - No special/Unicode characters in identifiers.
- One public type per file; file name matches the type name.
- Keep methods small and single-purpose. If a method needs a paragraph to explain, split it.
- Avoid magic numbers — use named constants or serialized/ScriptableObject config.

## Architecture rules

- **Modular, low coupling.** Systems communicate through interfaces, events, or ScriptableObjects — never hard references across features.
- **Data-driven with ScriptableObjects.** Store tunable/shared data (prices, spawn rates, menu items, locale list) in ScriptableObject assets, not hardcoded in MonoBehaviours. Designers tune assets; code reads them.
- **Scenes are clean slates.** No transient data carried implicitly between scenes. Pass state explicitly (through a persistent service or saved data), load each scene fresh.
- **Separation of concerns:** logic in C#, layout/style in UXML/USS. UI controllers bind data to views; they do not hold game logic.
- **Centralized scene flow.** All scene transitions go through one `SceneLoader` (Core/SceneFlow). No scattered `SceneManager.LoadScene` calls.
- **Respect asmdef boundaries.** A feature may depend on Core/Shared/Data, not on another feature. If two features need to talk, do it through Core or an event channel.
- **MonoBehaviour is glue, not logic.** Keep heavy logic in plain C# classes that are testable; MonoBehaviours wire them to the engine.

## Namespaces

- Root namespace: `Startup`.
- Match folders: `Startup.Core.SceneFlow`, `Startup.Features.MainMenu`, `Startup.Data`, `Startup.Shared`, etc.

## Localization rules (CRITICAL — enforce on every change)

- **Every user-facing string MUST go through the localization system.** This includes UI labels, button text, dialogue, item names/descriptions, tooltips, toasts, and error messages shown to players.
- Use the **Unity Localization package** with **String Tables** (key → translated value per locale) and **Smart Strings** for placeholders/plurals.
- When you add or change any user-facing text:
  1. Add a **key** to the appropriate String Table (e.g. `menu_title`, `btn_start`, `shop_buy_table`).
  2. Provide the **Vietnamese (default)** value and at least an **English** value.
  3. Reference the key in UI/code via `LocalizedString` — never inline the literal.
- **Key naming:** lowercase `snake_case`, grouped by feature prefix: `menu_`, `splash_`, `bargame_`, `hud_`, `shop_`, `common_`.
- Default/development locale: **Vietnamese (vi)**. Also maintain **English (en)**. Structure so adding a new locale = adding a column, no code change.
- Never concatenate localized fragments to build sentences — use Smart String placeholders so other languages can reorder.
- Fonts must cover the character sets of supported locales (check before shipping a new language).

## Workflow & verification

- Work in the order of `plan.html` (10 phases). Finish and verify a phase before moving on.
- After code changes, make sure the project compiles and Play mode runs without errors.
- Do NOT add tests unless explicitly requested.
- Keep diffs small and focused. Fix the root cause, not the symptom.
- Primitive-based visuals are acceptable; do not block gameplay work waiting for art.
