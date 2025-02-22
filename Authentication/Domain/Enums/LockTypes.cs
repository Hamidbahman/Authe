namespace Authentication.Domain.Enums;

public enum LockTypes : short
{
    TemporaryLock = 1,
    PermanentLock = 2,
    ExpiringLock = 3,
    ConditionalLock = 4,
    None = 0
}
