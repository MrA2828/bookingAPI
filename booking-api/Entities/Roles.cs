namespace BookingApi.Entities;

public static class Roles
{
    public const string Admin = "ADMIN";
    public const string Customer = "CUSTOMERS";

    // The full list, used to seed the roles table on startup.
    public static readonly string[] All = [Admin, Customer];
}
