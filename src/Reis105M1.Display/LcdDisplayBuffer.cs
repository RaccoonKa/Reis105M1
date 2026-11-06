using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Reis105M1.Core.Models;

namespace Reis105M1.Display;

public enum LcdGridMode
{
    Ticks,
    FullGrid,
    None
}

public enum LcdScreenMode
{
    Trace,
    MainMenu,
    Memory,
    PowerConfirm,
    CableTable,
    Parameters,
    Battery
}

public class LcdDisplayBuffer
{
    public const int ScreenWidth = 128;
    public const int ScreenHeight = 64;

    private static readonly uint[] BacklightColors =
    {
        0xFF8F9F58,
        0xFFAEC566,
        0xFF6C7C3E
    };

    private const uint ColorFg = 0xFF14180E;

    private int _backlightIndex = 0;
    private uint _currentColorBg = BacklightColors[0];
    private readonly uint[] _pixelBuffer;
    private readonly WriteableBitmap _bitmap;
    private readonly Int32Rect _rect;
    private readonly int _stride;

    public WriteableBitmap Bitmap => _bitmap;
    public LcdGridMode GridMode { get; set; } = LcdGridMode.Ticks;
    public LcdScreenMode ScreenMode { get; set; } = LcdScreenMode.Trace;
    public int ActiveCursorIndex { get; set; } = 0;
    public int BacklightLevel => _backlightIndex;

    private static readonly Dictionary<char, byte[]> Font5x7 = new()
    {
        ['0'] = new byte[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E },
        ['1'] = new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E },
        ['2'] = new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x08, 0x1F },
        ['3'] = new byte[] { 0x1E, 0x01, 0x01, 0x0E, 0x01, 0x01, 0x1E },
        ['4'] = new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 },
        ['5'] = new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E },
        ['6'] = new byte[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E },
        ['7'] = new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 },
        ['8'] = new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E },
        ['9'] = new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C },
        ['.'] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x0C },
        [','] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x04, 0x04, 0x08 },
        [':'] = new byte[] { 0x00, 0x0C, 0x0C, 0x00, 0x0C, 0x0C, 0x00 },
        ['='] = new byte[] { 0x00, 0x1F, 0x00, 0x1F, 0x00, 0x00, 0x00 },
        ['-'] = new byte[] { 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00 },
        ['+'] = new byte[] { 0x00, 0x04, 0x04, 0x1F, 0x04, 0x04, 0x00 },
        ['%'] = new byte[] { 0x19, 0x1A, 0x04, 0x08, 0x0B, 0x13, 0x00 },
        ['/'] = new byte[] { 0x01, 0x02, 0x04, 0x08, 0x10, 0x00, 0x00 },
        ['('] = new byte[] { 0x02, 0x04, 0x08, 0x08, 0x08, 0x04, 0x02 },
        [')'] = new byte[] { 0x08, 0x04, 0x02, 0x02, 0x02, 0x04, 0x08 },
        ['['] = new byte[] { 0x0E, 0x08, 0x08, 0x08, 0x08, 0x08, 0x0E },
        [']'] = new byte[] { 0x0E, 0x02, 0x02, 0x02, 0x02, 0x02, 0x0E },
        ['?'] = new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x00, 0x04 },
        ['!'] = new byte[] { 0x04, 0x04, 0x04, 0x04, 0x04, 0x00, 0x04 },
        ['<'] = new byte[] { 0x02, 0x04, 0x08, 0x10, 0x08, 0x04, 0x02 },
        ['>'] = new byte[] { 0x08, 0x04, 0x02, 0x01, 0x02, 0x04, 0x08 },
        [' '] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
        ['A'] = new byte[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
        ['B'] = new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E },
        ['C'] = new byte[] { 0x0F, 0x10, 0x10, 0x10, 0x10, 0x10, 0x0F },
        ['D'] = new byte[] { 0x1E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1E },
        ['E'] = new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F },
        ['F'] = new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 },
        ['G'] = new byte[] { 0x0F, 0x10, 0x10, 0x17, 0x11, 0x11, 0x0F },
        ['H'] = new byte[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
        ['I'] = new byte[] { 0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E },
        ['J'] = new byte[] { 0x07, 0x02, 0x02, 0x02, 0x02, 0x12, 0x0C },
        ['K'] = new byte[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 },
        ['L'] = new byte[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F },
        ['M'] = new byte[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 },
        ['N'] = new byte[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 },
        ['O'] = new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
        ['P'] = new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 },
        ['Q'] = new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x15, 0x09, 0x16 },
        ['R'] = new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 },
        ['S'] = new byte[] { 0x0F, 0x10, 0x10, 0x0E, 0x01, 0x01, 0x1E },
        ['T'] = new byte[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 },
        ['U'] = new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
        ['V'] = new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 },
        ['W'] = new byte[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x1B, 0x11 },
        ['X'] = new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 },
        ['Y'] = new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 },
        ['Z'] = new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F },
        ['А'] = new byte[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
        ['Б'] = new byte[] { 0x1F, 0x10, 0x1E, 0x11, 0x11, 0x11, 0x1E },
        ['В'] = new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E },
        ['Г'] = new byte[] { 0x1F, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10 },
        ['Д'] = new byte[] { 0x06, 0x0A, 0x0A, 0x0A, 0x0A, 0x1F, 0x11 },
        ['Е'] = new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F },
        ['Ё'] = new byte[] { 0x0A, 0x00, 0x1F, 0x1E, 0x10, 0x10, 0x1F },
        ['Ж'] = new byte[] { 0x15, 0x15, 0x0E, 0x04, 0x0E, 0x15, 0x15 },
        ['З'] = new byte[] { 0x1E, 0x01, 0x02, 0x0C, 0x02, 0x01, 0x1E },
        ['И'] = new byte[] { 0x11, 0x13, 0x15, 0x15, 0x19, 0x11, 0x11 },
        ['Й'] = new byte[] { 0x0A, 0x04, 0x11, 0x15, 0x15, 0x19, 0x11 },
        ['К'] = new byte[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 },
        ['Л'] = new byte[] { 0x07, 0x09, 0x09, 0x09, 0x09, 0x11, 0x11 },
        ['М'] = new byte[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 },
        ['Н'] = new byte[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
        ['О'] = new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
        ['П'] = new byte[] { 0x1F, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11 },
        ['Р'] = new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 },
        ['С'] = new byte[] { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E },
        ['Т'] = new byte[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 },
        ['У'] = new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 },
        ['Ф'] = new byte[] { 0x04, 0x0E, 0x15, 0x15, 0x0E, 0x04, 0x04 },
        ['Х'] = new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 },
        ['Ц'] = new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x1F, 0x01 },
        ['Ч'] = new byte[] { 0x11, 0x11, 0x11, 0x0F, 0x01, 0x01, 0x01 },
        ['Ш'] = new byte[] { 0x15, 0x15, 0x15, 0x15, 0x15, 0x1F, 0x11 },
        ['Щ'] = new byte[] { 0x15, 0x15, 0x15, 0x15, 0x15, 0x1F, 0x01 },
        ['Ъ'] = new byte[] { 0x18, 0x08, 0x0E, 0x09, 0x09, 0x0E, 0x00 },
        ['Ы'] = new byte[] { 0x11, 0x11, 0x1D, 0x13, 0x13, 0x1D, 0x00 },
        ['Ь'] = new byte[] { 0x10, 0x10, 0x1E, 0x11, 0x11, 0x1E, 0x00 },
        ['Э'] = new byte[] { 0x1E, 0x01, 0x01, 0x0F, 0x01, 0x01, 0x1E },
        ['Ю'] = new byte[] { 0x12, 0x15, 0x1D, 0x15, 0x15, 0x12, 0x00 },
        ['Я'] = new byte[] { 0x0F, 0x11, 0x11, 0x0F, 0x05, 0x09, 0x11 },
        ['γ'] = new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x08, 0x10 }
    };

    public LcdDisplayBuffer()
    {
        _stride = ScreenWidth * 4;
        _pixelBuffer = new uint[ScreenWidth * ScreenHeight];
        _bitmap = new WriteableBitmap(ScreenWidth, ScreenHeight, 96, 96, PixelFormats.Bgra32, null);
        _rect = new Int32Rect(0, 0, ScreenWidth, ScreenHeight);
        Clear();
    }

    public void CycleBacklight()
    {
        _backlightIndex = (_backlightIndex + 1) % BacklightColors.Length;
        _currentColorBg = BacklightColors[_backlightIndex];
    }

    public void SetBacklightLevel(int level)
    {
        _backlightIndex = Math.Clamp(level, 0, BacklightColors.Length - 1);
        _currentColorBg = BacklightColors[_backlightIndex];
    }

    public void Clear()
    {
        Array.Fill(_pixelBuffer, _currentColorBg);
    }

    public void ClearDark()
    {
        Array.Fill(_pixelBuffer, 0xFF353A2A);
    }

    public void SetPixel(int x, int y, bool isBlack = true)
    {
        if (x < 0 || x >= ScreenWidth || y < 0 || y >= ScreenHeight)
        {
            return;
        }

        _pixelBuffer[y * ScreenWidth + x] = isBlack ? ColorFg : _currentColorBg;
    }

    public void SetPixelColor(int x, int y, uint color)
    {
        if (x < 0 || x >= ScreenWidth || y < 0 || y >= ScreenHeight)
        {
            return;
        }

        _pixelBuffer[y * ScreenWidth + x] = color;
    }

    public void DrawHLine(int x1, int x2, int y, bool isBlack = true)
    {
        int minX = Math.Max(0, Math.Min(x1, x2));
        int maxX = Math.Min(ScreenWidth - 1, Math.Max(x1, x2));

        for (int x = minX; x <= maxX; x++)
        {
            SetPixel(x, y, isBlack);
        }
    }

    public void DrawVLine(int x, int y1, int y2, bool isBlack = true)
    {
        int minY = Math.Max(0, Math.Min(y1, y2));
        int maxY = Math.Min(ScreenHeight - 1, Math.Max(y1, y2));

        for (int y = minY; y <= maxY; y++)
        {
            SetPixel(x, y, isBlack);
        }
    }

    public void FillRect(int rx, int ry, int w, int h, uint color)
    {
        int minX = Math.Max(0, rx);
        int maxX = Math.Min(ScreenWidth, rx + w);
        int minY = Math.Max(0, ry);
        int maxY = Math.Min(ScreenHeight, ry + h);

        for (int y = minY; y < maxY; y++)
        {
            int rowOffset = y * ScreenWidth;
            for (int x = minX; x < maxX; x++)
            {
                _pixelBuffer[rowOffset + x] = color;
            }
        }
    }

    public void DrawText(string text, int x, int y, bool invert = false)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (invert)
        {
            FillRect(x - 1, y - 1, (text.Length * 6) + 1, 9, ColorFg);
        }

        int curX = x;
        uint charColor = invert ? _currentColorBg : ColorFg;

        foreach (char rawChar in text)
        {
            char c = rawChar == 'γ' ? 'γ' : char.ToUpperInvariant(rawChar);
            if (c == 'Ё') c = 'Е';

            if (Font5x7.TryGetValue(c, out var glyph))
            {
                for (int row = 0; row < 7; row++)
                {
                    byte rowBits = glyph[row];
                    for (int col = 0; col < 5; col++)
                    {
                        if ((rowBits & (0x10 >> col)) != 0)
                        {
                            SetPixelColor(curX + col, y + row, charColor);
                        }
                    }
                }
            }
            curX += 6;
        }
    }

    public void DrawGrid()
    {
        if (GridMode == LcdGridMode.None)
        {
            return;
        }

        if (GridMode == LcdGridMode.Ticks)
        {
            for (int x = 0; x < ScreenWidth; x += 2)
            {
                SetPixel(x, 38, true);
            }

            for (int x = 0; x < ScreenWidth; x += 16)
            {
                DrawVLine(x, 35, 41, true);
            }
        }
        else if (GridMode == LcdGridMode.FullGrid)
        {
            for (int y = 14; y <= 48; y += 11)
            {
                for (int x = 0; x < ScreenWidth; x += 2)
                {
                    SetPixel(x, y, true);
                }
            }

            for (int x = 0; x < ScreenWidth; x += 16)
            {
                for (int y = 12; y <= 50; y += 2)
                {
                    SetPixel(x, y, true);
                }
            }
        }
    }

    public void DrawTrace(IReadOnlyList<double> samples, int baselineY = 38, double scale = 22.0)
    {
        if (samples == null || samples.Count == 0) return;

        double stepX = (double)(ScreenWidth - 1) / (samples.Count - 1);
        int prevX = 0;
        int prevY = baselineY - (int)Math.Round(samples[0] * scale);
        prevY = Math.Clamp(prevY, 13, 50);

        for (int i = 1; i < samples.Count; i++)
        {
            int currX = (int)Math.Round(i * stepX);
            int currY = baselineY - (int)Math.Round(samples[i] * scale);
            currY = Math.Clamp(currY, 13, 50);

            DrawLine(prevX, prevY, currX, currY);

            prevX = currX;
            prevY = currY;
        }
    }

    public void DrawCursor(int x, bool isDotted = false)
    {
        x = Math.Clamp(x, 0, ScreenWidth - 1);

        for (int y = 13; y <= 50; y++)
        {
            if (!isDotted || (y % 2 == 0))
            {
                SetPixel(x, y, true);
            }
        }

        if (isDotted)
        {
            SetPixel(x, 12, true);
            SetPixel(x - 1, 11, true);
            SetPixel(x + 1, 11, true);
            SetPixel(x - 2, 10, true);
            SetPixel(x + 2, 10, true);
        }
        else
        {
            DrawHLine(x - 2, x + 2, 12, true);
            DrawHLine(x - 1, x + 1, 11, true);
            SetPixel(x, 10, true);
        }
    }

    public void DrawBatteryIcon(int x, int y)
    {
        DrawHLine(x, x + 9, y, true);
        DrawHLine(x, x + 9, y + 5, true);
        DrawVLine(x, y, y + 5, true);
        DrawVLine(x + 9, y, y + 5, true);
        DrawVLine(x + 10, y + 1, y + 4, true);
        FillRect(x + 2, y + 2, 6, 2, ColorFg);
    }

    public void DrawTraceHeader(string portText, double shortening)
    {
        DrawText(portText, 2, 2);
        DrawText($"γ:{shortening:F3}", 68, 2);
        DrawBatteryIcon(115, 2);
    }

    public void DrawTraceFooter(double distance, int activeCursor, double cursorPosition, string? badge)
    {
        string distStr = $"L={distance:F1}м";
        DrawText(distStr, 2, 54);

        if (!string.IsNullOrEmpty(badge))
        {
            int badgeX = Math.Max(46, (ScreenWidth - (badge.Length * 6)) / 2);
            DrawText(badge, badgeX, 54, invert: true);
        }
        else
        {
            string curStr = $"[{activeCursor}={cursorPosition:F1}]";
            DrawText(curStr, 46, 54, invert: true);
        }
    }

    public void DrawMainMenuScreen(int selectedIndex = 0)
    {
        Clear();
        DrawText("  ГЛАВНОЕ МЕНЮ  ", 18, 2);
        for (int x = 0; x < ScreenWidth; x += 2) SetPixel(x, 11, true);

        string[] leftItems = { "Нач.устан.", "Аккумулятор", "Укорочение", "Режим", "Усреднение" };
        string[] rightItems = { "КОМПЬЮТЕР", "Калибровка", "ИНФОРМАЦИЯ", "ПАРАМЕТРЫ", "Выход" };

        for (int i = 0; i < 5; i++)
        {
            int y = 14 + (i * 10);
            bool isLeftSelected = (selectedIndex == i);
            bool isRightSelected = (selectedIndex == i + 5);

            DrawText(leftItems[i], 4, y, isLeftSelected);
            DrawText(rightItems[i], 66, y, isRightSelected);
        }
    }

    public void DrawMemoryScreen(int selectedIndex = 1, double freePercent = 98.8)
    {
        Clear();
        DrawText($"СВОБОДНО ПАМЯТИ:{freePercent:F1}%", 4, 2);
        for (int x = 0; x < ScreenWidth; x += 2) SetPixel(x, 11, true);

        string[] items =
        {
            "Запись в память",
            "Быстрая запись",
            "Выход",
            "Чтение из памяти",
            "Удаление из памяти",
            "Изменение имени"
        };

        for (int i = 0; i < items.Length; i++)
        {
            int y = 14 + (i * 8);
            bool isSel = (i == selectedIndex);
            DrawText(items[i], 8, y, isSel);
        }
    }

    public void DrawPowerConfirmScreen()
    {
        Clear();
        DrawText("ВЫКЛЮЧИТЬ ПРИБОР?", 13, 14);

        DrawText("ДА - ", 44, 29);

        int cx = 78;
        int cy = 33;

        SetPixel(cx - 2, cy - 4, true);
        SetPixel(cx - 3, cy - 3, true);
        SetPixel(cx - 4, cy - 2, true);
        SetPixel(cx - 4, cy - 1, true);
        SetPixel(cx - 4, cy,     true);
        SetPixel(cx - 4, cy + 1, true);
        SetPixel(cx - 4, cy + 2, true);
        SetPixel(cx - 3, cy + 3, true);
        SetPixel(cx - 2, cy + 4, true);
        SetPixel(cx - 1, cy + 4, true);
        SetPixel(cx,     cy + 4, true);
        SetPixel(cx + 1, cy + 4, true);
        SetPixel(cx + 2, cy + 4, true);
        SetPixel(cx + 3, cy + 3, true);
        SetPixel(cx + 4, cy + 2, true);
        SetPixel(cx + 4, cy + 1, true);
        SetPixel(cx + 4, cy,     true);
        SetPixel(cx + 4, cy - 1, true);
        SetPixel(cx + 4, cy - 2, true);
        SetPixel(cx + 3, cy - 3, true);
        SetPixel(cx + 2, cy - 4, true);

        DrawVLine(cx, cy - 5, cy, true);

        DrawText("НЕТ - ЛЮБАЯ ДРУГАЯ", 10, 46);
    }

    public void DrawCableTableScreen(int selectedIndex = 3)
    {
        Clear();
        DrawText("ВЫХОД", 4, 2, selectedIndex == -1);
        DrawHLine(0, ScreenWidth - 1, 11, true);

        string[] cables =
        {
            "РК-50-2-11    1.520",
            "РК-50-2-21    1.410",
            "РК-100-7-1    1.200",
            "РК-75-9-12    1.500",
            "ФКБ 1 х 1.3   1.300",
            "ВОЗД. ЛИНИЯ   1.000"
        };

        for (int i = 0; i < cables.Length; i++)
        {
            int y = 14 + (i * 8);
            bool isSel = (i == selectedIndex);
            DrawText(cables[i], 4, y, isSel);
        }

        DrawVLine(124, 13, 62, true);
        DrawVLine(126, 13, 62, true);
        FillRect(124, 25, 3, 14, ColorFg);
    }

    public void DrawParametersScreen(DeviceState state)
    {
        Clear();
        DrawText("ПАРАМЕТРЫ ТРАКТА", 18, 2);
        DrawHLine(0, ScreenWidth - 1, 11, true);

        DrawText($"Диапазон = {state.Range}", 4, 14);
        DrawText($"Укорочение = {state.ShorteningFactor:F3}", 4, 22);
        DrawText($"Растяжка = {state.ZoomFactor}", 4, 30);
        DrawText($"Усиление = {state.GainDb} dB", 4, 38);
        DrawText($"Зонд = 125нс", 4, 46);
        DrawText($"Смещение = {state.VerticalOffsetPercent:F1}%", 4, 54);
    }

    public void DrawBatteryScreen(int percent = 100)
    {
        Clear();
        DrawText("АККУМУЛЯТОР", 32, 4);
        DrawHLine(0, ScreenWidth - 1, 14, true);

        DrawText($"{percent}%", 52, 24);

        int barX = 24;
        int barY = 36;
        int barW = 80;
        int barH = 12;

        DrawHLine(barX, barX + barW, barY, true);
        DrawHLine(barX, barX + barW, barY + barH, true);
        DrawVLine(barX, barY, barY + barH, true);
        DrawVLine(barX + barW, barY, barY + barH, true);
        DrawVLine(barX + barW + 2, barY + 3, barY + barH - 3, true);

        int fillW = (int)Math.Round((barW - 4) * (percent / 100.0));
        FillRect(barX + 2, barY + 2, fillW, barH - 3, ColorFg);

        DrawText("OK - Выход", 36, 52);
    }

    public void RenderToBitmap()
    {
        _bitmap.Lock();
        _bitmap.WritePixels(_rect, _pixelBuffer, _stride, 0);
        _bitmap.Unlock();
    }

    private void DrawLine(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            SetPixel(x0, y0, true);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}