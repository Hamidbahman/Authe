using System;
using System.ComponentModel.DataAnnotations.Schema;
using Enums;


namespace Entities;

[Table("tbConfigurationLock")]
public class ConfigurationLock : BaseEntity
{
    public bool CaptchaNeeded {get;private set;} = false;
    public short FailedLoginAmountBeforeCaptcha {get;private set;}
    public int LockTimeInterval {get;private set;}
    public LockTypes LockType {get;private set;}
    [ForeignKey("Application")]
    public long ApplicationId {get;private set;}
    public Application Application {get;private set;}

    public ConfigurationLock(){}

    public ConfigurationLock(
        long id,
        bool captchaNeeded,
        short failedLoginAmountBeforeCaptcha,
        int lockTimeInterval,
        LockTypes lockType,
        long applicationId
    ) {
        Id = id;
        CaptchaNeeded = captchaNeeded;
        FailedLoginAmountBeforeCaptcha = failedLoginAmountBeforeCaptcha;
        LockTimeInterval = lockTimeInterval;
        LockType = lockType;
        ApplicationId = applicationId;
    }
}
