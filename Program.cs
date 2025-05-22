using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Port for Http
builder.WebHost.UseUrls("http://localhost:5016");

builder.Services.AddControllersWithViews();
// Key Service will be saved for a KeyController to controll dependency inj we should register on Program.cs
builder.Services.AddSingleton<KeyService>();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Removido o redirecionamento para HTTPS
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapControllers(); // Important for API

app.Run();
