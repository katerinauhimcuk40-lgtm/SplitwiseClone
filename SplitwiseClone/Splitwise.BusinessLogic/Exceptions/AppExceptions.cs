namespace Splitwise.BusinessLogic.Exceptions;

/// <summary>
/// Базовий тип усіх очікуваних (доменних) помилок. Глобальний обробник в API
/// перетворює їх на відповіді ProblemDetails з відповідним HTTP-статусом.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }

    /// <summary>Машинозчитуваний код помилки (потрапляє у поле "code" відповіді).</summary>
    public virtual string ErrorCode => GetType().Name.Replace("Exception", string.Empty);
}

// ---------- Категорії (відповідають HTTP-статусам) ----------

/// <summary>Сутність не знайдена -> 404.</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Немає прав на дію -> 403.</summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>Порушення бізнес-правила -> 400.</summary>
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>Конфлікт стану (наприклад, дублікат) -> 409.</summary>
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>
/// Вхідні дані не пройшли валідацію (FluentValidation) -> 400.
/// Errors: ім'я поля -> список повідомлень.
/// </summary>
public class RequestValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public RequestValidationException(IDictionary<string, string[]> errors)
        : base("Один або кілька параметрів запиту не пройшли валідацію.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}

// ---------- Конкретні доменні винятки ----------

public class UserNotFoundException : NotFoundException
{
    public UserNotFoundException(string message = "Користувача не знайдено.") : base(message) { }
}

public class GroupNotFoundException : NotFoundException
{
    public GroupNotFoundException(int groupId) : base($"Групу з id={groupId} не знайдено.") { }
}

public class ExpenseNotFoundException : NotFoundException
{
    public ExpenseNotFoundException(int expenseId) : base($"Витрату з id={expenseId} не знайдено.") { }
}

public class NotGroupMemberException : ForbiddenException
{
    public NotGroupMemberException() : base("Ви не є учасником цієї групи.") { }
}

public class ExpenseDeletionNotAllowedException : ForbiddenException
{
    public ExpenseDeletionNotAllowedException()
        : base("Видалити витрату може лише той, хто її оплатив.") { }
}

public class UserAlreadyInGroupException : ConflictException
{
    public UserAlreadyInGroupException() : base("Користувач уже є учасником групи.") { }
}

public class InvalidExpenseException : BusinessRuleException
{
    public InvalidExpenseException(string message) : base(message) { }
}
