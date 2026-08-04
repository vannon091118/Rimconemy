using System;
using System.IO;
using System.Reflection;
using System.Xml;
using Verse;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Phase-Foundation (2026-08-04): Echte Scribe-File-Roundtrip-Helper — treibt
    /// <c>Scribe.mode</c>, <c>Scribe.saver</c> und <c>Scribe.loader</c> via
    /// Reflection so, dass ein voller Save-via-MemoryStream → Load-from-MemoryStream
    /// Scribe-Zyklus OHNE aktive Game-Session möglich ist.
    ///
    /// Nutzung:
    /// <code>
    /// var state = new SomeExposable();
    /// state.SchemaVersion = 0;
    /// ScribeRoundTripHelper.RoundTrip(state);
    /// Assert.AreEqual(1, state.SchemaVersion);
    /// </code>
    ///
    /// WARNUNG: Diese Klasse manipuliert globalen Scribe-State.
    /// Sie MUSS in einem strikten try/finally laufen; der Helper stellt
    /// den Original-Zustand nach der Operation wieder her. Bei unerwarteten
    /// Exceptions kann der Scribe-State beschädigt zurückbleiben — der Helper
    /// ist ausschließlich für Tests gedacht, NIEMALS in Production-Code.
    ///
    /// Owner: Foundation (Paket 01), test-only.
    /// </summary>
    public static class ScribeRoundTripHelper
    {
        /// <summary>
        /// Führt einen vollständigen Scribe-Roundtrip auf demselben Objekt durch.
        /// Speichert den aktuellen Zustand per MemoryStream, lädt ihn zurück
        /// und ruft PostLoadInit (inkl. MigrateIfNeeded) auf.
        /// </summary>
        /// <typeparam name="T">Ein IExposable-Objekt mit parameterlosem Konstruktor (via new()).</typeparam>
        /// <param name="instance">Das Objekt, auf dem der Roundtrip ausgeführt wird.</param>
        /// <returns>True wenn der Roundtrip erfolgreich durchlief.</returns>
        public static bool RoundTrip<T>(T instance) where T : IExposable
        {
            if (instance == null) return false;

            // ── Sichere Original-Scribe-State ─────────────
            object savedMode = Scribe.mode;
            object savedSaver = null;
            object savedLoader = null;

            FieldInfo saverField = null;
            FieldInfo loaderField = null;

            try
            {
                // Scribe.saver/loader Backing-Fields entdecken (RimWorld 1.6)
                BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
                saverField = typeof(Scribe).GetField("<saver>k__BackingField", flags)
                    ?? typeof(Scribe).GetField("saverInternal", flags);

                // Als Fallback: Property mit private Setter via SetValue
                PropertyInfo saverProp = typeof(Scribe).GetProperty("saver",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                loaderField = typeof(Scribe).GetField("<loader>k__BackingField", flags)
                    ?? typeof(Scribe).GetField("loaderInternal", flags);

                PropertyInfo loaderProp = typeof(Scribe).GetProperty("loader",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (saverField != null)
                    savedSaver = saverField.GetValue(null);
                if (loaderField != null)
                    savedLoader = loaderField.GetValue(null);

                // ── 1. Save-Phase: Objekt → MemoryStream ──

                // Get internal ScribeSaver type from Verse assembly
                Type saverType = typeof(Scribe).Assembly.GetType("Verse.ScribeSaver");
                Type loaderType = typeof(Scribe).Assembly.GetType("Verse.ScribeLoader");

                if (saverType == null || loaderType == null)
                {
                    Log.Message("[Rimconemy.Foundation] ScribeRoundTripHelper: Cannot find ScribeSaver/ScribeLoader types in Verse assembly.");
                    return false;
                }

                using (var memStream = new MemoryStream())
                {
                    // Create XmlWriter
                    var xmlSettings = new XmlWriterSettings
                    {
                        Indent = true,
                        OmitXmlDeclaration = false,
                        Encoding = System.Text.Encoding.UTF8,
                        CloseOutput = false,
                    };
                    XmlWriter xmlWriter = XmlWriter.Create(memStream, xmlSettings);

                    // Construct ScribeSaver via Reflection
                    object saver = ConstructScribeSaver(saverType, xmlWriter);
                    if (saver == null)
                    {
                        Log.Message("[Rimconemy.Foundation] ScribeRoundTripHelper: Cannot construct ScribeSaver.");
                        xmlWriter.Dispose();
                        return false;
                    }

                    // Set Scribe.mode = Saving
                    typeof(Scribe).GetField("<mode>k__BackingField",
                        BindingFlags.Static | BindingFlags.NonPublic)
                        ?.SetValue(null, LoadSaveMode.Saving);

                    // Set Scribe.saver
                    if (saverField != null)
                        saverField.SetValue(null, saver);
                    else if (saverProp != null && saverProp.CanWrite)
                        saverProp.SetValue(null, saver);

                    // Enter root XML node
                    Scribe.EnterNode("RimconemyTest");

                    // SAVE — ExposeData writes all fields
                    instance.ExposeData();

                    Scribe.ExitNode();
                    xmlWriter.Flush();
                    xmlWriter.Close();

                    // ── 2. Load-Phase: MemoryStream → Objekt ──

                    // Get XML bytes and parse as XmlDocument
                    byte[] xmlBytes = memStream.ToArray();
                    var xmlDoc = new XmlDocument();
                    using (var readStream = new MemoryStream(xmlBytes))
                    {
                        xmlDoc.Load(readStream);
                    }

                    // Construct ScribeLoader via Reflection
                    object loader = ConstructScribeLoader(loaderType, xmlDoc);
                    if (loader == null)
                    {
                        Log.Message("[Rimconemy.Foundation] ScribeRoundTripHelper: Cannot construct ScribeLoader.");
                        return false;
                    }

                    // Set Scribe.mode = LoadingVars
                    typeof(Scribe).GetField("<mode>k__BackingField",
                        BindingFlags.Static | BindingFlags.NonPublic)
                        ?.SetValue(null, LoadSaveMode.LoadingVars);

                    // Set Scribe.loader
                    if (loaderField != null)
                        loaderField.SetValue(null, loader);
                    else if (loaderProp != null && loaderProp.CanWrite)
                        loaderProp.SetValue(null, loader);

                    // LOAD — ExposeData reads all fields from XmlDocument
                    instance.ExposeData();

                    // ── 3. PostLoadInit ─────────────────────
                    typeof(Scribe).GetField("<mode>k__BackingField",
                        BindingFlags.Static | BindingFlags.NonPublic)
                        ?.SetValue(null, LoadSaveMode.PostLoadInit);

                    instance.ExposeData();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.Foundation] ScribeRoundTripHelper RoundTrip failed: " +
                    ex.GetType().Name + ": " + ex.Message);
                return false;
            }
            finally
            {
                // ── Restore original Scribe-State ─────────
                // Scribe.mode-Restore: Fallback wenn BackingField nicht gefunden wird.
                var modeField = typeof(Scribe).GetField("<mode>k__BackingField",
                    BindingFlags.Static | BindingFlags.NonPublic)
                    ?? typeof(Scribe).GetField("modeInternal",
                        BindingFlags.Static | BindingFlags.NonPublic);
                if (modeField != null)
                    modeField.SetValue(null, savedMode);

                if (saverField != null)
                {
                    saverField.SetValue(null, savedSaver);
                }

                if (loaderField != null)
                {
                    loaderField.SetValue(null, savedLoader);
                }
            }
        }

        /// <summary>
        /// Versucht, einen ScribeSaver zu konstruieren. Fallback-Kette:
        /// 1. Constructor(XmlWriter)
        /// 2. Parameterlos + init-Methode aufrufen
        /// </summary>
        private static object ConstructScribeSaver(Type saverType, XmlWriter writer)
        {
            try
            {
                // Path 1: Constructor taking XmlWriter
                ConstructorInfo ctor = saverType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(XmlWriter) }, null);
                if (ctor != null)
                    return ctor.Invoke(new object[] { writer });

                // Path 2: Parameterless constructor + InitSaving method.
                // InitSaving expects either string filePath oder XmlWriter —
                // wir prüfen den Parameter-Typ bevor wir aufrufen.
                ctor = saverType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                if (ctor != null)
                {
                    object saver = ctor.Invoke(null);
                    MethodInfo initMethod = saverType.GetMethod("InitSaving",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (initMethod != null)
                    {
                        var parms = initMethod.GetParameters();
                        if (parms.Length == 1)
                        {
                            if (parms[0].ParameterType == typeof(XmlWriter))
                                initMethod.Invoke(saver, new object[] { writer });
                            else if (parms[0].ParameterType == typeof(string))
                            {
                                // string-Pfad: XmlWriter ist nicht direkt nutzbar,
                                // aber wir können den konstruierten Saver trotzdem
                                // verwenden (RimWorld 1.6 toleriert null-initialisierten
                                // Saver für Scribe_Values/Collections-only).
                            }
                        }
                    }
                    return saver;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.Foundation] ScribeSaver construction failed: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Versucht, einen ScribeLoader zu konstruieren.
        /// Constructor(XmlDocument) ist die 1.6-Standard-Signatur.
        /// </summary>
        private static object ConstructScribeLoader(Type loaderType, XmlDocument doc)
        {
            try
            {
                // Path 1: Constructor taking XmlDocument
                ConstructorInfo ctor = loaderType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(XmlDocument) }, null);
                if (ctor != null)
                    return ctor.Invoke(new object[] { doc });

                // Path 2: Parameterless constructor
                ctor = loaderType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                if (ctor != null)
                    return ctor.Invoke(null);

                // Path 3: static FromXml / FromFile method (FromXml zuerst,
                // da es XmlDocument erwartet; FromFile erwartet string).
                MethodInfo fromMethod = loaderType.GetMethod("FromXml",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? loaderType.GetMethod("FromFile",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (fromMethod != null && fromMethod.GetParameters().Length == 1)
                {
                    var fparm = fromMethod.GetParameters()[0];
                    object arg = fparm.ParameterType == typeof(XmlDocument)
                        ? (object)doc
                        : fparm.ParameterType == typeof(string)
                            ? (object)doc.OuterXml
                            : (object)doc;
                    object result = fromMethod.Invoke(null, new object[] { arg });
                    if (result != null) return result;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.Foundation] ScribeLoader construction failed: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
            return null;
        }
    }
}
