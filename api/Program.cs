using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
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

builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IPasswordHasher<UserTable>, PasswordHasher<UserTable>>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AngularApp");
app.UseAuthentication();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

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
