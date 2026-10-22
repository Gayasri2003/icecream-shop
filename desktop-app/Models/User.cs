namespace IceCreamShop.Desktop.Models;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public bool IsOwner => string.Equals(Role, "owner", StringComparison.OrdinalIgnoreCase);
    public bool IsManager => string.Equals(Role, "manager", StringComparison.OrdinalIgnoreCase);
    public bool IsCashier => string.Equals(Role, "cashier", StringComparison.OrdinalIgnoreCase);

    public string RoleDisplayName => Role switch
    {
        "owner" => "Owner",
        "manager" => "Manager",
        "cashier" => "Cashier",
        _ => Role
    };
}
