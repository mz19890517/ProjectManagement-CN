using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProjectManagement.UI;

/// <summary>
/// 模拟易语言“输入框”的通用输入对话框。
/// 对应原：输入框 (提示信息, 窗口标题, 初始文本, 局部变量, 类型, 父窗口)
/// </summary>
public static class InputBox
{
    /// <summary>显示输入框；返回用户点击了确定。text 为初始值/返回值。</summary>
    public static bool Show(IWin32Window owner, string prompt, string title, ref string text)
    {
        using var form = new Form
        {
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = string.IsNullOrEmpty(title) ? "输入" : title,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(340, 120),
            Font = new Font("宋体", 9F),
        };

        var promptLabel = new Label
        {
            Text = prompt,
            Bounds = new Rectangle(12, 10, 316, 18),
        };
        var box = new TextBox
        {
            Bounds = new Rectangle(12, 34, 316, 21),
            Text = text ?? "",
        };
        var ok = new Button
        {
            Text = "确定",
            Bounds = new Rectangle(156, 76, 80, 26),
            DialogResult = DialogResult.OK,
        };
        var cancel = new Button
        {
            Text = "取消",
            Bounds = new Rectangle(248, 76, 80, 26),
            DialogResult = DialogResult.Cancel,
        };

        form.Controls.AddRange(new Control[] { promptLabel, box, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        if (form.ShowDialog(owner) == DialogResult.OK)
        {
            text = box.Text;
            return true;
        }

        return false;
    }
}
