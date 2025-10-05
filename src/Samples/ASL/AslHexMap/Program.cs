using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AslHexMap.Data;
using AslHexMap.Services;
using AslHexMap.Core.Schema;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

// Register generic JSON loader services
builder.Services.AddSingleton<JsonFileLoader<BoardData>>();
builder.Services.AddSingleton<BoardFilePathResolver>(serviceProvider =>
{
    var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();
    return new BoardFilePathResolver(env.ContentRootPath);
});
builder.Services.AddSingleton<JsonBoardLoader>(serviceProvider =>
    new JsonBoardLoader(
        serviceProvider.GetRequiredService<JsonFileLoader<BoardData>>(),
        serviceProvider.GetRequiredService<BoardFilePathResolver>()
    ));

// Register legend services with proper dependency injection
builder.Services.AddSingleton<FilePathResolver>();
builder.Services.AddSingleton<LegendJsonParser>();
builder.Services.AddSingleton<LegendService>(serviceProvider => 
    new LegendService(
        serviceProvider.GetRequiredService<FilePathResolver>(),
        serviceProvider.GetRequiredService<LegendJsonParser>()
    ));

// Register new rendering and legend services
builder.Services.AddScoped<BoardRenderingService>();
builder.Services.AddScoped<LegendBuilderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
