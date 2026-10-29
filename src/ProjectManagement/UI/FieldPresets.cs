using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ProjectManagement.Core;

namespace ProjectManagement.UI;

/// <summary>
/// 表单字段的预设候选：
/// 归属 = 「模板」文件夹的一级子文件夹名 ∪ 当前项目列表里出现过的归属；
/// 系统 = 当前项目列表里出现过的系统。
/// （「模板」文件夹下的归属子目录/文件由用户手动创建，软件只读取名称。）
/// </summary>
public static class FieldPresets
{
    public static List<string> 归属预设(IEnumerable<ProjectItem> items) =>
        Merge(FromItems(items, i => i.归属), Templates.模板归属预设());

    public static List<string> 系统预设(IEnumerable<ProjectItem> items) =>
        FromItems(items, i => i.系统);

    private static List<string> FromItems(IEnumerable<ProjectItem> items, Func<ProjectItem, string> pick) =>
        items?.Where(i => i != null).Select(pick).Where(v => v != "").Distinct().OrderBy(v => v).ToList()
        ?? new List<string>();

    private static List<string> Merge(params List<string>[] lists) =>
        lists.SelectMany(l => l ?? new List<string>())
            .Where(v => v != "")
            .Distinct()
            .OrderBy(v => v)
            .ToList();
}
