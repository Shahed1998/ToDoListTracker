using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ToDoListTracker.Data;
using ToDoListTracker.Models;

namespace ToDoListTracker.Controllers;

public class SubEntriesController : Controller
{
    private readonly AppDbContext _context;

    public SubEntriesController(AppDbContext context)
    {
        _context = context;
    }

    // POST: SubEntries/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int timeBoxEntryId,
        [Required(ErrorMessage = "Task name is required"), StringLength(150)] string taskName,
        [Range(0, 24)] double actualHours,
        bool isCompleted,
        [StringLength(300)] string? notes)
    {
        var parent = await _context.TimeBoxEntries.FindAsync(timeBoxEntryId);
        if (parent == null) return NotFound();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Couldn't add sub-task: " + string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return RedirectToAction("Details", "TimeBoxEntries", new { id = timeBoxEntryId });
        }

        _context.SubEntries.Add(new SubEntry
        {
            TimeBoxEntryId = timeBoxEntryId,
            TaskName = taskName.Trim(),
            ActualHours = actualHours,
            IsCompleted = isCompleted,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        });
        await _context.SaveChangesAsync();
        TempData["Toast"] = $"Sub-task \"{taskName.Trim()}\" added";

        return RedirectToAction("Details", "TimeBoxEntries", new { id = timeBoxEntryId });
    }

    // POST: SubEntries/Toggle/5 - flips the completed checkbox for a sub-task
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var subEntry = await _context.SubEntries.FindAsync(id);
        if (subEntry == null) return NotFound();

        subEntry.IsCompleted = !subEntry.IsCompleted;
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "TimeBoxEntries", new { id = subEntry.TimeBoxEntryId });
    }

    // POST: SubEntries/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var subEntry = await _context.SubEntries.FindAsync(id);
        if (subEntry == null) return NotFound();

        int parentId = subEntry.TimeBoxEntryId;
        _context.SubEntries.Remove(subEntry);
        await _context.SaveChangesAsync();
        TempData["Toast"] = $"Sub-task \"{subEntry.TaskName}\" deleted";

        return RedirectToAction("Details", "TimeBoxEntries", new { id = parentId });
    }
}
