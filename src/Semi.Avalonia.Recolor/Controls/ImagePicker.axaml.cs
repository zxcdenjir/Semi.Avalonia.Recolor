using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Media;

namespace Semi.Avalonia.Recolor.Controls;

/// <summary>Область выбора изображения с настраиваемой заглушкой и кнопкой сброса.</summary>
public partial class ImagePicker : UserControl
{
    public static readonly StyledProperty<string?> ImagePathProperty =
        AvaloniaProperty.Register<ImagePicker, string?>(nameof(ImagePath), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<Bitmap?> ImageProperty =
        AvaloniaProperty.Register<ImagePicker, Bitmap?>(nameof(Image), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<Control?> PlaceholderProperty =
        AvaloniaProperty.Register<ImagePicker, Control?>(nameof(Placeholder));
    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<ImagePicker, Stretch>(nameof(Stretch), Stretch.UniformToFill);
    public static readonly StyledProperty<bool> HasImageProperty =
        AvaloniaProperty.Register<ImagePicker, bool>(nameof(HasImage));
    public static readonly StyledProperty<bool> ShowCustomPlaceholderProperty =
        AvaloniaProperty.Register<ImagePicker, bool>(nameof(ShowCustomPlaceholder));
    public static readonly StyledProperty<bool> ShowDefaultPlaceholderProperty =
        AvaloniaProperty.Register<ImagePicker, bool>(nameof(ShowDefaultPlaceholder));
    public static readonly StyledProperty<bool> IsBusyProperty =
        AvaloniaProperty.Register<ImagePicker, bool>(nameof(IsBusy));
    public static readonly StyledProperty<long> MaxImageSizeBytesProperty =
        AvaloniaProperty.Register<ImagePicker, long>(nameof(MaxImageSizeBytes), 30L * 1024 * 1024);
    public static readonly StyledProperty<int> MaxImageDimensionProperty =
        AvaloniaProperty.Register<ImagePicker, int>(nameof(MaxImageDimension), 2048);

    private static readonly FilePickerFileType SupportedImages = new("Image files (no GIF)")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"],
        MimeTypes = ["image/png", "image/jpeg", "image/webp", "image/bmp"],
        AppleUniformTypeIdentifiers = ["public.png", "public.jpeg", "org.webmproject.webp", "com.microsoft.bmp"],
    };

    private Bitmap? _ownedBitmap;
    private readonly AsyncCommand _pickCommand;

    /// <summary>Команда открытия системного диалога выбора изображения.</summary>
    public ICommand PickImageCommand => _pickCommand;
    /// <summary>Команда сброса выбранного изображения.</summary>
    public ICommand ClearImageCommand { get; }
    /// <summary>Путь или URI выбранного изображения.</summary>
    public string? ImagePath { get => GetValue(ImagePathProperty); set => SetValue(ImagePathProperty, value); }
    /// <summary>Загруженное изображение для предпросмотра.</summary>
    public Bitmap? Image { get => GetValue(ImageProperty); set => SetValue(ImageProperty, value); }
    /// <summary>Пользовательский контрол заглушки, отображаемый до выбора файла.</summary>
    public Control? Placeholder { get => GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    /// <summary>Правило масштабирования изображения в области предпросмотра.</summary>
    public Stretch Stretch { get => GetValue(StretchProperty); set => SetValue(StretchProperty, value); }
    /// <summary>Показывает, выбрано ли изображение.</summary>
    public bool HasImage { get => GetValue(HasImageProperty); private set => SetValue(HasImageProperty, value); }
    /// <summary>Показывает пользовательскую заглушку, если изображение не выбрано.</summary>
    public bool ShowCustomPlaceholder { get => GetValue(ShowCustomPlaceholderProperty); private set => SetValue(ShowCustomPlaceholderProperty, value); }
    /// <summary>Показывает стандартную заглушку, если не выбраны изображение и пользовательская заглушка.</summary>
    public bool ShowDefaultPlaceholder { get => GetValue(ShowDefaultPlaceholderProperty); private set => SetValue(ShowDefaultPlaceholderProperty, value); }
    /// <summary>Показывает, выполняется ли загрузка изображения.</summary>
    public bool IsBusy { get => GetValue(IsBusyProperty); private set => SetValue(IsBusyProperty, value); }
    /// <summary>Максимальный размер файла в байтах. По умолчанию — 30 МиБ; ноль отключает ограничение.</summary>
    public long MaxImageSizeBytes { get => GetValue(MaxImageSizeBytesProperty); set => SetValue(MaxImageSizeBytesProperty, value); }
    /// <summary>Максимальная ширина или высота изображения в пикселях. Ноль сохраняет исходный размер.</summary>
    public int MaxImageDimension { get => GetValue(MaxImageDimensionProperty); set => SetValue(MaxImageDimensionProperty, value); }

    /// <summary>Возникает, если файл не удалось прочитать или декодировать.</summary>
    public event Action<ImagePicker, Exception>? ImageLoadFailed;

    public ImagePicker()
    {
        _pickCommand = new AsyncCommand(PickImageAsync, value => IsBusy = value);
        ClearImageCommand = new DelegateCommand(ClearImage);
        InitializeComponent();
        PickerSurface.PropertyChanged += OnPickerSurfacePropertyChanged;
        UpdatePlaceholderVisibility();
        if (CornerRadius == default)
            CornerRadius = new CornerRadius(8);
        UpdateClipCornerRadius();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PlaceholderProperty)
        {
            UpdatePlaceholderVisibility();
            return;
        }

        if (change.Property != ImageProperty)
            return;

        if (_ownedBitmap is { } owned && !ReferenceEquals(owned, Image))
        {
            owned.Dispose();
            _ownedBitmap = null;
        }

        HasImage = Image is not null;
        UpdatePlaceholderVisibility();
    }

    private void UpdatePlaceholderVisibility()
    {
        var hasImage = Image is not null;
        var hasCustomPlaceholder = Placeholder is not null;
        ShowCustomPlaceholder = !hasImage && hasCustomPlaceholder;
        ShowDefaultPlaceholder = !hasImage && !hasCustomPlaceholder;
    }

    private void OnPickerSurfacePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == Border.BorderThicknessProperty || change.Property == Border.CornerRadiusProperty)
            UpdateClipCornerRadius();
    }

    private void UpdateClipCornerRadius()
    {
        var radius = PickerSurface.CornerRadius;
        var thickness = PickerSurface.BorderThickness;

        static double InnerRadius(double radius, double horizontal, double vertical) =>
            Math.Max(0, radius - Math.Max(horizontal, vertical));

        PickerClipSurface.CornerRadius = new CornerRadius(
            InnerRadius(radius.TopLeft, thickness.Left, thickness.Top),
            InnerRadius(radius.TopRight, thickness.Right, thickness.Top),
            InnerRadius(radius.BottomRight, thickness.Right, thickness.Bottom),
            InnerRadius(radius.BottomLeft, thickness.Left, thickness.Bottom));
    }

    private async Task PickImageAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        try
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите изображение",
                AllowMultiple = false,
                FileTypeFilter = [SupportedImages],
            });

            if (files.Count == 0)
                return;

            await using var source = await files[0].OpenReadAsync();
            using var memory = new MemoryStream();
            await CopyWithLimitAsync(source, memory, MaxImageSizeBytes);
            memory.Position = 0;

            Bitmap bitmap;
            if (MaxImageDimension > 0)
            {
                bitmap = Bitmap.DecodeToWidth(memory, MaxImageDimension, BitmapInterpolationMode.HighQuality);
                if (bitmap.PixelSize.Height > MaxImageDimension)
                {
                    bitmap.Dispose();
                    memory.Position = 0;
                    bitmap = Bitmap.DecodeToHeight(memory, MaxImageDimension, BitmapInterpolationMode.HighQuality);
                }
            }
            else
            {
                bitmap = new Bitmap(memory);
            }

            ImagePath = files[0].Path.ToString();
            Image = bitmap;
            _ownedBitmap = bitmap;
        }
        catch (Exception exception)
        {
            ImageLoadFailed?.Invoke(this, exception);
        }
    }

    private void ClearImage()
    {
        ImagePath = null;
        Image = null;
    }

    private static async Task CopyWithLimitAsync(Stream source, MemoryStream destination, long maximumBytes)
    {
        var buffer = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(buffer)) != 0)
        {
            if (maximumBytes > 0 && destination.Length + read > maximumBytes)
                throw new InvalidDataException($"Image exceeds the {maximumBytes} byte size limit.");
            await destination.WriteAsync(buffer.AsMemory(0, read));
        }
    }

    private sealed class AsyncCommand(Func<Task> execute, Action<bool> setBusy) : ICommand
    {
        private bool _running;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => !_running;

        public async void Execute(object? parameter)
        {
            if (_running)
                return;

            _running = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            setBusy(true);
            try
            {
                await execute();
            }
            finally
            {
                setBusy(false);
                _running = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private sealed class DelegateCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
}
