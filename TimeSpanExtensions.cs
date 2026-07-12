namespace ToDoListTracker;

public static class TimeSpanExtensions
{
    public static string ToClockString(this TimeSpan? t)
    {
        if (t == null) return "-";
        return DateTime.Today.Add(t.Value).ToString("h:mm tt");
    }

    public static string ToClockString(this TimeSpan t)
    {
        return DateTime.Today.Add(t).ToString("h:mm tt");
    }
}
