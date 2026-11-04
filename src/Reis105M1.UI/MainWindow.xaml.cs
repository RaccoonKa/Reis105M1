using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Reis105M1.Core.Engine;
using Reis105M1.Core.Models;
using Reis105M1.Display;

namespace Reis105M1.UI;

public partial class MainWindow : Window
{
    private record TutorialStep(
        string Title, 
        MeasurementRange Range, 
        double Shortening, 
        double Cursor1, 
        double Cursor2, 
        List<CableDefect> Defects);

    private record KeyDocumentation(string Title, string Summary, string Details);

    private readonly DeviceState _state;
    private readonly ReflectometryEngine _engine;
    private readonly LcdDisplayBuffer _lcdBuffer;
    private readonly List<CableDefect> _currentDefects;
    private readonly List<TutorialStep> _steps;
    private readonly Dictionary<string, KeyDocumentation> _keysDoc;

    private int _currentStepIndex = 0;
    private int _selectedPort = 1;

    public MainWindow()
    {
        InitializeComponent();

        _state = new DeviceState
        {
            IsPoweredOn = true,
            Range = MeasurementRange.Range100m,
            ShorteningFactor = 1.500,
            GainDb = 20,
            Cursor1Position = 0.0,
            Cursor2Position = 50.0
        };

        _engine = new ReflectometryEngine();
        _lcdBuffer = new LcdDisplayBuffer();
        _currentDefects = new List<CableDefect>();

        _steps = new List<TutorialStep>
        {
            new TutorialStep(
                "Назначение прибора и ТБ",
                MeasurementRange.Range100m,
                1.500,
                0.0,
                50.0,
                new List<CableDefect>()
            ),
            new TutorialStep(
                "Подключение кабельной линии",
                MeasurementRange.Range100m,
                1.500,
                0.0,
                70.0,
                new List<CableDefect> { new CableDefect(70.0, DefectType.OpenCircuit, 0.9) }
            ),
            new TutorialStep(
                "Включение и калибровка шкалы",
                MeasurementRange.Range100m,
                1.500,
                0.0,
                70.0,
                new List<CableDefect> { new CableDefect(70.0, DefectType.OpenCircuit, 0.9) }
            ),
            new TutorialStep(
                "Настройка укорочения и диапазона",
                MeasurementRange.Range100m,
                1.500,
                5.0,
                65.0,
                new List<CableDefect> { new CableDefect(65.0, DefectType.OpenCircuit, 0.85) }
            ),
            new TutorialStep(
                "Локализация обрыва жилы",
                MeasurementRange.Range100m,
                1.500,
                0.0,
                65.0,
                new List<CableDefect> { new CableDefect(65.0, DefectType.OpenCircuit, 0.9) }
            ),
            new TutorialStep(
                "Локализация короткого замыкания (КЗ)",
                MeasurementRange.Range100m,
                1.500,
                0.0,
                42.5,
                new List<CableDefect> { new CableDefect(42.5, DefectType.ShortCircuit, 0.95) }
            )
        };

        _keysDoc = new Dictionary<string, KeyDocumentation>
        {
            ["GAIN_PLUS"] = new KeyDocumentation(
                "УСИЛЕНИЕ △", 
                "Увеличение усиления входного тракта рефлектометра с шагом 6 дБ (до +60 дБ).", 
                "Применяется при поиске дефектов на длинных кабельных линиях, где сигнал затухает. Позволяет различить слабые отражения от муфт и повреждений в конце трассы."
            ),
            ["GAIN_MINUS"] = new KeyDocumentation(
                "УСИЛЕНИЕ ▽", 
                "Уменьшение усиления входного тракта рефлектометра с шагом 6 дБ.", 
                "Используется, если сигнал перегружен, выходит за верхнюю/нижнюю границу экрана дисплея или зашумлен паразитными наводками."
            ),
            ["RANGE_PLUS"] = new KeyDocumentation(
                "ДИАПАЗОН ▷", 
                "Увеличение измеряемого расстояния (шкалы развертки).", 
                "Переключает шкалу ряда: 12.5, 25, 50, 100, 200, 400, 800, 1600, 3200, 6400, 12800, 25600 м. Выбирается так, чтобы конец кабеля укладывался в пределы экрана."
            ),
            ["RANGE_MINUS"] = new KeyDocumentation(
                "ДИАПАЗОН ◁", 
                "Уменьшение измеряемого расстояния (шкалы развертки).", 
                "Позволяет сузить шкалу для детального исследования ближней зоны кабеля и локализации близко расположенных муфт."
            ),
            ["ZOOM_PLUS"] = new KeyDocumentation(
                "РАСТЯЖКА △", 
                "Увеличение растяжки вокруг активного курсора в 2, 4, 8... раз.", 
                "Служит для детального просмотра рефлектограммы и точного совмещения курсора с фронтом импульса (погрешность отсчета снижается)."
            ),
            ["ZOOM_MINUS"] = new KeyDocumentation(
                "РАСТЯЖКА ▽", 
                "Уменьшение коэффициента растяжки.", 
                "Возвращает обзорный масштаб для контроля общей картины линии."
            ),
            ["CURSOR_TOGGLE"] = new KeyDocumentation(
                "КУРСОР", 
                "Выбор активного измерительного курсора (Курсор 0 или Курсор 1).", 
                "Курсор 0 выставляется на начало зондирующего импульса, Курсор 1 — на фронт отражения. Дистанция L между ними рассчитывается прибором автоматически."
            ),
            ["CURSOR_LEFT"] = new KeyDocumentation(
                "СТРЕЛКА ◀ (Влево)", 
                "Смещение активного курсора влево по временной оси.", 
                "Используется для точного подведения курсора к точке перегиба отраженного импульса."
            ),
            ["CURSOR_RIGHT"] = new KeyDocumentation(
                "СТРЕЛКА ▶ (Вправо)", 
                "Смещение активного курсора вправо по временной оси.", 
                "Служит для перемещения курсора вглубь линии к удаленным повреждениям."
            ),
            ["NAV_UP"] = new KeyDocumentation(
                "СТРЕЛКА ▲ (Вверх)", 
                "Смещение линии развертки вверх по вертикали / навигация в меню.", 
                "Позволяет вернуть график в центр экрана при постоянных смещениях сигнала или увеличить усреднение."
            ),
            ["NAV_DOWN"] = new KeyDocumentation(
                "СТРЕЛКА ▼ (Вниз)", 
                "Смещение линии развертки вниз по вертикали / навигация в меню.", 
                "Сдвигает базовую линию для удобства наблюдения разнополярных всплесков КЗ."
            ),
            ["SHORTENING_PLUS"] = new KeyDocumentation(
                "УКОРОЧЕНИЕ ▷", 
                "Увеличение коэффициента укорочения (КУ) с шагом 0.001.", 
                "КУ (γ) отражает замедление волны в диэлектрике кабеля (V = c / γ). Без точной установки КУ расчет расстояния будет неверным."
            ),
            ["SHORTENING_MINUS"] = new KeyDocumentation(
                "УКОРОЧЕНИЕ ◁", 
                "Уменьшение коэффициента укорочения (КУ) с шагом 0.001.", 
                "Позволяет откалибровать скорость распространения импульса под марку измеряемого кабеля."
            ),
            ["MENU_OK"] = new KeyDocumentation(
                "ОК / МЕНЮ", 
                "Вызов главного меню / подтверждение выбора параметра.", 
                "Открывает доступ к контролю батареи, усреднению помех, калибровке, библиотеке кабелей и связи с ПК по RS-232."
            ),
            ["TURBO"] = new KeyDocumentation(
                "ТУРБО", 
                "Включение мощного зондирующего импульса (амплитуда не менее 25 В).", 
                "Удерживается для 'пробивания' кабелей с огромным затуханием (протяженные телефонные пары малого сечения ТПП), когда обычный сигнал затухает в ноль."
            ),
            ["PULSE_PLUS"] = new KeyDocumentation(
                "ИМПУЛЬС ▷", 
                "Увеличение длительности зондирующего импульса (до 5 мкс).", 
                "Широкий импульс несет больше энергии и увеличивает дальность обнаружения дефектов на дальнем конце линии."
            ),
            ["PULSE_MINUS"] = new KeyDocumentation(
                "ИМПУЛЬС ◁", 
                "Уменьшение длительности зондирующего импульса (до 10 нс).", 
                "Узкий короткий импульс обеспечивает высочайшую разрешающую способность, позволяя различить дефекты, находящиеся близко друг к другу."
            ),
            ["MEMORY"] = new KeyDocumentation(
                "ПАМЯТЬ", 
                "Работа с энергонезависимым архивом (более 200 снимков линий).", 
                "Позволяет записать эталонную рефлектограмму кабеля и затем сравнивать ее с поврежденным проводом в режимах 'Сравнение' и 'Разность'."
            ),
            ["COMP_PLUS"] = new KeyDocumentation(
                "КОМПЕНСАЦИЯ ▷", 
                "Включение и увеличение противоимпульса компенсации.", 
                "Действует на шкалах от 800 м и устраняет искажения и 'лыжу' в начале трассы, открывая спрятанные близкие повреждения."
            ),
            ["COMP_MINUS"] = new KeyDocumentation(
                "КОМПЕНСАЦИЯ ◁", 
                "Уменьшение длительности импульса компенсации.", 
                "Подстраивает уровень подавления собственного переходного процесса прибора."
            ),
            ["BACKLIGHT"] = new KeyDocumentation(
                "ПОДСВЕТКА (☀)", 
                "Включение / выключение подсветки ЖК-дисплея.", 
                "Обеспечивает четкую видимость графиков и курсоров при измерениях в темных кабельных коллекторах, подвалах и колодцах."
            ),
            ["POWER"] = new KeyDocumentation(
                "ПИТАНИЕ (⏻)", 
                "Включение и отключение прибора.", 
                "При включении прибор автоматически калибрует внутренние генераторы и проверяет уровень заряда четырех аккумуляторов АА."
            )
        };

        LcdImage.Source = _lcdBuffer.Bitmap;
        StepsListBox.SelectedIndex = 0;
        ApplyStep(0);
    }

    private string GetStepDescription(int stepIndex, int port)
    {
        var portName = port == 2 ? "ВХОД 2" : "ВХОД 1";

        return stepIndex switch
        {
            0 => "РЕЙС-105М1 — цифровой импульсный рефлектометр для определения расстояний до повреждений и неоднородностей линий связи и силовых кабелей. Золотое правило безопасности: все измерения проводятся строго на отключенной линии с обеих сторон, жилы предварительно разряжаются на заземляющее устройство!",
            1 => port == 0 
                ? "Кабель отключен от прибора. Выберите ВХОД 1 или ВХОД 2 в блоке подключения, чтобы направить зондирующий импульс в соответствующий канал."
                : port == 2
                    ? "Присоединительный кабель подключен к разъему ВХОД 2 на верхнем торце. Прибор проводит одиночные измерения через ВХОД 2 с той же точностью, что и через ВХОД 1 — схема входов полностью равнозначна. Разделение на два входа служит для режимов 'Сравнение', 'Разность' и 'Раздельный'."
                    : "Присоединительный кабель подключен к разъему ВХОД 1 на верхнем торце. Сигнал от катушки поступает в первый измерительный тракт. Вы также можете переключить провод на ВХОД 2 — для одиночного поиска обрыва или КЗ оба входа работают идентично.",
            2 => $"При нажатии кнопки питания прибор проводит внутреннюю автокалибровку. На монохромном ЖК-дисплее 128x64 формируется координатная сетка и линия нуля. Отраженный сигнал от измерительной катушки принимается трактом {portName}.",
            3 => $"Скорость электромагнитной волны в кабеле зависит от материала изоляции (V = c / γ). Для радиочастотных кабелей и связи коэффициент укорочения равен γ ≈ 1.500. Диапазон развертки для канала {portName} подбирается так, чтобы отраженный сигнал от конца линии полностью помещался на экране.",
            4 => $"При обрыве или разомкнутом конце линии отраженный импульс имеет ту же полярность, что и зондирующий (направлен вверх). Рефлектометр по каналу {portName} регистрирует импульс обрыва на дистанции L = 65.0 м. Курсор 0 совмещается с началом импульса, курсор 1 — с фронтом отражения.",
            5 => $"При коротком замыкании между жилами или жилы на броню отраженный импульс меняет полярность на противоположную (направлен строго вниз). На канале {portName} курсор 1 наводится на начало перегиба импульса КЗ, и оператор получает точную дистанцию L = 42.5 м до аварийного участка.",
            _ => string.Empty
        };
    }

    private void ApplyStep(int index)
    {
        if (index < 0 || index >= _steps.Count)
        {
            return;
        }

        _currentStepIndex = index;
        var step = _steps[index];

        var activePort = index == 0 ? 0 : (_selectedPort == 0 ? 1 : _selectedPort);
        if (index > 0 && _selectedPort == 0)
        {
            _selectedPort = 1;
        }

        TxtStepHeader.Text = $"Этап {index + 1} из {_steps.Count}";
        TxtStepTitle.Text = step.Title;
        TxtStepDescription.Text = GetStepDescription(index, activePort);
        StepProgress.Value = ((index + 1.0) / _steps.Count) * 100.0;

        _state.Range = step.Range;
        _state.ShorteningFactor = step.Shortening;
        _state.Cursor1Position = step.Cursor1;
        _state.Cursor2Position = step.Cursor2;

        _currentDefects.Clear();
        _currentDefects.AddRange(step.Defects);

        TxtInfoRange.Text = $"{ReflectometryEngine.GetMaxDistanceMeters(_state.Range):F1} м";
        TxtInfoShortening.Text = $"{_state.ShorteningFactor:F3}";
        TxtInfoDistance.Text = $"{_state.MeasuredDistance:F2} м";

        SetCablePort(activePort);

        if (StepsListBox.SelectedIndex != index)
        {
            StepsListBox.SelectedIndex = index;
        }

        RenderScreen();
    }

    private void RenderScreen()
    {
        _lcdBuffer.Clear();

        if (_state.IsPoweredOn)
        {
            _lcdBuffer.DrawGrid();

            if (_selectedPort > 0 && _currentStepIndex > 0)
            {
                var trace = _engine.GenerateTrace(_state, _currentDefects, samplesCount: 128);
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

    private void SetCablePort(int port)
    {
        _selectedPort = port;

        double targetX;
        double targetY;
        double endX;

        if (port == 1)
        {
            targetX = 541.0;
            targetY = 36.0;
            endX = 557.0;
            TxtInfoPort.Text = "ВХОД 1 (Активен)";
            BtnPort1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            BtnPort2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
            BtnUnplug.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
        }
        else if (port == 2)
        {
            targetX = 647.0;
            targetY = 36.0;
            endX = 663.0;
            TxtInfoPort.Text = "ВХОД 2 (Активен)";
            BtnPort1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
            BtnPort2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            BtnUnplug.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
        }
        else
        {
            targetX = 541.0;
            targetY = -25.0;
            endX = 557.0;
            TxtInfoPort.Text = "Отключен";
            BtnPort1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
            BtnPort2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
            BtnUnplug.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
        }

        var duration = TimeSpan.FromMilliseconds(450);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseInOut };

        var animX = new DoubleAnimation(targetX, duration) { EasingFunction = ease };
        var animY = new DoubleAnimation(targetY, duration) { EasingFunction = ease };

        CablePlug.BeginAnimation(Canvas.LeftProperty, animX);
        CablePlug.BeginAnimation(Canvas.TopProperty, animY);

        var p2X = port == 2 ? 663.0 : 557.0;
        var p1X = port == 2 ? 320.0 : 297.0;

        var animCableP3 = new PointAnimation(new Point(endX, targetY), duration) { EasingFunction = ease };
        var animCableP2 = new PointAnimation(new Point(p2X, targetY - 66), duration) { EasingFunction = ease };
        var animCableP1 = new PointAnimation(new Point(p1X, 267), duration) { EasingFunction = ease };

        var animShadowP3 = new PointAnimation(new Point(endX, targetY + 10), duration) { EasingFunction = ease };
        var animShadowP2 = new PointAnimation(new Point(p2X, targetY - 56), duration) { EasingFunction = ease };

        CableBezier.BeginAnimation(BezierSegment.Point3Property, animCableP3);
        CableBezier.BeginAnimation(BezierSegment.Point2Property, animCableP2);
        CableBezier.BeginAnimation(BezierSegment.Point1Property, animCableP1);

        CableBorderBezier.BeginAnimation(BezierSegment.Point3Property, animCableP3);
        CableBorderBezier.BeginAnimation(BezierSegment.Point2Property, animCableP2);
        CableBorderBezier.BeginAnimation(BezierSegment.Point1Property, animCableP1);

        CableShadowBezier.BeginAnimation(BezierSegment.Point3Property, animShadowP3);
        CableShadowBezier.BeginAnimation(BezierSegment.Point2Property, animShadowP2);

        TxtStepDescription.Text = GetStepDescription(_currentStepIndex, _selectedPort);

        RenderScreen();
    }

    private void OnKeyInspectClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is string keyTag && _keysDoc.TryGetValue(keyTag, out var doc))
        {
            TxtInspectorKeyTitle.Text = doc.Title;
            TxtInspectorSummary.Text = doc.Summary;
            TxtInspectorDetail.Text = doc.Details;

            var flashAnim = new ColorAnimation
            {
                From = (Color)ColorConverter.ConvertFromString("#38BDF8"),
                To = (Color)ColorConverter.ConvertFromString("#0284C7"),
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true
            };
            InspectorCard.BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, flashAnim);
        }
    }

    private void OnSelectPort1Clicked(object sender, RoutedEventArgs e)
    {
        SetCablePort(1);
    }

    private void OnSelectPort2Clicked(object sender, RoutedEventArgs e)
    {
        SetCablePort(2);
    }

    private void OnUnplugClicked(object sender, RoutedEventArgs e)
    {
        SetCablePort(0);
    }

    private void OnNextStepClicked(object sender, RoutedEventArgs e)
    {
        if (_currentStepIndex < _steps.Count - 1)
        {
            ApplyStep(_currentStepIndex + 1);
        }
    }

    private void OnPrevStepClicked(object sender, RoutedEventArgs e)
    {
        if (_currentStepIndex > 0)
        {
            ApplyStep(_currentStepIndex - 1);
        }
    }

    private void OnStepSelected(object sender, SelectionChangedEventArgs e)
    {
        if (StepsListBox.SelectedIndex >= 0 && StepsListBox.SelectedIndex != _currentStepIndex)
        {
            ApplyStep(StepsListBox.SelectedIndex);
        }
    }

    private void OnCloseAppClicked(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void OnMinimizeAppClicked(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
}