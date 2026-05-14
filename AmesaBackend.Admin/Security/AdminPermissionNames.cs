namespace AmesaBackend.Admin.Security;

public static class AdminPermissionNames
{
    public const string DashboardRead = "dashboard.read";
    public const string HousesRead = "houses.read";
    public const string HousesWrite = "houses.write";
    public const string HousesPublish = "houses.publish";
    public const string HousesDelete = "houses.delete";
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";
    public const string UsersSuspend = "users.suspend";
    public const string TicketsRead = "tickets.read";
    public const string DrawsRead = "draws.read";
    public const string DrawsConduct = "draws.conduct";
    public const string PaymentsRead = "payments.read";
    public const string PaymentsRefund = "payments.refund";
    public const string TranslationsRead = "translations.read";
    public const string TranslationsWrite = "translations.write";
    public const string PromotionsRead = "promotions.read";
    public const string PromotionsWrite = "promotions.write";
    public const string NotificationsRead = "notifications.read";
    public const string NotificationsSend = "notifications.send";
    public const string AuditRead = "audit.read";
    public const string AdminUsersManage = "admin_users.manage";
    public const string SettingsManage = "settings.manage";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        DashboardRead,
        HousesRead,
        HousesWrite,
        HousesPublish,
        HousesDelete,
        UsersRead,
        UsersWrite,
        UsersSuspend,
        TicketsRead,
        DrawsRead,
        DrawsConduct,
        PaymentsRead,
        PaymentsRefund,
        TranslationsRead,
        TranslationsWrite,
        PromotionsRead,
        PromotionsWrite,
        NotificationsRead,
        NotificationsSend,
        AuditRead,
        AdminUsersManage,
        SettingsManage
    };
}
