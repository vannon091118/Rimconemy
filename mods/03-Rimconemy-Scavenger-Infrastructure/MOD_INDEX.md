# MOD_INDEX.md — 03 · Rimconemy Scavenger Infrastructure

> **Stand:** 2026-08-06 · **Owner:** Scavenger Infrastructure
> **Abgeleitet von:** [ROADMAP.md](../../ROADMAP.md) + [INTERFACE_CONTRACT.md](../../docs/INTERFACE_CONTRACT.md) + [BLUEPRINT.md](BLUEPRINT.md)
> **Code-Status:** [CODE_STATUS.md](../../docs/CODE_STATUS.md)
> **Tatsächlicher Code:** `Source/` (17 .cs-Dateien), `Tests/` (7 Suiten)

---

## §1 Abhängigkeitsmatrix

### §1.1 Compile-Dependencies

| Von | Nach | Typ |
|---|---|---|
| 03 Scavenger | 01 Foundation | **DLL-Ref** |
| 03 Scavenger | RimWorld Assemblies | DLL-Ref |
| 03 Scavenger | Harmony | DLL-Ref |

### §1.2 Runtime-Dependencies (Capability-Gate)

| Benötigte Capability | Owner | Fallback bei fehlend |
|---|---|---|
| `rimconemy.foundation.profile` | 01 | — (zwingend, via DLL-Ref) |
| `rimconemy.foundation.colonials` | 01 | — |
| `rimconemy.foundation.dlc_filter` | 01 | — |
| `rimconemy.survivalprogression.progression` | 02 | Building-XP wird nicht gemeldet |
| `rimconemy.infectedautomation.threat` | 05 | Power-Drain = 0 |

### §1.3 DLC-Anker

| DLC | Nutzung in 03 | Fehlend-Fallback |
|---|---|---|
| Core | Bauen, Pflanzen, Power, Storage | unverzichtbar |
| Royalty | Empire-Trader (TributeTrader Patch) | Core-only: Vanilla-Trader |
| Ideology | ResourceFairness-Precept | Core-only |
| Biotech | Mechanitor-Gene (Mining-Patch) | Core-only |
| Anomaly | Odyssey-Ruins (ConstructionDebris) | Core-only |
| Odyssey | Ruins-Loot-Stashes | Core-only: Vanilla-Ancient-Ruins |

---

## §2 Standalone-Prüfung

### §2.1 Funktionen die Foundation bereitstellt → Consumer-Code NUTZT diese

| Funktion | Foundation-Pfad | Wo in 03 genutzt |
|---|---|---|
| `CapabilityAudit.HasCapabilityOrWarn` | `01/Source/Registry/` | `Bootstrap.cs` |
| `PackageRegistry.IsRegistered` | `01/Source/Registry/` | `Bootstrap.cs` |
| `DLCFilter.IsContentEnabled` | `01/Source/DLC/` | `Bootstrap.cs` |
| `RimconemyUi` (UI-Helper) | `01/Source/UI/` | `InfrastructureDashboard.cs` |

### §2.2 Funktionen die in 03 DUPLIZIERT sein müssen (Standalone-Nutzbarkeit)

| Funktion | Grund für Duplikat | Pfad in 03 |
|---|---|---|
| **StorageQuery** | Foundation kennt kein Storage-System. 03 ist **Sole-Owner** aller physischen Ressourcen-Reads | `Source/Storage/StorageQuery.cs` |
| **StorageSnapshot** | Snapshots sind 03-Domäne; 05 liest sie via DLL-Ref | `Source/Storage/StorageSnapshot.cs` |
| **Bauschutt-System** | Foundation hat keine Bau-Ressourcen. 03 besitzt `BauschuttRemapService` | `Source/Building/` |
| **Power-Chain** | Foundation hat kein Power-Modell. 03 besitzt `PowerChainService` | `Source/Power/` |
| **Building-Snapshots** | Reine 03-Domäne: `BuildingSnapshotService` | `Source/Building/` |
| **Pflanzen/Farming** | Foundation kennt keine Pflanzen. 03 besitzt `FoodHarvestCycleService` | `Source/Plants/` |
| **ArrowTurret** | Reine 03-Domäne: `ArrowTurretPowerGate` | `Source/Building/` |

### §2.3 Funktionen die Foundation BEREITS hat → 03-Duplikat wäre ein Fehler

| Foundation-Funktion | 03 DARF NICHT duplizieren | Grund |
|---|---|---|
| Profil-Erkennung | ❌ | `ProfileDetector` in 01 ist SSOT |
| DLC-Prüfung | ❌ | `DLCFilter` in 01 ist SSOT |
| Capability-Gates | ❌ | `CapabilityAudit` in 01 ist SSOT |
| UI-Base-Classes | ❌ | `RimconemyWindow` in 01 ist SSOT |
| Kolonisten-Zählung | ❌ | `ColonialReader` in 01 ist SSOT |

---

## §3 Tatsächlicher Code-Stand

### §3.1 Was existiert (CODE + COMPILES + BOOT)

| Modul | Dateien | Status |
|---|---|---|
| Bootstrap | `Bootstrap.cs` | BOOT ✅ |
| Storage | `StorageQuery.cs`, `StorageSnapshot.cs`, `StorageScope.cs`, `StorageWriteMutationService.cs`, `CaravanStorageEnumerator.cs` | BOOT ✅ |
| Building | `BauschuttRemapService.cs`, `BauschuttRemapApply.cs`, `BuildingSnapshot.cs`, `BuildingSnapshotService.cs`, `Designator_BuildWallBauschutt.cs` | BOOT ✅ |
| Power | `PowerChainService.cs`, `FueledGeneratorService.cs` | BOOT ✅ |
| ArrowTurret | `ArrowTurretPowerGate.cs` | BOOT ✅ |
| Plants | `FoodHarvestCycleService.cs`, `PlantHelper.cs` | BOOT ✅ |
| Resources | `ResourceCategory.cs` | BOOT ✅ |
| UI | `InfrastructureDashboard.cs` | BOOT ✅ |
| Tests | 7 Suiten (ArrowTurretBlock, BauschuttRemapApply, BuildingCore, CampfireScraps, CaravanStorage, CoalChain, StainlessSteelChain) | BOOT ✅ |
| Defs | Coal, MachineParts, CraftingStations, MakeCoal, SalvageMachineParts, WoodCoalGenerator, Campfire | BOOT ✅ |
| Patches | 12 XML-Patches (Wall/Door/Barricade, Smithy, TableMachining, Royalty, Biotech, Ideology, Anomaly, Woody) | BOOT ✅ |

### §3.2 Was fehlt (OPEN)

| Geplant (BLUEPRINT) | Status | Blockiert durch |
|---|---|---|
| Wasser-Quelle/Lager/Verbrauch (I3) | OPEN | `API-POWER-01` |
| Elektrischer Hochofen (T2-Strom) | OPEN | I3 + Live-Tests |
| Live Save/Load für Coal-Chain | OPEN | User-Tests |
| Live-Test: Bauschutt → Wand/Tür → Nahrung → Power → Pfeilturm | OPEN | User-Tests (kompletter Loop) |

---

## §4 Roadmap (abgeleitet aus globaler ROADMAP.md)

| Schritt | Task | Status |
|---|---|---|
| I-01 | P0 Coal Chain: Coal, MachineParts, MakeCoal, SalvageMachineParts, WoodCoalGenerator | ✅ |
| I-02 | Bauschutt-System: BuildingSnapshot, BauschuttRemap, Designator | ✅ |
| I-03 | StorageQuery: SSOT für physische Ressourcen-Reads (05 liest via DLL-Ref) | ✅ |
| I-04 | Storage-Bridge: StorageHash an 05 über Capability | ✅ |
| I-05 | ArrowTurret: Strom als harte Bedingung, PowerGate | ✅ |
| I-06 | FoodHarvestCycle: Nahrung+Hanf getrennt | ✅ |
| I-07 | PowerChain: FueledGenerator, WoodLog/Coal-Input | ✅ |
| I-08 | Live-Tests: kompletter Loop, Save/Load | ⏸️ OPEN |

---

## §5 Änderungshistorie

| Datum | Änderung |
|---|---|
| 2026-08-06 | Initial. MOD_INDEX.md aus BLUEPRINT.md + INTERFACE_CONTRACT.md + Source-Inventar abgeleitet. |
