# MOD_INDEX.md — 05 · Rimconemy Infected & Automation

> **Stand:** 2026-08-06 · **Owner:** Infected & Automation
> **Abgeleitet von:** [ROADMAP.md](../../ROADMAP.md) + [INTERFACE_CONTRACT.md](../../docs/INTERFACE_CONTRACT.md) + [BLUEPRINT.md](BLUEPRINT.md)
> **Code-Status:** [CODE_STATUS.md](../../docs/CODE_STATUS.md)
> **Tatsächlicher Code:** `Source/` (75 .cs-Dateien), `Tests/` (23 Suiten)

---

## §1 Abhängigkeitsmatrix

### §1.1 Compile-Dependencies

| Von | Nach | Typ |
|---|---|---|
| 05 Infected | 01 Foundation | **DLL-Ref** |
| 05 Infected | 03 Scavenger | **DLL-Ref** (Phase B: `StorageQuery.ReadStorage`) |
| 05 Infected | RimWorld Assemblies | DLL-Ref |
| 05 Infected | Harmony | DLL-Ref |

**Keine** DLL-Ref auf 02 oder 04. Cross-Package-Kommunikation via `CrossPackageState` (01).

### §1.2 Runtime-Dependencies (Capability-Gate)

| Benötigte Capability | Owner | Fallback bei fehlend |
|---|---|---|
| `rimconemy.foundation.profile` | 01 | — (zwingend) |
| `rimconemy.foundation.colonials` | 01 | — |
| `rimconemy.foundation.dlc_filter` | 01 | — |
| `rimconemy.foundation.eventlog` | 01 | — |
| `rimconemy.survivalprogression.needs` | 02 | Need-Snapshot: leer |
| `rimconemy.survivalprogression.progression` | 02 | Progression-Read: leer |
| `rimconemy.survivalprogression.gameover` | 02 | **05 schreibt nur** `MarkGameOverPending` via Bridge |
| `rimconemy.scavengerinfrastructure.resources` | 03 | StorageHash = FNV-1a-Fallback |
| `rimconemy.economyterritory.wallet` | 04 | Wallet-Read via `CrossPackageState` Bridge |

### §1.3 DLC-Anker

| DLC | Nutzung in 05 | Fehlend-Fallback |
|---|---|---|
| Core | IncidentWorker, Storyteller, Pawn, Faction | unverzichtbar |
| Ideology | IdeologyAssigner, SettingRules, ThoughtWorker | Core-only: Vanilla-Precepts |
| Royalty | Empire-Tribute-Trader (Pragmatic-Meme) | Core-only |
| Biotech | Mechanitor-Meme, Gene-Mining | Core-only |
| Anomaly | Entity-Beute als Scraps, Odyssey-Ruins | Core-only: Vanilla-Ancient-Ruins |
| Odyssey | Gravship, WorldMap-Travel | Core-only: Vanilla-Caravan |

---

## §2 Standalone-Prüfung

### §2.1 Funktionen die Foundation bereitstellt → Consumer-Code NUTZT diese

| Funktion | Foundation-Pfad | Wo in 05 genutzt |
|---|---|---|
| `CapabilityAudit.HasCapabilityOrWarn` | `01/Source/Registry/` | `Bootstrap.cs`, `StoryDirector` |
| `PackageRegistry.IsRegistered` | `01/Source/Registry/` | `Bootstrap.cs` |
| `DLCFilter.IsContentEnabled` | `01/Source/DLC/` | `Bootstrap.cs`, `IdeologyAssigner` |
| `ColonialReader.GetActiveColonists()` | `01/Source/Colonials/` | `StoryDirector.BuildLiveSnapshot()` |
| `CrossPackageState` | `01/Source/CrossPackage/` | `StoryDirector` (GameOver-Pending, Wallet-Read) |
| `ISchemaMigratable` | `01/Source/Save/` | `StoryState`, `PopulationLedger` |
| `RimconemyUi.T()` | `01/Source/UI/` | `ThreatDashboard`, diverse UI |
| `RimconemyTheme` | `01/Source/UI/` | `ThreatDashboard`, diverse UI |
| `TimeConstants` | `01/Source/` | `StoryDirector`, `PopulationLedger` |
| `EventLog` | `01/Source/Events/` | `StoryDirector` (write) |

### §2.2 Funktionen die 05 via DLL-Ref von 03 nutzt (NICHT duplizieren)

| Funktion | 03-Pfad | Wo in 05 genutzt |
|---|---|---|
| `StorageQuery.ReadStorage()` | `03/Source/Storage/` | `StoryDirector.AssignStorageHashFromCapability()` |
| `StorageSnapshot.ContentHash` | `03/Source/Storage/` | `StoryDirector.BuildLiveSnapshot()` |

### §2.3 Funktionen die in 05 DUPLIZIERT sein müssen (Standalone-Nutzbarkeit)

| Funktion | Grund für Duplikat | Pfad in 05 |
|---|---|---|
| **StoryDirector** | Foundation hat keinen Story-Layer. 05 besitzt komplette Story-Engine | `Source/Story/StoryDirector.cs` |
| **ThreatAggregator** | Foundation hat kein Threat-System. 05 besitzt `ThreatAggregator`, `ThreatSnapshotBridge` | `Source/Threat/` |
| **PopulationLedger** | Foundation hat kein Population-Tracking. 05 besitzt `PopulationLedger`, `PopulationProfileMultipliers` | `Source/Population/` |
| **Horde-System** | Foundation hat kein Horde-System. 05 besitzt `HordeManifest`, `HordeSpawner`, `HordeMigrationDriver` usw. | `Source/Horde/` (14 Dateien) |
| **Infected-Spawn** | Foundation hat keinen Spawn-Pfad. 05 besitzt `InfectedRaidWorker`, `InfectedRaidSpawnService` | `Source/Incidents/` |
| **Tutorial** | Foundation hat kein Tutorial. 05 besitzt `TutorialDirector`, `TutorialState`, `Dialog_TutorialStep` | `Source/Story/` |
| **Ideology** | Foundation hat kein Ideology-System. 05 besitzt `IdeologyAssigner`, `SettingRulesCatalog`, ThoughtWorker | `Source/Ideology/` (10 Dateien) |
| **World** | Foundation hat kein World-Map-System. 05 besitzt `ChunkController`, `WorldRaidCoordinator`, `ColonistSightSystem` | `Source/World/` (14 Dateien) |
| **Inoculation** | Foundation hat kein Inoculation-System. 05 besitzt `RandomInoculationService`, `AnimalInfectionDriver` | `Source/Inoculation/` (7 Dateien) |
| **Mechadroids** | Foundation hat kein Mechadroid-System. 05 besitzt `MechadroidJobs` | `Source/Mechadroids/` |
| **DeterministicRng** | Foundation hat keinen FNV-1a-Hash. 05 besitzt eigenen RNG für Story-Determinism | `Source/Story/DeterministicRng.cs` |

### §2.4 Funktionen die Foundation BEREITS hat → 05-Duplikat wäre ein Fehler

| Foundation-Funktion | 05 DARF NICHT duplizieren | Grund |
|---|---|---|
| Profil-Erkennung | ❌ | `ProfileDetector` in 01 ist SSOT |
| DLC-Prüfung | ❌ | `DLCFilter` in 01 ist SSOT |
| Capability-Gates | ❌ | `CapabilityAudit` in 01 ist SSOT |
| Kolonisten-Zählung | ❌ | `ColonialReader` in 01 ist SSOT |
| UI-Base-Classes | ❌ | `RimconemyWindow` in 01 ist SSOT |
| UI-Helper | ❌ | `RimconemyUi.T()` in 01 ist SSOT |
| Save-Migration | ❌ | `ISchemaMigratable` in 01 ist SSOT |
| CrossPackageState | ❌ | `CrossPackageState` in 01 ist SSOT |
| StorageQuery | ❌ | 03 ist SSOT via DLL-Ref |

---

## §3 Tatsächlicher Code-Stand

### §3.1 Was existiert (CODE + COMPILES + BOOT)

| Modul | Dateien | Status |
|---|---|---|
| Bootstrap | `Bootstrap.cs` | BOOT ✅ |
| Story | `StoryDirector.cs`, `StoryState.cs`, `StorySelector.cs`, `StoryEventCatalog.cs`, `StoryEventDef.cs`, `StoryEventSpec.cs`, `SituationSnapshot.cs`, `SettingProfile.cs`, `EventFamilyMap.cs`, `PlaceholderResolver.cs` | BOOT ✅ |
| Tutorial | `TutorialDirector.cs`, `TutorialState.cs`, `TutorialStepDef.cs`, `Dialog_TutorialStep.cs`, `RimconemyTutorialLetter.cs` | BOOT ✅ |
| Threat | `ThreatAggregator.cs`, `ThreatSnapshotBridge.cs` | BOOT ✅ |
| Incidents | `InfectedRaidWorker.cs`, `InfectedRaidSpawnService.cs`, `IncidentClassifier.cs`, `DirectorAccessStub.cs` | BOOT ✅ |
| Population | `PopulationLedger.cs`, `PopulationProfileMultipliers.cs`, `PopulationLedgerReconciler.cs` | BOOT ✅ |
| Horde | `HordeManifest.cs`, `HordeSpawner.cs`, `HordeMigrationDriver.cs`, `HordeMaterializationService.cs`, `HordeCalculator.cs`, `HordeBurstLayer.cs`, `HordeCameraOverlay.cs`, `HordeSectionLayer.cs`, `HordeUpdateLogic.cs`, `HordeWorldObject.cs`, `HiddenPawnStamp.cs`, `TravelTileRecord.cs` | BOOT ✅ |
| World | `ChunkController.cs`, `ChunkGridComponent.cs`, `ChunkState.cs`, `ChunkAlertState.cs`, `WorldRaidCoordinator.cs`, `ColonistSightSystem.cs`, `DarknessSectionLayerLifecycle.cs`, `InfectedPawnState.cs`, `InfectedBehavior.cs`, `InfectedPackBehavior.cs`, `LightSystem.cs`, `NoiseSystem.cs`, `PerceptionMath.cs`, `SightConeMath.cs`, `EnvironmentSnapshot.cs` | BOOT ✅ |
| Inoculation | `RandomInoculationService.cs`, `AnimalInfectionDriver.cs`, `AnimalInfectionChance.cs`, `AnimalInfectionAiOverlay.cs`, `InoculationCandidate.cs`, `InoculationConverter.cs`, `InoculationSelectorLogic.cs` | BOOT ✅ |
| Ideology | `IdeologyAssigner.cs`, `SettingRulesCatalog.cs`, `CollectiveDefenseTracker.cs`, `CollectiveDefensePostCombatPatch.cs`, `TransparencyTracker.cs`, `ThoughtWorker_ResourceFairness.cs`, `ThoughtWorker_Transparency.cs`, `ThoughtDefs_*.cs` | BOOT ✅ |
| Mechadroids | `MechadroidJobs.cs` | BOOT ✅ |
| Building | `BuildingThreatAdapter.cs` | BOOT ✅ |
| Scenarios | `ScenPart_IntroSequence.cs`, `ScenPart_RimconemyStartEnemies.cs`, `ScenPart_StartingWoodPiles.cs`, `RimconemyStartEnemiesLedger.cs`, `InfectedFactionUtility.cs`, `TutorialTriggerBridge.cs` | BOOT ✅ |
| RNG | `DeterministicRng.cs` | BOOT ✅ |
| Resources | `ResourceThresholds.cs` | BOOT ✅ |
| UI | `ThreatDashboard.cs`, `SettingRulesInspector.cs` | BOOT ✅ |
| Tests | 23 Suiten (ALLES 0/0 nach Fix in 487a5c0) | BOOT ✅ |

### §3.2 Was fehlt (OPEN)

| Geplant (BLUEPRINT) | Status | Blockiert durch |
|---|---|---|
| Echter Infizierten-Spawn (nicht nur Letter) | PARTIAL | Live-Tests |
| Vollständige World-Map-Raids | CODE ✅ | Live-Tests |
| Mechadroid-Job-Ausführung | CODE ✅ | Live-Tests |
| AnimalInfection Live-Integration | CODE ✅ | Live-Tests |
| Horde-Materialization (Tile-basiert) | CODE ✅ | Live-Tests |
| IdeologyAssigner Auto-Assignment | PARTIAL | `ModsConfig.IdeologyActive` API |
| ThoughtWorker → von Mod 02 übernehmen (S-T4) | DECIDED, nicht migriert | Mod 02 muss abgeben |
| Auto-Resolve + Manueller Raid | CODE ✅ | Live-Tests |

---

## §4 Roadmap (abgeleitet aus globaler ROADMAP.md)

| Schritt | Task | Status |
|---|---|---|
| A-01 | StoryDirector + StorySelector + StoryState | ✅ |
| A-02 | ThreatAggregator + ThreatSnapshotBridge | ✅ |
| A-03 | InfectedRaidWorker + InfectedRaidSpawnService | ✅ |
| A-04 | PopulationLedger + PopulationProfileMultipliers | ✅ |
| A-05 | Horde-System (14 Dateien: Manifest, Spawner, Migration, Materialization) | ✅ |
| A-06 | World-System (15 Dateien: Chunk, Sight, Darkness, Light, Noise, Perception) | ✅ |
| A-07 | Inoculation-System (7 Dateien: RandomInoculation, AnimalInfectionDriver) | ✅ |
| A-08 | Ideology-System (10 Dateien: Assigner, Rules, ThoughtWorker, CollectiveDefense) | ✅ |
| A-09 | TutorialDirector + TutorialState + Dialog_TutorialStep | ✅ |
| A-10 | DeterministicRng (FNV-1a) | ✅ |
| A-11 | MechadroidJobs | ✅ |
| A-12 | **Alle 23 Test-Suiten: 0 Failures** (Fix in 487a5c0) | ✅ |
| A-13 | Live-Tests: Spawn, Raid, Horde, Tier, Save/Load | ⏸️ OPEN |

---

## §5 Änderungshistorie

| Datum | Änderung |
|---|---|
| 2026-08-06 | Initial. MOD_INDEX.md aus BLUEPRINT.md + INTERFACE_CONTRACT.md + Source-Inventar abgeleitet. |
