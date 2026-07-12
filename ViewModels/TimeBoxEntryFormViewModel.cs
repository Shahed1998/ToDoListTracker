using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ToDoListTracker.Models;

namespace ToDoListTracker.ViewModels;

public class TimeBoxEntryFormViewModel
{
    public int Id { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Task name is required")]
    [StringLength(150)]
    [Display(Name = "Task")]
    public string TaskName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Pick a category")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    // Planned start: 12-hour picker (only used for Positive categories)
    [Range(1, 12)]
    public int StartHour { get; set; } = 9;
    [Range(0, 59)]
    public int StartMinute { get; set; } = 0;
    public string StartAmPm { get; set; } = "AM";

    // Planned end: 12-hour picker
    [Range(1, 12)]
    public int EndHour { get; set; } = 10;
    [Range(0, 59)]
    public int EndMinute { get; set; } = 0;
    public string EndAmPm { get; set; } = "AM";

    // Used directly as "hours spent" for Negative categories (no planned slot needed)
    [Range(0, 24)]
    [Display(Name = "Hours Spent")]
    public double NegativeHours { get; set; }

    [Range(0, 24)]
    [Display(Name = "Actual Hours Completed")]
    public double ActualHours { get; set; }

    [Display(Name = "Status")]
    public EntryStatus Status { get; set; } = EntryStatus.NotStarted;

    [StringLength(500)]
    public string? Notes { get; set; }

    public List<SelectListItem> Categories { get; set; } = new();
    public List<Category> CategoryLookup { get; set; } = new(); // used by JS to know each category's Type/color

    public TimeSpan GetStartTimeSpan()
    {
        int h24 = StartAmPm == "PM" && StartHour != 12 ? StartHour + 12 : (StartAmPm == "AM" && StartHour == 12 ? 0 : StartHour);
        return new TimeSpan(h24, StartMinute, 0);
    }

    public TimeSpan GetEndTimeSpan()
    {
        int h24 = EndAmPm == "PM" && EndHour != 12 ? EndHour + 12 : (EndAmPm == "AM" && EndHour == 12 ? 0 : EndHour);
        return new TimeSpan(h24, EndMinute, 0);
    }

    public static (int hour12, int minute, string ampm) ToTwelveHour(TimeSpan t)
    {
        int hour24 = t.Hours;
        string ampm = hour24 >= 12 ? "PM" : "AM";
        int hour12 = hour24 % 12;
        if (hour12 == 0) hour12 = 12;
        return (hour12, t.Minutes, ampm);
    }
}
