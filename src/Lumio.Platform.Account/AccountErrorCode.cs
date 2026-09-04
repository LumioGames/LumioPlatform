namespace Lumio.Platform.Account;

public static class AccountErrorCode
{
    public const string InvalidRequest = "invalid_request";
    public const string InvalidUsername = "invalid_username";
    public const string InvalidPassword = "invalid_password";
    public const string WrongPassword = "wrong_password";
    public const string BotNamespaceRegisterForbidden = "bot_namespace_register_forbidden";
    public const string BotNamespaceLoginForbidden = "bot_namespace_login_forbidden";
    public const string BotNamespaceAdmissionForbidden = "bot_namespace_admission_forbidden";
    public const string BotToolCredentialMalformed = "bot_tool_credential_malformed";
    public const string BotToolCredentialInvalid = "bot_tool_credential_invalid";
    public const string BotToolCredentialExpired = "bot_tool_credential_expired";
    public const string AdmissionCredentialMalformed = "admission_credential_malformed";
    public const string AdmissionCredentialInvalidSignature = "admission_credential_invalid_signature";
    public const string AdmissionCredentialExpired = "admission_credential_expired";
    public const string AdmissionBindingMismatch = "admission_binding_mismatch";
    public const string AdmissionCredentialUnbound = "admission_credential_unbound";
    public const string RegistrationRequiresPlatform = "registration_requires_platform";
    public const string RateLimited = "rate_limited";
    public const string AccountBanned = "account_banned";
    public const string TakeoverNoticeInvalid = "takeover_notice_invalid";

    public static readonly string[] All =
    [
        InvalidRequest,
        InvalidUsername,
        InvalidPassword,
        WrongPassword,
        BotNamespaceRegisterForbidden,
        BotNamespaceLoginForbidden,
        BotNamespaceAdmissionForbidden,
        BotToolCredentialMalformed,
        BotToolCredentialInvalid,
        BotToolCredentialExpired,
        AdmissionCredentialMalformed,
        AdmissionCredentialInvalidSignature,
        AdmissionCredentialExpired,
        AdmissionBindingMismatch,
        AdmissionCredentialUnbound,
        TakeoverNoticeInvalid,
        RegistrationRequiresPlatform,
        RateLimited,
        AccountBanned,
    ];
}

