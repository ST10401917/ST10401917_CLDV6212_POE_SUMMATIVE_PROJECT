using CLDV6212_POE_PART_1.Services;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CLDV6212_POE_PART_1.Data;
using CLDV6212_POE_PART_1.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CLDV6212_POE_PART_1Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CLDV6212_POE_PART_1Context") ?? throw new InvalidOperationException("Connection string 'CLDV6212_POE_PART_1Context' not found.")));
var configuration = builder.Configuration;

builder.Configuration.AddJsonFile("appsettings.json");


// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton(new TableStorageService(configuration.GetConnectionString("AzureStorage")));
builder.Services.AddSingleton(new BlobService(configuration.GetConnectionString("AzureStorage"), "your-container-name"));

builder.Services.AddSingleton<QueueService>(sp =>
{
    var connectionString = configuration.GetConnectionString("AzureStorage");
    return new QueueService(connectionString, "orders");
});


builder.Services.AddSingleton<AzureFileShareService>(sp =>
{
    var connectionString = configuration.GetConnectionString("AzureStorage");
    return new AzureFileShareService(connectionString, "contractshare");
});

builder.Services.AddHttpClient();

builder.Services.AddSession();
var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CLDV6212_POE_PART_1Context>();
    db.Database.Migrate(); // Ensure DB exists

    // Create admin if not exists
    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        db.Users.Add(new User
        {
            FirstName = "System",
            LastName = "Administrator",
            Username = "admin",
            Password = "admin123",
            Role = "Admin",
            Email = "admin@pixelheaven.com"

        });

        db.SaveChanges();
    }
}




app.UseSession();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value.ToLower();
    var role = context.Session.GetString("Role");

    // 1. If NOT logged in  redirect to login
    if (role == null && !path.Contains("/login"))
    {
        context.Response.Redirect("/Login/Index");
        return;
    }

    // . Customers CANNOT access any Admin pages
    if (role == "Customer" && path.Contains("/admin"))
    {
        context.Response.Redirect("/Product/Products");
        return;
    }

    await next();
});


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseStaticFiles(); // <-- Add this line


app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
