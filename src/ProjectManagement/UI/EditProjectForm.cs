using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

using ProjectManagement.Core;

namespace ProjectManagement.UI;

/// <summary>
/// 对应原易语言“窗口1”：更改项目（序号/项目名称/归属/生产编号/系统/进度）。
/// </summary>
public sealed class EditProjectForm : Form
{
    private readonly ProjectRepository _repo;
    private readonly ProjectItem _source;

    private readonly Label _errorLabel;
    private readonly TextBox _edit序号;
    private readonly TextBox _edit项目名称;
    private readonly ComboBox _edit归属;
    private readonly TextBox _edit生产编号;
    private readonly ComboBox _edit系统;
    private readonly TextBox _edit进度;
    private readonly Button _btnConfirm;

    private bool _renamed;

    /// <summary>确认修改成功后为 true。</summary>
    public bool WasRenamed() => _renamed;

    public EditProjectForm(ProjectRepository repo, ProjectItem source, IEnumerable<ProjectItem> existingItems)
    {
        _repo = repo;
        _source = source;

        Text = "";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(645, 246);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("宋体", 9F);
        KeyPreview = true;
        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };

        _errorLabel = new Label
        {
            Bounds = new Rectangle(8, 8, 608, 32),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        (_edit序号, _) = AddRow("序号", 43, 47, 113, 160);
        (_edit项目名称, _) = AddRow("项目名称", 40, 82, 112, 160);
        _edit归属 = AddComboRow("归属", 352, 48, 422, 160, FieldPresets.归属预设(existingItems));
        (_edit生产编号, _) = AddRow("生产编号", 40, 114, 112, 160);
        _edit系统 = AddComboRow("系统", 352, 114, 424, 160, FieldPresets.系统预设(existingItems));
        (_edit进度, _) = AddRow("进度", 351, 80, 423, 160);

        // 原程序双击“进度”标签可直接置为“完成”
        foreach (Control ctrl in Controls)
        {
            if (ctrl is Label label && ctrl.Text == "进度")
            {
                label.DoubleClick += (s, e) => _edit进度.Text = "完成";
            }
        }

        _btnConfirm = new Button
        {
            Text = "确认修改",
            Bounds = new Rectangle(240, 186, 128, 24),
        };
        _btnConfirm.Click += OnConfirmClicked;
        Controls.Add(_btnConfirm);

        // 预填当前选中项目（对应 _窗口1_创建完毕）
        _edit序号.Text = _source.序号;
        _edit项目名称.Text = _source.项目名称;
        _edit归属.Text = _source.归属;
        _edit生产编号.Text = _source.生产编号;
        _edit系统.Text = _source.系统;
        _edit进度.Text = _source.进度;
    }

    private (TextBox, Label) AddRow(string caption, int labelX, int labelY, int editorX, int editorWidth)
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
        return (editor, label);
    }

    /// <summary>下拉选择（DropDown=可输入也可选预设）。预设出自 FieldPresets。</summary>
    private ComboBox AddComboRow(string caption, int labelX, int labelY, int editorX, int editorWidth, IEnumerable<string> presets)
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

    private void OnConfirmClicked(object sender, EventArgs e)
    {
        var edited = new ProjectItem
        {
            FolderName = _source.FolderName,
            序号 = _edit序号.Text,
            项目名称 = _edit项目名称.Text,
            归属 = _edit归属.Text,
            生产编号 = _edit生产编号.Text,
            系统 = _edit系统.Text,
            进度 = _edit进度.Text,
        };

        try
        {
            if (!_repo.Rename(_source, edited.ToFolderName()))
            {
                _errorLabel.Text = "修改失败，文件被占用";
                return;
            }

            _renamed = true;
            _errorLabel.Text = "修改成功";
            _btnConfirm.Enabled = false;

            // 复刻原程序的 1 秒延迟后再关闭窗口
            _ = Task.Delay(1000).ContinueWith(_ =>
                BeginInvoke(new Action(() =>
                {
                    _btnConfirm.Enabled = true;
                    Close();
                })));
        }
        catch (Exception ex)
        {
            _errorLabel.Text = ex.HResult == unchecked((int)0x80070020)
                ? "修改失败，文件被占用"
                : "修改失败：" + ex.Message;
        }
    }
}
