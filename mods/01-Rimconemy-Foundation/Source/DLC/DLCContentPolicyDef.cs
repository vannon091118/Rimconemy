using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.DLC
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    ///
    /// Phase-7 DLC-Content-Policy-Layer, Phase-2 Runtime-Layer (2026-08-04):
    /// Def-basierte Override-Schicht für DLCContentPolicy-Werte.
    ///
    /// ## Architektur-Vertrag
    ///
    /// Phase-1 (Hardcoded): die Werte in <see cref="DLCContentPolicy"/> sind
    /// statisch kompiliert und über Build-Zeit unveränderlich. Konsumenten
    /// fragen <see cref="DLCFilter.IsContentEnabled"/>.
    ///
    /// Phase-2 (Def-Override): diese Def-Klasse erlaubt es, einzelne
    /// Phase-1-Flags via XML umzuschreiben. Der Override wird zur Boot-Zeit
    /// vom <see cref="DLCPolicyConfig"/> aus dem DefDatabase gelesen und
    /// per Reflection auf die statischen Felder von DLCContentPolicy
    /// angewandt. Konsumenten sehen die überschriebenen Werte über die
    /// bestehende DLCFilter-Schnittstelle — kein Code-Change an Mod 02..05.
    ///
    /// ## Last-Writer-Wins
    ///
    /// Beliebig viele DLCContentPolicyDefs im DefDatabase sind erlaubt. Bei
    /// mehreren Definitionen gewinnt pro Field der zuletzt angewandte
    /// Override. Das ermöglicht schichtweise Konfigurations-Stacks
    /// (z.B. Foundation-Defaults → ContentPack-Overrides → Player-Overrides).
    ///
    /// ## Reflection-Schemata-Schlüssel
    ///
    /// Der PolicyPath-Format ist strikt "SubClass.Field" (z.B.
    /// "Anomaly.Shamblers"). Tippfehler oder ungültige Sub-Klassen werden
    /// in Log.Warning gemeldet und der fehlerhafte Eintrag wird ignoriert.
    /// Das ist fail-safe: ein falscher Eintrag kann nicht das Boot
    /// durchfallen lassen.
    /// </summary>
    public class DLCContentPolicyDef : Def
    {
        /// <summary>
        /// Liste der Override-Einträge. Jeder Eintrag setzt einen einzelnen
        /// bool-Wert in DLCContentPolicy.
        /// </summary>
        public List<DLCPolicyEntry> policies = new List<DLCPolicyEntry>();

        /// <summary>
        /// Wendet alle Einträge aus <see cref="policies"/> per Reflection auf
        /// <see cref="DLCContentPolicy"/> an. Idempotent: bei re-entry werden
        /// Werte einfach überschrieben.
        /// </summary>
        /// <returns>Anzahl erfolgreich überschriebener Felder.</returns>
        public int ApplyToPolicy()
        {
            if (policies == null)
            {
                return 0;
            }

            int applied = 0;
            int skipped = 0;
            var policyType = typeof(DLCContentPolicy);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

            foreach (var entry in policies)
            {
                if (entry == null) { skipped++; continue; }
                if (string.IsNullOrEmpty(entry.PolicyPath))
                {
                    skipped++;
                    continue;
                }

                var parts = entry.PolicyPath.Split('.');
                if (parts.Length != 2)
                {
                    Log.Warning(
                        $"[Rimconemy.Foundation] DLCPolicyConfig: def={defName} entry=" +
                        $"'{entry.PolicyPath}' has invalid path format (expected 'SubClass.Field'). " +
                        "Policy override skipped.");
                    skipped++;
                    continue;
                }

                var subClass = policyType.GetNestedType(parts[0], flags);
                if (subClass == null)
                {
                    Log.Warning(
                        $"[Rimconemy.Foundation] DLCPolicyConfig: def={defName} entry=" +
                        $"'{entry.PolicyPath}': unknown sub-class '{parts[0]}'. " +
                        $"Available: {string.Join(", ", GetSubClassNames(policyType))}. " +
                        "Policy override skipped.");
                    skipped++;
                    continue;
                }

                var field = subClass.GetField(parts[1], flags);
                if (field == null || field.FieldType != typeof(bool))
                {
                    Log.Warning(
                        $"[Rimconemy.Foundation] DLCPolicyConfig: def={defName} entry=" +
                        $"'{entry.PolicyPath}': field not found or not bool. " +
                        "Policy override skipped.");
                    skipped++;
                    continue;
                }

                try
                {
                    // static readonly fields CAN be set via reflection —
                    // the compiler emits them as static fields with
                    // a special "initonly" flag that SetValue bypasses.
                    field.SetValue(null, entry.Value);
                    applied++;
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        $"[Rimconemy.Foundation] DLCPolicyConfig: def={defName} entry=" +
                        $"'{entry.PolicyPath}': SetValue threw {ex.GetType().Name}: {ex.Message}. " +
                        "Policy override skipped.");
                    skipped++;
                }
            }

            Log.Message(
                $"[Rimconemy.Foundation] DLCPolicyConfig: def={defName} applied={applied} " +
                $"skipped={skipped} (last-writer-wins value applied to static fields).");

            return applied;
        }

        private static IEnumerable<string> GetSubClassNames(Type policyType)
        {
            foreach (var t in policyType.GetNestedTypes(BindingFlags.Public))
                yield return t.Name;
        }
    }

    /// <summary>
    /// Einzelner Eintrag in einem <see cref="DLCContentPolicyDef"/>.
    /// PolicyPath-Format: "SubClass.Field" (z.B. "Anomaly.Shamblers").
    /// Value wird per Reflection auf das DLCContentPolicy.SubClass.Field
    /// static-field gesetzt.
    /// </summary>
    public class DLCPolicyEntry
    {
        public string PolicyPath;
        public bool Value;
    }
}
