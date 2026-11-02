using Reis105M1.Core.Models;

namespace Reis105M1.Core.StateMachine;

/// <summary>
/// Команды нажатия клавиш на лицевой панели РЕЙС-105М1.
/// </summary>
public enum DeviceButton
{
    Power,
    RangePlus,
    RangeMinus,
    GainPlus,
    GainMinus,
    ShorteningPlus,
    ShorteningMinus,
    CursorToggle,
    CursorLeft,
    CursorRight,
    ModeSelect,
    Enter
}

/// <summary>
/// Конечный автомат интерфейса и меню прибора.
/// </summary>
public class MenuStateMachine
{
    private readonly DeviceState _state;

    public MenuStateMachine(DeviceState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public DeviceState State => _state;

    /// <summary>
    /// Обработка нажатия клавиши панели прибора.
    /// </summary>
    public void HandleInput(DeviceButton button)
    {
        if (button == DeviceButton.Power)
        {
            _state.IsPoweredOn = !_state.IsPoweredOn;
            return;
        }

        if (!_state.IsPoweredOn)
        {
            return; // При выключенном питании клавиши не действуют
        }

        switch (button)
        {
            case DeviceButton.RangePlus:
                if ((int)_state.Range < (int)MeasurementRange.Range25000m)
                {
                    _state.Range = (MeasurementRange)((int)_state.Range + 1);
                }
                break;

            case DeviceButton.RangeMinus:
                if ((int)_state.Range > (int)MeasurementRange.Range12_5m)
                {
                    _state.Range = (MeasurementRange)((int)_state.Range - 1);
                }
                break;

            case DeviceButton.GainPlus:
                _state.GainDb = Math.Min(80, _state.GainDb + 2);
                break;

            case DeviceButton.GainMinus:
                _state.GainDb = Math.Max(0, _state.GainDb - 2);
                break;

            case DeviceButton.ShorteningPlus:
                _state.ShorteningFactor = Math.Round(_state.ShorteningFactor + 0.005, 3);
                break;

            case DeviceButton.ShorteningMinus:
                _state.ShorteningFactor = Math.Max(1.000, Math.Round(_state.ShorteningFactor - 0.005, 3));
                break;

            case DeviceButton.CursorToggle:
                _state.ActiveCursorIndex = _state.ActiveCursorIndex == 1 ? 2 : 1;
                break;

            case DeviceButton.CursorLeft:
                MoveActiveCursor(-1.0);
                break;

            case DeviceButton.CursorRight:
                MoveActiveCursor(1.0);
                break;

            case DeviceButton.ModeSelect:
                _state.OperatingMode = _state.OperatingMode switch
                {
                    DeviceOperatingMode.Reflectometer => DeviceOperatingMode.ResistanceMeter,
                    DeviceOperatingMode.ResistanceMeter => DeviceOperatingMode.MemoryViewer,
                    DeviceOperatingMode.MemoryViewer => DeviceOperatingMode.Calibration,
                    DeviceOperatingMode.Calibration => DeviceOperatingMode.Reflectometer,
                    _ => DeviceOperatingMode.Reflectometer
                };
                break;
        }
    }

    private void MoveActiveCursor(double delta)
    {
        if (_state.ActiveCursorIndex == 1)
        {
            _state.Cursor1Position = Math.Max(0.0, _state.Cursor1Position + delta);
        }
        else
        {
            _state.Cursor2Position = Math.Max(0.0, _state.Cursor2Position + delta);
        }
    }
}
