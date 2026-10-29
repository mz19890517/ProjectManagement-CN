using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using ProjectManagement.Core;

namespace ProjectManagement.UI;

/// <summary>设置对话框：指定项目目录、编辑检测列（对应原程序“总目录”）。</summary>
public sealed class SettingsForm : Form
{
    private readonly TextBox _rootText;
    private readonly FolderBrowserDialog _folderDialog = new()
    {
        Description = "选择项目目录（即各项目文件夹所在的总目录）",
        UseDescriptionForTitle = true,
    };

    private string _projectRoot;
    private List<DetectorColumn> _detectors;
    private List<CustomMenuItem> _customMenu;

    /// <summary>确定后返回选择的目录；空字符串表示使用程序所在目录。</summary>
    public string GetProjectRoot() => _projectRoot;

    /// <summary>确定后返回编辑后的检测列定义。</summary>
    public IReadOnlyList<DetectorColumn> GetDetectors() => _detectors;

    /// <summary>确定后返回编辑后的自定义右键菜单。</summary>
    public IReadOnlyList<CustomMenuItem> GetCustomMenu() => _customMenu;

    /// <param name="currentRoot">当前生效的项目目录；空字符串表示使用 exe 所在目录。</param>
    /// <param name="currentDetectors">当前检测列定义。</param>
    /// <param name="currentMenu">当前自定义右键菜单。</param>
    public SettingsForm(
        string currentRoot,
        IReadOnlyList<DetectorColumn> currentDetectors,
        IReadOnlyList<CustomMenuItem> currentMenu)
    {
        Text = "设置";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(470, 150);
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

        var label = new Label
        {
            Text = "项目目录：",
            Bounds = new Rectangle(12, 15, 70, 21),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _rootText = new TextBox
        {
            Bounds = new Rectangle(84, 12, 300, 21),
            Text = currentRoot,
        };
        var btnBrowse = new Button
        {
            Text = "浏览…",
            Bounds = new Rectangle(392, 11, 64, 24),
        };
        btnBrowse.Click += OnBrowseClicked;

        var hint = new Label
        {
            Text = "留空表示使用程序所在目录。每个项目是目录下的一个子文件夹。",
            Bounds = new Rectangle(12, 42, 446, 18),
            ForeColor = SystemColors.GrayText,
        };

        var btnEdit = new Button
        {
            Text = "编辑模式…",
            Bounds = new Rectangle(84, 70, 104, 26),
        };
        btnEdit.Click += OnEditColumnsClicked;
        var editHint = new Label
        {
            Text = "编辑列表的列头：增删、改名、调整顺序及关联关键字",
            Bounds = new Rectangle(196, 74, 260, 18),
            ForeColor = SystemColors.GrayText,
        };

        var btnMenu = new Button
        {
            Text = "定制右键菜单…",
            Bounds = new Rectangle(84, 100, 104, 26),
        };
        btnMenu.Click += OnEditMenuClicked;
        var menuHint = new Label
        {
            Text = "右键菜单项与要复制进项目的模板文件对应",
            Bounds = new Rectangle(196, 104, 260, 18),
            ForeColor = SystemColors.GrayText,
        };

        var ok = new Button
        {
            Text = "确定",
            Bounds = new Rectangle(278, 138, 80, 26),
            DialogResult = DialogResult.OK,
        };
        ok.Click += OnOkClicked;
        var cancel = new Button
        {
            Text = "取消",
            Bounds = new Rectangle(370, 138, 80, 26),
            DialogResult = DialogResult.Cancel,
        };

        Controls.AddRange(new Control[] { label, _rootText, btnBrowse, hint, btnEdit, editHint, btnMenu, menuHint, ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;
        _customMenu = currentMenu?
            .Select(c => new CustomMenuItem { 名称 = c.名称, 模板文件 = c.模板文件, 目标文件 = c.目标文件 })
            .ToList() ?? new List<CustomMenuItem>();
        _detectors = currentDetectors?.Select(c => new DetectorColumn { 名称 = c.名称, 关键字 = c.关键字 }).ToList()
            ?? new List<DetectorColumn>();
    }

    private void OnBrowseClicked(object sender, EventArgs e)
    {
        _folderDialog.SelectedPath = Directory.Exists(_rootText.Text)
            ? _rootText.Text
            : "";
        if (_folderDialog.ShowDialog(this) == DialogResult.OK)
        {
            _rootText.Text = _folderDialog.SelectedPath;
        }
    }

    private void OnEditColumnsClicked(object sender, EventArgs e)
    {
        using var form = new ColumnEditorForm(_detectors);
        form.ShowDialog(this);
        if (form.GetResult() != null)
        {
            _detectors = form.GetResult();
        }
    }

    private void OnEditMenuClicked(object sender, EventArgs e)
    {
        using var form = new CustomMenuEditorForm(_customMenu);
        form.ShowDialog(this);
        if (form.GetResult() != null)
        {
            _customMenu = form.GetResult();
        }
    }

    private void OnOkClicked(object sender, EventArgs e)
    {
        string path = _rootText.Text.Trim();
        if (path != "" && !Directory.Exists(path))
        {
            var choice = MessageBox.Show(
                this,
                "目录不存在，是否创建？\n" + path,
                "提示",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (choice != DialogResult.Yes)
            {
                DialogResult = DialogResult.None; // 阻止关闭，继续编辑
                return;
            }
        }
        _projectRoot = path;
    }
}
