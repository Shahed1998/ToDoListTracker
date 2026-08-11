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

    // Hex color used for badges/bars in the UI, e.g. #4f46e5. Rendered unescaped into inline
    // style attributes in views, so this must stay strictly validated server-side (not just
    // relying on the <input type="color"> picker, which a client can bypass).
    [StringLength(7)]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a hex value like #4f46e5.")]
    public string ColorHex { get; set; } = "#4f46e5";

    public ICollection<TimeBoxEntry> Entries { get; set; } = new List<TimeBoxEntry>();
}
