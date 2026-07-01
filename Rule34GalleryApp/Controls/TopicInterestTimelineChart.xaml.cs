using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Rule34Gallery.Core.Services;

namespace Rule34GalleryApp.Controls;

public partial class TopicInterestTimelineChart : UserControl
{
    private static readonly Color[] SeriesColors =
    [
        Color.FromRgb(0xE8, 0x5D, 0x5D),
        Color.FromRgb(0x5D, 0x9C, 0xE8),
        Color.FromRgb(0x7D, 0xE8, 0x6A),
        Color.FromRgb(0xE8, 0xC5, 0x5D),
        Color.FromRgb(0xC5, 0x7D, 0xE8),
        Color.FromRgb(0x5D, 0xE8, 0xD0),
    ];

    private ForYouInterestTimelineResult _timeline = ForYouInterestTimelineResult.Empty;

    public TopicInterestTimelineChart()
    {
        InitializeComponent();
    }

    public void Bind(ForYouInterestTimelineResult timeline)
    {
        _timeline = timeline;
        Render();
    }

    private void ChartHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
        => Render();

    private void Render()
    {
        ChartCanvas.Children.Clear();
        LegendPanel.Children.Clear();

        if (!_timeline.HasData)
        {
            EmptyText.Visibility = Visibility.Visible;
            LegendPanel.Visibility = Visibility.Collapsed;
            ChartCanvas.Visibility = Visibility.Collapsed;
            TimeStartLabel.Text = string.Empty;
            TimeEndLabel.Text = string.Empty;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;
        LegendPanel.Visibility = Visibility.Visible;
        ChartCanvas.Visibility = Visibility.Visible;

        var timestamps = _timeline.SampleTimestampsUnix;
        TimeStartLabel.Text = FormatTimestamp(timestamps[0]);
        TimeEndLabel.Text = FormatTimestamp(timestamps[^1]);

        var width = Math.Max(1, ChartCanvas.ActualWidth);
        var height = Math.Max(1, ChartCanvas.ActualHeight);
        var maxScore = Math.Max(10, _timeline.MaxScore);
        var pointCount = _timeline.Series[0].Points.Count;
        if (pointCount < 2)
        {
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        DrawGrid(width, height, maxScore);

        foreach (var series in _timeline.Series)
        {
            var color = SeriesColors[series.ColorIndex % SeriesColors.Length];
            LegendPanel.Children.Add(CreateLegendItem(series, color));
            DrawSeries(series, color, width, height, maxScore, pointCount);
        }
    }

    private static UIElement CreateLegendItem(ForYouInterestTimelineSeries series, Color color)
    {
        var trend = series.IsRising ? " ⬆" : series.IsDeclining ? " ⬇" : string.Empty;
        var label = series.Label.Length > 14 ? series.Label[..11] + "…" : series.Label;
        return new TextBlock
        {
            Text = label + trend,
            Foreground = new SolidColorBrush(color),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6),
            TextTrimming = TextTrimming.CharacterEllipsis,
            ToolTip = series.Label,
        };
    }

    private void DrawGrid(double width, double height, double maxScore)
    {
        for (var i = 0; i <= 4; i++)
        {
            var y = height - (height * i / 4.0);
            ChartCanvas.Children.Add(new Line
            {
                X1 = 0,
                X2 = width,
                Y1 = y,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                StrokeThickness = 1,
            });
        }

        var zeroLabel = new TextBlock
        {
            Text = "0",
            Foreground = (Brush)FindResource("MutedBrush"),
            FontSize = 9,
        };
        Canvas.SetLeft(zeroLabel, 0);
        Canvas.SetTop(zeroLabel, height - 12);
        ChartCanvas.Children.Add(zeroLabel);

        var maxLabel = new TextBlock
        {
            Text = $"{maxScore:0}",
            Foreground = (Brush)FindResource("MutedBrush"),
            FontSize = 9,
        };
        Canvas.SetLeft(maxLabel, 0);
        Canvas.SetTop(maxLabel, 0);
        ChartCanvas.Children.Add(maxLabel);
    }

    private void DrawSeries(
        ForYouInterestTimelineSeries series,
        Color color,
        double width,
        double height,
        double maxScore,
        int pointCount)
    {
        var brush = new SolidColorBrush(color);
        var points = new PointCollection();
        for (var i = 0; i < pointCount; i++)
        {
            var x = pointCount == 1 ? 0 : width * i / (pointCount - 1.0);
            var score = Math.Clamp(series.Points[i], 0, maxScore);
            var y = height - (score / maxScore * height);
            points.Add(new Point(x, y));
        }

        ChartCanvas.Children.Add(new Polyline
        {
            Points = points,
            Stroke = brush,
            StrokeThickness = 2.2,
            StrokeLineJoin = PenLineJoin.Round,
            SnapsToDevicePixels = true,
        });
    }

    private static string FormatTimestamp(long unixSeconds)
    {
        if (unixSeconds <= 0)
        {
            return string.Empty;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime().ToString("MMM d");
    }
}
