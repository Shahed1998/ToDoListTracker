using Microsoft.EntityFrameworkCore;
using ToDoListTracker.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Auto-create the DB on first run (schema + seeded categories) so you don't need to run migrations manually.
// Retries with a short backoff: in Docker Compose the web container can start slightly before Postgres
// finishes accepting connections, even with a healthcheck in place.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    const int maxAttempts = 8;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            db.Database.EnsureCreated();
            break;
        }
        catch (Exception) when (attempt < maxAttempts)
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TimeBoxEntries}/{action=Index}/{id?}");

app.Run();
