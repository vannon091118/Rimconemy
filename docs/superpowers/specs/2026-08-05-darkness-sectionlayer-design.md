# Darkness/Fog-of-War SectionLayer Design

**Datum:** 2026-08-05
**Paket:** 05 — Rimconemy Infected & Automation
**Status:** Design freigegeben; Implementierung noch nicht gestartet
**Ziel:** Screen-Space-Schachbrett durch einen lückenfreien World-Space-Map-Renderer ersetzen.

## 1. Problem und belegte Ursache

Der aktuelle Renderer liegt in `ColonistSightSystem.MapComponentOnGUI()` und zeichnet für jeden sichtbaren 4×4-Block ein eigenes `GUI.DrawTexture`-Rechteck. Die Weltposition wird mit `Camera.current.WorldToScreenPoint` in Screen-Space projiziert. Bei RimWorlds Kamera-Zoom, Fenstermodus und Fließkomma-/Pixelrundung entstehen sichtbare Spalten zwischen den Rechtecken. Das erzeugt das beobachtete Schachbrett-/Kastenmuster.

Die Sichtdaten haben zusätzlich einen unabhängigen Mangel: `SightConeMath.ComputeCellVisibility` kennt keine Hindernisse. Sicht kann dadurch durch Wände und Gebirge laufen. Das ist ein Logikproblem, aber nicht die primäre Ursache der Pixel-Lücken.

## 2. Ziele

- Keine `GUI.DrawTexture`-Rechtecke für die Map-Dunkelheit.
- Keine `WorldToScreenPoint`-Berechnung im Darkness-Renderer.
- World-Space-Mesh mit zusammenhängenden Zellflächen und Vertexfarben/Alpha.
- Sichtwerte bleiben als kontinuierliche Werte `[0,1]` erhalten.
- Wände/Gebirge blockieren Sicht via lokal verifizierter `GenSight.LineOfSight`-API.
- Rebuild nur für dirty Sections, nicht jeden Frame und nicht die komplette Map.
- Overlay bleibt bei Pawn-, Gebäude- und Item-Auswahl sowie geöffneten Fenstern aktiv.
- Save/Load setzt den Renderzustand sicher zurück und regeneriert die betroffenen Sections.

## 3. Nicht-Ziele dieser Iteration

- Kein neues persistentes `0/1/2`-Fog-State-Modell. Der bestehende Float-Sichtwert bleibt die SSOT für Helligkeit.
- Kein vollständiger Recursive-Shadowcasting-Algorithmus. `GenSight.LineOfSight` ist für die erste Occlusion-Korrektur ausreichend und lokal bestätigt.
- Keine Änderung an Licht-/Tageszeitformeln, Fackel-/Feuerlogik oder Schatten-Noise, solange der World-Space-Renderer nicht stabil ist.
- Keine Änderung der Vanilla-Dateien oder des Vanilla-`LangIcon.png`.

## 4. Lokale RimWorld-1.6-API-Befunde

Gegen `/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll` (RimWorld 1.6.4566) wurden folgende Typen geprüft:

- `Verse.MapDrawLayer` ist abstrakt, Konstruktor `MapDrawLayer(Map map)`, mit `Regenerate()`, Submesh-Verwaltung und `FinalizeMesh()`.
- `Verse.SectionLayer` ist abstrakt, Konstruktor `SectionLayer(Section section)`, abgeleitet von `MapDrawLayer`.
- `Verse.LayerSubMesh` besitzt Konstruktor `LayerSubMesh(Mesh mesh, Material material, Bounds? bounds)` sowie öffentliche Listen `verts`, `tris`, `colors` und `FinalizeMesh(MeshParts)`.
- `Verse.Section` besitzt `GetLayer(Type)`, `RegenerateSingleLayer(SectionLayer)`, `RegenerateAllLayers()` und interne Layerlisten.
- `Verse.MapDrawer` besitzt `RegenerateLayerNow(Type)`, `RegenerateEverythingNow()`, `MapMeshDirty(...)` und interne Sections.
- Vanilla enthält bereits `Verse.SectionLayer_Darkness` und `Verse.SectionLayer_FogOfWar`.
- Es gibt keine bestätigte öffentliche Mod-Registry zum freien Hinzufügen beliebiger SectionLayer. Ein eigener Layer benötigt daher kontrollierte Harmony-Injection in den Section-Layer-Lifecycle.
- `Verse.ShaderDatabase.Transparent` und `WorldOverlayTransparent` sind lokal vorhanden; die konkrete Materialwahl muss in einer kleinen Build-/Runtime-Probe bestätigt werden.
- `GenSight.LineOfSight(IntVec3, IntVec3, Map)` ist lokal verifiziert.

## 5. Architektur

### 5.1 Sichtdaten: `ColonistSightSystem`

`ColonistSightSystem` bleibt `MapComponent` und Eigentümer des per-Map-Sichtgrids:

```text
_visibilityGrid[cellIndex] : float [0..1]
```

Die Berechnung bleibt zunächst auf dem vorhandenen Trichter-/Lichtmodell aufgebaut:

```text
Kegel + Tageslicht + Glower/Fackel + Mauslicht
→ GenSight.LineOfSight(pawnPos, cell, map)
→ max-Sichtwert ins Grid
```

Für die Berechnung werden nur Kandidaten im maximalen Radius betrachtet. Eine Zelle außerhalb der LOS erhält keinen Sichtbeitrag. Die Pawn-Zelle bleibt stets sichtbar.

### 5.2 Dirty-State

Der Sight-MapComponent führt einen Dirty-Status für das Grid und betroffene Sections. Mindestens folgende Ereignisse markieren den Renderer dirty:

- Pawn bewegt sich oder ändert Blickrichtung.
- Sichtrelevantes Tageslicht-/Wetterintervall aktualisiert sich.
- relevante Glower-/Feuerlage ändert sich.
- Map wird geladen/finalisiert.
- Konstruktion, Abriss oder Türzustand verändert die Occlusion.

Die erste Implementierung darf konservativ alle Sections dirty markieren, wenn ein sicherer Zell-/Gebäude-Hook noch nicht verifiziert ist. Optimierung auf einzelne Sections folgt erst nach funktionaler Stabilität.

### 5.3 Renderer-Priorität

1. **Vanilla-Layer prüfen:** `SectionLayer_Darkness` und `SectionLayer_FogOfWar` gegen die lokale Assembly vollständig enumerieren. Falls deren Mesh-/Material-/Regenerate-Pfad ohne Überschreiben vanilla-seitiger Daten sicher patchbar ist, diesen Pfad bevorzugen.
2. **Eigener Layer:** Falls Vanilla-Layer nicht geeignet sind, `DarknessSectionLayer : SectionLayer` in Package 05 implementieren.
3. **Lifecycle-Injection:** Eine minimale Harmony-Patchstelle fügt den eigenen Layer in die `Section`-Layerliste ein oder ersetzt ausschließlich den für die Darkness zuständigen Vanilla-Layer. Keine globale MapDrawer-Transpilation ohne vorherige Signatur-/IL-Prüfung.

Der Renderer darf nicht parallel zur bisherigen `MapComponentOnGUI`-Kästchenschleife aktiv bleiben, da zwei Darkness-Schichten die Alpha-Werte verdoppeln würden.

### 5.4 Section-Mesh

Für jede Section wird ein zusammenhängendes Mesh aus Zellquads erzeugt. Jede Zelle erhält vier Vertices und zwei Dreiecke; die Sichthelligkeit wird über `Color32`-Werte gespeichert. Benachbarte Vertexfarben werden so interpoliert, dass der Übergang zwischen Zellen weich bleibt.

Pro Zellquad:

```text
world vertices: (x,0,z), (x+1,0,z), (x+1,0,z+1), (x,0,z+1)
color alpha: maxAlpha * sqrt(1 - visibility)
```

Die genaue Höhe/Layerposition und die `MeshParts`-Flags werden an vorhandene Vanilla-Layer angepasst, damit das Mesh nicht z-fightet und die Map-Darstellung nicht verdeckt wird.

### 5.5 Material

Der Layer verwendet ein unbeleuchtetes, transparentes Vanilla-Material aus der lokal bestätigten Shader-Datenbank. Keine neue Shader-Datei in dieser Iteration. Das Material muss:

- Vertexfarben/Alpha verwenden;
- Alpha-Blending aktivieren;
- Depth Write vermeiden oder dem Vanilla-Overlay-Verhalten entsprechen;
- die Map nicht außerhalb der Sichtzellen abdunkeln.

## 6. Fehler- und Lifecycle-Verhalten

- Wenn Map, Section, Mesh oder Material noch nicht verfügbar ist: kein Crash, Layer bleibt dirty und wird beim nächsten gültigen Lifecycle regeneriert.
- Wenn LOS eine Exception auslöst: Zellbeitrag wird sicher auf 0 gesetzt und ein deduplizierter Warnlog geschrieben; kein vollständiger Renderabbruch.
- Bei Save/Load: Grid auf sicheren Initialwert setzen, `_lastUpdateTick` zurücksetzen, alle Sections dirty markieren und erst nach `FinalizeInit` regenerieren.
- Bei Map-Wechsel: alter Renderer wird nicht weiter gezeichnet; neuer Map-Layer erhält eigenen Zustand.
- Bei UI-Fenstern: kein Early-Return im Map-Renderer; die Fenster-Zeichenreihenfolge bleibt RimWorld überlassen.

## 7. Tests und Gates

### Datenebene

- Pawn-Zelle ist `1`.
- Sichtwert liegt immer in `[0,1]`.
- Geschlossene Wand blockiert eine Zelle dahinter.
- Freie Zelle bleibt bei gleicher Beleuchtung sichtbar.
- Mehrere Pawns verwenden den Maximalwert.
- Fackel/Glower kann lokale Sichtbarkeit erhöhen.

### Mesh-Ebene

- Jede betroffene Section besitzt nach Regeneration ein gültiges Mesh.
- Vertices, Dreiecke und Farben haben kompatible Anzahlen.
- Alphawerte sind in gültigem Bereich.
- Leere/ungültige Sections erzeugen kein fehlerhaftes Mesh.

### Runtime-Ebene

- Fenster- und Vollbildmodus.
- Minimaler und maximaler Zoom.
- Kameraschwenk über Map-Rand und Gebirge.
- Pawn-/Gebäude-/Item-Auswahl.
- InfoCard und Inspect geöffnet.
- Save → Load mit aktivem Renderer.
- 1, 2 und 3× Spielgeschwindigkeit.
- Keine sichtbaren Lücken oder Schachbrettflächen.

Bestehende Regressionstests bleiben unverändert und werden um reine Mathematik-/Mesh-Helfertests ergänzt, soweit sie ohne echte Unity-Renderinstanz möglich sind. Der endgültige visuelle Gate bleibt ein echter RimWorld-Live-Test.

## 8. Rollback

Der bisherige Screen-Space-Renderer wird erst entfernt, wenn der neue Layer in einem Build kompiliert und mindestens ein Map-Load ohne Exception durchlaufen hat. Während der Entwicklung bleibt der alte Pfad in einem klar isolierten, nicht gleichzeitig aktivierten Fallback-Block. Nach erfolgreichem Runtime-Gate wird er vollständig entfernt.

## 9. Offene Implementierungsentscheidung

Vor dem ersten Codeumbau muss die konkrete Vanilla-Layer-Implementierung (`SectionLayer_Darkness`/`SectionLayer_FogOfWar`) weiter enumeriert werden. Danach wird entschieden, ob Patch-in-place oder eigener Layer verwendet wird. Diese Entscheidung darf nicht anhand von Namen allein getroffen werden, sondern anhand von Konstruktor, Regenerate-Pfad, Layer-Material und Section-Registrierung.
