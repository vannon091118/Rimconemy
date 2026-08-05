using System;
using System.Linq;
using Mono.Cecil;
internal static class ApiProbe
{
    public static int Main(string[] args)
    {
        using var raw = System.IO.File.OpenRead("/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll");
        var asm = AssemblyDefinition.ReadAssembly(raw, new ReaderParameters { InMemory = true });
        foreach (var name in new[] { "Verse.AI.Toil", "Verse.AI.JobDriver", "RimWorld.CompIngredients", "RimWorld.CompQuality", "RimWorld.CompArt", "RimWorld.CompCrafting", "RimWorld.Plant", "Verse.ThingDef" })
        {
            var t = asm.MainModule.Types.FirstOrDefault(x => x.FullName == name) ?? asm.MainModule.Types.FirstOrDefault(x => x.Name == name.Split('.').Last());
            Console.WriteLine("TYPE " + (t?.FullName ?? name));
            if (t == null) continue;
            foreach (var m in t.Methods.Where(m => !m.IsConstructor && (m.IsPublic || m.IsFamily)).OrderBy(m => m.Name)) Console.WriteLine(" M " + m.ReturnType.FullName + " " + m.Name + "(" + string.Join(", ", m.Parameters.Select(p => p.ParameterType.FullName + " " + p.Name)) + ")");
            foreach (var p in t.Properties.Where(p => p.GetMethod?.IsPublic == true || p.GetMethod?.IsFamily == true || p.SetMethod?.IsPublic == true || p.SetMethod?.IsFamily == true).OrderBy(p => p.Name)) Console.WriteLine(" P " + p.PropertyType.FullName + " " + p.Name);
            foreach (var f in t.Fields.Where(f => f.IsPublic || f.IsFamily).OrderBy(f => f.Name)) Console.WriteLine(" F " + f.FieldType.FullName + " " + f.Name);
        }
        return 0;
    }
}
