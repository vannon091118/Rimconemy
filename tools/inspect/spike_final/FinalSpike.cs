using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;

internal static class Program
{
    private const string AsmPath = "/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll";

    private static readonly string[] TargetTypes = new[]
    {
        "Verse.CameraDriver",
        "Verse.WeatherDef",
        "Verse.LetterStack",
        "Verse.Window",
        "Verse.KeyBindingDef",
        "Verse.ContentFinder`1",
        "RimWorld.MainTabDef",
        "RimWorld.ScenPart_GameStartDialog",
        "RimWorld.ScenPart_GameCondition",
        "RimWorld.ScenPart_PermaGameCondition",
        "Verse.LetterStack",
        "Verse.LetterDef",
        "Verse.LookTargets",
        "Verse.TaggedString",
        "RimWorld.IncidentWorker",
        "Verse.GameComponent",
        "Verse.Def",
    };

    private static int Main()
    {
        Console.WriteLine("[spike] reading Assembly-CSharp.dll");
        if (!File.Exists(AsmPath)) { Console.Error.WriteLine("DLL not found"); return 2; }

        using var raw = File.OpenRead(AsmPath);
        var asm = AssemblyDefinition.ReadAssembly(raw, new ReaderParameters { ReadWrite = false, InMemory = true });

        var md = new StringBuilder();
        md.AppendLine("# Targeted API Spike — Critical Verification");
        md.AppendLine();
        md.AppendLine($"Source: {AsmPath}");
        md.AppendLine();

        foreach (var target in TargetTypes)
        {
            var baseName = target.Contains("`") ? target.Split('`')[0] : target;
            var def = asm.MainModule.Types.FirstOrDefault(t => t.FullName == target)
                     ?? asm.MainModule.Types.FirstOrDefault(t => t.Name == baseName);

            if (def == null)
            {
                md.AppendLine($"## {target} — NOT FOUND");
                md.AppendLine();
                continue;
            }

            md.AppendLine($"## {def.FullName}");
            md.AppendLine($"BaseType: {def.BaseType?.FullName ?? "<none>"} · Sealed: {def.IsSealed} · Abstract: {def.IsAbstract}");
            md.AppendLine();

            // Constructors
            var ctors = def.Methods.Where(m => m.IsConstructor && !m.IsStatic).ToList();
            if (ctors.Count > 0)
            {
                md.AppendLine("### Constructors");
                md.AppendLine("```csharp");
                foreach (var c in ctors)
                {
                    var parms = string.Join(", ", c.Parameters.Select(p => p.ParameterType.FullName + " " + p.Name));
                    md.AppendLine("new " + def.Name + "(" + parms + ");");
                }
                md.AppendLine("```");
                md.AppendLine();
            }

            // Methods
            var methods = def.Methods
                .Where(m => !m.IsConstructor && (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly))
                .OrderBy(m => m.Name)
                .Take(50)
                .ToList();

            if (methods.Count > 0)
            {
                md.AppendLine("### Public/Protected Methods");
                md.AppendLine("| Return | Name | Params | Static | Notes |");
                md.AppendLine("|---|---|---|---|---|");
                foreach (var m in methods)
                {
                    var ret = m.ReturnType.FullName;
                    var parms = string.Join(", ", m.Parameters.Select(p => p.ParameterType.Name + " " + p.Name));
                    var notes = new List<string>();
                    if (m.IsStatic) notes.Add("static");
                    if (m.IsVirtual) notes.Add("virtual");
                    if (m.IsAbstract) notes.Add("abstract");
                    if (m.IsFinal) notes.Add("final");
                    md.AppendLine("| " + ret + " | " + m.Name + " | " + parms + " | " + (m.IsStatic ? "YES" : "") + " | " + string.Join(" ", notes) + " |");
                }
                md.AppendLine();
            }

            // Properties
            var props = def.Properties
                .Where(p => (p.GetMethod != null && (p.GetMethod.IsPublic || p.GetMethod.IsFamily))
                         || (p.SetMethod != null && (p.SetMethod.IsPublic || p.SetMethod.IsFamily)))
                .ToList();
            if (props.Count > 0)
            {
                md.AppendLine("### Public/Protected Properties");
                md.AppendLine("| Type | Name | Get | Set | Static |");
                md.AppendLine("|---|---|---|---|---|");
                foreach (var p in props)
                {
                    var type = p.PropertyType.FullName;
                    var hasGet = p.GetMethod != null && (p.GetMethod.IsPublic || p.GetMethod.IsFamily);
                    var hasSet = p.SetMethod != null && (p.SetMethod.IsPublic || p.SetMethod.IsFamily);
                    var st = (p.GetMethod?.IsStatic ?? false) || (p.SetMethod?.IsStatic ?? false);
                    md.AppendLine("| " + type + " | " + p.Name + " | " + (hasGet ? "yes" : "") + " | " + (hasSet ? "yes" : "") + " | " + (st ? "YES" : "") + " |");
                }
                md.AppendLine();
            }

            // Special: WeatherDef - all fields
            if (def.Name == "WeatherDef")
            {
                var allFields = def.Fields.Where(f => f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly).ToList();
                md.AppendLine("### All Fields (WeatherDef)");
                md.AppendLine("| Type | Name |");
                md.AppendLine("|---|---|");
                foreach (var f in allFields)
                {
                    md.AppendLine("| " + f.FieldType.FullName + " | " + f.Name + " |");
                }
                md.AppendLine();

                var allProps = def.Properties.ToList();
                md.AppendLine("### All Properties (WeatherDef)");
                md.AppendLine("| Type | Name | Get | Set |");
                md.AppendLine("|---|---|---|---|");
                foreach (var p in allProps)
                {
                    var g = p.GetMethod?.IsPublic == true || p.GetMethod?.IsFamily == true;
                    var s = p.SetMethod?.IsPublic == true || p.SetMethod?.IsFamily == true;
                    md.AppendLine("| " + p.PropertyType.FullName + " | " + p.Name + " | " + (g ? "yes" : "") + " | " + (s ? "yes" : "") + " |");
                }
                md.AppendLine();
            }

            // LetterStack: ReceiveLetter overloads
            if (def.Name == "LetterStack")
            {
                var recvMethods = def.Methods.Where(m => m.Name == "ReceiveLetter" && (m.IsPublic || m.IsFamily)).ToList();
                md.AppendLine("### ReceiveLetter Overloads");
                md.AppendLine("| Return | Params | Static | Notes |");
                md.AppendLine("|---|---|---|---|");
                foreach (var m in recvMethods)
                {
                    var parms = string.Join(", ", m.Parameters.Select(p => p.ParameterType.FullName + " " + p.Name));
                    var notes = new List<string>();
                    if (m.IsStatic) notes.Add("static");
                    if (m.IsVirtual) notes.Add("virtual");
                    md.AppendLine("| " + m.ReturnType.FullName + " | " + parms + " | " + (m.IsStatic ? "YES" : "") + " | " + string.Join(" ", notes) + " |");
                }
                md.AppendLine();
            }

            // Window: all props
            if (def.Name == "Window")
            {
                var propsAll = def.Properties.Where(p => p.GetMethod?.IsPublic == true || p.GetMethod?.IsFamily == true).ToList();
                md.AppendLine("### All Public/Protected Properties (Window)");
                md.AppendLine("| Type | Name | Get | Set | Static |");
                md.AppendLine("|---|---|---|---|---|");
                foreach (var p in propsAll)
                {
                    var g = p.GetMethod?.IsPublic == true || p.GetMethod?.IsFamily == true;
                    var s = p.SetMethod?.IsPublic == true || p.SetMethod?.IsFamily == true;
                    var st = (p.GetMethod?.IsStatic ?? false) || (p.SetMethod?.IsStatic ?? false);
                    md.AppendLine("| " + p.PropertyType.FullName + " | " + p.Name + " | " + (g ? "yes" : "") + " | " + (s ? "yes" : "") + " | " + (st ? "YES" : "") + " |");
                }
                md.AppendLine();
            }

            // CameraDriver: JumpTo, SetRootPosAndSize
            if (def.Name == "CameraDriver")
            {
                var camMethods = def.Methods.Where(m => m.Name.Contains("Jump") || m.Name.Contains("Root") || m.Name.Contains("Pos") || m.Name.Contains("Size") || m.Name.Contains("Move")).ToList();
                md.AppendLine("### CameraDriver Relevant Methods");
                md.AppendLine("| Return | Name | Params | Static | Notes |");
                md.AppendLine("|---|---|---|---|---|");
                foreach (var m in camMethods)
                {
                    var ret = m.ReturnType.FullName;
                    var parms = string.Join(", ", m.Parameters.Select(p => p.ParameterType.Name + " " + p.Name));
                    var notes = new List<string>();
                    if (m.IsStatic) notes.Add("static");
                    if (m.IsVirtual) notes.Add("virtual");
                    md.AppendLine("| " + ret + " | " + m.Name + " | " + parms + " | " + (m.IsStatic ? "YES" : "") + " | " + string.Join(" ", notes) + " |");
                }
                md.AppendLine();
            }

            // IncidentWorker: SendStandardLetter overloads
            if (def.Name == "IncidentWorker")
            {
                var sendMethods = def.Methods.Where(m => m.Name.Contains("Send") && m.IsPublic).ToList();
                md.AppendLine("### Send* Methods (IncidentWorker)");
                md.AppendLine("| Return | Name | Params | Static | Notes |");
                md.AppendLine("|---|---|---|---|---|");
                foreach (var m in sendMethods)
                {
                    var ret = m.ReturnType.FullName;
                    var parms = string.Join(", ", m.Parameters.Select(p => p.ParameterType.Name + " " + p.Name));
                    var notes = new List<string>();
                    if (m.IsStatic) notes.Add("static");
                    if (m.IsVirtual) notes.Add("virtual");
                    md.AppendLine("| " + ret + " | " + m.Name + " | " + parms + " | " + (m.IsStatic ? "YES" : "") + " | " + string.Join(" ", notes) + " |");
                }
                md.AppendLine();
            }

            md.AppendLine();
        }

        var outPath = "tools/inspect/api-spike-final.raw.md";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath, md.ToString());
        Console.WriteLine("[spike] wrote " + outPath);
        return 0;
    }
}
