using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using PCS_API.Handlers;
using PCS_API.Models;
using PCS_API.Repositories;
using PCS_API.Services;

var builder = WebApplication.CreateBuilder(args);

// Register Dapper Type Handlers
Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
Dapper.SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

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

        // Sliding expiration: session is extended with each authenticated request.
        // The absolute maximum lifetime is capped at 7 days from last activity.
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
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

    // Investors & Investments
    options.AddPolicy("CanViewInvestors", p => p.Requirements.Add(new PermissionRequirementHandler("investor:view")));
    options.AddPolicy("CanCreateInvestors", p => p.Requirements.Add(new PermissionRequirementHandler("investor:create")));
    options.AddPolicy("CanEditInvestors", p => p.Requirements.Add(new PermissionRequirementHandler("investor:edit")));
    options.AddPolicy("CanViewInvestments", p => p.Requirements.Add(new PermissionRequirementHandler("investment:view")));
    options.AddPolicy("CanCreateInvestments", p => p.Requirements.Add(new PermissionRequirementHandler("investment:create")));
    options.AddPolicy("CanEditInvestments", p => p.Requirements.Add(new PermissionRequirementHandler("investment:edit")));

    // Finance
    options.AddPolicy("CanViewFinancials", p => p.Requirements.Add(new PermissionRequirementHandler("finance:view")));
    options.AddPolicy("CanManageFinancials", p => p.Requirements.Add(new PermissionRequirementHandler("finance:manage")));

    // Bank Accounts
    options.AddPolicy("CanViewBankAccounts", p => p.Requirements.Add(new PermissionRequirementHandler("bank_account:view")));
    options.AddPolicy("CanManageBankAccounts", p => p.Requirements.Add(new PermissionRequirementHandler("bank_account:manage")));
    options.AddPolicy("CanEditPurchaseOrder", p => p.Requirements.Add(new PermissionRequirementHandler("purchase-order:edit")));
    options.AddPolicy("CanDeletePurchaseOrder", p => p.Requirements.Add(new PermissionRequirementHandler("purchase-order:delete")));

    // Policy for authenticated access to protected static files (uploaded images)
    options.AddPolicy("AuthenticatedOnly", p => p.RequireAuthenticatedUser());
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
builder.Services.AddScoped<IPackingRepository, PackingRepository>();
builder.Services.AddScoped<IPackingService, PackingService>();

//  Financial, Sales & Procurement modules 
builder.Services.AddScoped<IChartOfAccountRepository, ChartOfAccountRepository>();
builder.Services.AddScoped<IChartOfAccountService, ChartOfAccountService>();
builder.Services.AddScoped<ITaxInvoiceRepository, TaxInvoiceRepository>();
builder.Services.AddScoped<ITaxInvoiceService, TaxInvoiceService>();
builder.Services.AddScoped<IFinancialTransactionRepository, FinancialTransactionRepository>();
builder.Services.AddScoped<IFinancialTransactionService, FinancialTransactionService>();
builder.Services.AddScoped<IFinancialReportRepository, FinancialReportRepository>();
builder.Services.AddScoped<IFinancialReportService, FinancialReportService>();
builder.Services.AddScoped<IPartnerBankAccountRepository, PartnerBankAccountRepository>();
builder.Services.AddScoped<ICompanyBankAccountRepository, CompanyBankAccountRepository>();
builder.Services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
builder.Services.AddScoped<ISalesOrderService, SalesOrderService>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();

//  Investors & Investments modules 
builder.Services.AddScoped<IInvestorRepository, InvestorRepository>();
builder.Services.AddScoped<IInvestorBankAccountRepository, InvestorBankAccountRepository>();
builder.Services.AddScoped<IInvestmentRepository, InvestmentRepository>();
builder.Services.AddScoped<IInvestorService, InvestorService>();
builder.Services.AddScoped<IInvestmentService, InvestmentService>();
builder.Services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
builder.Services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
builder.Services.AddSingleton<ImageCleanupChannel>();
builder.Services.AddHostedService<ImageCleanupBackgroundService>();
builder.Services.AddHostedService<TempFileCleanupBackgroundService>();
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

//  Security Headers 
// Applied early so every response carries them, including error responses.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseCors("AngularApp");
app.UseRateLimiter();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

//  Public static files (non-upload assets) 
app.UseStaticFiles();

//  Protected uploaded files 
// Requires authentication. The path /api/uploads/* is served only if the
// request carries a valid AuthCookie. Unauthenticated callers receive 401.
var uploadsPath = Path.Combine(
    builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"),
    "uploads");

app.MapGet("/api/uploads/{**path}", async (HttpContext context, string path, IWebHostEnvironment env, IAuthorizationService authorizationService) =>
{
    // Enforce authentication
    var authResult = await authorizationService.AuthorizeAsync(context.User, "AuthenticatedOnly");
    if (!authResult.Succeeded)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    // Path traversal guard: resolve and confirm the final path is inside uploads
    var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
    var uploadsDir = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
    var requestedFile = Path.GetFullPath(Path.Combine(uploadsDir, path));

    if (!requestedFile.StartsWith(uploadsDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    if (!File.Exists(requestedFile))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var ext = Path.GetExtension(requestedFile).ToLowerInvariant();
    var contentType = ext switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };

    context.Response.ContentType = contentType;
    // Prevent caching of sensitive images across sessions
    context.Response.Headers["Cache-Control"] = "private, max-age=3600";
    await context.Response.SendFileAsync(requestedFile);
});

//  Custom CSRF Validation Middleware 
// Validates the double-submit cookie pattern for all state-changing requests.
// Exemptions: GET/HEAD/OPTIONS/TRACE (safe methods), login, and csrf-token endpoints.
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

