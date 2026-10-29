using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectManagement.Core;

/// <summary>
/// 模板资源访问。模板以嵌入资源的形式随程序发布，
/// 资源名形如 NTpl_模板易能质检报告。
/// </summary>
public static class Templates
{
    public const string 正智配置文件Rar名 = "正智配置文件模板.rar";

    /// <summary>exe 旁的“模板”文件夹。</summary>
    public static string 模板文件夹 =>
        Path.Combine(AppContext.BaseDirectory, "模板");

    /// <summary>嵌入资源 → 导出时的真实文件名（文件名中的空格与原版一致）。</summary>
    private static readonly Dictionary<string, string> 导出名 = new()
    {
        ["模板易能质检报告"] = "易能质检 报告.xls",
        ["模板丰源质检报告"] = "丰源质检 报告.xls",
        ["模板长园深瑞质检报告"] = "长园深瑞质检 报告.xls",
        ["模板南京国聚质检报告"] = "南京国聚质检 报告.xls",
        ["模板众盛质检报告"] = "众盛质检 报告.xls",
        ["模板易能合格证"] = "易能合格 证.lsf",
        ["模板丰源合格证"] = "丰源合格 证.lsf",
        ["模板长园深瑞合格证"] = "长园深瑞合格 证.lsf",
        ["模板南京国聚合格证"] = "南京国聚合格 证.lsf",
        ["模板众盛合格证"] = "众盛合格 证.lsf",
        ["模板柜间线长统计表"] = "柜间线统 计表.xlsx",
        ["模板备注"] = "备 注.xlsm",
        ["模板正智配置文件模板"] = "正智配置文件模板.rar",
        ["模板通和配置文件模板"] = "通和配置文件模板.xlsx",
    };

    /// <summary>「模板」文件夹下一级子文件夹名（手动创建，作为归属预设；自动归一化目录分隔符）。</summary>
    public static List<string> 模板归属预设()
    {
        string folder = 模板文件夹;
        if (!Directory.Exists(folder))
        {
            return new List<string>();
        }
        return Directory.EnumerateDirectories(folder)
            .Select(Path.GetFileName)
            .Where(n => n != "")
            .OrderBy(n => n)
            .ToList();
    }

    /// <summary>把全部内置模板导出到“模板”文件夹（已存在则跳过）。返回导出文件数。</summary>
    public static int 导出内置模板(string folder)
    {
        Directory.CreateDirectory(folder);
        int count = 0;
        foreach ((string resource, string fileName) in 导出名)
        {
            string target = Path.Combine(folder, fileName);
            if (File.Exists(target) || !Exists(resource))
            {
                continue;
            }
            WriteToFile(target, resource);
            count++;
        }
        return count;
    }

    public static byte[] GetBytes(string logicalName)
    {
        using var stream = typeof(Templates).Assembly.GetManifestResourceStream("NTpl_" + logicalName);
        if (stream == null)
        {
            throw new FileNotFoundException("找不到嵌入模板资源：" + logicalName);
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public static bool Exists(string logicalName) =>
        typeof(Templates).Assembly.GetManifestResourceNames()
            .Contains("NTpl_" + logicalName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 把模板写出为项目目录下的文件（对应原 krnln 写到文件）。
    /// 父目录不存在时自动创建（原程序未做，属小改进）。
    /// </summary>
    public static string WriteToFile(string path, string logicalName)
    {
        string dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(path, GetBytes(logicalName));
        return path;
    }
}
