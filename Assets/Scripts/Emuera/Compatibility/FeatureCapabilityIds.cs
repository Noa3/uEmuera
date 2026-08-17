using System;

namespace MinorShift.Emuera.Compatibility
{
    /// <summary>
    /// Stable identifiers used by parity fixtures, reports and runtime capability checks.
    /// Keep identifiers lowercase and never reuse an identifier for a different contract.
    /// </summary>
    public static class FeatureCapabilityIds
    {
        public const string HtmlDiv = "html.div";
        public const string HtmlClearButton = "html.clearbutton";
        public const string HtmlImageSrcb = "html.img.srcb";
        public const string HtmlImageSrcm = "html.img.srcm";
        public const string HtmlPrintIsland = "html.print_island";
        public const string CbgSprite = "cbg.sprite";
        public const string CbgButtonMap = "cbg.buttonmap";
        public const string CbgOrdering = "cbg.ordering";
        public const string DtCreate = "dt.create";
        public const string DtColumn = "dt.column";
        public const string DtRow = "dt.row";
        public const string DtCell = "dt.cell";
        public const string DtSelect = "dt.select";
        public const string DtSerialization = "dt.serialization";
        public const string MapCreate = "map.create";
        public const string MapOperations = "map.operations";
        public const string MapSerialization = "map.serialization";
        public const string XmlGet = "xml.get";
        public const string XmlSet = "xml.set";
        public const string XmlMutation = "xml.mutation";
        public const string ErdNamedIndices = "erd.named_indices";
        public const string InputMouse = "input.mouseb";
        public const string InputCoordinates = "input.coordinates";
        public const string SaveExtendedData = "save.extended_data";
        public const string SaveMultidimensionalStrings = "save.multidimensional_strings";
        public const string VirtualFileSystem = "filesystem.virtual";
        public const string EncodingDetection = "filesystem.encoding";

        public static readonly string[] All =
        {
            HtmlDiv, HtmlClearButton, HtmlImageSrcb, HtmlImageSrcm, HtmlPrintIsland,
            CbgSprite, CbgButtonMap, CbgOrdering,
            DtCreate, DtColumn, DtRow, DtCell, DtSelect, DtSerialization,
            MapCreate, MapOperations, MapSerialization,
            XmlGet, XmlSet, XmlMutation, ErdNamedIndices,
            InputMouse, InputCoordinates, SaveExtendedData, SaveMultidimensionalStrings,
            VirtualFileSystem, EncodingDetection,
        };
    }
}
