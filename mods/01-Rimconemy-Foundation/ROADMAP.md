# Roadmap 01 – Rimconemy Foundation

> Eigenständige Paketaufgabe 1 von 5  
> Zielplattform: RimWorld 1.6 mit Royalty, Ideology, Biotech, Anomaly und Odyssey  
> Status: Foundation-Runtime implementiert; verbleibende Gameplay- und Falsifizierungs-Gates offen

## 1. Paketauftrag

Foundation ist ein eigenständiges Diagnose-, Profil- und Vertragsmod. Es bietet allein einen sichtbaren Mehrwert für Vanilla-Spielstände und stellt im vollständigen Overhaul die gemeinsamen Regeln bereit, über die die vier Feature-Pakete sicher miteinander kommunizieren.

Foundation ist **keine versteckte Pflichtbibliothek**. Es darf keine Feature-Logik simulieren und darf keine direkten Compile-Referenzen auf optionale Rimconemy-Assemblies verlangen.

## 2. Standalone-Ziel

Mit nur Harmony, RimWorld 1.6 und Foundation kann der Spieler:

- Vanilla-Ressourcen, Produktion und Verbrauch überblicken,
- Engpässe und Energieprobleme erkennen,
- Ereignisse und relevante Transaktionen protokollieren,
- aktive Rimconemy-Pakete und DLCs prüfen,
- das aktive Profil sehen: Vanilla, Teilprofil oder Full Overhaul,
- Save-Schema und Kompatibilitätswarnungen verstehen.

## 3. Full-Overhaul-Ziel

Im Full Overhaul liefert Foundation:

- stabile Paket- und Capability-IDs,
- gemeinsame Versions- und Profilprüfung,
- gemeinsame UI-Snapshots,
- gemeinsame Event-/Transaktionskategorien,
- Save-Migrationsstatus,
- zentrale Warnungen ohne Log-Spam,
- die verbindliche Anzeige, welche Systeme tatsächlich aktiv sind.

## 4. Implementierungs- und Restarbeiten

Die Runtime-Basis ist vorhanden. Die folgenden Schritte bleiben als überprüfbare Restarbeiten bestehen: vollständige Standalone-UI, Save-/Map-/DLC-Matrix, Performance-Gates und der `SURVIVED`-Falsifizierungsbericht.

## 5. Sequenzielle Arbeitsschritte

### Task 1.1 – Technischer Mod- und Build-Spike

- RimWorld-1.6-Assemblys und Harmony-Referenzpfade der Entwicklungsumgebung festhalten.
- Eine minimale Assembly `Rimconemy.Foundation.dll` bauen.
- Mod lädt allein mit Harmony.
- Ladefehler werden als verständliche Moddiagnose ausgegeben.

**Gate:** Foundation lädt ohne Feature-Pakete und ohne Fehlerdialog-Spam.

### Task 1.2 – Paket- und Capability-Vertrag

Definiere stabile IDs für:

```text
rimconemy.foundation
rimconemy.survivalprogression
rimconemy.scavengerinfrastructure
rimconemy.economyterritory
rimconemy.infectedautomation
```

Jedes Feature meldet:

- Paket-ID
- Paketversion
- Save-Schema-Version
- verfügbare Capabilities
- kompatibles Profil

Keine Capability darf nur aus einem Ordnernamen abgeleitet werden.

**Gate:** Foundation erkennt jedes Paket korrekt, auch wenn die übrigen vier fehlen.

### Task 1.3 – Profil- und DLC-Erkennung

Prüfe zur Laufzeit:

- RimWorld-Version 1.6,
- Royalty,
- Ideology,
- Biotech,
- Anomaly,
- Odyssey,
- Paketversionen,
- Save-Migrationsblockaden.

Das UI muss zwischen `Standalone`, `Partial` und `Full Overhaul` unterscheiden.

**Gate:** Kein Save wird als Full Overhaul markiert, wenn ein Paket, DLC oder eine Migration fehlt.

### Task 1.4 – Snapshot-, Event- und Diagnosemodell

Definiere ein neutrales Snapshot-Modell für:

- Ressourcen,
- Produktion/Verbrauch,
- Bedürfnisse,
- Forschung/XP,
- Bedrohung,
- Wallets/Markt,
- Outposts/Verbindungen,
- Mechadroids/Automation.

Nicht installierte Domänen liefern den Status `Unavailable`, nicht erfundene Nullwerte.

**Gate:** Das Dashboard kann fehlende Domänen erklären, ohne sie als „0“ missverständlich darzustellen.

### Task 1.5 – Save-Vertrag

- Foundation-Datenpräfix und Schema-Version festlegen.
- Fehlende Pakete erkennen.
- Migrationen protokollieren.
- den verbindlichen Foundation-Fall aus `../../docs/SAVE_CONTRACT.md` testen: altes Profil ohne Capability-Snapshot wird als `Migrated` neu erkannt, ohne Feature-Daten zu erzeugen.
- Laden bei nicht migrierbaren Daten kontrolliert ablehnen oder das betroffene System einfrieren.
- Niemals persistente Feature-Daten still löschen.

**Gate:** Testsave mit fehlendem Paket erzeugt eine klare Warnung und keine Phantomdaten.

### Task 1.6 – Vanilla-Dashboard und UI-Basis

Standalone-UI implementieren:

- Profilstatus,
- Paketstatus,
- DLC-Status,
- Ressourcen-/Verbrauchsübersicht,
- Ereignislog,
- Save-/Migrationsstatus.

**Gate:** Das UI zeigt jede relevante Abweichung vom Full Overhaul sichtbar an.

### Task 1.7 – Kompatibilitätsprüfung

Prüfe insbesondere:

- Harmony-Ladefolge,
- doppelte Patch-Anwendung,
- XML-Loadfehler,
- fehlende Defs,
- Fehler bei fehlenden DLCs,
- Log-Flood bei optionalen Paketen,
- Save-Load mit älteren Foundation-Schemata.

**Gate:** Foundation bleibt bei beliebiger Kombination der fünf Rimconemy-Pakete diagnostisch funktionsfähig.

## 6. Blindspots, die Foundation absichern muss

| Blindspot | Gegenmaßnahme |
|---|---|
| Optionales Paket wird zur Compile-Pflicht | Capability-Vertrag statt direkter Assembly-Referenz |
| Teilprofil sieht wie Full Overhaul aus | sichtbarer Profilstatus und fehlende-Systeme-Liste |
| DLC fehlt, aber Code nimmt Inhalte an | Laufzeitprüfung und Adapter-Gates |
| Save lädt mit still verlorenem Zustand | Schema-Version, Migration oder kontrollierte Ablehnung |
| Diagnose selbst erzeugt Log-Flood | deduplizierte Warnungen und UI-Zusammenfassung |
| UI zeigt fehlende Systeme als 0 | `Unavailable`-Zustand mit Ursache |
| spätere Pakete definieren inkompatible IDs | IDs und Verträge vor Paket 2 einfrieren |

## 7. Kompatibilitätsregeln

- Native Defs/PatchOperations zuerst; Harmony nur für notwendige Laufzeit-Hooks.
- Keine globale statische Feature-Simulation.
- Keine direkten Referenzen auf Klassen aus Paket 2–5.
- Foundation stellt im Full Profile den Registry-/Servicebus bereit; Feature-Pakete werden late-bound über versionierte Vertragsnamen erkannt.
- Foundation darf andere Paket-Assemblies erkennen, aber nicht deren Implementierung importieren.
- Ohne Foundation bleiben Feature-Pakete standalone oder in einem nicht integrierten Teilprofil.
- Jede externe Mod-/DLC-Annahme wird im UI diagnostizierbar.
- Foundation darf keinen Vanilla-Storyteller, Markt oder Need heimlich ersetzen.

## 8. Tests

### Standalone

- Foundation allein lädt.
- Dashboard zeigt Vanilla-Daten.
- Profilstatus ist korrekt.
- Einstellungen speichern/laden.
- Save-Schema-Warnung funktioniert.

### Integrationsmatrix

Teste mindestens:

```text
Foundation allein
Foundation + jedes einzelne Paket
Foundation + jedes Paar
Foundation + alle fünf Pakete
```

### Full Profile

- alle fünf DLCs erkannt,
- alle fünf Pakete kompatibel,
- Full Profile aktiviert,
- fehlende Capability sichtbar,
- keine Feature-Daten doppelt registriert.

## 9. Performance-Gate

Foundation darf bei unverändertem Spielzustand keine wiederholten Warnungen oder Snapshot-Updates erzeugen. Die Diagnose muss mit den Lastprofilen P1, P2 und P3 aus `../../docs/INTERFACE_CONTRACT.md` laufen; Dashboard-Updates werden gebündelt und dürfen keine Feature-Simulation pro Tick starten.

**Messbares Exit-Kriterium:** P3 läuft zehn Ingame-Tage mit höchstens 2 ms durchschnittlicher und 5 ms 99.-Perzentil-Updatezeit pro 60-Tick-Update, höchstens 1 MiB Netto-Speicherwachstum pro Ingame-Tag und höchstens 20 deduplizierten Diagnoseeinträgen pro Ingame-Tag; Kartenwechsel darf keine Zustandsabweichung erzeugen.

## 10. Falsifizierungs-Gate

Vor Übergabe muss der Bericht `../../docs/FALSIFICATION_REPORTS/rimconemy.foundation__Servicebus.md` den Status `SURVIVED` erreichen. Seine Achsen A–G müssen jeweils einen eigenen Test, ein Ergebnis und einen Beleg enthalten: Vanilla-Doppelbetrieb, Besitz/Daten, Minimalität, Fremdmod-Konflikt, Save/Lifecycle, UI-Blackbox und Performance/Determinismus. Die L0–L7-Load-Order-Fälle aus `../../docs/COMPATIBILITY_MATRIX.md` werden im Bericht eingegrenzt; ohne vollständigen Scope bleibt er `UNVERIFIED`.

## 11. Exit-Kriterien für Übergabe an Paket 2

- Foundation allein ist spielbar und sichtbar nützlich.
- Capability-, Registry-, Command-/Event-, Profil- und Save-Verträge aus `../../docs/INTERFACE_CONTRACT.md` sind dokumentiert und getestet.
- Paket 2 kann ohne direkte Compile-Abhängigkeit auf Foundation entwickelt werden.
- Snapshot- und Event-Schnittstellen sind versioniert.
- keine offenen kritischen Blindspots aus Abschnitt 5.
