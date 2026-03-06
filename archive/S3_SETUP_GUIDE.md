# S3 Service Setup Guide

## Current Status

✅ **Completed:**
- S3Service interface created (`IS3Service.cs`)
- S3Service implementation created (`S3Service.cs`)
- S3 bucket policies configured in Terraform
- Profile picture upload UI completed
- Image preview functionality working
- **AWS SDK packages installed**
- **S3Service registered in Program.cs**
- **UserService updated with S3 dependency**
- **Upload/Delete methods fully implemented**
- **Frontend upload handler working**
- **Docker configuration complete**
- **Local development ready**

⏳ **Remaining Steps:**
- Create `.env` file with AWS credentials (for local testing)
- Add AWS environment variables to ECS task definition (for AWS deployment)

---

## Step 1: Install AWS SDK for .NET

Add the AWS S3 NuGet package to the backend:

```bash
cd backend/Vector.Api
dotnet add package AWSSDK.S3
dotnet add package AWSSDK.Extensions.NETCore.Setup
```

---

## Step 2: Configure AWS Credentials

### Local Development (appsettings.Development.json):

```json
{
  "AWS": {
    "Profile": "default",
    "Region": "us-east-1",
    "S3": {
      "BucketName": "dev-vector-user-uploads"
    }
  }
}
```

### Docker (docker-compose.yml):

```yaml
backend:
  environment:
    - AWS__Region=us-east-1
    - AWS__S3__BucketName=dev-vector-user-uploads
    - AWS_ACCESS_KEY_ID=${AWS_ACCESS_KEY_ID}
    - AWS_SECRET_ACCESS_KEY=${AWS_SECRET_ACCESS_KEY}
```

### AWS ECS (Terraform variables.tf):

Already configured in `infrastructure/terraform/main.tf` with task role.

---

## Step 3: Register S3Service in Program.cs

Add to `backend/Vector.Api/Program.cs`:

```csharp
// Add AWS Services
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();

// Register S3 Service
builder.Services.AddScoped<IS3Service, S3Service>();
```

Add this import at the top:
```csharp
using Amazon.S3;
using Vector.Api.Services;
```

---

## Step 4: Update UserService Constructor

In `backend/Vector.Api/Services/UserService.cs`, uncomment the S3Service dependency:

```csharp
private readonly IS3Service _s3Service;

public UserService(ApplicationDbContext context, ILogger<UserService> logger, IS3Service s3Service)
{
    _context = context;
    _logger = logger;
    _s3Service = s3Service;
}
```

---

## Step 5: Implement Profile Picture Upload Endpoint

In `backend/Vector.Api/Controllers/UserController.cs`, add:

```csharp
/// <summary>
/// Upload profile picture
/// </summary>
[HttpPost("me/profile-picture")]
[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> UploadProfilePicture(IFormFile file)
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Unauthorized(new { error = "Invalid token" });
    }

    if (file == null || file.Length == 0)
    {
        return BadRequest(new { error = "No file uploaded" });
    }

    // Validate file type
    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
    if (!allowedTypes.Contains(file.ContentType.ToLower()))
    {
        return BadRequest(new { error = "Invalid file type. Only JPG, PNG, and GIF are allowed" });
    }

    // Validate file size (5MB max)
    if (file.Length > 5 * 1024 * 1024)
    {
        return BadRequest(new { error = "File size exceeds 5MB limit" });
    }

    try
    {
        using var stream = file.OpenReadStream();
        var pictureUrl = await _userService.UploadProfilePictureAsync(userId, stream, file.FileName, file.ContentType);
        
        return Ok(new { profilePictureUrl = pictureUrl });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to upload profile picture for user {UserId}", userId);
        return StatusCode(500, new { error = "Failed to upload profile picture" });
    }
}
```

---

## Step 6: Uncomment S3 Upload Logic in UserService

In `backend/Vector.Api/Services/UserService.cs`, uncomment the S3 code in `UploadProfilePictureAsync`:

```csharp
public async Task<string> UploadProfilePictureAsync(Guid userId, Stream fileStream, string fileName, string contentType)
{
    var user = await _context.Users.FindAsync(userId);
    
    if (user == null)
    {
        throw new InvalidOperationException("User not found");
    }

    // Delete old profile picture if exists
    if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
    {
        await _s3Service.DeleteFileAsync(user.ProfilePictureUrl);
    }

    // Upload new profile picture
    var pictureUrl = await _s3Service.UploadFileAsync(fileStream, fileName, contentType, "profile-pictures");
    
    user.ProfilePictureUrl = pictureUrl;
    user.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    
    return pictureUrl;
}
```

---

## Step 7: Update Frontend to Handle Upload

In `frontend/src/pages/profile/ProfilePage.tsx`, add upload handler:

```typescript
const handleSaveProfile = async (e: React.FormEvent) => {
  e.preventDefault();
  setIsSaving(true);
  setErrorMessage('');
  setSuccessMessage('');

  try {
    // Upload profile picture if selected
    if (profilePicture) {
      const formData = new FormData();
      formData.append('file', profilePicture);
      
      try {
        const response = await api.post('/users/me/profile-picture', formData, {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        });
        console.log('Profile picture uploaded:', response.data.profilePictureUrl);
      } catch (uploadErr) {
        console.error('Failed to upload profile picture:', uploadErr);
        setErrorMessage('Profile picture upload failed, but continuing with profile update');
      }
    }

    // Update profile data
    await api.put('/users/me', profileData);
    setSuccessMessage('Profile updated successfully!');

    setTimeout(() => {
      window.location.reload();
    }, 1500);
  } catch (err) {
    const errorMsg = err && typeof err === 'object' && 'response' in err
      ? (err.response as { data?: { error?: string } })?.data?.error
      : undefined;
    setErrorMessage(errorMsg || 'Failed to update profile');
  } finally {
    setIsSaving(false);
  }
};
```

Also add state:
```typescript
const [profilePicture, setProfilePicture] = useState<File | null>(null);
```

---

## Step 8: Test Profile Picture Upload

1. **Login:** http://localhost:3000/login
2. **Go to Profile:** http://localhost:3000/profile
3. **Click "Upload New Picture"**
4. **Select an image file**
5. **Preview appears**
6. **Click "Save Changes"**
7. **Image uploads to S3**
8. **Profile picture URL saved to database**
9. **Page reloads with new profile picture**

---

## AWS Configuration

### Environment Variables Needed:

**Local Docker (.env):**
```env
AWS_ACCESS_KEY_ID=your_access_key
AWS_SECRET_ACCESS_KEY=your_secret_key
AWS_REGION=us-east-1
```

**GitHub Secrets (for CI/CD):**
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`

**ECS Task Definition:**
Uses IAM task role (already configured in Terraform).

---

## S3 Bucket Configuration

### Already Configured in Terraform:

1. **Bucket Name:** `dev-vector-user-uploads`
2. **Region:** `us-east-1`
3. **Encryption:** AES256
4. **CORS:** Enabled for PUT/GET/POST/DELETE
5. **Policies:**
   - ECS task role has full access
   - Public read for `profile-pictures/*` with tag `public=true`
6. **Lifecycle:** Delete old versions after 90 days (prod only)

---

## Implementation Checklist

- [x] Create IS3Service interface
- [x] Create S3Service implementation
- [x] Configure S3 bucket policies (Terraform)
- [x] Install AWSSDK.S3 NuGet package
- [x] Add AWS configuration to appsettings
- [x] Register S3Service in Program.cs
- [x] Uncomment S3Service in UserService
- [x] Create profile picture upload endpoint
- [x] Update frontend to handle file upload
- [ ] Test locally with AWS credentials (requires .env file)
- [x] Deploy to Docker
- [ ] Test on AWS dev environment (requires ECS task definition update)

---

## Security Considerations

1. **File Validation:**
   - Only allow image/* MIME types
   - Max file size: 5MB
   - Generate unique filenames (prevent overwrites)

2. **Access Control:**
   - Profile pictures: Public read (with tags)
   - All other files: Private (presigned URLs)

3. **Cleanup:**
   - Delete old profile picture when uploading new one
   - Lifecycle rules clean up old versions

4. **Error Handling:**
   - Graceful degradation if upload fails
   - Log all errors
   - Don't expose AWS errors to users

---

## Estimated Time: 30-45 minutes

**Next Step:** Install AWSSDK.S3 and register services.

---

**Created:** December 2, 2025  
**Status:** Ready for implementation

