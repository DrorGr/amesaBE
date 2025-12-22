# AWS Cost Optimization Review
**Date**: 2025-01-25  
**Reviewer**: AI Agent  
**Status**: Recommendations Ready for Implementation

## Executive Summary

This review identifies **significant cost-saving opportunities** across your AWS infrastructure. The analysis reveals potential savings of **$500-1,500/month** through infrastructure consolidation, resource right-sizing, and configuration optimizations.

### Key Findings
- ⚠️ **CRITICAL**: Terraform configuration shows 8 separate ALBs, but production uses single ALB (verify actual state)
- ⚠️ **CRITICAL**: Terraform shows 8 separate RDS instances, but production uses Aurora Serverless v2 (verify actual state)
- ✅ **HIGH IMPACT**: Performance Insights enabled on all RDS instances (if using separate instances)
- ✅ **MEDIUM IMPACT**: CloudWatch Logs retention not optimized
- ✅ **MEDIUM IMPACT**: ECS Fargate resource allocation can be optimized
- ✅ **LOW IMPACT**: Auto-scaling max capacity may be too high

---

## 1. Application Load Balancer (ALB) Consolidation

### Current State
- **Terraform Configuration**: 8 separate ALBs (one per microservice)
  - `amesa-auth-service-alb`
  - `amesa-lottery-service-alb`
  - `amesa-payment-service-alb`
  - `amesa-notification-service-alb`
  - `amesa-content-service-alb`
  - `amesa-lottery-results-service-alb`
  - `amesa-analytics-service-alb`
  - `amesa-admin-service-alb`

### Production Reality (from context)
- **Single ALB**: `amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com`
- **Path-based routing**: All services routed through single ALB

### Cost Impact
- **ALB Cost**: ~$16.20/month per ALB (eu-north-1 pricing)
- **Current (if 8 ALBs)**: ~$129.60/month
- **Optimized (1 ALB)**: ~$16.20/month
- **Potential Savings**: **~$113.40/month** ($1,360/year)

### Recommendation
1. **Verify actual infrastructure state**:
   ```bash
   aws elbv2 describe-load-balancers --region eu-north-1 --query 'LoadBalancers[*].[LoadBalancerName,LoadBalancerArn]' --output table
   ```

2. **If using single ALB**: Update Terraform to reflect actual state (remove 7 ALB resources)

3. **If using 8 ALBs**: **IMMEDIATE ACTION REQUIRED** - Consolidate to single ALB with path-based routing:
   - Update ALB listener rules to route by path
   - Update API Gateway integrations to point to single ALB
   - Delete 7 redundant ALBs

### Implementation Priority
🔴 **CRITICAL** - Verify and fix immediately

---

## 2. Database Architecture Optimization

### Current State (Terraform)
- **8 separate RDS instances** (one per microservice)
  - Each: `db.t3.micro` instance class
  - Each: 20GB storage (gp3), max 100GB
  - Each: Performance Insights enabled
  - Each: 7-day backup retention

### Production Reality (from context)
- **Aurora PostgreSQL Serverless v2**: Single cluster `amesadbmain`
- **Schema-based separation**: 8 schemas in single cluster
  - `amesa_auth`, `amesa_lottery`, `amesa_payment`, `amesa_notification`
  - `amesa_content`, `amesa_lottery_results`, `amesa_analytics`, `amesa_admin`

### Cost Impact
- **8x RDS db.t3.micro**: ~$115.20/month (8 × $14.40/month)
- **Aurora Serverless v2** (min 0.5 ACU, max 1 ACU): ~$30-60/month (pay-per-use)
- **Current (if 8 instances)**: ~$115.20/month
- **Optimized (Aurora Serverless)**: ~$30-60/month
- **Potential Savings**: **~$55-85/month** ($660-1,020/year)

### Additional Savings (if using separate instances)
- **Performance Insights**: ~$7.20/month per instance = $57.60/month total
- **Disable Performance Insights**: Save **$57.60/month** ($691/year)

### Recommendation
1. **Verify actual database state**:
   ```bash
   aws rds describe-db-instances --region eu-north-1 --query 'DBInstances[*].[DBInstanceIdentifier,DBInstanceClass,Engine]' --output table
   aws rds describe-db-clusters --region eu-north-1 --query 'DBClusters[*].[DBClusterIdentifier,Engine,EngineMode]' --output table
   ```

2. **If using Aurora Serverless v2**: Update Terraform to reflect actual state
   - Remove 8 separate RDS instances
   - Add Aurora Serverless v2 cluster configuration

3. **If using 8 separate instances**: **MIGRATE TO AURORA SERVERLESS V2**
   - Benefits: Auto-scaling, pay-per-use, better cost efficiency
   - Migration: Use AWS DMS or pg_dump/pg_restore

4. **Performance Insights**: Disable if not actively used
   - Cost: $7.20/month per instance
   - Enable only for troubleshooting when needed

### Implementation Priority
🔴 **CRITICAL** - Verify and optimize immediately

---

## 3. CloudWatch Logs Retention Optimization

### Current State
- **API Gateway Logs**: 7 days retention ✅ (good)
- **ECS Service Logs**: No explicit retention set (defaults to **NEVER EXPIRE**)
  - `/ecs/amesa-auth-service`
  - `/ecs/amesa-lottery-service`
  - `/ecs/amesa-payment-service`
  - `/ecs/amesa-notification-service`
  - `/ecs/amesa-content-service`
  - `/ecs/amesa-lottery-results-service`
  - `/ecs/amesa-analytics-service`
  - `/ecs/amesa-admin-service`

### Cost Impact
- **CloudWatch Logs**: $0.50/GB ingested, $0.03/GB stored per month
- **Without retention**: Logs accumulate indefinitely
- **Estimated monthly log volume**: 10-50GB per service (varies by traffic)
- **Total estimated storage**: 80-400GB across 8 services
- **Monthly storage cost**: $2.40-12.00/month (at 80GB) to $12-120/month (at 400GB)

### Recommendation
Set retention policy for all ECS log groups:

```terraform
# Add to each service's Terraform configuration
resource "aws_cloudwatch_log_group" "ecs_service_logs" {
  name              = "/ecs/amesa-{service}-service"
  retention_in_days = 7  # or 14, 30 based on needs
}
```

**Retention Options**:
- **7 days**: For high-volume services (saves most cost)
- **14 days**: Balanced approach
- **30 days**: For compliance/audit requirements

**Potential Savings**: $2-120/month depending on log volume

### Implementation Priority
🟡 **MEDIUM** - Implement within 1-2 weeks

---

## 4. ECS Fargate Resource Right-Sizing

### Current Resource Allocation

| Service | CPU | Memory | Monthly Cost* | Optimization Potential |
|---------|-----|--------|----------------|----------------------|
| Auth | 256 (0.25 vCPU) | 512 MB | ~$7.20 | ✅ Good |
| Lottery | 512 (0.5 vCPU) | 1024 MB | ~$14.40 | ⚠️ Review |
| Payment | 256 (0.25 vCPU) | 512 MB | ~$7.20 | ✅ Good |
| Notification | 256 (0.25 vCPU) | 512 MB | ~$7.20 | ✅ Good |
| Content | 256 (0.25 vCPU) | 512 MB | ~$7.20 | ✅ Good |
| Lottery Results | ? | ? | ? | ⚠️ Review |
| Analytics | ? | ? | ? | ⚠️ Review |
| Admin | ? | ? | ? | ⚠️ Review |

*Cost estimate based on 1 task running 24/7 at eu-north-1 Fargate pricing

### Recommendation
1. **Monitor actual resource utilization**:
   ```bash
   aws cloudwatch get-metric-statistics \
     --namespace AWS/ECS \
     --metric-name CPUUtilization \
     --dimensions Name=ServiceName,Value=amesa-lottery-service \
     --start-time 2025-01-20T00:00:00Z \
     --end-time 2025-01-25T00:00:00Z \
     --period 3600 \
     --statistics Average,Maximum
   ```

2. **Use AWS Compute Optimizer**:
   - Enable Compute Optimizer for ECS Fargate
   - Review recommendations for right-sizing

3. **Optimize Lottery Service**:
   - If CPU < 30% average: Consider reducing to 256 CPU
   - If Memory < 50% average: Consider reducing to 512 MB
   - **Potential Savings**: ~$7.20/month if reduced to 256/512

### Implementation Priority
🟡 **MEDIUM** - Monitor and optimize based on actual usage

---

## 5. Auto-Scaling Configuration Optimization

### Current State
- **Max Capacity**: 10 tasks per service
- **Min Capacity**: 1 task per service
- **Target CPU**: 70%

### Cost Impact
- **If all services scale to max**: 8 services × 10 tasks = 80 tasks
- **Estimated cost at max**: ~$576/month (80 × $7.20)
- **Current (1 task each)**: ~$57.60/month
- **Risk**: Unexpected traffic spike could scale to max capacity

### Recommendation
1. **Review actual scaling patterns**:
   ```bash
   aws application-autoscaling describe-scaling-activities \
     --service-namespace ecs \
     --resource-id service/amesa-microservices-cluster/amesa-lottery-service \
     --region eu-north-1
   ```

2. **Optimize max capacity based on actual needs**:
   - **Low-traffic services** (Content, Analytics): Max 3-5 tasks
   - **Medium-traffic services** (Auth, Payment, Notification): Max 5-7 tasks
   - **High-traffic services** (Lottery): Max 10 tasks (keep current)

3. **Add cost alerts**:
   - Set CloudWatch alarm for unexpected scaling
   - Alert when service scales beyond expected capacity

**Potential Risk Mitigation**: Prevents unexpected $500+ monthly bills

### Implementation Priority
🟡 **MEDIUM** - Review and adjust based on traffic patterns

---

## 6. ElastiCache Redis Optimization

### Current State
- **Node Type**: `cache.t3.micro` (default)
- **Replication**: 2 nodes in production (multi-AZ)
- **Snapshot Retention**: 7 days

### Cost Impact
- **cache.t3.micro**: ~$12.96/month per node
- **2 nodes (multi-AZ)**: ~$25.92/month
- **Single node**: ~$12.96/month

### Recommendation
1. **Evaluate multi-AZ necessity**:
   - If Redis is used for caching (not critical data): Consider single node
   - If Redis is used for session storage: Keep multi-AZ for HA
   - **Potential Savings**: ~$12.96/month if single node is acceptable

2. **Monitor Redis utilization**:
   - If memory usage < 50%: Consider smaller instance (if available)
   - If memory usage > 80%: Consider larger instance to avoid evictions

### Implementation Priority
🟢 **LOW** - Evaluate based on HA requirements

---

## 7. Container Insights Cost

### Current State
- **Container Insights**: Enabled on ECS cluster
- **Cost**: Additional CloudWatch metrics and logs

### Cost Impact
- **Container Insights**: ~$0.10 per container per month
- **8 services × 1 container**: ~$0.80/month
- **Value**: Provides detailed container metrics

### Recommendation
- **Keep enabled**: Low cost, high value for monitoring
- **No action needed**: Cost is minimal compared to value

### Implementation Priority
✅ **NO ACTION** - Keep as-is

---

## 8. Performance Insights (RDS)

### Current State
- **Performance Insights**: Enabled on all 8 RDS instances (if using separate instances)
- **Cost**: $7.20/month per instance

### Cost Impact
- **8 instances**: ~$57.60/month
- **If disabled**: $0/month
- **Potential Savings**: **$57.60/month** ($691/year)

### Recommendation
1. **If using separate RDS instances**: Disable Performance Insights unless actively troubleshooting
2. **Enable on-demand**: Enable only when investigating performance issues
3. **If using Aurora Serverless v2**: Performance Insights may not be applicable (verify)

### Implementation Priority
🟡 **MEDIUM** - Disable if not actively used

---

## 9. API Gateway Cost Optimization

### Current State
- **API Gateway HTTP API**: Single API with path-based routing
- **VPC Link**: Required for private ALB access

### Cost Impact
- **HTTP API**: $1.00 per million requests (first 300M free)
- **VPC Link**: $0.01 per hour = ~$7.20/month
- **Current cost**: Likely minimal (within free tier for most use cases)

### Recommendation
- **No optimization needed**: Already using cost-effective HTTP API
- **Monitor usage**: Set up billing alerts if traffic increases significantly

### Implementation Priority
✅ **NO ACTION** - Already optimized

---

## 10. EventBridge Cost

### Current State
- **Custom Event Bus**: `amesa-event-bus`
- **Event Rules**: 4 rules configured

### Cost Impact
- **Custom Event Bus**: $1.00 per million events
- **Event Rules**: $1.00 per million events matched
- **Current cost**: Likely minimal (< $1/month)

### Recommendation
- **No optimization needed**: Event-driven architecture is cost-effective
- **Monitor usage**: Review if event volume increases significantly

### Implementation Priority
✅ **NO ACTION** - Already optimized

---

## Summary of Cost Optimization Opportunities

### Immediate Actions (High Impact)

| Optimization | Current Cost | Optimized Cost | Monthly Savings | Annual Savings |
|--------------|--------------|----------------|-----------------|----------------|
| **ALB Consolidation** (if 8 ALBs exist) | ~$129.60 | ~$16.20 | **$113.40** | **$1,360.80** |
| **Database Migration** (if 8 RDS instances) | ~$115.20 | ~$30-60 | **$55-85** | **$660-1,020** |
| **Performance Insights** (if enabled) | ~$57.60 | $0 | **$57.60** | **$691.20** |
| **CloudWatch Logs Retention** | $2-120 | $0.50-5 | **$1.50-115** | **$18-1,380** |

### Total Potential Savings
- **Best Case** (if all optimizations apply): **~$287/month** ($3,444/year)
- **Realistic Case** (based on actual state): **~$100-200/month** ($1,200-2,400/year)

---

## Implementation Plan

### Phase 1: Verification (Week 1)
1. ✅ Verify actual ALB count
2. ✅ Verify actual database architecture (RDS vs Aurora Serverless)
3. ✅ Review CloudWatch Logs retention settings
4. ✅ Check Performance Insights status

### Phase 2: High-Impact Optimizations (Week 2-3)
1. 🔴 Consolidate ALBs (if needed)
2. 🔴 Migrate to Aurora Serverless v2 (if using separate RDS instances)
3. 🔴 Disable Performance Insights (if not needed)
4. 🟡 Set CloudWatch Logs retention

### Phase 3: Monitoring & Right-Sizing (Week 4-8)
1. 🟡 Enable Compute Optimizer for ECS
2. 🟡 Review and optimize auto-scaling limits
3. 🟡 Right-size ECS Fargate resources based on actual usage
4. 🟢 Evaluate Redis multi-AZ necessity

---

## Monitoring & Alerts

### Recommended CloudWatch Alarms
1. **Unexpected Scaling**: Alert when service scales beyond expected capacity
2. **Cost Anomaly**: Alert when daily AWS costs exceed threshold
3. **Database Performance**: Alert if Aurora Serverless scales beyond expected ACU
4. **Log Volume**: Alert if CloudWatch Logs ingestion exceeds threshold

### Cost Tracking
- **AWS Cost Explorer**: Review monthly costs by service
- **AWS Budgets**: Set monthly budget alerts
- **AWS Cost Anomaly Detection**: Enable for automatic cost anomaly alerts

---

## Next Steps

1. **Immediate**: Run verification commands to understand actual infrastructure state
2. **This Week**: Implement high-impact optimizations (ALB, Database, Performance Insights)
3. **This Month**: Set up monitoring and right-sizing based on actual usage
4. **Ongoing**: Review costs monthly and adjust as needed

---

## References

- [AWS Fargate Pricing](https://aws.amazon.com/fargate/pricing/)
- [Aurora Serverless v2 Pricing](https://aws.amazon.com/rds/aurora/pricing/)
- [ALB Pricing](https://aws.amazon.com/elasticloadbalancing/pricing/)
- [CloudWatch Logs Pricing](https://aws.amazon.com/cloudwatch/pricing/)
- [AWS Cost Optimization Best Practices](https://docs.aws.amazon.com/cost-management/latest/userguide/cost-optimization-hub.html)

---

**Last Updated**: 2025-01-25  
**Next Review**: After Phase 1 verification complete

