# Architecture Boundaries Sprint

> **Owner:** Foundation (01)
> **Stand:** 2026-08-04
> **Status:** DRAFT → EXECUTING
> **Spec-Origin:** Audit vom 2026-08-04 (5 Cross-Package-Konflikte verifiziert)
> **Sprint-Scope:** Alle 5 Fixes in empfohlener Reihenfolge + Capability-Mock-Test-Suite

## Ziel

Die 5 im Audit vom 2026-08-04 verifizierten Konflikt-Punkte strukturell schließen, ohne neue Funktionalität zu schaffen. Das primäre Werkzeug ist das bereits implementierte aber ungenutzte `PackageRegistry.HasCapability(capability, minVersion)`.

## Owner-Map (Capability → Domain)

| Capability | Owner-Paket | Eigentum | Leser-Pakete |
|---|---|---|---|
| `rimconemy.foundation.profile` | 01 | ProfileDetector-Output (Standalone/Partial/Full) | alle |
| `rimconemy.foundation.eventlog` | 01 | Append-Only-EventLog | 02, 05 |
| `rimconemy.foundation.save_diagnosis` | 01 | Save-Schema-Diagnose | UI |
| `rimconemy.foundation.colonials` | 01 | **(NEU)** `ColonialReader.GetActiveColonists()` — unified Filter | 02, 03, 05 |
| `rimconemy.survivalprogression.progression` | 02 | XP/Progression/SkillBudget-Logik | 04, 05 |
| `rimconemy.survivalprogression.gameover` | 02 | **(CLARIFY)** Sole-Owner für `Find.GameEnder?.CheckOrUpdateGameOver()` | 05 (write-only pending) |
| `rimconemy.scavengerinfrastructure.resources` | 03 | StorageSnapshot.ContentHash | 04, 05 |
| `rimconemy.scavengerinfrastructure.power` | 03 | Power-Chain | 02 (XP-Domain: Power), 04 |
| `rimconemy.economyterritory.wallet` | 04 | CreditsLedger | 02 (Reward), 05 (Bribe-impact) |
| `rimconemy.infectedautomation.threat` | 05 | ThreatAggregator | 02 (XP-Multiplier), 03 (Power-Drain) |
| `rimconemy.infectedautomation.automation` | 05 | Ideology/Gamemode/Stories | 02 (ThoughtWorker gate) |

**Regel:** Wer **schreibt**, ist **Owner**. Alle anderen Pakete sind **Reader-only** und **müssen `HasCapability` vor jedem Cross-Package-Read prüfen**.

## Die 5 Fixes

### F-V4 Capability-Aktivierung (Reihenfolge: als erstes)

**Problem:** `HasCapability` existiert seit 2026-07, aber kein Cross-Package-Reader nutzt es aktiv.

**Lösung:** Audit-Helper-Pattern.

```csharp
// Foundation/CapabilityAudit.cs (neu)
public static class CapabilityAudit
{
    /// <summary>
    /// Returns true if capability exists AND minimum version met.
    /// Logs a one-time warning per (capability, version) tuple when false,
    /// so silent feature-loss is observable in the log.
    /// </summary>
    public static bool HasCapabilityOrWarn(string capabilityId, int minVersion = 1, string readerContext = null)
    {
        if (PackageRegistry.HasCapability(capabilityId, minVersion))
            return true;
        // Log once per (capability, missingVersion)
        LogOnce(capabilityId, minVersion, readerContext);
        return false;
    }
}
```

**Anwendung:**
- Mod 02 `ProgressionGameComponent` — vor Read von Threat (05): `CapabilityAudit.HasCapabilityOrWarn("rimconemy.infectedautomation.threat",1,"XP-Multiplier")`
- Mod 05 `StoryDirector` — vor StorageRead (03): `CapabilityAudit.HasCapabilityOrWarn("rimconemy.scavengerinfrastructure.resources",1,"StorageHash")`
- Mod 02 `ThoughtWorker_ResourceFairness` — vor Ideology-Context: `CapabilityAudit.HasCapabilityOrWarn("rimconemy.infectedautomation.automation",1,"ResourceFairness-Ideo-Gate")`

### F-V2 GameOver-Ownership

**Problem:** Mod 02 triggert `Find.GameEnder?.CheckOrUpdateGameOver()`. Mod 05 hat `SituationSnapshot.GameOverPending` (deklariert, nie gesetzt).

**Lösung:**
- Mod 02 = sole-Owner für `CheckOrUpdateGameOver()`. Schreibt den `GameOverReason` als String in `ProgressionGameComponent.GameOverReason`.
- Mod 05 darf *nicht* `CheckOrUpdateGameOver()` aufrufen. Statt dessen: `StoryState.MarkGameOverPending(string reason)`.
- Mod 02 liest in der nächsten Tick-Iteration `storyDirector?.State?.GameOverPending` (capability-gated!), mapped auf `GameOverReason` und triggert.

**Implementation:**
1. Neuer `mods/05-.../Source/Story/StoryState.cs`: `bool GameOverPending {get; private set;} string GameOverReasonPending {get; private set;} void MarkGameOverPending(string reason)`.
2. `mods/05-.../Source/Story/StoryDirector.cs`: bei Raid-Wipe-Detection `State.MarkGameOverPending(reason)`. (Hier: in `InfectedRaidWorker.PostProcess` oder als GameComponent-Tick-Check auf pawn-Count.)
3. `mods/02-.../Source/Progression/ProgressionGameComponent.cs`: 
   - Capability-gate: `bool storyGameOver = CapabilityAudit.HasCapabilityOrWarn(...,"rimconemy.infectedautomation.automation",1,"GameOver-Read") && StoryStateReader.TryReadGameOverPending(out var reason)`
   - Race-frei durch reihenfolge-check: erst lokale Prüfung, dann Read, dann triggern.

### F-V1 ColonialReader zentralisieren

**Problem:** Pawn-Enumeration in 5+ Stellen mit unterschiedlicher Filterlogik.

**Lösung:**
- NEU: `mods/01-.../Source/Colonials/ColonialReader.cs`:
  ```csharp
  public static class ColonialReader
  {
      /// <summary>Single source of truth for "active player colonists".
      /// Filter: not null, IsColonist, !Dead, !DestroyedOrNull, deduplicated by thingIDNumber.
      /// Returns empty list in main menu (Find.Maps empty). Never throws.</summary>
      public static List<Pawn> GetActiveColonists() { ... }
      
      public static int ActiveColonistCount { get { ... } }
      
      public static float AverageHealthPercent { get { ... } }  // convenience for StoryDirector
  }
  ```
- Mods 02, 03, 05 rufen `ColonialReader.GetActiveColonists()` statt lokaler Enumeration.
- Foundation-DLL-Ref für Mod 02 → bereits vorhanden (Phase 0-A §8.6).
- Foundation-DLL-Ref für Mod 03 + 05 → werden analog hinzugefügt.

### F-V3 Storage-Bridge aktivieren

**Problem:** StoryDirector ignoriert Mod-03-ContentHash; baut `"live-" + tick`.

**Lösung:**
- `mods/05-.../Source/Snapshot/SnapshotPipeline.cs` (NEU): kapselt Snapshot-Building.
- Capability-gate prüft `rimconemy.scavengerinfrastructure.resources`.
- Wenn `false` → wie bisher live-tick-Hash (MVP).
- Wenn `true` → `var storage = StorageQuery.ReadSnapshot(...); snapshot.StorageHash = storage.ContentHash;`
- INTERFACE_CONTRACT §3 von „Phase 3 Ziel" auf „Phase 1.5 verfügbar (capability-gated)" updaten.

### F-V5 Ideology-Grenze

**Problem:** Mod 02 hat Setting-Rules die zur Ideology-Domäne gehören.

**Lösung:**
- `mods/02-.../Source/Ideology/ThoughtWorker_ResourceFairness.cs`: `ThoughtState.Active` nur wenn `CapabilityAudit.HasCapabilityOrWarn(...)`.
- Mod 05 bleibt Ideology-Architekt: 3 `Rimconemy_Ideo_*.xml` + `IdeologyAssigner` + `EventFamilyMap`-Ideo-Tags.
- Mod 02 = Need-Feeder (was er schon ist). Ideology-Definitionen werden zu 100% Mod 05.

## Capability-Mock-Tests

**Ziel:** Sicherstellen dass Capability-Gates greifen, ohne dass RimWorld-Runtime nötig ist.

**Approach:**
- In-Memory-Registry-Mock: `MockPackageRegistry` mit `SetCapability(id, version)` / `RemoveCapability(id)` / `IsActive`.
- Tests in 01-Foundation unter `mods/01-.../Tests/Foundation.CapabilityGateTests.cs` (oder als Self-Contained `StorySelectorTests`-Style).
- Pro Capability-Gate-Stelle ein Test:
  - `ProgressionGameComponent_GameOverRead_GatedByInfectedAutomation`
  - `StoryDirector_StorageHashRead_GatedByScavenger`
  - `ThoughtWorker_ResourceFairness_GatedByInfectedAutomation`
  - `ColonialReader_ActiveColonists_Pure`
  - `SnapshotPipeline_FallsBackToLiveTick_WhenCapabilityMissing`
- Pro Mock-Registry: zwei Modi (Standalone/Partial/Full), 6 Capability-Setups × 4 Reads = ~24 Tests.

## Risiken

| R-Nr | Risiko | Mitigation |
|---|---|---|
| R1 | DLL-Ref Pattern für Mod 03 + 05 → Foundation | bereits in Phase 0-A für Mod 02 etabliert; Re-Apply für 03/05 |
| R2 | `GameOverPending` Migration → Save-Schema-Compat | neue Felder in `StoryState` default-init, kein breaking-change |
| R3 | ColonialReader-Break in PowerChain (aufrufer erwartet mehr Felder) | liefert nur active pawns; Per-Caller-Properties über separate Helper |
| R4 | Capability-Tests benötigen Reflection auf interne Klassen | Test-Klasse im selben Assembly wie Implementierung → kein Ref-Problem |
| R5 | Sentinel-Pattern `MarkGameOverPending` wird vergessen | Code-Reviewer-Spawn nach F-V2 implementation |

## Akzeptanzkriterien

1. **Build:** Alle 5 Pakete 0W/0E.
2. **Capability-Gates:** 100% Coverage der Cross-Package-Reads — kein direkter Cross-Package-Aufruf ohne `CapabilityAudit.HasCapabilityOrWarn` davor.
3. **Mod-02-only GameOver:** grep nach `CheckOrUpdateGameOver` zeigt genau **1 Treffer** in Mod 02.
4. **Mod-05-only State.GameOverPending** write: genau **1 set-callsite** in Mod 05.
5. **ColonialReader:** `grep -r "FreeColonistsSpawned\." mods/*/Source/` zeigt nur `ColonialReader.cs` (Außer dem Interface selbst).
6. **Storage-Bridge:** wenn Mod 03 aktiv, hat StoryDirector-Output anderer StorageHash als `"live-..."`.
7. **Mock-Tests:** 24+ Capability-Mock-Tests grün, dokumentieren alle 5 Audit-Befunde geschlossen.

## Was offen bleibt (Phase B+)

- Track A-Phase 2 (Bio-Remap), A-3 (TraitPool-Erweiterung) — separate Spec
- Track B (Difficulty-Scaling) — separate Spec
- Track C (Loot-Hunt) — separate Spec
- ECONOMY/MARKET-STUB-Implementation — Phase 2.0

## Reihenfolge

1. **F-V4** Capability-Audit-Helper + erstes Gate in `ProgressionGameComponent` (Schalter)
2. **F-V2** GameOver-Ownership (verwendet F-V4)
3. **F-V1** ColonialReader (pure logic, kein Cross-Package-Read nötig)
4. **F-V3** Snapshot-Pipeline (verwendet F-V4)
5. **F-V5** Ideology-Grenze (verwendet F-V4)
6. **Capability-Mock-Tests** über alle 4 Gates
7. Documentation-Update (INTERFACE_CONTRACT §3, §10)
8. Build-all + Version-Bump + Code-Review
