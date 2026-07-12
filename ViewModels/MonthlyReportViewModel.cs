namespace ToDoListTracker.ViewModels;

public class DailyStat
{
    public DateTime Date { get; set; }
    public double PlannedHours { get; set; }
    public double ActualHours { get; set; }
    public double PenaltyHours { get; set; }
    public bool HasEntries { get; set; }
    public double NetPercentage { get; set; }
    public bool MeetsThreshold { get; set; } // netPct >= threshold, used for streaks
}

public class CategoryBreakdownItem
{
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#4f46e5";
    public bool IsNegative { get; set; }
    public double TotalHours { get; set; }
    public int EntryCount { get; set; }
}

public class MonthlyReportViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public const int ConsistencyThreshold = 80; // % completion considered "a good day"

    public List<DailyStat> Days { get; set; } = new();
    public List<CategoryBreakdownItem> CategoryBreakdown { get; set; } = new();

    public int DaysLoggedCount => Days.Count(d => d.HasEntries);
    public int DaysAtOrAboveThreshold => Days.Count(d => d.MeetsThreshold);
    public double AverageCompletion => Days.Any(d => d.HasEntries)
        ? Math.Round(Days.Where(d => d.HasEntries).Average(d => d.NetPercentage), 1)
        : 0;

    public double TotalPlannedHours => Days.Sum(d => d.PlannedHours);
    public double TotalActualHours => Days.Sum(d => d.ActualHours);
    public double TotalPenaltyHours => Days.Sum(d => d.PenaltyHours);

    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
}
