# Additional AWS Cost Optimization Opportunities
**Date**: 2025-01-25  
**Status**: Analysis Complete

## Executive Summary

After deeper analysis, I've identified **5 additional cost-saving opportunities** beyond Performance Insights:

1. ⚠️ **DUPLICATE REDIS CLUSTERS** - 2 Redis clusters running (likely redundant)
2. ✅ **Aurora Serverless Min Capacity** - Set to 0.5 ACU (always running)
3. ✅ **ECS Resource Right-Sizing** - Some services may be over-provisioned
4. ✅ **Aurora Backup Retention** - 3 days (could optimize)
5. ✅ **ECR Image Cleanup** - Potential unused images

**Total Additional Potential Savings**: $20-50/month ($240-600/year)

---

## 1. Duplicate Redis Clusters ⚠️ CRITICAL

### Current State
- **Found 2 Redis clusters**:
  - `amesa-redis` (cache.t3.micro, 1 node)
  - `amesa-redis-prod-001` (cache.t3.micro, 1 node)
- **Cost per cluster**: ~$12.96/month
- **Total cost**: ~$25.92/month

### Issue
Having 2 Redis clusters is likely redundant. Only one should be active.

### Cost Impact
- **Current**: ~$25.92/month (2 clusters)
- **Optimized**: ~$12.96/month (1 cluster)
- **Potential Savings**: **$12.96/month** ($155.52/year)

### Recommendation
1. **Verify which cluster is actually being used**:
   ```bash
   # Check connection strings in SSM Parameter Store
   aws ssm get-parameter --name "/amesa/prod/ConnectionStrings/Redis" --region eu-north-1
   
   # Check which cluster is referenced in ECS task definitions
   grep -r "redis" Infrastructure/ecs-task-definitions/
   ```

2. **If one is unused**: Delete the unused cluster
   ```bash
   # Delete unused cluster (BE CAREFUL - verify it's not in use first!)
   aws elasticache delete-replication-group \
     --replication-group-id <unused-cluster-id> \
     --region eu-north-1
   ```

3. **If both are needed**: Consider consolidating to single cluster with higher capacity if needed

### Implementation Priority
🔴 **CRITICAL** - Verify and consolidate immediately

---

## 2. Aurora Serverless v2 Min Capacity Optimization

### Current State
- **Cluster**: `amesa-prod`
- **Min Capacity**: 0.5 ACU (always running)
- **Max Capacity**: 4.0 ACU (scales up as needed)
- **Backup Retention**: 3 days

### Cost Impact
- **0.5 ACU minimum**: ~$30-45/month (always running, even during low/no traffic)
- **If min = 0**: Pay only when in use (could save during idle periods)

### Recommendation
**Option A: Reduce Min Capacity to 0** (if acceptable downtime is OK)
- **Savings**: $15-30/month during idle periods
- **Risk**: Cold start delay when scaling from 0 (5-30 seconds)
- **Best for**: Non-critical services, development, or predictable low-traffic periods

**Option B: Keep Min at 0.5** (current - recommended for production)
- **No savings**, but ensures instant availability
- **Best for**: Production workloads requiring immediate response

**Option C: Schedule-based scaling** (advanced)
- Use EventBridge to scale to 0 during known low-traffic hours
- **Savings**: $5-15/month (depends on schedule)
- **Complexity**: Medium (requires automation)

### Implementation
```bash
# Option A: Set min to 0 (if acceptable)
aws rds modify-db-cluster \
  --db-cluster-identifier amesa-prod \
  --serverless-v2-scaling-configuration MinCapacity=0,MaxCapacity=4.0 \
  --region eu-north-1 \
  --apply-immediately
```

### Implementation Priority
🟡 **MEDIUM** - Evaluate based on traffic patterns and acceptable downtime

---

## 3. ECS Fargate Resource Right-Sizing

### Current Resource Allocation

| Service | CPU | Memory | Monthly Cost* | Optimization Potential |
|---------|-----|--------|----------------|----------------------|
| Auth | 256 (0.25 vCPU) | 512 MB | ~$7.20 | ✅ Good |
| Lottery | 512 (0.5 vCPU) | 1024 MB | ~$14.40 | ⚠️ Review |
| Payment | 256 (0.25 vCPU) | 512 MB | ~$7.20 | ✅ Good |
| Notification | 256 (0.25 vCPU) | 512 MB | ~$7.20 | ✅ Good |
| Content | 256 (0.25 vCPU) | 512 MB | ~$7.20 | ✅ Good |
| Lottery Results | 256 (0.25 vCPU) | 512 MB | ~$7.20 | ✅ Good |
| Analytics | 256 (0.25 vCPU) | 512 MB | ~$7.20 | ✅ Good |
| Admin | 512 (0.5 vCPU) | 1024 MB | ~$14.40 | ⚠️ Review |

*Cost estimate based on 1 task running 24/7 at eu-north-1 Fargate pricing

### Analysis Needed
**Lottery Service** and **Admin Service** use 2x the resources of other services.

### Recommendation
1. **Monitor actual resource utilization** for 1-2 weeks:
   ```bash
   # Check CloudWatch metrics for CPU and Memory utilization
   # Use AWS Console: CloudWatch → Metrics → ECS → Service Metrics
   ```

2. **If Lottery Service CPU < 30% average**:
   - Consider reducing to 256 CPU (0.25 vCPU)
   - **Potential Savings**: ~$7.20/month

3. **If Admin Service CPU < 30% average**:
   - Consider reducing to 256 CPU (0.25 vCPU)
   - **Potential Savings**: ~$7.20/month

4. **Use AWS Compute Optimizer**:
   - Enable Compute Optimizer for ECS Fargate
   - Review recommendations after 2 weeks of data collection

### Implementation Priority
🟡 **MEDIUM** - Monitor first, then optimize based on actual usage

---

## 4. Aurora Backup Retention Optimization

### Current State
- **Backup Retention**: 3 days
- **Current Snapshots**: 3 automated snapshots
- **Cost**: Minimal (~$0.10-0.50/month for storage)

### Recommendation
**Option A: Keep 3 days** (recommended for production)
- Good balance between recovery and cost
- **No change needed**

**Option B: Reduce to 1 day** (if acceptable)
- **Savings**: ~$0.20-0.40/month (minimal)
- **Risk**: Less recovery window
- **Not recommended** for production

**Option C: Increase to 7 days** (if compliance requires)
- **Cost increase**: ~$0.20-0.40/month
- **Benefit**: Better recovery options

### Implementation Priority
🟢 **LOW** - Current setting is optimal, minimal cost impact

---

## 5. ECR Image Cleanup

### Current State
- **8 ECR repositories** (one per service)
- **Image Tag Mutability**: MUTABLE (allows overwriting tags)
- **No lifecycle policy visible**

### Potential Issue
- Old/unused images accumulate over time
- Each image consumes storage (~$0.10/GB/month)
- Can accumulate significant storage costs over months

### Recommendation
1. **Check current storage usage**:
   ```bash
   aws ecr describe-repositories --region eu-north-1 \
     --query 'repositories[*].[repositoryName,repositoryUri]' \
     --output table
   
   # Check image count per repository
   for repo in $(aws ecr describe-repositories --region eu-north-1 --query 'repositories[*].repositoryName' --output text); do
     echo "$repo: $(aws ecr list-images --repository-name $repo --region eu-north-1 --query 'length(imageIds)') images"
   done
   ```

2. **Implement ECR Lifecycle Policies**:
   ```json
   {
     "rules": [
       {
         "rulePriority": 1,
         "description": "Keep last 10 images",
         "selection": {
           "tagStatus": "any",
           "countType": "imageCountMoreThan",
           "countNumber": 10
         },
         "action": {
           "type": "expire"
         }
       }
     ]
   }
   ```

3. **Apply lifecycle policy**:
   ```bash
   aws ecr put-lifecycle-policy \
     --repository-name <repository-name> \
     --lifecycle-policy-text file://lifecycle-policy.json \
     --region eu-north-1
   ```

### Potential Savings
- **Current**: Depends on image accumulation (could be $1-10/month)
- **With lifecycle policy**: ~$0-1/month (only recent images)
- **Potential Savings**: $1-10/month (if images have accumulated)

### Implementation Priority
🟡 **MEDIUM** - Implement lifecycle policies to prevent future accumulation

---

## 6. Auto-Scaling Configuration Review

### Current State
- **Auto-scaling policies**: Not found (Terraform shows configs but not applied)
- **All services**: Running 1 task each (desiredCount = 1)
- **Max capacity in Terraform**: 10 tasks per service

### Analysis
**Good News**: No unexpected scaling = predictable costs  
**Potential Issue**: Services might scale unnecessarily if auto-scaling is enabled later

### Recommendation
1. **If auto-scaling is not needed**: Keep current state (1 task per service)
2. **If auto-scaling is needed**: Configure conservative limits:
   - **Low-traffic services**: Max 3-5 tasks
   - **Medium-traffic services**: Max 5-7 tasks
   - **High-traffic services**: Max 10 tasks
3. **Set up cost alerts** before enabling auto-scaling:
   ```bash
   aws cloudwatch put-metric-alarm \
     --alarm-name ecs-cost-alert \
     --alarm-description "Alert when ECS tasks exceed expected count" \
     --metric-name RunningTaskCount \
     --namespace AWS/ECS \
     --statistic Sum \
     --period 300 \
     --threshold 20 \
     --comparison-operator GreaterThanThreshold \
     --evaluation-periods 1
   ```

### Implementation Priority
🟢 **LOW** - Current state is good, just ensure proper limits if enabling later

---

## 7. CloudWatch Metrics and Logs Optimization

### Current State
- **Container Insights**: Enabled (adds ~$0.80/month for 8 services)
- **CloudWatch Logs**: All have retention policies ✅
- **Custom Metrics**: Unknown (need to check)

### Recommendation
1. **Keep Container Insights**: Low cost, high value ($0.80/month is worth it)
2. **Review custom metrics**: Check if any custom metrics are being published unnecessarily
3. **Optimize log levels**: Ensure services aren't logging at DEBUG level in production

### Implementation Priority
🟢 **LOW** - Already optimized, Container Insights is worth the cost

---

## Summary of Additional Cost Optimization Opportunities

### Immediate Actions (High Impact)

| Optimization | Current Cost | Optimized Cost | Monthly Savings | Annual Savings | Priority |
|--------------|--------------|----------------|-----------------|----------------|----------|
| **Consolidate Redis Clusters** | $25.92 | $12.96 | **$12.96** | **$155.52** | 🔴 Critical |
| **Disable Performance Insights** | $14.40 | $0 | **$14.40** | **$172.80** | 🔴 Critical |
| **ECR Lifecycle Policies** | $1-10 | $0-1 | **$1-10** | **$12-120** | 🟡 Medium |

### Potential Optimizations (Require Monitoring)

| Optimization | Potential Savings | Risk | Priority |
|--------------|-------------------|------|----------|
| **Right-size Lottery Service** | $7.20/month | Low | 🟡 Medium |
| **Right-size Admin Service** | $7.20/month | Low | 🟡 Medium |
| **Aurora Min Capacity = 0** | $15-30/month* | Medium | 🟡 Medium |

*Only during idle periods

### Total Additional Potential Savings

- **Immediate (Redis + Performance Insights)**: **$27.36/month** ($328.32/year)
- **With ECR cleanup**: **$28-37/month** ($336-444/year)
- **With right-sizing (if applicable)**: **$42-51/month** ($504-612/year)
- **With Aurora optimization (if acceptable)**: **$57-81/month** ($684-972/year)

---

## Recommended Implementation Order

### Week 1: Critical Fixes
1. ✅ **Verify and consolidate Redis clusters** (if duplicate)
   - **Savings**: $12.96/month
   - **Time**: 30 minutes
   - **Risk**: Low (if verified unused)

2. ✅ **Complete Performance Insights disable** (wait for cluster ready)
   - **Savings**: $14.40/month
   - **Time**: 5 minutes
   - **Risk**: None

### Week 2: Medium Priority
3. ✅ **Implement ECR lifecycle policies**
   - **Savings**: $1-10/month (prevents future accumulation)
   - **Time**: 1 hour
   - **Risk**: None

4. ✅ **Monitor ECS resource utilization**
   - **Action**: Set up CloudWatch dashboards
   - **Time**: 2 hours
   - **Risk**: None

### Week 3-4: Optimization Based on Data
5. ✅ **Right-size ECS services** (if over-provisioned)
   - **Savings**: $0-14.40/month
   - **Time**: 1 hour per service
   - **Risk**: Low (can revert)

6. ✅ **Evaluate Aurora min capacity** (if acceptable downtime)
   - **Savings**: $15-30/month (during idle)
   - **Time**: 30 minutes
   - **Risk**: Medium (cold start delay)

---

## Cost Monitoring Setup

### Recommended CloudWatch Alarms

1. **Unexpected ECS Scaling**:
   ```bash
   aws cloudwatch put-metric-alarm \
     --alarm-name ecs-unexpected-scaling \
     --alarm-description "Alert when total ECS tasks exceed 10" \
     --metric-name RunningTaskCount \
     --namespace AWS/ECS \
     --statistic Sum \
     --period 300 \
     --threshold 10 \
     --comparison-operator GreaterThanThreshold \
     --evaluation-periods 1 \
     --region eu-north-1
   ```

2. **Aurora Capacity Alert**:
   ```bash
   aws cloudwatch put-metric-alarm \
     --alarm-name aurora-capacity-alert \
     --alarm-description "Alert when Aurora ACU exceeds 3.0" \
     --metric-name ServerlessDatabaseCapacity \
     --namespace AWS/RDS \
     --statistic Average \
     --period 300 \
     --threshold 3.0 \
     --comparison-operator GreaterThanThreshold \
     --evaluation-periods 2 \
     --dimensions Name=DBClusterIdentifier,Value=amesa-prod \
     --region eu-north-1
   ```

3. **Monthly Cost Alert**:
   ```bash
   aws cloudwatch put-metric-alarm \
     --alarm-name monthly-cost-alert \
     --alarm-description "Alert when estimated charges exceed $200" \
     --metric-name EstimatedCharges \
     --namespace AWS/Billing \
     --statistic Maximum \
     --period 86400 \
     --threshold 200 \
     --comparison-operator GreaterThanThreshold \
     --evaluation-periods 1 \
     --region us-east-1  # Billing metrics are in us-east-1
   ```

---

## Next Steps

1. **Immediate** (Today):
   - Verify which Redis cluster is in use
   - Complete Performance Insights disable (once cluster ready)

2. **This Week**:
   - Consolidate duplicate Redis cluster (if confirmed unused)
   - Implement ECR lifecycle policies
   - Set up cost monitoring alarms

3. **This Month**:
   - Monitor ECS resource utilization
   - Review Aurora capacity patterns
   - Right-size services based on actual usage

---

## Conclusion

**Total Immediate Savings Available**: **$27.36/month** ($328.32/year)
- Redis consolidation: $12.96/month
- Performance Insights: $14.40/month

**Additional Potential Savings** (with monitoring/optimization): **$15-54/month** ($180-648/year)

**Grand Total Potential Savings**: **$42-81/month** ($504-972/year)

---

**Last Updated**: 2025-01-25  
**Next Review**: After implementing immediate optimizations


