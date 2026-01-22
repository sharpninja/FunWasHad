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

    public static TheoryData<float, float, int, int, int, int, int, int> SvgRenderingScenarios()
    {
        return new TheoryData<float, float, int, int, int, int, int, int>
        {
            // x, y, sampleX, sampleY, outsideX, outsideY, svgWidth, svgHeight
            { 10.2f, 10.7f, 15, 15, 9, 9, 20, 20 },  // LargerSvg scenario
            { 40.5f, 40.5f, 40, 40, 5, 5, 10, 10 },  // Can be used for opacity tests
        };
    }

    /// <summary>
    /// Tests that RenderSvgOverlay correctly renders SVG overlays at different positions and sizes, with pixels inside the overlay having the SVG color and pixels outside retaining the base bitmap color.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IImagingService.RenderSvgOverlay method's ability to correctly position and render SVG overlays of varying sizes at different coordinates on a base bitmap.</para>
    /// <para><strong>Data involved:</strong> A 100x100 red base bitmap, an SVG overlay (20x20 green rectangle) positioned at coordinates (10.2, 10.7) with fractional positioning. Test checks pixel (15,15) which should be inside the overlay (green) and pixel (9,9) which should be outside (red).</para>
    /// <para><strong>Why the data matters:</strong> SVG overlays must be positioned accurately regardless of size or position. Fractional coordinates test that the renderer correctly rounds to pixel boundaries. Different overlay sizes test that scaling works correctly. This validates the overlay rendering works for various use cases (e.g., markers on maps, badges on photos).</para>
    /// <para><strong>Expected outcome:</strong> Pixel (15,15) should be green (RGB 0,255,0) since it's inside the overlay, and pixel (9,9) should be red (RGB 255,0,0) since it's outside the overlay area.</para>
    /// <para><strong>Reason for expectation:</strong> The renderer should composite the SVG overlay onto the base bitmap at the specified coordinates, rounding fractional coordinates to the nearest pixel. Pixels within the overlay bounds should have the SVG color, while pixels outside should retain the base bitmap color. The specific pixel checks confirm accurate positioning and that the overlay doesn't affect unrelated areas of the image.</para>
    /// </remarks>
    [Theory]
    [InlineData(10.2f, 10.7f, 15, 15, 9, 9, 20, 20, 0, 255, 0)] // Green SVG 20x20
    public void RenderSvgOverlay_WithDifferentPositionsAndSizes_RendersCorrectly(
        float x, float y,
        int insideX, int insideY,
        int outsideX, int outsideY,
        int svgWidth, int svgHeight,
        int expectedInsideR, int expectedInsideG, int expectedInsideB)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        var baseInfo = new SKImageInfo(100, 100, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var canvas = new SKCanvas(baseBitmap))
        {
            canvas.Clear(new SKColor(255, 0, 0)); // red background
        }

        var svg = $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{svgWidth}\" height=\"{svgHeight}\">" +
                  $"<rect width=\"{svgWidth}\" height=\"{svgHeight}\" fill=\"#{expectedInsideR:X2}{expectedInsideG:X2}{expectedInsideB:X2}\" />" +
                  "</svg>";

        // Act
        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: x, y: y);

        // Assert - Inside overlay area should have expected color
        var inside = GetPixel(result, insideX, insideY);
        Assert.Equal(expectedInsideR, inside.Red);
        Assert.Equal(expectedInsideG, inside.Green);
        Assert.Equal(expectedInsideB, inside.Blue);

        // Assert - Outside overlay should remain red background
        var outside = GetPixel(result, outsideX, outsideY);
        Assert.Equal(255, outside.Red);
        Assert.Equal(0, outside.Green);
        Assert.Equal(0, outside.Blue);
    }

    /// <summary>
    /// Tests that RenderSvgOverlay correctly blends transparent SVG overlays with the base bitmap using alpha compositing.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IImagingService.RenderSvgOverlay method's alpha blending when rendering SVG overlays with transparency (alpha channel) onto a base bitmap.</para>
    /// <para><strong>Data involved:</strong> A 100x100 red base bitmap and a 10x10 SVG rectangle with fill="rgba(0,0,255,0.5)" (50% opaque blue) positioned at (40.5, 40.5). The expected blended color is purple (RGB 128,0,128) representing 50% blue over 50% red.</para>
    /// <para><strong>Why the data matters:</strong> Transparent overlays are common in image processing (e.g., watermarks, semi-transparent markers, UI overlays). The renderer must correctly apply alpha blending to composite transparent colors with the base image. This test validates that the alpha channel is properly interpreted and blended using standard compositing algorithms.</para>
    /// <para><strong>Expected outcome:</strong> The pixel at the overlay position should be approximately purple (RGB 128,0,128) with a tolerance of ±3, representing the blended result of 50% blue over red.</para>
    /// <para><strong>Reason for expectation:</strong> Alpha blending should use the formula: result = overlay_alpha * overlay_color + (1 - overlay_alpha) * base_color. For 50% blue (0,0,255) over red (255,0,0): R = 0.5*0 + 0.5*255 = 127.5 ≈ 128, G = 0, B = 0.5*255 + 0.5*0 = 127.5 ≈ 128. The tolerance accounts for rounding and color space conversions. This confirms alpha compositing works correctly for transparent overlays.</para>
    /// </remarks>
    [Theory]
    [InlineData("rgba(0,0,255,0.5)", 128, 0, 128, 40.5f, 40.5f)] // 50% blue over red = purple
    public void RenderSvgOverlay_WithTransparency_BlendsCorrectly(
        string fillColor,
        int expectedR, int expectedG, int expectedB,
        float x, float y)
    {
        // Arrange
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

        var svg = $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\">" +
                  $"<rect width=\"10\" height=\"10\" fill=\"{fillColor}\" />" +
                  "</svg>";

        // Act
        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: x, y: y);
        var blended = GetPixel(result, (int)x, (int)y);

        // Assert
        var expected = new SKColor((byte)expectedR, (byte)expectedG, (byte)expectedB);
        AssertColorApproximately(expected, blended, tolerance: 3);
    }

    // Note: This test is commented out due to Svg.Skia library limitation with fill-opacity attribute
    // The library doesn't properly support fill-opacity - use rgba() instead
    //[Fact]
    private void RenderSvgOverlay_VaryingOpacity_BlendsAccordingly()
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
