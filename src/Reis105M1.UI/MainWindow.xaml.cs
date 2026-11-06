using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
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
    private bool _isCrocodilesConnected = false;

    private SoundPlayer? _clickSoundPlayer;

    public MainWindow()
    {
        InitializeComponent();
        InitializeAssets();

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
                "Назначение прибора и подготовка",
                MeasurementRange.Range100m,
                1.500,
                0.0,
                50.0,
                new List<CableDefect>()
            ),
            new TutorialStep(
                "Подключение зажимов к жилам",
                MeasurementRange.Range100m,
                1.500,
                0.0,
                70.0,
                new List<CableDefect> { new CableDefect(70.0, DefectType.OpenCircuit, 0.9) }
            ),
            new TutorialStep(
                "Подключение к рефлектометру",
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
                "Настройка координатной сетки",
                MeasurementRange.Range100m,
                1.500,
                0.0,
                70.0,
                new List<CableDefect> { new CableDefect(70.0, DefectType.OpenCircuit, 0.9) }
            ),
            new TutorialStep(
                "Библиотека кабелей и выбор КУ",
                MeasurementRange.Range100m,
                1.500,
                0.0,
                70.0,
                new List<CableDefect> { new CableDefect(70.0, DefectType.OpenCircuit, 0.9) }
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
        SetCrocodilesConnected(false, animate: false);
        ApplyStep(0);
    }

    private static Stream? FindAssetStream(string extension, string subfolderHint)
    {
        try
        {
            var asm = typeof(MainWindow).Assembly;
            var resNames = asm.GetManifestResourceNames();

            foreach (var name in resNames)
            {
                if (name.IndexOf(subfolderHint, StringComparison.OrdinalIgnoreCase) >= 0 &&
                    name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    var stream = asm.GetManifestResourceStream(name);
                    if (stream != null)
                    {
                        var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        ms.Position = 0;
                        return ms;
                    }
                }
            }

            foreach (var name in resNames)
            {
                if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    var stream = asm.GetManifestResourceStream(name);
                    if (stream != null)
                    {
                        var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        ms.Position = 0;
                        return ms;
                    }
                }
            }
        }
        catch { }

        try
        {
            string[] searchDirs = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", subfolderHint),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subfolderHint),
                Path.Combine(Directory.GetCurrentDirectory(), "assets", subfolderHint),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "assets", subfolderHint)
            };

            foreach (var dir in searchDirs)
            {
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "*" + extension);
                    if (files.Length > 0)
                    {
                        return new MemoryStream(File.ReadAllBytes(files[0]));
                    }
                }
            }
        }
        catch { }

        return null;
    }

    private static MemoryStream CreateSynthesizedClickWav()
    {
        var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true);

        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.025);
        short[] pcm = new short[samples];

        for (int i = 0; i < samples; i++)
        {
            double t = (double)i / sampleRate;
            double envelope = Math.Exp(-t * 240.0);
            double wave = Math.Sin(2.0 * Math.PI * 1300.0 * t) * 0.65 + Math.Sin(2.0 * Math.PI * 650.0 * t) * 0.35;
            pcm[i] = (short)(wave * envelope * 24000);
        }

        int subChunk2Size = samples * 2;
        int chunkSize = 36 + subChunk2Size;

        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(chunkSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);

        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(subChunk2Size);
        for (int i = 0; i < samples; i++)
        {
            bw.Write(pcm[i]);
        }

        bw.Flush();
        ms.Position = 0;
        return ms;
    }

    private void InitializeAssets()
    {
        try
        {
            var iconStream = FindAssetStream(".ico", "icon") 
                          ?? FindAssetStream(".png", "icon")
                          ?? FindAssetStream(".ico", "")
                          ?? FindAssetStream(".png", "");

            if (iconStream != null)
            {
                ImageSource? iconSource = null;
                try
                {
                    iconSource = BitmapFrame.Create(iconStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
                catch
                {
                    iconStream.Position = 0;
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = iconStream;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    iconSource = bmp;
                }

                if (iconSource != null)
                {
                    this.Icon = iconSource;

                    var appLogoImage = FindName("AppLogoImage") as Image;
                    var appLogoGlyph = FindName("AppLogoGlyph") as TextBlock;

                    if (appLogoImage != null && appLogoGlyph != null)
                    {
                        appLogoImage.Source = iconSource;
                        appLogoImage.Visibility = Visibility.Visible;
                        appLogoGlyph.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
        catch { }

        try
        {
            var soundStream = FindAssetStream(".wav", "sound") ?? CreateSynthesizedClickWav();
            _clickSoundPlayer = new SoundPlayer(soundStream);
            _clickSoundPlayer.Load();
        }
        catch { }

        EventManager.RegisterClassHandler(
            typeof(Button), 
            ButtonBase.ClickEvent, 
            new RoutedEventHandler(OnGlobalButtonClick));
    }

    private void OnGlobalButtonClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _clickSoundPlayer?.Play();
        }
        catch { }
    }

    private string GetStepDescription(int stepIndex, int port)
    {
        var portName = port == 2 ? "ВХОД 2" : "ВХОД 1";

        return stepIndex switch
        {
            0 => "РЕЙС-105М1 — цифровой импульсный рефлектометр для обнаружения повреждений и неоднородностей в силовых кабелях и линиях связи. Перед подключением зажимов убедитесь, что линия отключена с обеих сторон и разряжена. Нажмите «Далее ▶», чтобы подключить измерительные зажимы к исследуемой паре жил кабеля.",
            1 => "Зажимы типа «крокодил» подключены к исследуемой паре жил кабеля: красный зажим («ВХОД / СИГНАЛ») соединен с первой жилой, черный зажим («ЗЕМЛЯ / ЭКРАН») — со второй жилой. Зондирующий импульс направляется непосредственно в исследуемую линию.",
            2 => port == 0 
                ? "Присоединительный кабель отключен от рефлектометра. Выберите ВХОД 1 или ВХОД 2, чтобы соединить линию со схемой прибора."
                : port == 2
                    ? "Присоединительный кабель BNC-разъемом подключен к гнезду ВХОД 2 на верхнем торце. Прибор проводит одиночные измерения через ВХОД 2 с той же точностью, что и через ВХОД 1 — оба входа схемотехнически равнозначны."
                    : "Присоединительный кабель подключен к разъему ВХОД 1 на верхнем торце. Сигнал от выбранных жил поступает в первый измерительный тракт рефлектометра.",
            3 => $"При нажатии кнопки питания прибор проводит внутреннюю автокалибровку. На монохромном ЖК-дисплее 128x64 формируется координатная сетка и линия нуля. Отраженный сигнал от исследуемой кабельной пары принимается трактом {portName}.",
            4 => $"Координатная сетка дисплея для канала {portName} переключается кнопками прибора между 3 режимами: 1) Засечки (быстрая визуальная шкала делений через каждые 16 пикселей); 2) Клетки (полная сетка 8х4 для измерения амплитуды в вольтах); 3) Без сетки (чистый экран для поиска мелких дефектов без визуальных помех).",
            5 => $"Для точного вычисления дистанции по каналу {portName} необходимо установить коэффициент укорочения (КУ). В приборе есть энергонезависимая память до 64 типов линий. Кликните меню или кнопку укорочения, чтобы выбрать значение КУ под марку линии.",
            6 => $"При обрыве или разомкнутом конце линии отраженный импульс имеет ту же полярность, что и зондирующий (направлен вверх). Рефлектометр по каналу {portName} регистрирует импульс обрыва на дистанции L = 65.0 м. Курсор 0 совмещается с началом импульса, курсор 1 — с фронтом отражения.",
            7 => $"При коротком замыкании между жилами или жилы на броню отраженный импульс меняет полярность на противоположную (направлен строго вниз). На канале {portName} курсор 1 наводится на начало перегиба импульса КЗ, и оператор получает точную дистанцию L = 42.5 м до аварийного участка.",
            _ => string.Empty
        };
    }

    private void SetCrocodilesConnected(bool connected, bool animate = true)
    {
        _isCrocodilesConnected = connected;

        double targetRedX = connected ? 255.0 : 330.0;
        double targetRedY = connected ? 149.0 : 230.0;
        double targetBlackX = connected ? 255.0 : 330.0;
        double targetBlackY = connected ? 220.0 : 285.0;

        Point redStart = connected ? new Point(375, 170) : new Point(450, 250);
        Point redP1 = connected ? new Point(400, 170) : new Point(440, 250);
        Point redP2 = connected ? new Point(420, 205) : new Point(435, 225);

        Point blackStart = connected ? new Point(375, 241) : new Point(450, 305);
        Point blackP1 = connected ? new Point(400, 241) : new Point(440, 305);
        Point blackP2 = connected ? new Point(420, 224) : new Point(435, 230);

        if (!animate)
        {
            Canvas.SetLeft(CrocodileRed, targetRedX);
            Canvas.SetTop(CrocodileRed, targetRedY);
            Canvas.SetLeft(CrocodileBlack, targetBlackX);
            Canvas.SetTop(CrocodileBlack, targetBlackY);

            CrocodileLeadRedFigure.StartPoint = redStart;
            CrocodileLeadRedBezier.Point1 = redP1;
            CrocodileLeadRedBezier.Point2 = redP2;

            CrocodileLeadBlackFigure.StartPoint = blackStart;
            CrocodileLeadBlackBezier.Point1 = blackP1;
            CrocodileLeadBlackBezier.Point2 = blackP2;
            return;
        }

        var duration = TimeSpan.FromMilliseconds(450);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseInOut };

        var animRedX = new DoubleAnimation(targetRedX, duration) { EasingFunction = ease };
        var animRedY = new DoubleAnimation(targetRedY, duration) { EasingFunction = ease };
        var animBlackX = new DoubleAnimation(targetBlackX, duration) { EasingFunction = ease };
        var animBlackY = new DoubleAnimation(targetBlackY, duration) { EasingFunction = ease };

        CrocodileRed.BeginAnimation(Canvas.LeftProperty, animRedX);
        CrocodileRed.BeginAnimation(Canvas.TopProperty, animRedY);
        CrocodileBlack.BeginAnimation(Canvas.LeftProperty, animBlackX);
        CrocodileBlack.BeginAnimation(Canvas.TopProperty, animBlackY);

        var animRedStart = new PointAnimation(redStart, duration) { EasingFunction = ease };
        var animRedP1 = new PointAnimation(redP1, duration) { EasingFunction = ease };
        var animRedP2 = new PointAnimation(redP2, duration) { EasingFunction = ease };

        CrocodileLeadRedFigure.BeginAnimation(PathFigure.StartPointProperty, animRedStart);
        CrocodileLeadRedBezier.BeginAnimation(BezierSegment.Point1Property, animRedP1);
        CrocodileLeadRedBezier.BeginAnimation(BezierSegment.Point2Property, animRedP2);

        var animBlackStart = new PointAnimation(blackStart, duration) { EasingFunction = ease };
        var animBlackP1 = new PointAnimation(blackP1, duration) { EasingFunction = ease };
        var animBlackP2 = new PointAnimation(blackP2, duration) { EasingFunction = ease };

        CrocodileLeadBlackFigure.BeginAnimation(PathFigure.StartPointProperty, animBlackStart);
        CrocodileLeadBlackBezier.BeginAnimation(BezierSegment.Point1Property, animBlackP1);
        CrocodileLeadBlackBezier.BeginAnimation(BezierSegment.Point2Property, animBlackP2);

        if (connected)
        {
            _clickSoundPlayer?.Play();
        }
    }

    private void ApplyStep(int index)
    {
        if (index < 0 || index >= _steps.Count)
        {
            return;
        }

        _currentStepIndex = index;
        var step = _steps[index];

        var activePort = (_selectedPort == 0 && index > 2) ? 1 : _selectedPort;

        if (index == 0)
        {
            SetCrocodilesConnected(false, animate: true);
            TxtInfoClips.Text = "Отключены";
            TxtInfoClips.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
        }
        else
        {
            SetCrocodilesConnected(true, animate: true);
            TxtInfoClips.Text = "Подключены";
            TxtInfoClips.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        }

        if (index == 4)
        {
            _lcdBuffer.ScreenMode = LcdScreenMode.Trace;
            _lcdBuffer.GridMode = LcdGridMode.FullGrid;
        }
        else if (index == 5)
        {
            _lcdBuffer.ScreenMode = LcdScreenMode.CableTable;
        }
        else
        {
            _lcdBuffer.ScreenMode = LcdScreenMode.Trace;
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
        TxtInfoGrid.Text = _lcdBuffer.GridMode switch
        {
            LcdGridMode.Ticks => "Засечки",
            LcdGridMode.FullGrid => "Клетки",
            _ => "Без сетки"
        };

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
            switch (_lcdBuffer.ScreenMode)
            {
                case LcdScreenMode.MainMenu:
                    _lcdBuffer.DrawMainMenuScreen(0);
                    break;
                case LcdScreenMode.CableTable:
                    _lcdBuffer.DrawCableTableScreen(3);
                    break;
                case LcdScreenMode.Parameters:
                    _lcdBuffer.DrawParametersScreen(_state);
                    break;
                case LcdScreenMode.Battery:
                    _lcdBuffer.DrawBatteryScreen(100);
                    break;
                default:
                    _lcdBuffer.DrawGrid();
                    _lcdBuffer.DrawText($"КУ:{_state.ShorteningFactor:F3}", 2, 2);
                    _lcdBuffer.DrawText(_selectedPort == 2 ? "ВХОД 2" : "ВХОД 1", 64, 2);
                    _lcdBuffer.DrawBatteryIcon(112, 2);

                    if (_selectedPort > 0 && _isCrocodilesConnected)
                    {
                        var trace = _engine.GenerateTrace(_state, _currentDefects, samplesCount: 128);
                        _lcdBuffer.DrawTrace(trace, baselineY: 34, scale: 20.0);

                        var maxDist = ReflectometryEngine.GetMaxDistanceMeters(_state.Range);
                        var cursor1Pixel = (int)(_state.Cursor1Position / maxDist * LcdDisplayBuffer.ScreenWidth);
                        var cursor2Pixel = (int)(_state.Cursor2Position / maxDist * LcdDisplayBuffer.ScreenWidth);

                        _lcdBuffer.DrawCursor(cursor1Pixel, isDotted: false);
                        _lcdBuffer.DrawCursor(cursor2Pixel, isDotted: true);
                    }

                    _lcdBuffer.DrawText($"L={_state.MeasuredDistance:F2}м", 2, 54);
                    _lcdBuffer.DrawText("КУРСОР 1", 76, 54, invert: true);
                    break;
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
        var p1X = port == 2 ? 490.0 : 475.0;

        var animCableP3 = new PointAnimation(new Point(endX, targetY), duration) { EasingFunction = ease };
        var animCableP2 = new PointAnimation(new Point(p2X, targetY - 66), duration) { EasingFunction = ease };
        var animCableP1 = new PointAnimation(new Point(p1X, 205), duration) { EasingFunction = ease };

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

    private void OnAboutClicked(object sender, RoutedEventArgs e)
    {
        AboutModal.Visibility = Visibility.Visible;
    }

    private void OnCloseAboutClicked(object sender, RoutedEventArgs e)
    {
        AboutModal.Visibility = Visibility.Collapsed;
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