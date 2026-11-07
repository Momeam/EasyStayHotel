using EasyStayHotel.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// -----------------------------------------------------

builder.Services.AddDbContext<EasystaydbContext>(options =>
    options.UseMySql("server=localhost;port=3306;database=easystaydb;user=root;password=1234;",
    ServerVersion.AutoDetect("server=localhost;port=3306;database=easystaydb;user=root;password=1234;")));
// -----------------------------------------------------


var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();