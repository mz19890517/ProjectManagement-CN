using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectManagement.Core;

/// <summary>
/// 单个测试项目的数据模型。
/// 项目文件夹命名规则：序号—项目名称—归属—生产编号—系统—进度
/// 分隔符为全角破折号“—”（U+2014），与原易语言程序保持一致。
/// </summary>
public sealed class ProjectItem
{
    public const char Separator = '—';

    public string FolderName = "";   // 当前实际文件夹名（作为身份锚点）
    public string 序号 = "";
    public string 项目名称 = "";
    public string 归属 = "";
    public string 生产编号 = "";
    public string 系统 = "";
    public string 进度 = "";

    /// <summary>
    /// 各检测列的命中结果（与设置中的“检测列”一一对应，索引一致）。
    /// </summary>
    public readonly List<bool> 检测结果 = new();

    /// <summary>由文件夹名解析项目；不符合命名规则返回 null。</summary>
    /// <remarks>原程序规则：分割出的“—”个数 > 3（即至少 5 段）才认作项目文件夹。</remarks>
    public static ProjectItem Parse(string folderName)
    {
        if (string.IsNullOrEmpty(folderName))
        {
            return null;
        }

        string[] parts = folderName.Split(Separator);
        if (parts.Length < 5)
        {
            return null;
        }

        return new ProjectItem
        {
            FolderName = folderName,
            序号 = parts[0],
            项目名称 = parts[1],
            归属 = parts[2],
            生产编号 = parts[3],
            系统 = parts[4],
            进度 = parts.Length >= 6 ? parts[5] : "",
        };
    }

    public string ToFolderName() =>
        string.Join(Separator, 序号, 项目名称, 归属, 生产编号, 系统, 进度);

    /// <summary>下一个可用序号：现有项目里数字序号取最大值 +1（非数字序号忽略，空列表返回 "1"）。</summary>
    public static string NextSerial(IEnumerable<ProjectItem> items)
    {
        int max = 0;
        if (items != null)
        {
            foreach (ProjectItem item in items)
            {
                if (int.TryParse(item.序号?.Trim(), out int n) && n > max)
                {
                    max = n;
                }
            }
        }
        return (max + 1).ToString();
    }
}

/// <summary>
/// 项目存储层：负责总目录的扫描、检测、改名与创建，
/// 对应原易语言的 目录_枚举子目录 / 文件_枚举W / 目录_改名 / 目录_创建。
/// </summary>
public sealed class ProjectRepository
{
    public string RootPath { get; }

    /// <summary>当前生效的检测列定义（来自设置）。</summary>
    public IReadOnlyList<DetectorColumn> Detectors { get; private set; }

    public ProjectRepository(string rootPath)
    {
        RootPath = rootPath;
        Detectors = DetectorColumn.默认列();
    }

    public void SetDetectors(IReadOnlyList<DetectorColumn> detectors)
    {
        Detectors = detectors ?? DetectorColumn.默认列();
    }

    private string DirOf(ProjectItem item) => Path.Combine(RootPath, item.FolderName);

    public string PathOf(ProjectItem item) => DirOf(item);

    /// <summary>
    /// 读取全部项目。原程序先按名称 a→z 枚举再倒序插入表格，
    /// 这里直接返回正序（a→z），显示更合理。
    /// </summary>
    public List<ProjectItem> LoadAll()
    {
        var result = new List<ProjectItem>();

        if (!Directory.Exists(RootPath))
        {
            return result;
        }

        var names = Directory
            .EnumerateDirectories(RootPath, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (string name in names)
        {
            ProjectItem item = ProjectItem.Parse(name);
            if (item == null)
            {
                continue;
            }

            DetectContents(item);
            result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// 检测项目目录内的文件与一级子目录名，按设置的检测列关键字标记。
    /// 与原程序一致：同时枚举文件和文件夹（立即层，不递归）。
    /// </summary>
    private void DetectContents(ProjectItem item)
    {
        item.检测结果.Clear();
        foreach (var _ in Detectors)
        {
            item.检测结果.Add(false);
        }

        string dir = DirOf(item);
        if (!Directory.Exists(dir) || Detectors.Count == 0)
        {
            return;
        }

        foreach (string f in Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly))
        {
            MarkByName(Path.GetFileName(f), item);
        }

        foreach (string d in Directory.EnumerateDirectories(dir, "*.*", SearchOption.TopDirectoryOnly))
        {
            MarkByName(Path.GetFileName(d), item);
        }
    }

    /// <summary>
    /// 按文件/文件夹名与关键字（大小写不敏感的子串包含）标记。
    /// </summary>
    private void MarkByName(string name, ProjectItem item)
    {
        for (int i = 0; i < Detectors.Count; i++)
        {
            if (item.检测结果[i])
            {
                continue;
            }

            foreach (string keyword in Detectors[i].GetKeywords())
            {
                if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    item.检测结果[i] = true;
                    break;
                }
            }
        }
    }

    /// <summary>重命名项目文件夹（对应 目录_改名）。失败（例如被 Excel 占用）返回 false。</summary>
    public bool Rename(ProjectItem item, string newFolderName)
    {
        string oldPath = DirOf(item);
        string newPath = Path.Combine(RootPath, newFolderName);

        if (string.Equals(item.FolderName, newFolderName, StringComparison.Ordinal))
        {
            return true;
        }

        if (Directory.Exists(newPath))
        {
            return false;
        }

        Directory.Move(oldPath, newPath);
        item.FolderName = newFolderName;
        ParseInto(item, newFolderName);
        return true;
    }

    private static void ParseInto(ProjectItem item, string folderName)
    {
        ProjectItem fresh = ProjectItem.Parse(folderName);
        if (fresh == null)
        {
            return;
        }

        item.序号 = fresh.序号;
        item.项目名称 = fresh.项目名称;
        item.归属 = fresh.归属;
        item.生产编号 = fresh.生产编号;
        item.系统 = fresh.系统;
        item.进度 = fresh.进度;
    }

    /// <summary>创建项目文件夹（对应 目录_创建）。</summary>
    public void Create(ProjectItem item)
    {
        Directory.CreateDirectory(Path.Combine(RootPath, item.ToFolderName()));
    }
}
