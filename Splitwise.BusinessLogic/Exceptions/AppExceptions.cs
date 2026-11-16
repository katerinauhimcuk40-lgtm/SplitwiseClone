namespace Splitwise.BusinessLogic.Exceptions;

/// <summary>Базовий тип помилок бізнес-логіки (API перетворює їх на HTTP-відповіді).</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

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

/// <summary>Порушення бізнес-правила / невалідні дані -> 400.</summary>
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>Конфлікт стану (наприклад, дублікат) -> 409.</summary>
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}
