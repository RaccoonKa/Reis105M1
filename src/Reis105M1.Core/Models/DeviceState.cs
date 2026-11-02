namespace Reis105M1.Core.Models;

/// <summary>
/// Диапазоны измерения расстояния рефлектометра РЕЙС-105М1 в метрах.
/// </summary>
public enum MeasurementRange
{
    Range12_5m = 0,
    Range25m = 1,
    Range50m = 2,
    Range100m = 3,
    Range250m = 4,
    Range500m = 5,
    Range1000m = 6,
    Range2500m = 7,
    Range5000m = 8,
    Range12500m = 9,
    Range25000m = 10
}

/// <summary>
/// Режимы работы прибора.
/// </summary>
public enum DeviceOperatingMode
{
    Reflectometer,
    ResistanceMeter,
    MemoryViewer,
    Calibration
}

/// <summary>
/// Состояние параметров и органов управления прибора РЕЙС-105М1.
/// </summary>
public class DeviceState
{
    public bool IsPoweredOn { get; set; } = true;

    public DeviceOperatingMode OperatingMode { get; set; } = DeviceOperatingMode.Reflectometer;

    public MeasurementRange Range { get; set; } = MeasurementRange.Range100m;

    /// <summary>
    /// Коэффициент укорочения электромагнитной волны в кабеле (VOP factor), типовые значения 1.000 - 7.000.
    /// </summary>
    public double ShorteningFactor { get; set; } = 1.500;

    /// <summary>
    /// Усиление тракта в дБ (0..80 дБ).
    /// </summary>
    public int GainDb { get; set; } = 24;

    /// <summary>
    /// Длительность зондирующего импульса в наносекундах.
    /// </summary>
    public double PulseWidthNs { get; set; } = 10.0;

    /// <summary>
    /// Положение первого измерительного визира (курсора) в метрах или отсчетах.
    /// </summary>
    public double Cursor1Position { get; set; } = 0.0;

    /// <summary>
    /// Положение второго измерительного визира (курсора).
    /// </summary>
    public double Cursor2Position { get; set; } = 50.0;

    /// <summary>
    /// Активный в данный момент курсор для перемещения.
    /// </summary>
    public int ActiveCursorIndex { get; set; } = 1;

    /// <summary>
    /// Разность положений курсоров (расстояние между метками).
    /// </summary>
    public double DeltaDistance => Math.Abs(Cursor2Position - Cursor1Position);
}
