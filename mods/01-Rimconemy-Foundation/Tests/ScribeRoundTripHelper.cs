using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using Verse;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Test-only Scribe roundtrip for RimWorld 1.6.
    ///
    /// RimWorld 1.6 exposes Scribe.saver/loader/mode as direct static fields.
    /// ScribeSaver.InitSaving and ScribeLoader.InitLoading are file-path APIs,
    /// so this helper creates the normal parameterless objects and injects the
    /// in-memory stream/XML state into their private fields. This exercises the
    /// same Scribe_Values/Scribe_Collections/Scribe_Deep calls as a file save,
    /// without requiring Current.Game or touching the filesystem.
    ///
    /// The helper is deliberately strict: it returns false on any reflection or
    /// Scribe failure. Callers must not fall back to direct migration, otherwise
    /// a logic-only test could be misreported as a stream roundtrip.
    /// </summary>
    public static class ScribeRoundTripHelper
    {
        private const BindingFlags InstanceFields =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private const BindingFlags StaticFields =
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        public static bool RoundTrip<T>(T instance) where T : IExposable
        {
            if (instance == null)
                return false;

            FieldInfo modeField = typeof(Scribe).GetField("mode", StaticFields);
            FieldInfo saverField = typeof(Scribe).GetField("saver", StaticFields);
            FieldInfo loaderField = typeof(Scribe).GetField("loader", StaticFields);
            if (modeField == null || saverField == null || loaderField == null)
            {
                Log.Error("[Rimconemy.Foundation] ScribeRoundTripHelper: RimWorld 1.6 Scribe fields unavailable.");
                return false;
            }

            object originalMode = modeField.GetValue(null);
            object originalSaver = saverField.GetValue(null);
            object originalLoader = loaderField.GetValue(null);

            try
            {
                Type saverType = typeof(Scribe).Assembly.GetType("Verse.ScribeSaver");
                Type loaderType = typeof(Scribe).Assembly.GetType("Verse.ScribeLoader");
                if (saverType == null || loaderType == null)
                    return false;

                FieldInfo saveStreamField = saverType.GetField("saveStream", InstanceFields);
                FieldInfo writerField = saverType.GetField("writer", InstanceFields);
                FieldInfo currentXmlField = loaderType.GetField("curXmlParent", InstanceFields);
                FieldInfo currentPathField = loaderType.GetField("curPathRelToParent", InstanceFields);
                if (saveStreamField == null || writerField == null || currentXmlField == null)
                    return false;

                object saver = Activator.CreateInstance(saverType, true);
                object loader = Activator.CreateInstance(loaderType, true);
                if (saver == null || loader == null)
                    return false;

                byte[] xmlBytes;
                using (var stream = new MemoryStream())
                {
                    var settings = new XmlWriterSettings
                    {
                        Encoding = new UTF8Encoding(false),
                        Indent = true,
                        OmitXmlDeclaration = false,
                        CloseOutput = false,
                    };

                    using (XmlWriter writer = XmlWriter.Create(stream, settings))
                    {
                        // These are the fields ScribeSaver.InitSaving would set,
                        // except InitSaving only accepts a filesystem path.
                        saveStreamField.SetValue(saver, stream);
                        writerField.SetValue(saver, writer);

                        modeField.SetValue(null, LoadSaveMode.Saving);
                        saverField.SetValue(null, saver);

                        if (!Scribe.EnterNode("RimconemyTest"))
                            return false;
                        instance.ExposeData();
                        Scribe.ExitNode();

                        writer.WriteEndDocument();
                        writer.Flush();
                    }

                    xmlBytes = stream.ToArray();
                }

                if (xmlBytes.Length == 0)
                    return false;

                var document = new XmlDocument();
                using (var input = new MemoryStream(xmlBytes))
                    document.Load(input);
                if (document.DocumentElement == null)
                    return false;

                // InitLoading normally parses a file into an XmlDocument and
                // stores DocumentElement in curXmlParent. Inject that exact
                // post-init state, avoiding the path-only InitLoading API.
                currentXmlField.SetValue(loader, document.DocumentElement);
                if (currentPathField != null)
                    currentPathField.SetValue(loader, null);

                modeField.SetValue(null, LoadSaveMode.LoadingVars);
                loaderField.SetValue(null, loader);
                instance.ExposeData();

                modeField.SetValue(null, LoadSaveMode.PostLoadInit);
                instance.ExposeData();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[Rimconemy.Foundation] ScribeRoundTripHelper failed: " +
                    ex.GetType().Name + ": " + ex.Message);
                return false;
            }
            finally
            {
                modeField.SetValue(null, originalMode);
                saverField.SetValue(null, originalSaver);
                loaderField.SetValue(null, originalLoader);
            }
        }
    }
}
