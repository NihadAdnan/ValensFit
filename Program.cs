using ValensFit.Services;
using ValensFit.Services.Exercise;
using ValensFit.Services.Grocery;
using ValensFit.Services.Nutrition;

// Prevent Linux inotify limit crashes in container/cloud environments (Render, Railway, Fly.io, Azure, Docker)
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");

var builder = WebApplication.CreateBuilder(args);

// Configure configuration sources with reloadOnChange=false to prevent inotify instance exhaustion in Linux containers
builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

// Add services to the container.
var mvcBuilder = builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

#if DEBUG
mvcBuilder.AddRazorRuntimeCompilation();
#endif

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Nutrition & Engine Services
builder.Services.AddSingleton<FoodDatabase>();
builder.Services.AddSingleton<BmrCalculator>();
builder.Services.AddSingleton<TdeeCalculator>();
builder.Services.AddSingleton<MacroCalculator>();
builder.Services.AddSingleton<MealBuilderService>();
builder.Services.AddSingleton<MealSwapService>();
builder.Services.AddSingleton<ExercisePlanService>();
builder.Services.AddSingleton<GroceryPricingService>();
builder.Services.AddSingleton<DailyCalorieCalculatorService>();

// HTTP Client for Ollama
builder.Services.AddHttpClient<OllamaClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Plan Orchestrator
builder.Services.AddScoped<PlanCalculatorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
