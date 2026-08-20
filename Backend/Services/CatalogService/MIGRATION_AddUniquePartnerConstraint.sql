-- ============================================================================
-- MIGRATION: Add Unique Constraint for Partner Restaurant
-- Purpose: Ensure each partner can only have ONE active restaurant
-- Date: 2026-05-03
-- ============================================================================

-- Step 1: Check for existing violations (partners with multiple active restaurants)
-- This query will show if there are any partners that currently have multiple active restaurants
SELECT 
    PartnerUserId,
    COUNT(*) as ActiveRestaurantCount,
    STRING_AGG(CAST(Id AS NVARCHAR(MAX)), ', ') as RestaurantIds
FROM Restaurants
WHERE IsDeleted = 0
GROUP BY PartnerUserId
HAVING COUNT(*) > 1;

-- If the above query returns any rows, you need to manually resolve the conflicts before proceeding
-- Options:
-- 1. Soft delete all but one restaurant for each partner
-- 2. Contact the partners to decide which restaurant to keep

-- Step 2: Create a unique filtered index
-- This ensures only ONE active (non-deleted) restaurant per partner
-- Deleted restaurants are excluded from the constraint
CREATE UNIQUE NONCLUSTERED INDEX IX_Restaurants_PartnerUserId_Active
ON Restaurants(PartnerUserId)
WHERE IsDeleted = 0;

-- Step 3: Verify the constraint
-- This should return 0 rows if the constraint is working
SELECT 
    PartnerUserId,
    COUNT(*) as ActiveRestaurantCount
FROM Restaurants
WHERE IsDeleted = 0
GROUP BY PartnerUserId
HAVING COUNT(*) > 1;

-- ============================================================================
-- ROLLBACK (if needed)
-- ============================================================================
-- DROP INDEX IX_Restaurants_PartnerUserId_Active ON Restaurants;
-- ============================================================================

PRINT '✅ Migration completed: Unique constraint added for Partner-Restaurant relationship';
PRINT '⚠️  Each partner can now only have ONE active restaurant';
PRINT '✅ Deleted restaurants are excluded from this constraint';
