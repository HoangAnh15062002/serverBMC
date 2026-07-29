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
        await SeedEstimatesAsync(db, ct);
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

    private static async Task SeedEstimatesAsync(ServerBMCDbContext db, CancellationToken ct)
    {
        if (await db.Estimates.AnyAsync(ct))
            return; // đã seed rồi

        var adminUserId = await db.Users
            .Where(u => u.Email == "admin@bmc.vn")
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);

        if (adminUserId == 0) return;

        // Tạo sample Estimate dựa trên dữ liệu Excel "Móng M02B"
        var estimate = new Estimate
        {
            ProjectName = "ĐẦU TƯ XÂY DỰNG KHU NHÀ Ơ BIỆT THỰ BT-A22, BT-C1",
            Category = "MÓNG",
            Location = "KHU ĐÔ THỊ SINH THÁI CHÁNH MỘ (GIAI ĐOẠN 1), PHƯỜNG CHÁNH MỘ, THÀNH PHỐ THỦ ĐẦU MỘT, TỈNH BÌNH DƯƠNG",
            Investor = "TỔNG CÔNG TY ĐẦU TƯ PHÁT TRIỂN NHÀ VÀ ĐÔ THỊ",
            Consultant = "CÔNG TY TNHH KIẾN TRÚC SCENE PLUS",
            Scope = "XÂY THÔ VÀ HOÀN THIỆN MẶT NGOÀI (LO BT-C1)",
            DocumentType = "M-02B",
            DocumentNumber = "BT-C1.05, BT-C1.06, BT-C1.08, BT-C1.09",
            TotalAmount = 754_741_000m,
            TotalAmountText = "Bảy trăm năm mươi tư triệu bảy trăm bốn mươi mốt nghìn đồng chẵn",
            CreatedBy = adminUserId
        };

        db.Estimates.Add(estimate);
        await db.SaveChangesAsync(ct);

        // Tạo sample Work Items (chỉ lưu thông tin cơ bản, không lưu Total)
        var workItems = new List<EstimateWorkItem>
        {
            new() { EstimateId = estimate.Id, Stt = 1, Code = "TT", Name = "Cung cấp Cọc bê tông dự ứng lực D300mm", Unit = "m", Quantity = 728m },
            new() { EstimateId = estimate.Id, Stt = 2, Code = "AC.26311", Name = "Ép cọc ứng bê tông cốt thép dự ứng lực bằng máy ép Robot thủy lực tự hành, đất cấp I, dùng khoan cọc 300mm", Unit = "100m", Quantity = 7.28m },
            new() { EstimateId = estimate.Id, Stt = 3, Code = "AC.26311", Name = "Ép âm cọc ứng bê tông cốt thép dự ứng lực bằng máy ép Robot thủy lực tự hành, đất cấp I, dùng khoan cọc 300mm", Unit = "100m", Quantity = 0.091m },
            new() { EstimateId = estimate.Id, Stt = 4, Code = "AI.11132", Name = "Gia cường cọc đàn ép âm", Unit = "tấn", Quantity = 0.0117m },
            new() { EstimateId = estimate.Id, Stt = 5, Code = "AC.29411", Name = "Nối cọc ứng bê tông cốt thép, dùng khoan cọc 300mm", Unit = "mối nối", Quantity = 52m },
            new() { EstimateId = estimate.Id, Stt = 6, Code = "SA.32111", Name = "Cắt đầu cọc D300", Unit = "m", Quantity = 12.2522m },
            new() { EstimateId = estimate.Id, Stt = 7, Code = "TT", Name = "Bốc xếp vận chuyển di dời bằng ô tô tự đổ 5T", Unit = "ca", Quantity = 1m },
            new() { EstimateId = estimate.Id, Stt = 8, Code = "AM.12502", Name = "Bốc xếp cọc bê tông đúc sẵn trọng lượng <= 5T bằng cần cẩu - bốc xếp xuống", Unit = "cấu kiện", Quantity = 78m },
            new() { EstimateId = estimate.Id, Stt = 9, Code = "AB.25311", Name = "Đào móng công trình, chiều rộng móng <= 20m, bằng máy đào 0,8m3, đất cấp I", Unit = "100m3", Quantity = 1.5811m },
            new() { EstimateId = estimate.Id, Stt = 10, Code = "AB.11351", Name = "Đào đất móng bằng thủ công, rộng > 3m, sâu <= 1m, đất cấp I", Unit = "m3", Quantity = 21.96m }
        };

        db.EstimateWorkItems.AddRange(workItems);
        await db.SaveChangesAsync(ct);

        // Add Work Item Details với đơn giá (Đơn giá → Thành tiền)
        var details = new List<WorkItemDetail>
        {
            // Item 1: Cung cấp Cọc bê tông D300mm
            // Thành tiền = 728 × 290,000 × 1.01 = 213,231,200
            new() { WorkItemId = workItems[0].Id, Category = "Vật liệu", Code = "TT", Name = "Cọc bê tông dự ứng lực D300mm", Unit = "m", Quantity = 1.01m, UnitPrice = 290_000m, Factor = 1.0m, TotalAmount = 1.01m * 290_000m },

            // Item 2: Ép cọc
            // Thành tiền NC = 7.28 × 2,103,633 = 15,312,461
            new() { WorkItemId = workItems[1].Id, Category = "Nhân công", Code = "AC.26311", Name = "Nhân công ép cọc", Unit = "công", Quantity = 7.28m, UnitPrice = 2_103_633m, Factor = 1.0m, TotalAmount = 7.28m * 2_103_633m },
            // Thành tiền M = 7.28 × 16,927,537 = 123,230,476
            new() { WorkItemId = workItems[1].Id, Category = "Máy", Code = "AC.26311", Name = "Máy ép cọc Robot", Unit = "ca", Quantity = 7.28m, UnitPrice = 16_927_537m, Factor = 1.0m, TotalAmount = 7.28m * 16_927_537m },

            // Item 3: Ép âm cọc
            // Thành tiền NC = 0.091 × 2,208,308 = 200,976
            new() { WorkItemId = workItems[2].Id, Category = "Nhân công", Code = "AC.26311", Name = "Nhân công ép âm cọc", Unit = "công", Quantity = 0.091m, UnitPrice = 2_208_308m, Factor = 1.0m, TotalAmount = 0.091m * 2_208_308m },
            // Thành tiền M = 0.091 × 17,773_626 = 1,617,400
            new() { WorkItemId = workItems[2].Id, Category = "Máy", Code = "AC.26311", Name = "Máy ép âm", Unit = "ca", Quantity = 0.091m, UnitPrice = 17_773_626m, Factor = 1.0m, TotalAmount = 0.091m * 17_773_626m },

            // Item 4: Gia cường cọc đàn ép âm
            // Thành tiền VL = 0.0117 × 18,530,000 = 216,801
            new() { WorkItemId = workItems[3].Id, Category = "Vật liệu", Code = "AI.11132", Name = "Vật liệu gia cường", Unit = "kg", Quantity = 11.7m, UnitPrice = 18_530m, Factor = 1.0m, TotalAmount = 11.7m * 18_530m },
            // Thành tiền NC = 0.0117 × 5,400,000 = 63,158
            new() { WorkItemId = workItems[3].Id, Category = "Nhân công", Code = "AI.11132", Name = "Nhân công gia cường", Unit = "công", Quantity = 0.0117m, UnitPrice = 5_400_000m, Factor = 1.0m, TotalAmount = 0.0117m * 5_400_000m },
            // Thành tiền M = 0.0117 × 3,955,000 = 46,269
            new() { WorkItemId = workItems[3].Id, Category = "Máy", Code = "AI.11132", Name = "Máy gia cường", Unit = "ca", Quantity = 0.0117m, UnitPrice = 3_955_000m, Factor = 1.0m, TotalAmount = 0.0117m * 3_955_000m },

            // Item 5: Nối cọc
            // Thành tiền VL = 52 × 39,375 = 2,047,500
            new() { WorkItemId = workItems[4].Id, Category = "Vật liệu", Code = "AC.29411", Name = "Vật liệu nối cọc", Unit = "bộ", Quantity = 52m, UnitPrice = 39_375m, Factor = 1.0m, TotalAmount = 52m * 39_375m },
            // Thành tiền NC = 52 × 252,000 = 13,104,000
            new() { WorkItemId = workItems[4].Id, Category = "Nhân công", Code = "AC.29411", Name = "Nhân công nối cọc", Unit = "công", Quantity = 52m, UnitPrice = 252_000m, Factor = 1.0m, TotalAmount = 52m * 252_000m },
            // Thành tiền M = 52 × 186,937 = 9,720,713
            new() { WorkItemId = workItems[4].Id, Category = "Máy", Code = "AC.29411", Name = "Máy nối cọc", Unit = "ca", Quantity = 52m, UnitPrice = 186_937m, Factor = 1.0m, TotalAmount = 52m * 186_937m },

            // Item 6: Cắt đầu cọc
            // Thành tiền NC = 12.2522 × 211,671 = 2,593,546
            new() { WorkItemId = workItems[5].Id, Category = "Nhân công", Code = "SA.32111", Name = "Nhân công cắt đầu cọc", Unit = "công", Quantity = 12.2522m, UnitPrice = 211_671m, Factor = 1.0m, TotalAmount = 12.2522m * 211_671m },
            // Thành tiền M = 12.2522 × 3,554 = 43,545
            new() { WorkItemId = workItems[5].Id, Category = "Máy", Code = "SA.32111", Name = "Máy cắt đầu cọc", Unit = "ca", Quantity = 12.2522m, UnitPrice = 3_554m, Factor = 1.0m, TotalAmount = 12.2522m * 3_554m },

            // Item 7: Bốc xếp vận chuyển
            // Thành tiền M = 1 × 1,570,071 = 1,570,071
            new() { WorkItemId = workItems[6].Id, Category = "Máy", Code = "TT", Name = "Ô tô tự đổ 5T", Unit = "ca", Quantity = 1m, UnitPrice = 1_570_071m, Factor = 1.0m, TotalAmount = 1m * 1_570_071m },

            // Item 8: Bốc xếp cọc
            // Thành tiền NC = 78 × 30,038 = 2,342,966
            new() { WorkItemId = workItems[7].Id, Category = "Nhân công", Code = "AM.12502", Name = "Nhân công bốc xếp", Unit = "công", Quantity = 78m, UnitPrice = 30_038m, Factor = 1.0m, TotalAmount = 78m * 30_038m },
            // Thành tiền M = 78 × 60,917 = 4,751,546
            new() { WorkItemId = workItems[7].Id, Category = "Máy", Code = "AM.12502", Name = "Cần cẩu", Unit = "ca", Quantity = 78m, UnitPrice = 60_917m, Factor = 1.0m, TotalAmount = 78m * 60_917m },

            // Item 9: Đào móng bằng máy
            // Thành tiền NC = 1.5811 × 305,834 = 483,566
            new() { WorkItemId = workItems[8].Id, Category = "Nhân công", Code = "AB.25311", Name = "Nhân công đào móng", Unit = "công", Quantity = 1.5811m, UnitPrice = 305_834m, Factor = 1.0m, TotalAmount = 1.5811m * 305_834m },
            // Thành tiền M = 1.5811 × 804,215 = 1,271,549
            new() { WorkItemId = workItems[8].Id, Category = "Máy", Code = "AB.25311", Name = "Máy đào 0.8m3", Unit = "ca", Quantity = 1.5811m, UnitPrice = 804_215m, Factor = 1.0m, TotalAmount = 1.5811m * 804_215m },

            // Item 10: Đào móng thủ công
            // Thành tiền NC = 21.96 × 125,659 = 2,758,474
            new() { WorkItemId = workItems[9].Id, Category = "Nhân công", Code = "AB.11351", Name = "Nhân công đào đất thủ công", Unit = "công", Quantity = 21.96m, UnitPrice = 125_659m, Factor = 1.0m, TotalAmount = 21.96m * 125_659m }
        };

        db.WorkItemDetails.AddRange(details);
        await db.SaveChangesAsync(ct);

        // Calculate totals for all work items (từ WorkItemDetails tính ra WorkItem)
        foreach (var wi in workItems)
        {
            CalculateWorkItemTotals(wi, db);
        }
        await db.SaveChangesAsync(ct);

        // Calculate cost summary
        CalculateCostSummary(estimate.Id, db);
        await db.SaveChangesAsync(ct);
    }

    private static void CalculateWorkItemTotals(EstimateWorkItem workItem, ServerBMCDbContext db)
    {
        var details = db.WorkItemDetails.Where(x => x.WorkItemId == workItem.Id).ToList();

        workItem.MaterialTotal = details.Where(x => x.Category == "Vật liệu").Sum(x => x.TotalAmount);
        workItem.LaborTotal = details.Where(x => x.Category == "Nhân công").Sum(x => x.TotalAmount);
        workItem.MachineTotal = details.Where(x => x.Category == "Máy").Sum(x => x.TotalAmount);
        workItem.TotalAmount = workItem.MaterialTotal + workItem.LaborTotal + workItem.MachineTotal;

        if (workItem.Quantity > 0)
        {
            workItem.MaterialUnitPrice = workItem.MaterialTotal / workItem.Quantity;
            workItem.LaborUnitPrice = workItem.LaborTotal / workItem.Quantity;
            workItem.MachineUnitPrice = workItem.MachineTotal / workItem.Quantity;
        }
    }

    private static void CalculateCostSummary(int estimateId, ServerBMCDbContext db)
    {
        var workItems = db.EstimateWorkItems.Where(x => x.EstimateId == estimateId).ToList();

        var summary = db.CostSummaries.FirstOrDefault(x => x.EstimateId == estimateId)
                      ?? new CostSummary { EstimateId = estimateId };

        summary.MaterialCost = workItems.Sum(x => x.MaterialTotal);
        summary.LaborCost = workItems.Sum(x => x.LaborTotal);
        summary.MachineCost = workItems.Sum(x => x.MachineTotal);
        summary.DirectCost = summary.MaterialCost + summary.LaborCost + summary.MachineCost;

        summary.GeneralCost = Math.Round(summary.DirectCost * summary.GeneralCostRate);
        summary.OverheadCost = Math.Round(summary.DirectCost * summary.OverheadCostRate);
        summary.UndeterminedCost = Math.Round(summary.DirectCost * summary.UndeterminedCostRate);
        summary.IndirectCost = summary.GeneralCost + summary.OverheadCost + summary.UndeterminedCost;

        summary.PreTaxIncome = Math.Round((summary.DirectCost + summary.IndirectCost) * summary.PreTaxIncomeRate);
        summary.PreTaxAmount = summary.DirectCost + summary.IndirectCost + summary.PreTaxIncome;

        summary.VatAmount = Math.Round(summary.PreTaxAmount * summary.VatRate);
        summary.PostTaxAmount = summary.PreTaxAmount + summary.VatAmount;
        summary.RoundedAmount = Math.Round(summary.PostTaxAmount);

        var estimate = db.Estimates.Find(estimateId);
        if (estimate != null)
        {
            estimate.TotalAmount = summary.RoundedAmount;
            estimate.TotalAmountText = NumberToText(summary.RoundedAmount);
        }

        if (summary.Id == 0)
            db.CostSummaries.Add(summary);
    }

    private static string NumberToText(decimal number)
    {
        if (number == 0) return "Không đồng";
        var units = new[] { "", "nghìn", "triệu", "tỷ" };
        var result = "";
        var numStr = ((long)number).ToString("N0");
        var parts = numStr.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            var partValue = long.Parse(parts[i]);
            var unitIndex = parts.Length - i - 1;
            if (partValue > 0)
                result += $"{partValue:N0} {units[unitIndex]} ";
        }
        return result.Trim() + " đồng";
    }
}
