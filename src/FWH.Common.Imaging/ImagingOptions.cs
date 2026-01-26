using SkiaSharp;

namespace FWH.Common.Imaging;

public enum FitMode
{
    BestFit,
    Fill,
    Stretch
}

public enum Anchor
{
    Center,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public class ImagingOptions
{
    public FitMode FitMode { get; set; } = FitMode.BestFit;
    public Anchor Anchor { get; set; } = Anchor.TopLeft;

    /// <summary>
    /// Padding in pixels applied around the SVG when fitting.
    /// </summary>
    public int Padding { get; set; }

    /// <summary>
    /// Background color to paint in the output before composing base and SVG.
    /// Defaults to transparent.
    /// </summary>
    public SKColor BackgroundColor { get; set; } = SKColors.Transparent;
}
