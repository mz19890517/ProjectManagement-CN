using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace ProjectManagement.Core;

/// <summary>
/// 程序设置，保存在 exe 同目录的 settings.json（不可写时回退到 %LOCALAPPDATA%）。
/// </summary>
public sealed class AppSettings
{
    /// <summary>项目目录（总目录）；空串表示使用程序所在目录。</summary>
    public string 项目目录 { get; set; } = "";

    /// <summary>检测列定义（编辑模式可增删改）。</summary>
    public List<DetectorColumn> 检测列 { get; set; } = DetectorColumn.默认列();

    /// <summary>自定义右键菜单项。</summary>
    public List<CustomMenuItem> 右键菜单 { get; set; } = CustomMenuItem.默认菜单();

    private static readonly string ExePath =
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    private static readonly string LocalPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "项目管理",
            "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static AppSettings Load()
    {
        try
        {
            var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(ExePath), JsonOpts);
            if (s != null)
            {
                Normalize(s);
                return s;
            }
        }
        catch
        {
        }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Normalize(settings);
        string json = JsonSerializer.Serialize(settings, JsonOpts);
        try
        {
            File.WriteAllText(ExePath, json);
        }
        catch
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LocalPath));
            File.WriteAllText(LocalPath, json);
        }
    }

    private static void Normalize(AppSettings s)
    {
        s.项目目录 ??= "";
        s.检测列 ??= DetectorColumn.默认列();
        foreach (DetectorColumn c in s.检测列.Where(c => c != null))
        {
            c.名称 ??= "";
            c.关键字 ??= "";
        }
        s.右键菜单 ??= CustomMenuItem.默认菜单();
        foreach (CustomMenuItem m in s.右键菜单.Where(m => m != null))
        {
            m.名称 ??= "";
            m.模板文件 ??= "";
            m.目标文件 ??= "";
        }
    }
}
