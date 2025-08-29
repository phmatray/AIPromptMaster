using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AIPromptManager.Models;

namespace AIPromptManager.Data;

public class PromptManagerContext(
    DbContextOptions<PromptManagerContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Prompt> Prompts { get; set; }
    public DbSet<Tag> Tags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Prompt entity
        modelBuilder.Entity<Prompt>(entity =>
        {
            entity.HasKey(p => p.Id);
                
            entity.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(p => p.Description)
                .HasMaxLength(500);
                
            entity.Property(p => p.Content)
                .IsRequired();
                
            entity.Property(p => p.CreatedAt)
                .IsRequired();
                
            entity.Property(p => p.UpdatedAt)
                .IsRequired();

            // Configure foreign key relationship with User
            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Performance optimization: Add indexes for common queries
            entity.HasIndex(p => p.UpdatedAt)
                .HasDatabaseName("IX_Prompts_UpdatedAt");
                
            entity.HasIndex(p => p.CreatedAt)
                .HasDatabaseName("IX_Prompts_CreatedAt");
                
            entity.HasIndex(p => p.Title)
                .HasDatabaseName("IX_Prompts_Title");

            entity.HasIndex(p => p.UserId)
                .HasDatabaseName("IX_Prompts_UserId");

            // Configure many-to-many relationship with Tags
            entity.HasMany(p => p.Tags)
                .WithMany(t => t.Prompts)
                .UsingEntity<Dictionary<string, object>>(
                    "PromptTags",
                    j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Prompt>().WithMany().HasForeignKey("PromptId").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("PromptId", "TagId");
                        j.ToTable("PromptTags");
                        // Add indexes for junction table performance
                        j.HasIndex("PromptId").HasDatabaseName("IX_PromptTags_PromptId");
                        j.HasIndex("TagId").HasDatabaseName("IX_PromptTags_TagId");
                    });
        });

        // Configure Tag entity
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(t => t.Id);
                
            entity.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(t => t.CreatedAt)
                .IsRequired();

            // Create unique index on Name to prevent duplicate tags
            entity.HasIndex(t => t.Name)
                .IsUnique();
        });
    }
}