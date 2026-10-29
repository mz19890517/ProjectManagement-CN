using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

using ProjectManagement.Core;

namespace ProjectManagement.UI;

public partial class MainForm : Form
{
    // 原 易语言 窗口布局（超级列表框报表；检测列可由“编辑模式”增删改，宽度默认 50）
    private static readonly int[] 前五列宽 = { 80, 210, 100, 140, 30 };
    private static readonly string[] 前五列名 = { "序号", "项目名称", "归属", "生产编号", "系统" };
    private const int 检测列默认宽 = 50;
    private const int 进度列默认宽 = 60;

    // 原尺寸代码中的参考常量
    private const int 参考宽度 = 910;
    private const int 参考高度 = 655;
    private const int 列表参考宽度 = 900;
    private const int 列表参考高度 = 592;

    private ProjectRepository _repo;
    private readonly DataGridView _grid;
    private readonly ContextMenuStrip _projectMenu;
    private readonly TextBox _filterBox;
    private List<ProjectItem> _items = new();
    private IReadOnlyList<DetectorColumn> _detectors;
    private IReadOnlyList<CustomMenuItem> _customMenu;
    private List<int> _defaultWidths = new();

    // 排序状态：默认按序号列降序；点列头切换
    private int _sortCol;
    private bool _sortDesc = true;

    public MainForm()
    {
        Text = "项目管理-by 孟国胜";
        ClientSize = new Size(864, 666);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(128, 128, 128);
        Font = new Font("宋体", 9F);

        var btnRead = new Button
        {
            Name = "按钮读取",
            Text = "读取项目",
            Bounds = new Rectangle(0, 0, 104, 32),
        };
        btnRead.Click += (s, e) => RefreshAll();

        var btnSettings = new Button
        {
            Name = "按钮设置",
            Text = "设置",
            Bounds = new Rectangle(110, 0, 104, 32),
        };
        btnSettings.Click += (s, e) => OpenSettings();

        var filterLabel = new Label
        {
            Text = "筛选：",
            Bounds = new Rectangle(222, 6, 44, 22),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _filterBox = new TextBox
        {
            Name = "筛选输入",
            Bounds = new Rectangle(266, 4, 280, 24),
            TabIndex = 1,
        };
        _filterBox.TextChanged += (s, e) => RebuildGrid();

        _grid = new DataGridView
        {
            Name = "超级列表框1",
            Bounds = new Rectangle(4, 32, 852, 592),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToOrderColumns = false,
            MultiSelect = true,
            ReadOnly = true,
            RowHeadersVisible = false,
            BackgroundColor = EplColors.白色,
            GridColor = SystemColors.ControlLight,
            Font = new Font("黑体", 19F, FontStyle.Bold, GraphicsUnit.Pixel),
            RowTemplate = { Height = 28 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EditMode = DataGridViewEditMode.EditProgrammatically,
        };
        _grid.CellMouseDoubleClick += GridCellDoubleClicked;
        _grid.MouseDown += GridMouseDown;
        _grid.ColumnHeaderMouseClick += GridHeaderClicked;

        Controls.Add(btnRead);
        Controls.Add(btnSettings);
        Controls.Add(filterLabel);
        Controls.Add(_filterBox);
        Controls.Add(_grid);

        AppSettings settings = AppSettings.Load();
        _detectors = settings.检测列;
        _customMenu = settings.右键菜单;
        _repo = new ProjectRepository(EffectiveRoot(settings.项目目录));
        _repo.SetDetectors(_detectors);

        _projectMenu = BuildProjectMenu();
        ContextMenuStrip = _projectMenu;

        RebuildColumns();
    }

    /// <summary>按检测列定义重建表格列序（前5列与“进度”列固定）。</summary>
    private void RebuildColumns()
    {
        _grid.Columns.Clear();
        _defaultWidths = new List<int>();

        for (int i = 0; i < 5; i++)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "col" + 前五列名[i],
                HeaderText = 前五列名[i],
                SortMode = DataGridViewColumnSortMode.Programmatic,
            });
            _defaultWidths.Add(前五列宽[i]);
        }

        foreach (DetectorColumn detector in _detectors)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDet" + detector.名称,
                HeaderText = detector.名称,
                SortMode = DataGridViewColumnSortMode.Programmatic,
            });
            _defaultWidths.Add(检测列默认宽);
        }

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "col进度",
            HeaderText = "进度",
            SortMode = DataGridViewColumnSortMode.Programmatic,
        });
        _defaultWidths.Add(进度列默认宽);

        SetSortGlyph();
        ApplyResize();
    }

    /// <summary>设置中的项目目录为空时，使用程序所在目录（与原版“取运行目录”一致）。</summary>
    private static string EffectiveRoot(string setting) =>
        string.IsNullOrWhiteSpace(setting) ? AppContext.BaseDirectory : setting;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RefreshAll(); // 原程序启动后需要手动读一遍，这里改为启动自动读取
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ApplyResize();
    }

    // ------------------------------------------------------------------
    // 数据刷新与着色（对应 _按钮读取_被单击 / _载入项目 / 子程序颜色表记）
    // ------------------------------------------------------------------

    private void RefreshAll()
    {
        _items = _repo.LoadAll();
        RebuildGrid();
        ApplyRowColors();
    }

    private void RebuildGrid()
    {
        _grid.SuspendLayout();
        _grid.Rows.Clear();

        string query = (_filterBox?.Text ?? "").Trim();
        IEnumerable<ProjectItem> visible = string.IsNullOrEmpty(query)
            ? _items
            : _items.Where(i => MatchFilter(i, query));

        IEnumerable<ProjectItem> sorted = SortItems(visible, _sortCol, !_sortDesc);

        foreach (ProjectItem item in sorted)
        {
            int index = _grid.Rows.Add();
            DataGridViewRow row = _grid.Rows[index];
            row.Cells[0].Value = item.序号;
            row.Cells[1].Value = item.项目名称;
            row.Cells[2].Value = item.归属;
            row.Cells[3].Value = item.生产编号;
            row.Cells[4].Value = item.系统;

            for (int i = 0; i < _detectors.Count; i++)
            {
                bool hasKeyword = _detectors[i].GetKeywords().Count > 0;
                row.Cells[5 + i].Value = hasKeyword
                    ? (item.检测结果[i] ? "有" : "无")
                    : ""; // 无关键字的列（如“备注”）留空白
            }

            row.Cells[5 + _detectors.Count].Value = item.进度;
            row.Tag = item;
        }

        _grid.ResumeLayout();
        ApplyRowColors();
    }

    private static bool MatchFilter(ProjectItem item, string query)
    {
        foreach (string field in (string[])[item.序号, item.项目名称, item.归属, item.生产编号, item.系统, item.进度, item.FolderName])
        {
            if (field.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 按列排序生成展示序列。
    /// 列序结构：0..4 命名段；5..5+n-1 检测列；5+n 进度。
    /// 序号列按数字排序（非数字按字符串）；其余按字符串；检测列 有/无。
    /// </summary>
    private IEnumerable<ProjectItem> SortItems(IEnumerable<ProjectItem> items, int sortCol, bool ascending)
    {
        var list = new List<ProjectItem>(items);
        int detectorCount = _detectors.Count;

        int lastIndex = 5 + detectorCount;
        Comparison<ProjectItem> compare;
        if (sortCol == 0)
        {
            compare = (a, b) => SerialCompare(a.序号, b.序号);
        }
        else if (sortCol >= 5 && sortCol < lastIndex)
        {
            compare = (a, b) => DetectorCompare(a, b, sortCol - 5);
        }
        else
        {
            compare = (a, b) => StringCompare(CellText(a, sortCol), CellText(b, sortCol));
        }

        list.Sort(compare);
        if (!ascending)
        {
            list.Reverse();
        }
        return list;
    }

    private string CellText(ProjectItem item, int col)
    {
        if (col < 5)
        {
            return col switch { 0 => item.序号, 1 => item.项目名称, 2 => item.归属, 3 => item.生产编号, 4 => item.系统, _ => "" };
        }
        if (col < 5 + _detectors.Count)
        {
            bool hasKeyword = _detectors[col - 5].GetKeywords().Count > 0;
            return hasKeyword ? (item.检测结果[col - 5] ? "有" : "无") : "";
        }
        return item.进度;
    }

    private static int StringCompare(string a, string b) =>
        string.Compare(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);

    private static int SerialCompare(string a, string b)
    {
        bool aNum = int.TryParse(a?.Trim(), out int nA);
        bool bNum = int.TryParse(b?.Trim(), out int nB);
        if (aNum && bNum)
        {
            return nA.CompareTo(nB);
        }
        if (aNum != bNum)
        {
            return aNum ? -1 : 1; // 数字排在字符串前
        }
        return string.Compare(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);
    }

    private static int DetectorCompare(ProjectItem a, ProjectItem b, int detectorIndex)
    {
        return a.检测结果[detectorIndex].CompareTo(b.检测结果[detectorIndex]);
    }

    /// <summary>
    /// 着色规则：
    /// 进度=完成的行 → 整行嫩绿；
    /// 其余行默认底色；所有检测列的“无”标记红底，完成行中的“无”用灰底。
    /// </summary>
    private void ApplyRowColors()
    {
        _grid.SuspendLayout();
        int detectorStart = 5;
        int detectorEnd = detectorStart + _detectors.Count - 1; // 含
        foreach (DataGridViewRow row in _grid.Rows)
        {
            var item = row.Tag as ProjectItem;
            if (item == null)
            {
                continue;
            }

            bool done = item.进度 == "完成";

            for (int col = 0; col < _grid.Columns.Count; col++)
            {
                SetCellColor(row.Cells[col], done ? EplColors.嫩绿Brush : EplColors.白色, SystemColors.ControlText);
            }

            for (int col = detectorStart; col <= detectorEnd; col++)
            {
                if ((row.Cells[col].Value as string) == "无")
                {
                    Color bg = done ? EplColors.灰色Brush : EplColors.红色Brush;
                    SetCellColor(row.Cells[col], bg, SystemColors.ControlText);
                }
            }
        }
        _grid.ResumeLayout();
        _grid.Invalidate();
    }

    private static void SetCellColor(DataGridViewCell cell, Color back, Color fore)
    {
        cell.Style.BackColor = back;
        cell.Style.ForeColor = fore;
        // 选中行高亮（否则选中与底色混在一起看不出来）
        cell.Style.SelectionBackColor = Color.FromArgb(204, 232, 255);
        cell.Style.SelectionForeColor = fore;
    }

    // ------------------------------------------------------------------
    // 窗口/按列宽度比例缩放（对应 __启动窗口_尺寸被改变）
    // ------------------------------------------------------------------

    private void ApplyResize()
    {
        if (_grid == null || _defaultWidths == null || _grid.Columns.Count < 7)
        {
            return;
        }

        int clientWidth = ClientSize.Width;
        int clientHeight = ClientSize.Height;
        if (clientWidth <= 0 || clientHeight <= 0)
        {
            return;
        }

        _grid.Bounds = new Rectangle(
            4, 32,
            clientWidth * 列表参考宽度 / 参考宽度,
            clientHeight * 列表参考高度 / 参考高度);

        for (int col = 1; col < _grid.Columns.Count - 1; col++)
        {
            _grid.Columns[col].Width = Math.Max(20, _defaultWidths[col] * clientWidth / 参考宽度);
        }
    }

    // ------------------------------------------------------------------
    // 右键菜单（对应 弹出菜单 (_启动窗口.菜单)）
    // ------------------------------------------------------------------

    private ContextMenuStrip BuildProjectMenu()
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false };

        AddMenuItem(menu, "新建项目", NewProjectMenuClicked);
        AddMenuItem(menu, "打开项目文件夹", (s, e) => OpenSelectedProject());
        menu.Items.Add(new ToolStripSeparator());
        AddMenuItem(menu, "更改项目", EditProjectMenuClicked);
        menu.Items.Add(new ToolStripSeparator());

        // 自定义菜单项（设置 → 定制右键菜单）：把模板复制/解压到选定项目
        foreach (CustomMenuItem mi in _customMenu)
        {
            CustomMenuItem captured = mi;
            AddMenuItem(menu, captured.名称, (s, e) => CopyFromCustomMenu(captured));
        }

        menu.Items.Add(new ToolStripSeparator());
        AddMenuItem(menu, "标记进度", MarkProgressMenuClicked);
        menu.Items.Add(new ToolStripSeparator());
        AddMenuItem(menu, "设置", (s, e) => OpenSettings());

        return menu;
    }

    private static void AddMenuItem(ContextMenuStrip menu, string text, EventHandler onClick)
    {
        var item = new ToolStripMenuItem(text)
        {
            ShowShortcutKeys = false,
        };
        item.Click += onClick;
        menu.Items.Add(item);
    }

    private void GridMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        var hit = _grid.HitTest(e.X, e.Y);
        if (hit.RowIndex >= 0)
        {
            _grid.ClearSelection();
            _grid.Rows[hit.RowIndex].Selected = true;
        }
        _projectMenu.Show(_grid, e.Location);
    }

    private void GridCellDoubleClicked(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            OpenSelectedProject();
        }
    }

    /// <summary>点列头排序；同列反复点击切换升/降序。</summary>
    private void GridHeaderClicked(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.ColumnIndex >= _grid.Columns.Count)
        {
            return;
        }

        if (e.ColumnIndex == _sortCol)
        {
            _sortDesc = !_sortDesc;
        }
        else
        {
            _sortCol = e.ColumnIndex;
            _sortDesc = false;
        }

        SetSortGlyph();
        RebuildGrid();
        ApplyRowColors();
    }

    private void SetSortGlyph()
    {
        if (_grid == null || _sortCol >= _grid.Columns.Count)
        {
            return;
        }

        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.HeaderCell.SortGlyphDirection = SortOrder.None;
        }
        _grid.Columns[_sortCol].HeaderCell.SortGlyphDirection =
            _sortDesc ? SortOrder.Descending : SortOrder.Ascending;
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_repo.RootPath, _detectors, _customMenu);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        AppSettings.Save(new AppSettings
        {
            项目目录 = form.GetProjectRoot(),
            检测列 = form.GetDetectors().ToList(),
            右键菜单 = form.GetCustomMenu().ToList(),
        });

        _detectors = form.GetDetectors();
        _customMenu = form.GetCustomMenu();
        _repo = new ProjectRepository(EffectiveRoot(form.GetProjectRoot()));
        _repo.SetDetectors(_detectors);
        RebuildColumns();
        ContextMenuStrip = BuildProjectMenu();
        RefreshAll();
    }

    // ------------------------------------------------------------------
    // 菜单动作
    // ------------------------------------------------------------------

    private ProjectItem SelectedItem() =>
        _grid.SelectedRows.OfType<DataGridViewRow>().FirstOrDefault()?.Tag as ProjectItem;

    private void RequireSelection(Action<ProjectItem> action)
    {
        var item = SelectedItem();
        if (item == null)
        {
            MessageBox.Show(this, "请先选中项目", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        action(item);
    }

    private void NewProjectMenuClicked(object sender, EventArgs e)
    {
        RefreshAll(); // 原菜单选择“新建项目”时会先刷新列表
        using var form = new NewProjectForm(_repo, _items);
        form.ProjectCreated += RefreshAll;
        form.ShowDialog(this);
    }

    private void EditProjectMenuClicked(object sender, EventArgs e)
    {
        var item = SelectedItem();
        if (item == null)
        {
            MessageBox.Show(this, "请先选择要修改项目", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new EditProjectForm(_repo, item, _items);
        form.ShowDialog(this);
        if (form.WasRenamed())
        {
            RefreshAll();
        }
    }

    private void MarkProgressMenuClicked(object sender, EventArgs e)
    {
        RequireSelection(item =>
        {
            string 进度 = item.进度;
            if (!InputBox.Show(this, "输入进度", "进度标记", ref 进度) || string.IsNullOrEmpty(进度))
            {
                return;
            }

            var edited = new ProjectItem
            {
                FolderName = item.FolderName,
                序号 = item.序号,
                项目名称 = item.项目名称,
                归属 = item.归属,
                生产编号 = item.生产编号,
                系统 = item.系统,
                进度 = 进度,
            };

            try
            {
                _repo.Rename(item, edited.ToFolderName());
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "修改失败，请确认是否打开项目目录的文件\n" + ex.Message,
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        });
    }

    private void OpenSelectedProject()
    {
        RequireSelection(item =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    _repo.PathOf(item))
                {
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "打不开项目目录：" + ex.Message,
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        });
    }

    // ------------------------------------------------------------------
    // 自定义菜单动作：复制模板到选定项目（设置 → 定制右键菜单）
    // ------------------------------------------------------------------

    private void CopyFromCustomMenu(CustomMenuItem mi)
    {
        RequireSelection(item =>
        {
            string tpl = CustomMenuItem.Resolve(mi.模板文件, item);
            if (string.IsNullOrWhiteSpace(tpl))
            {
                MessageBox.Show(this, "菜单“" + mi.名称 + "”未指定模板文件", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string source = Path.IsPathRooted(tpl)
                ? tpl
                : Path.Combine(Templates.模板文件夹, tpl);
            if (!File.Exists(source))
            {
                MessageBox.Show(this,
                    "菜单“" + mi.名称 + "”的模板文件不存在：\n" + source +
                    "\n\n可在 设置 → 定制右键菜单 里用“导出内置模板…”导出后再改。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string targetName = CustomMenuItem.Resolve(mi.目标文件, item)?.Trim();
            bool templateWasEmpty = string.IsNullOrWhiteSpace(mi.目标文件);

            // 关联文件夹：弹出其下文件列表，选定后再创建
            if (Directory.Exists(source))
            {
                using var picker = new TemplatePickForm(source);
                if (picker.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                source = picker.GetSelectedFile();
                if (templateWasEmpty)
                {
                    targetName = null; // 关联文件夹且目标未填：按选中文件原名复制
                }
            }

            if (string.IsNullOrWhiteSpace(targetName))
            {
                targetName = Path.GetFileName(source);
            }
            if (string.IsNullOrWhiteSpace(targetName))
            {
                targetName = Path.GetFileName(source);
            }

            try
            {
                string targetPath = Path.Combine(_repo.PathOf(item), targetName);
                if (CustomMenuItem.IsCompressedFile(source))
                {
                    var choice = MessageBox.Show(this,
                        source + "\n\n是否解压到项目文件夹？\n（选“否”则复制原压缩包）",
                        "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (choice == DialogResult.Cancel)
                    {
                        return;
                    }

                    if (choice == DialogResult.Yes)
                    {
                        // 目标文件非空时视为解压到的子目录名，留空则解压到项目根目录
                        string destDir = string.IsNullOrWhiteSpace(targetName) || targetName == Path.GetFileName(source)
                            ? _repo.PathOf(item)
                            : targetPath;
                        if (RarExtractor.Extract(source, destDir, out string error))
                        {
                            MessageBox.Show(this, "解压成功，已创建到：\n" + destDir, "提示",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            RefreshAll();
                            return;
                        }
                        MessageBox.Show(this, error, "提示",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                string destDirParent = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(destDirParent))
                {
                    Directory.CreateDirectory(destDirParent);
                }
                File.Copy(source, targetPath, overwrite: false);
                MessageBox.Show(this, "复制到项目成功：" + targetName, "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "创建到项目失败\n" + ex.Message, "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        });
    }
}
