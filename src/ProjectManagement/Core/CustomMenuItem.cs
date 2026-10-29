using System;
using System.Collections.Generic;

namespace ProjectManagement.Core;

/// <summary>
/// 自定义右键菜单项：点击后把“模板文件”按“目标文件”复制到选定项目文件夹里。
/// 支持占位符：{序号} {项目名称} {归属} {生产编号} {系统} {进度}。
/// </summary>
public sealed class CustomMenuItem
{
    public string 名称 { get; set; } = "";
    public string 模板文件 { get; set; } = "";
    /// <summary>留空表示与模板文件同名复制。</summary>
    public string 目标文件 { get; set; } = "";

    /// <summary>与原程序一致的默认菜单（模板文件位于“模板”文件夹）。</summary>
    public static List<CustomMenuItem> 默认菜单() => new()
    {
        new CustomMenuItem { 名称 = "创建所属质检报告模板", 模板文件 = "{归属}质检 报告.xls" },
        new CustomMenuItem { 名称 = "创建柜间线长统计表模板", 模板文件 = "柜间线统 计表.xlsx" },
        new CustomMenuItem { 名称 = "创建归属合格证模板", 模板文件 = "{归属}合格 证.lsf" },
        new CustomMenuItem { 名称 = "创建备注模板", 模板文件 = "备 注.xlsm" },
        new CustomMenuItem { 名称 = "创建对应系统配置文件模板", 模板文件 = "正智配置文件模板.rar", 目标文件 = "{项目名称}配置 文件" },
        new CustomMenuItem { 名称 = "创建通和配置文件模板", 模板文件 = "通和配置文件模板.xlsx", 目标文件 = "{项目名称}配置 文件.xlsx" },
    };

    /// <summary>按扩展名判断压缩包（触发“是否解压”提示）。</summary>
    public static bool IsCompressedFile(string path)
    {
        string ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
        return ext == ".rar" || ext == ".zip" || ext == ".7z";
    }

    /// <summary>替换文本中的项目信息占位符。</summary>
    public static string Resolve(string text, ProjectItem item)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }
        return text
            .Replace("{序号}", item.序号)
            .Replace("{项目名称}", item.项目名称)
            .Replace("{归属}", item.归属)
            .Replace("{生产编号}", item.生产编号)
            .Replace("{系统}", item.系统)
            .Replace("{进度}", item.进度);
    }
}
