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
using System.Windows.Threading;
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

    private PathFigure _leadRedFigure = null!;
    private BezierSegment _leadRedBezier = null!;
    private PathFigure _leadBlackFigure = null!;
    private BezierSegment _leadBlackBezier = null!;

    private int _currentStepIndex = 0;
    private int _selectedPort = 1;
    private bool _isCrocodilesClamped = false;
    private int _activeCursor = 0;
    private int _zoomFactor = 1;
    private bool _isTurbo = false;
    private int _pulseWidthNs = 125;
    private int _menuIndex = 0;
    private int _memoryIndex = 1;
    private int _baselineShift = 0;
    private int _outputImpedanceIndex = 1;
    private static readonly int[] ImpedanceValues = { 20, 100, 200, 300, 400, 500 };

    private string? _transientBadge = null;
    private DispatcherTimer? _badgeTimer;

    private SoundPlayer? _clickSoundPlayer;

    public MainWindow()
    {
        InitializeComponent();
        InitializeLeadGeometries();
        InitializeAssets();

        _state = new DeviceState
        {
            IsPoweredOn = true,
            Range = MeasurementRange.Range100m,
            ShorteningFactor = 1.379,
            GainDb = 20,
            Cursor1Position = 0.0,
            Cursor2Position = 42.5
        };

        _engine = new ReflectometryEngine();
        _lcdBuffer = new LcdDisplayBuffer();
        _currentDefects = new List<CableDefect>();

        _steps = new List<TutorialStep>
        {
            new TutorialStep(
                "Назначение прибора и подготовка",
                MeasurementRange.Range100m,
                1.379,
                0.0,
                50.0,
                new List<CableDefect>()
            ),
            new TutorialStep(
                "Подключение зажимов к жилам",
                MeasurementRange.Range100m,
                1.379,
                0.0,
                70.0,
                new List<CableDefect> { new CableDefect(70.0, DefectType.OpenCircuit, 0.9) }
            ),
            new TutorialStep(
                "Подключение к рефлектометру",
                MeasurementRange.Range100m,
                1.379,
                0.0,
                70.0,
                new List<CableDefect> { new CableDefect(70.0, DefectType.OpenCircuit, 0.9) }
            ),
            new TutorialStep(
                "Включение и калибровка шкалы",
                MeasurementRange.Range100m,
                1.379,
                0.0,
                70.0,
                new List<CableDefect> { new CableDefect(70.0, DefectType.OpenCircuit, 0.9) }
            ),
            new TutorialStep(
                "Настройка координатной сетки",
                MeasurementRange.Range100m,
                1.379,
                0.0,
                70.0,
                new List<CableDefect> { new CableDefect(70.0, DefectType.OpenCircuit, 0.9) }
            ),
            new TutorialStep(
                "Библиотека кабелей и выбор КУ",
                MeasurementRange.Range100m,
                1.379,
                0.0,
                70.0,
                new List<CableDefect> { new CableDefect(70.0, DefectType.OpenCircuit, 0.9) }
            ),
            new TutorialStep(
                "Локализация обрыва жилы",
                MeasurementRange.Range100m,
                1.379,
                0.0,
                65.0,
                new List<CableDefect> { new CableDefect(65.0, DefectType.OpenCircuit, 0.9) }
            ),
            new TutorialStep(
                "Локализация короткого замыкания (КЗ)",
                MeasurementRange.Range100m,
                1.379,
                0.0,
                42.5,
                new List<CableDefect> { new CableDefect(42.5, DefectType.ShortCircuit, 0.95) }
            )
        };

        _keysDoc = new Dictionary<string, KeyDocumentation>
        {
            ["OUTPUT_IMPEDANCE"] = new KeyDocumentation(
                "ВЫХОД СОПР (Регулировка выходного сопротивления)",
                "Плавная подстройка выходного сопротивления генератора импульсов от 20 Ом (MIN) до 500 Ом (MAX).",
                "Служит для согласования выходного тракта рефлектометра с волновым сопротивлением исследуемой линии. Точное согласование подавляет ложные повторные отражения от начала кабеля и позволяет различить дефекты вблизи прибора."
            ),
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

        _badgeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2.0)
        };
        _badgeTimer.Tick += (s, e) =>
        {
            _badgeTimer.Stop();
            _transientBadge = null;
            RenderScreen();
        };

        LcdImage.Source = _lcdBuffer.Bitmap;
        StepsListBox.SelectedIndex = 0;
        SetCrocodilesClamped(false, animate: false);
        ApplyStep(0);
    }

    private void InitializeLeadGeometries()
    {
        var redGeom = new PathGeometry();
        _leadRedFigure = new PathFigure { StartPoint = new Point(445, 170) };
        _leadRedBezier = new BezierSegment(new Point(420, 170), new Point(405, 195), new Point(400, 205), true);
        _leadRedFigure.Segments.Add(_leadRedBezier);
        redGeom.Figures.Add(_leadRedFigure);
        CrocodileLeadRed.Data = redGeom;

        var blackGeom = new PathGeometry();
        _leadBlackFigure = new PathFigure { StartPoint = new Point(445, 240) };
        _leadBlackBezier = new BezierSegment(new Point(420, 240), new Point(405, 215), new Point(400, 205), true);
        _leadBlackFigure.Segments.Add(_leadBlackBezier);
        blackGeom.Figures.Add(_leadBlackFigure);
        CrocodileLeadBlack.Data = blackGeom;
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

    private void ShowBadge(string text)
    {
        _transientBadge = text;
        _badgeTimer?.Stop();
        _badgeTimer?.Start();
    }

    private string GetStepDescription(int stepIndex, int port)
    {
        var portName = port == 2 ? "ВХОД 2" : "ВХОД 1";

        return stepIndex switch
        {
            0 => "РЕЙС-105М1 — цифровой импульсный рефлектометр для обнаружения повреждений и неоднородностей в силовых кабелях и линиях связи. На начальном этапе линия обязательно отключается с обеих сторон, а жилы разряжаются. Зажимы 'крокодил' пока отведены и отключены от жил кабеля.",
            1 => "Зажимы 'крокодил' плавно подключены к измеряемой паре жил: красный зажим надежно обжимает сигнальную жилу, черный зажим — вторую жилу пары. Теперь зондирующий импульс рефлектометра сможет распространяться по исследуемой линии.",
            2 => port == 0 
                ? "Присоединительный кабель отключен от рефлектометра. Выберите ВХОД 1 или ВХОД 2 в блоке подключения, чтобы направить сигнал в соответствующий канал."
                : port == 2
                    ? "Присоединительный кабель BNC-разъемом подключен к гнезду ВХОД 2 на верхнем торце прибора. Измерения проводятся через тракт ВХОД 2 с той же точностью, что и через ВХОД 1."
                    : "Присоединительный кабель подключен к разъему ВХОД 1 на верхнем торце прибора. Сигнал от жил кабеля поступает в первый измерительный тракт рефлектометра.",
            3 => $"При нажатии кнопки питания рефлектометр проводит автоматическую калибровку внутренних генераторов. На ЖК-дисплее 128x64 формируется координатная сетка, линия нуля и отраженная волна от тракта {portName}.",
            4 => $"Координатная сетка дисплея для канала {portName} служит для отсчета амплитуд и масштаба. Кнопками панели прибора можно детально исследовать графики неоднородностей линии.",
            5 => $"Для точного вычисления расстояния необходимо установить коэффициент укорочения (КУ). В энергонезависимой памяти прибора заложено до 64 типов кабелей (РК-50, РК-75, ФКБ, телефонные и силовые кабели). Скорость волны зависит от диэлектрика (V = c / γ).",
            6 => $"При обрыве жилы отраженный импульс имеет ту же полярность, что и зондирующий (направлен вверх). Рефлектометр по каналу {portName} регистрирует импульс обрыва на дистанции L = 65.0 м. Курсор 0 совмещается с началом импульса, курсор 1 — с фронтом отражения.",
            7 => $"При коротком замыкании (КЗ) между жилами отраженный импульс меняет полярность на противоположную (направлен строго вниз). На канале {portName} курсор 1 наводится на точку перегиба импульса КЗ, и оператор получает точную дистанцию L = 42.5 м до аварийного участка.",
            _ => string.Empty
        };
    }

    private void SetCrocodilesClamped(bool isClamped, bool animate = true)
    {
        _isCrocodilesClamped = isClamped;

        double targetRedX = isClamped ? 250.0 : 330.0;
        double targetBlackX = isClamped ? 250.0 : 330.0;

        Point targetRedStart = isClamped ? new Point(365, 170) : new Point(445, 170);
        Point targetRedP1 = isClamped ? new Point(385, 170) : new Point(420, 170);
        Point targetRedP2 = isClamped ? new Point(395, 195) : new Point(405, 195);

        Point targetBlackStart = isClamped ? new Point(365, 240) : new Point(445, 240);
        Point targetBlackP1 = isClamped ? new Point(385, 240) : new Point(420, 240);
        Point targetBlackP2 = isClamped ? new Point(395, 215) : new Point(405, 215);

        TxtInfoClips.Text = isClamped ? "Подключены (Жилы 1-2)" : "Отключены";
        TxtInfoClips.Foreground = isClamped 
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")) 
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));

        if (animate)
        {
            var duration = TimeSpan.FromMilliseconds(420);
            var ease = new QuadraticEase { EasingMode = EasingMode.EaseInOut };

            var animRedX = new DoubleAnimation(targetRedX, duration) { EasingFunction = ease };
            var animBlackX = new DoubleAnimation(targetBlackX, duration) { EasingFunction = ease };

            var animRedStart = new PointAnimation(targetRedStart, duration) { EasingFunction = ease };
            var animRedP1 = new PointAnimation(targetRedP1, duration) { EasingFunction = ease };
            var animRedP2 = new PointAnimation(targetRedP2, duration) { EasingFunction = ease };

            var animBlackStart = new PointAnimation(targetBlackStart, duration) { EasingFunction = ease };
            var animBlackP1 = new PointAnimation(targetBlackP1, duration) { EasingFunction = ease };
            var animBlackP2 = new PointAnimation(targetBlackP2, duration) { EasingFunction = ease };

            CrocodileRed.BeginAnimation(Canvas.LeftProperty, animRedX);
            CrocodileBlack.BeginAnimation(Canvas.LeftProperty, animBlackX);

            _leadRedFigure.BeginAnimation(PathFigure.StartPointProperty, animRedStart);
            _leadRedBezier.BeginAnimation(BezierSegment.Point1Property, animRedP1);
            _leadRedBezier.BeginAnimation(BezierSegment.Point2Property, animRedP2);

            _leadBlackFigure.BeginAnimation(PathFigure.StartPointProperty, animBlackStart);
            _leadBlackBezier.BeginAnimation(BezierSegment.Point1Property, animBlackP1);
            _leadBlackBezier.BeginAnimation(BezierSegment.Point2Property, animBlackP2);
        }
        else
        {
            CrocodileRed.SetValue(Canvas.LeftProperty, targetRedX);
            CrocodileBlack.SetValue(Canvas.LeftProperty, targetBlackX);

            _leadRedFigure.StartPoint = targetRedStart;
            _leadRedBezier.Point1 = targetRedP1;
            _leadRedBezier.Point2 = targetRedP2;

            _leadBlackFigure.StartPoint = targetBlackStart;
            _leadBlackBezier.Point1 = targetBlackP1;
            _leadBlackBezier.Point2 = targetBlackP2;
        }

        RenderScreen();
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

        SetCrocodilesClamped(index > 0, animate: true);

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

        if (!_state.IsPoweredOn)
        {
            _lcdBuffer.ClearDark();
            _lcdBuffer.RenderToBitmap();
            return;
        }

        switch (_lcdBuffer.ScreenMode)
        {
            case LcdScreenMode.MainMenu:
                _lcdBuffer.DrawMainMenuScreen(_menuIndex);
                break;
            case LcdScreenMode.Memory:
                _lcdBuffer.DrawMemoryScreen(_memoryIndex);
                break;
            case LcdScreenMode.PowerConfirm:
                _lcdBuffer.DrawPowerConfirmScreen();
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

                string portHeader = _selectedPort == 2 ? "ВХОД 2" : "ВХОД 1";
                _lcdBuffer.DrawTraceHeader(portHeader, _state.ShorteningFactor);

                double activeCurPos = _activeCursor == 0 ? _state.Cursor1Position : _state.Cursor2Position;
                _lcdBuffer.DrawTraceFooter(_state.MeasuredDistance, _activeCursor, activeCurPos, _transientBadge);

                if (_selectedPort > 0 && _isCrocodilesClamped && _currentStepIndex > 0)
                {
                    var trace = _engine.GenerateTrace(_state, _currentDefects, samplesCount: 128);
                    double baseScale = 22.0 + (_state.GainDb - 20) * 0.9;
                    if (_isTurbo) baseScale *= 1.7;

                    _lcdBuffer.DrawTrace(trace, baselineY: 38 + _baselineShift, scale: baseScale);

                    var maxDist = ReflectometryEngine.GetMaxDistanceMeters(_state.Range);
                    var cursor1Pixel = (int)(_state.Cursor1Position / maxDist * LcdDisplayBuffer.ScreenWidth);
                    var cursor2Pixel = (int)(_state.Cursor2Position / maxDist * LcdDisplayBuffer.ScreenWidth);

                    _lcdBuffer.DrawCursor(cursor1Pixel, isDotted: false);
                    _lcdBuffer.DrawCursor(cursor2Pixel, isDotted: true);
                }
                else
                {
                    _lcdBuffer.DrawHLine(0, LcdDisplayBuffer.ScreenWidth - 1, 38 + _baselineShift, true);
                }
                break;
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
        var p1X = port == 2 ? 470.0 : 455.0;

        var animCableP3 = new PointAnimation(new Point(endX, targetY), duration) { EasingFunction = ease };
        var animCableP2 = new PointAnimation(new Point(p2X, targetY - 66), duration) { EasingFunction = ease };
        var animCableP1 = new PointAnimation(new Point(p1X, 200), duration) { EasingFunction = ease };

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
        if (sender is not FrameworkElement element || element.Tag is not string keyTag)
        {
            return;
        }

        if (_keysDoc.TryGetValue(keyTag, out var doc))
        {
            TxtInspectorKeyTitle.Text = doc.Title;
            TxtInspectorSummary.Text = doc.Summary;
            TxtInspectorDetail.Text = doc.Details;

            var flashAnim = new ColorAnimation
            {
                From = (Color)ColorConverter.ConvertFromString("#38BDF8"),
                To = (Color)ColorConverter.ConvertFromString("#0284C7"),
                Duration = TimeSpan.FromMilliseconds(250),
                AutoReverse = true
            };
            InspectorCard.BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, flashAnim);
        }

        if (_lcdBuffer.ScreenMode == LcdScreenMode.PowerConfirm)
        {
            if (keyTag == "POWER")
            {
                _state.IsPoweredOn = false;
                _lcdBuffer.ScreenMode = LcdScreenMode.Trace;
            }
            else
            {
                _lcdBuffer.ScreenMode = LcdScreenMode.Trace;
            }
            RenderScreen();
            return;
        }

        if (!_state.IsPoweredOn)
        {
            if (keyTag == "POWER")
            {
                _state.IsPoweredOn = true;
                _lcdBuffer.ScreenMode = LcdScreenMode.Trace;
                ShowBadge("РЕЙС-105М1");
                RenderScreen();
            }
            return;
        }

        switch (keyTag)
        {
            case "OUTPUT_IMPEDANCE":
                _outputImpedanceIndex = (_outputImpedanceIndex + 1) % ImpedanceValues.Length;
                int currentR = ImpedanceValues[_outputImpedanceIndex];
                string rText = currentR == 20 ? "MIN (20 Ом)" : (currentR == 500 ? "MAX (500 Ом)" : $"{currentR} Ом");
                ShowBadge($"R={rText}");
                break;

            case "POWER":
                _lcdBuffer.ScreenMode = LcdScreenMode.PowerConfirm;
                break;

            case "BACKLIGHT":
                _lcdBuffer.CycleBacklight();
                ShowBadge($"ЯРК.{_lcdBuffer.BacklightLevel + 1}/3");
                break;

            case "CURSOR_TOGGLE":
                _activeCursor = 1 - _activeCursor;
                _lcdBuffer.ActiveCursorIndex = _activeCursor;
                ShowBadge(_activeCursor == 0 ? "КУРСОР 0" : "КУРСОР 1");
                break;

            case "CURSOR_LEFT":
                if (_activeCursor == 0)
                {
                    _state.Cursor1Position = Math.Max(0.0, _state.Cursor1Position - 1.0);
                }
                else
                {
                    _state.Cursor2Position = Math.Max(_state.Cursor1Position, _state.Cursor2Position - 1.0);
                }
                TxtInfoDistance.Text = $"{_state.MeasuredDistance:F2} м";
                _transientBadge = null;
                break;

            case "CURSOR_RIGHT":
                var maxDist = ReflectometryEngine.GetMaxDistanceMeters(_state.Range);
                if (_activeCursor == 0)
                {
                    _state.Cursor1Position = Math.Min(_state.Cursor2Position, _state.Cursor1Position + 1.0);
                }
                else
                {
                    _state.Cursor2Position = Math.Min(maxDist, _state.Cursor2Position + 1.0);
                }
                TxtInfoDistance.Text = $"{_state.MeasuredDistance:F2} м";
                _transientBadge = null;
                break;

            case "GAIN_PLUS":
                _state.GainDb = Math.Min(60, _state.GainDb + 6);
                ShowBadge($"УС +{_state.GainDb}dB");
                break;

            case "GAIN_MINUS":
                _state.GainDb = Math.Max(0, _state.GainDb - 6);
                ShowBadge($"УС +{_state.GainDb}dB");
                break;

            case "RANGE_PLUS":
                int curRange = (int)_state.Range;
                if (curRange < 11)
                {
                    _state.Range = (MeasurementRange)(curRange + 1);
                }
                TxtInfoRange.Text = $"{ReflectometryEngine.GetMaxDistanceMeters(_state.Range):F1} м";
                ShowBadge($"D={ReflectometryEngine.GetMaxDistanceMeters(_state.Range):F0}м");
                break;

            case "RANGE_MINUS":
                int curRangeMin = (int)_state.Range;
                if (curRangeMin > 0)
                {
                    _state.Range = (MeasurementRange)(curRangeMin - 1);
                }
                TxtInfoRange.Text = $"{ReflectometryEngine.GetMaxDistanceMeters(_state.Range):F1} м";
                ShowBadge($"D={ReflectometryEngine.GetMaxDistanceMeters(_state.Range):F0}м");
                break;

            case "ZOOM_PLUS":
                _zoomFactor = Math.Min(8, _zoomFactor * 2);
                ShowBadge($"КУ х{_zoomFactor}");
                break;

            case "ZOOM_MINUS":
                _zoomFactor = Math.Max(1, _zoomFactor / 2);
                ShowBadge($"КУ х{_zoomFactor}");
                break;

            case "SHORTENING_PLUS":
                _state.ShorteningFactor = Math.Round(_state.ShorteningFactor + 0.001, 3);
                TxtInfoShortening.Text = $"{_state.ShorteningFactor:F3}";
                ShowBadge($"γ={_state.ShorteningFactor:F3}");
                break;

            case "SHORTENING_MINUS":
                _state.ShorteningFactor = Math.Round(Math.Max(1.000, _state.ShorteningFactor - 0.001), 3);
                TxtInfoShortening.Text = $"{_state.ShorteningFactor:F3}";
                ShowBadge($"γ={_state.ShorteningFactor:F3}");
                break;

            case "MENU_OK":
                if (_lcdBuffer.ScreenMode == LcdScreenMode.Trace)
                {
                    _lcdBuffer.ScreenMode = LcdScreenMode.MainMenu;
                }
                else if (_lcdBuffer.ScreenMode == LcdScreenMode.MainMenu)
                {
                    if (_menuIndex == 1) _lcdBuffer.ScreenMode = LcdScreenMode.Battery;
                    else if (_menuIndex == 2) _lcdBuffer.ScreenMode = LcdScreenMode.CableTable;
                    else if (_menuIndex == 7) _lcdBuffer.ScreenMode = LcdScreenMode.Parameters;
                    else _lcdBuffer.ScreenMode = LcdScreenMode.Trace;
                }
                else
                {
                    _lcdBuffer.ScreenMode = LcdScreenMode.Trace;
                }
                break;

            case "MEMORY":
                _lcdBuffer.ScreenMode = _lcdBuffer.ScreenMode == LcdScreenMode.Memory 
                    ? LcdScreenMode.Trace 
                    : LcdScreenMode.Memory;
                break;

            case "TURBO":
                _isTurbo = !_isTurbo;
                ShowBadge(_isTurbo ? "ТУРБО ВКЛ" : "ТУРБО ВЫКЛ");
                break;

            case "PULSE_PLUS":
                _pulseWidthNs = Math.Min(5000, _pulseWidthNs * 2);
                ShowBadge($"ИМП {_pulseWidthNs}нс");
                break;

            case "PULSE_MINUS":
                _pulseWidthNs = Math.Max(10, _pulseWidthNs / 2);
                ShowBadge($"ИМП {_pulseWidthNs}нс");
                break;

            case "COMP_PLUS":
                ShowBadge("КОМП +");
                break;

            case "COMP_MINUS":
                ShowBadge("КОМП -");
                break;

            case "NAV_UP":
                if (_lcdBuffer.ScreenMode == LcdScreenMode.MainMenu)
                {
                    _menuIndex = Math.Max(0, _menuIndex - 1);
                }
                else if (_lcdBuffer.ScreenMode == LcdScreenMode.Memory)
                {
                    _memoryIndex = Math.Max(0, _memoryIndex - 1);
                }
                else
                {
                    _baselineShift = Math.Min(10, _baselineShift + 2);
                }
                break;

            case "NAV_DOWN":
                if (_lcdBuffer.ScreenMode == LcdScreenMode.MainMenu)
                {
                    _menuIndex = Math.Min(9, _menuIndex + 1);
                }
                else if (_lcdBuffer.ScreenMode == LcdScreenMode.Memory)
                {
                    _memoryIndex = Math.Min(5, _memoryIndex + 1);
                }
                else
                {
                    _baselineShift = Math.Max(-10, _baselineShift - 2);
                }
                break;
        }

        RenderScreen();
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