# Route 53 Setup Complete for amesa-group.net

## ✅ What's Been Done

1. **Hosted Zone Created**: `/hostedzone/Z002379923AKV17LKD0Y4`
2. **A Record (Alias) for Root Domain**: `amesa-group.net` → CloudFront
3. **A Record (Alias) for WWW**: `www.amesa-group.net` → CloudFront

## 📋 Route 53 Nameservers

Update your GoDaddy nameservers to these Route 53 nameservers:

```
ns-1300.awsdns-34.org
ns-1863.awsdns-40.co.uk
ns-803.awsdns-36.net
ns-313.awsdns-39.com
```

## 🔧 Final Step: Update Nameservers in GoDaddy

### Step 1: Go to GoDaddy Domain Settings

1. Go to: https://dcc.godaddy.com/manage/amesa-group.net/dns
2. Look for **"Nameservers"** section (usually at the top or in domain settings)
3. Click **"Change"** or **"Edit"** next to nameservers

### Step 2: Update to Route 53 Nameservers

1. Select **"Custom"** or **"I'll use my own nameservers"**
2. Enter the 4 Route 53 nameservers:
   - `ns-1300.awsdns-34.org`
   - `ns-1863.awsdns-40.co.uk`
   - `ns-803.awsdns-36.net`
   - `ns-313.awsdns-39.com`
3. Click **"Save"** or **"Update"**

### Step 3: Wait for Propagation

- **Usually**: 15-30 minutes
- **Sometimes**: Up to 48 hours (rare)
- **Check status**: Use https://dnschecker.org/ to verify nameserver propagation

## ✅ DNS Records Created in Route 53

### Root Domain (amesa-group.net)
- **Type**: A (Alias)
- **Name**: `amesa-group.net`
- **Alias Target**: `dpqbvdgnenckf.cloudfront.net`
- **Status**: ✅ Created

### WWW Subdomain (www.amesa-group.net)
- **Type**: A (Alias)
- **Name**: `www.amesa-group.net`
- **Alias Target**: `dpqbvdgnenckf.cloudfront.net`
- **Status**: ✅ Created

## 🔍 Verification

After updating nameservers, verify:

```bash
# Check nameservers
nslookup -type=NS amesa-group.net

# Check root domain
nslookup amesa-group.net

# Check www subdomain
nslookup www.amesa-group.net
```

## 📝 Important Notes

1. **Keep GoDaddy DNS Records**: Don't delete the CNAME records in GoDaddy until nameservers are updated
2. **Validation Records**: The ACM validation CNAME records will be managed in Route 53 after nameserver update
3. **DNS Propagation**: Can take 15-60 minutes after nameserver update
4. **Cost**: Route 53 hosted zone costs $0.50/month + $0.40 per million queries

## 🎯 Next Steps

1. ✅ Update nameservers in GoDaddy (see above)
2. ⏳ Wait for DNS propagation (15-60 minutes)
3. ✅ Test domains: `https://amesa-group.net` and `https://www.amesa-group.net`
4. ✅ Verify SSL certificates are working
5. ✅ Test OAuth redirects

## 📊 Route 53 Hosted Zone Details

- **Hosted Zone ID**: `Z002379923AKV17LKD0Y4`
- **Domain**: `amesa-group.net`
- **Region**: Global (Route 53 is global service)
- **Records Created**: 2 A records (Alias) pointing to CloudFront

## 🔗 Useful Commands

### Check Hosted Zone
```bash
aws route53 get-hosted-zone --id /hostedzone/Z002379923AKV17LKD0Y4
```

### List All Records
```bash
aws route53 list-resource-record-sets --hosted-zone-id /hostedzone/Z002379923AKV17LKD0Y4
```

### Check Change Status
```bash
aws route53 get-change --id /change/C06612343QAQVXGDFLOHR
```

## ✅ Status

- [x] Route 53 hosted zone created
- [x] A record (Alias) for root domain created
- [x] A record (Alias) for www subdomain created
- [ ] Nameservers updated in GoDaddy (pending user action)
- [ ] DNS propagation complete (pending)
- [ ] Domains tested and working (pending)
