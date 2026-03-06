# Staging Swagger Access Fix

## Issue

Swagger UI at `http://staging-vector-alb-2020798622.us-east-1.elb.amazonaws.com/swagger` was not accessible, while the frontend was working correctly.

## Root Cause

The ALB listener rule for staging environment only routed `/api/*` paths to the backend target group. The `/swagger` and `/swagger/*` paths were not included, so they were being routed to the frontend target group instead of the backend.

**Staging ALB Configuration (Before):**
```hcl
condition {
  path_pattern {
    values = ["/api/*"]  # ❌ Missing /swagger paths
  }
}
```

**Dev ALB Configuration (Correct):**
```hcl
condition {
  path_pattern {
    values = ["/api", "/api/*", "/swagger", "/swagger/*"]  # ✅ Includes Swagger
  }
}
```

## Fix Applied

Updated the staging ALB listener rule to include Swagger paths:

```hcl
condition {
  path_pattern {
    values = ["/api/*", "/swagger", "/swagger/*"]  # ✅ Now includes Swagger
  }
}
```

## Terraform Changes

**File:** `infrastructure/terraform/modules/alb/main.tf`

**Resource:** `aws_lb_listener_rule.backend_api_staging`

**Change:** Added `/swagger` and `/swagger/*` to the path pattern values.

## Deployment

1. **Terraform Plan:**
   ```bash
   cd infrastructure/terraform
   terraform workspace select staging
   terraform plan -target=module.alb.aws_lb_listener_rule.backend_api_staging
   ```

2. **Terraform Apply:**
   ```bash
   terraform apply
   ```

3. **Verification:**
   - Swagger UI: `http://staging-vector-alb-2020798622.us-east-1.elb.amazonaws.com/swagger`
   - API Health: `http://staging-vector-alb-2020798622.us-east-1.elb.amazonaws.com/api/health`
   - Frontend: `http://staging-vector-alb-2020798622.us-east-1.elb.amazonaws.com`

## Expected Behavior After Fix

- ✅ `/swagger` → Routes to backend (Swagger UI)
- ✅ `/swagger/*` → Routes to backend (Swagger assets)
- ✅ `/api/*` → Routes to backend (API endpoints)
- ✅ `/` (root) → Routes to frontend (default)
- ✅ All other paths → Routes to frontend (default)

## Backend Swagger Configuration

The backend is already configured to serve Swagger UI in staging environment:

```csharp
// Program.cs
if (app.Environment.IsDevelopment() || 
    app.Environment.EnvironmentName == "Staging" || 
    app.Environment.EnvironmentName == "Dev" || ...)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vector API v1");
        c.RoutePrefix = "swagger";
    });
}
```

## Status

- ✅ Terraform configuration updated
- ⏳ Terraform apply pending (run `terraform apply` in staging workspace)
- ⏳ Verification pending (test Swagger UI after apply)

---

**Fixed:** December 9, 2025  
**Status:** ✅ Configuration fixed, pending Terraform apply

