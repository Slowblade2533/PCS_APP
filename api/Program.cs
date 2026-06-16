using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using PCS_API.Handlers;
using PCS_API.Models;
using PCS_API.Repositories;
using PCS_API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("GlobalLimiter", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });

    options.AddFixedWindowLimiter("LoginLimiter", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Cookie.MaxAge = options.ExpireTimeSpan;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddAuthorization(options =>
{
    // System
    options.AddPolicy("CanManageSettings", p => p.Requirements.Add(new PermissionRequirementHandler("system:settings")));
    options.AddPolicy("CanViewAuditLog", p => p.Requirements.Add(new PermissionRequirementHandler("system:audit_log")));

    // User
    options.AddPolicy("CanViewUsers", p => p.Requirements.Add(new PermissionRequirementHandler("user:view")));
    options.AddPolicy("CanManageUsers", p => p.Requirements.Add(new PermissionRequirementHandler("user:manage")));

    // Product
    options.AddPolicy("CanViewProduct", p => p.Requirements.Add(new PermissionRequirementHandler("product:view")));
    options.AddPolicy("CanCreateProduct", p => p.Requirements.Add(new PermissionRequirementHandler("product:create")));
    options.AddPolicy("CanEditProduct", p => p.Requirements.Add(new PermissionRequirementHandler("product:edit")));
    options.AddPolicy("CanViewProductCost", p => p.Requirements.Add(new PermissionRequirementHandler("product:view_cost")));

    // Stock
    options.AddPolicy("CanViewStock", p => p.Requirements.Add(new PermissionRequirementHandler("stock:view")));
    options.AddPolicy("CanStockIn", p => p.Requirements.Add(new PermissionRequirementHandler("stock:in")));
    options.AddPolicy("CanStockOut", p => p.Requirements.Add(new PermissionRequirementHandler("stock:out")));
    options.AddPolicy("CanStockAdjust", p => p.Requirements.Add(new PermissionRequirementHandler("stock:adjust")));

    // Upload
    options.AddPolicy("CanUploadImage", p => p.Requirements.Add(new PermissionRequirementHandler("upload:image")));

    // Report
    options.AddPolicy("CanViewReport", p => p.Requirements.Add(new PermissionRequirementHandler("report:view")));
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IPasswordHasher<UserTableModel>, PasswordHasher<UserTableModel>>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IVcbOrderRepository, VcbOrderRepository>();
builder.Services.AddScoped<IVcbShipmentRepository, VcbShipmentRepository>();
builder.Services.AddScoped<IVcbDeliveryRepository, VcbDeliveryRepository>();
builder.Services.AddScoped<IAccountTransactionRepository, AccountTransactionRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IVcbOrderService, VcbOrderService>();
builder.Services.AddScoped<IVcbShipmentService, VcbShipmentService>();
builder.Services.AddScoped<IVcbDeliveryService, VcbDeliveryService>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddSingleton<ImageCleanupChannel>();
builder.Services.AddHostedService<ImageCleanupBackgroundService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    });
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AngularApp");
app.UseRateLimiter(); // Add Rate Limiter

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles(); // After Authorization so we can protect it if needed

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"), "uploads")
        ),
    RequestPath = "/api/uploads"
});

app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method)
        || HttpMethods.IsHead(context.Request.Method)
        || HttpMethods.IsOptions(context.Request.Method)
        || HttpMethods.IsTrace(context.Request.Method))
    {
        await next();
        return;
    }

    if (context.Request.Path.StartsWithSegments("/api/auth/login"))
    {
        await next();
        return;
    }

    if (context.Request.Path.StartsWithSegments("/api/auth/csrf-token"))
    {
        await next();
        return;
    }

    var cookieToken = context.Request.Cookies["XSRF-TOKEN"];
    var headerToken = context.Request.Headers["X-XSRF-TOKEN"].ToString();

    if (string.IsNullOrWhiteSpace(cookieToken)
        || string.IsNullOrWhiteSpace(headerToken)
        || cookieToken != headerToken)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        await context.Response.WriteAsJsonAsync(new
        {
            statusCode = 400,
            message = "Invalid CSRF token."
        });

        return;
    }

    await next();
});

app.MapControllers();

app.Run();
