# TimeBox Tracker

An ASP.NET Core MVC app for tracking your timeboxed tasks — planned hours vs. actual hours completed, per day.

## Stack
- ASP.NET Core MVC (.NET 8)
- EF Core + SQL Server (auto-creates the database on first run via `EnsureCreated()`, no migrations needed)

## Run it

By default `appsettings.json` points at LocalDB (`(localdb)\MSSQLLocalDB`), which ships with Visual Studio / SQL Server Express tooling on Windows. If you're using a different SQL Server instance (Docker, a named instance, Azure SQL, etc.), update the `DefaultConnection` string in `appsettings.json` first, e.g.:

```json
"DefaultConnection": "Server=localhost,1433;Database=ToDoListTrackerDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

Then:

```bash
cd ToDoListTracker
dotnet restore
dotnet run
```

Then open the printed `https://localhost:xxxx` URL. It lands on the daily tracker for today.

> **Note:** if you have `todotracker.db`, `todotracker.db-shm`, or `todotracker.db-wal` files lying around in the project folder, those are leftovers from an earlier SQLite-based version of this project. They're unused now that the app runs on SQL Server — safe to delete.

## ⚠️ Schema changes (read this if you've run the app before)

`EnsureCreated()` only builds the database schema the *first* time — it will **not** add new tables or columns to a database that already exists. This update added an `IsCompleted` column to `SubEntries` (on top of the `SubEntries` table itself from the previous update). If you've already run this app before, do one of the following:

- **Easiest**: drop the existing database (`ToDoListTrackerDb`) in SQL Server Management Studio / Azure Data Studio, then run the app again — it'll be recreated with the current schema (you'll lose existing data).
- **Keep your data**: switch to EF Core migrations (see below) and run `dotnet ef migrations add <Name>` + `dotnet ef database update` after each schema change instead of dropping the DB.

## What it does

- **Daily view** (`/`): entries for a selected date, with a planned/achieved/penalty/net-completion summary and date navigation. If you hit 100% net completion for the day, confetti and balloons celebrate for a few seconds.
- **Categories**: manage your own categories (Study, Work, Personal Time, SCEcommerz, etc. come pre-seeded). Each category is either:
  - **Positive** — has a planned time slot and counts toward your daily completion %.
  - **Negative** — for self-penalty logging (e.g. Procrastination, Oversleeping — pre-seeded, but you can rename/add your own). These have no planned slot, just "hours spent," and subtract directly from your day's net score.
- **12-hour time picker**: planned start/end times use hour (1–12) + minute + AM/PM dropdowns instead of a 24-hour clock, and every displayed time (Index, Details, Delete) shows as e.g. `1:00 PM`.
- **Sub-tasks**: a planned block (e.g. `9:30 AM – 6:30 PM`) can be broken into multiple sub-tasks from its Details page — each with its own name, hours, and a completed checkbox. Only *checked* sub-tasks count toward the block's "Actual Hours" and completion % — so if you plan 3 prayers within a block and only complete 2, the block correctly shows less than 100%.
  - Consistency calendar (heatmap): green = day met your completion threshold (default 80%), yellow = partial, red = 0%/over-penalized, grey = no entries. Click any day to jump to its daily view.
  - Current streak and best streak of "good days" in a row.
  - Average completion %, days logged, days at/above threshold.
  - Time-by-category bar breakdown (positive hours vs. negative/penalty hours).

## How the score is calculated

For a given day:
```
Net % = max(0, (Actual Hours − Penalty Hours) / Planned Hours × 100)
```
Only **Positive** category entries count toward Planned/Actual hours. **Negative** category entries contribute to Penalty Hours. A day with no positive entries logged shows 0%.

## Extending it

- Want to change the "good day" threshold (currently 80%)? Edit `MonthlyReportViewModel.ConsistencyThreshold`.
- Want proper migrations instead of `EnsureCreated()`? Run:
  ```bash
  dotnet ef migrations add Initial
  dotnet ef database update
  ```
  then remove the `EnsureCreated()` call in `Program.cs`. Note: category seed data is defined via `HasData()` in `AppDbContext`, so it'll come through in the first migration too.
- Want auth (multi-device sync)? This is a natural next step to bolt on JWT auth, same pattern as your DatasoftAML work.
