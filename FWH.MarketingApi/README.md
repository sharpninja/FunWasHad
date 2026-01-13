# Marketing API

REST API for business marketing features in the Fun Was Had application.

## Features

### Business Marketing Data
- **Themes**: Custom app appearance when at business location
- **Coupons**: Promotional offers and discounts
- **Menus**: Restaurant menus and product listings
- **News**: Business announcements and updates

### User Feedback
- **Text Reviews**: Written feedback with ratings
- **Image Attachments**: Photo reviews
- **Video Attachments**: Video reviews

## API Endpoints

### Marketing Endpoints

#### Get Complete Marketing Data
```http
GET /api/marketing/{businessId}
```

Returns theme, active coupons, available menu items, and recent news.

#### Get Business Theme
```http
GET /api/marketing/{businessId}/theme
```

#### Get Active Coupons
```http
GET /api/marketing/{businessId}/coupons
```

#### Get Menu Items
```http
GET /api/marketing/{businessId}/menu?category={category}
```

#### Get Menu Categories
```http
GET /api/marketing/{businessId}/menu/categories
```

#### Get News Items
```http
GET /api/marketing/{businessId}/news?limit=10
```

#### Find Nearby Businesses
```http
GET /api/marketing/nearby?latitude={lat}&longitude={lon}&radiusMeters=1000
```

### Feedback Endpoints

#### Submit Feedback
```http
POST /api/feedback
Content-Type: application/json

{
  "businessId": 1,
  "userId": "device-123",
  "userName": "John Doe",
  "userEmail": "john@example.com",
  "feedbackType": "review",
  "subject": "Great experience!",
  "message": "Had an amazing time...",
  "rating": 5,
  "isPublic": true,
  "latitude": 37.7749,
  "longitude": -122.4194
}
```

#### Upload Image Attachment
```http
POST /api/feedback/{feedbackId}/attachments/image
Content-Type: multipart/form-data

file: <image file>
```

#### Upload Video Attachment
```http
POST /api/feedback/{feedbackId}/attachments/video
Content-Type: multipart/form-data

file: <video file>
```

#### Get Feedback
```http
GET /api/feedback/{id}
```

#### Get Business Feedback
```http
GET /api/feedback/business/{businessId}?publicOnly=true&includeAttachments=false
```

#### Get Feedback Statistics
```http
GET /api/feedback/business/{businessId}/stats
```

## Database Schema

### Tables
- `businesses` - Business information
- `business_themes` - Theme customization
- `coupons` - Promotional offers
- `menu_items` - Menu/product items
- `news_items` - News and announcements
- `feedback` - User feedback
- `feedback_attachments` - Media attachments

## Running Locally

```bash
# Start with Aspire AppHost
dotnet run --project FWH.AppHost

# Or run directly
dotnet run --project FWH.MarketingApi
```

## Configuration

Connection string is managed by Aspire. For standalone:

```json
{
  "ConnectionStrings": {
    "marketing": "Host=localhost;Port=5432;Database=marketing;Username=postgres;Password=postgres"
  }
}
```

## File Upload Limits

- Maximum file size: 50MB
- Supported image types: JPEG, PNG, GIF, WebP
- Supported video types: MP4, QuickTime, AVI
