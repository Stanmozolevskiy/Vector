# S3 Bucket for User Uploads
resource "aws_s3_bucket" "user_uploads" {
  bucket = "${var.environment}-vector-user-uploads"

  tags = {
    Name        = "${var.environment}-vector-user-uploads"
    Environment = var.environment
  }
}

# Enable ACLs for the bucket (required for PublicRead ACL on objects)
resource "aws_s3_bucket_ownership_controls" "user_uploads" {
  bucket = aws_s3_bucket.user_uploads.id

  rule {
    object_ownership = "BucketOwnerPreferred"
  }
}

# S3 Bucket Versioning
resource "aws_s3_bucket_versioning" "user_uploads" {
  bucket = aws_s3_bucket.user_uploads.id

  versioning_configuration {
    status = var.environment == "prod" ? "Enabled" : "Disabled"
  }
}

# S3 Bucket Server-Side Encryption
resource "aws_s3_bucket_server_side_encryption_configuration" "user_uploads" {
  bucket = aws_s3_bucket.user_uploads.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# S3 Bucket Public Access Block
# Profile pictures need public read access, so we allow public ACLs
resource "aws_s3_bucket_public_access_block" "user_uploads" {
  bucket = aws_s3_bucket.user_uploads.id

  block_public_acls       = false  # Allow public ACLs for profile pictures
  block_public_policy     = false  # Allow bucket policies
  ignore_public_acls      = false  # Respect public ACLs
  restrict_public_buckets = false  # Allow public bucket access
}

# S3 Bucket CORS Configuration
resource "aws_s3_bucket_cors_configuration" "user_uploads" {
  bucket = aws_s3_bucket.user_uploads.id

  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET", "PUT", "POST", "DELETE", "HEAD"]
    allowed_origins = var.allowed_origins
    expose_headers  = ["ETag"]
    max_age_seconds = 3000
  }
}

# S3 Bucket Lifecycle Configuration
resource "aws_s3_bucket_lifecycle_configuration" "user_uploads" {
  bucket = aws_s3_bucket.user_uploads.id

  rule {
    id     = "delete-old-versions"
    status = var.environment == "prod" ? "Enabled" : "Disabled"

    filter {}

    noncurrent_version_expiration {
      noncurrent_days = 90
    }
  }

  rule {
    id     = "delete-incomplete-multipart"
    status = "Enabled"

    filter {}

    abort_incomplete_multipart_upload {
      days_after_initiation = 7
    }
  }
}

# S3 Bucket Policy (for application access and public profile pictures)
resource "aws_s3_bucket_policy" "user_uploads" {
  bucket = aws_s3_bucket.user_uploads.id
  depends_on = [
    aws_s3_bucket_public_access_block.user_uploads,
    aws_s3_bucket_ownership_controls.user_uploads
  ]

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowApplicationAccess"
        Effect = "Allow"
        Principal = {
          AWS = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:root"
        }
        Action = [
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject",
          "s3:PutObjectAcl"
        ]
        Resource = "${aws_s3_bucket.user_uploads.arn}/*"
      },
      {
        Sid       = "PublicReadProfilePictures"
        Effect    = "Allow"
        Principal = "*"
        Action    = "s3:GetObject"
        Resource  = "${aws_s3_bucket.user_uploads.arn}/profile-pictures/*"
      }
    ]
  })
}

data "aws_caller_identity" "current" {}

