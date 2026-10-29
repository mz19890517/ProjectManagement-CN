using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using ProjectManagement.Core;

namespace ProjectManagement.UI;

/// <summary>
/// 编辑模式：对检测列进行添加 / 删除 / 改名 / 修改关联关键字 / 排序。
/// 前 5 列（序号—项目名称—归属—生产编号—系统）与最后的“进度”列
/// 由项目文件夹命名规则决定，不在编辑范围。
/// </summary>
public sealed class ColumnEditorForm : Form
{
    private readonly DataGridView _grid;
    private readonly Button _btnUp;
    private readonly Button _btnDown;

    private List<DetectorColumn> _result;

    /// <summary>确定后返回编辑后的列定义；取消或校验失败时为 null。</summary>
    public List<DetectorColumn> GetResult() => _result;

    /// <param name="current">当前检测列定义（将复制后编辑，不影响原有列表）。</param>
    public ColumnEditorForm(IReadOnlyList<DetectorColumn> current)
    {
        Text = "编辑模式（列定义）";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(500, 320);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("宋体", 9F);
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };

        _grid = new DataGridView
        {
            Bounds = new Rectangle(12, 12, 380, 230),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = SystemColors.Window,
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
        };

        var colName = new DataGridViewTextBoxColumn
        {
            Name = "名称",
            HeaderText = "列头名称",
            DataPropertyName = "名称",
            Width = 140,
            FillWeight = 40,
        };
        var colKeyword = new DataGridViewTextBoxColumn
        {
            Name = "关键字",
            HeaderText = "关联关键字（多个用逗号分隔）",
            DataPropertyName = "关键字",
            Width = 230,
            FillWeight = 60,
        };
        _grid.Columns.Add(colName);
        _grid.Columns.Add(colKeyword);
        _grid.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
        _grid.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
        _grid.AllowUserToAddRows = false;

        foreach (DetectorColumn d in current)
        {
            _grid.Rows.Add(d.名称, d.关键字);
        }

        var hint = new Label
        {
            Text = "规则：项目目录里的文件或一级子文件夹名包含该列任一关键字，此格就记“有”。\r\n" +
                   "最下面的空白行填好后，按 Tab 或点击“添加列”即可追加新列。",
            Bounds = new Rectangle(12, 246, 380, 34),
            ForeColor = SystemColors.GrayText,
        };

        var btnAdd = new Button { Text = "添加列", Bounds = new Rectangle(402, 14, 84, 26) };
        btnAdd.Click += (_, _) => AddRow();
        var btnDel = new Button { Text = "删除列", Bounds = new Rectangle(402, 48, 84, 26) };
        btnDel.Click += (_, _) => DeleteSelected();
        _btnUp = new Button { Text = "上移", Bounds = new Rectangle(402, 82, 84, 26) };
        _btnUp.Click += (_, _) => MoveSelected(-1);
        _btnDown = new Button { Text = "下移", Bounds = new Rectangle(402, 116, 84, 26) };
        _btnDown.Click += (_, _) => MoveSelected(1);

        var ok = new Button
        {
            Text = "确定",
            Bounds = new Rectangle(310, 284, 80, 26),
            DialogResult = DialogResult.OK,
        };
        var cancel = new Button
        {
            Text = "取消",
            Bounds = new Rectangle(402, 284, 84, 26),
            DialogResult = DialogResult.Cancel,
        };

        Controls.AddRange(new Control[] { _grid, hint, btnAdd, btnDel, _btnUp, _btnDown, ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void AddRow()
    {
        _grid.EndEdit();
        _grid.Rows.Add("", "");
        _grid.CurrentCell = _grid.Rows[_grid.Rows.Count - 1].Cells[0];
    }

    private void DeleteSelected()
    {
        if (_grid.CurrentRow != null && !_grid.CurrentRow.IsNewRow)
        {
            _grid.Rows.RemoveAt(_grid.CurrentRow.Index);
        }
    }

    private void MoveSelected(int delta)
    {
        if (_grid.CurrentRow == null)
        {
            return;
        }

        int from = _grid.CurrentRow.Index;
        int to = from + delta;
        if (to < 0 || to >= _grid.Rows.Count)
        {
            return;
        }

        var cells = _grid.Rows[from].Cells;
        string a = cells[0].Value?.ToString() ?? "";
        string b = cells[1].Value?.ToString() ?? "";
        _grid.Rows.RemoveAt(from);
        _grid.Rows.Insert(to, a, b);
        _grid.CurrentCell = _grid.Rows[to].Cells[1];
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (DialogResult != DialogResult.OK)
        {
            return;
        }

        if (!TryCollect(out var list))
        {
            MessageBox.Show(this, "列头名称不能为空，且各列头名称不能重复", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true; // 留在编辑器里继续改
            return;
        }
        _result = list;
    }

    /// <summary>收集全部行并提交未完成编辑；列头空白或重名时返回 false。</summary>
    private bool TryCollect(out List<DetectorColumn> result)
    {
        _grid.EndEdit();
        var list = new List<DetectorColumn>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            string name = row.Cells[0].Value?.ToString()?.Trim() ?? "";
            string keyword = row.Cells[1].Value?.ToString()?.Trim() ?? "";
            if (name == "")
            {
                result = null;
                return false;
            }
            if (list.Any(x => x.名称 == name))
            {
                result = null;
                return false;
            }
            list.Add(new DetectorColumn { 名称 = name, 关键字 = keyword });
        }
        result = list;
        return true;
    }
}

