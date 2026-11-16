using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Splitwise.Api.Configuration;
using Splitwise.Api.Extensions;
using Splitwise.Api.Filters;
using Splitwise.Api.Middleware;
using Splitwise.BusinessLogic;
using Splitwise.BusinessLogic.Abstractions;
using Splitwise.Domain.Entities;
using Splitwise.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Render (та інші PaaS) повідомляє порт через змінну PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ---------- База даних (PostgreSQL / Neon) ----------
builder.Services.AddDbContext<SplitwiseDbContext>(options =>
    options.UseNpgsql(ConnectionStringResolver.Resolve(builder.Configuration)));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<SplitwiseDbContext>());

// ---------- Шар бізнес-логіки ----------
builder.Services.AddBusinessLogic();

// ---------- Автентифікація / авторизація (ASP.NET Core Identity API endpoints) ----------
builder.Services.AddAuthorization();
builder.Services
    .AddIdentityApiEndpoints<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<SplitwiseDbContext>();

// ---------- За reverse proxy (Render) ----------
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks();

// ---------- Глобальна обробка помилок (порядок важливий: від конкретних до загального) ----------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();     // кастомні винятки -> 400/403/404/409
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();  // решта -> 500

// ValidationFilter валідує вхідні DTO через FluentValidation (валідатори реєструє AddBusinessLogic)
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();

    // Відсутні поля не дають "автоматичної" помилки MVC — їх перевіряють FluentValidation-валідатори,
    // щоб усі помилки валідації мали єдиний формат відповіді.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Bearer-токен, який повертає POST /api/auth/login (поле accessToken).
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Вставте accessToken, отриманий із POST /api/auth/login"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();

// Swagger доступний і на деплої (вимикається через Swagger:Enabled=false).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// На Render TLS завершується на проксі, тому редірект на HTTPS потрібен лише локально.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.ApplyDatabaseMigrations();

app.UseAuthentication();
app.UseAuthorization();

// /api/auth/register, /login, /refresh, /confirmEmail, /forgotPassword, /manage/info ...
app.MapGroup("/api/auth").MapIdentityApi<User>();

app.MapControllers();

// Службові ендпоінти для деплою
app.MapHealthChecks("/health");
app.MapGet("/version", () => Results.Ok(new
{
    // Render автоматично встановлює RENDER_GIT_COMMIT — використовується для smoke-тесту після деплою.
    commit = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT")
})).ExcludeFromDescription();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
