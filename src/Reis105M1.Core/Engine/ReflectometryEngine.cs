using Reis105M1.Core.Models;

namespace Reis105M1.Core.Engine;

/// <summary>
/// Тип неоднородности/дефекта в кабельной линии.
/// </summary>
public enum DefectType
{
    None,
    OpenCircuit,   // Обрыв (положительный импульс отражения, k = +1)
    ShortCircuit,  // КЗ (отрицательный импульс отражения, k = -1)
    MismatchedLoad // Согласованная/рассогласованная муфта или замятие
}

/// <summary>
/// Параметры дефекта в линии.
/// </summary>
public record CableDefect(double DistanceMeters, DefectType Type, double ReflectionCoefficient = 1.0);

/// <summary>
/// Физико-математический движок импульсной рефлектометрии.
/// Рассчитывает распространение импульса, задержку, затухание, отражения и шум.
/// </summary>
public class ReflectometryEngine
{
    private const double SpeedOfLightMetersPerSec = 299_792_458.0;

    /// <summary>
    /// Скорость распространения импульса в среде с учетом коэффициента укорочения: v = c / vop.
    /// </summary>
    public static double CalculateSignalVelocity(double shorteningFactor)
    {
        if (shorteningFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shorteningFactor), "Коэффициент укорочения должен быть строго положительным.");
        }
        return SpeedOfLightMetersPerSec / shorteningFactor;
    }

    /// <summary>
    /// Расчет расстояния по времени задержки отраженного сигнала: L = (v * dt) / 2.
    /// </summary>
    public static double CalculateDistance(double timeDelaySeconds, double shorteningFactor)
    {
        var velocity = CalculateSignalVelocity(shorteningFactor);
        return (velocity * timeDelaySeconds) / 2.0;
    }

    /// <summary>
    /// Расчет задержки времени отражения от дефекта на заданном расстоянии: dt = (2 * L) / v.
    /// </summary>
    public static double CalculateTimeDelay(double distanceMeters, double shorteningFactor)
    {
        var velocity = CalculateSignalVelocity(shorteningFactor);
        return (2.0 * distanceMeters) / velocity;
    }

    /// <summary>
    /// Генерация дискретных точек рефлектограммы для отображения.
    /// </summary>
    /// <param name="state">Текущие настройки прибора (диапазон, усиление, укорочение).</param>
    /// <param name="defects">Список дефектов и неоднородностей в линии.</param>
    /// <param name="samplesCount">Число дискретных отсчетов (по умолчанию 128 точек экрана).</param>
    /// <param name="noiseLevel">Амплитуда шума (0..1).</param>
    public double[] GenerateTrace(DeviceState state, IReadOnlyList<CableDefect> defects, int samplesCount = 128, double noiseLevel = 0.05)
    {
        var trace = new double[samplesCount];
        var maxDistance = GetMaxDistanceMeters(state.Range);
        var distancePerSample = maxDistance / samplesCount;
        var random = new Random(42);

        // 1. Формирование зондирующего импульса в начале линии
        var probeWidthSamples = Math.Max(2, (int)(samplesCount * 0.03));
        for (var i = 0; i < probeWidthSamples; i++)
        {
            var factor = Math.Sin(Math.PI * i / probeWidthSamples);
            trace[i] += 1.0 * factor;
        }

        // 2. Добавление отражений от дефектов с учетом экспоненциального затухания
        var attenuationAlpha = 0.0005; // затухание на метр
        foreach (var defect in defects)
        {
            if (defect.DistanceMeters <= 0 || defect.DistanceMeters > maxDistance)
            {
                continue;
            }

            var sampleIndex = (int)(defect.DistanceMeters / distancePerSample);
            var attenuation = Math.Exp(-attenuationAlpha * defect.DistanceMeters);
            var reflectionAmplitude = defect.Type switch
            {
                DefectType.OpenCircuit => defect.ReflectionCoefficient * attenuation,
                DefectType.ShortCircuit => -defect.ReflectionCoefficient * attenuation,
                DefectType.MismatchedLoad => defect.ReflectionCoefficient * attenuation * 0.5,
                _ => 0.0
            };

            for (var offset = -probeWidthSamples; offset <= probeWidthSamples; offset++)
            {
                var idx = sampleIndex + offset;
                if (idx >= 0 && idx < samplesCount)
                {
                    var bell = Math.Exp(-Math.Pow(offset / 1.5, 2));
                    trace[idx] += reflectionAmplitude * bell;
                }
            }
        }

        // 3. Усиление (Gain) и добавление фонового шума
        var gainMultiplier = Math.Pow(10.0, state.GainDb / 40.0);
        for (var i = 0; i < samplesCount; i++)
        {
            var noise = (random.NextDouble() * 2.0 - 1.0) * noiseLevel;
            trace[i] = (trace[i] + noise) * gainMultiplier;

            // Ограничение амплитуды (клиппинг входного тракта АЦП)
            trace[i] = Math.Clamp(trace[i], -1.0, 1.0);
        }

        return trace;
    }

    public static double GetMaxDistanceMeters(MeasurementRange range) => range switch
    {
        MeasurementRange.Range12_5m => 12.5,
        MeasurementRange.Range25m => 25.0,
        MeasurementRange.Range50m => 50.0,
        MeasurementRange.Range100m => 100.0,
        MeasurementRange.Range250m => 250.0,
        MeasurementRange.Range500m => 500.0,
        MeasurementRange.Range1000m => 1000.0,
        MeasurementRange.Range2500m => 2500.0,
        MeasurementRange.Range5000m => 5000.0,
        MeasurementRange.Range12500m => 12500.0,
        MeasurementRange.Range25000m => 25000.0,
        _ => 100.0
    };
}
