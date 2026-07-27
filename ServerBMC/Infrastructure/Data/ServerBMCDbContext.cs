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
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
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
        b.Entity<Project>(e =>
        {
            e.ToTable("Projects");
            e.Property(x => x.ProjectCode).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.ProjectCode).IsUnique();
            e.HasIndex(x => x.Status);
            e.Property(x => x.ContractValue).HasPrecision(18, 2);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- ProjectLots
        b.Entity<ProjectLot>(e =>
        {
            e.ToTable("ProjectLots");
            e.HasIndex(x => new { x.ProjectId, x.LotCode }).IsUnique();
            e.HasIndex(x => x.ProjectId);
            e.Property(x => x.Area).HasPrecision(18, 4);
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
            e.HasOne(x => x.Category).WithMany(x => x.SubCategories)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- WorkItems
        b.Entity<WorkItem>(e =>
        {
            e.ToTable("WorkItems");
            e.HasIndex(x => new { x.SubCategoryId, x.ItemCode }).IsUnique();
            e.HasIndex(x => x.SubCategoryId);
            e.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            e.Property(x => x.StandardQuantity).HasPrecision(18, 6);
            e.Property(x => x.MaterialNorm).HasPrecision(18, 6);
            e.Property(x => x.LaborNorm).HasPrecision(18, 6);
            e.Property(x => x.MachineNorm).HasPrecision(18, 6);
            e.HasOne(x => x.SubCategory).WithMany(x => x.WorkItems)
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
            e.HasOne(x => x.WorkItem).WithMany(x => x.ActualCosts)
                .HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
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
        });

        // ---- Warnings
        b.Entity<Warning>(e =>
        {
            e.ToTable("Warnings");
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Resolver).WithMany().HasForeignKey(x => x.ResolvedBy).OnDelete(DeleteBehavior.SetNull);
        });

        // ---- PaymentPlans
        b.Entity<PaymentPlan>(e =>
        {
            e.ToTable("PaymentPlans");
            e.Property(x => x.PlanAmount).HasPrecision(18, 2);
            e.Property(x => x.ActualAmount).HasPrecision(18, 2);
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
    }
}