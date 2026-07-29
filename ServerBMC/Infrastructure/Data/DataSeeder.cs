using Microsoft.EntityFrameworkCore;
using ServerBMC.Domain.Entities;
using ServerBMC.Infrastructure.Data;
using ServerBMC.Infrastructure.Security;

namespace ServerBMC.Infrastructure.Data;

public static class DataSeeder
{
    // BCrypt workFactor 12 — generated via BCrypt.Net-Next 4.0.3
    private const string HashAdmin123 = "$2a$12$ChbdsgYQJ4H2wpOYpnj8FuBD8NN2LtaDiXWKCvNzuzc39mWRevDRC";
    private const string HashUser123  = "$2a$12$T8jE24COCaMH1rD65oma3.vSVMnnQYCW4zyRwLBMNgQ5c2tQqza9a";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerBMCDbContext>();

        await SeedRolesAsync(db, ct);
        await SeedUsersAsync(db, ct);
        await SeedProjectsAsync(db, ct);
    }

    private static async Task SeedRolesAsync(ServerBMCDbContext db, CancellationToken ct)
    {
        if (await db.Roles.AnyAsync(ct)) return;

        var roles = new[]
        {
            new Role { Code = "Admin",     Name = "Quản trị viên",     Description = "Toàn quyền quản lý hệ thống", IsActive = true },
            new Role { Code = "VP",        Name = "Giám đốc",           Description = "Giám đốc dự án",            IsActive = true },
            new Role { Code = "Director",  Name = "Đạo diễn",           Description = "Đạo diễn dự án",            IsActive = true },
            new Role { Code = "Engineer",  Name = "Kỹ sư",              Description = "Kỹ sư giám sát",             IsActive = true },
            new Role { Code = "Accountant",Name = "Kế toán",            Description = "Kế toán dự án",              IsActive = true },
            new Role { Code = "Viewer",    Name = "Người xem",          Description = "Chỉ xem thông tin",          IsActive = true }
        };
        db.Roles.AddRange(roles);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedUsersAsync(ServerBMCDbContext db, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == "admin@bmc.vn", ct))
            return; // đã seed rồi

        var adminRole  = await db.Roles.FirstOrDefaultAsync(r => r.Code == "Admin", ct);
        var vpRole     = await db.Roles.FirstOrDefaultAsync(r => r.Code == "VP", ct);
        var engineerRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "Engineer", ct);

        if (adminRole is null || engineerRole is null)
        {
            throw new InvalidOperationException("Roles Admin and Engineer must exist before seeding users");
        }

        // Seed user theo spec: admin@bmc.vn / admin123
        var adminUser = new User
        {
            Email = "admin@bmc.vn",
            PasswordHash = HashAdmin123,
            FullName = "Quản trị viên BMC",
            IsActive = true
        };
        db.Users.Add(adminUser);
        await db.SaveChangesAsync(ct);

        db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id, IsPrimary = true });

        // Seed user01@bmc.vn / user123
        var user01 = new User
        {
            Email = "user01@bmc.vn",
            PasswordHash = HashUser123,
            FullName = "Nguyễn Văn A",
            Phone = "0909123456",
            IsActive = true
        };
        db.Users.Add(user01);
        await db.SaveChangesAsync(ct);

        db.UserRoles.Add(new UserRole { UserId = user01.Id, RoleId = engineerRole.Id, IsPrimary = true });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedProjectsAsync(ServerBMCDbContext db, CancellationToken ct)
    {
        if (await db.Projects.AnyAsync(ct))
            return; // đã seed rồi

        var adminUserId = await db.Users
            .Where(u => u.Email == "admin@bmc.vn")
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);

        if (adminUserId == 0) return;

        var projects = new[]
        {
            new Project
            {
                ProjectCode = "M-02B",
                ProjectName = "Biệt thự M-02B",
                ProjectType = "Biệt thự cao cấp",
                Location = "Quận 2, TP. Thủ Đức",
                Investor = "Ông Trần Văn Minh",
                Contractor = "Công ty TNHH Xây dựng BMC",
                ContractValue = 8_500_000_000m,
                StartDate = new DateTime(2026, 3, 1),
                EndDate = new DateTime(2027, 6, 30),
                Status = "Đang thi công",
                Description = "Công trình biệt thự 3 tầng, diện tích 450m², bao gồm phần thô và hoàn thiện.",
                CreatedBy = adminUserId
            },
            new Project
            {
                ProjectCode = "M-03",
                ProjectName = "Căn hộ M-03",
                ProjectType = "Căn hộ chung cư",
                Location = "Quận 7, TP. HCM",
                Investor = "Bà Lê Thị Hương",
                Contractor = "Công ty TNHH Xây dựng BMC",
                ContractValue = 12_000_000_000m,
                StartDate = new DateTime(2026, 5, 15),
                EndDate = new DateTime(2028, 12, 31),
                Status = "Đang thi công",
                Description = "Dự án căn hộ 200 căn, 20 tầng, hoàn thiện full nội thất.",
                CreatedBy = adminUserId
            },
            new Project
            {
                ProjectCode = "N-01",
                ProjectName = "Nhà phố N-01",
                ProjectType = "Nhà phố liên kế",
                Location = "Quận 9, TP. Thủ Đức",
                Investor = "Anh Nguyễn Hoàng Nam",
                Contractor = "Công ty TNHH Xây dựng BMC",
                ContractValue = 3_200_000_000m,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2027, 3, 31),
                Status = "Đang thi công",
                Description = "Nhà phố 4 tầng, diện tích 180m², thi công phần thô.",
                CreatedBy = adminUserId
            }
        };

        db.Projects.AddRange(projects);
        await db.SaveChangesAsync(ct);
    }
}
