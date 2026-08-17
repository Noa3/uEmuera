using System;
using System.Collections.Generic;
using System.Xml;
using MinorShift.Emuera.GameData;
using NUnit.Framework;

namespace uEmuera.Tests.EditMode
{
    public class EraDataTableSemanticsTests
    {
        [Test]
        public void CreateAddsProtectedIdColumnAndIdsSurviveRemoval()
        {
            EraDataTable table = new EraDataTable();
            Assert.AreEqual((int)EraDataTable.ColType.Int64, table.ColExist("id"));
            Assert.IsFalse(table.RemoveCol("id"));
            Assert.IsTrue(table.AddColumn("value", EraDataTable.ColType.Int32, true));

            long first = table.AddRow();
            long second = table.AddRow();
            Assert.AreNotEqual(first, second);
            Assert.IsTrue(table.RemoveRowById(first));
            Assert.AreEqual(1, table.RowCount);
            Assert.AreEqual(second, table.GetInt(0, "id"));
            Assert.IsFalse(table.RemoveRowById(0));
        }

        [Test]
        public void NullableEmptyAndDefaultAreDifferentStates()
        {
            EraDataTable table = new EraDataTable();
            Assert.IsTrue(table.AddColumn("text", EraDataTable.ColType.String, true));
            Assert.IsTrue(table.AddColumn("count", EraDataTable.ColType.Int16, true));
            table.SetDefault("count", 7L);
            long id = table.AddRow();

            Assert.IsTrue(table.IsNull(0, "text"));
            Assert.AreEqual(string.Empty, table.GetStr(0, "text"));
            string error;
            Assert.IsTrue(table.TrySet(0, "text", string.Empty, false, out error));
            Assert.IsFalse(table.IsNull(0, "text"));
            Assert.IsFalse(table.IsNull(id, "text", true));
            Assert.AreEqual(string.Empty, table.GetStr(id, "text", true));
            Assert.AreEqual(7, table.GetInt(0, "count"));
            Assert.IsFalse(table.TrySet(0, "count", null, false, out error));
        }

        [Test]
        public void SelectSupportsNullLikeAndStableMultiColumnSort()
        {
            EraDataTable table = new EraDataTable();
            table.AddColumn("name", EraDataTable.ColType.String, true);
            table.AddColumn("score", EraDataTable.ColType.Int32, true);
            Dictionary<string, object> row;
            long id;
            string error;
            row = new Dictionary<string, object> { { "name", "Alpha" }, { "score", 10L } };
            Assert.IsTrue(table.TryAddRow(row, out id, out error));
            row = new Dictionary<string, object> { { "name", "Beta" }, { "score", 20L } };
            Assert.IsTrue(table.TryAddRow(row, out id, out error));
            row = new Dictionary<string, object> { { "name", string.Empty }, { "score", null } };
            Assert.IsTrue(table.TryAddRow(row, out id, out error));

            List<int> selected = DataTableExpressionParser.Select(table, "score >= 10 AND name LIKE 'A*'", "score DESC");
            Assert.AreEqual(1, selected.Count);
            Assert.AreEqual("Alpha", table.GetStr(selected[0], "name"));
            selected = DataTableExpressionParser.Select(table, "score IS NULL", "id ASC");
            Assert.AreEqual(1, selected.Count);
            Assert.IsTrue(table.IsNull(selected[0], "score"));
        }

        [Test]
        public void SchemaAndDataRoundTripAndExternalEntitiesAreRejected()
        {
            EraDataTable table = new EraDataTable();
            table.AddColumn("name", EraDataTable.ColType.String, true);
            Dictionary<string, object> values = new Dictionary<string, object> { { "name", "Ada" } };
            long id;
            string error;
            Assert.IsTrue(table.TryAddRow(values, out id, out error));
            EraDataTable restored = EraDataTable.FromXml(table.ToXmlSchema(), table.ToXml());
            Assert.AreEqual(table.ColumnCount, restored.ColumnCount);
            Assert.AreEqual(table.RowCount, restored.RowCount);
            Assert.AreEqual("Ada", restored.GetStr(0, "name"));
            Assert.Throws<XmlException>(delegate
            {
                EraDataTable.FromXml("<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///secret'>]><emueraDataTableSchema />", table.ToXml());
            });
        }
    }
}
