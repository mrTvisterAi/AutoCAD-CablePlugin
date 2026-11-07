using System;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows;
using CablePlugin.Core.Models;
using CablePlugin.Core.Services;
using CablePlugin.Data;
using CablePlugin.UI;
using CablePlugin.UI.ViewModels;
using CablePlugin.UI.Views;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CablePlugin.UIHost
{
    /// <summary>
    /// Палитра свойств кабеля: синхронизация с выделением в чертеже.
    /// </summary>
    public static class CablePropertiesPalette
    {
        private static PaletteSet _palette;
        private static CablePropertiesViewModel _vm;
        private static ObjectId _currentId = ObjectId.Null;
        private static Document _subscribedDoc;
        private static readonly CableLineRepository Repo = new CableLineRepository();
        private static readonly PipeTableRepository PipeRepo = new PipeTableRepository();
        private static readonly ElectricalCalculator Electrical = new ElectricalCalculator();

        public static void Show()
        {
            EnsureCreated();
            _palette.Visible = true;
            SubscribeDocument(AcApp.DocumentManager.MdiActiveDocument);
            RefreshFromSelection();
        }

        public static void Toggle()
        {
            EnsureCreated();
            _palette.Visible = !_palette.Visible;
            if (_palette.Visible)
            {
                SubscribeDocument(AcApp.DocumentManager.MdiActiveDocument);
                RefreshFromSelection();
            }
        }

        private static void EnsureCreated()
        {
            if (_palette != null) return;

            _vm = new CablePropertiesViewModel();
            _vm.RequestApply += OnApply;
            _vm.RequestEditFull += OnEditFull;

            var wpf = new CablePropertiesControl(_vm);
            var host = new ElementHost
            {
                Dock = DockStyle.Fill,
                Child = wpf
            };

            _palette = new PaletteSet("Свойства кабеля", new Guid("A7C0E2B1-4F3D-4A9E-9C21-8B6D5E0F1A22"))
            {
                Style = PaletteSetStyles.ShowCloseButton
                        | PaletteSetStyles.ShowAutoHideButton
                        | PaletteSetStyles.ShowPropertiesMenu,
                MinimumSize = new System.Drawing.Size(260, 320),
                DockEnabled = (DockSides)((int)DockSides.Left | (int)DockSides.Right)
            };

            _palette.Add("Кабель", host);

            AcApp.DocumentManager.DocumentActivated += (s, e) =>
            {
                SubscribeDocument(e.Document);
                if (_palette != null && _palette.Visible)
                    RefreshFromSelection();
            };
        }

        private static void SubscribeDocument(Document doc)
        {
            if (doc == null) return;

            if (_subscribedDoc != null)
            {
                try { _subscribedDoc.ImpliedSelectionChanged -= OnImpliedSelectionChanged; }
                catch { /* disposed */ }
            }

            _subscribedDoc = doc;
            _subscribedDoc.ImpliedSelectionChanged += OnImpliedSelectionChanged;
        }

        private static void OnImpliedSelectionChanged(object sender, EventArgs e)
        {
            if (_palette == null || !_palette.Visible) return;
            if (_vm != null && _vm.IsDirty)
                return; // не затирать несохранённые правки

            RefreshFromSelection();
        }

        public static void RefreshFromSelection()
        {
            if (_vm == null) return;

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                _vm.Clear();
                _currentId = ObjectId.Null;
                return;
            }

            var ed = doc.Editor;
            var db = doc.Database;

            ObjectId id = ObjectId.Null;
            try
            {
                var psr = ed.SelectImplied();
                if (psr.Status == PromptStatus.OK && psr.Value != null && psr.Value.Count > 0)
                    id = psr.Value[0].ObjectId;
            }
            catch
            {
                id = ObjectId.Null;
            }

            if (id.IsNull)
            {
                _currentId = ObjectId.Null;
                _vm.Clear();
                return;
            }

            using (var tr = db.TransactionManager.StartTransaction())
            {
                if (!PolylineLengthHelper.IsSupportedCurve(id, tr))
                {
                    _currentId = ObjectId.Null;
                    _vm.Clear();
                    tr.Commit();
                    return;
                }

                var line = Repo.Load(id, tr);
                var handle = id.Handle.ToString();
                double len = PolylineLengthHelper.GetLength(id, tr);

                if (line == null)
                {
                    _currentId = id;
                    var draft = new CableLine
                    {
                        Cable = new CableInfo { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 1.5 },
                        Segments =
                        {
                            new CableSegment
                            {
                                LayingMethod = "Гофра",
                                Length = len,
                                IsPipeDiameterAuto = true
                            }
                        },
                        ReservePercent = 20,
                        ScaleFactor = 1.0,
                        RoundUpTo = 1.0,
                        GeometricLength = len
                    };
                    LengthCalculator.Calculate(draft);
                    _vm.LoadFrom(draft, handle, "Нет данных кабеля — заполните и нажмите Применить");
                    tr.Commit();
                    return;
                }

                _currentId = id;
                _vm.LoadFrom(line, handle);
                tr.Commit();
            }
        }

        private static void OnApply()
        {
            if (_vm == null || !_vm.HasSelection || _currentId.IsNull) return;

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var db = doc.Database;

            try
            {
                using (doc.LockDocument())
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var existing = Repo.Load(_currentId, tr);
                    var updated = _vm.ToModel(existing);

                    var pipeTable = PipeRepo.Load(db, tr);
                    var pipeSelector = new PipeSelector(pipeTable);
                    pipeSelector.ApplyToLine(updated);
                    updated.GeometricLength = PolylineLengthHelper.GetLength(_currentId, tr);
                    LengthCalculator.Calculate(updated);
                    Electrical.Calculate(updated);

                    Repo.Save(_currentId, updated, tr);
                    tr.Commit();

                    _vm.LoadFrom(updated, _currentId.Handle.ToString(), "Сохранено");
                    _vm.MarkSaved();
                    doc.Editor.WriteMessage(
                        $"\n[CablePlugin] Свойства сохранены: {updated.Cable?.DisplayName}, L={updated.TotalLength:0.###} м");
                }
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage("\n[CablePlugin] Ошибка сохранения свойств: " + ex.Message);
            }
        }

        private static void OnEditFull()
        {
            if (_currentId.IsNull) return;

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var db = doc.Database;

            CableLine existing;
            System.Collections.Generic.List<PipeCorrespondence> pipeTable;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                existing = Repo.Load(_currentId, tr) ?? _vm.ToModel();
                pipeTable = PipeRepo.Load(db, tr);
                tr.Commit();
            }

            IntPtr owner;
            try { owner = AcApp.MainWindow.Handle; }
            catch { owner = IntPtr.Zero; }

            var edited = CableDialogService.EditLine(existing, pipeTable, owner);
            if (edited == null) return;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var pipeSelector = new PipeSelector(pipeTable);
                pipeSelector.ApplyToLine(edited);
                LengthCalculator.Calculate(edited);
                Electrical.Calculate(edited);
                Repo.Save(_currentId, edited, tr);
                tr.Commit();
            }

            _vm.LoadFrom(edited, _currentId.Handle.ToString(), "Сохранено из полного диалога");
            _vm.MarkSaved();
        }
    }
}
