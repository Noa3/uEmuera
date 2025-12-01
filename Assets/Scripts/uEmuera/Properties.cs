using System.Collections.Generic;

namespace Properties
{
    public static class ResourceManager
    {
        static Dictionary<string, string> dict = new Dictionary<string, string>
        {
            { "RuntimeErrMesMethodCIMGCreateOutOfRange0","{0} function: image range is out of bounds"},
            { "RuntimeErrMesMethodColorARGB0","{0} function: invalid value (0x{1:X8}) specified for ColorARGB argument"},
            { "RuntimeErrMesMethodDefaultArgumentOutOfRange0","{0} function: invalid value ({1}) specified for argument {2}"},
            { "RuntimeErrMesMethodGColorMatrix0","{0} function: specified ColorMatrix element ({1}, {2}) is invalid or does not meet 5x5 requirements"},
            { "RuntimeErrMesMethodGDIPLUSOnly","{0} function: cannot be used when drawing option is WINAPI"},
            { "RuntimeErrMesMethodGHeight0","{0} function: value ({1}) of 0 or below was specified for Graphics Height"},
            { "RuntimeErrMesMethodGHeight1","{0} function: value ({1}) of {2} or above was specified for Graphics Height"},
            { "RuntimeErrMesMethodGraphicsID0","{0} function: negative value ({1}) was specified for GraphicsID"},
            { "RuntimeErrMesMethodGraphicsID1","{0} function: GraphicsID value ({1}) is too large"},
            { "RuntimeErrMesMethodGWidth0","{0} function: value ({1}) of 0 or below was specified for Graphics Width"},
            { "RuntimeErrMesMethodGWidth1","{0} function: value ({1}) of {2} or above was specified for Graphics Width"},
            { "SyntaxErrMesMethodDefaultArgumentNotNullable0","{0} function: argument {1} cannot be omitted"},
            { "SyntaxErrMesMethodDefaultArgumentNum0","{0} function: incorrect number of arguments"},
            { "SyntaxErrMesMethodDefaultArgumentNum1","{0} function: at least {1} arguments are required"},
            { "SyntaxErrMesMethodDefaultArgumentNum2","{0} function: too many arguments"},
            { "SyntaxErrMesMethodDefaultArgumentType0","{0} function: type of argument {1} is incorrect"},
            { "SyntaxErrMesMethodGraphicsColorMatrix0","{0} function: ColorMatrix requires a 5x5 or larger two-dimensional numeric array variable as argument"},
        };

        public static string GetString(string key, object culture)
        {
            string s;
            dict.TryGetValue(key, out s);
            return s;
        }
    }

    public static class Resources
    {
        private static global::System.Globalization.CultureInfo resourceCulture;

        /// <summary>
        ///   Looks up a localized string similar to {0} function: image range is out of bounds.
        /// </summary>
        public static string RuntimeErrMesMethodCIMGCreateOutOfRange0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodCIMGCreateOutOfRange0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: invalid value (0x{1:X8}) specified for ColorARGB argument.
        /// </summary>
        public static string RuntimeErrMesMethodColorARGB0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodColorARGB0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: invalid value ({1}) specified for argument {2}.
        /// </summary>
        public static string RuntimeErrMesMethodDefaultArgumentOutOfRange0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodDefaultArgumentOutOfRange0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: specified ColorMatrix element ({1}, {2}) is invalid or does not meet 5x5 requirements.
        /// </summary>
        public static string RuntimeErrMesMethodGColorMatrix0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGColorMatrix0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: cannot be used when drawing option is WINAPI.
        /// </summary>
        public static string RuntimeErrMesMethodGDIPLUSOnly
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGDIPLUSOnly", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: value ({1}) of 0 or below was specified for Graphics Height.
        /// </summary>
        public static string RuntimeErrMesMethodGHeight0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGHeight0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: value ({1}) of {2} or above was specified for Graphics Height.
        /// </summary>
        public static string RuntimeErrMesMethodGHeight1
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGHeight1", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: negative value ({1}) was specified for GraphicsID.
        /// </summary>
        public static string RuntimeErrMesMethodGraphicsID0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGraphicsID0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: GraphicsID value ({1}) is too large.
        /// </summary>
        public static string RuntimeErrMesMethodGraphicsID1
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGraphicsID1", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: value ({1}) of 0 or below was specified for Graphics Width.
        /// </summary>
        public static string RuntimeErrMesMethodGWidth0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGWidth0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: value ({1}) of {2} or above was specified for Graphics Width.
        /// </summary>
        public static string RuntimeErrMesMethodGWidth1
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGWidth1", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: argument {1} cannot be omitted.
        /// </summary>
        public static string SyntaxErrMesMethodDefaultArgumentNotNullable0
        {
            get
            {
                return ResourceManager.GetString("SyntaxErrMesMethodDefaultArgumentNotNullable0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: incorrect number of arguments.
        /// </summary>
        public static string SyntaxErrMesMethodDefaultArgumentNum0
        {
            get
            {
                return ResourceManager.GetString("SyntaxErrMesMethodDefaultArgumentNum0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: at least {1} arguments are required.
        /// </summary>
        public static string SyntaxErrMesMethodDefaultArgumentNum1
        {
            get
            {
                return ResourceManager.GetString("SyntaxErrMesMethodDefaultArgumentNum1", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: too many arguments.
        /// </summary>
        public static string SyntaxErrMesMethodDefaultArgumentNum2
        {
            get
            {
                return ResourceManager.GetString("SyntaxErrMesMethodDefaultArgumentNum2", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: type of argument {1} is incorrect.
        /// </summary>
        public static string SyntaxErrMesMethodDefaultArgumentType0
        {
            get
            {
                return ResourceManager.GetString("SyntaxErrMesMethodDefaultArgumentType0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} function: ColorMatrix requires a 5x5 or larger two-dimensional numeric array variable as argument.
        /// </summary>
        public static string SyntaxErrMesMethodGraphicsColorMatrix0
        {
            get
            {
                return ResourceManager.GetString("SyntaxErrMesMethodGraphicsColorMatrix0", resourceCulture);
            }
        }
    }
}
