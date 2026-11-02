using System;

namespace Reis105M1.Core.Models;

public sealed class DeviceState
{
    public bool IsPoweredOn { get; set; } = true;
    public MeasurementRange Range { get; set; } = MeasurementRange.Range100m;
    public double ShorteningFactor { get; set; } = 1.500;
    public int GainDb { get; set; } = 20;
    public double OutputResistance { get; set; } = 50.0;
    public bool IsTurbo { get; set; }

    public int ActiveCursorIndex { get; set; } = 1;
    public double Cursor1Position { get; set; } = 5.0;
    public double Cursor2Position { get; set; } = 45.0;

    public DeviceOperatingMode OperatingMode { get; set; } = DeviceOperatingMode.Reflectometer;

    public double CurrentRange { get; set; } = 100.0;
    public double VerticalOffsetPercent { get; set; } = 0.0;
    public int ZoomFactor { get; set; } = 1;

    public double MeasuredDistance => Math.Abs(Cursor2Position - Cursor1Position);
}