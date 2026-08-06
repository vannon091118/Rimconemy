# MOD_INDEX.md — 04 · Rimconemy Economy & Territory

> **Stand:** 2026-08-06 · **Owner:** Economy & Territory
> **Abgeleitet von:** [ROADMAP.md](../../ROADMAP.md) + [INTERFACE_CONTRACT.md](../../docs/INTERFACE_CONTRACT.md) + [BLUEPRINT.md](BLUEPRINT.md)
> **Code-Status:** [CODE_STATUS.md](../../docs/CODE_STATUS.md)
> **Tatsächlicher Code:** `Source/` (9 .cs-Dateien), `Tests/` (3 Suiten)

---

## §1 Abhängigkeitsmatrix

### §1.1 Compile-Dependencies

| Von | Nach | Typ |
|---|---|---|
| 04 Economy | 01 Foundation | **DLL-Ref** |
| 04 Economy | RimWorld Assemblies | DLL-Ref |
| 04 Economy | Harmony | DLL-Ref |

**Hinweis:** 04 hat **keine** DLL-Ref auf 02, 03 oder 05. Alle Cross-Package-Reads gehen via Reflection-Bridge (`CrossPackageState` in 01) oder Capability-Gate.

### §1.2 Runtime-Dependencies (Capability-Gate)

| Benötigte Capability | Owner | Fallback bei fehlend |
|---|---|---|
| `rimconemy.foundation.profile` | 01 | — |
| `rimconemy.foundation.save_diagnosis` | 01 | — |
| `rimconemy.survivalprogression.progression` | 02 | Progression-Read: leer |
| `rimconemy.scavengerinfrastructure.resources` | 03 | Physische Waren: Vanilla-Things |

### §1.3 DLC-Anker

| DLC | Nutzung in 04 | Fehlend-Fallback |
|---|---|---|
| Core | TradeSession, WorldObjects, Caravan | unverzichtbar |
| Royalty | Empire-Tribute, Titel-Permissions | Core-only: Vanilla-Trader |
| Ideology | — | — |
| Biotech | — | — |
| Anomaly | — | — |
| Odyssey | Gravship-Transport | Core-only: Vanilla-Caravan |

---

## §2 Standalone-Prüfung

### §2.1 Funktionen die Foundation bereitstellt → Consumer-Code NUTZT diese

| Funktion | Foundation-Pfad | Wo in 04 genutzt |
|---|---|---|
| `CapabilityAudit.HasCapabilityOrWarn` | `01/Source/Registry/` | `Bootstrap.cs` |
| `PackageRegistry.IsRegistered` | `01/Source/Registry/` | `Bootstrap.cs` |
| `ISchemaMigratable` | `01/Source/Save/` | `CreditsLedger`, `Market` |
| `CrossPackageState.TryReadWalletBalance` | `01/Source/CrossPackage/` | 05 liest Wallet via Bridge |

### §2.2 Funktionen die in 04 DUPLIZIERT sein müssen (Standalone-Nutzbarkeit)

| Funktion | Grund für Duplikat | Pfad in 04 |
|---|---|---|
| **CreditsLedger** | Foundation hat kein Wallet. 04 besitzt eigenes persistentes Wallet | `Source/Wallet/CreditsLedger.cs` |
| **Market** | Foundation hat keinen Markt. 04 besitzt `Market.cs`, `MapMarketComponent` | `Source/Market/` |
| **Outpost** | Foundation hat keine Outposts. 04 besitzt `Outpost.cs`, `OutpostWorldObject.cs` | `Source/Outposts/` |
| **PhysicalTransfer** | Foundation hat kein Transfer-System. 04 besitzt eigenes Reserve/Execute/Cancel | `Source/Transfers/` |
| **TradePanel** | Reine 04-Domäne | `Source/Wallet/TradePanel.cs` |
| **BuildingInputAdapter** | Foundation hat keine Baukosten-Adapter. 04 liest `def.costList` für Markt-Preise | `Source/Building/BuildingInputAdapter.cs` |

### §2.3 Funktionen die Foundation BEREITS hat → 04-Duplikat wäre ein Fehler

| Foundation-Funktion | 04 DARF NICHT duplizieren | Grund |
|---|---|---|
| Profil-Erkennung | ❌ | `ProfileDetector` in 01 ist SSOT |
| DLC-Prüfung | ❌ | `DLCFilter` in 01 ist SSOT |
| Save-Migration-Framework | ❌ | `ISchemaMigratable` in 01 ist SSOT |
| Kolonisten-Zählung | ❌ | `ColonialReader` in 01 ist SSOT |
| Capability-Gates | ❌ | `CapabilityAudit` in 01 ist SSOT |

---

## §3 Tatsächlicher Code-Stand

### §3.1 Was existiert (CODE + COMPILES + BOOT)

| Modul | Dateien | Status |
|---|---|---|
| Bootstrap | `Bootstrap.cs` | BOOT ✅ |
| Wallet | `CreditsLedger.cs`, `EconomyHub.cs`, `TradePanel.cs` | BOOT ✅ |
| Market | `Market.cs` | BOOT ✅ |
| Outposts | `Outpost.cs`, `OutpostWorldObject.cs`, `OutpostProxyGraph.cs` | BOOT ✅ |
| Transfers | `PhysicalTransfer.cs` | BOOT ✅ |
| Building | `BuildingInputAdapter.cs` | BOOT ✅ |
| Tests | 3 Suiten (CreditsLedgerSchemaBump, MarketPersistence, PhysicalTransfer) | BOOT ✅ |

### §3.2 Was fehlt (OPEN)

| Geplant (BLUEPRINT) | Status | Blockiert durch |
|---|---|---|
| E1 Wallet-Atomicität (Kauf/Einnahme/Rückbuchung atomar) | PARTIAL | Live-Tests |
| E2 Physische Waren + Transport (Reserve/Execute/Cancel) | PARTIAL | `PhysicalTransfer` existiert; Live-Tests offen |
| E3 Outpost + Produktion (Brutto/Schutz/Wartung/Netto) | PARTIAL | `API-WORLD-01` Spike |
| E4 Proxy-Graph + 3-Tages-Countdown | CODE ✅ | Live-Tests |
| E5 WorldMap-Overlay + DLC-Integration | OPEN | E1–E4 Live-Belege |
| `API-TRADE-01` (MarketValue-Semantik) | OPEN | Spike |
| `API-WORLD-01` (WorldObject-Lifecycle) | OPEN | Spike |

---

## §4 Roadmap (abgeleitet aus globaler ROADMAP.md)

| Schritt | Task | Status |
|---|---|---|
| E-01 | CreditsLedger als persisted GameComponent | ✅ |
| E-02 | Market + MapMarketComponent | ✅ |
| E-03 | Outpost + OutpostWorldObject + ProxyGraph | ✅ |
| E-04 | PhysicalTransfer (Reserve/Execute/Cancel) | ✅ |
| E-05 | BuildingInputAdapter (liest costList für Markt) | ✅ |
| E-06 | EconomyHub (Wallet+Market+Outpost-Integration) | ✅ |
| E-07 | TradePanel | ✅ |
| E-08 | Live-Tests: Wallet-Atomicität, Transfer, Outpost-Countdown | ⏸️ OPEN |

---

## §5 Änderungshistorie

| Datum | Änderung |
|---|---|
| 2026-08-06 | Initial. MOD_INDEX.md aus BLUEPRINT.md + INTERFACE_CONTRACT.md + Source-Inventar abgeleitet. |
