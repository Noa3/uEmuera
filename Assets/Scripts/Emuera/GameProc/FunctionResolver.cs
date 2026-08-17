using System;
using System.Collections.Generic;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Single runtime resolver for user-defined functions.
    ///
    /// A label dictionary is authoritative for compiled functions.  When a label is
    /// absent, the catalog is authoritative for existence and the interpreter-owned
    /// lazy compiler is asked to publish the containing file synchronously.
    /// </summary>
    internal static class FunctionResolver
    {
        public static FunctionLabelLine ResolveNormalLabel(LabelDictionary labels, string name)
        {
            string key = Normalize(name);
            if (string.IsNullOrEmpty(key))
                return null;

            FunctionLabelLine label = labels == null ? null : labels.GetNonEventLabel(key);
            if (label != null)
                return label;

            EnsureCompiled(key, false);
            return labels == null ? null : labels.GetNonEventLabel(key);
        }

        public static List<FunctionLabelLine>[] ResolveEventLabels(LabelDictionary labels, string name)
        {
            string key = Normalize(name);
            if (string.IsNullOrEmpty(key))
                return null;

            List<FunctionLabelLine>[] labelsForEvent = labels == null ? null : labels.GetEventLabels(key);
            if (labelsForEvent != null)
                return labelsForEvent;

            EnsureCompiled(key, true);
            return labels == null ? null : labels.GetEventLabels(key);
        }

        public static bool Exists(string name)
        {
            string key = Normalize(name);
            if (string.IsNullOrEmpty(key))
                return false;

            LabelDictionary labels = GlobalStatic.LabelDictionary;
            if (labels != null &&
                (labels.GetNonEventLabel(key) != null || labels.GetEventLabels(key) != null))
                return true;

            FunctionCatalog catalog = FunctionCatalog.Instance;
            return catalog != null && catalog.IsReady && catalog.FunctionExists(key);
        }

        public static int ExistFunctionValue(string name)
        {
            string key = Normalize(name);
            if (string.IsNullOrEmpty(key))
                return 0;

            LabelDictionary labels = GlobalStatic.LabelDictionary;
            if (labels != null)
            {
                FunctionLabelLine normal = labels.GetNonEventLabel(key);
                if (normal != null)
                {
                    if (normal.IsMethod)
                        return normal.MethodType == typeof(string) ? 3 : 2;
                    return 1;
                }
                if (labels.GetEventLabels(key) != null)
                    return 1;
            }

            FunctionCatalog catalog = FunctionCatalog.Instance;
            return catalog != null && catalog.IsReady
                ? catalog.ExistFunctionValue(key)
                : 0;
        }

        public static bool IsKnown(string name)
        {
            string key = Normalize(name);
            if (string.IsNullOrEmpty(key))
                return false;

            FunctionCatalog catalog = FunctionCatalog.Instance;
            return Exists(key) || (catalog != null && catalog.IsReady && catalog.FunctionExists(key));
        }

        public static bool IsKnownMethod(string name)
        {
            string key = Normalize(name);
            if (string.IsNullOrEmpty(key))
                return false;

            LabelDictionary labels = GlobalStatic.LabelDictionary;
            FunctionLabelLine label = labels == null ? null : labels.GetNonEventLabel(key);
            if (label != null)
                return label.IsMethod;

            FunctionCatalog catalog = FunctionCatalog.Instance;
            return catalog != null && catalog.IsReady &&
                catalog.GetReturnKind(key) != FunctionReturnKind.Void;
        }

        static void EnsureCompiled(string key, bool allDeclarations)
        {
            FunctionCatalog catalog = FunctionCatalog.Instance;
            if (catalog == null || !catalog.IsReady || !catalog.FunctionExists(key))
                return;

            OnDemandErbCompiler compiler = OnDemandErbCompiler.Instance;
            if (compiler == null)
                return;

            if (allDeclarations)
                compiler.EnsureEventLoaded(key);
            else
                compiler.EnsureFunction(key);

            LazyCompileFailure failure = compiler.GetFailure(key);
            if (failure != null)
                throw failure.ToCodeException();
        }

        static string Normalize(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            return Config.ICFunction ? name.ToUpper() : name;
        }
    }
}
