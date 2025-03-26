using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Web;
using ToDo.Components;
using ToDo.Data;
using ToDo.Services;
using Blazored.TextEditor;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();



var connectionString = builder.Configuration.GetConnectionString("StudentDB");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<TodoService>();
builder.Services.AddScoped<JournalService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
