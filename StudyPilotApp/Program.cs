using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Options;
using StudyPilotApp.Services;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ASP.NET Core Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // User settings
        options.User.RequireUniqueEmail = true;

        // Password requirements
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;

        // Account lockout protection
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        // Email confirmation can be added later
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Authentication cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "StudyPilot.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// StudyPilot services
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<IGpaService, GpaService>();
builder.Services.AddScoped<IPriorityService, PriorityService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStudentDashboardService, StudentDashboardService>();
builder.Services.AddScoped<ISmartStudyPlanService, SmartStudyPlanService>();
builder.Services.AddScoped<IAcademicContextService, AcademicContextService>();
builder.Services.AddScoped<IAcademicAIConversationService, AcademicAIConversationService>();
builder.Services.AddScoped<IAcademicAIService, AcademicAIService>();

builder.Services.Configure<AcademicAIOptions>(
    builder.Configuration.GetSection(AcademicAIOptions.SectionName));
builder.Services.AddHttpClient<IAITextProvider, GeminiAITextProvider>((services, client) =>
{
    var configuration = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AcademicAIOptions>>().Value;
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(configuration.TimeoutSeconds, 5, 60));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("academic-ai", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// Authentication must come before authorization
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// Serve CSS, JavaScript and image files
app.MapStaticAssets();

// MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Seed Student, Faculty, Admin roles and initial Admin account
await IdentitySeeder.SeedAsync(app.Services, app.Configuration);
await EventSeeder.SeedAsync(app.Services);

app.Run();
