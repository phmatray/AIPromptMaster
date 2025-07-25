using System.ComponentModel.DataAnnotations;

namespace AIPromptManager.Models;

public class Tag
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tag name is required")]
    [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters")]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for many-to-many relationship
    public virtual ICollection<Prompt> Prompts { get; set; } = new List<Prompt>();
}