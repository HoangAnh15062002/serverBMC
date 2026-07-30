using Microsoft.EntityFrameworkCore;
using ServerBMC.Domain.Entities;

namespace ServerBMC.Infrastructure.Data;

public class ServerBMCDbContext : DbContext
{
    public ServerBMCDbContext(DbContextOptions<ServerBMCDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectLot> ProjectLots => Set<ProjectLot>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<SubCategory> SubCategories => Set<SubCategory>();
    public DbSet<ProjectWorkItem> ProjectWorkItems => Set<ProjectWorkItem>();
    public DbSet<UnitPrice> UnitPrices => Set<UnitPrice>();
    public DbSet<ActualCost> ActualCosts => Set<ActualCost>();
    public DbSet<AcceptedQuantity> AcceptedQuantities => Set<AcceptedQuantity>();
    public DbSet<Progress> Progresses => Set<Progress>();
    public DbSet<Warning> Warnings => Set<Warning>();
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportAttachment> ReportAttachments => Set<ReportAttachment>();
    public DbSet<ReportApproval> ReportApprovals => Set<ReportApproval>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    
    // Dự toán xây dựng
    public DbSet<EstimateCategory> EstimateCategories => Set<EstimateCategory>();
    public DbSet<Estimate> Estimates => Set<Estimate>();
    public DbSet<EstimateItem> EstimateItems => Set<EstimateItem>();
    public DbSet<EstimateItemDetail> EstimateItemDetails => Set<EstimateItemDetail>();
    public DbSet<CostSummary> CostSummaries => Set<CostSummary>();

    // Bảng tham chiếu (global)
    public DbSet<MaterialSummary> MaterialSummaries => Set<MaterialSummary>();
    public DbSet<LaborSummary> LaborSummaries => Set<LaborSummary>();
    public DbSet<MachineSummary> MachineSummaries => Set<MachineSummary>();
    public DbSet<MonthlyPrice> MonthlyPrices => Set<MonthlyPrice>();
    public DbSet<PriceInput> PriceInputs => Set<PriceInput>();
    public DbSet<MaterialNorm> MaterialNorms => Set<MaterialNorm>();



    protected override void OnModelCreating(ModelBuilder b)
    {
        // ---- Roles
        b.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        // ---- Users
        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        });

        // ---- UserRoles
        b.Entity<UserRole>(e =>
        {
            e.ToTable("UserRoles");
            e.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            e.HasOne(x => x.User).WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Projects
        // ---- Projects
        b.Entity<Project>(e =>
        {
            e.ToTable("Projects");
            e.Property(x => x.ProjectCode).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.ProjectCode).IsUnique();
            e.HasIndex(x => x.Status);
            e.Property(x => x.ContractValue).HasPrecision(18, 2);
            e.Property(x => x.TotalEstimateValue).HasPrecision(18, 2);
            e.Property(x => x.GuaranteeValue).HasPrecision(18, 2);
            e.Property(x => x.ContractNumber).HasMaxLength(100);
            e.Property(x => x.DesignUnit).HasMaxLength(200);
            e.Property(x => x.SupervisionUnit).HasMaxLength(200);
            e.Property(x => x.ProjectManager).HasMaxLength(100);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- ProjectLots
        b.Entity<ProjectLot>(e =>
        {
            e.ToTable("ProjectLots");
            e.HasIndex(x => new { x.ProjectId, x.LotCode }).IsUnique();
            e.HasIndex(x => x.ProjectId);
            e.Property(x => x.Area).HasPrecision(18, 4);
            e.Property(x => x.ContractValue).HasPrecision(18, 2);
            e.HasOne(x => x.Project).WithMany(x => x.Lots)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Categories
        b.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasIndex(x => new { x.ProjectLotId, x.CategoryCode }).IsUnique();
            e.HasIndex(x => x.ProjectLotId);
            e.Property(x => x.ProgressPercent).HasPrecision(5, 2);
            e.Property(x => x.Weight).HasPrecision(5, 2);
            e.Property(x => x.PlannedCost).HasPrecision(18, 2);
            e.HasOne(x => x.ProjectLot).WithMany(x => x.Categories)
                .HasForeignKey(x => x.ProjectLotId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- SubCategories
        b.Entity<SubCategory>(e =>
        {
            e.ToTable("SubCategories");
            e.HasIndex(x => new { x.CategoryId, x.SubCategoryCode }).IsUnique();
            e.HasIndex(x => x.CategoryId);
            e.Property(x => x.ProgressPercent).HasPrecision(5, 2);
            e.Property(x => x.Weight).HasPrecision(5, 2);
            e.Property(x => x.PlannedCost).HasPrecision(18, 2);
            e.HasOne(x => x.Category).WithMany(x => x.SubCategories)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- ProjectWorkItems (Đầu mục công tác trong dự án dự thầu)
        b.Entity<ProjectWorkItem>(e =>
        {
            e.ToTable("WorkItems");
            e.HasIndex(x => new { x.SubCategoryId, x.ItemCode }).IsUnique();
            e.HasIndex(x => x.SubCategoryId);
            e.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            e.Property(x => x.StandardQuantity).HasPrecision(18, 6);
            
            e.Property(x => x.ContractQuantity).HasPrecision(18, 6);
            e.Property(x => x.ContractUnitPrice).HasPrecision(18, 4);
            e.Property(x => x.BidMaterialPrice).HasPrecision(18, 4);
            e.Property(x => x.BidLaborPrice).HasPrecision(18, 4);
            e.Property(x => x.BidMachinePrice).HasPrecision(18, 4);
            e.Property(x => x.NormCode).HasMaxLength(50);

            e.HasOne(x => x.SubCategory).WithMany(x => x.ProjectWorkItems)
                .HasForeignKey(x => x.SubCategoryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- UnitPrices
        b.Entity<UnitPrice>(e =>
        {
            e.ToTable("UnitPrices");
            e.Property(x => x.PriceType).HasMaxLength(20).IsRequired();
            e.Property(x => x.UnitPriceValue).HasPrecision(18, 4);
            e.HasIndex(x => new { x.WorkItemId, x.PriceType, x.EffectiveFrom }).IsUnique();
            e.HasOne(x => x.WorkItem).WithMany(x => x.UnitPrices)
                .HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- ActualCosts
        b.Entity<ActualCost>(e =>
        {
            e.ToTable("ActualCosts");
            e.Property(x => x.CostType).HasMaxLength(20).IsRequired();
            e.Property(x => x.Quantity).HasPrecision(18, 6);
            e.Property(x => x.UnitPriceValue).HasPrecision(18, 4);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.WorkItemId);
            e.HasIndex(x => x.CostDate);
            e.HasIndex(x => x.CostType);
            e.HasIndex(x => x.MaterialSummaryId);
            e.HasIndex(x => x.LotId);
            e.HasIndex(x => x.CategoryId);
            e.HasOne(x => x.WorkItem).WithMany(x => x.ActualCosts)
                .HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MaterialSummary).WithMany().HasForeignKey(x => x.MaterialSummaryId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Lot).WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(x => x.ActualCosts).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- AcceptedQuantities
        b.Entity<AcceptedQuantity>(e =>
        {
            e.ToTable("AcceptedQuantities");
            e.Property(x => x.AcceptedQuantityValue).HasPrecision(18, 6);
            e.HasIndex(x => x.WorkItemId);
            e.HasOne(x => x.WorkItem).WithMany(x => x.AcceptedQuantities)
                .HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Progress
        b.Entity<Progress>(e =>
        {
            e.ToTable("Progress");
            e.HasIndex(x => new { x.CategoryId, x.ProgressDate }).IsUnique();
            e.Property(x => x.ProgressPercent).HasPrecision(5, 2);
            e.Property(x => x.PlannedPercent).HasPrecision(5, 2);
            e.Property(x => x.Variance).HasPrecision(5, 2);
            e.HasOne(x => x.Category).WithMany(x => x.Progresses)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Verifier).WithMany().HasForeignKey(x => x.VerifiedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Warnings
        b.Entity<Warning>(e =>
        {
            e.ToTable("Warnings");
            e.HasIndex(x => x.CategoryId);
            e.HasIndex(x => x.WorkItemId);
            e.HasIndex(x => x.LotId);
            e.HasOne(x => x.Project).WithMany(x => x.Warnings).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Resolver).WithMany().HasForeignKey(x => x.ResolvedBy).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Category).WithMany(x => x.Warnings).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.WorkItem).WithMany(x => x.Warnings).HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Lot).WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.SetNull);
        });

        // ---- PaymentPlans
        b.Entity<PaymentPlan>(e =>
        {
            e.ToTable("PaymentPlans");
            e.Property(x => x.PlanAmount).HasPrecision(18, 2);
            e.Property(x => x.ActualAmount).HasPrecision(18, 2);
            e.Property(x => x.PaymentType).HasMaxLength(50).IsRequired();
            e.Property(x => x.PaymentMethod).HasMaxLength(50);
            e.Property(x => x.BankAccount).HasMaxLength(100);
            e.HasOne(x => x.Project).WithMany(x => x.PaymentPlans)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Reports
        b.Entity<Report>(e =>
        {
            e.ToTable("Reports");
            e.Property(x => x.ReportCode).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.ReportCode).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.ReportType);
            e.HasOne(x => x.Project).WithMany(x => x.Reports)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- ReportAttachments
        b.Entity<ReportAttachment>(e =>
        {
            e.ToTable("ReportAttachments");
            e.HasOne(x => x.Report).WithMany(x => x.Attachments)
                .HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Uploader).WithMany().HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- ReportApprovals
        b.Entity<ReportApproval>(e =>
        {
            e.ToTable("ReportApprovals");
            e.HasOne(x => x.Report).WithMany(x => x.Approvals)
                .HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Approver).WithMany().HasForeignKey(x => x.ApproverId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- AuditLogs
        b.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CreatedAt);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        // ---- EstimateCategories (Hạng mục dự toán)
        b.Entity<EstimateCategory>(e =>
        {
            e.ToTable("EstimateCategories");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.ProjectLotId);
            e.HasOne(x => x.Project).WithMany()
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ProjectLot).WithMany()
                .HasForeignKey(x => x.ProjectLotId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Estimates (Dự toán xây dựng)
        b.Entity<Estimate>(e =>
        {
            e.ToTable("Estimates");
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.ProjectLotId);
            e.HasIndex(x => x.EstimateCategoryId);
            e.HasIndex(x => x.CreatedAt);
            e.HasOne(x => x.Project).WithMany(x => x.Estimates)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ProjectLot).WithMany(x => x.Estimates)
                .HasForeignKey(x => x.ProjectLotId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EstimateCategory).WithMany(x => x.Estimates)
                .HasForeignKey(x => x.EstimateCategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Approver).WithMany().HasForeignKey(x => x.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CostSummary).WithOne(x => x.Estimate)
                .HasForeignKey<CostSummary>(x => x.EstimateId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---- EstimateItems (Giá tổng hợp)
        b.Entity<EstimateItem>(e =>
        {
            e.ToTable("EstimateWorkItems");
            e.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            e.Property(x => x.Quantity).HasPrecision(18, 6);
            e.Property(x => x.MaterialUnitPrice).HasPrecision(18, 4);
            e.Property(x => x.LaborUnitPrice).HasPrecision(18, 4);
            e.Property(x => x.MachineUnitPrice).HasPrecision(18, 4);
            e.Property(x => x.MaterialTotal).HasPrecision(18, 2);
            e.Property(x => x.LaborTotal).HasPrecision(18, 2);
            e.Property(x => x.MachineTotal).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.EstimateId);
            e.HasOne(x => x.Estimate).WithMany(x => x.Items)
                .HasForeignKey(x => x.EstimateId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---- EstimateItemDetails (Đơn giá chi tiết)
        b.Entity<EstimateItemDetail>(e =>
        {
            e.ToTable("EstimateItemDetails");
            e.Property(x => x.DetailType).HasMaxLength(20).IsRequired();
            e.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            e.Property(x => x.Quantity).HasPrecision(18, 6);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.Factor).HasPrecision(8, 4);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.FuelCost).HasPrecision(18, 4);
            e.Property(x => x.EnergyCost).HasPrecision(18, 4);
            e.Property(x => x.OperatorLaborCost).HasPrecision(18, 4);
            e.Property(x => x.DepreciationCost).HasPrecision(18, 4);
            e.Property(x => x.RepairCost).HasPrecision(18, 4);
            e.HasIndex(x => x.EstimateItemId);
            e.HasIndex(x => x.MaterialSummaryId);
            e.HasIndex(x => x.LaborSummaryId);
            e.HasIndex(x => x.MachineSummaryId);
            e.HasOne(x => x.EstimateItem).WithMany(x => x.Details)
                .HasForeignKey(x => x.EstimateItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MaterialSummary).WithMany().HasForeignKey(x => x.MaterialSummaryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.LaborSummary).WithMany().HasForeignKey(x => x.LaborSummaryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MachineSummary).WithMany().HasForeignKey(x => x.MachineSummaryId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- MaterialSummaries
        b.Entity<MaterialSummary>(e =>
        {
            e.ToTable("MaterialSummaries");
            e.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            e.Property(x => x.AveragePrice).HasPrecision(18, 4);
            e.Property(x => x.Factor).HasPrecision(5, 4);
            e.Property(x => x.CarFare).HasPrecision(18, 4);
            e.Property(x => x.DeliveredPrice).HasPrecision(18, 4);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- LaborSummaries
        b.Entity<LaborSummary>(e =>
        {
            e.ToTable("LaborSummaries");
            e.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            e.Property(x => x.SalaryFactor).HasPrecision(5, 2);
            e.Property(x => x.AverageLaborPrice).HasPrecision(18, 4);
            e.Property(x => x.AverageSalaryFactor).HasPrecision(5, 2);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- MachineSummaries
        b.Entity<MachineSummary>(e =>
        {
            e.ToTable("MachineSummaries");
            e.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            e.Property(x => x.FuelCost).HasPrecision(18, 4);
            e.Property(x => x.EnergyCost).HasPrecision(18, 4);
            e.Property(x => x.OperatorLaborCost).HasPrecision(18, 4);
            e.Property(x => x.DepreciationCost).HasPrecision(18, 4);
            e.Property(x => x.RepairCost).HasPrecision(18, 4);
            e.Property(x => x.TotalUnitCost).HasPrecision(18, 4);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- MonthlyPrices
        b.Entity<MonthlyPrice>(e =>
        {
            e.ToTable("MonthlyPrices");
            e.HasIndex(x => x.EffectiveMonth);
            e.HasIndex(x => new { x.Code, x.EffectiveMonth }).IsUnique();
            e.Property(x => x.EffectiveMonth).HasMaxLength(7).IsRequired();
            e.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            e.Property(x => x.MonthlyPriceValue).HasPrecision(18, 4);
            e.Property(x => x.Factor).HasPrecision(5, 4);
            e.Property(x => x.MainPrice).HasPrecision(18, 4);
            e.Property(x => x.PriceAfterVat).HasPrecision(18, 4);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- PriceInputs
        b.Entity<PriceInput>(e =>
        {
            e.ToTable("PriceInputs");
            e.HasIndex(x => x.EffectiveMonth);
            e.HasIndex(x => x.InputType);
            e.Property(x => x.EffectiveMonth).HasMaxLength(7).IsRequired();
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- MaterialNorms
        b.Entity<MaterialNorm>(e =>
        {
            e.ToTable("MaterialNorms");
            e.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            e.Property(x => x.Quantity).HasPrecision(18, 6);
            e.Property(x => x.MaterialNormValue).HasPrecision(18, 6);
            e.Property(x => x.LaborNormValue).HasPrecision(18, 6);
            e.Property(x => x.MachineNormValue).HasPrecision(18, 6);
            e.Property(x => x.Factor).HasPrecision(5, 4);
            e.Property(x => x.MaterialLossQuantity).HasPrecision(18, 6);
            e.Property(x => x.LaborLossQuantity).HasPrecision(18, 6);
            e.Property(x => x.MachineLossQuantity).HasPrecision(18, 6);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- CostSummaries
        b.Entity<CostSummary>(e =>
        {
            e.ToTable("CostSummaries");
            e.HasIndex(x => x.EstimateId).IsUnique();
            
            // Rate columns need precision
            e.Property(x => x.GeneralCostRate).HasPrecision(5, 4);
            e.Property(x => x.OverheadCostRate).HasPrecision(5, 4);
            e.Property(x => x.UndeterminedCostRate).HasPrecision(5, 4);
            e.Property(x => x.PreTaxIncomeRate).HasPrecision(5, 4);
            e.Property(x => x.VatRate).HasPrecision(5, 4);
        });
    }
}