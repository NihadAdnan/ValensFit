using ValensFit.Services;
using ValensFit.Services.Exercise;
using ValensFit.Services.Grocery;
using ValensFit.Services.Nutrition;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

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
    pattern: "{controller=Plan}/{action=Index}/{id?}");

app.Run();
