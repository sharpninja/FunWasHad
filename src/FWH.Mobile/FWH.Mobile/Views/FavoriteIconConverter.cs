using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using FWH.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Views;

/// <summary>
/// Converter to display star icon based on favorite status.
/// </summary>
public class FavoriteIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isFavorite)
        {
            return isFavorite ? "⭐" : "☆";
        }
        return "☆";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter to display tooltip text based on favorite status.
/// </summary>
public class FavoriteTooltipConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isFavorite)
        {
            return isFavorite ? "Remove from favorites" : "Add to favorites";
        }
        return "Add to favorites";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter to load images from URL strings using ImageService.
/// </summary>
public class UrlToImageConverter : IValueConverter
{
    private static readonly Dictionary<string, Bitmap?> _imageCache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrEmpty(url))
            return null;

        // Check cache first
        if (_imageCache.TryGetValue(url, out var cachedBitmap))
            return cachedBitmap;

        // Get ImageService from DI
        var imageService = App.ServiceProvider?.GetService<IImageService>();
        if (imageService == null)
        {
            // Fallback to direct download if ImageService not available
            return LoadImageDirectly(url);
        }

        // Load image via ImageService (which will cache in database)
        try
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    // Extract image type from parameter if provided
                    var imageType = parameter?.ToString() ?? "general";
                    var imageData = await imageService.GetImageAsync(url, imageType).ConfigureAwait(false);

                    if (imageData != null && imageData.Length > 0)
                    {
                        using var stream = new System.IO.MemoryStream(imageData);
                        return new Bitmap(stream);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading image via ImageService: {ex.Message}");
                }
                return null;
            });

            // Wait for result (blocks UI thread - not ideal but works)
            var bitmap = task.Result;
            if (bitmap != null)
            {
                _imageCache[url] = bitmap;
            }
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap? LoadImageDirectly(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            var task = Task.Run(async () =>
            {
                try
                {
                    var response = await httpClient.GetAsync(new Uri(url)).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        return new Bitmap(stream);
                    }
                }
                catch
                {
                    // Ignore errors
                }
                return null;
            });

            return task.Result;
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
