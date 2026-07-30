-- ================================================================
-- Migration: NewEstimateReferenceTables
-- Mục đích: Tạo các bảng mới cho Estimate Schema v2
-- Ghi chú: Không xoá data cũ trong Estimates, EstimateWorkItems
-- ================================================================

BEGIN TRANSACTION;

-- ================================================================
-- 1. ALTER TABLE Estimates — thêm EstimateCategoryId
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Estimates') AND name = 'EstimateCategoryId')
BEGIN
    ALTER TABLE Estimates ADD EstimateCategoryId int NULL;
END

-- ================================================================
-- 2. RENAME WorkItemDetails → EstimateItemDetails
-- ================================================================
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WorkItemDetails' AND type = 'U')
   AND NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EstimateItemDetails' AND type = 'U')
BEGIN
    EXEC sp_rename 'WorkItemDetails', 'EstimateItemDetails';
END

-- ================================================================
-- 3. RENAME WorkItemId → EstimateItemId in EstimateItemDetails
-- ================================================================
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EstimateItemDetails') AND name = 'WorkItemId')
BEGIN
    EXEC sp_rename 'EstimateItemDetails.WorkItemId', 'EstimateItemId', 'COLUMN';
END

-- ================================================================
-- 4. CREATE TABLE EstimateCategories
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EstimateCategories' AND type = 'U')
BEGIN
    CREATE TABLE EstimateCategories (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProjectId int NULL,
        Name nvarchar(100) NOT NULL,
        Description nvarchar(500) NULL,
        SortOrder int NOT NULL DEFAULT 0,
        Status nvarchar(50) NOT NULL DEFAULT N'Hoạt động',
        CreatedBy int NOT NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_EstimateCategories_Projects FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE SET NULL,
        CONSTRAINT FK_EstimateCategories_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
    );
    CREATE INDEX IX_EstimateCategories_ProjectId ON EstimateCategories(ProjectId);
    CREATE INDEX IX_EstimateCategories_CreatedBy ON EstimateCategories(CreatedBy);
END

-- ================================================================
-- 5. CREATE TABLE MaterialSummaries
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MaterialSummaries' AND type = 'U')
BEGIN
    CREATE TABLE MaterialSummaries (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(300) NOT NULL,
        Unit nvarchar(20) NOT NULL,
        Quantity decimal(18,6) NOT NULL DEFAULT 0,
        AveragePrice decimal(18,4) NOT NULL DEFAULT 0,
        Factor decimal(5,4) NOT NULL DEFAULT 1.0,
        CarFare decimal(18,4) NOT NULL DEFAULT 0,
        DeliveredPrice decimal(18,4) NOT NULL DEFAULT 0,
        TotalAmount decimal(18,2) NOT NULL DEFAULT 0,
        Notes nvarchar(200) NULL,
        CreatedBy int NOT NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_MaterialSummaries_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
    );
    CREATE INDEX IX_MaterialSummaries_CreatedBy ON MaterialSummaries(CreatedBy);
END

-- ================================================================
-- 6. CREATE TABLE LaborSummaries
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LaborSummaries' AND type = 'U')
BEGIN
    CREATE TABLE LaborSummaries (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(300) NOT NULL,
        Unit nvarchar(20) NOT NULL,
        SalaryFactor decimal(5,2) NOT NULL DEFAULT 0,
        AverageLaborPrice decimal(18,4) NOT NULL DEFAULT 0,
        AverageSalaryFactor decimal(5,2) NOT NULL DEFAULT 0,
        UnitPrice decimal(18,4) NOT NULL DEFAULT 0,
        Notes nvarchar(200) NULL,
        CreatedBy int NOT NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_LaborSummaries_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
    );
    CREATE INDEX IX_LaborSummaries_CreatedBy ON LaborSummaries(CreatedBy);
END

-- ================================================================
-- 7. CREATE TABLE MachineSummaries
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MachineSummaries' AND type = 'U')
BEGIN
    CREATE TABLE MachineSummaries (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(300) NOT NULL,
        Unit nvarchar(20) NOT NULL,
        FuelCost decimal(18,4) NOT NULL DEFAULT 0,
        EnergyCost decimal(18,4) NOT NULL DEFAULT 0,
        OperatorLaborCost decimal(18,4) NOT NULL DEFAULT 0,
        DepreciationCost decimal(18,4) NOT NULL DEFAULT 0,
        RepairCost decimal(18,4) NOT NULL DEFAULT 0,
        TotalUnitCost decimal(18,4) NOT NULL DEFAULT 0,
        Notes nvarchar(200) NULL,
        CreatedBy int NOT NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_MachineSummaries_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
    );
    CREATE INDEX IX_MachineSummaries_CreatedBy ON MachineSummaries(CreatedBy);
END

-- ================================================================
-- 8. CREATE TABLE MonthlyPrices
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MonthlyPrices' AND type = 'U')
BEGIN
    CREATE TABLE MonthlyPrices (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EffectiveMonth nvarchar(7) NOT NULL,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(300) NOT NULL,
        Unit nvarchar(20) NOT NULL,
        MonthlyPriceValue decimal(18,4) NOT NULL DEFAULT 0,
        Factor decimal(5,4) NOT NULL DEFAULT 1.0,
        MainPrice decimal(18,4) NOT NULL DEFAULT 0,
        PriceAfterVat decimal(18,4) NOT NULL DEFAULT 0,
        StandardCode nvarchar(50) NULL,
        Notes nvarchar(200) NULL,
        CreatedBy int NOT NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_MonthlyPrices_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
    );
    CREATE INDEX IX_MonthlyPrices_EffectiveMonth ON MonthlyPrices(EffectiveMonth);
    CREATE INDEX IX_MonthlyPrices_CreatedBy ON MonthlyPrices(CreatedBy);
END

-- ================================================================
-- 9. CREATE TABLE PriceInputs
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PriceInputs' AND type = 'U')
BEGIN
    CREATE TABLE PriceInputs (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EffectiveMonth nvarchar(7) NOT NULL,
        Name nvarchar(200) NOT NULL,
        Value decimal(18,4) NOT NULL DEFAULT 0,
        Unit nvarchar(20) NULL,
        InputType nvarchar(50) NULL,
        Notes nvarchar(200) NULL,
        CreatedBy int NOT NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_PriceInputs_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
    );
    CREATE INDEX IX_PriceInputs_EffectiveMonth ON PriceInputs(EffectiveMonth);
    CREATE INDEX IX_PriceInputs_InputType ON PriceInputs(InputType);
    CREATE INDEX IX_PriceInputs_CreatedBy ON PriceInputs(CreatedBy);
END

-- ================================================================
-- 10. CREATE TABLE MaterialNorms
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MaterialNorms' AND type = 'U')
BEGIN
    CREATE TABLE MaterialNorms (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code nvarchar(50) NOT NULL,
        WorkName nvarchar(300) NOT NULL,
        Unit nvarchar(20) NOT NULL,
        Quantity decimal(18,6) NOT NULL DEFAULT 0,
        MaterialNormValue decimal(18,6) NOT NULL DEFAULT 0,
        LaborNormValue decimal(18,6) NOT NULL DEFAULT 0,
        MachineNormValue decimal(18,6) NOT NULL DEFAULT 0,
        Factor decimal(5,4) NOT NULL DEFAULT 1.0,
        MaterialLossQuantity decimal(18,6) NOT NULL DEFAULT 0,
        LaborLossQuantity decimal(18,6) NOT NULL DEFAULT 0,
        MachineLossQuantity decimal(18,6) NOT NULL DEFAULT 0,
        Notes nvarchar(200) NULL,
        CreatedBy int NOT NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_MaterialNorms_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
    );
    CREATE INDEX IX_MaterialNorms_CreatedBy ON MaterialNorms(CreatedBy);
END

-- ================================================================
-- 11. CREATE TABLE ItemMaterialDetails
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ItemMaterialDetails' AND type = 'U')
BEGIN
    CREATE TABLE ItemMaterialDetails (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ItemDetailId int NOT NULL,
        MaterialSummaryId int NULL,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(300) NOT NULL,
        Unit nvarchar(20) NOT NULL,
        Quantity decimal(18,6) NOT NULL DEFAULT 0,
        UnitPrice decimal(18,4) NOT NULL DEFAULT 0,
        Factor decimal(5,4) NOT NULL DEFAULT 1.0,
        TotalAmount decimal(18,2) NOT NULL DEFAULT 0,
        CONSTRAINT FK_ItemMaterialDetails_EstimateItemDetails FOREIGN KEY (ItemDetailId)
            REFERENCES EstimateItemDetails(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ItemMaterialDetails_MaterialSummaries FOREIGN KEY (MaterialSummaryId)
            REFERENCES MaterialSummaries(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_ItemMaterialDetails_ItemDetailId ON ItemMaterialDetails(ItemDetailId);
    CREATE INDEX IX_ItemMaterialDetails_MaterialSummaryId ON ItemMaterialDetails(MaterialSummaryId);
END

-- ================================================================
-- 12. CREATE TABLE ItemLaborDetails
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ItemLaborDetails' AND type = 'U')
BEGIN
    CREATE TABLE ItemLaborDetails (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ItemDetailId int NOT NULL,
        LaborSummaryId int NULL,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(300) NOT NULL,
        Unit nvarchar(20) NOT NULL,
        Quantity decimal(18,6) NOT NULL DEFAULT 0,
        UnitPrice decimal(18,4) NOT NULL DEFAULT 0,
        Factor decimal(5,4) NOT NULL DEFAULT 1.0,
        TotalAmount decimal(18,2) NOT NULL DEFAULT 0,
        CONSTRAINT FK_ItemLaborDetails_EstimateItemDetails FOREIGN KEY (ItemDetailId)
            REFERENCES EstimateItemDetails(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ItemLaborDetails_LaborSummaries FOREIGN KEY (LaborSummaryId)
            REFERENCES LaborSummaries(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_ItemLaborDetails_ItemDetailId ON ItemLaborDetails(ItemDetailId);
    CREATE INDEX IX_ItemLaborDetails_LaborSummaryId ON ItemLaborDetails(LaborSummaryId);
END

-- ================================================================
-- 13. CREATE TABLE ItemMachineDetails
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ItemMachineDetails' AND type = 'U')
BEGIN
    CREATE TABLE ItemMachineDetails (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ItemDetailId int NOT NULL,
        MachineSummaryId int NULL,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(300) NOT NULL,
        Unit nvarchar(20) NOT NULL,
        Quantity decimal(18,6) NOT NULL DEFAULT 0,
        UnitPrice decimal(18,4) NOT NULL DEFAULT 0,
        Factor decimal(5,4) NOT NULL DEFAULT 1.0,
        TotalAmount decimal(18,2) NOT NULL DEFAULT 0,
        FuelCost decimal(18,4) NOT NULL DEFAULT 0,
        EnergyCost decimal(18,4) NOT NULL DEFAULT 0,
        OperatorLaborCost decimal(18,4) NOT NULL DEFAULT 0,
        DepreciationCost decimal(18,4) NOT NULL DEFAULT 0,
        RepairCost decimal(18,4) NOT NULL DEFAULT 0,
        CONSTRAINT FK_ItemMachineDetails_EstimateItemDetails FOREIGN KEY (ItemDetailId)
            REFERENCES EstimateItemDetails(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ItemMachineDetails_MachineSummaries FOREIGN KEY (MachineSummaryId)
            REFERENCES MachineSummaries(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_ItemMachineDetails_ItemDetailId ON ItemMachineDetails(ItemDetailId);
    CREATE INDEX IX_ItemMachineDetails_MachineSummaryId ON ItemMachineDetails(MachineSummaryId);
END

-- ================================================================
-- 14. Cập nhật FK EstimateItemDetails → EstimateWorkItems
-- ================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_EstimateItemDetails_EstimateWorkItems_EstimateItemId'
    AND parent_object_id = OBJECT_ID('EstimateItemDetails')
)
BEGIN
    -- Xoá FK cũ nếu có
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WorkItemDetails_EstimateWorkItems_WorkItemId')
    BEGIN
        ALTER TABLE EstimateItemDetails DROP CONSTRAINT FK_WorkItemDetails_EstimateWorkItems_WorkItemId;
    END

    ALTER TABLE EstimateItemDetails
        ADD CONSTRAINT FK_EstimateItemDetails_EstimateWorkItems_EstimateItemId
        FOREIGN KEY (EstimateItemId) REFERENCES EstimateWorkItems(Id) ON DELETE CASCADE;

    CREATE INDEX IX_EstimateItemDetails_EstimateItemId ON EstimateItemDetails(EstimateItemId);
END

-- ================================================================
-- 15. Thêm FK Estimates → EstimateCategories
-- ================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Estimates_EstimateCategories_EstimateCategoryId'
    AND parent_object_id = OBJECT_ID('Estimates')
)
BEGIN
    ALTER TABLE Estimates
        ADD CONSTRAINT FK_Estimates_EstimateCategories_EstimateCategoryId
        FOREIGN KEY (EstimateCategoryId) REFERENCES EstimateCategories(Id) ON DELETE RESTRICT;
END

COMMIT TRANSACTION;
PRINT 'Migration NewEstimateReferenceTables completed successfully.';
