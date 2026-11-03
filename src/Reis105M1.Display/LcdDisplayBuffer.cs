using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Reis105M1.Display;

/// <summary>
/// Виртуальный буфер монохромного ЖК-дисплея 128x64 рефлектометра РЕЙС-105М1.
/// </summary>
public class LcdDisplayBuffer
{
    public const int ScreenWidth = 128;
    public const int ScreenHeight = 64;

    private readonly byte[] _pixels; // 0 = выключен (светлый фон ЖК), 1 = включен (темный пиксель)
    private readonly WriteableBitmap _bitmap;
    private readonly uint[] _colorBuffer;

    private const uint PixelOnColor = 0xFF1C2214;
    private const uint PixelOffColor = 0xFF9EAC67;

    public LcdDisplayBuffer()
    {
        _pixels = new byte[ScreenWidth * ScreenHeight];
        _colorBuffer = new uint[ScreenWidth * ScreenHeight];
        _bitmap = new WriteableBitmap(ScreenWidth, ScreenHeight, 96, 96, PixelFormats.Bgr32, null);
    }

    public WriteableBitmap Bitmap => _bitmap;

    public void Clear(byte value = 0)
    {
        Array.Fill(_pixels, value);
    }

    public void SetPixel(int x, int y, bool isSet)
    {
        if (x >= 0 && x < ScreenWidth && y >= 0 && y < ScreenHeight)
        {
            _pixels[y * ScreenWidth + x] = (byte)(isSet ? 1 : 0);
        }
    }

    /// <summary>
    /// Отрисовка рефлектограммы по отсчетам (нормализованным к -1.0 .. +1.0).
    /// </summary>
    public void DrawTrace(double[] trace, int baselineY = 40, double scale = 20.0)
    {
        var count = Math.Min(ScreenWidth, trace.Length);
        int? prevY = null;

        for (var x = 0; x < count; x++)
        {
            var y = baselineY - (int)(trace[x] * scale);
            y = Math.Clamp(y, 10, ScreenHeight - 1);

            if (prevY.HasValue)
            {
                DrawLine(x - 1, prevY.Value, x, y);
            }
            else
            {
                SetPixel(x, y, true);
            }

            prevY = y;
        }
    }

    /// <summary>
    /// Отрисовка вертикального измерительного курсора (пунктир или сплошная линия).
    /// </summary>
    public void DrawCursor(int x, bool isDotted = false)
    {
        if (x < 0 || x >= ScreenWidth)
        {
            return;
        }

        for (var y = 10; y < ScreenHeight - 2; y++)
        {
            if (!isDotted || y % 2 == 0)
            {
                SetPixel(x, y, true);
            }
        }
    }

    /// <summary>
    /// Отрисовка служебной рамки и шкалы сетки.
    /// </summary>
    public void DrawGrid()
    {
        // Базовая линия нуля
        for (var x = 0; x < ScreenWidth; x += 4)
        {
            SetPixel(x, 40, true);
        }

        // Засечки шкалы
        for (var x = 0; x < ScreenWidth; x += 16)
        {
            SetPixel(x, 38, true);
            SetPixel(x, 42, true);
        }
    }

    /// <summary>
    /// Отрисовка линии алгоритмом Брезенхэма.
    /// </summary>
    public void DrawLine(int x0, int y0, int x1, int y1)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            SetPixel(x0, y0, true);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var e2 = 2 * err;
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

    /// <summary>
    /// Сброс буфера в WriteableBitmap для отображения на WPF UI.
    /// </summary>
    public void RenderToBitmap()
    {
        for (var i = 0; i < _pixels.Length; i++)
        {
            _colorBuffer[i] = _pixels[i] != 0 ? PixelOnColor : PixelOffColor;
        }

        _bitmap.Lock();
        try
        {
            unsafe
            {
                fixed (uint* srcPtr = _colorBuffer)
                {
                    Buffer.MemoryCopy(
                        srcPtr,
                        _bitmap.BackBuffer.ToPointer(),
                        _colorBuffer.Length * sizeof(uint),
                        _colorBuffer.Length * sizeof(uint));
                }
            }
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, ScreenWidth, ScreenHeight));
        }
        finally
        {
            _bitmap.Unlock();
        }
    }
}
