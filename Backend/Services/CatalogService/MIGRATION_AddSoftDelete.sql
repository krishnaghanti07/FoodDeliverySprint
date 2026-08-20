-- ═══════════════════════════════════════════════════════════════════════════
-- Migration: Add Soft Delete to Restaurants
-- Service: CatalogService
-- Date: 2026-05-03
-- Description: Adds soft delete fields to Restaurants table
-- ═══════════════════════════════════════════════════════════════════════════

USE FoodDeliveryCatalog;
GO

-- Add soft delete fields to Restaurants table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Restaurants]') AND name = 'IsDeleted')
BEGIN
    ALTER TABLE [dbo].[Restaurants] ADD 
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedBy] UNIQUEIDENTIFIER NULL,
        [DeletedAt] DATETIME2 NULL,
        [DeletionReason] NVARCHAR(500) NULL;
    
    PRINT 'Added soft delete fields to Restaurants table';
END
ELSE
BEGIN
    PRINT 'Soft delete fields already exist in Restaurants table';
END
GO

-- Create index for soft delete queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Restaurants_IsDeleted' AND object_id = OBJECT_ID(N'[dbo].[Restaurants]'))
BEGIN
    CREATE INDEX IX_Restaurants_IsDeleted ON [dbo].[Restaurants]([IsDeleted]) INCLUDE ([IsApproved], [IsOpen]);
    PRINT 'Created index IX_Restaurants_IsDeleted';
END
GO

PRINT 'Migration completed successfully!';
GO
