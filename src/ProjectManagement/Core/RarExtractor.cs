using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

using SharpCompress.Common;
using SharpCompress.Readers;

namespace ProjectManagement.Core;

/// <summary>
/// RAR 解压：优先使用 SharpCompress（内置支持 RAR），
/// 失败时回退到系统里可能存在的 7-Zip / WinRAR / UnRAR 命令行工具。
/// </summary>
public static class RarExtractor
{
    public static bool Extract(string rarPath, string targetDir, out string error)
    {
        error = "";
        try
        {
            if (TrySharpCompress(rarPath, targetDir))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            error = "内置解压失败：" + ex.Message;
        }

        foreach (string[] cli in CliCandidates())
        {
            try
            {
                return RunCli(cli[0], cli[1], rarPath, targetDir, out error);
            }
            catch (Exception)
            {
                // 继续尝试下一个外部工具
            }
        }

        error = string.IsNullOrEmpty(error) ? "未找到可用的 RAR 解压工具" : error;
        return false;
    }

    private static bool TrySharpCompress(string rarPath, string targetDir)
    {
        using FileStream stream = File.OpenRead(rarPath);
        using IReader reader = ReaderFactory.Open(stream);
        Directory.CreateDirectory(targetDir);
        while (reader.MoveToNextEntry())
        {
            if (!reader.Entry.IsDirectory)
            {
                reader.WriteEntryToDirectory(
                    targetDir,
                    new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true,
                    });
            }
        }
        return true;
    }

    private static IEnumerable<string[]> CliCandidates()
    {
        yield return new[] { @"C:\Program Files\7-Zip\7z.exe", @"x ""{0}"" -y -o""{1}""" };
        yield return new[] { @"C:\Program Files (x86)\7-Zip\7z.exe", @"x ""{0}"" -y -o""{1}""" };
        yield return new[] { @"C:\Program Files\WinRAR\UnRAR.exe", @"x -y ""{0}"" ""{1}\""" };
        yield return new[] { @"C:\Program Files (x86)\WinRAR\UnRAR.exe", @"x -y ""{0}"" ""{1}\""" };
        yield return new[] { "UnRAR.exe", @"x -y ""{0}"" ""{1}\""" };
        yield return new[] { "7z.exe", @"x ""{0}"" -y -o""{1}""" };
    }

    private static bool RunCli(string exe, string argsFormat, string rarPath, string targetDir, out string error)
    {
        error = "";
        Directory.CreateDirectory(targetDir);
        string args = string.Format(CultureInfo.InvariantCulture, argsFormat, rarPath, targetDir);

        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardError = true,
        };
        using var p = Process.Start(psi);
        string stdErr = p.StandardError.ReadToEnd();
        p.WaitForExit(120000);
        if (p.ExitCode != 0)
        {
            error = Path.GetFileName(exe) + " 解压失败（退出码 " + p.ExitCode + "）："
                    + stdErr.Trim();
        }
        return p.ExitCode == 0;
    }
}
