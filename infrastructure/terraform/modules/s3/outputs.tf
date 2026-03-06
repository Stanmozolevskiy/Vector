output "bucket_name" {
  description = "S3 bucket name"
  value       = aws_s3_bucket.user_uploads.id
}

output "bucket_arn" {
  description = "S3 bucket ARN"
  value       = aws_s3_bucket.user_uploads.arn
}

output "bucket_domain_name" {
  description = "S3 bucket domain name"
  value       = aws_s3_bucket.user_uploads.bucket_domain_name
}

