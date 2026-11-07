using CablePlugin.Core.Reports;
using CablePlugin.Core.Templates;
using Xunit;

namespace CablePlugin.Tests
{
    public class TemplateTests
    {
        [Fact]
        public void DefaultJournal_HasExpectedColumns()
        {
            var t = DefaultTemplates.Journal();
            Assert.True(t.Columns.Count >= 10);
            Assert.Contains(t.Columns, c => c.Id == "mark");
            Assert.Contains(t.Columns, c => c.Id == "length");
            Assert.All(t.VisibleColumns, c => Assert.True(c.Visible));
        }

        [Fact]
        public void TemplateCellResolver_Journal_FormatsCoresSection()
        {
            var row = new JournalRow
            {
                Number = 1,
                CableMark = "ВВГнг(А)-LS",
                Cores = 3,
                Section = 1.5,
                Length = 12.5,
                Breaker = "10"
            };

            Assert.Equal("3х1.5", TemplateCellResolver.Journal("cores_section", row));
            Assert.Equal("12.5", TemplateCellResolver.Journal("length", row));
            Assert.Equal("10", TemplateCellResolver.Journal("breaker", row));
        }

        [Fact]
        public void TemplateSet_CreateDefault_HasPanelBlocks()
        {
            var set = TemplateSet.CreateDefault();
            Assert.NotNull(set.PanelBlocks);
            Assert.Equal("CABLE_QF", set.PanelBlocks.BreakerBlockName);
            Assert.Equal("CABLE_BUS", set.PanelBlocks.BusBlockName);
            Assert.Equal("CABLE_LOAD", set.PanelBlocks.LoadBlockName);
        }

        [Fact]
        public void HideColumn_RemovesFromVisible()
        {
            var t = DefaultTemplates.Journal();
            var note = t.Columns.Find(c => c.Id == "note");
            Assert.NotNull(note);
            note.Visible = false;
            Assert.DoesNotContain(t.VisibleColumns, c => c.Id == "note");
        }
    }
}
