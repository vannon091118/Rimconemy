# Chat-Protokoll — User-Aussagen & Entscheidungen (2026-08-05)

> **Zweck:** Alle User-Aussagen aus dem Chat werden 1:1 dokumentiert (kein LLM-Output, keine Interpretation). Dient als Beweis- und Drift-Grundlage für Planung, Berichte und spätere Umsetzung. **Kein Skill-Output** — direkt aus dem Chat.
> **Stand:** 2026-08-05 · **Quelle:** Chat-Verlauf (Gameplay-Fragen 1–8 + Entscheidungen D1/D2)
> **Runtime-Kontext:** Letzter Runtime-Lauf `runtime-20260805-010905.txt` war **FAIL** (failures=1, warnings=1) — vor Live-Belegsammlung sollte das Gate wieder grün sein (siehe `.runtime-reports/`).

---

## 1. Antworten auf die Gameplay-Fragen (1–8)

### 1. Start / Charakter
> „einen überlebenen mi schifsbrüchigr start beingungen normalen inventar freien aler keine waffe. Skill punkte wurden nach char auwahll Vrgben was das charackter rollen Obsolet machte aber nich kommuniziert. ich will eig das die skill punkte in roll fenster ausgegeben werden und der spieler basierend darauf auch start raits und maluse bekommt. Balnce die skill punkte vorher (READ oNLY NUR PLANEN MIT MIR)"

**Essenz:**
- Start: **1 Überlebender**, Schiffsbruch-Bedingungen, normales Inventar, **keine Waffe**.
- Problem: Skillpunkte wurden **nach** der Charakter-Auswahl vergeben → machte die **Charakter-Rolle obsolet**, ohne dass das kommuniziert wurde.
- Wunsch: **Skillpunkte im Rollen-Fenster** ausgeben; der Spieler bekommt basierend darauf **Start-Traits und Malus**.
- **Balance der Skillpunkte VORHER** festlegen.
- ⛔ **READ ONLY — NUR PLANEN** (kein Code für diesen Punkt).

### 2. Waffe
> „nein" (Hattest du zu Beginn eine Schusswaffe?) → **Nein, keine Waffe** (bestätigt; Waffe wurde beim Load verworfen — XML-Kategorie-Fehler, siehe Log: `ThingDef 'Rimconemy_ScrapRifle' not loaded`).

### 3. Erste Nacht / Infizierte
> „nein kein spawn. dazu will ich eh Sobald wie möglich eine höhrere infizierten dichte die auf licht und im radius von X tiles 'hören' + sichtweitee beschränken"

**Essenz:**
- Kein Gegner-Spawn in der ersten Nacht (kein Infizierten-Spawn erlebt).
- Gewünscht **sobald wie möglich**: **höhere Infizierten-Dichte**, die
  - **auf Licht „hört"** (Licht als Wahrnehmungsquelle),
  - **innerhalb eines Radius von X Tiles** wahrnimmt,
  - **begrenzte Sichtweite** hat.

### 4. Ideologie
> „Idiologie ist vanilla" → Die Kolonie hatte die **Vanilla-Ideologie („Astropolitanisch")**, nicht `Rimconemy_Ideo_Survival` (Log: „Recommended preset NOT applied").

### 5. Phasenfortschritt / UI
> „NEIN seh ich nicht. ui ist sehr unübersichtlich" → Der Tab **„📈 Phasenfortschritt" ist nicht sichtbar**; die **UI wird als sehr unübersichtlich empfunden**.

### 6. Bauschutt bauen
> „JA Aber ds wure utomatisch am rnd der kaarte plaziert und ds icon ist ein campfire" → **Bauschutt-Bauen funktioniert (JA)**, aber: Gebäude wurden **automatisch am Kartenrand platziert** und das **Icon ist ein Campfire** (Bug).

### 7. Spielgefühl / Schwierigkeit
> „Viel zu langweilig kein druck kein impact nur weniger möglichkeiten aktuell" → Spielgefühl auf „Rough": **zu langweilig — kein Druck, kein Impact, nur weniger Möglichkeiten**.

### 8. Speichern/Laden
> „nein saves machen mir erst sorgen wenn da mod pas macht" → Save/Load **nicht getestet**; Saves werden erst relevant, **wenn der Mod funktioniert**.

---

## 2. Entscheidungen D1 + D2 (Character-Setup)

### D1 — Wann wird die Rolle gewählt?
> „Aktuell beim landen. Geplant Beim Characher roll Als Overwrite quasi"

**Essenz:**
- **Aktuell:** Rolle wird beim Landen gewählt (nach Spielstart).
- **Geplant:** Rolle wird **bei der Charakter-Roll-Wahl** gewählt, **als Overwrite** (überschreibt die generierte Rolle quasi).

### D2 — Traits & Skill-Gewichtung
> „Ich wäre dafür das traits Gewichtet werden und killpunkte nach wichtigkeit im gameplay loop. Ausserdem will ich eh die Fähigkeiten Lieber bündeln das zb Kunst Und Konstruktion siich ein skill teilen (je nachdem ob das balnceing und progess seitigen sinn macht)"

**Essenz:**
- **Traits gewichten** (nicht alle gleichwertig).
- **Skillpunkte nach Wichtigkeit im Gameplay-Loop gewichten** (Kern-Skills des Loops wiegen mehr).
- **Fähigkeiten bündeln:** z. B. **Kunst (Artistic) und Konstruktion (Construction) teilen sich EINEN Skill** — wenn das Balancing- und Progression-seitig Sinn macht (Kann-Entscheidung, keine Pflicht).

---

## 3. Prioritäten (User-Vorgabe 2026-08-05)

1. **Infizierten-Sensorik (#3)** — Pläne prüfen („das ist schon irgendwo dokumentiert mit Umsetzung") → Priorisieren; gleichwertig zur Bauschutt-Priorisierung.
2. **Bauschutt-Platzierungs-Bug (#6)** — gleichwertig priorisieren.
3. Nächster Output: **Analyse mit Grep-Referenzen + gegen-geprüfte Vanilla+DLC-Fakten + Execution-Tasklist**.
4. **Alles aus dem Chat dokumentieren** + Reports danach updaten.

---

## 4. Verlinkte Deliverables

| Deliverable | Pfad |
|---|---|
| Infizierten-Sensorik: Analyse + Execution-Tasklist | `ROADMAP.md §9.8` (integriert, Plan-Datei gelöscht) |
| Falsifikations-Bericht Erste Nacht (Phase 7) | `docs/falsification/earlygame__FirstNight.md` |
| Falsifikations-Bericht ThreatPressure | `docs/falsification/infected__ThreatPressure.md` |
| Falsifikations-Bericht InfectedRaid | `docs/falsification/infected__InfectedRaid.md` |
