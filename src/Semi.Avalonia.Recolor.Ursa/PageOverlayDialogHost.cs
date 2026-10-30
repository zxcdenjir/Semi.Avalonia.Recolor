using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Ursa.Controls;

namespace Semi.Avalonia.Recolor.Ursa;

/// <summary>Прикрепляет хост диалогов Ursa к внешней странице Avalonia.</summary>
public sealed class PageOverlayDialogHost
{
    private PageOverlayDialogHost() { }

    public const string PART_DialogHost = "PART_DialogHost";
    public const int DefaultDialogHostZIndex = 1000;

    /// <summary>Включает или отключает автоматическое создание хоста для страницы.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<PageOverlayDialogHost, Page, bool>("IsEnabled");

    /// <summary>
    /// Размещает хост поверх всего <c>TopLevel</c>; при значении <see langword="false"/>
    /// хост ограничен областью страницы. Свойство Ursa <c>IsTopLevel</c>, отвечающее за перетаскивание окна,
    /// этим параметром не изменяется.
    /// </summary>
    public static readonly AttachedProperty<bool> UseTopLevelOverlayProperty =
        AvaloniaProperty.RegisterAttached<PageOverlayDialogHost, Page, bool>("UseTopLevelOverlay", defaultValue: true);

    /// <summary>Задаёт порядок отрисовки хоста относительно других элементов overlay.</summary>
    public static readonly AttachedProperty<int> DialogHostZIndexProperty =
        AvaloniaProperty.RegisterAttached<PageOverlayDialogHost, Page, int>("DialogHostZIndex", DefaultDialogHostZIndex);

    /// <summary>Задаёт идентификатор хоста для маршрутизации диалогов Ursa.</summary>
    public static readonly AttachedProperty<string?> HostIdProperty =
        AvaloniaProperty.RegisterAttached<PageOverlayDialogHost, Page, string?>("HostId");

    /// <summary>Задаёт кисть затемнения модального overlay.</summary>
    public static readonly AttachedProperty<IBrush?> OverlayMaskBrushProperty =
        AvaloniaProperty.RegisterAttached<PageOverlayDialogHost, Page, IBrush?>("OverlayMaskBrush");

    /// <summary>Задаёт безопасные отступы для размещения диалогов и drawer.</summary>
    public static readonly AttachedProperty<Thickness> SafePaddingProperty =
        AvaloniaProperty.RegisterAttached<PageOverlayDialogHost, Page, Thickness>("SafePadding");

    private static readonly ConditionalWeakTable<Page, HostAttachment> Attachments = new();

    static PageOverlayDialogHost()
    {
        IsEnabledProperty.Changed.AddClassHandler<Page>((page, change) => SetEnabled(page, (bool)change.NewValue!));
        UseTopLevelOverlayProperty.Changed.AddClassHandler<Page>((page, _) => Refresh(page));
        HostIdProperty.Changed.AddClassHandler<Page>((page, _) => Refresh(page));
        DialogHostZIndexProperty.Changed.AddClassHandler<Page>((page, _) => UpdateZIndex(page));
        OverlayMaskBrushProperty.Changed.AddClassHandler<Page>((page, _) => UpdateHostProperties(page));
        SafePaddingProperty.Changed.AddClassHandler<Page>((page, _) => UpdateHostProperties(page));
    }

    /// <summary>Возвращает состояние автоматического создания хоста.</summary>
    public static bool GetIsEnabled(Page page) => page.GetValue(IsEnabledProperty);
    /// <summary>Задаёт состояние автоматического создания хоста.</summary>
    public static void SetIsEnabled(Page page, bool value) => page.SetValue(IsEnabledProperty, value);
    /// <summary>Возвращает режим размещения хоста поверх всего <c>TopLevel</c>.</summary>
    public static bool GetUseTopLevelOverlay(Page page) => page.GetValue(UseTopLevelOverlayProperty);
    /// <summary>Задаёт режим размещения хоста поверх всего <c>TopLevel</c>.</summary>
    public static void SetUseTopLevelOverlay(Page page, bool value) => page.SetValue(UseTopLevelOverlayProperty, value);
    /// <summary>Возвращает Z-порядок хоста.</summary>
    public static int GetDialogHostZIndex(Page page) => page.GetValue(DialogHostZIndexProperty);
    /// <summary>Задаёт Z-порядок хоста.</summary>
    public static void SetDialogHostZIndex(Page page, int value) => page.SetValue(DialogHostZIndexProperty, value);
    /// <summary>Возвращает идентификатор хоста Ursa.</summary>
    public static string? GetHostId(Page page) => page.GetValue(HostIdProperty);
    /// <summary>Задаёт идентификатор хоста Ursa.</summary>
    public static void SetHostId(Page page, string? value) => page.SetValue(HostIdProperty, value);
    /// <summary>Возвращает кисть модального overlay.</summary>
    public static IBrush? GetOverlayMaskBrush(Page page) => page.GetValue(OverlayMaskBrushProperty);
    /// <summary>Задаёт кисть модального overlay.</summary>
    public static void SetOverlayMaskBrush(Page page, IBrush? value) => page.SetValue(OverlayMaskBrushProperty, value);
    /// <summary>Возвращает безопасные отступы хоста.</summary>
    public static Thickness GetSafePadding(Page page) => page.GetValue(SafePaddingProperty);
    /// <summary>Задаёт безопасные отступы хоста.</summary>
    public static void SetSafePadding(Page page, Thickness value) => page.SetValue(SafePaddingProperty, value);

    /// <summary>Возвращает созданный для страницы хост или найденный хост UrsaWindow/UrsaView.</summary>
    public static OverlayDialogHost? GetDialogHost(Page page) =>
        Attachments.TryGetValue(page, out var attachment) ? attachment.Host : null;

    private static void SetEnabled(Page page, bool enabled)
    {
        if (enabled)
        {
            if (!Attachments.TryGetValue(page, out _))
            {
                var attachment = new HostAttachment(page);
                Attachments.Add(page, attachment);
                attachment.Start();
            }
        }
        else if (Attachments.TryGetValue(page, out var attachment))
        {
            attachment.Dispose();
            Attachments.Remove(page);
        }
    }

    private static void Refresh(Page page)
    {
        if (Attachments.TryGetValue(page, out var attachment))
            attachment.Refresh();
    }

    private static void UpdateZIndex(Page page)
    {
        if (Attachments.TryGetValue(page, out var attachment))
            attachment.UpdateZIndex();
    }

    private static void UpdateHostProperties(Page page)
    {
        if (Attachments.TryGetValue(page, out var attachment))
            attachment.UpdateProperties();
    }

    private sealed class HostAttachment : IDisposable
    {
        private readonly Page _page;
        private Panel? _visualParent;
        private OverlayDialogHost? _host;
        private bool _ownsHost;
        private bool _disposed;

        public OverlayDialogHost? Host => _host;

        public HostAttachment(Page page)
        {
            _page = page;
        }

        public void Start()
        {
            _page.Loaded += OnLoaded;
            _page.Unloaded += OnUnloaded;
            _page.PropertyChanged += OnPagePropertyChanged;
            if (_page.IsLoaded)
                Attach();
        }

        public void Refresh()
        {
            DetachOwnedHost();
            if (_page.IsLoaded)
                Attach();
        }

        public void UpdateZIndex()
        {
            if (_ownsHost && _host is not null)
                _host.ZIndex = GetDialogHostZIndex(_page);
        }

        public void UpdateProperties()
        {
            if (!_ownsHost || _host is null)
                return;

            _host.OverlayMaskBrush = GetOverlayMaskBrush(_page);
            _host.SafePadding = GetSafePadding(_page);
        }

        private void OnLoaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) => Attach();

        private void OnUnloaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) => DetachOwnedHost();

        private void OnPagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TemplatedControl.TemplateProperty)
            {
                Dispatcher.UIThread.Post(Refresh, DispatcherPriority.Loaded);
            }
        }

        private void Attach()
        {
            if (_disposed || _host is not null || !_page.IsLoaded)
                return;

            // One default host per TopLevel is enough. Pages in a NavigationPage share its host;
            // an explicit HostId opts a page into its own independently addressed host.
            if (GetHostId(_page) is null && _page.GetVisualAncestors().OfType<Page>().Any())
                return;

            var topLevel = TopLevel.GetTopLevel(_page);
            if (topLevel is null)
                return;

            var requestedId = GetHostId(_page);
            var existingHost = topLevel.GetVisualDescendants().OfType<OverlayDialogHost>()
                .FirstOrDefault(host => host.HostId == requestedId);
            if (existingHost is not null)
            {
                _host = existingHost;
                _ownsHost = false;
                return;
            }

            var isTopLevel = GetUseTopLevelOverlay(_page);
            var parent = isTopLevel
                ? GetTopLevelOverlay(_page, topLevel)
                : _page.GetVisualChildren().OfType<Panel>().FirstOrDefault();
            if (parent is null)
            {
#if DEBUG
                Debug.WriteLine($"{_page.GetType().Name}: template root has no Panel; Ursa OverlayDialogHost was not created.");
#endif
                return;
            }

            _host = new OverlayDialogHost
            {
                Name = PART_DialogHost,
                HostId = requestedId,
                // Ursa's IsTopLevel enables Window dragging; it is unrelated to where this
                // page-owned host is placed, and must stay false on windowless platforms.
                IsTopLevel = false,
                OverlayMaskBrush = GetOverlayMaskBrush(_page),
                SafePadding = GetSafePadding(_page),
            };
            _visualParent = parent;
            _ownsHost = true;
            parent.Children.Add(_host);
            _host.ZIndex = GetDialogHostZIndex(_page);
        }

        private static Panel? GetTopLevelOverlay(Page page, TopLevel topLevel)
        {
            var layer = OverlayLayer.GetOverlayLayer(page);
            if (layer is not null)
                return layer;

            var manager = topLevel.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
            if (manager is not null)
            {
                manager.EnableOverlayLayer = true;
                layer = OverlayLayer.GetOverlayLayer(page);
                if (layer is not null)
                    return layer;
            }

#if DEBUG
            Debug.WriteLine("Avalonia OverlayLayer is unavailable; the page-level Ursa OverlayDialogHost was not created.");
#endif
            return null;
        }

        private void DetachOwnedHost()
        {
            if (_ownsHost && _host is not null)
            {
                _visualParent?.Children.Remove(_host);
            }

            _host = null;
            _visualParent = null;
            _ownsHost = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _page.Loaded -= OnLoaded;
            _page.Unloaded -= OnUnloaded;
            _page.PropertyChanged -= OnPagePropertyChanged;
            DetachOwnedHost();
        }
    }
}
