using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Splitwise.Api.Filters;
using Splitwise.Api.Middleware;
using Splitwise.BusinessLogic;
using Splitwise.BusinessLogic.Abstractions;
using Splitwise.Domain.Entities;
using Splitwise.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---------- База даних ----------
builder.Services.AddDbContext<SplitwiseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
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

app.UseExceptionHandler();

// Налаштування середовища розробки та Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// /api/auth/register, /login, /refresh, /confirmEmail, /forgotPassword, /manage/info ...
app.MapGroup("/api/auth").MapIdentityApi<User>();

app.MapControllers();

app.Run();
