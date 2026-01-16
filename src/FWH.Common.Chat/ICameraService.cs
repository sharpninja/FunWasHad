using System.Threading.Tasks;

namespace FWH.Common.Chat.Services;

/// <summary>
/// Platform-specific camera service for capturing photos
/// </summary>
public interface ICameraService
{
    /// <summary>
    /// Opens the system camera app and captures a photo
    /// </summary>
    /// <returns>Byte array of the captured image (JPEG format), or null if cancelled</returns>
    Task<byte[]?> TakePhotoAsync();

    /// <summary>
    /// Checks if the device has a camera available
    /// </summary>
    bool IsCameraAvailable { get; }
}
