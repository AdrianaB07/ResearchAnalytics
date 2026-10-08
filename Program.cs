using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResearchAnalytics.Data;
using ResearchAnalytics.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

builder.Services.AddSession();

builder.Services.AddScoped<IResearchDataService, ResearchDataService>();
builder.Services.AddScoped<SemanticScholarTestService>();
builder.Services.AddHttpClient<ScopusService>();
builder.Services.AddHttpClient<OpenAlexService>();
builder.Services.AddHttpClient<OrcidService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string adminRole = "Admin";
    string userRole = "User";

    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRole));
    }

    if (!await roleManager.RoleExistsAsync(userRole))
    {
        await roleManager.CreateAsync(new IdentityRole(userRole));
    }

    string adminEmail = "admin@yahoo.com"; //Admin@321 ; Adriana@yahoo.com  ; User@321

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, adminRole))
    {
        await userManager.AddToRoleAsync(adminUser, adminRole);
    }
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
app.MapRazorPages();

app.Run();
