using System.ComponentModel.DataAnnotations;

namespace ToDoListTracker.Models;

public class SubEntry
{
    public int Id { get; set; }

    [Required]
    public int TimeBoxEntryId { get; set; }
    public TimeBoxEntry? TimeBoxEntry { get; set; }

    [Required(ErrorMessage = "Task name is required")]
    [StringLength(150)]
    [Display(Name = "Task")]
    public string TaskName { get; set; } = string.Empty;

    [Range(0, 24)]
    [Display(Name = "Hours")]
    public double ActualHours { get; set; }

    [Display(Name = "Completed")]
    public bool IsCompleted { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}
