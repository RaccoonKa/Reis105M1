using System.Windows;
using System.Windows.Media.Animation;
using Reis105M1.Core.Engine;
using Reis105M1.Core.Models;
using Reis105M1.Core.StateMachine;
using Reis105M1.Display;

namespace Reis105M1.UI;

public partial class MainWindow : Window
{
    private readonly DeviceState _state;
    private readonly MenuStateMachine _stateMachine;
    private readonly ReflectometryEngine _engine;
    private readonly LcdDisplayBuffer _lcdBuffer;
    private readonly List<CableDefect> _defects;

    private bool _isCableConnected = false;
    private bool _isZoomed = false;

    public MainWindow()
    {
        InitializeComponent();

        _state = new DeviceState
        {
            IsPoweredOn = true,
            Range = MeasurementRange.Range100m,
            ShorteningFactor = 1.500,
            GainDb = 20,
            Cursor1Position = 5.0,
            Cursor2Position = 45.0
        };

        _stateMachine = new MenuStateMachine(_state);
        _engine = new ReflectometryEngine();
        _lcdBuffer = new LcdDisplayBuffer();

        _defects = new List<CableDefect>
        {
            new CableDefect(DistanceMeters: 45.0, Type: DefectType.OpenCircuit, ReflectionCoefficient: 0.9)
        };

        LcdImage.Source = _lcdBuffer.Bitmap;
        RenderScreen();
    }

    private void RenderScreen()
    {
        _lcdBuffer.Clear();

        if (_state.IsPoweredOn)
        {
            _lcdBuffer.DrawGrid();

            if (_isCableConnected)
            {
                var trace = _engine.GenerateTrace(_state, _defects, samplesCount: 128);
                _lcdBuffer.DrawTrace(trace, baselineY: 36, scale: 22.0);

                var maxDist = ReflectometryEngine.GetMaxDistanceMeters(_state.Range);
                var cursor1Pixel = (int)(_state.Cursor1Position / maxDist * LcdDisplayBuffer.ScreenWidth);
                var cursor2Pixel = (int)(_state.Cursor2Position / maxDist * LcdDisplayBuffer.ScreenWidth);

                _lcdBuffer.DrawCursor(cursor1Pixel, isDotted: false);
                _lcdBuffer.DrawCursor(cursor2Pixel, isDotted: true);
            }
        }

        _lcdBuffer.RenderToBitmap();
    }

    private void OnPowerButtonClicked(object sender, RoutedEventArgs e)
    {
        _stateMachine.HandleInput(DeviceButton.Power);
        RenderScreen();
    }

    private void OnRangePlusClicked(object sender, RoutedEventArgs e)
    {
        _stateMachine.HandleInput(DeviceButton.RangePlus);
        RenderScreen();
    }

    private void OnRangeMinusClicked(object sender, RoutedEventArgs e)
    {
        _stateMachine.HandleInput(DeviceButton.RangeMinus);
        RenderScreen();
    }

    private void OnGainPlusClicked(object sender, RoutedEventArgs e)
    {
        _stateMachine.HandleInput(DeviceButton.GainPlus);
        RenderScreen();
    }

    private void OnGainMinusClicked(object sender, RoutedEventArgs e)
    {
        _stateMachine.HandleInput(DeviceButton.GainMinus);
        RenderScreen();
    }

    private void OnZoomPlusClicked(object sender, RoutedEventArgs e)
    {
        _state.ZoomFactor = Math.Min(64, _state.ZoomFactor * 2);
        RenderScreen();
    }

    private void OnZoomMinusClicked(object sender, RoutedEventArgs e)
    {
        _state.ZoomFactor = Math.Max(1, _state.ZoomFactor / 2);
        RenderScreen();
    }

    private void OnNavUpClicked(object sender, RoutedEventArgs e)
    {
        _state.VerticalOffsetPercent = Math.Min(100.0, _state.VerticalOffsetPercent + 5.0);
        RenderScreen();
    }

    private void OnNavDownClicked(object sender, RoutedEventArgs e)
    {
        _state.VerticalOffsetPercent = Math.Max(-100.0, _state.VerticalOffsetPercent - 5.0);
        RenderScreen();
    }

    private void OnCursorToggleClicked(object sender, RoutedEventArgs e)
    {
        _stateMachine.HandleInput(DeviceButton.CursorToggle);
        RenderScreen();
    }

    private void OnCursorLeftClicked(object sender, RoutedEventArgs e)
    {
        _stateMachine.HandleInput(DeviceButton.CursorLeft);
        RenderScreen();
    }

    private void OnCursorRightClicked(object sender, RoutedEventArgs e)
    {
        _stateMachine.HandleInput(DeviceButton.CursorRight);
        RenderScreen();
    }

    private void OnShorteningPlusClicked(object sender, RoutedEventArgs e)
    {
        _stateMachine.HandleInput(DeviceButton.ShorteningPlus);
        RenderScreen();
    }

    private void OnShorteningMinusClicked(object sender, RoutedEventArgs e)
    {
        _stateMachine.HandleInput(DeviceButton.ShorteningMinus);
        RenderScreen();
    }

    private void OnModeClicked(object sender, RoutedEventArgs e)
    {
        _stateMachine.HandleInput(DeviceButton.ModeSelect);
        RenderScreen();
    }

    private void OnTurboClicked(object sender, RoutedEventArgs e)
    {
        _state.IsTurbo = !_state.IsTurbo;
        RenderScreen();
    }

    private void OnPulsePlusClicked(object sender, RoutedEventArgs e)
    {
        RenderScreen();
    }

    private void OnPulseMinusClicked(object sender, RoutedEventArgs e)
    {
        RenderScreen();
    }

    private void OnMemoryClicked(object sender, RoutedEventArgs e)
    {
        RenderScreen();
    }

    private void OnBacklightClicked(object sender, RoutedEventArgs e)
    {
        RenderScreen();
    }

    private void OnCompensationPlusClicked(object sender, RoutedEventArgs e)
    {
        RenderScreen();
    }

    private void OnCompensationMinusClicked(object sender, RoutedEventArgs e)
    {
        RenderScreen();
    }

    private void OnConnectCableClicked(object sender, RoutedEventArgs e)
    {
        _isCableConnected = !_isCableConnected;

        var targetY = _isCableConnected ? 104.0 : 55.0;
        var plugAnimation = new DoubleAnimation
        {
            To = targetY,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        CablePlug.BeginAnimation(System.Windows.Controls.Canvas.TopProperty, plugAnimation);
        RenderScreen();
    }

    private void OnZoomCameraClicked(object sender, RoutedEventArgs e)
    {
        _isZoomed = !_isZoomed;

        var targetScale = _isZoomed ? 1.55 : 1.0;
        var targetY = _isZoomed ? -70.0 : 0.0;
        var duration = TimeSpan.FromMilliseconds(600);
        var easing = new QuadraticEase { EasingMode = EasingMode.EaseInOut };

        CameraScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, new DoubleAnimation(targetScale, duration) { EasingFunction = easing });
        CameraScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, new DoubleAnimation(targetScale, duration) { EasingFunction = easing });
        CameraTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation(targetY, duration) { EasingFunction = easing });
    }

    private void OnResetCameraClicked(object sender, RoutedEventArgs e)
    {
        _isZoomed = false;
        var duration = TimeSpan.FromMilliseconds(400);

        CameraScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, new DoubleAnimation(1.0, duration));
        CameraScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, new DoubleAnimation(1.0, duration));
        CameraTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation(0.0, duration));
    }
}