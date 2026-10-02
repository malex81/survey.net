# Встраивание браузера в Avalonia: CEF + CefGlue

> Проект: `ImageProcessing` · Модуль: `SurveyCef`
> Пакет: `CefGlue.Avalonia` **120.6099.215** (Chromium 120.0.6099.215) · Avalonia **11.3.\*** · `net8.0` · `x64`
> Статус: **рабочая конфигурация** — оконный режим рендеринга (`WindowlessRenderingEnabled = false`).

---

## TL;DR

| Вопрос | Ответ |
|---|---|
| Что показывает web-страницы? | `AvaloniaCefBrowser` из `Xilium.CefGlue.Avalonia`, размещённый в `ContentControl` |
| Где инициализируется CEF? | `Program.cs` → `BuildAvaloniaApp().AfterSetup(_ => InitializeCef())` |
| Через что инициализировать? | **Только** `CefRuntimeLoader.Initialize(...)`, никогда не `CefRuntime.Initialize` напрямую |
| Какой режим рендеринга? | **Оконный** (`WindowlessRenderingEnabled = false`) — рекомендован самим CefGlue |
| Почему не OSR? | В OSR битмап создаётся, но кадры CEF в него не попадают → поверхность остаётся пустой (см. §6) |
| Главное ограничение | **Airspace**: нативный HWND браузера всегда перекрывает Avalonia-контент (см. §5) |
| Команда сборки | `dotnet build ImageProcessing.sln -c Debug -p:Platform=x64` |

---

## Содержание

1. [Стек и терминология](#1-стек-и-терминология)
2. [Архитектура интеграции](#2-архитектура-интеграции)
3. [Инициализация CEF](#3-инициализация-cef)
4. [Два режима рендеринга](#4-два-режима-рендеринга)
5. [Ограничение оконного режима: airspace](#5-ограничение-оконного-режима-airspace)
6. [Разбор инцидента: пустой экран в OSR](#6-разбор-инцидента-пустой-экран-в-osr)
7. [Сборка и запуск](#7-сборка-и-запуск)
8. [Чек-лист диагностики](#8-чек-лист-диагностики)
9. [Если понадобится OSR](#9-если-понадобится-osr)
10. [Источники](#10-источники)

---

## 1. Стек и терминология

### CEF (Chromium Embedded Framework)

Нативная библиотека, упаковывающая движок Chromium (Blink + V8) так, чтобы его можно было
встроить в desktop-приложение как обычный native-компонент.

Отличие от `WebView2`, который обычно есть «под рукой» в .NET:

| | **WebView2** | **CEF** |
|---|---|---|
| Движок | системный Edge (Evergreen) | **собственный** Chromium из NuGet |
| Требования к машине | установленный Runtime | ничего, всё в дистрибутиве |
| Версия | зависит от того, что стоит у пользователя | зафиксирована пакетом (`120.6099.215`) |
| Вес | ~0 | **+250 МБ** к дистрибутиву |
| Контроль | ограничен | полный: свои URL-схемы, перехват запросов, OSR |

CEF — это не одна DLL. Рядом с `libcef.dll` (в нашем выводе — **214 793 728 байт**) обязательно
должны лежать ресурсы:

```
bin\x64\Debug\net8.0\
├── libcef.dll                    214 793 728
├── icudtl.dat                     10 717 392   (Unicode/ICU)
├── resources.pak                   8 187 390
├── chrome_100_percent.pak            691 420   (ресурсы для масштаба 100%)
├── chrome_200_percent.pak          1 053 763   (ресурсы для масштаба 200%)
├── snapshot_blob.bin                 285 366
├── v8_context_snapshot.bin           653 869
├── locales\                        <-- ПУСТАЯ папка, см. §3.3
├── runtimes\win-x64\native\locales\  55 .pak-файлов (ru.pak, en-US.pak, ...)
├── CefGlueBrowserProcess\
│   └── Xilium.CefGlue.BrowserProcess.exe   <-- subprocess-хост CEF
├── DawnCache\  GPUCache\           <-- создаются CEF в рантайме
└── Xilium.CefGlue*.dll
```

### CefGlue — три слоя

| Сборка | Роль |
|---|---|
| `Xilium.CefGlue.dll` | тонкий P/Invoke над C API CEF: `CefRuntime`, `CefBrowserHost`, `CefRenderHandler` |
| `Xilium.CefGlue.Common.dll` | платформенно-независимая логика: `BaseCefBrowser`, `CefRuntimeLoader`, `OffScreenRenderSurface` |
| `Xilium.CefGlue.Common.Shared.dll` | общие контракты, разделяемые с subprocess |
| `Xilium.CefGlue.Avalonia.dll` | Avalonia-контрол: `AvaloniaCefBrowser`, `AvaloniaControl`, `AvaloniaRenderSurface` |

### Многопроцессность

CEF работает как Chrome: наше приложение — это **browser process**, а рендеринг, GPU, сеть
и утилиты живут в **дочерних процессах** `Xilium.CefGlue.BrowserProcess.exe`.

При запущенном приложении с одним браузером наблюдалось **7 процессов** (1 + 6).

Два практических следствия:

- subprocess-экзешник обязан лежать в `CefGlueBrowserProcess\`, иначе `CefRuntimeLoader`
  бросает `FileNotFoundException`;
- завершать/убивать надо **все** процессы, иначе кэш CEF остаётся залоченным и следующий
  старт падает.

---

## 2. Архитектура интеграции

### Файлы модуля

| Файл | Назначение |
|---|---|
| `Program.cs` | точка входа, `BuildAvaloniaApp()`, **инициализация CEF** |
| `SurveyCef\ComponentRegistry.cs` | регистрация модуля как `ISurveyComponent` в DI |
| `SurveyCef\Views\WebViewer.axaml` | разметка: тулбар + контейнер браузера + статус |
| `SurveyCef\Views\WebViewer.axaml.cs` | создание `AvaloniaCefBrowser`, навигация, события загрузки |

### Поток запуска

```
Program.Main
  └─ BuildAvaloniaApp().StartWithClassicDesktopLifetime()
       └─ AppBuilder.AfterSetup(_ => InitializeCef())      // CefRuntimeLoader.Initialize
            └─ App / DI (AppServicesConfig.Surveys)
                 └─ ComponentRegistry (Code="Chromium", Title="Survey CIF")
                      └─ WebViewer (UserControl)
                           └─ new AvaloniaCefBrowser()     // ЗДЕСЬ реально стартует CefRuntime
                                └─ BrowserContainer.Content = _browser
```

Ключевой момент: `CefRuntimeLoader.Initialize` **не инициализирует** CEF. Он лишь запоминает
делегат; настоящий `CefRuntime.Initialize` выполняется **лениво** — при создании первого
`AvaloniaCefBrowser`. Поэтому ошибка инициализации проявляется не в `InitializeCef()`,
а заметно позже, в конструкторе `WebViewer`.

### Размещение в главном окне

`MainWindow.axaml`: `DockPanel` → `ListBox` (`Width=240`, `DockPanel.Dock="Left"`) +
`ContentControl Content="{Binding SelectedItem.View}"`. Окно `WindowState="Maximized"`,
при максимизации `Padding=8`.

Итоговые bounds браузера на экране 1920×1080: `240,52 → 1680×961`
(240 — ширина списка, 52 — высота тулбара `WebViewer` + отступы).

### Событийная модель `WebViewer`

```csharp
_browser = new AvaloniaCefBrowser();
_browser.BrowserInitialized += () => SetStatus("CEF browser initialized");
_browser.LoadStart += OnLoadStart;   // e.Frame.IsMain, e.Frame.Url
_browser.LoadEnd   += OnLoadEnd;     // e.Frame.IsMain, e.HttpStatusCode
_browser.LoadError += OnLoadError;   // e.FailedUrl, e.ErrorText, e.ErrorCode
_browser.Address   = AboutBlank;
BrowserContainer.Content = _browser;
```

Фильтр `if (!e.Frame.IsMain) return;` обязателен — иначе статус будет перезаписываться
событиями от iframe'ов. В `OnLoadError` дополнительно отбрасывается `CefErrorCode.Aborted`:
это штатная отмена предыдущей навигации, а не ошибка.

---

## 3. Инициализация CEF

### 3.1 Только через `CefRuntimeLoader`

```csharp
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace()
        .AfterSetup(_ => InitializeCef());
```

Прямой вызов `CefRuntime.Load` / `ExecuteProcess` / `Initialize` **запрещён**.
`CefRuntimeLoader.Initialize` делает четыре вещи, которые иначе придётся повторять вручную:

1. находит subprocess-экзешник и подставляет его в `BrowserSubprocessPath`;
2. выбирает режим message loop под платформу — на Windows выставляет
   `settings.MultiThreadedMessageLoop = true`;
3. регистрирует `CefRuntime.Shutdown` на выход из процесса (см. §3.4);
4. запоминает `IsOSREnabled = settings.WindowlessRenderingEnabled` — именно этот статический
   флаг потом читает `AvaloniaCefBrowser`, чтобы выбрать адаптер (см. §4).

Порядок поиска subprocess (`GetSubProcessPaths`) — сначала относительно
`AppContext.BaseDirectory`, затем относительно каталога самой сборки (сценарий плагинов):

```
<appdir>\CefGlueBrowserProcess\Xilium.CefGlue.BrowserProcess.exe
<appdir>\Xilium.CefGlue.BrowserProcess.exe
<dir of Xilium.CefGlue.Common.dll>\CefGlueBrowserProcess\Xilium.CefGlue.BrowserProcess.exe
<dir of Xilium.CefGlue.Common.dll>\Xilium.CefGlue.BrowserProcess.exe
```

Если ни один не найден — `FileNotFoundException` со списком всех проверенных путей.

### 3.2 Текущая конфигурация (`Program.cs`)

```csharp
CefRuntimeLoader.Initialize(new CefSettings
{
    RootCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ImageProcessing", "cef"),

    LocalesDirPath = ResolveLocalesDir(AppContext.BaseDirectory),
    Locale = "ru",

    WindowlessRenderingEnabled = false
});
```

- `RootCachePath` уводит кэш/cookies из папки программы в `%LocalAppData%` — иначе при
  установке в `Program Files` CEF не сможет писать и упадёт.
- `AfterSetup` выбран потому, что к этому моменту Avalonia уже настроена, но UI ещё не создан.
  Инициализировать CEF до `AppMain` нельзя.

### 3.3 `locales` — подводный камень

NuGet-пакет раскладывает `.pak`-локали по RID-папкам:

```
bin\x64\Debug\net8.0\runtimes\win-x64\native\locales\   <-- 55 файлов, здесь ru.pak и en-US.pak
bin\x64\Debug\net8.0\locales\                           <-- существует, но ПУСТА (0 файлов)
```

CEF по умолчанию ищет `<app dir>\locales`. Папка там есть, но пустая — поэтому CEF падает
или теряет строки локализации. Каталог приходится указывать явно:

```csharp
static string ResolveLocalesDir(string basePath)
{
    string[] candidates =
    [
        Path.Combine(basePath, "locales"),
        Path.Combine(basePath, "runtimes", "win-x64", "native", "locales"),
        Path.Combine(basePath, "runtimes", "win-arm64", "native", "locales")
    ];

    return candidates.FirstOrDefault(d => File.Exists(Path.Combine(d, "en-US.pak"))) ?? candidates[0];
}
```

Проверка именно по наличию `en-US.pak` (а не по `Directory.Exists`) — принципиальна:
пустая папка `locales\` иначе была бы выбрана первой.

### 3.4 Завершение работы: кто за что отвечает

Специального кода для завершения процессов в приложении **нет**, и он не нужен.
Штатное завершение обеспечивают два независимых механизма.

**1. Закрытие браузера — через существующую цепочку `Dispose` приложения:**

```
desktop.Exit                (App.axaml.cs, ShutdownMode.OnMainWindowClose)
  -> App.AppExit            -> kernel?.Dispose()
    -> EngineKernel.Dispose -> services.Dispose()              (ServiceProvider)
      -> ComponentRegistry.Dispose()                           (singleton, IDisposable)
        -> WebViewer.Dispose()   -> _browser.Dispose()
          -> BaseCefBrowser.Dispose -> _adapter.Dispose(true)  (CefGlue)
```

`ComponentRegistry` зарегистрирован как `AddSingleton<ISurveyComponent, ComponentRegistry>`
и действительно создаётся контейнером: все `ISurveyComponent` разрешаются через
`IEnumerable<ISurveyComponent>` в конструкторе `MainWindowModel`. Поэтому
`ServiceProvider.Dispose()` его освобождает. Это штатный паттерн проекта, а не
изобретение модуля `SurveyCef` — так же устроен `SurveyDragDrop\ComponentRegistry`.

**2. Выключение самого CEF — внутри `CefRuntimeLoader`:**

```csharp
AppDomain.CurrentDomain.ProcessExit += delegate
{
    CefRuntime.Shutdown();
};
```

Хук регистрируется один раз при `Initialize`. Вызывать `CefRuntime.Shutdown()` вручную
из приложения **не нужно**: дублирующий shutdown вреден.

Порядок получается корректным: сначала закрывается браузер (`desktop.Exit`), затем
процесс завершается и срабатывает `ProcessExit` -> `CefShutdown`, который и останавливает
дочерние процессы CEF.

**Что не покрыто:**

- `desktop.Exit` и `ProcessExit` срабатывают только при **штатном** выходе. При крахе или
  принудительном завершении (`Stop-Process`, Task Manager) не срабатывает ни то, ни другое:
  дочерние процессы могут остаться живыми, а кэш — залоченным.
- Самозавершение дочерних процессов CEF после гибели родительского процесса (мониторинг
  parent handle) в рамках этой работы **не проверялось** и как гарантия не рассматривается.

Поэтому пункт §8 про ручную проверку процессов — это **гигиена отладки**, а не требование
к рантайму приложения.

---

## 4. Два режима рендеринга

Флаг `WindowlessRenderingEnabled` — единственный, который определяет, **как картинка попадёт
на экран**. В `CefRuntimeLoader` он просто запоминается:

```csharp
IsOSREnabled = settings.WindowlessRenderingEnabled;
```

а `AvaloniaCefBrowser` по нему выбирает две принципиально разные реализации:

```csharp
internal override Common.Platform.IControl CreateControl()
    => new AvaloniaControl(this, VisualChildren);              // оконный режим

internal override IOffScreenControlHost CreateOffScreenControlHost()
    => new AvaloniaOffScreenControlHost(this, VisualChildren); // OSR
```

### 4.1 Сравнение

| | **Оконный** (`false`) — наш выбор | **Windowless / OSR** (`true`) |
|---|---|---|
| Как рисуется | CEF создаёт настоящее дочернее окно ОС (HWND) и рисует сам | Chromium растеризует в память, кадры отдаются через `CefRenderHandler.OnPaint` |
| Как встраивается | Avalonia `NativeControlHost` | `Image` + `WriteableBitmap` внутри `Viewbox` |
| Что видно в UI Automation | `Pane 'Chrome Legacy Window'` | ничего, обычный Avalonia-visual |
| Производительность | нативная | ниже (2 лишних копии кадра через memory-mapped file) |
| Ввод (мышь/клавиатура/IME/drag&drop) | отдаёт ОС | **хост обязан транслировать сам** |
| DPI / resize | отдаёт ОС | хост обязан сообщать `WasResized`, `ScreenInfoChanged` |
| Прозрачность, оверлеи, `RenderTransform`, скругления | невозможно (airspace) | полноценная композиция |
| Браузер внутри `ScrollViewer` | невозможно | возможно |
| Надёжность в CefGlue.Avalonia 120.6099.215 | работает | **не отдаёт кадры** (см. §6) |

Официальное демо CefGlue для Avalonia использует именно оконный режим:

```csharp
#if WINDOWLESS
    // its recommended to leave this off (false), since its less performant and can cause more issues
    WindowlessRenderingEnabled = true
#else
    WindowlessRenderingEnabled = false
#endif
```

OSR в CefGlue включается отдельной конфигурацией сборки, а не по умолчанию.

### 4.2 Оконный режим — как это устроено

CEF создаёт HWND, CefGlue оборачивает его и отдаёт Avalonia как `NativeControlHost`:

```csharp
public void InitializeRender(IntPtr browserHandle)
{
    _browserView = new HostWindow(browserHandle);          // Windows
    Dispatcher.UIThread.Post(() =>
        SetContent(new ExtendedAvaloniaNativeControlHost(_browserView.Handle)));
}
```

Размер передаётся обратно в CEF на **каждый** проход layout:

```csharp
private void OnLayoutUpdated(object sender, EventArgs e)
    => SizeChanged?.Invoke(new CefSize((int)_control.Bounds.Width, (int)_control.Bounds.Height));
```

Любопытная деталь, объясняющая «лишнее» окно в списке окон процесса: на Windows CefGlue
заранее создаёт **скрытое долгоживущее окно-родитель**, чтобы не падать при закрытии хоста:

```csharp
// on Windows all browsers will be hosted in the same long-lived window to prevent crashes
// during browser window creation, which could occur if the hosting window was closed
_hostWindowPlatformHandle = new Window().TryGetPlatformHandle();
```

Поэтому `EnumWindows` показывает два top-level окна процесса: `Обработка изображений`
и невидимое `Window`. Это нормально, а не утечка.

### 4.3 OSR — как это устроено

Цепочка доставки кадра (`OffScreenRenderSurface.Render`):

```csharp
public Task Render(IntPtr buffer, int width, int height, CefRectangle[] dirtyRects)
{
    // (1) кадр МОЛЧА выбрасывается, если размер не совпал
    if (width != ScaledWidth || height != ScaledHeight)
        return Task.CompletedTask;

    lock (_renderLock)
    {
        ...
        _mappedFile = MemoryMappedFile.CreateNew(null, byteCount, MemoryMappedFileAccess.ReadWrite);
        _viewAccessor = _mappedFile.CreateViewAccessor();
        var imageBuffer = _viewAccessor.SafeMemoryMappedViewHandle;

        // (2) копия кадра из CEF в свой буфер — это поток CEF, НЕ UI-поток Avalonia
        Buffer.MemoryCopy(buffer.ToPointer(), imageBuffer.DangerousGetHandle().ToPointer(),
                          imageBuffer.ByteLength, byteCount);

        return ExecuteInUIThread(() =>            // (3) маршалинг в UI-поток
        {
            if (width != ScaledWidth || height != ScaledHeight) return;   // (4) ещё проверка
            if (imageBuffer.IsInvalid || imageBuffer.IsClosed) return;    // (5) и ещё

            if (RenderedWidth != width || RenderedHeight != height)
                CreateBitmap(width, height);

            InnerRender(...);   // -> BeginBitmapUpdate() -> UpdateBitmap() -> endUpdate()
        });
    }
}
```

где `ScaledWidth => (int)Math.Ceiling(DeviceScaleFactor * _width)`, а `_width` приходит
из `Resize(width, height)`.

Avalonia-часть (`AvaloniaRenderSurface`):

```csharp
protected override void CreateBitmap(int width, int height)
{
    // TODO handle transparency
    _bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(DefaultDpi, DefaultDpi),
                                  PixelFormat.Bgra8888, AlphaFormat.Opaque);
    Image.Source = _bitmap;
}

protected override Action BeginBitmapUpdate()
{
    var lockedBuffer = _bitmap.Lock();
    _destinationBuffer = lockedBuffer.Address;
    return () => { lockedBuffer.Dispose(); Image.InvalidateVisual(); };
}
```

Хост (`AvaloniaOffScreenControlHost`) дополнительно транслирует в CEF мышь, клавиатуру,
`TextInput`, drag&drop, смену курсора, тултипы, видимость окна и `ScreenInfoChanged`.
Иерархия visual'ов: `AvaloniaCefBrowser` → `Viewbox(Stretch=Fill)` → `Image(Stretch=None)`.

Заметные следы незавершённости Avalonia-порта:

- `// TODO handle transparency` при жёстко зашитом `AlphaFormat.Opaque`;
- `AllowsTransparency => false`;
- `// TODO avalonia: get value from OS` для `MouseWheelDelta`;
- `// TODO BUG: sometimes the tooltips are left hanging`.

---

## 5. Ограничение оконного режима: airspace

Нативный HWND рисуется **поверх** всего Avalonia-контента, независимо от `ZIndex`,
порядка в дереве и `Opacity`. Отсюда правила вёрстки.

**Нельзя:**

- наложить на браузер полупрозрачную панель, оверлей, водяной знак, spinner;
- показать поверх браузера `Popup`, `ToolTip`, `ContextMenu`, выпадающее меню;
- применить к браузеру `RenderTransform`, скруглить углы через `Clip`/`CornerRadius`;
- анимировать позицию/размер браузера;
- положить браузер внутрь `ScrollViewer`, `Viewbox` или любого трансформирующего контейнера;
- сделать браузер полупрозрачным.

**Можно и нужно:**

- держать браузер в **отдельной ячейке layout**, не пересекающейся с другим контентом;
- менять его размер/позицию шагом, без анимации;
- скрывать целиком через `IsVisible` (CEF получит `WasHidden`).

В проекте это соблюдено: `WebViewer.axaml` выносит браузер в собственную строку `Grid`.

```xml
<Grid RowDefinitions="Auto, *, Auto">
    <StackPanel Grid.Row="0" ...> <!-- тулбар: кнопки + AddressBox --> </StackPanel>

    <!-- Браузер вынесен в отдельную строку Grid: при WindowlessRenderingEnabled=false CEF
         рисует в собственном дочернем HWND, который всегда перекрывает Avalonia-контент. -->
    <ContentControl Grid.Row="1" x:Name="BrowserContainer" Background="#323"/>

    <TextBlock Grid.Row="2" x:Name="StatusText" .../>
</Grid>
```

Отдельно: `ShowDeveloperTools()` в оконном режиме открывает **отдельное top-level окно**,
докинг внутри Avalonia невозможен.

---

## 6. Разбор инцидента: пустой экран в OSR

Историческая справка: изначально был выбран OSR-режим (`WindowlessRenderingEnabled = true`),
поскольку он «правильнее» для Avalonia — браузер становится обычным visual'ом в дереве,
нет airspace. Это решение и стоило основного времени.

### 6.1 Симптом

Экран пустой. При этом **одновременно** правдой было:

- нет ни одного exception: `debug.log` пустой, `unhandled_main.txt` не создан;
- `IsBrowserInitialized == true`, события `LoadStart`/`LoadEnd` приходят;
- `https://www.google.com/` загружается с кодом **200**;
- `ExecuteJavaScript` работает, `document.write` внедряет HTML;
- контрол attached, `IsEffectivelyVisible == true`, `Bounds = 1680×961` — layout корректный;
- `WriteableBitmap` **создан** правильного размера, `Image.Source` **присвоен**;
- содержимое битмапа — пустое.

То есть «браузерная» часть работала полностью. Не работало ровно одно звено в середине
цепочки рендеринга — и оно **не сообщает об ошибке никак**.

### 6.2 Метод диагностики

Отличить «Avalonia не рисует контрол» от «CEF не отдаёт кадры» стандартными средствами
нельзя: классы `AvaloniaRenderSurface` и `OffScreenRenderSurface` — `internal`, а логгер
CefGlue по умолчанию ничего не пишет.

Сработал временный диагностический код (после использования удалён из проекта):
обход visual tree с дампом `Bounds`/`IsVisible` каждого предка **плюс посемпельное чтение
пикселей прямо из `WriteableBitmap`**:

```csharp
// Временный диагностический код. В проект не входит, удаляется после использования.
// Требует AllowUnsafeBlocks=true (в csproj уже включён) и вызова только из UI-потока.
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

static void DumpSurface(Visual root, int depth = 0)
{
    string indent = new(' ', depth * 2);
    string line = $"{indent}{root.GetType().Name} bounds={root.Bounds} visible={root.IsVisible}";

    if (root is Avalonia.Controls.Image img && img.Source is WriteableBitmap bmp)
    {
        line += $" bitmap={bmp.PixelSize} {bmp.Format}";

        using (var fb = bmp.Lock())                 // только из UI-потока!
        {
            unsafe
            {
                byte* p = (byte*)fb.Address;
                int off = (bmp.PixelSize.Height / 2) * fb.RowBytes
                        + (bmp.PixelSize.Width / 2) * 4;
                line += $" center(B,G,R,A)={p[off]},{p[off + 1]},{p[off + 2]},{p[off + 3]}";
            }
        }
    }

    Console.WriteLine(line);
    foreach (Visual child in root.GetVisualChildren()) DumpSurface(child, depth + 1);
}
```

Именно этот приём дал решающий факт: битмап существует и имеет корректный `PixelSize`,
но пиксели в нём не меняются.

Дополнительно применялись: скриншоты окна, `EnumWindows`/`GetWindowRect` по процессу
(в оконном режиме так находится `Chrome Legacy Window` и проверяются его bounds)
и UI Automation-обход дерева.

### 6.3 Что доказано

1. `WriteableBitmap` создавался нужного размера → значит, `Render()` дошёл до UI-потока,
   проверка `width == ScaledWidth` прошла и `CreateBitmap` выполнился.
2. `CreateBitmap` и `InnerRender` вызываются **в одном и том же** UI-thread-действии,
   поэтому `BeginBitmapUpdate`/`UpdateBitmap` не могли «не выполниться» после создания битмапа.
3. Содержимое буфера при этом оставалось пустым.

**Вывод:** копирование выполнялось, но копировать было нечего — CEF отдавал пустой кадр,
либо копия из memory-mapped file терялась.

### 6.4 Гипотезы (не доказаны, по убыванию правдоподобия)

| # | Гипотеза | Основание |
|---|---|---|
| 1 | CEF отдаёт **прозрачный** кадр (альфа = 0), а Avalonia-сторона жёстко работает с `AlphaFormat.Opaque` | `// TODO handle transparency`, `AllowsTransparency => false`; для `about:blank` прозрачная страница даёт ровно «пустой» буфер |
| 2 | Рассинхрон `DeviceScaleFactor`/размера: первый кадр прошёл, остальные отброшены проверкой (1) или (4) | `ScaledWidth = ceil(DeviceScaleFactor * _width)`; `SizeChanged` стреляет на каждый `LayoutUpdated`. На масштабе 100% слабее, на дробном DPI выстрелит первой |
| 3 | Дефект самой сборки `120.6099.215` в связке с Avalonia 11.3 | пакет рассчитывался на более раннюю Avalonia; Avalonia-порт содержит незакрытые TODO |

### 6.5 Почему решили не чинить OSR

Все три гипотезы проверяются **одним** способом: собрать CefGlue из исходников локально
и добавить логирование в `internal`-классы `OffScreenRenderSurface` /
`AvaloniaRenderSurface` / `CommonOffscreenBrowserAdapter`. Это уже не «настроить приложение»,
а «чинить стороннюю библиотеку».

Одновременно официальное демо CefGlue прямо рекомендует оконный режим
(см. цитату в §4.1). Поэтому был переключён один флаг — и получился работающий результат
вместо продолжения реверс-инжиниринга.

### 6.6 Уроки

- **Не начинать с OSR.** Для Avalonia + CefGlue дефолт — `WindowlessRenderingEnabled = false`.
- **`LoadEnd` + статус 200 ≠ картинка на экране.** Загрузка страницы и доставка пикселей —
  две независимые подсистемы.
- **Глобальный `try/catch` мешает диагностике.** `Main` обёрнут в
  `try/catch → SysLog.TryLog(ex, "unhandled_main.txt")`, плюс
  `TaskScheduler.UnobservedTaskException`. Для продакшена это правильно, но отказ выглядит
  как «просто ничего не происходит». На время отладки стоит дублировать исключение в консоль.
- **«Тихие» отказы надо искать посемпельно.** Если нет логов — читайте пиксели, bounds
  и native-хендлы напрямую.

---

## 7. Сборка и запуск

Проект объявлен только под x64: `<Platforms>x64</Platforms>` в `ImageProcessing.csproj`.

```powershell
cd d:\dev.Net\survey.net.git\ImageProcessing
dotnet build ImageProcessing.sln -c Debug -p:Platform=x64
```

Результат: `Build succeeded`, `0 Warning(s)`, `0 Error(s)`,
выход → `bin\x64\Debug\net8.0\ImageProcessing.dll`.

Запуск:

```powershell
.\bin\x64\Debug\net8.0\ImageProcessing.exe
```

Публикация — штатным скриптом (`publish-win.bat`):

```powershell
dotnet publish ImageProcessing.csproj -r win-x64 --self-contained true -o .\output\ImageProcessing_win\bin
```

> В PowerShell 5.1 разделитель команд — `;`, а не `&&`.

### 7.1 Ловушка: две параллельные папки сборки

Если собрать **без** `-p:Platform=x64`, MSBuild положит результат в `bin\Debug\net8.0\`,
а не в `bin\x64\Debug\net8.0\`. Обе папки существуют одновременно и выглядят одинаково
правдоподобно. Зафиксированный реальный пример расхождения:

```
bin\Debug\net8.0\ImageProcessing.dll        02.10.2026 20:30:33   <-- устаревший
bin\x64\Debug\net8.0\ImageProcessing.dll    02.10.2026 20:47:42   <-- актуальный
```

Симптом: «я всё поправил, а поведение не изменилось». Лечение — всегда указывать
`-p:Platform=x64` и запускать именно из `bin\x64\...`. Устаревшую `bin\Debug\` лучше удалить.

Раскладку нативных ассетов CEF (`libcef.dll`, `.pak`, `locales`, `CefGlueBrowserProcess\`)
выполняет MSBuild-target `ResolveCEFAssets` из пакета — в логе сборки он виден явно.

### 7.2 Где лежат логи

`config\appsettings.json`:

```json
"Serilog": { "PathFormat": "../logs/log-{Date}.txt", ... }
```

Путь относительный к рабочей папке `net8.0`, поэтому логи попадают в `bin\x64\Debug\logs\`.
Туда же `SysLog.TryLog` пишет аварийные файлы:

| Файл | Источник |
|---|---|
| `cef_init.txt` | `catch` вокруг `CefRuntimeLoader.Initialize` |
| `unhandled_main.txt` | `catch` вокруг `StartWithClassicDesktopLifetime` |
| `unobserved_task.txt` | `TaskScheduler.UnobservedTaskException` |

**Важно:** `SysLog.TryLog` молча выходит, если `App.CurrentKernel == null` или если в
конфигурации нет `Serilog:PathFormat`. То есть отказы на самых ранних этапах запуска
могут не записаться **никуда**. При диагностике случая «приложение просто не запускается»
проверяйте не только папку логов, но и консоль/Event Viewer.

---

## 8. Чек-лист диагностики

### 8.1 «Браузер не виден» — порядок проверки

1. **Собрано ли под x64?** Сравнить `LastWriteTime` DLL в `bin\x64\Debug\net8.0\`
   с моментом правки (§7.1).
2. **Есть ли логи?** `bin\x64\Debug\logs\` → `cef_init.txt`, `unhandled_main.txt`,
   `unobserved_task.txt`, `log-*.txt`.
3. **Инициализирован ли браузер?** `IsBrowserInitialized`, событие `BrowserInitialized`,
   статус-строка в UI (`WebViewer` выводит её в `StatusText`).
4. **Живы ли subprocess'ы?**
   `Get-Process ImageProcessing, Xilium.CefGlue.BrowserProcess`
   (ожидаемо несколько процессов; если их нет — CEF не стартовал).
5. **Есть ли native-окно?** В оконном режиме — через `EnumWindows`/UI Automation ищем
   `Chrome Legacy Window` внутри процесса и сверяем его `GetWindowRect` с `Bounds` контрола.
   Если HWND есть и bounds совпадают, но картинки нет — проблема не в layout.
6. **Корректен ли layout?** Обход visual tree: `Bounds`, `IsVisible`, `IsEffectivelyVisible`
   для браузера и всех предков. Нулевой размер у любого предка = пустой экран.
7. **Для OSR** — семплировать пиксели `WriteableBitmap` (§6.2).

### 8.2 Прочие известные шероховатости

- **`data:` URL заблокирован.** Chromium запрещает top-level-навигацию на `data:`.
  Свой HTML внедряется в уже загруженный `about:blank` через JS:

  ```csharp
  void WriteHtml(string html)
  {
      string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(html));
      _browser.ExecuteJavaScript(
          "document.open();"
          + $"document.write(new TextDecoder().decode(Uint8Array.from(atob(\"{base64}\"), c => c.charCodeAt(0))));"
          + "document.close();");
  }
  ```

  Base64 + `TextDecoder` выбраны, чтобы не экранировать кириллицу и кавычки в JS-строке.
  Вызывать только из UI-потока (`Dispatcher.UIThread.Post`).

- **`LoadError` с `CefErrorCode.Aborted` — не ошибка.** Это штатная отмена предыдущей
  навигации; её нужно отфильтровывать, иначе статус мигает ложными ошибками.

- **События приходят не из UI-потока.** Обновление UI — только через
  `Dispatcher.UIThread.CheckAccess()` / `Post(...)` (см. `SetStatus`).

- **Кэш CEF остаётся залоченным** при аварийном завершении. Перед повторным запуском
  убедитесь, что убиты все `Xilium.CefGlue.BrowserProcess.exe`.

- **Рантарайм-папки `DawnCache\` и `GPUCache\`** создаются CEF рядом с экзешником. Это
  нормально; в git их не коммитим (`[Bb]in/` в `.gitignore`).

- **`Dispose`.** `WebViewer` реализует `IDisposable` и освобождает `_browser`;
  `ComponentRegistry.Dispose` вызывает его по цепочке
  `desktop.Exit -> kernel.Dispose -> ServiceProvider.Dispose` (см. §3.4).
  Писать отдельный код завершения процессов не требуется: `CefRuntime.Shutdown`
  зарегистрирован самим `CefRuntimeLoader`. Убивать процессы вручную нужно только
  после аварийного завершения.

---

## 9. Если понадобится OSR

OSR оправдан только если действительно нужны возможности, запрещённые airspace (§5):
оверлеи поверх страницы, прозрачность, трансформации, браузер внутри `ScrollViewer`,
скриншоты страницы средствами Avalonia.

План действий (оценка — порядка дня работы, это правка сторонней библиотеки):

1. **Собрать CefGlue из исходников** (`https://github.com/OutSystems/CefGlue`, ветка `main`)
   и подключить сборки проектами вместо NuGet-пакета, чтобы иметь доступ к `internal`-коду.
2. **Инструментировать OSR-путь кадра.** Логировать в `OffScreenRenderSurface.Render`:
   - входящие `width`/`height` против `ScaledWidth`/`ScaledHeight` — на какой из трёх
     проверок (1), (4), (5) кадр умирает;
   - значение `DeviceScaleFactor` и момент вызова `Resize`;
   - первые 16 байт `buffer` от CEF (нулевые = CEF отдаёт пустой кадр);
   - количество вызовов `CreateBitmap` и `UpdateBitmap`.
3. **Проверить гипотезу №1 из §6.4** — задать opaque `BackgroundColor` для браузера/CEF
   и посмотреть, появятся ли пиксели.
4. **Проверить гипотезу №2** — выставить масштаб ОС 125%/150% и сравнить поведение
   с 100%.
5. **Рассмотреть свежую сборку CefGlue** (более новая версия CEF и/или Avalonia-порта,
   где закрыты TODO по transparency).
6. **Как fallback** — не патчить CefGlue, а заменить поверхность: получить
   `CefRenderHandler.OnPaint` самостоятельно и рисовать в свой `WriteableBitmap`,
   минуя `AvaloniaRenderSurface`.

Пока ни один пункт не выполнен, **рабочим дефолтом остаётся оконный режим**.

---

## 10. Источники

| Что | Где |
|---|---|
| Исходники CefGlue | `https://github.com/OutSystems/CefGlue` (ветка `main`) |
| `CefRuntimeLoader` | `CefGlue.Common/CefRuntimeLoader.cs` |
| `BaseCefBrowser` | `CefGlue.Common/BaseCefBrowser.cs` |
| `AvaloniaCefBrowser` | `CefGlue.Avalonia/AvaloniaCefBrowser.cs` |
| Оконный хост | `CefGlue.Avalonia/Platform/AvaloniaControl.cs` |
| Обёртка HWND | `CefGlue.Avalonia/Platform/Windows/HostWindow.cs` |
| OSR-хост | `CefGlue.Avalonia/Platform/AvaloniaOffScreenControlHost.cs` |
| OSR-поверхность | `CefGlue.Common/Helpers/OffScreenRenderSurface.cs` |
| Avalonia-поверхность (битмап) | `CefGlue.Avalonia/AvaloniaRenderSurface.cs` |
| Демо Avalonia (рекомендация по режиму) | `CefGlue.Demo.Avalonia/Program.cs` |
| Документация CEF | `https://bitbucket.org/chromiumembedded/cef` |
| Пакет | `CefGlue.Avalonia` 120.6099.215 на NuGet |

---

*Документ составлен по результатам интеграции и отладки модуля `SurveyCef`.*
