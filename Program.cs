global using Microsoft.EntityFrameworkCore;
global using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<NoteManager>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddDbContextFactory<DBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ConnectionToDB")));
builder.Services.AddHostedService<DataRefreshService>();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
