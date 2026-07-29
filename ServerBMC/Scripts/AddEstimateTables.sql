-- Add Estimate Tables to existing database
-- Run this script manually in SQL Server

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Estimates')
BEGIN
    CREATE TABLE Estimates (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProjectName NVARCHAR(200) NOT NULL,
        Category NVARCHAR(100),
        Location NVARCHAR(500),
        Investor NVARCHAR(200),
        Consultant NVARCHAR(200),
        Scope NVARCHAR(500),
        DocumentType NVARCHAR(50) DEFAULT 'M-02B',
        DocumentNumber NVARCHAR(50),
        DocumentDate NVARCHAR(50),
        TotalAmount DECIMAL(18,2) DEFAULT 0,
        TotalAmountText NVARCHAR(200),
        CreatedBy INT NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EstimateWorkItems')
BEGIN
    CREATE TABLE EstimateWorkItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EstimateId INT NOT NULL,
        Stt INT DEFAULT 0,
        Code NVARCHAR(50),
        Name NVARCHAR(500),
        Unit NVARCHAR(20) NOT NULL,
        Quantity DECIMAL(18,6) DEFAULT 0,
        MaterialUnitPrice DECIMAL(18,4) DEFAULT 0,
        LaborUnitPrice DECIMAL(18,4) DEFAULT 0,
        MachineUnitPrice DECIMAL(18,4) DEFAULT 0,
        MaterialTotal DECIMAL(18,2) DEFAULT 0,
        LaborTotal DECIMAL(18,2) DEFAULT 0,
        MachineTotal DECIMAL(18,2) DEFAULT 0,
        TotalAmount DECIMAL(18,2) DEFAULT 0,
        CONSTRAINT FK_EstimateWorkItems_Estimates FOREIGN KEY (EstimateId) REFERENCES Estimates(Id) ON DELETE CASCADE
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkItemDetails')
BEGIN
    CREATE TABLE WorkItemDetails (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        WorkItemId INT NOT NULL,
        Category NVARCHAR(20) NOT NULL,
        Code NVARCHAR(50),
        Name NVARCHAR(300),
        Unit NVARCHAR(20) NOT NULL,
        Quantity DECIMAL(18,6) DEFAULT 0,
        UnitPrice DECIMAL(18,4) DEFAULT 0,
        Factor DECIMAL(8,4) DEFAULT 1.0,
        TotalAmount DECIMAL(18,2) DEFAULT 0,
        CONSTRAINT FK_WorkItemDetails_EstimateWorkItems FOREIGN KEY (WorkItemId) REFERENCES EstimateWorkItems(Id) ON DELETE CASCADE
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CostSummaries')
BEGIN
    CREATE TABLE CostSummaries (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EstimateId INT NOT NULL UNIQUE,
        MaterialCost DECIMAL(18,2) DEFAULT 0,
        LaborCost DECIMAL(18,2) DEFAULT 0,
        MachineCost DECIMAL(18,2) DEFAULT 0,
        DirectCost DECIMAL(18,2) DEFAULT 0,
        GeneralCostRate DECIMAL(5,4) DEFAULT 0.067,
        GeneralCost DECIMAL(18,2) DEFAULT 0,
        OverheadCostRate DECIMAL(5,4) DEFAULT 0.01,
        OverheadCost DECIMAL(18,2) DEFAULT 0,
        UndeterminedCostRate DECIMAL(5,4) DEFAULT 0.025,
        UndeterminedCost DECIMAL(18,2) DEFAULT 0,
        IndirectCost DECIMAL(18,2) DEFAULT 0,
        PreTaxIncomeRate DECIMAL(5,4) DEFAULT 0.055,
        PreTaxIncome DECIMAL(18,2) DEFAULT 0,
        PreTaxAmount DECIMAL(18,2) DEFAULT 0,
        VatRate DECIMAL(5,4) DEFAULT 0.10,
        VatAmount DECIMAL(18,2) DEFAULT 0,
        PostTaxAmount DECIMAL(18,2) DEFAULT 0,
        RoundedAmount DECIMAL(18,2) DEFAULT 0,
        CONSTRAINT FK_CostSummaries_Estimates FOREIGN KEY (EstimateId) REFERENCES Estimates(Id) ON DELETE CASCADE
    );
END
