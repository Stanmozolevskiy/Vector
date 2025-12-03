# Image Storage Strategy - How It Works

**Date:** December 3, 2025

---

## 📊 Overview: How Images Are Stored and Retrieved

### The Strategy: **Store URL in Database, Binary in S3**

We **DO NOT** store image binary data in the database. Instead:
1. ✅ **Image file (binary)** → Stored in **AWS S3 bucket**
2. ✅ **Image URL (string)** → Stored in **PostgreSQL database**
3. ✅ **Frontend** → Fetches image directly from S3 using the URL

---

## 🔄 Complete Upload Flow

### Step 1: User Selects Image (Frontend)

```typescript
// frontend/src/pages/profile/ProfilePage.tsx
const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
  const file = e.target.files?.[0];
  if (file) {
    setProfilePicture(file);  // Store File object in React state
    
    // Create preview (Base64 for display only)
    const reader = new FileReader();
    reader.onloadend = () => {
      setProfilePicturePreview(reader.result as string);
    };
    reader.readAsDataURL(file);
  }
};
```

**What happens:**
- User selects image file
- Frontend creates a **preview** (Base64) for display only
- Original file is stored in state, ready for upload

---

### Step 2: User Clicks "Save Changes" (Frontend Uploads)

```typescript
// frontend/src/pages/profile/ProfilePage.tsx
const handleSaveProfile = async () => {
  if (profilePicture) {
    // Create multipart form data
    const formData = new FormData();
    formData.append('file', profilePicture);  // Attach binary file
    
    // Upload to backend
    const response = await api.post('/users/me/profile-picture', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    
    // Backend returns S3 URL
    console.log(response.data.profilePictureUrl);
    // Example: https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/profile-pictures/abc123.jpg
  }
};
```

**What happens:**
- Frontend sends **binary file** to backend via multipart/form-data
- Backend receives the file as `IFormFile` object

---

### Step 3: Backend Receives Upload Request

```csharp
// backend/Vector.Api/Controllers/UserController.cs
[HttpPost("me/profile-picture")]
public async Task<IActionResult> UploadProfilePicture(IFormFile file)
{
    // Validate file
    if (file.Length > 5 * 1024 * 1024) 
        return BadRequest("File too large");
    
    // Get user ID from JWT token
    var userId = GetUserIdFromToken();
    
    // Upload to S3 and save URL to database
    using var stream = file.OpenReadStream();
    var pictureUrl = await _userService.UploadProfilePictureAsync(
        userId, 
        stream, 
        file.FileName, 
        file.ContentType
    );
    
    // Return S3 URL to frontend
    return Ok(new { profilePictureUrl = pictureUrl });
}
```

**What happens:**
- Backend validates file (size, type)
- Extracts user ID from JWT token
- Passes file stream to UserService

---

### Step 4: UserService Uploads to S3

```csharp
// backend/Vector.Api/Services/UserService.cs
public async Task<string> UploadProfilePictureAsync(
    Guid userId, 
    Stream fileStream, 
    string fileName, 
    string contentType)
{
    var user = await _context.Users.FindAsync(userId);
    
    // Delete old profile picture from S3 (if exists)
    if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
    {
        await _s3Service.DeleteFileAsync(user.ProfilePictureUrl);
    }
    
    // Upload NEW picture to S3
    var pictureUrl = await _s3Service.UploadFileAsync(
        fileStream, 
        fileName, 
        contentType, 
        "profile-pictures"  // S3 folder
    );
    
    // Save S3 URL to database
    user.ProfilePictureUrl = pictureUrl;
    await _context.SaveChangesAsync();
    
    return pictureUrl;
}
```

**What happens:**
- Finds user in database
- Deletes old picture from S3 (if exists)
- Uploads new picture to S3
- Saves **S3 URL** (not binary) to database
- Returns URL to controller

---

### Step 5: S3Service Uploads to AWS

```csharp
// backend/Vector.Api/Services/S3Service.cs
public async Task<string> UploadFileAsync(
    Stream fileStream, 
    string fileName, 
    string contentType, 
    string folder)
{
    // Generate unique filename
    var fileExtension = Path.GetExtension(fileName);
    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
    var key = $"{folder}/{uniqueFileName}";
    // Example: profile-pictures/abc-123-def-456.jpg
    
    // Upload to S3
    var transferUtility = new TransferUtility(_s3Client);
    await transferUtility.UploadAsync(new TransferUtilityUploadRequest
    {
        InputStream = fileStream,
        Key = key,
        BucketName = "dev-vector-user-uploads",
        ContentType = contentType,
        CannedACL = S3CannedACL.Private  // Private file
    });
    
    // Return S3 URL
    var url = $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key}";
    return url;
    // Example: https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/profile-pictures/abc-123.jpg
}
```

**What happens:**
- Generates unique filename (GUID + extension)
- Uploads binary to S3 bucket
- File stored at: `s3://dev-vector-user-uploads/profile-pictures/abc-123.jpg`
- Returns **public URL** to access the file

---

### Step 6: Database Stores Only the URL

```sql
-- PostgreSQL Users table
UPDATE "Users"
SET "ProfilePictureUrl" = 'https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/profile-pictures/abc-123.jpg',
    "UpdatedAt" = NOW()
WHERE "Id" = 'user-guid';
```

**What's stored:**
- ✅ **ProfilePictureUrl** column: Full S3 URL (string, ~100-200 characters)
- ❌ **NOT** storing: Image binary data (would be megabytes)

**Database size:**
- With URL: ~150 bytes per user
- With binary: ~2-5 MB per user (wasteful!)

---

### Step 7: Frontend Retrieves User Data

```typescript
// frontend/src/hooks/useAuth.tsx
const user = await api.get('/users/me');

// Response includes profilePictureUrl
console.log(user.data.profilePictureUrl);
// "https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/profile-pictures/abc-123.jpg"
```

**What happens:**
- Backend queries database for user
- Returns user data **including** `profilePictureUrl`
- Frontend receives the S3 URL as a string

---

### Step 8: Frontend Displays Image

```tsx
// frontend/src/components/Navbar.tsx
<img 
  src={user.profilePictureUrl} 
  alt="Profile"
/>

// Browser makes HTTP request directly to S3:
// GET https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/profile-pictures/abc-123.jpg
```

**What happens:**
- Browser sees `<img src="https://...s3.amazonaws.com/...">` 
- Browser fetches image **directly from S3** (not from our backend!)
- Image displays in UI

---

## 🎯 Why This Approach?

### ✅ Advantages:

1. **Scalability**
   - S3 handles billions of files effortlessly
   - Database stays small and fast
   - No server bandwidth used for images

2. **Performance**
   - Images served from S3 CDN (fast worldwide)
   - Database queries remain quick
   - No need to stream large files through backend

3. **Cost-Effective**
   - S3 storage: ~$0.023 per GB per month
   - Database storage: ~$0.10+ per GB per month
   - Bandwidth: S3 handles it efficiently

4. **Separation of Concerns**
   - Database: Structured data (user info)
   - S3: Unstructured data (images, videos, files)
   - Each service optimized for its purpose

5. **Easy Backups**
   - S3 has built-in versioning and backup
   - Database backups remain small
   - Can restore images independently

### ❌ Alternative (Storing Binary in Database):

**Problems:**
- Database size explodes (2-5 MB per profile picture)
- Queries become slow (scanning large BLOBs)
- Expensive database storage costs
- Backend must stream images (uses bandwidth)
- Difficult to scale
- Backups become huge

---

## 📊 Data Storage Comparison

### Scenario: 10,000 users with profile pictures

| Storage Method | Database Size | S3 Storage | Cost/Month |
|----------------|---------------|------------|------------|
| **URL in DB, Binary in S3** | ~1.5 MB | 20 GB | $0.46 |
| **Binary in DB** | 20 GB | 0 GB | $2.00+ |

**Savings:** ~77% cost reduction with S3 approach

---

## 🔒 Security: How Images Are Protected

### Current Implementation (Private S3):

1. **Upload:**
   - User must be authenticated (JWT token required)
   - Only owner can upload their profile picture
   - File validation (type, size) on backend

2. **Storage:**
   - Files stored with **private ACL** in S3
   - S3 bucket has "block all public access" enabled
   - Only authorized AWS accounts can access

3. **Access:**
   - Currently: Direct S3 URLs (works because we'll enable public read for profile pictures)
   - Future: Presigned URLs (temporary access for 1 hour)

### Option A: Public Profile Pictures (Recommended for profiles)

```csharp
// Upload with public-read ACL
uploadRequest.CannedACL = S3CannedACL.PublicRead;
```

**Pros:**
- URLs never expire
- Fast access
- No backend involvement

**Cons:**
- Anyone with URL can view image
- Suitable for profile pictures (public by design)

### Option B: Private with Presigned URLs (For sensitive files)

```csharp
// Generate temporary URL (expires in 1 hour)
var url = await _s3Service.GetPresignedUrlAsync(fileKey, expirationMinutes: 60);
```

**Pros:**
- Secure (URLs expire)
- Fine-grained access control

**Cons:**
- URLs expire (must regenerate)
- Backend must generate URLs

---

## 🗄️ Database Schema

```sql
CREATE TABLE "Users" (
  "Id" UUID PRIMARY KEY,
  "Email" VARCHAR(255) NOT NULL,
  "FirstName" VARCHAR(100),
  "LastName" VARCHAR(100),
  -- Profile picture URL stored here (NOT binary!)
  "ProfilePictureUrl" TEXT,  -- Example: https://dev-vector-user-uploads.s3...
  "CreatedAt" TIMESTAMPTZ NOT NULL,
  "UpdatedAt" TIMESTAMPTZ NOT NULL
);
```

**ProfilePictureUrl examples:**
- `NULL` - No profile picture
- `https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/profile-pictures/abc-123.jpg`
- `https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/profile-pictures/def-456.png`

---

## 🔍 How Frontend Finds the Image

### Answer: Frontend reads the URL from database via API

**1. User loads profile page:**
```typescript
// GET /api/users/me
const response = await api.get('/users/me');

// Response includes profilePictureUrl
{
  "id": "user-guid",
  "email": "user@example.com",
  "firstName": "John",
  "profilePictureUrl": "https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/profile-pictures/abc-123.jpg"
}
```

**2. Frontend displays image:**
```tsx
<img src={user.profilePictureUrl} />
```

**3. Browser fetches image from S3:**
```
Browser → S3
GET https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/profile-pictures/abc-123.jpg
→ Returns image binary
→ Displays in <img> tag
```

**Flow:**
```
Frontend → Backend API → Database → Returns URL → Frontend
Frontend → S3 (using URL) → Returns image binary → Displays
```

---

## 📝 Summary

**Question:** "Are we going to store the image path in the database or how will the Frontend find the binary?"

**Answer:**

1. ✅ **We store the image PATH (S3 URL) in the database**
   - Not the binary data
   - Just the URL string (~150 bytes)

2. ✅ **Frontend finds the binary by:**
   - Fetching user data from API
   - API returns `profilePictureUrl` from database
   - Frontend uses URL in `<img src={url}>` tag
   - Browser fetches image directly from S3

3. ✅ **The binary itself is stored in AWS S3**
   - Not in PostgreSQL
   - Not on our backend server
   - S3 is optimized for file storage

**This is the industry-standard approach** used by:
- Facebook (stores images in custom CDN, URLs in database)
- Twitter (stores images in S3, URLs in database)
- Instagram (stores images in CDN, URLs in database)
- Every major web application

---

**Created:** December 3, 2025  
**Status:** ✅ COMPLETE EXPLANATION

