# Rimconemy — Audit lokale Commits und Stub-Abdeckung

> **Stand:** 2026-08-05 | **Basis:** `origin/master..HEAD`, HEAD `193d7e3` |
> **Methodik:** 4-Quadrate (USER×UX, USER×TECHNICAL, CODER×UX, CODER×TECHNICAL)
> **Fokus:** sechs lokale Commits (`394fd4f..193d7e3`), neue Foundation-/Intro-/Tutorial-Pfade und Produktions-Stubs; keine Reparaturen in diesem Audit.

## Executive Summary — Top-Risiken VOR dem nächsten Feature-Sprint

| # | Risiko | Severity | LOC-Impact | Phase-N-Blocker |
|---|---|---:|---:|---:|
| 1 | Intro schließt ohne `TutorialDirector.NotifyIntroCompleted()`; Tutorial bleibt dauerhaft hinter `introCompleted == false` | crit | +1 | yes |
| 2 | `TutorialDirector` persistiert weder `introCompleted`/`currentStepIndex` noch rekonstruiert es aus `TutorialState`; Save/Load führt zu Stillstand oder doppelten Briefen | high | +10 | yes |
| 3 | Tutorial-Trigger sind nur unverdrahtete statische Bool-Flags; `SurvivalIntegration.Initialize()`/`SurvivalTutorialBridge.Initialize()` haben keinen Produktions-Caller, Generator/Turm/Outpost/Trade/Infected überhaupt keinen Caller | high | +30 | yes |
| 4 | `RimPadWindow` zeigt in allen fünf Tabs explizit `TODO`; das gelieferte Dashboard ist UI-Chrome ohne Feature-Inhalt | high | +5 | no |
| 5 | Neue `Foundation.Bridge.CapabilityAudit`/`EventBridge` bilden ein zweites, vom echten `Foundation.Registry`-System getrenntes Capability-Modell | med | +120 | no |
| 6 | Ungetrackte lokale Dateien enthalten weitere Tutorial-Stub-Schichten und werden nicht von den lokalen Commits abgedeckt | med | +6 | no |

## Lokaler Commitumfang

`git log --stat origin/master..HEAD` zeigt 6 Commits mit 23 versionierten Dateien und 1.369 Nettozeilen. Der aktuelle Arbeitsbaum enthält zusätzlich unversioniert:

- `mods/02-Rimconemy-Survival-Progression/Source/Bridge/`
- `mods/02-Rimconemy-Survival-Progression/Source/Survival/`
- `test_intro.py`

Der vorherige `./scripts/dev_quick_test.sh` war grün (Failures 0, Warnings 0), testet aber den neuen Intro-/Tutorial-Lifecycle nicht. `git diff --check origin/master..HEAD` meldet zahlreiche neue Dateien mit trailing whitespace; kein funktionaler Fehler, aber ein klarer Commit-Hygiene-Befund.

## TEIL A: Q1 — USER × UX (Spieler-Erleben)

| Befund | Beleg | Auswirkung |
|---|---|---|
| RimPad-Inhalt ist vollständig Platzhalter | `mods/01-Rimconemy-Foundation/Source/UI/RimPadWindow.cs:60-65`: fünf Methoden rendern nur `"... tab - TODO"` | Spieler sehen keine Survival-, Economy-, Threat- oder Diagnostics-Daten trotz MainButtonDef/Tab-Chrome. |
| Intro beendet sich ohne sichtbares Tutorial-Ergebnis | `IntroFlowWindow.cs:171-177` despawnt und schließt nur das Fenster; kein Notify-Aufruf | Der angekündigte nächste Schritt/erste Tutorial-Brief erscheint nicht. |
| Tutorial-Inhalt ist auf 5 Defs begrenzt | `Defs/TutorialSteps/TutorialStepDefs.xml:3-51` definiert nur Schritte 1–5; `unlockDefs`/Dismiss-UX wird nirgends verarbeitet | Der Spieler bekommt weder echte Freischaltungen noch eine Dismiss-/Guide-Steuerung. |
| Tutorial-Trigger zeigen keinen verlässlichen Fortschritt | `TutorialTriggerBridge.cs:12-20` hält globale Bool-Werte; kein UI-/Status-Readback | Kein belastbarer Hinweis, warum ein Guide-Schritt noch nicht feuert. |

## TEIL B: Q2 — USER × TECHNICAL (Spieler unter Last)

| Befund | Beleg | Auswirkung |
|---|---|---|
| Save/Load des Tutorials ist nicht geschlossen | `TutorialDirector.cs:14-17` hält `currentStepIndex` und `introCompleted`; `ExposeData()` in `:76-80` scribed nur `State` | Nach Reload ist `introCompleted` wieder false und der Director bleibt in `GameComponentTick()` (`:49-50`) stehen. Der Fortschritt wird nicht wieder aufgenommen. |
| Bereits gezeigte Schritte werden nach Rekonstruktion nicht übersprungen | `TutorialDirector.cs:69-74` ruft nur `InitializeSteps()` auf; `State.CompletedSteps` wird in der Indexauswahl `:52-66` nicht geprüft | Bei einer späteren Reparatur des Intro-Flags können alte Briefe erneut erscheinen. |
| Triggerzustand ist nicht save-/map-scoped | `TutorialTriggerBridge.cs:22-32` bietet nur `Reset()`, aber keinen Save-Envelope; im geprüften Produktionscode kein Aufruf von `Reset()` | Ereignisse aus einer vorherigen Karte/Session können neue Saves beeinflussen; umgekehrt gehen Trigger vor Initialisierung verloren. |
| Intro-Horde ist nicht über einen echten Runtime-Gate-Test abgesichert | `IntroFlowWindow.cs:179-204` erzeugt vier Pawns live; vorhandene Runtime-Gates prüfen keinen Intro-Fensterablauf | Spawn-/Despawn-/Close-Reihenfolge, Pausenverhalten und Def-/Faction-Fehler bleiben LIVE offen. |

## TEIL C: Q3 — CODER × UX (UI-Code-Pfad)

| Befund | Beleg | Bewertung |
|---|---|---|
| Dashboard trennt Tab-Routing von Daten nicht, sondern delegiert auf lokale TODO-Methoden | `RimPadWindow.cs:46-65` legt fünf Tabs an, deren Renderer sämtlich Placeholder sind | Der Commit liefert Struktur, aber keinen nutzbaren UI-Pfad. Das ist ein Scaffold, kein fertiges Dashboard. |
| Tab-Auswahl ist globaler statischer Zustand | `RimPadTabDrawer.cs:13-20` hält `selectedTabIndex` und `tabs` statisch | Mehrere RimPad-Instanzen/Save-Kontexte teilen Auswahl und Tab-Liste; außerdem fehlt ein Bounds-Guard bei `DrawSelectedTabContent()` (`:46-50`). |
| Tutorial-Schritte koppeln Trigger, Reihenfolge, Letter-Rendering und Fortschritt in einer Klasse | `TutorialStep.cs:21-70` | Kein separater Trigger-/Step-State-/Presentation-Seam; Tests können den eigentlichen Letter-/GameComponent-Pfad kaum isoliert prüfen. |
| `unlockDefs` und `DismissedSteps` sind Daten ohne Verhalten | `TutorialStepDef.cs:19`, `TutorialState.cs:13-16`, aber keine Consumer im Produktionscode | XML suggeriert Funktionen, die der Code nicht implementiert. |

## TEIL D: Q4 — CODER × TECHNICAL (System-Stabilität)

| Befund | Beleg | Risiko |
|---|---|---|
| Intro→Tutorial ist nicht verdrahtet | `IntroFlowWindow.cs:174-177`; einziger Treffer für `NotifyIntroCompleted` ist die Methodendefinition `TutorialDirector.cs:28` | Kritischer Dead-End im neuen Feature. Der Build kann grün sein, obwohl der Ablauf nie weitergeht. |
| TutorialDirector ist als GameComponent nicht vollständig zustandsfähig | `TutorialDirector.cs:14-17, 76-80` | Nichtpersistierte Orchestrierungsfelder verletzen den behaupteten „state persistence“-Anspruch. |
| Survival-Tutorial-Integration existiert doppelt und wird nirgends initialisiert | `Source/Bridge/SurvivalIntegration.cs:13-25` und `Source/Bridge/SurvivalTutorialBridge.cs:10-24`; `rg` findet keinen Caller von `Initialize()` | Selbst Campfire/Wall/Resource lösen im Spiel nicht zuverlässig aus; bei späterem Initialisieren drohen doppelte Event-Abos. |
| Neue Bridge ist parallel zum echten Capability-System | `mods/01.../Source/Bridge/CapabilityAudit.cs:4-50` ist Namespace `Rimconemy.Foundation.Bridge`; das aktive System liegt in `Source/Registry/CapabilityAudit.cs` und nutzt `PackageRegistry` (`:53-63`) | Registrierungen über die neue Bridge sind für Leser des echten Systems unsichtbar. Die beiden APIs haben außerdem unterschiedliche Capability-IDs. |
| Mehrere Trigger-Enum-Werte haben 0 Produktions-Caller | `TutorialStepDef.cs:24-31`; Trefferprüfung zeigt keine Aufrufer für `OnGeneratorBuilt`, `OnTurretBuilt`, `OnOutpostFounded`, `OnTradeDone`, `OnFirstInfectedContact` | Fünf definierte Tutorialpfade sind reine Stubs/Dead Ends. |
| Neue Produktionsdateien enthalten bewusst explizite Placeholder-Logik | `mods/02.../Source/Survival/CampfireManager.cs:8-19`, `WallBuilder.cs:8-19`, `ResourceCollector.cs:8-19`: „Placeholder“, keine Validierung, nur Event-Aufruf | Tests/Manuell aufgerufene Methoden können Erfolg vortäuschen; sie sind kein echter Bau-/Sammel-Hook. |
| `ScenPart_IntroSequence` setzt `introShown` vor dem Öffnen korrekt, aber nur per Scenario-Part | `ScenPart_IntroSequence.cs:18-27` | Idempotenz ist lokal verbessert; ein allgemeiner Intro-/Save-State außerhalb dieses Szenarios ist nicht vorhanden. |

## TEIL E: Risiko-Heatmap & empfohlene Reihenfolge (Phase N → Phase N+1)

### P0 — vor jedem weiteren Tutorial-/UI-Feature

1. **Intro completion schließen:** Beim erfolgreichen Ende exakt einmal `NotifyIntroCompleted()` am `TutorialDirector` aufrufen; null-sicher und testbar. Bei fehlender GameComponent-Instanz muss der Pfad sichtbar warnen statt still zu schließen.
2. **Tutorial-State konsistent machen:** `IntroCompleted`, aktueller Schritt und/oder eine aus `CompletedSteps` abgeleitete Position scribe/migrieren. Beim Laden bereits erledigte Defs überspringen. `TutorialState` braucht eine echte Schema-/Save-Strategie, nicht nur `IExposable`.
3. **Einen Integrationspfad wählen:** `SurvivalIntegration` oder `SurvivalTutorialBridge`, nicht beide. Initialisierung an einen vorhandenen Startup-/StaticConstructor-Punkt hängen und doppelte Subscriptions verhindern.

### P1 — echte Consumer statt Stubs

4. Produktions-Hooks für Campfire, Wall und Resource an vorhandene Vanilla-/Mod-Completion-Pfade anschließen; `TryBuild...`-Methoden nicht als simulierte Erfolgs-API stehen lassen.
5. Für Generator/Turm/Outpost/Trade/Infected jeweils einen Owner-Callback oder die betreffenden Trigger-Defs entfernen, bis der Consumer existiert.
6. `unlockDefs`, Dismissal und Trigger-State entweder implementieren oder aus Def/State entfernen; keine Funktionsversprechen ohne Consumer.

### P2 — UI und Architektur bereinigen

7. RimPad-Tabs an die bestehenden Snapshot-/Dashboard-Read-Models anschließen; TODO-Labels erst danach entfernen.
8. Eine CapabilityAudit-Implementierung als SSOT behalten: `Foundation.Registry.CapabilityAudit`/`PackageRegistry`. Die neue `Foundation.Bridge.CapabilityAudit` und das parallele EventBridge-Design entweder bewusst als API integrieren oder löschen.
9. Unit-/Runtime-Gates für Intro: Intro öffnet einmal, pausiert, führt Phasen aus, despawnt genau die eigenen Pawns, schließt und setzt Tutorial-Completion; Save/Load vor und nach Intro.

## Definition of Done für Phase N+1

- [ ] `rg -n "NotifyIntroCompleted" mods` zeigt mindestens einen Produktions-Caller und einen Test.
- [ ] `TutorialDirector.ExposeData()` bewahrt Intro-/Schrittfortschritt; ein Roundtrip-Test bestätigt, dass bereits gezeigte Defs nicht erneut erscheinen.
- [ ] Genau eine Tutorial-Bridge-Initialisierung existiert und ist von einem Produktions-Startup-Pfad erreichbar; keine doppelte Event-Subscription.
- [ ] Für jeden in `TutorialStepDef.TriggerType` verbleibenden Trigger existiert mindestens ein Produktions-Caller; nicht verdrahtete Trigger sind entfernt oder als offen dokumentiert.
- [ ] `rg -n 'tab - TODO|Placeholder|For now' mods/01-Rimconemy-Foundation/Source/UI mods/02-Rimconemy-Survival-Progression/Source/Survival` liefert keine Produktions-Placeholder im aktiven Pfad.
- [ ] RimPad rendert mindestens je einen echten Read-Model-Wert für Survival, Infrastructure, Economy, Threat und Diagnostics.
- [ ] Capability-Registrierung und Capability-Lesen verwenden dieselbe SSOT-API und dieselben IDs.
- [ ] Ein frischer Build, `./scripts/dev_quick_test.sh` und ein Intro-spezifischer Runtime-/Regression-Gate sind grün; `git diff --check` ist sauber.

**Wenn nur EINE Sache zuerst repariert wird:** Intro-Abschluss → `TutorialDirector`-State-Lifecycle schließen, weil der gesamte neue Tutorialpfad aktuell nach dem Intro als Dead End endet und ein grüner Build diesen Fehler nicht erkennt.
