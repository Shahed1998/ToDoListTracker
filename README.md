[![Build](https://github.com/Shahed1998/ToDoListTracker/actions/workflows/build.yml/badge.svg)](https://github.com/Shahed1998/ToDoListTracker/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

# TimeBox Tracker

An ASP.NET Core MVC app for tracking your timeboxed tasks — planned hours vs. actual hours completed, per day.

## Stack
- ASP.NET Core MVC (.NET 10)
- EF Core + PostgreSQL (auto-creates the database on first run via `EnsureCreated()`, no migrations needed)
- Docker + Docker Compose for one-command setup

## Run it

### Option A — Docker Compose (recommended)

This is the easiest path and matches how you'd actually run it day to day — brings up the app *and* a Postgres database together, with data persisted in a named volume across restarts.

```bash
cd ToDoListTracker
cp .env.example .env      # optional: edit the password inside first
docker compose up -d --build
```

Open `http://localhost:8080`. It lands on the daily tracker for today. Seeded categories and schema are created automatically on first boot.

To stop it: `docker compose down` (add `-v` to also wipe the database volume).

### Option B — native `dotnet run`

Requires your own local PostgreSQL server (e.g. `winget install PostgreSQL.PostgreSQL`, or Postgres.app on macOS). Update `DefaultConnection` in `appsettings.json` to match your local instance — the default assumes a `postgres`/`postgres` login on `localhost:5432`:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=ToDoListTrackerDb;Username=postgres;Password=postgres"
```

Then:

```bash
cd ToDoListTracker
dotnet restore
dotnet run
```

### Option C — publish to IIS (Windows)

The project already ships with `web.config` wired for the in-process ASP.NET Core Module, and a working file-system publish profile (`Properties/PublishProfiles/FolderProfile.pubxml`). To deploy:

1. Install the [.NET 10 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0) on the Windows server — this registers `aspNetCoreModuleV2`, which is the usual cause of a fresh-box 500 error if it's missing.
2. `dotnet publish -c Release -o <publish-folder>` (or, in Visual Studio: right-click the project → **Publish** → the `FolderProfile`).
3. Point an IIS site at `<publish-folder>`, with the app pool's **.NET CLR Version** set to **No Managed Code** (the app is self-hosted in-process; IIS just proxies to it).
4. Update the deployed `appsettings.json`'s `DefaultConnection` to point at a PostgreSQL server the IIS box can reach.

## ⚠️ Schema changes (read this if you've run the app before)

`EnsureCreated()` only builds the database schema the *first* time — it will **not** add new tables or columns to a database that already exists. This update added an `IsCompleted` column to `SubEntries` (on top of the `SubEntries` table itself from the previous update). If you've already run this app before, do one of the following:

- **Easiest**: drop the existing database, then run the app again — it'll be recreated with the current schema (you'll lose existing data). With Docker Compose: `docker compose down -v` then `docker compose up -d` again.
- **Keep your data**: switch to EF Core migrations (see below) and run `dotnet ef migrations add <Name>` + `dotnet ef database update` after each schema change instead of dropping the DB.

## Deploying so you can use it every day

Once this becomes your actual daily-entry tool rather than something you spin up occasionally, you want it running somewhere always-on:

- **Option A — an always-on machine you already have** (a spare PC, NAS, or Raspberry Pi on your home network). `docker compose up -d` (the `restart: unless-stopped` policy is already set in `docker-compose.yml`, so it survives reboots). Reach it at `http://<machine-ip>:8080` from your phone/laptop over wifi.
- **Option B — a small cloud VPS** (e.g. a ~$5/mo Hetzner, DigitalOcean, or Lightsail box) if you want access from anywhere, not just your home network. Install Docker, `git clone` this repo, `docker compose up -d`, and open port 8080 in the firewall. If you want a real domain with automatic HTTPS instead of a bare IP:port, put a lightweight reverse proxy like [Caddy](https://caddyserver.com/) in front — a single extra `caddy` service in `docker-compose.yml` pointed at `web:8080` is all that's needed once you have a domain pointed at the box.
- **Back up your data**: the Postgres volume alone isn't a backup. Run this periodically (or cron it):
  ```bash
  docker compose exec db pg_dump -U todotracker todotrackerdb > backup.sql
  ```

## What it does

- **Daily view** (`/`): entries for a selected date, with a planned/achieved/penalty/net-completion summary (shown as a circular progress ring) and date navigation. If you hit 100% net completion for the day, confetti and balloons celebrate for a few seconds. Keyboard shortcuts: `←`/`→` to change day, `T` for today, `N` for a new entry.
- **Dark mode**: toggle via the sun/moon button in the navbar; your choice is remembered per-browser.
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
