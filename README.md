# YVOS — A Life in the Realm

A text-based medieval fantasy life simulator built in Unity, loosely inspired by BitLife.

Live an entire life from birth to death: respond to random yearly events, join guilds, build relationships, accumulate wealth, fight monsters, and eventually die (hopefully of old age).

---

## Tech Stack

- **Engine:** Unity 6 LTS (2D, URP disabled)
- **Language:** C# (.NET Standard 2.1)
- **Extra Packages:**
  - `Newtonsoft.Json` — save/load serialization
  - `TextMeshPro` — UI text rendering

---

## Getting Started

### Prerequisites

- [Unity Hub](https://unity.com/download)
- Unity **6000.0.23f1** (install via Unity Hub → Installs → Add → find version)

### Opening the Project

1. Clone the repo: `git clone https://github.com/hansmel21/yvos.git`
2. Open **Unity Hub** → **Projects** → **Open** → select the `yvos/` folder
3. Unity will auto-generate `Library/`, `Temp/`, and `.meta` files on first open (~1–2 min)
4. Open the `MainMenu` scene from `Assets/Scenes/`

---

## Project Structure

```
Assets/
├── Scenes/               Unity scene files (MainMenu, CharacterCreation, GameScene)
├── Scripts/
│   ├── Core/             GameManager, TimeManager, EventBus
│   ├── Character/        CharacterStats, CharacterData, CharacterLifecycle, enums
│   ├── Events/           LifeEventSO, LifeEventEngine, EventResolver
│   ├── Career/           GuildDefinitionSO, GuildManager, CareerProgression
│   ├── Relationships/    RelationshipManager, FamilyTree, RelationshipData
│   ├── Economy/          EconomyManager, InventorySystem, ItemDefinitionSO
│   ├── History/          HistoryLog, HistoryEntry
│   ├── Persistence/      SaveManager, SaveData
│   ├── Combat/           CombatEngine, EnemyDefinitionSO, CombatData
│   ├── MiniGames/        MiniGameEngine, MiniGameDefinitionSO
│   └── UI/               All UI controller scripts
├── Data/
│   ├── Events/           ScriptableObject event definitions
│   ├── Careers/          Guild definitions
│   ├── Races/            Race definitions
│   ├── Items/            Item definitions
│   └── Enemies/          Enemy definitions
└── UI/Prefabs/           Reusable UI prefabs
```

---

## Development Phases

| Phase | Scope | Status |
|-------|-------|--------|
| 0 — Bootstrap | Project structure + core scripts | ✅ In Progress |
| 1 — MVP Loop | Birth → events → death | 🔜 |
| 2 — Guilds + Economy + Family | Career, relationships, dynasty | 🔜 |
| 2.5 — Combat | D-Pad simultaneous combat system | 🔜 |
| 3 — Mini-Games + Save | Guild mini-games, save/load, full content | 🔜 |
| 4 — V1.0 RC | Polish, builds, store | 🔜 |

---

## Combat System

Combat uses a **simultaneous D-Pad selection** mechanic (Rock-Paper-Scissors style):

```
       [ ↑ ATTACK ]
            |
[ ← DODGE ] ✦ [ → BLOCK ]
            |
    [ ↓ SPELL / ITEM ]
```

Both player and CPU pick at the same time. **No-repeat rule**: each action has a 1-round cooldown after use — you can't spam the same move every round.

---

## Contributing

See the plan at `.claude/plans/` for detailed implementation notes.
