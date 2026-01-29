# Blob Storage Implementation

This document describes the blob storage implementation for handling file uploads (images, videos) in the Marketing API.

## Overview

The blob storage system provides a flexible, abstracted interface for storing and retrieving files. It supports:
- **Local File System** - For development and Railway staging (with persistent volumes)
- **Future Cloud Storage** - Can be extended to support S3, Azure Blob Storage, etc.

## Architecture

### Service Interface

`IBlobStorageService` provides a clean abstraction for storage operations:
- `UploadAsync` - Upload files and get storage URLs
- `UploadWithThumbnailAsync` - Upload with optional thumbnail generation
- `DeleteAsync` - Delete files from storage
- `GetAsync` - Retrieve file streams
- `ExistsAsync` - Check if files exist

### Implementation

**LocalFileBlobStorageService** - Current implementation:
- Stores files on local filesystem
- Organizes files by container (e.g., `feedback/{feedbackId}/images`)
- Generates unique file names to prevent conflicts
- Sanitizes file names for security
- Serves files via static file middleware

## Configuration

### Development (appsettings.json)

```json
{
  "BlobStorage": {
    "Provider": "LocalFile",
    "LocalPath": "./uploads",
    "BaseUrl": "/uploads"
  }
}
```

### Staging (appsettings.Staging.json)

```json
{
  "BlobStorage": {
    "Provider": "LocalFile",
    "LocalPath": "/app/uploads",
    "BaseUrl": "/uploads"
  }
}
```

### Railway Environment Variables

```bash
BlobStorage__Provider=LocalFile
BlobStorage__LocalPath=/app/uploads
BlobStorage__BaseUrl=/uploads
```

## Railway Setup

### Persistent Volume Configuration

For Railway staging, configure a persistent volume:

1. **In Railway Dashboard:**
   - Go to Marketing API service
   - Click **"Settings"** → **"Volumes"**
   - Click **"Add Volume"**
   - **Mount Path:** `/app/uploads`
   - **Volume Name:** `marketing-api-uploads`
   - Click **"Add"**

2. **Why Persistent Volumes?**
   - Files persist across deployments
   - Files survive container restarts
   - Essential for production file storage

### File Organization

Files are organized in containers:
```
/app/uploads/
  feedback/
    123/
      images/
        {guid}_photo.jpg
        {guid}_photo_thumb.jpg
      videos/
        {guid}_video.mp4
```

## Usage

### In FeedbackController

The `FeedbackController` uses the blob storage service to upload attachments:

```csharp
// Upload image
using var fileStream = file.OpenReadStream();
(string storageUrl, string? thumbnailUrl) = await _blobStorage.UploadWithThumbnailAsync(
    fileStream,
    file.FileName,
    file.ContentType,
    $"feedback/{feedbackId}/images",
    generateThumbnail: true);
```

### File Serving

Files are served via static file middleware:
- **URL Pattern:** `/uploads/{container}/{filename}`
- **Example:** `/uploads/feedback/123/images/abc123_photo.jpg`

## Security Considerations

### File Name Sanitization

- Removes path separators and invalid characters
- Limits file name length to 255 characters
- Prevents directory traversal attacks

### File Size Limits

- Maximum file size: 50MB (configured in `FeedbackController`)
- Enforced at controller level before upload

### Content Type Validation

- Images: `image/jpeg`, `image/png`, `image/gif`, `image/webp`
- Videos: `video/mp4`, `video/quicktime`, `video/x-msvideo`

## Future Enhancements

### Cloud Storage Support

The interface can be extended to support cloud storage providers:

1. **AWS S3**
   - Implement `S3BlobStorageService`
   - Configure with AWS credentials
   - Use S3 bucket for storage

2. **Azure Blob Storage**
   - Implement `AzureBlobStorageService`
   - Configure with Azure connection string
   - Use Azure storage account

3. **Cloudflare R2**
   - S3-compatible API
   - Can reuse S3 implementation
   - Lower cost alternative

### Thumbnail Generation

Currently, thumbnail generation is not implemented. Future enhancements:
- Image thumbnails using ImageSharp or similar
- Video thumbnail extraction
- Automatic thumbnail generation on upload

### CDN Integration

For production, consider:
- CloudFront (AWS)
- Azure CDN
- Cloudflare CDN
- Serve files from CDN instead of API server

## Testing

### Local Testing

1. Start the Marketing API
2. Upload a file via POST `/api/feedback/{id}/attachments/image`
3. Verify file appears in `./uploads/feedback/{id}/images/`
4. Access file via `/uploads/feedback/{id}/images/{filename}`

### Railway Testing

1. Deploy to Railway
2. Upload a file via API
3. Verify file persists after redeployment
4. Check Railway volume contains files

## Troubleshooting

### Files Not Persisting

**Issue:** Files disappear after deployment
**Solution:** Ensure persistent volume is configured in Railway

### Files Not Accessible

**Issue:** 404 when accessing `/uploads/...`
**Solution:**
- Verify static file middleware is configured
- Check `BlobStorage:BaseUrl` matches request path
- Ensure files exist in storage path

### Permission Errors

**Issue:** Cannot write to `/app/uploads`
**Solution:**
- Check Dockerfile creates directory with correct permissions
- Verify Railway volume mount path matches configuration

## Files Modified

### New Files
- `src/FWH.MarketingApi/Services/IBlobStorageService.cs`
- `src/FWH.MarketingApi/Services/LocalFileBlobStorageService.cs`
- `docs/BLOB-STORAGE.md` (this file)

### Modified Files
- `src/FWH.MarketingApi/Program.cs` - Added service registration and static file serving
- `src/FWH.MarketingApi/Controllers/FeedbackController.cs` - Uses blob storage service
- `src/FWH.MarketingApi/appsettings.json` - Added blob storage configuration
- `src/FWH.MarketingApi/appsettings.Staging.json` - Added staging configuration
- `src/FWH.MarketingApi/Dockerfile` - Creates uploads directory
- `docs/deployment/railway-staging-setup.md` - Added blob storage setup instructions

## Migration Notes

### From Simulated Storage

Previously, attachments stored simulated URLs like `/uploads/feedback/{id}/images/{file}`. Now:
- Files are actually stored on disk
- URLs point to real files
- Files persist across deployments (with Railway volumes)

### Database Schema

No database changes required. The `storage_url` column in `feedback_attachments` now contains actual file URLs.
