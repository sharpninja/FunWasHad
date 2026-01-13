# Marketing API Implementation Summary

**Date:** 2026-01-13  
**Status:** ✅ **COMPLETE**  
**Feature:** Business Marketing and Feedback API

---

## Overview

Successfully implemented a comprehensive Marketing API that enables businesses to advertise through the mobile app, customize the user experience, and collect user feedback. The API includes business branding, themes, coupons, menus, news, and multimedia feedback submission.

---

## Requirements Met

### User Story
> Add a new API called MarketingApi. This API will be used to retrieve data about a business. Business can advertise through the mobile app to have branding added to posted reviews. Subscribing business can also have specific themes applied to the app while users are in their location. Businesses can also offer coupons through the app. In this api, add methods for: retrieving current theme, coupons, menus and news about a business, and submitting feedback from users to businesses, including text, video and pictures.

### Acceptance Criteria
✅ New MarketingApi project created with Aspire integration  
✅ GET endpoints for themes, coupons, menus, and news  
✅ POST endpoints for text feedback submission  
✅ File upload endpoints for image and video attachments  
✅ Database schema with migrations  
✅ Registered in AppHost with PostgreSQL  
✅ Service defaults and configuration  
✅ Build successful  

---

## Architecture

### Project Structure

```
FWH.MarketingApi/
├── Controllers/
│   ├── MarketingController.cs     # Business marketing data endpoints
│   └── FeedbackController.cs      # User feedback endpoints
├── Models/
│   ├── BusinessModels.cs          # Business, Theme, Coupon, Menu, News models
│   └── FeedbackModels.cs          # Feedback and attachment models
├── Data/
│   ├── MarketingDbContext.cs      # Entity Framework context
│   └── DatabaseMigrationService.cs # SQL migration service
├── Migrations/
│   └── 001_create_marketing_schema.sql
├── FWH.MarketingApi.csproj
├── Program.cs
├── Dockerfile
├── appsettings.json
└── README.md
```

---

## Database Schema

### Tables Created

#### businesses
- **Purpose:** Core business information
- **Key Fields:** name, address, latitude, longitude, is_subscribed
- **Indexes:** Location (lat/lon), subscription status

#### business_themes
- **Purpose:** Custom app appearance for business locations
- **Key Fields:** theme_name, colors (primary, secondary, accent, etc.), logo_url, custom_css
- **Relationship:** One-to-one with businesses

#### coupons
- **Purpose:** Promotional offers and discounts
- **Key Fields:** title, description, code, discount amounts, validity dates
- **Indexes:** Business + active status, validity period
- **Features:** Max redemptions tracking

#### menu_items
- **Purpose:** Restaurant menus or product listings
- **Key Fields:** name, category, price, calories, allergens, dietary tags
- **Indexes:** Business + category + availability
- **Features:** Sort order, nutritional information

#### news_items
- **Purpose:** Business announcements and updates
- **Key Fields:** title, content, published_at, expires_at
- **Indexes:** Business + published status + date
- **Features:** Featured items, expiration

#### feedback
- **Purpose:** User reviews and feedback
- **Key Fields:** user_id, feedback_type, subject, message, rating (1-5)
- **Indexes:** Business + date, user + date, public + approved
- **Features:** Moderation, business responses, location context

#### feedback_attachments
- **Purpose:** Media files for feedback
- **Key Fields:** attachment_type (image/video), storage_url, thumbnail_url
- **Features:** File size tracking, duration for videos

---

## API Endpoints

### Marketing Endpoints

#### 1. Get Complete Marketing Data
```http
GET /api/marketing/{businessId}
```

**Response:**
```json
{
  "businessId": 1,
  "businessName": "Starbucks Coffee",
  "theme": {
    "themeName": "Starbucks Green",
    "primaryColor": "#00704A",
    "secondaryColor": "#F7F7F7",
    "logoUrl": "https://cdn.example.com/logos/starbucks.png"
  },
  "coupons": [...],
  "menuItems": [...],
  "newsItems": [...]
}
```

#### 2. Get Business Theme
```http
GET /api/marketing/{businessId}/theme
```

**Use Case:** Apply custom branding when user enters business location

**Response:**
```json
{
  "id": 1,
  "businessId": 1,
  "themeName": "Starbucks Green",
  "primaryColor": "#00704A",
  "secondaryColor": "#F7F7F7",
  "accentColor": "#D4AF37",
  "logoUrl": "https://cdn.example.com/logos/starbucks.png",
  "customCss": ".header { background: #00704A; }"
}
```

#### 3. Get Active Coupons
```http
GET /api/marketing/{businessId}/coupons
```

**Features:**
- Only returns active coupons
- Filters by validity dates
- Checks redemption limits

**Response:**
```json
[
  {
    "id": 1,
    "title": "Buy One Get One Free",
    "description": "On any grande drink",
    "code": "BOGO2024",
    "discountPercentage": 50,
    "validFrom": "2024-01-01T00:00:00Z",
    "validUntil": "2024-12-31T23:59:59Z",
    "maxRedemptions": 1000,
    "currentRedemptions": 234
  }
]
```

#### 4. Get Menu Items
```http
GET /api/marketing/{businessId}/menu?category=beverages
```

**Features:**
- Optional category filtering
- Only available items
- Sorted by category and order

**Response:**
```json
[
  {
    "id": 1,
    "name": "Caffe Latte",
    "description": "Espresso with steamed milk",
    "category": "beverages",
    "price": 4.95,
    "currency": "USD",
    "calories": 190,
    "allergens": "milk",
    "dietaryTags": "vegetarian",
    "imageUrl": "https://cdn.example.com/menu/latte.jpg"
  }
]
```

#### 5. Get Menu Categories
```http
GET /api/marketing/{businessId}/menu/categories
```

**Response:**
```json
["beverages", "food", "pastries", "merchandise"]
```

#### 6. Get News Items
```http
GET /api/marketing/{businessId}/news?limit=10
```

**Features:**
- Published items only
- Not expired
- Featured items first

**Response:**
```json
[
  {
    "id": 1,
    "title": "New Seasonal Menu",
    "content": "Try our new pumpkin spice offerings...",
    "publishedAt": "2024-09-01T00:00:00Z",
    "isFeatured": true,
    "imageUrl": "https://cdn.example.com/news/fall-menu.jpg"
  }
]
```

#### 7. Find Nearby Businesses
```http
GET /api/marketing/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000
```

**Features:**
- Location-based search
- Only subscribed businesses
- Distance calculation

---

### Feedback Endpoints

#### 1. Submit Feedback
```http
POST /api/feedback
Content-Type: application/json

{
  "businessId": 1,
  "userId": "device-abc123",
  "userName": "John Doe",
  "userEmail": "john@example.com",
  "feedbackType": "review",
  "subject": "Great coffee!",
  "message": "Best latte I've ever had. Will definitely come back!",
  "rating": 5,
  "isPublic": true,
  "latitude": 37.7749,
  "longitude": -122.4194
}
```

**Feedback Types:**
- `review` - General review
- `complaint` - Complaint or issue
- `suggestion` - Improvement suggestion
- `compliment` - Positive feedback

**Validation:**
- Business must exist and be subscribed
- Rating must be 1-5 if provided
- Feedback type must be valid

**Response:**
```json
{
  "id": 123,
  "businessId": 1,
  "feedbackType": "review",
  "subject": "Great coffee!",
  "rating": 5,
  "submittedAt": "2024-01-13T10:30:00Z",
  "isApproved": false
}
```

#### 2. Upload Image Attachment
```http
POST /api/feedback/{feedbackId}/attachments/image
Content-Type: multipart/form-data

file: <image file>
```

**Limits:**
- Max file size: 50MB
- Allowed types: JPEG, PNG, GIF, WebP

**Response:**
```json
{
  "id": 456,
  "feedbackId": 123,
  "attachmentType": "image",
  "fileName": "coffee-photo.jpg",
  "contentType": "image/jpeg",
  "storageUrl": "/uploads/feedback/123/images/guid_coffee-photo.jpg",
  "thumbnailUrl": "/uploads/feedback/123/images/thumb_guid_coffee-photo.jpg",
  "fileSizeBytes": 2048576,
  "uploadedAt": "2024-01-13T10:31:00Z"
}
```

#### 3. Upload Video Attachment
```http
POST /api/feedback/{feedbackId}/attachments/video
Content-Type: multipart/form-data

file: <video file>
```

**Limits:**
- Max file size: 50MB
- Allowed types: MP4, QuickTime, AVI

**Response:**
```json
{
  "id": 457,
  "feedbackId": 123,
  "attachmentType": "video",
  "fileName": "coffee-review.mp4",
  "contentType": "video/mp4",
  "storageUrl": "/uploads/feedback/123/videos/guid_coffee-review.mp4",
  "thumbnailUrl": "/uploads/feedback/123/videos/thumb_guid_coffee-review.jpg",
  "fileSizeBytes": 15728640,
  "durationSeconds": 30,
  "uploadedAt": "2024-01-13T10:32:00Z"
}
```

#### 4. Get Feedback
```http
GET /api/feedback/{id}
```

**Response:**
```json
{
  "id": 123,
  "business": {
    "id": 1,
    "name": "Starbucks Coffee"
  },
  "userId": "device-abc123",
  "userName": "John Doe",
  "feedbackType": "review",
  "subject": "Great coffee!",
  "message": "Best latte I've ever had...",
  "rating": 5,
  "submittedAt": "2024-01-13T10:30:00Z",
  "attachments": [...]
}
```

#### 5. Get Business Feedback
```http
GET /api/feedback/business/{businessId}?publicOnly=true&includeAttachments=false
```

**Query Parameters:**
- `publicOnly` - Only public, approved feedback (default: true)
- `includeAttachments` - Include attachment details (default: false)

#### 6. Get Feedback Statistics
```http
GET /api/feedback/business/{businessId}/stats
```

**Response:**
```json
{
  "totalCount": 1247,
  "averageRating": 4.6,
  "ratingDistribution": {
    "5": 823,
    "4": 312,
    "3": 89,
    "2": 18,
    "1": 5
  },
  "typeDistribution": {
    "review": 1100,
    "compliment": 98,
    "suggestion": 42,
    "complaint": 7
  },
  "recentCount": 156
}
```

---

## Aspire Integration

### AppHost Registration

```csharp
// Add Marketing API with PostgreSQL dependency
var marketingDb = postgres.AddDatabase("marketing");

var marketingApi = builder.AddProject<Projects.FWH_MarketingApi>("marketingapi")
    .WithReference(marketingDb)
    .WithHttpEndpoint(port: 4750, name: "asp-http")
    .WithHttpsEndpoint(port: 4749, name: "asp-https")
    .WithExternalHttpEndpoints();
```

### Connection String

Managed automatically by Aspire:
```json
{
  "ConnectionStrings": {
    "marketing": "Host=localhost;Port=5432;Database=marketing;..."
  }
}
```

### Service Endpoints

**Desktop/iOS:**
- HTTPS: `https://localhost:4749`
- HTTP: `http://localhost:4750`

**Android Emulator:**
- HTTPS: Not recommended (certificate issues)
- HTTP: `http://10.0.2.2:4750`

**Android Physical Device:**
- Set environment variable: `MARKETING_API_BASE_URL=http://<YOUR_IP>:4750`

---

## Features

### 1. Business Subscription Model

Only subscribed businesses can:
- Have custom themes
- Offer coupons
- Display menus
- Post news
- Receive feedback

```csharp
// Check subscription
var business = await _context.Businesses
    .FirstOrDefaultAsync(b => b.Id == businessId && b.IsSubscribed);
```

### 2. Theme Customization

Businesses can customize:
- Colors (primary, secondary, accent, background, text)
- Logo and background images
- Custom CSS

**Use Case:** When user enters business location, mobile app applies custom theme

### 3. Coupon Management

Features:
- Validity period (start/end dates)
- Redemption limits
- Discount amounts or percentages
- Promo codes
- Terms and conditions

**Validation:**
```csharp
var now = DateTimeOffset.UtcNow;
var validCoupons = coupons
    .Where(c => c.IsActive 
        && c.ValidFrom <= now 
        && c.ValidUntil >= now
        && (c.MaxRedemptions == null || c.CurrentRedemptions < c.MaxRedemptions));
```

### 4. Menu System

Features:
- Categories (beverages, food, etc.)
- Pricing with currency
- Nutritional information
- Allergens and dietary tags
- Availability toggle
- Sort ordering
- Images

### 5. News System

Features:
- Publication scheduling
- Expiration dates
- Featured items
- Author attribution
- Rich content with images

### 6. Feedback Moderation

Workflow:
1. User submits feedback
2. `isApproved = false` by default
3. Business reviews and moderates
4. Business can respond
5. Approved feedback becomes public

### 7. Multimedia Attachments

**Image Support:**
- JPEG, PNG, GIF, WebP
- Automatic thumbnail generation (placeholder)
- File size tracking

**Video Support:**
- MP4, QuickTime, AVI
- Video thumbnails
- Duration tracking
- Large file handling

---

## Usage Examples

### Example 1: Apply Business Theme When User Arrives

```csharp
// Location tracking service detects user at business
var businessId = GetNearbyBusinessId(latitude, longitude);

// Fetch theme
var response = await httpClient.GetAsync($"/api/marketing/{businessId}/theme");
var theme = await response.Content.ReadFromJsonAsync<BusinessTheme>();

// Apply theme to app
if (theme != null)
{
    Application.Current.Resources["PrimaryColor"] = theme.PrimaryColor;
    Application.Current.Resources["SecondaryColor"] = theme.SecondaryColor;
    // Apply other theme properties...
}
```

### Example 2: Display Available Coupons

```csharp
// User views business profile
var coupons = await httpClient.GetFromJsonAsync<List<Coupon>>(
    $"/api/marketing/{businessId}/coupons");

// Display in UI
foreach (var coupon in coupons)
{
    ShowCoupon(coupon.Title, coupon.Description, coupon.Code);
}
```

### Example 3: Submit Review with Photos

```csharp
// User writes review
var feedback = new SubmitFeedbackRequest
{
    BusinessId = businessId,
    UserId = deviceId,
    FeedbackType = "review",
    Subject = "Great experience!",
    Message = "Loved the coffee and atmosphere",
    Rating = 5,
    IsPublic = true
};

var response = await httpClient.PostAsJsonAsync("/api/feedback", feedback);
var createdFeedback = await response.Content.ReadFromJsonAsync<Feedback>();

// Upload photos
foreach (var photo in selectedPhotos)
{
    var content = new MultipartFormDataContent();
    content.Add(new StreamContent(photo.Stream), "file", photo.FileName);
    
    await httpClient.PostAsync(
        $"/api/feedback/{createdFeedback.Id}/attachments/image",
        content);
}
```

### Example 4: Display Menu

```csharp
// Get menu categories
var categories = await httpClient.GetFromJsonAsync<List<string>>(
    $"/api/marketing/{businessId}/menu/categories");

// Get items for each category
foreach (var category in categories)
{
    var items = await httpClient.GetFromJsonAsync<List<MenuItem>>(
        $"/api/marketing/{businessId}/menu?category={category}");
    
    DisplayMenuSection(category, items);
}
```

---

## Security Considerations

### Input Validation

**Controller Level:**
```csharp
// Validate coordinates
if (latitude < -90 || latitude > 90)
{
    return BadRequest("Invalid latitude");
}

// Validate rating
if (rating.HasValue && (rating < 1 || rating > 5))
{
    return BadRequest("Rating must be 1-5");
}

// Validate feedback type
var validTypes = new[] { "review", "complaint", "suggestion", "compliment" };
if (!validTypes.Contains(feedbackType))
{
    return BadRequest("Invalid feedback type");
}
```

### File Upload Security

**Size Limits:**
```csharp
[RequestSizeLimit(50 * 1024 * 1024)] // 50MB
public async Task<ActionResult> UploadImage(IFormFile file)
{
    if (file.Length > MaxFileSize)
    {
        return BadRequest("File too large");
    }
}
```

**Content Type Validation:**
```csharp
private static readonly string[] AllowedImageTypes = 
    { "image/jpeg", "image/png", "image/gif", "image/webp" };

if (!AllowedImageTypes.Contains(file.ContentType))
{
    return BadRequest("Invalid file type");
}
```

### Moderation System

**Default Behavior:**
- All feedback starts as unapproved
- Only approved feedback is public
- Business can review before publication

**Business Response:**
- Businesses can respond to feedback
- Response timestamps tracked
- Reviewer attribution

---

## Database Migration

### Automatic Migration on Startup

```csharp
// In Program.cs
await ApplyDatabaseMigrationsAsync(app);
```

### Migration Service

Reads `.sql` files from `Migrations/` folder:
```
001_create_marketing_schema.sql
002_add_indexes.sql
003_add_feature_x.sql
```

Tracks applied migrations in `__migrations` table:
```sql
CREATE TABLE __migrations (
    id SERIAL PRIMARY KEY,
    migration_name VARCHAR(255) UNIQUE,
    applied_at TIMESTAMP WITH TIME ZONE
);
```

---

## Performance Considerations

### Indexes

**Location-based queries:**
```sql
CREATE INDEX idx_businesses_location ON businesses(latitude, longitude);
```

**Filtered queries:**
```sql
CREATE INDEX idx_coupons_business_active ON coupons(business_id, is_active);
CREATE INDEX idx_menu_items_business_category ON menu_items(business_id, category, is_available);
CREATE INDEX idx_news_items_business_published ON news_items(business_id, is_published, published_at);
```

### Query Optimization

**Include related data:**
```csharp
var business = await _context.Businesses
    .Include(b => b.Theme)
    .Include(b => b.Coupons.Where(c => c.IsActive))
    .Include(b => b.MenuItems.Where(m => m.IsAvailable))
    .FirstOrDefaultAsync(b => b.Id == businessId);
```

**Filter in database:**
```csharp
var coupons = await _context.Coupons
    .Where(c => c.ValidFrom <= now && c.ValidUntil >= now)
    .ToListAsync();
```

### Caching Recommendations

**Theme data:**
```csharp
// Cache for 1 hour (themes rarely change)
[ResponseCache(Duration = 3600)]
public async Task<ActionResult<BusinessTheme>> GetTheme(long businessId)
```

**Menu data:**
```csharp
// Cache for 15 minutes
[ResponseCache(Duration = 900)]
public async Task<ActionResult<List<MenuItem>>> GetMenu(long businessId)
```

---

## Testing

### Manual Testing with curl

**Get marketing data:**
```bash
curl https://localhost:4749/api/marketing/1
```

**Submit feedback:**
```bash
curl -X POST https://localhost:4749/api/feedback \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": 1,
    "userId": "test-user",
    "feedbackType": "review",
    "subject": "Test",
    "message": "Test message",
    "rating": 5
  }'
```

**Upload image:**
```bash
curl -X POST https://localhost:4749/api/feedback/1/attachments/image \
  -F "file=@photo.jpg"
```

### Sample Data

Create businesses and test data:
```sql
INSERT INTO businesses (name, address, latitude, longitude, is_subscribed)
VALUES ('Starbucks', '123 Main St', 37.7749, -122.4194, true);

INSERT INTO business_themes (business_id, theme_name, primary_color, secondary_color)
VALUES (1, 'Starbucks Green', '#00704A', '#F7F7F7');

INSERT INTO coupons (business_id, title, description, valid_from, valid_until, is_active)
VALUES (1, 'Free Coffee', 'Buy one get one free', NOW(), NOW() + INTERVAL '30 days', true);
```

---

## Future Enhancements

### Possible Improvements

1. **Cloud Storage Integration:**
   - AWS S3 or Azure Blob Storage for attachments
   - Automatic image resizing and optimization
   - CDN integration

2. **Real-time Updates:**
   - SignalR for live theme updates
   - Push notifications for new coupons
   - Real-time feedback moderation dashboard

3. **Analytics:**
   - Track coupon redemptions
   - Menu item popularity
   - Feedback sentiment analysis

4. **Advanced Theming:**
   - Multiple themes per business
   - Time-based themes (holiday specials)
   - A/B testing

5. **Loyalty Program:**
   - Points for feedback submission
   - Reward tiers
   - Exclusive coupons

6. **Business Dashboard:**
   - Admin UI for managing themes/coupons/menus
   - Feedback moderation interface
   - Analytics and reports

---

## Files Created

### Core Files ✅

1. ✅ `FWH.MarketingApi/FWH.MarketingApi.csproj` - Project file
2. ✅ `FWH.MarketingApi/Program.cs` - Application entry point
3. ✅ `FWH.MarketingApi/Models/BusinessModels.cs` - Business entity models
4. ✅ `FWH.MarketingApi/Models/FeedbackModels.cs` - Feedback entity models
5. ✅ `FWH.MarketingApi/Data/MarketingDbContext.cs` - EF Core context
6. ✅ `FWH.MarketingApi/Data/DatabaseMigrationService.cs` - SQL migration service
7. ✅ `FWH.MarketingApi/Migrations/001_create_marketing_schema.sql` - Database schema
8. ✅ `FWH.MarketingApi/Controllers/MarketingController.cs` - Marketing endpoints
9. ✅ `FWH.MarketingApi/Controllers/FeedbackController.cs` - Feedback endpoints
10. ✅ `FWH.MarketingApi/appsettings.json` - Configuration
11. ✅ `FWH.MarketingApi/appsettings.Development.json` - Dev configuration
12. ✅ `FWH.MarketingApi/Dockerfile` - Container image
13. ✅ `FWH.MarketingApi/README.md` - API documentation

### Modified Files ✅

14. ✅ `FWH.AppHost/Program.cs` - Added MarketingApi registration
15. ✅ `FWH.AppHost/FWH.AppHost.csproj` - Added project reference

### Documentation ✅

16. ✅ `Marketing_API_Implementation_Summary.md` - This document

---

## Summary

Successfully implemented a comprehensive Marketing API with the following features:

### ✅ Business Marketing
- Custom themes for location-based branding
- Coupon management with validity and redemptions
- Menu system with categories and nutritional info
- News and announcements system
- Nearby business discovery

### ✅ User Feedback
- Text reviews with ratings (1-5 stars)
- Multiple feedback types (review, complaint, suggestion, compliment)
- Image attachments (JPEG, PNG, GIF, WebP)
- Video attachments (MP4, QuickTime, AVI)
- Moderation system with business responses

### ✅ Infrastructure
- PostgreSQL database with migrations
- Aspire integration
- RESTful API design
- Input validation and security
- Performance optimizations
- Comprehensive logging

**Result:** Businesses can now advertise through the app, customize the user experience at their locations, and collect rich multimedia feedback from users! 🎉

---

**Implementation Status:** ✅ **COMPLETE**  
**Build Status:** ✅ **SUCCESSFUL**  
**Ready for Use:** ✅ **YES**

---

*Document Version: 1.0*  
*Author: GitHub Copilot*  
*Date: 2026-01-13*  
*Status: Complete*
