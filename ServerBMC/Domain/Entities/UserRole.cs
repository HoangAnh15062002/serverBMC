namespace ServerBMC.Domain.Entities;

public class UserRole
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}