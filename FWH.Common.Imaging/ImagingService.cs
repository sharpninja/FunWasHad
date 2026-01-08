using System;
using System.IO;
using SkiaSharp;
using Svg.Skia;

namespace FWH.Common.Imaging;

public class ImagingService : IImagingService
{
    public SKBitmap RenderSvgOverlay(SKBitmap baseBitmap, string svg, float x = 0, float y = 0, ImagingOptions? options = null)
    {
        return RenderSvgOverlay(baseBitmap, svg, baseBitmap.Width, baseBitmap.Height, x, y, options);
    }

    public Stream RenderSvgOverlayToPngStream(SKBitmap baseBitmap, string svg, float x = 0, float y = 0, ImagingOptions? options = null)
    {
        return RenderSvgOverlayToPngStream(baseBitmap, svg, baseBitmap.Width, baseBitmap.Height, x, y, options);
    }

    public SKBitmap RenderSvgOverlay(SKBitmap baseBitmap, string svg, int outputWidth, int outputHeight, float x = 0, float y = 0, ImagingOptions? options = null)
    {
        options ??= new ImagingOptions();

        ArgumentNullException.ThrowIfNull(baseBitmap);
        if (string.IsNullOrWhiteSpace(svg)) throw new ArgumentException("SVG must not be empty", nameof(svg));
        if (outputWidth <= 0 || outputHeight <= 0) throw new ArgumentException("Output dimensions must be positive");

        // Load SVG using Svg.Skia
        var svgDrawable = new SKSvg();
        using var svgStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svg));
        var svgPicture = svgDrawable.Load(svgStream);

        if (svgPicture == null)
        {
            throw new InvalidOperationException("Failed to parse SVG into drawable picture.");
        }

        // Get the SVG's natural size
        var srcRect = svgPicture.CullRect;
        var srcWidth = srcRect.Width;
        var srcHeight = srcRect.Height;

        // Fallback to minimal size if needed
        if (srcWidth <= 0) srcWidth = 1;
        if (srcHeight <= 0) srcHeight = 1;

        // Apply padding to output dimensions
        var paddedOutputWidth = Math.Max(1, outputWidth - options.Padding * 2);
        var paddedOutputHeight = Math.Max(1, outputHeight - options.Padding * 2);

        // Compute desired size based on FitMode
        float desiredWidthF = srcWidth;
        float desiredHeightF = srcHeight;

        switch (options.FitMode)
        {
            case FitMode.BestFit:
                {
                    var scaleX = paddedOutputWidth / srcWidth;
                    var scaleY = paddedOutputHeight / srcHeight;
                    var scale = Math.Min(scaleX, scaleY);
                    if (scale <= 0) scale = 1f;
                    desiredWidthF = srcWidth * scale;
                    desiredHeightF = srcHeight * scale;
                    break;
                }
            case FitMode.Fill:
                {
                    var scaleX = paddedOutputWidth / srcWidth;
                    var scaleY = paddedOutputHeight / srcHeight;
                    var scale = Math.Max(scaleX, scaleY);
                    if (scale <= 0) scale = 1f;
                    desiredWidthF = srcWidth * scale;
                    desiredHeightF = srcHeight * scale;
                    break;
                }
            case FitMode.Stretch:
                {
                    desiredWidthF = paddedOutputWidth;
                    desiredHeightF = paddedOutputHeight;
                    break;
                }
        }

        var desiredWidth = Math.Max(1, (int)Math.Ceiling(desiredWidthF));
        var desiredHeight = Math.Max(1, (int)Math.Ceiling(desiredHeightF));

        // Compute alignment offsets based on anchor within padded area
        float offsetX = 0f, offsetY = 0f;
        switch (options.Anchor)
        {
            case Anchor.Center:
                offsetX = options.Padding + (paddedOutputWidth - desiredWidth) / 2f;
                offsetY = options.Padding + (paddedOutputHeight - desiredHeight) / 2f;
                break;
            case Anchor.TopLeft:
                offsetX = options.Padding;
                offsetY = options.Padding;
                break;
            case Anchor.TopRight:
                offsetX = options.Padding + (paddedOutputWidth - desiredWidth);
                offsetY = options.Padding;
                break;
            case Anchor.BottomLeft:
                offsetX = options.Padding;
                offsetY = options.Padding + (paddedOutputHeight - desiredHeight);
                break;
            case Anchor.BottomRight:
                offsetX = options.Padding + (paddedOutputWidth - desiredWidth);
                offsetY = options.Padding + (paddedOutputHeight - desiredHeight);
                break;
        }

        // Apply user offsets
        offsetX += x;
        offsetY += y;

        var outInfo = new SKImageInfo(outputWidth, outputHeight, baseBitmap.ColorType, baseBitmap.AlphaType);
        var outBitmap = new SKBitmap(outInfo);

        using (var canvas = new SKCanvas(outBitmap))
        {
            // Paint background color if provided
            canvas.Clear(options.BackgroundColor);

            // Draw scaled base bitmap within the padded area
            if (options.Padding > 0)
            {
                // Draw base bitmap only within padded area
                var paddedRect = new SKRect(options.Padding, options.Padding, 
                    outputWidth - options.Padding, outputHeight - options.Padding);
                
                canvas.Save();
                canvas.ClipRect(paddedRect);
                canvas.Translate(options.Padding, options.Padding);
                
                // Scale to fit the padded area if needed
                if (baseBitmap.Width != paddedOutputWidth || baseBitmap.Height != paddedOutputHeight)
                {
                    var scaleX = (float)paddedOutputWidth / baseBitmap.Width;
                    var scaleY = (float)paddedOutputHeight / baseBitmap.Height;
                    var scale = Math.Min(scaleX, scaleY);
                    canvas.Scale(scale, scale);
                }
                
                canvas.DrawBitmap(baseBitmap, 0, 0);
                canvas.Restore();
            }
            else
            {
                // No padding - draw base bitmap normally
                if (baseBitmap.Width == outputWidth && baseBitmap.Height == outputHeight)
                {
                    canvas.DrawBitmap(baseBitmap, 0, 0);
                }
                else
                {
                    // Fit base bitmap into output preserving aspect ratio (letterbox)
                    var bw = baseBitmap.Width;
                    var bh = baseBitmap.Height;
                    var scaleX = (float)outputWidth / bw;
                    var scaleY = (float)outputHeight / bh;
                    var baseScale = Math.Min(scaleX, scaleY);
                    var drawW = bw * baseScale;
                    var drawH = bh * baseScale;
                    var drawX = (outputWidth - drawW) / 2f;
                    var drawY = (outputHeight - drawH) / 2f;
                    canvas.Save();
                    canvas.Translate(drawX, drawY);
                    canvas.Scale(baseScale, baseScale);
                    canvas.DrawBitmap(baseBitmap, 0, 0);
                    canvas.Restore();
                }
            }

            // Render SVG directly onto the canvas at the computed position
            // Round to nearest pixel to avoid anti-aliasing artifacts at fractional positions
            var roundedOffsetX = (float)Math.Round(offsetX);
            var roundedOffsetY = (float)Math.Round(offsetY);
            
            canvas.Save();
            canvas.Translate(roundedOffsetX, roundedOffsetY);

            // Calculate scale to match desired size
            var scaleToFitX = desiredWidthF / srcWidth;
            var scaleToFitY = desiredHeightF / srcHeight;
            canvas.Scale(scaleToFitX, scaleToFitY);

            // Translate to handle SVG's CullRect offset
            canvas.Translate(-srcRect.Left, -srcRect.Top);

            // Draw the SVG picture
            canvas.DrawPicture(svgPicture);

            canvas.Restore();
            canvas.Flush();
        }

        return outBitmap;
    }

    public Stream RenderSvgOverlayToPngStream(SKBitmap baseBitmap, string svg, int outputWidth, int outputHeight, float x = 0, float y = 0, ImagingOptions? options = null)
    {
        using var composed = RenderSvgOverlay(baseBitmap, svg, outputWidth, outputHeight, x, y, options);
        using var image = SKImage.FromBitmap(composed);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var ms = new MemoryStream();
        data.SaveTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}
