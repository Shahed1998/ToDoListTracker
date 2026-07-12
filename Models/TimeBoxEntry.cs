using System.ComponentModel.DataAnnotations;

namespace ToDoListTracker.Models;

public enum EntryStatus
{
    NotStarted,
    InProgress,
    Completed,
    Partial,
    Missed
}

public class TimeBoxEntry
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Date")]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Task name is required")]
    [StringLength(150)]
    [Display(Name = "Task")]
    public string TaskName { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Null for negative/penalty entries that don't have a planned slot - just a duration.
    [DataType(DataType.Time)]
    [Display(Name = "Planned Start")]
    public TimeSpan? PlannedStart { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Planned End")]
    public TimeSpan? PlannedEnd { get; set; }

    // Derived from PlannedStart/PlannedEnd for positive entries; entered directly (as time spent) for negative entries.
    [Display(Name = "Planned Hours")]
    [Range(0, 24)]
    public double PlannedHours { get; set; }

    // Manually entered actual hours - used only when there are no sub-entries logged for this block.
    [Range(0, 24)]
    [Display(Name = "Actual Hours")]
    public double ActualHours { get; set; }

    [Display(Name = "Status")]
    public EntryStatus Status { get; set; } = EntryStatus.NotStarted;

    [StringLength(500)]
    public string? Notes { get; set; }

    // Individual tasks done within this block (e.g. a "8am-10pm" work block might have several).
    public ICollection<SubEntry> SubEntries { get; set; } = new List<SubEntry>();

    // If sub-entries exist, actual hours is the sum of *completed* sub-entries' hours - an unchecked
    // sub-task (e.g. a prayer you skipped) doesn't count toward what you actually got done.
    public double EffectiveActualHours => SubEntries.Any()
        ? Math.Round(SubEntries.Where(s => s.IsCompleted).Sum(s => s.ActualHours), 2)
        : ActualHours;

    public int CompletedSubEntryCount => SubEntries.Count(s => s.IsCompleted);
    public int TotalSubEntryCount => SubEntries.Count;

    public double CompletionPercentage =>
        PlannedHours <= 0 ? 0 : Math.Min(100, Math.Round((EffectiveActualHours / PlannedHours) * 100, 1));
}
