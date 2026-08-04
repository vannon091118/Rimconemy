// Phase 8.3 spike — enumeriert Bauabschluss-Hooks in 1.6 Assembly.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mono.Cecil;

internal static class Phase8Construction
{
    private const string AsmPath = "/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll";

    public static int Main(string[] args)
    {
        var outPath = args.Length >= 1 ? args[0]
            : Path.Combine("tools", "inspect", "phase-8.3-construction-hooks.raw.md");

        using var raw = File.OpenRead(AsmPath);
        var asm = AssemblyDefinition.ReadAssembly(raw, new ReaderParameters { InMemory = true });

        var md = new StringBuilder();
        md.AppendLine("# Phase 8.3 Spike-Rohdaten: 1.6-Bauabschluss-Hooks");
        md.AppendLine();
        md.AppendLine($"Quelle: `{AsmPath}` · Datum: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        md.AppendLine();

        var candidates = new[]
        {
            "MakeFinished", "FinishConstruction", "CompleteConstruction",
            "SpawnFinished", "ConstructionCompleted", "Notify_BuildingComplete",
            "FrameSpawned", "FrameComplete", "FinishFrame", "SpawnFrame",
            "BuildingFrame", "Blueprint_Building", "GenConstruct"
        };

        md.AppendLine("## 1) Heuristik-Sweep: Namens-Kandidaten");
        md.AppendLine();
        foreach (var needle in candidates)
        {
            var hits = new List<string>();
            foreach (var t in asm.MainModule.Types)
            {
                foreach (var m in t.Methods)
                {
                    if (!m.IsPublic && !m.IsFamily) continue;
                    if (m.IsConstructor) continue;
                    if (m.Name != needle) continue;

                    var sig = $"{t.FullName}.{m.Name}({string.Join(", ", m.Parameters.Select(p => p.ParameterType.Name + " " + p.Name))}) -> {m.ReturnType.Name}";
                    hits.Add(sig);
                    if (hits.Count >= 12) break;
                }
                if (hits.Count >= 12) break;
            }
            md.AppendLine($"- `{needle}`: {hits.Count} Treffer");
            foreach (var h in hits.Take(8)) md.AppendLine($"  - `{h}`");
        }
        md.AppendLine();

        // Targeted class enumeration: GenConstruct, Blueprint_Build, Frame, Building
        string[] targetTypes = new[]
        {
            "RimWorld.GenConstruct",
            "Verse.GenSpawn",
            "Verse.Blueprint",
            "RimWorld.Blueprint_Build",
            "RimWorld.Frame",
            "Verse.Building",
        };

        md.AppendLine("## 2) Targeted Class-Enumerierung");
        md.AppendLine();
        foreach (var tn in targetTypes)
        {
            var def = asm.MainModule.Types.FirstOrDefault(t => t.FullName == tn)
                   ?? asm.MainModule.Types.FirstOrDefault(t => t.Name == tn.Split('.').Last());
            if (def == null)
            {
                md.AppendLine($"### {tn} — NICHT GEFUNDEN");
                md.AppendLine();
                continue;
            }

            md.AppendLine($"### {tn} — vorhanden");
            var pipe = def.Methods
                .Where(m => !m.IsConstructor && (m.IsPublic || m.IsFamily))
                .OrderBy(m => m.Name)
                .Select(m =>
                {
                    var parms = string.Join(", ", m.Parameters.Select(p => p.ParameterType.Name + " " + p.Name));
                    var flags = new List<string>();
                    if (m.IsStatic) flags.Add("static");
                    if (m.IsVirtual) flags.Add("virtual");
                    if (m.IsAbstract) flags.Add("abstract");
                    if (m.IsFinal) flags.Add("final");
                    var flagStr = flags.Count > 0 ? " · " + string.Join(" · ", flags) : "";
                    return $"`{m.ReturnType.Name} {m.Name}({parms})`{flagStr}";
                })
                .ToList();

            foreach (var s in pipe.Take(40)) md.AppendLine("- " + s);
            md.AppendLine();
        }

        // 3) Heat-Analyse: Spawnen vs. Konstruieren — beide Vanilla-API-Pfade
        md.AppendLine("## 3) Heat-Analyse: GenSpawn vs. GenConstruct");
        md.AppendLine();
        var genConstruct = asm.MainModule.Types.FirstOrDefault(t => t.FullName == "RimWorld.GenConstruct");
        if (genConstruct != null)
        {
            md.AppendLine($"### RimWorld.GenConstruct — {genConstruct.Methods.Count} Methods total");
            md.AppendLine();
            foreach (var m in genConstruct.Methods.Where(m => m.IsStatic && m.IsPublic)
                                                 .OrderBy(m => m.Name))
            {
                md.AppendLine($"- `{m.ReturnType.Name} {m.Name}({string.Join(", ", m.Parameters.Select(p => p.ParameterType.Name + " " + p.Name))})`");
            }
        }
        else
        {
            md.AppendLine("RimWorld.GenConstruct fehlt in der Assembly.");
        }

        var args2 = sha256(asm).Substring(0, 16);
        md.AppendLine();
        md.AppendLine($"## Identitäts-Hash (Trunc.): `{args2}`");

        File.WriteAllText(outPath, md.ToString());
        Console.WriteLine($"[spike-8.3] wrote {outPath} ({md.Length:N0} chars)");
        return 0;
    }

    private static string sha256(AssemblyDefinition asm)
    {
        using var sha = SHA256.Create();
        var bytes = File.ReadAllBytes(AsmPath);
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }
}
