using Microsoft.EntityFrameworkCore;
using AIPromptManager.Models;

namespace AIPromptManager.Data
{
    public class PromptManagerContext : DbContext
    {
        public PromptManagerContext(DbContextOptions<PromptManagerContext> options) : base(options)
        {
        }

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
}