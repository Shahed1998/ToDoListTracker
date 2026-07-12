using System.ComponentModel.DataAnnotations;

namespace ToDoListTracker.Models;

public enum CategoryType
{
    Positive,
    Negative
}

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public CategoryType Type { get; set; } = CategoryType.Positive;

    // Hex color used for badges/bars in the UI, e.g. #4f46e5
    [StringLength(7)]
    public string ColorHex { get; set; } = "#4f46e5";

    public ICollection<TimeBoxEntry> Entries { get; set; } = new List<TimeBoxEntry>();
}
