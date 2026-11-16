# Splitwise Clone (Backend API)

![CI](https://github.com/katerinauhimcuk40-lgtm/SplitwiseClone/actions/workflows/ci.yml/badge.svg)

REST API для клону сервісу **Splitwise**, розроблений на **.NET 10 / EF Core** з дотриманням принципів **Clean Architecture**.

## 🛠 Технологічний стек
- **Framework:** ASP.NET Core Web API (.NET 10)
- **Database:** PostgreSQL (локально — Docker, у проді — [Neon](https://neon.tech))
- **ORM:** Entity Framework Core (Code First)
- **Auth:** ASP.NET Core Identity (Identity API endpoints, Bearer-токени)
- **Validation:** FluentValidation 12
- **Tests:** xUnit, EF Core InMemory, coverlet
- **CI/CD:** GitHub Actions → Docker → [Render](https://render.com)
- **Documentation:** Swagger / OpenAPI

## 🏗 Архітектура

```
Splitwise.Api             → контролери (тільки HTTP), Program.cs, обробка помилок
Splitwise.BusinessLogic   → сервіси з бізнес-логікою, DTO, виняткові ситуації, IApplicationDbContext
Splitwise.Infrastructure  → SplitwiseDbContext (EF Core + Identity), міграції
Splitwise.Domain          → сутності (User, Group, GroupMember, Expense, ExpenseSplit, Settlement)
Splitwise.Tests           → unit-тести (xUnit)
```

Залежності: `Api → BusinessLogic → Domain`, `Infrastructure → BusinessLogic → Domain`.
Контролери не працюють з `DbContext` напряму — вони викликають сервіси (`IUserService`, `IGroupService`, `IExpenseService`).

### Бізнес-правила (шар BusinessLogic)
- Групи та витрати доступні лише учасникам групи.
- Автор групи автоматично стає її учасником; нових учасників додають за email.
- Платник і всі учасники розподілу мають бути учасниками групи.
- Якщо частки не вказано — витрата ділиться порівну (з розподілом копійок).
- Видалити витрату може лише той, хто її оплатив.

## 🔐 Автентифікація та авторизація
Використано [ASP.NET Core Identity API endpoints](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization). Користувач (`User`) успадковує `IdentityUser<int>`.

Ендпоінти автентифікації (`/api/auth/...`): `register`, `login`, `refresh`, `manage/info` та ін.
Усі `api/users`, `api/groups`, `api/expenses` захищені атрибутом `[Authorize]`.

Порядок роботи: `POST /api/auth/register` → `POST /api/auth/login` → у заголовку `Authorization: Bearer <accessToken>` (у Swagger — кнопка **Authorize**).

## ✅ Валідація вхідних DTO (FluentValidation)
Валідатори лежать у `Splitwise.BusinessLogic/Validators` (`CreateGroupRequestValidator`, `AddGroupMemberRequestValidator`, `CreateExpenseRequestValidator`, `ExpenseSplitRequestValidator`) і реєструються через `AddValidatorsFromAssemblyContaining`.

Пакет `FluentValidation.AspNetCore` застарів, тому інтеграція зроблена власним `ValidationFilter` (`Splitwise.Api/Filters`): перед виконанням екшена він знаходить `IValidator<T>` для кожного аргумента і, якщо є помилки, кидає `RequestValidationException`.

## ⚠️ Кастомні винятки та глобальна обробка помилок
Ієрархія (`Splitwise.BusinessLogic/Exceptions`):

```
AppException (ErrorCode)
├── NotFoundException (404)        → UserNotFoundException, GroupNotFoundException, ExpenseNotFoundException
├── ForbiddenException (403)       → NotGroupMemberException, ExpenseDeletionNotAllowedException
├── ConflictException (409)        → UserAlreadyInGroupException
├── BusinessRuleException (400)    → InvalidExpenseException
└── RequestValidationException (400, помилки по полях)
```

`AppExceptionHandler` перетворює їх на `ProblemDetails` (RFC 9457) з полем `code`, а `GlobalExceptionHandler` логує будь-які інші винятки й повертає безпечну відповідь 500. Обидва підключені через `IExceptionHandler` + `app.UseExceptionHandler()`.

Приклад відповіді на невалідний запит (`400`):

```json
{
  "title": "Помилка валідації",
  "status": 400,
  "detail": "Один або кілька параметрів запиту не пройшли валідацію.",
  "errors": {
    "Description": ["Опис витрати обов'язковий."],
    "Amount": ["Сума витрати має бути більшою за нуль."]
  },
  "code": "RequestValidation"
}
```

## 🧪 Unit-тести
Проєкт `Splitwise.Tests` (xUnit). Сервіси тестуються на `InMemory`-базі EF Core, окрема ізольована база на кожен тест.

| Що покрито | Файли |
|---|---|
| Бізнес-логіка сервісів (групи, витрати, користувачі) | `Services/*ServiceTests.cs` |
| FluentValidation-валідатори DTO | `Validators/*Tests.cs` |
| Глобальні обробники помилок, ієрархія винятків | `Api/AppExceptionHandlerTests.cs` |
| Фільтр валідації вхідних DTO | `Api/ValidationFilterTests.cs` |
| Отримання Id користувача з токена | `Api/ClaimsPrincipalExtensionsTests.cs` |

Запуск локально:

```bash
dotnet test
# з покриттям коду
dotnet test --collect:"XPlat Code Coverage"
```

## 🔄 CI (GitHub Actions)
Workflow `.github/workflows/ci.yml` запускається при кожному `push` і `pull request` у `main` (а також вручну): відновлює залежності, збирає solution у Release, запускає всі тести, рахує покриття і зберігає звіти (`.trx`, `coverage.cobertura.xml`) як artifact `test-results`. Підсумок покриття видно на сторінці запуску (Summary).

## 🚀 Локальний запуск

**Варіант 1 — Docker Compose (API + PostgreSQL):**

```bash
docker compose up --build
# Swagger: http://localhost:8080/swagger
```

**Варіант 2 — `dotnet run` + локальний PostgreSQL** (рядок підключення в `Splitwise.Api/appsettings.json`):

```bash
docker run -d --name splitwise-db -p 5432:5432 -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=splitwise postgres:17
dotnet run --project Splitwise.Api
# Swagger: http://localhost:5013/swagger
```

Міграції застосовуються автоматично під час старту (`Database:MigrateOnStartup`).

### Міграції EF Core (PostgreSQL)
Міграції створюються один раз і комітяться в репозиторій. Якщо в `Splitwise.Infrastructure/Migrations` лежать старі міграції для SQL Server — видаліть папку й створіть нову:

```bash
dotnet ef migrations add InitialPostgres -p Splitwise.Infrastructure -s Splitwise.Api -o Migrations
```

## ☁️ Деплой (Neon + Render + GitHub Actions)

```
git push → GitHub Actions: build + tests + docker build → deploy hook → Render (Docker) → Neon (PostgreSQL)
```

### 1. База даних — Neon
1. Створіть проєкт на [neon.tech](https://neon.tech) і скопіюйте **Connection string** (формат `postgresql://user:password@ep-xxx.neon.tech/neondb?sslmode=require`).
2. У вікні Connection string вимкніть перемикач **Connection pooling** — потрібен прямий (не `-pooler`) хост, бо міграції EF Core використовують advisory locks.

Застосунок сам розуміє формат URI (`ConnectionStringResolver`), конвертувати рядок вручну не треба.

### 2. API — Render
1. Закомітьте й запуште репозиторій на GitHub (разом із міграціями).
2. Render → **New → Blueprint** → оберіть репозиторій (підхопиться `render.yaml`) **або** New → Web Service → Runtime **Docker**.
3. У **Environment** додайте змінну `DATABASE_URL` = рядок підключення з Neon.
4. Автодеплой із Git вимкнено (`autoDeployTrigger: off`) — деплоєм керує GitHub Actions.
5. Settings → **Deploy Hook** → скопіюйте URL.

### 3. CD — GitHub Actions
У репозиторії: **Settings → Secrets and variables → Actions**:

| Тип | Назва | Значення |
|---|---|---|
| Secret | `RENDER_DEPLOY_HOOK_URL` | Deploy Hook URL з Render |
| Variable | `API_URL` | `https://<ваш-сервіс>.onrender.com` |

Також створіть **Environment** `production` (Settings → Environments), якщо його ще немає.

Workflow `.github/workflows/ci.yml`: тести та збірка Docker-образу → (тільки `push` у `main`) виклик deploy hook → smoke test: чекає, поки `/version` поверне SHA щойно запушеного коміту, і перевіряє `/health`.

### Службові ендпоінти
| Ендпоінт | Призначення |
|---|---|
| `/health` | health check для Render |
| `/version` | SHA задеплоєного коміту |
| `/swagger` | документація API |

> На безкоштовному плані Render сервіс «засинає» після 15 хв без запитів — перший запит після паузи може тривати ~1 хв. Bearer-токени Identity після перезапуску сервісу стають недійсними (ключі Data Protection не зберігаються) — потрібно залогінитись повторно.

## Діаграма варіантів використання (Use Case Diagram)

![Use Case Diagram](docs/images/use-case-diagram.png)
