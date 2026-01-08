using SkiaSharp;
using System.IO;

namespace FWH.Common.Imaging;

public interface IImagingService
{
    /// <summary>
    /// Renders the provided SVG text and overlays it on the supplied base SKBitmap at the given coordinates.
    /// Returns a new <see cref="SKBitmap"/> instance containing the composited image. Caller is responsible for disposing the returned bitmap.
    /// </summary>
    SKBitmap RenderSvgOverlay(SKBitmap baseBitmap, string svg, float x = 0, float y = 0, ImagingOptions? options = null);

    /// <summary>
    /// Renders the provided SVG text and overlays it on the supplied base SKBitmap, then returns an encoded PNG stream.
    /// The returned stream is positioned at the beginning and the caller is responsible for disposing it.
    /// </summary>
    Stream RenderSvgOverlayToPngStream(SKBitmap baseBitmap, string svg, float x = 0, float y = 0, ImagingOptions? options = null);

    /// <summary>
    /// Renders the provided SVG text and overlays it on the supplied base SKBitmap at the given coordinates,
    /// using the specified output dimensions. Returns a new <see cref="SKBitmap"/> instance containing the composited image.
    /// Caller is responsible for disposing the returned bitmap.
    /// </summary>
    SKBitmap RenderSvgOverlay(SKBitmap baseBitmap, string svg, int outputWidth, int outputHeight, float x = 0, float y = 0, ImagingOptions? options = null);

    /// <summary>
    /// Renders the provided SVG text and overlays it on the supplied base SKBitmap, then returns an encoded PNG stream,
    /// using the specified output dimensions. The returned stream is positioned at the beginning and the caller is responsible for disposing it.
    /// </summary>
    Stream RenderSvgOverlayToPngStream(SKBitmap baseBitmap, string svg, int outputWidth, int outputHeight, float x = 0, float y = 0, ImagingOptions? options = null);
}
