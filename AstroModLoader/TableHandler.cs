﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace AstroModLoader
{
    internal enum ColumnType
    {
        Text,
        CheckBox,
        ComboBox
    }

    internal class ColumnData
    {
        public string Name;
        public ColumnType Type;
        public DataGridViewAutoSizeColumnMode OverrideSizeMode;

        public ColumnData(string Name, ColumnType Type, DataGridViewAutoSizeColumnMode OverrideSizeMode = DataGridViewAutoSizeColumnMode.NotSet)
        {
            this.Name = Name;
            this.Type = Type;
            this.OverrideSizeMode = OverrideSizeMode;
        }
    }

    public class TableHandler
    {
        private DataGridView _gv;

        private DataGridView GridView
        {
            get
            {
                if (AMLUtils.InvokeRequired()) throw new InvalidOperationException("Attempt to get TableHandler.GridView outside of UI thread");
                return _gv;
            }
            set
            {
                if (AMLUtils.InvokeRequired()) throw new InvalidOperationException("Attempt to set TableHandler.GridView outside of UI thread");
                _gv = value;
            }
        }
        private ModHandler ModManager;

        public TableHandler(DataGridView gridView, ModHandler modManager)
        {
            GridView = gridView;
            ModManager = modManager;
        }

        internal void PaintCell(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.ColumnIndex > -1 && e.RowIndex > -1 && GridView.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn && GridView[e.ColumnIndex, e.RowIndex].ReadOnly && e.Value != null)
            {
                Size normalCheckSize = CheckBoxRenderer.GetGlyphSize(e.Graphics, CheckBoxState.CheckedNormal);
                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);

                Point newGlyphLocation = new Point(e.CellBounds.X + e.CellBounds.Width / 2 - normalCheckSize.Width / 2, e.CellBounds.Y + e.CellBounds.Height / 2 - normalCheckSize.Height / 2);
                CheckBoxRenderer.DrawCheckBox(e.Graphics, newGlyphLocation, (bool)e.Value ? CheckBoxState.CheckedDisabled : CheckBoxState.UncheckedDisabled);
            }
        }

        private void AddColumns(List<ColumnData> ourColumns)
        {
            for (int i = 0; i < ourColumns.Count; i++)
            {
                ColumnData columnData = ourColumns[i];
                if (columnData == null) continue;

                DataGridViewColumn dgc;
                if (columnData.Type == ColumnType.CheckBox)
                {
                    dgc = new DataGridViewCheckBoxColumn
                    {
                        HeaderText = columnData.Name
                    };
                    dgc.ReadOnly = false;
                }
                else if (columnData.Type == ColumnType.ComboBox)
                {
                    dgc = new DataGridViewComboBoxColumn
                    {
                        HeaderText = columnData.Name
                    };
                    dgc.ReadOnly = false;
                }
                else
                {
                    dgc = new DataGridViewTextBoxColumn
                    {
                        HeaderText = columnData.Name
                    };
                    dgc.ReadOnly = true;
                }

                dgc.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                if (columnData.OverrideSizeMode != DataGridViewAutoSizeColumnMode.NotSet) dgc.AutoSizeMode = columnData.OverrideSizeMode;

                dgc.SortMode = DataGridViewColumnSortMode.NotSortable;
                GridView.Columns.Add(dgc);
            }
        }

        public Mod GetCurrentlySelectedMod()
        {
            int idx = -1;
            AMLUtils.InvokeUI(() =>
            {
                if (GridView == null || GridView.SelectedRows == null || GridView.SelectedRows.Count == 0 || GridView.SelectedRows[0].Index < 0 || GridView.SelectedRows[0].Index >= ModManager.Mods.Count)
                {
                    idx = -1;
                }
                else
                {
                    idx = GridView.SelectedRows[0].Index;
                }
            });
            if (idx < 0) return null;
            return ModManager.Mods[idx];
        }

        public void Refresh()
        {
            AMLUtils.InvokeUI(RefreshInternal);
        }

        // While we allow clients to enable RefuseMismatchedConnections too, the average co-op player doesn't need the level of configuration that the Optional column provides
        public static bool ShouldContainOptionalColumn()
        {
            return Program.CommandLineOptions.ServerMode && ModHandler.OurIntegrator.RefuseMismatchedConnections;
        }

        private void RefreshInternal()
        {
            GridView.SuspendLayout();
            GridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            RefreshInternal2();
            GridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            GridView.ResumeLayout();
        }

        private int? LastNumMods = 0;
        private int LastScrollPos = 0;
        private void RefreshInternal2()
        {
            LastNumMods = GridView.Rows?.Count;
            LastScrollPos = GridView.FirstDisplayedScrollingRowIndex;
            Mod selectedMod = GetCurrentlySelectedMod();

            GridView.DataSource = null;
            GridView.EndEdit();
            GridView.Visible = true;
            GridView.Columns.Clear();
            GridView.Rows.Clear();
            GridView.AllowUserToAddRows = false;
            GridView.ReadOnly = false;
            GridView.AutoGenerateColumns = false;

            if (ModManager.Mods.Count == 0)
            {
                ModManager.BaseForm.AdjustModInfoText("");
                return;
            }

            AddColumns(new List<ColumnData>
            {
                new ColumnData("", ColumnType.CheckBox),
                new ColumnData("Name", ColumnType.Text),
                new ColumnData("Version", ColumnType.ComboBox),
                new ColumnData("Author", ColumnType.Text),
                new ColumnData("Game Build", ColumnType.Text),
                ShouldContainOptionalColumn() ? new ColumnData("Optional?", ColumnType.CheckBox, DataGridViewAutoSizeColumnMode.ColumnHeader) : null,
                new ColumnData("", ColumnType.Text, DataGridViewAutoSizeColumnMode.Fill) // We fill the rest of the space with an empty column so that any extra room is still highlighted for the selected row
            });

            List<DataGridViewRow> newRows = new List<DataGridViewRow>();
            foreach (Mod mod in ModManager.Mods)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.Tag = mod;
                row.CreateCells(GridView);

                row.Cells[0].Value = mod.Enabled;
                if (row.Cells[0] is DataGridViewCheckBoxCell checkCell)
                {
                    if (ModManager.IsReadOnly)
                    {
                        checkCell.ReadOnly = true;
                    }
                }

                row.Cells[1].Value = mod.CurrentModData.Name;

                bool hasAutoUpdate = ModManager.GlobalIndexFile != null && ModManager.GlobalIndexFile.ContainsKey(mod.CurrentModData.ModID);

                if (row.Cells[2] is DataGridViewComboBoxCell cbCell)
                {
                    if (hasAutoUpdate) cbCell.Items.Add("Latest (" + mod.AvailableVersions[0] + ")");
                    foreach (Version ver in mod.AvailableVersions)
                    {
                        cbCell.Items.Add(ver.ToString());
                    }

                    if (hasAutoUpdate && mod.ForceLatest)
                    {
                        cbCell.Value = "Latest (" + mod.AvailableVersions[0] + ")";
                    }
                    else
                    {
                        cbCell.Value = mod.InstalledVersion.ToString();
                    }
                    cbCell.FlatStyle = FlatStyle.Flat;
                    cbCell.Style.BackColor = AMLPalette.DropDownBackgroundColor;
                    cbCell.Style.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, (byte)0);
                    cbCell.ReadOnly = ModManager.IsReadOnly;
                }

                row.Cells[3].Value = mod.CurrentModData.Author;

                if (mod.CurrentModData.GameBuild == null)
                {
                    row.Cells[4].Value = mod.CurrentModData.Sync == AstroModIntegrator.SyncMode.None ? "" : "Any";
                }
                else
                {
                    row.Cells[4].Value = mod.CurrentModData.GameBuild;
                    if (ModManager.InstalledAstroBuild != null && !mod.CurrentModData.GameBuild.AcceptablySimilar(ModManager.InstalledAstroBuild))
                    {
                        row.Cells[4].Style.ForeColor = AMLPalette.WarningColor;
                        row.Cells[4].Style.SelectionForeColor = AMLPalette.WarningColor;
                    }
                }

                if (ShouldContainOptionalColumn())
                {
                    row.Cells[5].Value = mod.IsOptional;
                    if (row.Cells[5] is DataGridViewCheckBoxCell checkCell2)
                    {
                        if (ModManager.IsReadOnly || mod.CurrentModData.Sync != AstroModIntegrator.SyncMode.ServerAndClient)
                        {
                            checkCell2.ReadOnly = true;
                        }
                    }
                }

                row.ReadOnly = false;
                newRows.Add(row);
            }
            GridView.Rows.AddRange(newRows.ToArray());
            GridView.ClearSelection();

            foreach (DataGridViewRow row in GridView.Rows)
            {
                if (object.ReferenceEquals(row.Tag, selectedMod))
                {
                    row.Selected = true;
                    row.Cells[0].Selected = true;
                    break;
                }
            }

            int? currentRowCount = GridView.Rows?.Count;
            if (currentRowCount != null && currentRowCount > 0 && currentRowCount == LastNumMods)
            {
                GridView.FirstDisplayedScrollingRowIndex = LastScrollPos;
            }
            LastNumMods = currentRowCount;
        }
    }
}
