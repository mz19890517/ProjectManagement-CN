using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using ProjectManagement.Core;

namespace ProjectManagement.UI;

/// <summary>
/// 定制右键菜单：编辑菜单项（名称 / 模板文件 / 目标文件）。
/// 点击菜单时把模板文件复制到选定项目文件夹；支持 {归属} 等占位符；
/// 模板文件是相对名时位于 exe 旁的“模板”文件夹。
/// </summary>
public sealed class CustomMenuEditorForm : Form
{
    private readonly DataGridView _grid;

    private List<CustomMenuItem> _result;

    /// <summary>确定后返回编辑后的菜单；取消或校验失败时为 null。</summary>
    public List<CustomMenuItem> GetResult() => _result;

    /// <param name="current">当前自定义菜单（复制后编辑，不影响原有列表）。</param>
    public CustomMenuEditorForm(IReadOnlyList<CustomMenuItem> current)
    {
        Text = "定制右键菜单";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(640, 340);
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
            Bounds = new Rectangle(12, 12, 520, 230),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = SystemColors.Window,
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
        };

        var cName = new DataGridViewTextBoxColumn
        {
            HeaderText = "菜单名称",
            Width = 170,
        };
        var cTpl = new DataGridViewTextBoxColumn
        {
            HeaderText = "模板文件（模板文件夹内）",
            Width = 180,
        };
        var cTarget = new DataGridViewTextBoxColumn
        {
            HeaderText = "目标文件（留空=同名复制）",
            Width = 160,
        };
        _grid.Columns.Add(cName);
        _grid.Columns.Add(cTpl);
        _grid.Columns.Add(cTarget);
        foreach (DataGridViewColumn c in _grid.Columns)
        {
            c.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        // “模板文件”列（第2列）：单元格内右键 → 直接选取「模板」文件夹里的文件
        _grid.CellMouseClick += OnCellRightClick;

        foreach (CustomMenuItem m in current)
        {
            _grid.Rows.Add(m.名称, m.模板文件, m.目标文件);
        }

        var hint = new Label
        {
            Text = "占位符：{序号} {项目名称} {归属} {生产编号} {系统} {进度}\r\n" +
                   "模板文件留空或填不到的文件时点击菜单会提示。目标文件留空 = 复制后保持模板文件名。",
            Bounds = new Rectangle(12, 246, 520, 34),
            ForeColor = SystemColors.GrayText,
        };

        var btnAdd = new Button { Text = "添加", Bounds = new Rectangle(542, 14, 86, 26) };
        btnAdd.Click += (_, _) => { _grid.EndEdit(); _grid.Rows.Add("", "", ""); _grid.CurrentCell = _grid.Rows[_grid.Rows.Count - 1].Cells[0]; };
        var btnDel = new Button { Text = "删除", Bounds = new Rectangle(542, 48, 86, 26) };
        btnDel.Click += (_, _) =>
        {
            if (_grid.CurrentRow != null && !_grid.CurrentRow.IsNewRow)
            {
                _grid.Rows.RemoveAt(_grid.CurrentRow.Index);
            }
        };
        var btnUp = new Button { Text = "上移", Bounds = new Rectangle(542, 82, 86, 26) };
        btnUp.Click += (_, _) => MoveSelected(-1);
        var btnDown = new Button { Text = "下移", Bounds = new Rectangle(542, 116, 86, 26) };
        btnDown.Click += (_, _) => MoveSelected(1);
        var btnExport = new Button
        {
            Text = "导出内置模板…",
            Bounds = new Rectangle(542, 158, 86, 26),
        };
        btnExport.Click += OnExportClicked;

        var ok = new Button
        {
            Text = "确定",
            Bounds = new Rectangle(450, 306, 80, 26),
            DialogResult = DialogResult.OK,
        };
        var cancel = new Button
        {
            Text = "取消",
            Bounds = new Rectangle(542, 306, 86, 26),
            DialogResult = DialogResult.Cancel,
        };

        Controls.AddRange(new Control[] { _grid, hint, btnAdd, btnDel, btnUp, btnDown, btnExport, ok, cancel });
        AcceptButton = null; // 回车要求提交单元格编辑
        CancelButton = cancel;
    }

    // ------------------------------------------------------------------
    // “模板文件”列右键选文件 + 占位符提示
    // ------------------------------------------------------------------

    private void OnCellRightClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0 || e.ColumnIndex != 1)
        {
            return;
        }

        if (e.RowIndex >= _grid.Rows.Count)
        {
            return;
        }

        _grid.EndEdit();
        _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[1];

        var folderMenu = new ContextMenuStrip { ShowImageMargin = false };
        string folder = Templates.模板文件夹;
        int count = 0;
        if (Directory.Exists(folder))
        {
            // 先列归属子文件夹（关联文件夹：点菜单时弹出其中的文件列表）
            foreach (string d in Directory.EnumerateDirectories(folder))
            {
                string name = Path.GetFileName(d);
                folderMenu.Items.Add(name + "（文件夹）", null, (_, _) => ApplyTemplateChoice(name));
                count++;
            }
            if (count > 0)
            {
                folderMenu.Items.Add(new ToolStripSeparator());
            }
            foreach (string f in Directory.EnumerateFiles(folder))
            {
                string fileName = Path.GetFileName(f);
                folderMenu.Items.Add(fileName, null, (_, _) => ApplyTemplateChoice(fileName));
                count++;
            }
        }

        if (count == 0)
        {
            folderMenu.Items.Add("（「模板」文件夹为空）", null, (_, _) => PickTemplateByDialog());
        }
        folderMenu.Items.Add(new ToolStripSeparator());
        folderMenu.Items.Add("浏览其他文件…", null, (_, _) => PickTemplateByDialog());
        folderMenu.Items.Add("占位符说明", null, (_, _) => ShowPlaceholderHint());
        folderMenu.Show(_grid, e.Location);
    }

    private void PickTemplateByDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择模板文件",
            Filter = "模板文件|*.*",
            FilterIndex = 0,
        };
        string folder = Templates.模板文件夹;
        if (Directory.Exists(folder))
        {
            dialog.InitialDirectory = folder;
        }
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string picked = dialog.FileName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string assign = FolderContains(folder, picked)
            ? picked.Substring(folder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            : picked;
        ApplyTemplateChoice(assign);
    }

    private static bool FolderContains(string folder, string fullFilePath)
    {
        try
        {
            string fullFolder = Path.GetFullPath(folder);
            string fullFile = Path.GetFullPath(fullFilePath);
            return fullFile.StartsWith(fullFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fullFile, fullFolder, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>把选中的模板文件填入“模板文件”单元格，并提示可用占位符。</summary>
    private void ApplyTemplateChoice(string templateFile)
    {
        if (_grid.CurrentRow != null)
        {
            _grid.CurrentRow.Cells[1].Value = templateFile;
            _grid.EndEdit();
            ShowPlaceholderHint();
        }
    }

    private static void ShowPlaceholderHint()
    {
        MessageBox.Show(
            "已选定模板文件。\n\n" +
            "模板文件与目标文件均可使用的占位符：\n" +
            "  {序号}    项目文件夹名的序号段\n" +
            "  {项目名称}\n" +
            "  {归属}\n" +
            "  {生产编号}\n" +
            "  {系统}\n" +
            "  {进度}\n\n" +
            "例：{归属}质检 报告.xls → 易能项目自动填成“易能质检 报告.xls”。",
            "占位符提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        string[] values = cells.Cast<DataGridViewCell>().Select(c => c.Value?.ToString() ?? "").ToArray();
        _grid.Rows.RemoveAt(from);
        _grid.Rows.Insert(to, values);
        _grid.CurrentCell = _grid.Rows[to].Cells[0];
    }

    private void OnExportClicked(object sender, EventArgs e)
    {
        try
        {
            int n = Templates.导出内置模板(Templates.模板文件夹);
            MessageBox.Show(this,
                "已导出 " + n + " 个内置模板到：\n" + Templates.模板文件夹 + "\n（已存在的跳过）",
                "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "导出失败：\n" + ex.Message, "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
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
            MessageBox.Show(this, "菜单名称与模板文件必须填写，且菜单名称不能重复", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }
        _result = list;
    }

    private bool TryCollect(out List<CustomMenuItem> result)
    {
        _grid.EndEdit();
        var list = new List<CustomMenuItem>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            string name = row.Cells[0].Value?.ToString()?.Trim() ?? "";
            string tpl = row.Cells[1].Value?.ToString()?.Trim() ?? "";
            string target = row.Cells[2].Value?.ToString()?.Trim() ?? "";
            if (name == "" || tpl == "")
            {
                result = null;
                return false;
            }
            if (list.Any(x => x.名称 == name))
            {
                result = null;
                return false;
            }
            list.Add(new CustomMenuItem { 名称 = name, 模板文件 = tpl, 目标文件 = target });
        }
        result = list;
        return true;
    }
}
