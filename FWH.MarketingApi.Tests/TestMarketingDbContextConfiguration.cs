using FWH.MarketingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FWH.MarketingApi.Tests;

/// <summary>
/// Test-specific model configuration that maps all properties to lowercase column names
/// to match PostgreSQL's default behavior of converting unquoted identifiers to lowercase.
/// </summary>
public static class TestMarketingDbContextConfiguration
{
    public static void ConfigureForTests(ModelBuilder modelBuilder)
    {
        // Business configuration with explicit lowercase column names
        modelBuilder.Entity<Business>(entity =>
        {
            entity.ToTable("businesses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500).HasColumnName("address");
            entity.Property(e => e.PhoneNumber).HasMaxLength(50).HasColumnName("phone_number");
            entity.Property(e => e.Email).HasMaxLength(200).HasColumnName("email");
            entity.Property(e => e.Website).HasMaxLength(500).HasColumnName("website");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasColumnName("longitude");
            entity.Property(e => e.IsSubscribed).HasColumnName("is_subscribed");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'").HasColumnName("created_at");
            entity.Property(e => e.SubscriptionExpiresAt).HasColumnName("subscription_expires_at");
            
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
            entity.HasIndex(e => e.IsSubscribed);
        });

        // BusinessTheme configuration
        modelBuilder.Entity<BusinessTheme>(entity =>
        {
            entity.ToTable("business_themes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BusinessId).HasColumnName("business_id");
            entity.Property(e => e.ThemeName).IsRequired().HasMaxLength(100).HasColumnName("theme_name");
            entity.Property(e => e.PrimaryColor).HasMaxLength(20).HasColumnName("primary_color");
            entity.Property(e => e.SecondaryColor).HasMaxLength(20).HasColumnName("secondary_color");
            entity.Property(e => e.AccentColor).HasColumnName("accent_color");
            entity.Property(e => e.BackgroundColor).HasColumnName("background_color");
            entity.Property(e => e.TextColor).HasColumnName("text_color");
            entity.Property(e => e.LogoUrl).HasColumnName("logo_url");
            entity.Property(e => e.BackgroundImageUrl).HasColumnName("background_image_url");
            entity.Property(e => e.CustomCss).HasColumnName("custom_css");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            entity.HasOne(e => e.Business)
                .WithOne(b => b.Theme)
                .HasForeignKey<BusinessTheme>(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Coupon configuration
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.ToTable("coupons");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BusinessId).HasColumnName("business_id");
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200).HasColumnName("title");
            entity.Property(e => e.Description).IsRequired().HasColumnName("description");
            entity.Property(e => e.Code).HasMaxLength(50).HasColumnName("code");
            entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount");
            entity.Property(e => e.DiscountPercentage).HasColumnName("discount_percentage");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.TermsAndConditions).HasColumnName("terms_and_conditions");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ValidFrom).HasColumnName("valid_from");
            entity.Property(e => e.ValidUntil).HasColumnName("valid_until");
            entity.Property(e => e.CurrentRedemptions).HasColumnName("current_redemptions");
            entity.Property(e => e.MaxRedemptions).HasColumnName("max_redemptions");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'").HasColumnName("created_at");
            
            entity.HasOne(e => e.Business)
                .WithMany(b => b.Coupons)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.BusinessId, e.IsActive });
            entity.HasIndex(e => new { e.ValidFrom, e.ValidUntil });
        });

        // MenuItem configuration
        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("menu_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BusinessId).HasColumnName("business_id");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100).HasColumnName("category");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Currency).HasMaxLength(10).HasColumnName("currency");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.IsAvailable).HasColumnName("is_available");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.Calories).HasColumnName("calories");
            entity.Property(e => e.Allergens).HasColumnName("allergens");
            entity.Property(e => e.DietaryTags).HasColumnName("dietary_tags");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            entity.HasOne(e => e.Business)
                .WithMany(b => b.MenuItems)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.BusinessId, e.Category, e.IsAvailable });
        });

        // NewsItem configuration
        modelBuilder.Entity<NewsItem>(entity =>
        {
            entity.ToTable("news_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BusinessId).HasColumnName("business_id");
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200).HasColumnName("title");
            entity.Property(e => e.Content).IsRequired().HasColumnName("content");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.Author).HasMaxLength(100).HasColumnName("author");
            entity.Property(e => e.IsPublished).HasColumnName("is_published");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            entity.HasOne(e => e.Business)
                .WithMany(b => b.NewsItems)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.BusinessId, e.IsPublished, e.PublishedAt });
        });

        // Feedback configuration
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("feedback");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BusinessId).HasColumnName("business_id");
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(200).HasColumnName("user_id");
            entity.Property(e => e.UserName).HasMaxLength(200).HasColumnName("user_name");
            entity.Property(e => e.UserEmail).HasMaxLength(200).HasColumnName("user_email");
            entity.Property(e => e.FeedbackType).IsRequired().HasMaxLength(50).HasColumnName("feedback_type");
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200).HasColumnName("subject");
            entity.Property(e => e.Message).IsRequired().HasColumnName("message");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("now() at time zone 'utc'").HasColumnName("submitted_at");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.BusinessResponse).HasColumnName("business_response");
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at");
            entity.Property(e => e.IsPublic).HasColumnName("is_public");
            entity.Property(e => e.IsApproved).HasColumnName("is_approved");
            entity.Property(e => e.ModerationNotes).HasColumnName("moderation_notes");
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasColumnName("longitude");
            
            entity.HasOne(e => e.Business)
                .WithMany(b => b.Feedback)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.BusinessId, e.SubmittedAt });
            entity.HasIndex(e => new { e.UserId, e.SubmittedAt });
            entity.HasIndex(e => new { e.IsPublic, e.IsApproved });
        });

        // FeedbackAttachment configuration
        modelBuilder.Entity<FeedbackAttachment>(entity =>
        {
            entity.ToTable("feedback_attachments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
            entity.Property(e => e.AttachmentType).IsRequired().HasMaxLength(20).HasColumnName("attachment_type");
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500).HasColumnName("file_name");
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100).HasColumnName("content_type");
            entity.Property(e => e.StorageUrl).IsRequired().HasColumnName("storage_url");
            entity.Property(e => e.ThumbnailUrl).HasColumnName("thumbnail_url");
            entity.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(e => e.DurationSeconds).HasColumnName("duration_seconds");
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("now() at time zone 'utc'").HasColumnName("uploaded_at");
            
            entity.HasOne(e => e.Feedback)
                .WithMany(f => f.Attachments)
                .HasForeignKey(e => e.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
