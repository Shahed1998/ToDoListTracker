using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoListTracker.Data;
using ToDoListTracker.Models;
using ToDoListTracker.ViewModels;

namespace ToDoListTracker.Controllers;

public class ReportsController : Controller
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Monthly(int? year, int? month)
    {
        var today = DateTime.Today;
        int y = year ?? today.Year;
        int m = month ?? today.Month;

        var firstDay = new DateTime(y, m, 1);
        var daysInMonth = DateTime.DaysInMonth(y, m);
        var lastDay = new DateTime(y, m, daysInMonth);

        var entries = await _context.TimeBoxEntries
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.SubEntries)
            .Where(e => e.Date >= firstDay && e.Date <= lastDay)
            .ToListAsync();

        var vm = new MonthlyReportViewModel { Year = y, Month = m };

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(y, m, d);
            var dayEntries = entries.Where(e => e.Date.Date == date).ToList();

            var positive = dayEntries.Where(e => e.Category!.Type == CategoryType.Positive).ToList();
            var negative = dayEntries.Where(e => e.Category!.Type == CategoryType.Negative).ToList();

            double planned = positive.Sum(e => e.PlannedHours);
            double actual = positive.Sum(e => e.EffectiveActualHours);
            double penalty = negative.Sum(e => e.ActualHours);
            double netPct = planned <= 0 ? 0 : Math.Max(0, Math.Round(((actual - penalty) / planned) * 100, 1));

            vm.Days.Add(new DailyStat
            {
                Date = date,
                PlannedHours = planned,
                ActualHours = actual,
                PenaltyHours = penalty,
                HasEntries = dayEntries.Any(),
                NetPercentage = netPct,
                MeetsThreshold = planned > 0 && netPct >= MonthlyReportViewModel.ConsistencyThreshold
            });
        }

        // Streaks: consecutive days (up to today, or end of month if in the past) meeting the threshold.
        int current = 0, best = 0, running = 0;
        var cutoff = (y == today.Year && m == today.Month) ? today : lastDay;
        foreach (var day in vm.Days.Where(d => d.Date <= cutoff))
        {
            if (day.MeetsThreshold)
            {
                running++;
                best = Math.Max(best, running);
            }
            else
            {
                running = 0;
            }
        }
        // Current streak = trailing run ending at the cutoff day.
        foreach (var day in vm.Days.Where(d => d.Date <= cutoff).Reverse())
        {
            if (day.MeetsThreshold) current++;
            else break;
        }
        vm.CurrentStreak = current;
        vm.BestStreak = best;

        vm.CategoryBreakdown = entries
            .GroupBy(e => e.CategoryId)
            .Select(g =>
            {
                var category = g.First().Category!;
                return new CategoryBreakdownItem
                {
                    Name = category.Name,
                    ColorHex = category.ColorHex,
                    IsNegative = category.Type == CategoryType.Negative,
                    TotalHours = Math.Round(g.Sum(e => e.EffectiveActualHours), 2),
                    EntryCount = g.Count()
                };
            })
            .OrderByDescending(c => c.TotalHours)
            .ToList();

        return View(vm);
    }
}
