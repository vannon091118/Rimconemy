# MOD_INDEX.md — 02 · Rimconemy Survival & Progression

> **Stand:** 2026-08-06 · **Owner:** Survival & Progression
> **Abgeleitet von:** [ROADMAP.md](../../ROADMAP.md) + [INTERFACE_CONTRACT.md](../../docs/INTERFACE_CONTRACT.md) + [BLUEPRINT.md](BLUEPRINT.md)
> **Code-Status:** [CODE_STATUS.md](../../docs/CODE_STATUS.md)
> **Tatsächlicher Code:** `Source/` (50 .cs-Dateien), `Tests/` (7 Suiten)

---

## §1 Abhängigkeitsmatrix

### §1.1 Compile-Dependencies

| Von | Nach | Typ |
|---|---|---|
| 02 Survival | 01 Foundation | **DLL-Ref** (kein ProjectReference) |
| 02 Survival | RimWorld Assemblies | DLL-Ref |
| 02 Survival | Harmony | DLL-Ref |

### §1.2 Runtime-Dependencies (Capability-Gate)

| Benötigte Capability | Owner | Fallback bei fehlend |
|---|---|---|
| `rimconemy.foundation.profile` | 01 | — (zwingend, via DLL-Ref) |
| `rimconemy.foundation.colonials` | 01 | — (zwingend) |
| `rimconemy.foundation.eventlog` | 01 | — |
| `rimconemy.foundation.save_diagnosis` | 01 | — |
| `rimconemy.scavengerinfrastructure.resources` | 03 | StorageHash = `"unavailable"` |
| `rimconemy.scavengerinfrastructure.power` | 03 | Power-Read: 0 (kein XP-Malus) |
| `rimconemy.economyterritory.wallet` | 04 | Wallet-Read: 0 Credits |
| `rimconemy.economyterritory.market` | 04 | Market-Read: leer |
| `rimconemy.infectedautomation.threat` | 05 | Threat-Faktor: 0 (kein XP-Multiplier) |
| `rimconemy.infectedautomation.automation` | 05 | — |

### §1.3 DLC-Anker

| DLC | Nutzung in 02 | Fehlend-Fallback |
|---|---|---|
| Core | Szenario, Needs, Skills, Research | unverzichtbar |
| Ideology | ThoughtWorker (geplant: → Mod 05) | Core-only: Vanilla-Precepts |
| Biotech | Gene-Adapters für Needs | Core-only |
| Anomaly | — | — |
| Royalty | — | — |
| Odyssey | — | — |

---

## §2 Standalone-Prüfung

### §2.1 Funktionen die Foundation bereitstellt → Consumer-Code NUTZT diese

| Funktion | Foundation-Pfad | Wo in 02 genutzt |
|---|---|---|
| `ColonialReader.GetActiveColonists()` | `01/Source/Colonials/` | `ProgressionGameComponent.UpdateRuntimeState()` |
| `CapabilityAudit.HasCapabilityOrWarn` | `01/Source/Registry/` | `Bootstrap.cs`, `ProgressionGameComponent` |
| `PackageRegistry.IsRegistered` | `01/Source/Registry/` | `Bootstrap.cs` |
| `ISchemaMigratable` | `01/Source/Save/` | `CharacterSetupState`, `ProgressionGameComponent` |
| `RimconemyWindow` / `RimconemyMainTabWindow` | `01/Source/UI/` | `PhaseProgressWindow`, `SkillBudgetWindow` |
| `RimconemyUi.T()` / `DrawSectionTitle` | `01/Source/UI/` | `PhaseProgressWindow` |
| `RimconemyTheme` | `01/Source/UI/` | `SurvivalProgressionDashboard` |
| `DLCFilter.IsContentEnabled` | `01/Source/DLC/` | `ProgressionGameComponent` |
| `CrossPackageState` | `01/Source/CrossPackage/` | GameOver-Bridge (liest `MarkGameOverPending` von 05) |
| `TimeConstants` | `01/Source/` | `ProgressionGameComponent` (Tick-Intervalle) |

### §2.2 Funktionen die in 02 DUPLIZIERT sein müssen (Standalone-Nutzbarkeit)

| Funktion | Grund für Duplikat | Pfad in 02 |
|---|---|---|
| **Needs** | Foundation kennt keine Needs. 02 besitzt `NeedMapping`, `Need_SettingIdentity` als eigene Domäne | `Source/Needs/` |
| **Progression/XP** | Foundation hat kein XP-System. 02 besitzt `DomainXpState`, `ProgressionGameComponent` | `Source/Progression/` |
| **Character-Setup** | Foundation hat kein Charakter-System. 02 besitzt `CharacterSetup`, `SkillBudgetCalculator` | `Source/Character/` |
| **GameOver** | Foundation hat keinen GameOver-Detektor. 02 ist **Sole-Owner** von `CheckOrUpdateGameOver()` | `Source/GameOver/` |
| **Phase-Progress** | Foundation hat kein Phasen-System. 02 besitzt `PhaseProgressResolver`, `PhaseProgressWindow` | `Source/Phase/` |
| **Bundled Skills** | Reine 02-Domäne: `BundledSkillAllocation` | `Source/Character/` |

### §2.3 Funktionen die Foundation BEREITS hat → 02-Duplikat wäre ein Fehler

| Foundation-Funktion | 02 DARF NICHT duplizieren | Grund |
|---|---|---|
| Profil-Erkennung | ❌ | `ProfileDetector` in 01 ist SSOT |
| DLC-Prüfung | ❌ | `DLCFilter` in 01 ist SSOT |
| Kolonisten-Zählung | ❌ | `ColonialReader` in 01 ist SSOT |
| UI-Base-Classes | ❌ | `RimconemyWindow` in 01 ist SSOT |
| UI-Helper (`T()`, `DrawSectionTitle`) | ❌ | `RimconemyUi` in 01 ist SSOT |
| Save-Migration-Framework | ❌ | `ISchemaMigratable` in 01 ist SSOT |

---

## §3 Tatsächlicher Code-Stand

### §3.1 Was existiert (CODE + COMPILES + BOOT)

| Modul | Dateien | Status |
|---|---|---|
| Bootstrap | `Bootstrap.cs` | BOOT ✅ |
| Character | `CharacterSetup.cs`, `CharacterSetupState.cs`, `SkillBudgetCalculator.cs`, `SkillBudgetWindow.cs`, `TraitAssigner.cs`, `RoleSkillCatalog.cs`, `RoleSkillResolver.cs` | BOOT ✅ |
| Needs | `NeedMapping.cs`, `NeedAmplifier.cs`, `Need_SettingIdentity.cs`, `Hediff_NeedAmplifier.cs`, `SurvivalNeedCategory.cs` | BOOT ✅ |
| Progression | `ProgressionGameComponent.cs`, `ProgressionSnapshot.cs`, `ProgressionDomain.cs`, `DomainXpState.cs`, `ProgressionActionResult.cs`, `BuildingProgressionAdapter.cs` | BOOT ✅ |
| Unlocks | `UnlockService.cs`, `RimconemyUnlockExtension.cs` | BOOT ✅ |
| Hooks | `BuildingCompletionBridge.cs`, `FrameCompletionPatch.cs` | BOOT ✅ |
| GameOver | `GameOverDetector.cs`, `GameOverMode.cs` | BOOT ✅ |
| Scenarios | `RimconemyStartState.cs`, `ScenPart_RimconemyStart.cs`, `ScenPart_StartInSandbox.cs`, `SingleSurvivorScenario.cs` | BOOT ✅ |
| Mining | `MiningGateExt.cs`, `MiningHookPatch.cs` | BOOT ✅ |
| Phase | `PhaseProgressResolver.cs`, `PhaseProgressWindow.cs`, `PhaseContractGate.cs` | BOOT ✅ |
| Survival | `CampfireManager.cs`, `WallBuilder.cs`, `ResourceCollector.cs` | BOOT ✅ |
| Construction | `BuilderDurability.cs`, `ConstructionSpeed_StatPart.cs` | BOOT ✅ |
| Cooking | `CookingEffects.cs`, `PawnCookingTraitPatch.cs` | BOOT ✅ |
| Tools | `CompAxeDurability.cs`, `AxeDurabilityPatch.cs` | BOOT ✅ |
| Farming | `PlantSkillComp.cs` | BOOT ✅ |
| UI | `ProgressionPawnTab.cs`, `SurvivalProgressionDashboard.cs` | BOOT ✅ |
| Bridge | `SurvivalIntegration.cs`, `SurvivalTutorialBridge.cs` | BOOT ✅ |
| Patches | `Page_ConfigureStartingPawnsBioPatch.cs`, `Designator_PlantsCut_TreeCuttingGate.cs` | BOOT ✅ |
| Intellectual | `IntellectualLearning_StatPart.cs` | BOOT ✅ |
| Bundled | `BundledSkillAllocation.cs` | BOOT ✅ |
| Tests | 7 Suiten (BioRemap, NeedMapping, DomainXpState, UnlockService, BuildingCompletionBridge, SchemaBump, ScenarioContract) | BOOT ✅ |

### §3.2 Was fehlt (OPEN)

| Geplant (BLUEPRINT) | Status | Blockiert durch |
|---|---|---|
| Vollständige Need-Live-Integration (P2) | OPEN | `API-NEED-01` Spike |
| Job-/Output-XP-Commit (P3) | OPEN | `API-JOB-01` Spike |
| Experience-/Unlock-Gameplay (P4) | OPEN | P3-Abschluss |
| ThoughtWorker → Mod 05 migrieren (S-T4) | DECIDED, nicht migriert | Mod 05 muss ThoughtWorker aufnehmen |
| Live Save/Load-Roundtrip | OPEN | User-Tests |

---

## §4 Roadmap (abgeleitet aus globaler ROADMAP.md)

| Schritt | Task | Status |
|---|---|---|
| S-01 | Character-Setup + BioRemap via Harmony | ✅ |
| S-02 | NeedMapping + SettingIdentity (Hybrid: eigene Defs, Vanilla-Needs liefern Daten) | ✅ |
| S-03 | ProgressionGameComponent + DomainXpState | ✅ |
| S-04 | GameOverDetector (Sole-Owner) + CrossPackageState-Bridge | ✅ |
| S-05 | PhaseProgressResolver + Window (via Foundation-UI-Toolkit) | ✅ |
| S-06 | BundledSkillAllocation (Artistic+Construction → ein Skill) | ✅ |
| S-07 | Mining-Gate (Phase-Contract: Mining erst ab Production-Phase) | ✅ |
| S-08 | SkillBudgetWindow (Charakter-Roll-Fenster) | ✅ |
| S-09 | Live-Tests: Need-Integration, XP-Commit, Save/Load | ⏸️ OPEN |

---

## §5 Änderungshistorie

| Datum | Änderung |
|---|---|
| 2026-08-06 | Initial. MOD_INDEX.md aus BLUEPRINT.md + INTERFACE_CONTRACT.md + Source-Inventar abgeleitet. |
