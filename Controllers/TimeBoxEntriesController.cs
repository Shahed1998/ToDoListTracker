using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoListTracker.Data;
using ToDoListTracker.Models;
using ToDoListTracker.ViewModels;

namespace ToDoListTracker.Controllers;

public class TimeBoxEntriesController : Controller
{
    private readonly AppDbContext _context;

    public TimeBoxEntriesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: TimeBoxEntries?date=2026-07-12
    public async Task<IActionResult> Index(DateTime? date)
    {
        var selectedDate = (date ?? DateTime.Today).Date;

        var entries = await _context.TimeBoxEntries
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.SubEntries)
            .Where(e => e.Date == selectedDate)
            .OrderBy(e => e.PlannedStart)
            .ToListAsync();

        var positive = entries.Where(e => e.Category!.Type == CategoryType.Positive).ToList();
        var negative = entries.Where(e => e.Category!.Type == CategoryType.Negative).ToList();

        double plannedTotal = positive.Sum(e => e.PlannedHours);
        double actualTotal = positive.Sum(e => e.EffectiveActualHours);
        double penaltyTotal = negative.Sum(e => e.ActualHours);

        double netPct = plannedTotal <= 0 ? 0 : Math.Max(0, Math.Round(((actualTotal - penaltyTotal) / plannedTotal) * 100, 1));

        ViewBag.SelectedDate = selectedDate;
        ViewBag.PlannedTotal = plannedTotal;
        ViewBag.ActualTotal = actualTotal;
        ViewBag.PenaltyTotal = penaltyTotal;
        ViewBag.NetPct = netPct;
        ViewBag.CelebrateToday = netPct >= 100 && plannedTotal > 0;

        return View(entries);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var entry = await _context.TimeBoxEntries
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.SubEntries)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (entry == null) return NotFound();
        return View(entry);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new TimeBoxEntryFormViewModel();
        await PopulateCategories(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TimeBoxEntryFormViewModel vm)
    {
        var category = await _context.Categories.FindAsync(vm.CategoryId);
        if (category == null)
        {
            ModelState.AddModelError(nameof(vm.CategoryId), "Pick a valid category.");
        }

        var entry = new TimeBoxEntry
        {
            Date = vm.Date,
            TaskName = vm.TaskName,
            CategoryId = vm.CategoryId,
            Notes = vm.Notes
        };

        if (category?.Type == CategoryType.Negative)
        {
            // Negative/penalty entries: no planned slot, just hours spent logged as a self-penalty.
            entry.PlannedStart = null;
            entry.PlannedEnd = null;
            entry.PlannedHours = vm.NegativeHours;
            entry.ActualHours = vm.NegativeHours;
            entry.Status = EntryStatus.Completed;
        }
        else
        {
            var start = vm.GetStartTimeSpan();
            var end = vm.GetEndTimeSpan();
            if (end <= start)
            {
                ModelState.AddModelError(nameof(vm.EndHour), "Planned end must be after planned start.");
            }
            entry.PlannedStart = start;
            entry.PlannedEnd = end;
            entry.PlannedHours = Math.Round((end - start).TotalHours, 2);
            entry.ActualHours = vm.ActualHours;
            entry.Status = vm.Status;
        }

        if (ModelState.IsValid)
        {
            _context.Add(entry);
            await _context.SaveChangesAsync();
            TempData["Toast"] = $"Entry \"{entry.TaskName}\" saved";
            return RedirectToAction(nameof(Index), new { date = entry.Date.ToString("yyyy-MM-dd") });
        }

        await PopulateCategories(vm);
        return View(vm);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var entry = await _context.TimeBoxEntries.AsNoTracking().Include(e => e.Category).FirstOrDefaultAsync(e => e.Id == id);
        if (entry == null) return NotFound();

        var vm = new TimeBoxEntryFormViewModel
        {
            Id = entry.Id,
            Date = entry.Date,
            TaskName = entry.TaskName,
            CategoryId = entry.CategoryId,
            Notes = entry.Notes,
            Status = entry.Status,
            ActualHours = entry.ActualHours
        };

        if (entry.Category?.Type == CategoryType.Negative)
        {
            vm.NegativeHours = entry.ActualHours;
        }
        else if (entry.PlannedStart.HasValue && entry.PlannedEnd.HasValue)
        {
            var (sh, sm, sap) = TimeBoxEntryFormViewModel.ToTwelveHour(entry.PlannedStart.Value);
            var (eh, em, eap) = TimeBoxEntryFormViewModel.ToTwelveHour(entry.PlannedEnd.Value);
            vm.StartHour = sh; vm.StartMinute = sm; vm.StartAmPm = sap;
            vm.EndHour = eh; vm.EndMinute = em; vm.EndAmPm = eap;
        }

        await PopulateCategories(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TimeBoxEntryFormViewModel vm)
    {
        if (id != vm.Id) return NotFound();

        var entry = await _context.TimeBoxEntries.FindAsync(id);
        if (entry == null) return NotFound();

        var category = await _context.Categories.FindAsync(vm.CategoryId);
        if (category == null)
        {
            ModelState.AddModelError(nameof(vm.CategoryId), "Pick a valid category.");
        }

        entry.Date = vm.Date;
        entry.TaskName = vm.TaskName;
        entry.CategoryId = vm.CategoryId;
        entry.Notes = vm.Notes;

        if (category?.Type == CategoryType.Negative)
        {
            entry.PlannedStart = null;
            entry.PlannedEnd = null;
            entry.PlannedHours = vm.NegativeHours;
            entry.ActualHours = vm.NegativeHours;
            entry.Status = EntryStatus.Completed;
        }
        else
        {
            var start = vm.GetStartTimeSpan();
            var end = vm.GetEndTimeSpan();
            if (end <= start)
            {
                ModelState.AddModelError(nameof(vm.EndHour), "Planned end must be after planned start.");
            }
            entry.PlannedStart = start;
            entry.PlannedEnd = end;
            entry.PlannedHours = Math.Round((end - start).TotalHours, 2);
            entry.ActualHours = vm.ActualHours;
            entry.Status = vm.Status;
        }

        if (ModelState.IsValid)
        {
            await _context.SaveChangesAsync();
            TempData["Toast"] = $"Entry \"{entry.TaskName}\" updated";
            return RedirectToAction(nameof(Index), new { date = entry.Date.ToString("yyyy-MM-dd") });
        }

        await PopulateCategories(vm);
        return View(vm);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var entry = await _context.TimeBoxEntries.AsNoTracking().Include(e => e.Category).FirstOrDefaultAsync(e => e.Id == id);
        if (entry == null) return NotFound();
        return View(entry);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var entry = await _context.TimeBoxEntries.FindAsync(id);
        var date = entry?.Date ?? DateTime.Today;
        if (entry != null)
        {
            _context.TimeBoxEntries.Remove(entry);
            await _context.SaveChangesAsync();
            TempData["Toast"] = $"Entry \"{entry.TaskName}\" deleted";
        }
        return RedirectToAction(nameof(Index), new { date = date.ToString("yyyy-MM-dd") });
    }

    private async Task PopulateCategories(TimeBoxEntryFormViewModel vm)
    {
        vm.CategoryLookup = await _context.Categories.AsNoTracking().OrderBy(c => c.Type).ThenBy(c => c.Name).ToListAsync();
    }
}
