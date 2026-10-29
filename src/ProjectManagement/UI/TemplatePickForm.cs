using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProjectManagement.UI;

/// <summary>
/// 右键菜单项关联了文件夹时弹出：
/// 双击（或选中后确认）其中一个文件 → 创建到当前选定项目。
/// </summary>
public sealed class TemplatePickForm : Form
{
    private readonly ListBox _list;
    private readonly Label _title;

    private readonly string _folder;
    private string _selected;

    /// <summary>确定后返回选中的文件全路径；未选时为 null。</summary>
    public string GetSelectedFile() => _selected;

    public TemplatePickForm(string folder)
    {
        _folder = folder;
        Text = "选择模板文件";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(440, 330);
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

        _title = new Label
        {
            Text = "本项关联的是文件夹，请选择要创建的模板：\n" + folder,
            Bounds = new Rectangle(10, 8, 420, 32),
            ForeColor = SystemColors.GrayText,
        };
        _list = new ListBox
        {
            Bounds = new Rectangle(10, 44, 420, 220),
            HorizontalScrollbar = true,
        };

        foreach (string file in Directory.EnumerateFiles(folder)
            .Select(Path.GetFileName)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
        {
            _list.Items.Add(file);
        }

        _list.DoubleClick += (s, e) => Confirm();
        var ok = new Button
        {
            Text = "确定",
            Bounds = new Rectangle(258, 294, 80, 26),
        };
        ok.Click += (s, e) => Confirm();
        var cancel = new Button
        {
            Text = "取消",
            Bounds = new Rectangle(350, 294, 80, 26),
            DialogResult = DialogResult.Cancel,
        };

        Controls.AddRange(new Control[] { _title, _list, ok, cancel });
        CancelButton = cancel;
    }

    private void Confirm()
    {
        if (_list.SelectedItem == null)
        {
            MessageBox.Show(this, "请先选择一个模板文件", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.None; // 留在选取窗
            return;
        }
        _selected = Path.Combine(_folder, _list.SelectedItem.ToString());
        Close();
    }
}
