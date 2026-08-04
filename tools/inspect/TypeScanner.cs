using System;
using System.IO;
using System.Linq;
using Mono.Cecil;

internal static class TypeScanner
{
    public static int Main(string[] args)
    {
        using var raw = File.OpenRead("/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll");
        var asm = AssemblyDefinition.ReadAssembly(raw, new ReaderParameters { InMemory = true });
        var needles = new[] {
            "ScenarioBase","ScenPart","GameComponent","MapComponent","WorldComponent",
            "ThingComp","IncidentWorker","RecipeWorker","Designator","GenSight",
            "FogGrid","PawnGenerator","ResearchManager","CompRefuelable","CompGlower",
            "ScenPart_StartingThing_Defined","ScenPart_ConfigPage_ConfigureStartingPawns",
            "ScenPart_RimconemyStart","TryStartCastOn","TryCastShot","RoomTemperature"
        };
        Console.WriteLine("--- Types containing any needle ---");
        foreach (var needle in needles)
        {
            var hits = asm.MainModule.Types
                .Where(t => (t.FullName ?? "").Contains(needle))
                .Select(t => (t.FullName, "BaseType=" + (t.BaseType?.FullName ?? "?")))
                .Take(8).ToList();
            Console.WriteLine($"[{needle}] hits={hits.Count}");
            foreach (var h in hits) Console.WriteLine($"    {h.FullName} | {h.Item2}");
        }
        Console.WriteLine($"Total types: {asm.MainModule.Types.Count}");
        return 0;
    }
}
