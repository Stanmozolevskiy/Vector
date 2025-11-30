# Swagger Fix

## Problem

Swagger was showing a blank page when accessing `http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/swagger`

## Root Causes

1. **ALB Routing Issue**: The Application Load Balancer listener rule only routed `/api` and `/api/*` to the backend. Requests to `/swagger` were being routed to the frontend (which returned a blank page).

2. **Swagger Configuration**: Swagger was only enabled for `IsDevelopment()`, but we also want it enabled for dev/staging environments explicitly.

## Fixes Applied

### 1. Updated ALB Listener Rule

**File:** `infrastructure/terraform/modules/alb/main.tf`

Added `/swagger` and `/swagger/*` to the path pattern so these requests are routed to the backend:

```terraform
condition {
  path_pattern {
    values = ["/api", "/api/*", "/swagger", "/swagger/*"]
  }
}
```

### 2. Enhanced Swagger Configuration

**File:** `backend/Vector.Api/Program.cs`

Updated Swagger to be enabled for Development, Staging, and Dev environments:

```csharp
// Enable Swagger for Development and non-Production environments
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Staging" || app.Environment.EnvironmentName == "Dev")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vector API v1");
        c.RoutePrefix = "swagger";
    });
}
```

## Deployment

1. **Backend Code**: Changes committed and pushed to `develop` branch
2. **Terraform**: ALB listener rule update needs to be applied

### Apply Terraform Changes

```powershell
cd infrastructure/terraform
terraform apply -var="environment=dev" -var="db_password=$Memic1234" -auto-approve
```

### Wait for CI/CD

After Terraform is applied and CI/CD deploys the new backend image, Swagger will be available at:
- `http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/swagger`

## Testing

After deployment:

1. **Test Swagger UI:**
   ```powershell
   Start-Process "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/swagger"
   ```

2. **Test Swagger JSON:**
   ```powershell
   Invoke-WebRequest -Uri "http://dev-vector-alb-1842167636.us-east-1.elb.amazonaws.com/swagger/v1/swagger.json" -UseBasicParsing
   ```

## Expected Result

- Swagger UI should load correctly
- All API endpoints should be visible in Swagger
- You can test endpoints directly from Swagger UI

## Notes

- Swagger is **disabled** in Production for security reasons
- Swagger is **enabled** in Development, Staging, and Dev environments
- The ALB routing ensures `/swagger` requests go to the backend, not the frontend

