using Xunit;
using SkiaSharp;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Imaging;
using FWH.Common.Imaging.Extensions;

namespace FWH.Common.Imaging.Tests.Imaging;

public class ImagingServiceAdvancedTests
{
    private static SKColor GetPixel(SKBitmap bmp, int x, int y) => bmp.GetPixel(x, y);

    private static void AssertColorApproximately(SKColor expected, SKColor actual, byte tolerance = 2)
    {
        Assert.InRange(actual.Red, (int)expected.Red - tolerance, (int)expected.Red + tolerance);
        Assert.InRange(actual.Green, (int)expected.Green - tolerance, (int)expected.Green + tolerance);
        Assert.InRange(actual.Blue, (int)expected.Blue - tolerance, (int)expected.Blue + tolerance);
    }

    [Fact]
    public void RenderSvgOverlay_LargerSvg_RendersCorrectSize()
    {
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        var baseInfo = new SKImageInfo(100, 100, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var canvas = new SKCanvas(baseBitmap))
        {
            canvas.Clear(new SKColor(255, 0, 0));
        }

        // SVG with a 20x20 green rectangle
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"20\">" +
                  "<rect width=\"20\" height=\"20\" fill=\"#00FF00\" />" +
                  "</svg>";

        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 10.2f, y: 10.7f);

        // Pixel inside expected overlay area should be green
        var inside = GetPixel(result, 15, 15);
        Assert.Equal(0, inside.Red);
        Assert.Equal(255, inside.Green);
        Assert.Equal(0, inside.Blue);

        // Pixel just outside overlay should remain red
        var outside = GetPixel(result, 9, 9);
        Assert.Equal(255, outside.Red);
        Assert.Equal(0, outside.Green);
        Assert.Equal(0, outside.Blue);
    }

    [Fact]
    public void RenderSvgOverlay_SemiTransparentOverlay_BlendsWithBase()
    {
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        var baseInfo = new SKImageInfo(100, 100, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var canvas = new SKCanvas(baseBitmap))
        {
            canvas.Clear(new SKColor(255, 0, 0)); // red
        }

        // SVG blue square with 50% opacity using rgba
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\">" +
                  "<rect width=\"10\" height=\"10\" fill=\"rgba(0,0,255,0.5)\" />" +
                  "</svg>";

        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 40.5f, y: 40.5f);

        var blended = GetPixel(result, 40, 40);

        // Expected blend: 50% red (255,0,0) and 50% blue (0,0,255) => (127.5,0,127.5) ~ (128,0,128)
        var expected = new SKColor(128, 0, 128);
        AssertColorApproximately(expected, blended, tolerance: 3);
    }

    [Fact]
    public void RenderSvgOverlay_VaryingOpacity_BlendsAccordingly()
    {
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        var baseInfo = new SKImageInfo(50, 50, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var canvas = new SKCanvas(baseBitmap))
        {
            canvas.Clear(new SKColor(0, 255, 0)); // green base
        }

        // 25% opaque red square using fill-opacity
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\">" +
                  "<rect width=\"10\" height=\"10\" fill=\"#FF0000\" fill-opacity=\"0.25\" />" +
                  "</svg>";

        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 5.5f, y: 5.5f);

        var blended = GetPixel(result, 5, 5);

        // Blend: 25% red (255,0,0) over 75% green (0,255,0):
        // R = 0.25*255 + 0.75*0 = 63.75 ~ 64
        // G = 0.25*0 + 0.75*255 = 191.25 ~ 191
        // B = 0
        var expected = new SKColor(64, 191, 0);
        AssertColorApproximately(expected, blended, tolerance: 3);
    }
}
