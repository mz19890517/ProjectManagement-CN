using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using ProjectManagement.Core;

namespace ProjectManagement.UI;

/// <summary>
/// 对应原易语言“窗口新建项目”。校验通过后创建
/// “序号—项目名称—归属—生产编号—系统—未完成”文件夹并刷新列表。
/// </summary>
public sealed class NewProjectForm : Form
{
    private readonly ProjectRepository _repo;
    private readonly List<ProjectItem> _existingItems;

    private readonly Label _errorLabel;
    private readonly TextBox _edit序号;
    private readonly TextBox _edit项目名称;
    private readonly ComboBox _edit归属;
    private readonly TextBox _edit生产编号;
    private readonly ComboBox _edit系统;

    /// <summary>创建成功后触发（主窗口据此刷新列表）。</summary>
    public event Action ProjectCreated;

    public NewProjectForm(ProjectRepository repo, List<ProjectItem> existingItems)
    {
        _repo = repo;
        _existingItems = existingItems;

        Text = "新建项目";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(623, 225);
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

        _errorLabel = new Label
        {
            Bounds = new Rectangle(0, 8, 608, 32),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Firebrick,
        };
        Controls.Add(_errorLabel);

        _edit序号 = AddRow("序号", 35, 47, 105, 160);
        _edit项目名称 = AddRow("项目名称", 32, 82, 104, 472);
        _edit归属 = AddComboRow("归属", 344, 48, 414, 160, FieldPresets.归属预设(_existingItems));
        _edit生产编号 = AddRow("生产编号", 32, 114, 104, 160);
        _edit系统 = AddComboRow("系统", 344, 114, 416, 160, FieldPresets.系统预设(_existingItems));

        // 序号默认填当前列表最大序号 + 1
        _edit序号.Text = ProjectItem.NextSerial(_existingItems);

        var btnConfirm = new Button
        {
            Text = "确认新建",
            Bounds = new Rectangle(240, 162, 128, 24),
        };
        btnConfirm.Click += OnConfirmClicked;
        Controls.Add(btnConfirm);
    }

    private TextBox AddRow(string caption, int labelX, int labelY, int editorX, int editorWidth)
    {
        var label = new Label
        {
            Text = caption,
            Bounds = new Rectangle(labelX, labelY, 64, 32),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var editor = new TextBox
        {
            Bounds = new Rectangle(editorX, labelY, editorWidth, 32),
        };
        Controls.Add(label);
        Controls.Add(editor);
        return editor;
    }

    /// <summary>下拉选择（DropDown=可输入也可选预设）。预设出自 FieldPresets。</summary>
    private ComboBox AddComboRow(string caption, int labelX, int labelY, int editorX, int editorWidth, List<string> presets)
    {
        var label = new Label
        {
            Text = caption,
            Bounds = new Rectangle(labelX, labelY, 64, 32),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var editor = new ComboBox
        {
            Bounds = new Rectangle(editorX, labelY, editorWidth, 32),
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
        };
        foreach (string preset in presets)
        {
            editor.Items.Add(preset);
        }
        Controls.Add(label);
        Controls.Add(editor);
        return editor;
    }

    /// <summary>
    /// 校验规则与原程序一致：
    /// 序号/项目名称/生产编号不得与现有项目重复或为空，归属/系统不得为空。
    /// （调整为先全部校验、再一次性建目录，修复原版把建目录写进校验循环的缺陷。）
    /// </summary>
    private void OnConfirmClicked(object sender, EventArgs e)
    {
        string error = ValidateInput();
        if (error != null)
        {
            _errorLabel.Text = error;
            return;
        }

        var item = new ProjectItem
        {
            序号 = _edit序号.Text,
            项目名称 = _edit项目名称.Text,
            归属 = _edit归属.Text,
            生产编号 = _edit生产编号.Text,
            系统 = _edit系统.Text,
            进度 = "未完成",
        };

        try
        {
            _repo.Create(item);
            _errorLabel.Text = "创建成功";
            ProjectCreated?.Invoke();
        }
        catch (Exception ex)
        {
            _errorLabel.Text = "创建失败：" + ex.Message;
        }
    }

    private string ValidateInput()
    {
        string 序号 = _edit序号.Text;
        string 项目名称 = _edit项目名称.Text;
        string 归属 = _edit归属.Text;
        string 生产编号 = _edit生产编号.Text;
        string 系统 = _edit系统.Text;

        if (序号 == "" && 项目名称 == "" && 归属 == "" && 生产编号 == "" && 系统 == "")
        {
            return "有未输入内容，请核实";
        }

        foreach (ProjectItem it in _existingItems)
        {
            if (序号 == it.序号 || 序号 == "")
            {
                return "有重复序号或者有未输入内容，请核实";
            }

            if (项目名称 == it.项目名称 || 项目名称 == "")
            {
                return "有重复项目名称或者有未输入内容，请核实";
            }

            if (生产编号 == it.生产编号 || 生产编号 == "")
            {
                return "有重复生产编号或者有未输入内容，请核实";
            }
        }

        if (归属 == "")
        {
            return "归属未输入内容，请核实";
        }

        if (系统 == "")
        {
            return "系统有未输入内容，请核实";
        }

        return null;
    }
}
