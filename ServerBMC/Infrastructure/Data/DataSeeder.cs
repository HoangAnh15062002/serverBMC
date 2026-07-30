using Microsoft.EntityFrameworkCore;
using ServerBMC.Domain.Entities;
using ServerBMC.Infrastructure.Data;
using ServerBMC.Features.Estimates;

namespace ServerBMC.Infrastructure.Data;

public static class DataSeeder
{
    private const string HashAdmin123 = "$2a$12$ChbdsgYQJ4H2wpOYpnj8FuBD8NN2LtaDiXWKCvNzuzc39mWRevDRC";
    private const string HashUser123  = "$2a$12$T8jE24COCaMH1rD65oma3.vSVMnnQYCW4zyRwLBMNgQ5c2tQqza9a";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerBMCDbContext>();

        await SeedRolesAsync(db, ct);
        await SeedUsersAsync(db, ct);
        await SeedProjectsAsync(db, ct);
        await SeedEstimateCategoriesAsync(db, ct);
    }

    // ====================================================================
    // ROLES
    // ====================================================================

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

    // ====================================================================
    // USERS
    // ====================================================================

    private static async Task SeedUsersAsync(ServerBMCDbContext db, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == "admin@bmc.vn", ct)) return;

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "Admin", ct);
        var engineerRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "Engineer", ct);
        if (adminRole is null || engineerRole is null) return;

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

    // ====================================================================
    // PROJECTS
    // ====================================================================

    private static async Task SeedProjectsAsync(ServerBMCDbContext db, CancellationToken ct)
    {
        if (await db.Projects.AnyAsync(ct)) return;

        var adminUserId = await db.Users
            .Where(u => u.Email == "admin@bmc.vn")
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);
        if (adminUserId == 0) return;

        var project = new Project
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
            Description = "Công trình biệt thự 3 tầng, diện tích 450m².",
            CreatedBy = adminUserId
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
    }

    // ====================================================================
    // ESTIMATE CATEGORIES + SAMPLE ESTIMATE
    // ====================================================================

    private static async Task SeedEstimateCategoriesAsync(ServerBMCDbContext db, CancellationToken ct)
    {
        if (await db.EstimateCategories.AnyAsync(ct)) return;

        var adminUserId = await db.Users
            .Where(u => u.Email == "admin@bmc.vn")
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);
        var projectId = await db.Projects.Select(p => (int?)p.Id).FirstOrDefaultAsync(ct);

        if (adminUserId == 0) return;

        // Tạo EstimateCategory "Móng" gắn với Project
        var category = new EstimateCategory
        {
            ProjectId = projectId,
            Name = "Móng",
            Description = "Hạng mục móng cọc bê tông dự ứng lực",
            SortOrder = 1,
            Status = "Hoạt động",
            CreatedBy = adminUserId
        };
        db.EstimateCategories.Add(category);
        await db.SaveChangesAsync(ct);

        // Tạo Estimate M-02B
        var estimate = new Estimate
        {
            EstimateCategoryId = category.Id,
            DocumentType = "M-02B",
            DocumentNumber = "BT-C1.05",
            DocumentDate = "2026-07-01",
            CreatedBy = adminUserId
        };
        db.Estimates.Add(estimate);
        await db.SaveChangesAsync(ct);

        // Sample items
        var items = new List<EstimateItem>
        {
            new() { EstimateId = estimate.Id, Stt = 1, Code = "TT",     Name = "Cung cấp Cọc bê tông dự ứng lực D300mm", Unit = "m",       Quantity = 728m },
            new() { EstimateId = estimate.Id, Stt = 2, Code = "AC.26311",Name = "Ép cọc ứng bê tông cốt thép dự ứng lực bằng máy ép Robot thủy lực tự hành, đất cấp I, dùng khoan cọc 300mm", Unit = "100m", Quantity = 7.28m },
            new() { EstimateId = estimate.Id, Stt = 3, Code = "AC.26311",Name = "Ép âm cọc ứng bê tông cốt thép dự ứng lực bằng máy ép Robot thủy lực tự hành, đất cấp I, dùng khoan cọc 300mm", Unit = "100m", Quantity = 0.091m },
        };
        db.EstimateItems.AddRange(items);
        await db.SaveChangesAsync(ct);

        // Sample ItemDetails
        var details = new List<EstimateItemDetail>
        {
            new() { EstimateItemId = items[0].Id, DetailType = "Vật liệu", Code = "TT", Name = "Cọc bê tông dự ứng lực D300mm", Unit = "m", Quantity = 1.01m, UnitPrice = 290_000m, Factor = 1.0m, TotalAmount = 1.01m * 290_000m },
            new() { EstimateItemId = items[1].Id, DetailType = "Nhân công", Code = "AC.26311", Name = "Nhân công ép cọc", Unit = "công", Quantity = 7.28m, UnitPrice = 2_103_633m, Factor = 1.0m, TotalAmount = 7.28m * 2_103_633m },
            new() { EstimateItemId = items[1].Id, DetailType = "Máy", Code = "AC.26311", Name = "Máy ép cọc Robot", Unit = "ca", Quantity = 7.28m, UnitPrice = 16_927_537m, Factor = 1.0m, TotalAmount = 7.28m * 16_927_537m },
        };
        db.EstimateItemDetails.AddRange(details);
        await db.SaveChangesAsync(ct);

        // Tính totals
        var service = new EstimateService(db);
        foreach (var item in items)
            service.RecalculateItemTotals(item);
        await db.SaveChangesAsync(ct);

        service.RecalculateEstimateTotals(estimate.Id);
        await db.SaveChangesAsync(ct);
    }
}
