using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Core;

/// <summary>
/// 一个“检测列”：列头名称 + 关联关键字。
/// 项目文件夹里的文件/一级子目录名包含任一关键字 → 该列记“有”。
/// </summary>
public sealed class DetectorColumn
{
    public string 名称 { get; set; } = "";
    public string 关键字 { get; set; } = "";

    /// <summary>关键字支持多个，用中/英文逗号分隔。</summary>
    public List<string> GetKeywords() =>
        关键字
            .Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim())
            .Where(k => k != "")
            .ToList();

    /// <summary>与原程序一致的默认检测列（顺序即列顺序）。</summary>
    public static List<DetectorColumn> 默认列() => new()
    {
        new DetectorColumn { 名称 = "报告",     关键字 = "质检报告" },
        new DetectorColumn { 名称 = "配置单",   关键字 = "配置单" },
        new DetectorColumn { 名称 = "柜间线",   关键字 = "柜间线" },
        new DetectorColumn { 名称 = "线统计",   关键字 = "柜间线统计" },
        new DetectorColumn { 名称 = "布局存档", 关键字 = "布局存档" },
        new DetectorColumn { 名称 = "配置文件", 关键字 = "配置文件,配置 文件" },
        new DetectorColumn { 名称 = "备注",     关键字 = "" },
    };
}
