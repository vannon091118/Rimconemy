// Vanilla API Matrix Spike — Phase 0 / Task 0.1
// Lädt die lokale RimWorld 1.6 Assembly-CSharp.dll und enumeriert die 13 Anker-APIs
// aus dem Vertical-Slice-Plan. Ausgabe: Markdown-Tabelle(n) für docs/vanilla-api-matrix-1.6.md.
//
// Aufruf:  dotnet run --project tools/inspect -- [outPath]
// outPath  optional; Standard: tools/inspect/api-matrix.raw.md

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;

internal static class Program
{
    // Die 13 Vanilla-Anker aus Vertical-Slice-Plan Phase 0 / Task 0.1.
    // Schlüssel = kurze Spalte in der Ausgabe-Matrix; VollName = Assembly-Qualified Name.
    private static readonly Dictionary<string, string> Anchors = new(StringComparer.Ordinal)
    {
        ["ScenarioBase"]   = "Verse.ScenarioBase",
        ["ScenPart"]       = "RimWorld.ScenPart",
        ["GameComponent"]  = "Verse.GameComponent",
        ["MapComponent"]   = "Verse.MapComponent",
        ["WorldComponent"] = "Verse.WorldComponent",
        ["ThingComp"]      = "Verse.ThingComp",
        ["IncidentWorker"] = "RimWorld.IncidentWorker",
        ["RecipeWorker"]   = "RimWorld.RecipeWorker",
        ["Designator"]     = "RimWorld.Designator",
        ["GenSight"]       = "RimWorld.GenSight",
        ["FogGrid"]        = "Verse.FogGrid",
        ["PawnGenerator"]  = "RimWorld.PawnGenerator",
        ["ResearchManager"]= "RimWorld.ResearchManager",
        ["CompRefuelable"] = "RimWorld.CompRefuelable",
        ["CompGlower"]     = "RimWorld.CompGlower",
        ["Compound_Touch"] = "FORCE_FALLTHROUGH",
    };

    private const string AsmPath = "/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll";

    private static int Main(string[] args)
    {
        var outPath = args.Length >= 1 ? args[0] : Path.Combine("tools", "inspect", "api-matrix.raw.md");
        Console.WriteLine($"[spike] reading {AsmPath}");
        if (!File.Exists(AsmPath))
        {
            Console.Error.WriteLine($"[spike][FATAL] {AsmPath} not found");
            return 2;
        }

        using var raw = File.OpenRead(AsmPath);
        var assembly = AssemblyDefinition.ReadAssembly(raw, new ReaderParameters
        {
            ReadWrite = false,
            InMemory  = true,
        });

        var md = new System.Text.StringBuilder();
        md.AppendLine("# Vanilla-API-Matrix (Spike-Rohdaten, 2026-08-04)");
        md.AppendLine();
        md.AppendLine($"Quelle: `{AsmPath}` · RimWorld 1.6.4566 (Linux/GOG Build).");
        md.AppendLine();
        md.AppendLine("Hinweis: Rohdaten. Verbindliche Matrix: `docs/vanilla-api-matrix-1.6.md` (manuell kuratiert).");
        md.AppendLine();

        // 1) Existenz + Pflicht-Methoden pro Anchor enumerieren.
        foreach (var (key, fullName) in Anchors)
        {
            if (key == "Compound_Touch") continue;
            // Exakter FullName-Treffer ODER Fallback auf Name (für Klassen, die Mono.Cecil nicht mit FullName indexiert)
            var def = assembly.MainModule.Types.FirstOrDefault(t => t.FullName == fullName)
                   ?? assembly.MainModule.Types.FirstOrDefault(t => t.Name == key);

            if (def == null)
            {
                md.AppendLine($"## {key} — `{fullName}`");
                md.AppendLine();
                md.AppendLine("**STATUS: NICHT GEFUNDEN** — Klasse fehlt in der lokalen 1.6-Assembly (oder umbenannt/vor DLL geladen).");
                md.AppendLine();
                continue;
            }

            md.AppendLine($"## {key} — `{fullName}`");
            md.AppendLine();
            md.AppendLine($"BaseType: `{def.BaseType?.FullName ?? "<none>"}` · Sealed: {def.IsSealed} · Abstract: {def.IsAbstract}");
            md.AppendLine();

            // Constructors
            var ctors = def.Methods.Where(m => m.IsConstructor && !m.IsStatic).ToList();
            if (ctors.Count > 0)
            {
                md.AppendLine("### Constructors");
                md.AppendLine();
                md.AppendLine("```csharp");
                foreach (var c in ctors)
                {
                    var parms = string.Join(", ", c.Parameters.Select(p => $"{p.ParameterType.FullName} {p.Name}"));
                    md.AppendLine($"// {c.Attributes}");
                    md.AppendLine($"new {key}({parms});");
                }
                md.AppendLine("```");
                md.AppendLine();
            }

            // Public + protected methods (cap at 20 most-relevant)
            var methods = def.Methods
                .Where(m => !m.IsConstructor && (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly))
                .OrderBy(m => m.Name)
                .ToList();

            md.AppendLine($"### Public/Protected Methods ({methods.Count} total)");
            md.AppendLine();
            md.AppendLine("| Return | Name | Params | Static | Notes |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (var m in methods.Take(40))
            {
                var ret = m.ReturnType.FullName;
                var parms = string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                var notes = new List<string>();
                if (m.IsStatic) notes.Add("static");
                if (m.IsVirtual) notes.Add("virtual");
                if (m.IsAbstract) notes.Add("abstract");
                if (m.IsFinal) notes.Add("final");
                if (m.HasGenericParameters) notes.Add($"generic({m.GenericParameters.Count})");
                if (m.IsPInvokeImpl) notes.Add("pinvoke");
                var note = string.Join(" · ", notes);
                md.AppendLine($"| `{ret}` | `{m.Name}` | `{parms}` | {(m.IsStatic ? "✅" : "")} | {note} |");
            }
            md.AppendLine();

            // Public + protected properties/fields (statisch oder Instanz)
            var props = def.Properties
                .Where(p => (p.GetMethod != null && (p.GetMethod.IsPublic || p.GetMethod.IsFamily))
                         || (p.SetMethod != null && (p.SetMethod.IsPublic || p.SetMethod.IsFamily)))
                .ToList();
            if (props.Count > 0)
            {
                md.AppendLine($"### Public/Protected Properties ({props.Count} total)");
                md.AppendLine();
                md.AppendLine("| Type | Name | Get | Set | Static |");
                md.AppendLine("|---|---|---|---|---|");
                foreach (var p in props)
                {
                    var type = p.PropertyType.FullName;
                    var hasGet = p.GetMethod != null && (p.GetMethod.IsPublic || p.GetMethod.IsFamily);
                    var hasSet = p.SetMethod != null && (p.SetMethod.IsPublic || p.SetMethod.IsFamily);
                    var st = (p.GetMethod?.IsStatic ?? false) || (p.SetMethod?.IsStatic ?? false);
                    md.AppendLine($"| `{type}` | `{p.Name}` | {(hasGet ? "✓" : "")} | {(hasSet ? "✓" : "")} | {(st ? "✅" : "")} |");
                }
                md.AppendLine();
            }

            // Sub-Classes (nur erste 10 / relevante)
            var nestedAndDerived = assembly.MainModule.Types
                .Where(t => t.BaseType != null && t.BaseType.FullName == fullName)
                .Take(15)
                .Select(t => t.FullName)
                .ToList();
            if (nestedAndDerived.Count > 0)
            {
                md.AppendLine($"### Derived Types (sample of {nestedAndDerived.Count})");
                md.AppendLine();
                foreach (var n in nestedAndDerived) md.AppendLine($"- `{n}`");
                md.AppendLine();
            }
        }

        // 2) Heuristik-Sweeps für zentrale Spike-Pflicht-Methoden (Phase 1.3 / 3.2 / 5.2 / 6.2)
        md.AppendLine("## Spike-Pflicht-Heuristik-Sweeps");
        md.AppendLine();
        SweepHeuristic(md, assembly,
            "TryStartCastOn / TryCastShot / Launch",
            new[] { "TryStartCastOn", "TryCastShot", "Launch" });
        SweepHeuristic(md, assembly,
            "Temperature-Readout (1.6-API)",
            new[] { "GenTemperature", "RoomTemperature", "TemperatureAtCell", "OutdoorTemperature" });
        SweepHeuristic(md, assembly,
            "Burning/Refuelable/Fuel",
            new[] { "IsBurning", "Fuel", "FuelPercent", "ConsumeFuel", "Refuel" });
        SweepHeuristic(md, assembly,
            "LineOfSight (1.6-API)",
            new[] { "LineOfSight", "LineOfSightTo", "VisibleTo" });
        SweepHeuristic(md, assembly,
            "Pawn-Bauabschluss-Hooks",
            new[] { "FrameCompleted", "FinishBlueprint", "InstallBlueprint", "Notify_IterationCompleted" });

        // 3) Identitäts-Verifikation: RimWorld-Version + DLL-Hash
        var asmBytes = File.ReadAllBytes(AsmPath);
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(asmBytes);
        md.AppendLine("## Identität");
        md.AppendLine();
        md.AppendLine($"- Datei: `{AsmPath}`");
        md.AppendLine($"- Größe: {asmBytes.Length:N0} Bytes");
        md.AppendLine($"- SHA-256: `{Convert.ToHexString(hash)}`");
        md.AppendLine($"- Erfasst am: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        md.AppendLine();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath, md.ToString());
        Console.WriteLine($"[spike] wrote {outPath} ({md.Length:N0} chars)");

        return 0;
    }

    /// <summary>
    /// Heuristischer Sweep: enumeriert die ersten 20 öffentlichen Methoden mit einem bestimmten Namen
    /// in *allen* Typen der Assembly. Wird genutzt um zu zeigen, welche Member in 1.6 existieren.
    /// </summary>
    private static void SweepHeuristic(StringBuilder md, AssemblyDefinition asm, string title, string[] needles)
    {
        md.AppendLine($"### {title}");
        md.AppendLine();
        foreach (var needle in needles)
        {
            var hits = new List<(string DeclType, string ReturnType, string Params)>();
            foreach (var t in asm.MainModule.Types)
            {
                foreach (var m in t.Methods)
                {
                    if (!m.IsPublic && !m.IsFamily) continue;
                    if (m.IsConstructor) continue;
                    if (m.Name != needle) continue;

                    hits.Add((t.FullName!, m.ReturnType.Name, string.Join(", ", m.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))));
                    if (hits.Count >= 12) break;
                }
                if (hits.Count >= 12) break;
            }
            md.AppendLine($"- `{needle}`: {hits.Count} Treffer");
            foreach (var hit in hits.Take(8))
            {
                md.AppendLine($"  - `{hit.DeclType}.{needle}({hit.Params}) -> {hit.ReturnType}`");
            }
        }
        md.AppendLine();
    }
}
