using System;
using System.Drawing;

namespace ProjectManagement.UI;

/// <summary>
/// 易语言标准颜色常量以及 COLORREF（0x00BBGGRR）到 GDI+ 的换算。
/// 数值来源：系统核心支持库 krnln 的常量定义。
/// </summary>
public static class EplColors
{
    public const int 默认色 = -16777216;
    public const int 红色 = 255;
    public const int 灰色 = 8421504;
    public const int 嫩绿 = 57472;

    /// <summary>把易语言 COLORREF 颜色值转换为 System.Drawing.Color。</summary>
    public static Color FromEpl(int value) =>
        Color.FromArgb(value & 0xFF, (value >> 8) & 0xFF, (value >> 16) & 0xFF);

    public static readonly Color 嫩绿Brush = FromEpl(嫩绿);
    public static readonly Color 灰色Brush = FromEpl(灰色);
    public static readonly Color 红色Brush = FromEpl(红色);
    public static readonly Color 白色 = FromEpl(16777215);

    /// <summary>判断易语言颜色值是否为“默认色”（-16777216）。</summary>
    public static bool IsDefault(int value) => value == 默认色;
}
