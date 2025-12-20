-- Migration: Create promotion_usage_audit table
-- Schema: amesa_lottery
-- Purpose: Track promotion usage when discount is applied but usage tracking fails

CREATE TABLE IF NOT EXISTS amesa_lottery.promotion_usage_audit (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id UUID NOT NULL,
    user_id UUID NOT NULL,
    promotion_code VARCHAR(50) NOT NULL,
    discount_amount DECIMAL(10,2) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    resolved_at TIMESTAMP,
    resolution_notes VARCHAR(1000),
    resolved_by_user_id UUID
);

-- Indexes for efficient querying
CREATE INDEX IF NOT EXISTS idx_promotion_audit_transaction 
    ON amesa_lottery.promotion_usage_audit(transaction_id);
    
CREATE INDEX IF NOT EXISTS idx_promotion_audit_user 
    ON amesa_lottery.promotion_usage_audit(user_id);
    
CREATE INDEX IF NOT EXISTS idx_promotion_audit_status 
    ON amesa_lottery.promotion_usage_audit(status);
    
CREATE INDEX IF NOT EXISTS idx_promotion_audit_created 
    ON amesa_lottery.promotion_usage_audit(created_at);
    
CREATE INDEX IF NOT EXISTS idx_promotion_audit_status_created 
    ON amesa_lottery.promotion_usage_audit(status, created_at);

-- Add comment
COMMENT ON TABLE amesa_lottery.promotion_usage_audit IS 
    'Audit log for promotion usage tracking failures. Created when discount is applied but usage cannot be recorded.';

COMMENT ON COLUMN amesa_lottery.promotion_usage_audit.status IS 
    'Status: Pending, Resolved, Reversed';


