# MOD_INDEX.md — 01 · Rimconemy Foundation

> **Stand:** 2026-08-06 · **Owner:** Foundation
> **Abgeleitet von:** [ROADMAP.md](../../ROADMAP.md) + [INTERFACE_CONTRACT.md](../../docs/INTERFACE_CONTRACT.md) + [BLUEPRINT.md](BLUEPRINT.md)
> **Code-Status:** [CODE_STATUS.md](../../docs/CODE_STATUS.md)
> **Tatsächlicher Code:** `Source/` (46 .cs-Dateien), `Defs/`, `Tests/` (8 Suiten)

---

## §1 Abhängigkeitsmatrix

### §1.1 Compile-Dependencies
Foundation hat **keine** DLL-Referenzen auf andere Rimconemy-Pakete. Nur RimWorld-Assemblies + Harmony.

### §1.2 Runtime — Wer hängt von Foundation ab?

| Consumer | Typ | Genutzte Capabilities |
|---|---|---|
| **02 Survival** | DLL-Ref | `profile`, `eventlog`, `colonials`, `save_diagnosis` |
| **03 Scavenger** | DLL-Ref | `profile`, `colonials`, `dlc_filter` |
| **04 Economy** | Reflection (late-bound) | `profile`, `save_diagnosis` |
| **05 Infected** | DLL-Ref | `profile`, `colonials`, `dlc_filter`, `eventlog` |

### §1.3 DLC-Anker

| DLC | Foundation-Nutzung | Fehlend-Fallback |
|---|---|---|
| Core | Profil-Erkennung, UI | unverzichtbar |
| Royalty | Empire-Detection via ModsConfig | Partial-Profil |
| Ideology | Ideo-Detection via ModsConfig | Partial-Profil |
| Biotech | Gene/Mechanitor-Detection | Partial-Profil |
| Anomaly | Entity-Detection | Partial-Profil |
| Odyssey | Gravship/Orbit-Detection | Partial-Profil |

---

## §2 Standalone-Prüfung

### §2.1 Funktionen die Foundation bereitstellt — Consumer NUTZEN diese, NICHT duplizieren

| Funktion | Pfad | Consumer | Duplikat-Verbot |
|---|---|---|---|
| `PackageRegistry` | `Source/Registry/PackageRegistry.cs` | 02,03,04,05 | ❌ Kein eigenes Registry-System |
| `CapabilityAudit.HasCapabilityOrWarn` | `Source/Registry/CapabilityAudit.cs` | 02,03,04,05 | ❌ Kein eigenes Capability-Check-System |
| `ColonialReader.GetActiveColonists()` | `Source/Colonials/ColonialReader.cs` | 02,03,05 | ❌ Keine eigene Kolonisten-Zählung |
| `DLCFilter.IsContentEnabled()` | `Source/DLC/DLCFilter.cs` | alle | ❌ Keine eigene DLC-Prüfung |
| `ProfileDetector` | `Source/Profile/ProfileDetector.cs` | alle (read-only) | ❌ Kein eigenes Profil-System |
| `FoundationSaveData` | `Source/Save/FoundationSaveData.cs` | 02 (liest) | ❌ Kein eigenes Save-Metadaten-System |
| `ISchemaMigratable` | `Source/Save/ISchemaMigratable.cs` | 02,04,05 implementieren | ✅ MUSS implementiert werden (Interface) |
| `RimconemyWindow` / `RimconemyMainTabWindow` | `Source/UI/` | 02,05 | ❌ Keine eigenen Window-Base-Classes |
| `RimconemyUi.T()` / `DrawSectionTitle` etc. | `Source/UI/RimconemyUi.cs` | 02,05 | ❌ Keine eigenen UI-Helper |
| `RimconemyTheme` | `Source/UI/RimconemyTheme.cs` | 02,05 | ❌ Keine eigenen Theme-Konstanten |
| `CrossPackageState` | `Source/CrossPackage/CrossPackageState.cs` | 05 | ❌ Keine direkten Peer-Refs |
| `EventLog` | `Source/Events/EventLog.cs` | 02,05 (write) | ❌ Kein eigenes Event-Log |
| `TimeConstants` | `Source/TimeConstants.cs` | 02,05 | ❌ Keine eigenen Tick-Konstanten |

### §2.2 Funktionen die NUR in Foundation existieren DÜRFEN (Sole-Owner)

| Funktion | Begründung |
|---|---|
| Profil-Erkennung | Genau eine Quelle für `FullOverhaul`/`Partial`/`Standalone` |
| Paket-Registry | Genau eine Quelle für `IsRegistered(packageId)` |
| DLC-Filter | Zentrale Policy: kein Paket macht eigene DLC-Entscheidungen |
| ColonialReader | Single Source of Truth für `GetActiveColonists()` |
| CapabilityAudit | Zentraler Gate für alle Cross-Package-Reads |
| CrossPackageState | Reflection-Bridge: kein Paket hat direkten Peer-Compile-Ref |

---

## §3 Tatsächlicher Code-Stand

### §3.1 Was existiert (CODE + COMPILES + BOOT)

| Modul | Dateien | Status |
|---|---|---|
| Bootstrap | `Bootstrap.cs`, `FoundationInitializer.cs` | BOOT ✅ |
| Registry | `PackageRegistry.cs`, `CapabilityAudit.cs`, `ITutorialTriggerBridge.cs` | BOOT ✅ |
| Profile | `ProfileDetector.cs`, `RuntimeMeter.cs` | BOOT ✅ |
| DLC | `DLCContentPolicy.cs`, `DLCContentPolicyDef.cs`, `DLCFilter.cs`, `DLCPolicyComponent.cs`, `DLCPolicyConfig.cs` | BOOT ✅ |
| Save | `FoundationSaveData.cs`, `ISchemaMigratable.cs`, `MigrationRegistry.cs`, `MigrationStepWalker.cs`, `SchemaMigratableExtensions.cs`, `SchemaStep.cs` | BOOT ✅ |
| Colonials | `ColonialReader.cs` | BOOT ✅ |
| CrossPackage | `CrossPackageState.cs` | BOOT ✅ |
| Events | `EventLog.cs`, `EventRecord.cs` | BOOT ✅ |
| UI Toolkit | `RimconemyWindow.cs`, `RimconemyMainTabWindow.cs`, `RimconemyInspectTab.cs`, `RimconemyTheme.cs`, `RimconemyUi.cs`, `ThemeSettings.cs`, `GlobalThemeOverride.cs` | BOOT ✅ |
| Dashboard | `FoundationDashboard.cs` | BOOT ✅ |
| RimPad | `RimPadWindow.cs`, `RimPadTab.cs`, `RimPadTabDrawer.cs`, `RimPadTheme.cs` | BOOT ✅ |
| Intro | `IntroFlowWindow.cs` | BOOT ✅ |
| Maps | `MapRegistry.cs` | BOOT ✅ |
| Canonical | `MaterialIdentity.cs`, `RoomRoleResolver.cs`, `SettingIdentity.cs` | BOOT ✅ |
| Catalog | `FoundationDefInventory.cs`, `FoundationVanillaInventory.cs`, `StorytellerInventory.cs` | BOOT ✅ |
| Models | `PackageDescriptor.cs`, `PackageSnapshot.cs`, `ProfileStatus.cs` | BOOT ✅ |
| Time | `TimeConstants.cs` | BOOT ✅ |
| Tests | 8 Suiten (CapabilityGate, ColonialReader, CrossPackageState, EventLog, Profile, TimeConstants, WindowFallback, HonestBannerAudit) | BOOT ✅ |

### §3.2 Was fehlt (OPEN)

| Geplant (BLUEPRINT) | Status | Blockiert durch |
|---|---|---|
| `API-RESOURCE-01` (Ressourcen-Read-Model) | OPEN | Kein Live-Verbraucher |
| FoundationServiceBus (Phase 3) | ENTFERNT | Kein Konsument; Code gelöscht in Phase-B-Sprint |
| Mod-Settings-Panel für ThemeSettings | OPEN | UX-Entscheidung ausstehend |

---

## §4 Roadmap (abgeleitet aus globaler ROADMAP.md)

| Schritt | Task | Status |
|---|---|---|
| F-01 | Foundation ist stabil, alle Consumer nutzen Capability-Gates | ✅ |
| F-02 | ColonialReader als SSOT in 02/03/05 migriert | ✅ |
| F-03 | CrossPackageState als Reflection-Bridge (GameOver, Wallet) | ✅ |
| F-04 | DLCFilter zentral; keine eigene DLC-Prüfung in 02–05 | ✅ |
| F-05 | ServiceBus: erst wenn erster Consumer benannt → derzeit ENTFERNT | ⏸️ |

---

## §5 Änderungshistorie

| Datum | Änderung |
|---|---|
| 2026-08-06 | Initial. MOD_INDEX.md aus BLUEPRINT.md + INTERFACE_CONTRACT.md + Source-Inventar abgeleitet. |
