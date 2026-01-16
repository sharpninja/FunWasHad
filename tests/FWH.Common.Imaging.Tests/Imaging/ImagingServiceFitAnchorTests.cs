using System;
using System.Collections.Generic;
using Xunit;
using SkiaSharp;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Imaging;
using FWH.Common.Imaging.Extensions;

namespace FWH.Common.Imaging.Tests.Imaging;

public class ImagingServiceFitAnchorTests
{
    public static IEnumerable<object[]> FitAnchorData()
    {
        foreach (FitMode fm in Enum.GetValues(typeof(FitMode)))
        {
            foreach (Anchor a in Enum.GetValues(typeof(Anchor)))
            {
                yield return new object[] { fm, a };
            }
        }
    }

    [Theory]
    [MemberData(nameof(FitAnchorData))]
    public void RenderSvgOverlay_FitModeAnchor_ComposesOverlay(FitMode fitMode, Anchor anchor)
    {
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        const int baseW = 100;
        const int baseH = 100;

        // Create base red bitmap
        var baseInfo = new SKImageInfo(baseW, baseH, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var c = new SKCanvas(baseBitmap)) c.Clear(new SKColor(255, 0, 0));

        // Simple SVG: rectangle 20x10 filled blue
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"10\">" +
                  "<rect x=\"0\" y=\"0\" width=\"20\" height=\"10\" fill=\"#0000FF\" />" +
                  "</svg>";

        var options = new ImagingOptions { FitMode = fitMode, Anchor = anchor, Padding = 0 };

        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 0, y: 0, options: options);

        // Compute expected placement using same logic as ImagingService
        var srcW = 20f; var srcH = 10f;
        var paddedW = baseW - options.Padding * 2;
        var paddedH = baseH - options.Padding * 2;

        float desiredWf = srcW, desiredHf = srcH;
        switch (options.FitMode)
        {
            case FitMode.BestFit:
                {
                    var scaleX = paddedW / srcW;
                    var scaleY = paddedH / srcH;
                    var scale = Math.Min(scaleX, scaleY);
                    desiredWf = srcW * scale; desiredHf = srcH * scale; break;
                }
            case FitMode.Fill:
                {
                    var scaleX = paddedW / srcW;
                    var scaleY = paddedH / srcH;
                    var scale = Math.Max(scaleX, scaleY);
                    desiredWf = srcW * scale; desiredHf = srcH * scale; break;
                }
            case FitMode.Stretch:
                desiredWf = paddedW; desiredHf = paddedH; break;
        }

        var desiredW = Math.Ceiling(desiredWf);
        var desiredH = Math.Ceiling(desiredHf);

        float offsetX = 0f, offsetY = 0f;
        switch (options.Anchor)
        {
            case Anchor.Center:
                offsetX = options.Padding + (float)((paddedW - desiredW) / 2.0);
                offsetY = options.Padding + (float)((paddedH - desiredH) / 2.0);
                break;
            case Anchor.TopLeft:
                offsetX = options.Padding; offsetY = options.Padding; break;
            case Anchor.TopRight:
                offsetX = options.Padding + (float)(paddedW - desiredW); offsetY = options.Padding; break;
            case Anchor.BottomLeft:
                offsetX = options.Padding; offsetY = options.Padding + (float)(paddedH - desiredH); break;
            case Anchor.BottomRight:
                offsetX = options.Padding + (float)(paddedW - desiredW); offsetY = options.Padding + (float)(paddedH - desiredH); break;
        }

        // Intersection with output
        var interLeft = Math.Max(0, offsetX);
        var interTop = Math.Max(0, offsetY);
        var interRight = Math.Min(baseW, offsetX + (float)desiredW);
        var interBottom = Math.Min(baseH, offsetY + (float)desiredH);

        Assert.True(interRight > interLeft && interBottom > interTop, "Overlay did not intersect output (unexpected)");

        var sampleX = (int)Math.Round((interLeft + interRight) / 2.0);
        var sampleY = (int)Math.Round((interTop + interBottom) / 2.0);
        sampleX = Math.Clamp(sampleX, 0, baseW - 1);
        sampleY = Math.Clamp(sampleY, 0, baseH - 1);

        var color = result.GetPixel(sampleX, sampleY);

        // Expect blue at the sampled point
        Assert.Equal(0, color.Red);
        Assert.Equal(0, color.Green);
        Assert.Equal(255, color.Blue);

        // Additionally assert the overlay top-left aligns exactly for each anchor (rounded to nearest pixel)
        var expectedTopLeftX = (int)Math.Round(offsetX);
        var expectedTopLeftY = (int)Math.Round(offsetY);
        expectedTopLeftX = Math.Clamp(expectedTopLeftX, 0, baseW - 1);
        expectedTopLeftY = Math.Clamp(expectedTopLeftY, 0, baseH - 1);

        var topLeftColor = result.GetPixel(expectedTopLeftX, expectedTopLeftY);
        Assert.Equal(0, topLeftColor.Red);
        Assert.Equal(0, topLeftColor.Green);
        Assert.Equal(255, topLeftColor.Blue);
    }

    [Fact]
    public void RenderSvgOverlay_PaddingAndBackground_AppliedCorrectly()
    {
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        const int baseW = 120;
        const int baseH = 80;

        var baseInfo = new SKImageInfo(baseW, baseH, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var c = new SKCanvas(baseBitmap)) c.Clear(new SKColor(200, 200, 200));

        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"10\">" +
                  "<rect x=\"0\" y=\"0\" width=\"20\" height=\"10\" fill=\"#0000FF\" />" +
                  "</svg>";

        var options = new ImagingOptions { FitMode = FitMode.BestFit, Anchor = Anchor.TopLeft, Padding = 10, BackgroundColor = new SKColor(0, 255, 0) };

        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 0, y: 0, options: options);

        // Corners outside padded area should equal background color (0,255,0)
        var c1 = result.GetPixel(0, 0);
        Assert.Equal(0, c1.Red); Assert.Equal(255, c1.Green); Assert.Equal(0, c1.Blue);

        // Pixel inside padded area but where base is drawn should not be background; check somewhere near padded top-left + small offset
        var insideX = options.Padding + 1;
        var insideY = options.Padding + 1;
        var c2 = result.GetPixel(insideX, insideY);

        // c2 should be either base bitmap or overlay; ensure it's not the background
        Assert.False(c2.Equals(new SKColor(0, 255, 0)));
    }
}
