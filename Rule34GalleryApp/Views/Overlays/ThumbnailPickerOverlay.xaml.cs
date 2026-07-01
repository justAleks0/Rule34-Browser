using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Overlays;

public partial class ThumbnailPickerOverlay : UserControl
{
    private readonly AppServices _app = AppServices.Current;
    private string _filePath = string.Empty;
    private bool _suppressSlider;
    private bool _isLoadingPreview;

    public ThumbnailPickerOverlay()
    {
        InitializeComponent();
    }

    public void Open(PostItem post)
    {
        _filePath = post.FileUrl;
        if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
        {
            _filePath = post.PlaybackUrl;
        }

        if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
        {
            _app.Messenger.Show("Thumbnail", "File not found on disk.", AppMessageKind.Warning);
            return;
        }

        FileNameText.Text = Path.GetFileName(_filePath);
        var fraction = ThumbnailSeekStore.GetFraction(_filePath);
        _suppressSlider = true;
        PositionSlider.Value = fraction * 100;
        UpdatePositionLabel();
        _suppressSlider = false;

        Visibility = Visibility.Visible;
        _ = LoadPreviewAsync();
    }

    public void Close() => Visibility = Visibility.Collapsed;

    private double CurrentFraction => PositionSlider.Value / 100d;

    private void UpdatePositionLabel()
        => PositionLabel.Text = $"{PositionSlider.Value:0}%";

    private async Task LoadPreviewAsync()
    {
        if (_isLoadingPreview || string.IsNullOrWhiteSpace(_filePath))
        {
            return;
        }

        _isLoadingPreview = true;
        PositionLabel.Text = "Loading preview…";
        try
        {
            var frame = await LocalVideoThumbnailService.CapturePreviewAsync(
                _filePath,
                CurrentFraction,
                640);

            if (frame is null)
            {
                PreviewImage.Source = null;
                PositionLabel.Text = "Could not read frame";
                return;
            }

            var preview = new BitmapImage();
            preview.BeginInit();
            preview.CacheOption = BitmapCacheOption.OnLoad;
            preview.DecodePixelWidth = 480;
            preview.StreamSource = Encode(frame);
            preview.EndInit();
            if (preview.CanFreeze)
            {
                preview.Freeze();
            }

            PreviewImage.Source = preview;
            UpdatePositionLabel();
        }
        catch (Exception ex)
        {
            PositionLabel.Text = "Preview failed";
            _app.Messenger.Show("Thumbnail preview", ex.Message, AppMessageKind.Warning);
        }
        finally
        {
            _isLoadingPreview = false;
        }
    }

    private static MemoryStream Encode(BitmapSource source)
    {
        var encoder = new JpegBitmapEncoder { QualityLevel = 80 };
        encoder.Frames.Add(BitmapFrame.Create(source));
        var memory = new MemoryStream();
        encoder.Save(memory);
        memory.Position = 0;
        return memory;
    }

    private void PositionSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressSlider)
        {
            return;
        }

        UpdatePositionLabel();
    }

    private void PreviewButton_OnClick(object sender, RoutedEventArgs e)
        => _ = LoadPreviewAsync();

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            LocalVideoThumbnailService.SaveThumbnailAt(_filePath, CurrentFraction);
            _app.Messenger.Show("Thumbnail saved", "Gallery will refresh this card.", AppMessageKind.Info);
            Close();
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Thumbnail save failed", ex.Message, AppMessageKind.Warning);
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();
}
