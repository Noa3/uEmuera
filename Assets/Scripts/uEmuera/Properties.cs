using System.Collections.Generic;

namespace Properties
{
    public static class ResourceManager
    {
        static Dictionary<string, string> dict = new Dictionary<string, string>
        {
            { "RuntimeErrMesMethodCIMGCreateOutOfRange0",GameMessages.T("{0} function: the specified image range is out of bounds")},
            { "RuntimeErrMesMethodColorARGB0",GameMessages.T("{0} function: an invalid value (0x{1:X8}) was specified for the ColorARGB argument")},
            { "RuntimeErrMesMethodDefaultArgumentOutOfRange0",GameMessages.T("{0} function: an invalid value ({1}) was specified for argument #{2}")},
            { "RuntimeErrMesMethodGColorMatrix0",GameMessages.T("{0} function: the specified ColorMatrix element ({1}, {2}) is invalid or is not 5x5")},
            { "RuntimeErrMesMethodGDIPLUSOnly",GameMessages.T("{0} function: cannot be used when the drawing option is WINAPI")},
            { "RuntimeErrMesMethodGHeight0",GameMessages.T("{0} function: a value of 0 or less ({1}) was specified for the Graphics Height")},
            { "RuntimeErrMesMethodGHeight1",GameMessages.T("{0} function: a value of {2} or more ({1}) was specified for the Graphics Height")},
            { "RuntimeErrMesMethodGraphicsID0",GameMessages.T("{0} function: a negative value ({1}) was specified for the GraphicsID")},
            { "RuntimeErrMesMethodGraphicsID1",GameMessages.T("{0} function: the GraphicsID value ({1}) is too large")},
            { "RuntimeErrMesMethodGWidth0",GameMessages.T("{0} function: a value of 0 or less ({1}) was specified for the Graphics Width")},
            { "RuntimeErrMesMethodGWidth1",GameMessages.T("{0} function: a value of {2} or more ({1}) was specified for the Graphics Width")},
            { "SyntaxErrMesMethodDefaultArgumentNotNullable0",GameMessages.T("{0} function: argument #{1} cannot be omitted")},
            { "SyntaxErrMesMethodDefaultArgumentNum0",GameMessages.T("{0} function: the number of arguments is incorrect")},
            { "SyntaxErrMesMethodDefaultArgumentNum1",GameMessages.T("{0} function: at least {1} arguments are required")},
            { "SyntaxErrMesMethodDefaultArgumentNum2",GameMessages.T("{0} function: too many arguments")},
            { "SyntaxErrMesMethodDefaultArgumentType0",GameMessages.T("{0} function: the type of argument #{1} is incorrect")},
            { "SyntaxErrMesMethodGraphicsColorMatrix0",GameMessages.T("{0} function: the ColorMatrix argument is not a 2D numeric array variable of 5x5 or larger")},
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
        ///   Looks up a localized string similar to "{0} function: the specified image range is out of bounds".
        /// </summary>
        public static string RuntimeErrMesMethodCIMGCreateOutOfRange0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodCIMGCreateOutOfRange0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: an invalid value (0x{1:X8}) was specified for the ColorARGB argument".
        /// </summary>
        public static string RuntimeErrMesMethodColorARGB0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodColorARGB0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: an invalid value ({1}) was specified for argument #{2}".
        /// </summary>
        public static string RuntimeErrMesMethodDefaultArgumentOutOfRange0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodDefaultArgumentOutOfRange0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: the specified ColorMatrix element ({1}, {2}) is invalid or is not 5x5".
        /// </summary>
        public static string RuntimeErrMesMethodGColorMatrix0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGColorMatrix0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: cannot be used when the drawing option is WINAPI".
        /// </summary>
        public static string RuntimeErrMesMethodGDIPLUSOnly
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGDIPLUSOnly", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: a value of 0 or less ({1}) was specified for the Graphics Height".
        /// </summary>
        public static string RuntimeErrMesMethodGHeight0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGHeight0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: a value of {2} or more ({1}) was specified for the Graphics Height".
        /// </summary>
        public static string RuntimeErrMesMethodGHeight1
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGHeight1", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: a negative value ({1}) was specified for the GraphicsID".
        /// </summary>
        public static string RuntimeErrMesMethodGraphicsID0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGraphicsID0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: the GraphicsID value ({1}) is too large".
        /// </summary>
        public static string RuntimeErrMesMethodGraphicsID1
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGraphicsID1", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: a value of 0 or less ({1}) was specified for the Graphics Width".
        /// </summary>
        public static string RuntimeErrMesMethodGWidth0
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGWidth0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: a value of {2} or more ({1}) was specified for the Graphics Width".
        /// </summary>
        public static string RuntimeErrMesMethodGWidth1
        {
            get
            {
                return ResourceManager.GetString("RuntimeErrMesMethodGWidth1", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: argument #{1} cannot be omitted".
        /// </summary>
        public static string SyntaxErrMesMethodDefaultArgumentNotNullable0
        {
            get
            {
                return ResourceManager.GetString("SyntaxErrMesMethodDefaultArgumentNotNullable0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: the number of arguments is incorrect".
        /// </summary>
        public static string SyntaxErrMesMethodDefaultArgumentNum0
        {
            get
            {
                return ResourceManager.GetString("SyntaxErrMesMethodDefaultArgumentNum0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: at least {1} arguments are required".
        /// </summary>
        public static string SyntaxErrMesMethodDefaultArgumentNum1
        {
            get
            {
                return ResourceManager.GetString("SyntaxErrMesMethodDefaultArgumentNum1", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: too many arguments".
        /// </summary>
        public static string SyntaxErrMesMethodDefaultArgumentNum2
        {
            get
            {
                return ResourceManager.GetString("SyntaxErrMesMethodDefaultArgumentNum2", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: the type of argument #{1} is incorrect".
        /// </summary>
        public static string SyntaxErrMesMethodDefaultArgumentType0
        {
            get
            {
                return ResourceManager.GetString("SyntaxErrMesMethodDefaultArgumentType0", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to "{0} function: the ColorMatrix argument is not a 2D numeric array variable of 5x5 or larger".
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
