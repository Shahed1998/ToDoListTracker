# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

TimeBox Tracker — an ASP.NET Core MVC (.NET 10) app for tracking timeboxed tasks: planned hours vs. actual hours completed, per day, with a monthly consistency report.

## Commands

```bash
dotnet restore
dotnet run                 # runs the app; prints the https://localhost:xxxx URL
dotnet build
```

There are no automated tests in this repo (no test project in the `.slnx`).

### EF Core / database

The app currently uses `Database.EnsureCreated()` in `Program.cs`, **not** migrations — schema changes to `Models/` or `AppDbContext.OnModelCreating` won't apply to an existing database. When changing the schema, tell the user they need to either drop and recreate `ToDoListTrackerDb`, or switch the project to migrations:

```bash
dotnet ef migrations add <Name>
dotnet ef database update
```

(`dotnet-ef` is pinned via `dotnet-tools.json` as a local tool — restore with `dotnet tool restore` if it's not on PATH.)

The connection string lives in `appsettings.json` (`ConnectionStrings:DefaultConnection`) and currently points at a local SQL Server Express instance.

## Architecture

Standard ASP.NET Core MVC, no API/SPA layer — controllers return Razor views directly, forms post back to controller actions.

**Domain model** (`Models/`): `TimeBoxEntry` belongs to a `Category` (`CategoryType.Positive` or `.Negative`) and can have many `SubEntry` records (cascade-deleted with the parent). A `TimeBoxEntry` with no sub-entries uses its own `ActualHours`; if sub-entries exist, `EffectiveActualHours` sums only the ones with `IsCompleted == true` — this distinction matters everywhere hours are aggregated (controllers and views both use `EffectiveActualHours`, never raw `ActualHours`, once sub-entries are in play).

**Positive vs. Negative categories drive two different entry shapes**, handled by branching in `TimeBoxEntriesController.Create`/`Edit`:
- Positive: has a planned time slot (`PlannedStart`/`PlannedEnd`, entered via the 12-hour picker in `TimeBoxEntryFormViewModel` and converted with `GetStartTimeSpan`/`GetEndTimeSpan`/`ToTwelveHour`); `PlannedHours` is derived from the slot duration.
- Negative: no planned slot — `NegativeHours` on the form is copied directly into both `PlannedHours` and `ActualHours` as a self-penalty, status forced to `Completed`.

**Score calculation** (duplicated in `TimeBoxEntriesController.Index` for the daily view and `ReportsController.Monthly` for each day of the month — if you change the formula, update both places):
```
Net % = max(0, (sum of Positive EffectiveActualHours − sum of Negative ActualHours) / sum of Positive PlannedHours × 100)
```
Only Positive entries count toward Planned/Actual; Negative entries only subtract as penalty. No positive entries logged for a day → 0%.

**`ReportsController.Monthly`** builds a `MonthlyReportViewModel` by looping every day of the month and computing the same net-% formula, then derives streaks (`CurrentStreak`/`BestStreak`, consecutive days meeting `MonthlyReportViewModel.ConsistencyThreshold`, currently 80%) and a per-category hours breakdown. This is the heaviest controller — grep here first when working on the report/heatmap/streak features.

**Seed data**: starter categories are defined via `HasData()` in `AppDbContext.OnModelCreating`, not inserted at runtime — if using migrations (see above), seed changes need a new migration.

**View organization** mirrors controllers 1:1 (`Views/TimeBoxEntries/`, `Views/Categories/`, `Views/Reports/`), plus `Views/Shared/_Layout.cshtml`. There's no view for `SubEntriesController` — it's a pure POST-and-redirect controller (Create/Toggle/Delete all redirect back to `TimeBoxEntries/Details`), so sub-entry UI lives entirely inside `Views/TimeBoxEntries/Details.cshtml`.

`TimeSpanExtensions.ToClockString()` is the shared helper for rendering any `TimeSpan`/`TimeSpan?` as a 12-hour clock string (`h:mm tt`) — used across Index/Details/Delete views instead of ad hoc formatting.
