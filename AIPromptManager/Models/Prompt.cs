using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AIPromptManager.Models;

public class Prompt
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    // Foreign key to User - nullable to preserve existing data
    public string? UserId { get; set; }

    // Navigation property to User
    public virtual IdentityUser? User { get; set; }

    // Navigation property for many-to-many relationship
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Concurrency token for optimistic concurrency control
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}