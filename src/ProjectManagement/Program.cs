using System;
using System.Threading;
using System.Windows.Forms;

using ProjectManagement.UI;

namespace ProjectManagement;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\项目管理_V31_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "程序已在运行，请勿重复启动。",
                "提示",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        GC.KeepAlive(mutex);
    }
}
