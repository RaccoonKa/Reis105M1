using Reis105M1.Core.Engine;
using Reis105M1.Core.Models;
using Reis105M1.Core.StateMachine;
using Xunit;

namespace Reis105M1.Core.Tests;

public class ReflectometryEngineTests
{
    [Fact]
    public void CalculateSignalVelocity_WithStandardFactor_ReturnsExpectedVelocity()
    {
        // Arrange
        double vop = 1.5;

        // Act
        double velocity = ReflectometryEngine.CalculateSignalVelocity(vop);

        // Assert
        Assert.True(velocity > 199_000_000 && velocity < 201_000_000);
    }

    [Fact]
    public void CalculateDistance_And_CalculateTimeDelay_AreReciprocal()
    {
        // Arrange
        double distance = 120.0; // 120 метров
        double vop = 1.66;

        // Act
        double timeDelay = ReflectometryEngine.CalculateTimeDelay(distance, vop);
        double calculatedDist = ReflectometryEngine.CalculateDistance(timeDelay, vop);

        // Assert
        Assert.Equal(distance, calculatedDist, precision: 5);
    }

    [Fact]
    public void GenerateTrace_ShouldProduceValidSamplePoints()
    {
        // Arrange
        var engine = new ReflectometryEngine();
        var state = new DeviceState { Range = MeasurementRange.Range100m, GainDb = 10 };
        var defects = new List<CableDefect>
        {
            new CableDefect(50.0, DefectType.OpenCircuit, 1.0)
        };

        // Act
        var trace = engine.GenerateTrace(state, defects, samplesCount: 128);

        // Assert
        Assert.NotNull(trace);
        Assert.Equal(128, trace.Length);
        Assert.All(trace, val => Assert.InRange(val, -1.0, 1.0));
    }

    [Fact]
    public void MenuStateMachine_RangeAndGain_ButtonsChangeState()
    {
        // Arrange
        var state = new DeviceState { Range = MeasurementRange.Range100m, GainDb = 20, IsPoweredOn = true };
        var fsm = new MenuStateMachine(state);

        // Act
        fsm.HandleInput(DeviceButton.RangePlus);
        fsm.HandleInput(DeviceButton.GainPlus);

        // Assert
        Assert.Equal(MeasurementRange.Range250m, state.Range);
        Assert.Equal(22, state.GainDb);
    }
}
