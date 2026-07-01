using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Rule34GalleryApp.Services;

/// <summary>
/// Builds a dark-theme FlowDocument for changelog.md (headings, bullets, bold, inline code).
/// </summary>
public static class ChangelogDocumentBuilder
{
    private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(0xB8, 0xB8, 0xB8));
    private static readonly Brush AccentBrush = new SolidColorBrush(Color.FromRgb(0xA0, 0xE0, 0x60));
    private static readonly Brush CodeBackground = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26));

    public static FlowDocument Build(string markdown)
    {
        var doc = new FlowDocument
        {
            Background = Brushes.Transparent,
            Foreground = TextBrush,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13,
            PagePadding = new Thickness(0),
        };

        foreach (var line in Normalize(markdown).Split('\n'))
        {
            var trimmed = line.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 4, 0, 4) });
                continue;
            }

            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                var title = trimmed[3..].Trim();
                var section = new Paragraph(new Run(title))
                {
                    Margin = new Thickness(0, 14, 0, 6),
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = AccentBrush,
                };
                doc.Blocks.Add(section);
                continue;
            }

            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                var body = trimmed[2..];
                var bullet = new Paragraph { Margin = new Thickness(14, 2, 0, 2) };
                bullet.Inlines.Add(new Run("•  ") { Foreground = MutedBrush });
                AppendInlineRuns(bullet.Inlines, body);
                doc.Blocks.Add(bullet);
                continue;
            }

            var fallback = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
            AppendInlineRuns(fallback.Inlines, trimmed);
            doc.Blocks.Add(fallback);
        }

        return doc;
    }

    private static string Normalize(string markdown)
    {
        var trimmed = markdown.Trim();
        return Regex.Replace(trimmed, @"^\s*#\s*Changelog\s*\r?\n+", "", RegexOptions.IgnoreCase);
    }

    private static void AppendInlineRuns(InlineCollection inlines, string text)
    {
        var pattern = new Regex(@"\*\*(.+?)\*\*|(`[^`]+`)", RegexOptions.Singleline);
        var index = 0;
        foreach (Match match in pattern.Matches(text))
        {
            if (match.Index > index)
            {
                inlines.Add(new Run(text[index..match.Index]) { Foreground = TextBrush });
            }

            if (match.Value.StartsWith("**", StringComparison.Ordinal))
            {
                inlines.Add(new Run(match.Groups[1].Value)
                {
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TextBrush,
                });
            }
            else
            {
                var code = match.Value.Trim('`');
                inlines.Add(new Run(code)
                {
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = AccentBrush,
                    Background = CodeBackground,
                });
            }

            index = match.Index + match.Length;
        }

        if (index < text.Length)
        {
            inlines.Add(new Run(text[index..]) { Foreground = TextBrush });
        }
    }
}
