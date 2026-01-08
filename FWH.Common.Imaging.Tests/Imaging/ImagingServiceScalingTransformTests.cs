using Xunit;
using SkiaSharp;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Imaging;
using FWH.Common.Imaging.Extensions;

namespace FWH.Common.Imaging.Tests.Imaging;

public class ImagingServiceScalingTransformTests
{
    private static SKColor GetPixel(SKBitmap bmp, int x, int y) => bmp.GetPixel(x, y);

    [Fact]
    public void RenderSvgOverlay_ViewBoxScaling_AppliesCorrectScale()
    {
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        var baseInfo = new SKImageInfo(200, 200, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var canvas = new SKCanvas(baseBitmap))
        {
            canvas.Clear(new SKColor(255, 255, 255)); // white background
        }

        // SVG defines viewBox 0 0 100 100 but width/height 50x50 - should scale down
        var svg = @"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100' width='50' height='50'>
                        <rect x='0' y='0' width='100' height='100' fill='#FF0000' />
                    </svg>";

        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 10f, y: 10f);

        // Since rect covers the whole viewBox and width/height 50x50, the rendered rect should be 50x50
        // Check center pixel within expected scaled area
        var center = GetPixel(result, 10 + 25, 10 + 25); // center of the 50x50 area
        Assert.Equal(255, center.Red);
        Assert.Equal(0, center.Green);
        Assert.Equal(0, center.Blue);

        // Outside area remains white
        var outside = GetPixel(result, 5, 5);
        Assert.Equal(255, outside.Red);
        Assert.Equal(255, outside.Green);
        Assert.Equal(255, outside.Blue);
    }

    [Fact]
    public void RenderSvgOverlay_RotatedSvg_RendersRotatedContent()
    {
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        var baseInfo = new SKImageInfo(100, 100, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var canvas = new SKCanvas(baseBitmap))
        {
            canvas.Clear(new SKColor(255, 255, 255)); // white background
        }

        // SVG: a red rectangle rotated 45 degrees about its center
        var svg = @"<svg xmlns='http://www.w3.org/2000/svg' width='40' height='40' viewBox='0 0 40 40'>
                        <rect x='10' y='0' width='20' height='40' fill='#FF0000' transform='rotate(45 20 20)' />
                    </svg>";

        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 30f, y: 30f);

        // Rotated shape should place red pixels off the original axis; check a couple of expected locations
        var p1 = GetPixel(result, 30 + 20, 30 + 20); // center area should be red
        Assert.Equal(255, p1.Red);

        // A corner outside rotated rect should remain white
        var outside = GetPixel(result, 30 + 0, 30 + 0);
        Assert.Equal(255, outside.Red);
        Assert.Equal(255, outside.Green);
        Assert.Equal(255, outside.Blue);
    }
}
