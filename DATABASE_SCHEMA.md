# Database Schema Design

## Entity Relationship Overview

This document describes the database schema for the Vector platform using PostgreSQL.

---

## Core Entities

### 1. Users

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    role VARCHAR(20) NOT NULL DEFAULT 'student' CHECK (role IN ('student', 'coach', 'admin')),
    subscription_status VARCHAR(20) DEFAULT 'free' CHECK (subscription_status IN ('free', 'premium', 'enterprise')),
    subscription_expires_at TIMESTAMP,
    profile_picture_url TEXT,
    bio TEXT,
    job_title VARCHAR(100),
    company VARCHAR(100),
    skills TEXT[],
    email_verified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_users_email (email),
    INDEX idx_users_role (role),
    INDEX idx_users_subscription_status (subscription_status)
);
```

### 2. Courses

```sql
CREATE TABLE courses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    slug VARCHAR(255) UNIQUE NOT NULL,
    description TEXT,
    domain VARCHAR(50) NOT NULL CHECK (domain IN ('PM', 'EM', 'SWE', 'DS', 'DE', 'ML')),
    level VARCHAR(20) NOT NULL CHECK (level IN ('beginner', 'intermediate', 'advanced')),
    instructor_id UUID REFERENCES users(id) ON DELETE SET NULL,
    thumbnail_url TEXT,
    duration_hours INTEGER DEFAULT 0,
    lessons_count INTEGER DEFAULT 0,
    price DECIMAL(10, 2) DEFAULT 0.00,
    published BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_courses_domain (domain),
    INDEX idx_courses_level (level),
    INDEX idx_courses_published (published),
    INDEX idx_courses_instructor (instructor_id)
);
```

### 3. Lessons

```sql
CREATE TABLE lessons (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    content TEXT,
    video_url TEXT,
    video_duration_seconds INTEGER,
    order_index INTEGER NOT NULL,
    lesson_type VARCHAR(20) NOT NULL CHECK (lesson_type IN ('video', 'reading', 'quiz', 'assignment')),
    resources JSONB,
    is_free BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(course_id, order_index),
    INDEX idx_lessons_course (course_id),
    INDEX idx_lessons_order (course_id, order_index)
);
```

### 4. Course Enrollments

```sql
CREATE TABLE course_enrollments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    progress_percentage DECIMAL(5, 2) DEFAULT 0.00 CHECK (progress_percentage >= 0 AND progress_percentage <= 100),
    completed_lessons UUID[] DEFAULT ARRAY[]::UUID[],
    enrolled_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP,
    last_accessed_at TIMESTAMP,
    
    UNIQUE(user_id, course_id),
    INDEX idx_enrollments_user (user_id),
    INDEX idx_enrollments_course (course_id),
    INDEX idx_enrollments_progress (user_id, progress_percentage)
);
```

### 5. Interview Questions

```sql
CREATE TABLE interview_questions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    question_text TEXT NOT NULL,
    domain VARCHAR(50) NOT NULL CHECK (domain IN ('PM', 'EM', 'SWE', 'DS', 'DE', 'ML', 'general')),
    difficulty VARCHAR(20) NOT NULL CHECK (difficulty IN ('easy', 'medium', 'hard')),
    question_type VARCHAR(50) NOT NULL CHECK (question_type IN ('behavioral', 'technical', 'case_study', 'system_design', 'coding')),
    expert_answer_video_url TEXT,
    expert_answer_text TEXT,
    sample_responses JSONB,
    tags TEXT[],
    views_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_questions_domain (domain),
    INDEX idx_questions_difficulty (difficulty),
    INDEX idx_questions_type (question_type),
    INDEX idx_questions_tags USING GIN (tags)
);

-- Full-text search index
CREATE INDEX idx_questions_fulltext ON interview_questions USING GIN(to_tsvector('english', title || ' ' || question_text));
```

### 6. Mock Interviews

```sql
CREATE TABLE mock_interviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    coach_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    scheduled_at TIMESTAMP NOT NULL,
    duration_minutes INTEGER DEFAULT 60,
    status VARCHAR(20) NOT NULL DEFAULT 'scheduled' CHECK (status IN ('scheduled', 'in_progress', 'completed', 'cancelled')),
    meeting_link TEXT,
    meeting_id TEXT,
    domain VARCHAR(50) CHECK (domain IN ('PM', 'EM', 'SWE', 'DS', 'DE', 'ML')),
    feedback_text TEXT,
    student_rating INTEGER CHECK (student_rating >= 1 AND student_rating <= 5),
    coach_rating INTEGER CHECK (coach_rating >= 1 AND coach_rating <= 5),
    student_feedback TEXT,
    coach_feedback TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_interviews_student (student_id),
    INDEX idx_interviews_coach (coach_id),
    INDEX idx_interviews_status (status),
    INDEX idx_interviews_scheduled (scheduled_at)
);
```

### 7. Subscriptions

```sql
CREATE TABLE subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    plan_type VARCHAR(20) NOT NULL CHECK (plan_type IN ('monthly', 'annual', 'lifetime')),
    status VARCHAR(20) NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'cancelled', 'expired', 'past_due')),
    current_period_start TIMESTAMP NOT NULL,
    current_period_end TIMESTAMP NOT NULL,
    stripe_subscription_id VARCHAR(255) UNIQUE,
    stripe_customer_id VARCHAR(255),
    payment_method_id VARCHAR(255),
    price DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'USD',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    cancelled_at TIMESTAMP,
    
    INDEX idx_subscriptions_user (user_id),
    INDEX idx_subscriptions_status (status),
    INDEX idx_subscriptions_stripe (stripe_subscription_id)
);
```

### 8. Payments

```sql
CREATE TABLE payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    subscription_id UUID REFERENCES subscriptions(id) ON DELETE SET NULL,
    amount DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'USD',
    payment_type VARCHAR(20) NOT NULL CHECK (payment_type IN ('subscription', 'service', 'one_time')),
    status VARCHAR(20) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'completed', 'failed', 'refunded')),
    stripe_payment_intent_id VARCHAR(255) UNIQUE,
    stripe_charge_id VARCHAR(255),
    transaction_id VARCHAR(255),
    description TEXT,
    metadata JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_payments_user (user_id),
    INDEX idx_payments_status (status),
    INDEX idx_payments_subscription (subscription_id),
    INDEX idx_payments_stripe (stripe_payment_intent_id)
);
```

### 9. Resume Reviews

```sql
CREATE TABLE resume_reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    reviewer_id UUID REFERENCES users(id) ON DELETE SET NULL,
    resume_url TEXT NOT NULL,
    feedback_text TEXT,
    rating INTEGER CHECK (rating >= 1 AND rating <= 5),
    status VARCHAR(20) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'in_review', 'completed', 'cancelled')),
    submitted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    reviewed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_resume_reviews_user (user_id),
    INDEX idx_resume_reviews_reviewer (reviewer_id),
    INDEX idx_resume_reviews_status (status)
);
```

### 10. Community Posts

```sql
CREATE TABLE community_posts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    post_type VARCHAR(20) NOT NULL CHECK (post_type IN ('question', 'discussion', 'resource', 'announcement')),
    tags TEXT[],
    likes_count INTEGER DEFAULT 0,
    comments_count INTEGER DEFAULT 0,
    views_count INTEGER DEFAULT 0,
    is_pinned BOOLEAN DEFAULT FALSE,
    is_locked BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_posts_user (user_id),
    INDEX idx_posts_type (post_type),
    INDEX idx_posts_tags USING GIN (tags),
    INDEX idx_posts_created (created_at DESC)
);

-- Full-text search index
CREATE INDEX idx_posts_fulltext ON community_posts USING GIN(to_tsvector('english', title || ' ' || content));
```

### 11. Post Comments

```sql
CREATE TABLE post_comments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    post_id UUID NOT NULL REFERENCES community_posts(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    parent_comment_id UUID REFERENCES post_comments(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    likes_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_comments_post (post_id),
    INDEX idx_comments_user (user_id),
    INDEX idx_comments_parent (parent_comment_id),
    INDEX idx_comments_created (created_at)
);
```

### 12. Post Likes

```sql
CREATE TABLE post_likes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    post_id UUID NOT NULL REFERENCES community_posts(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(post_id, user_id),
    INDEX idx_likes_post (post_id),
    INDEX idx_likes_user (user_id)
);
```

### 13. Coach Profiles

```sql
CREATE TABLE coach_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    bio TEXT,
    specializations TEXT[],
    years_of_experience INTEGER,
    company VARCHAR(255),
    rating DECIMAL(3, 2) DEFAULT 0.00 CHECK (rating >= 0 AND rating <= 5),
    total_reviews INTEGER DEFAULT 0,
    hourly_rate DECIMAL(10, 2),
    availability_schedule JSONB, -- Store weekly availability
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_coach_profiles_user (user_id),
    INDEX idx_coach_profiles_active (is_active),
    INDEX idx_coach_profiles_rating (rating DESC)
);
```

### 14. Notifications

```sql
CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL,
    title VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    link TEXT,
    is_read BOOLEAN DEFAULT FALSE,
    metadata JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_notifications_user (user_id),
    INDEX idx_notifications_read (user_id, is_read),
    INDEX idx_notifications_created (created_at DESC)
);
```

### 15. User Activity Logs

```sql
CREATE TABLE user_activity_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    activity_type VARCHAR(50) NOT NULL,
    entity_type VARCHAR(50),
    entity_id UUID,
    metadata JSONB,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_activity_user (user_id),
    INDEX idx_activity_type (activity_type),
    INDEX idx_activity_created (created_at DESC)
);
```

---

## Relationships Summary

```
Users (1) ──< (M) Course Enrollments (M) >── (1) Courses
Users (1) ──< (M) Subscriptions
Users (1) ──< (M) Payments
Users (1) ──< (M) Mock Interviews (as student)
Users (1) ──< (M) Mock Interviews (as coach)
Users (1) ──< (1) Coach Profiles
Users (1) ──< (M) Resume Reviews (as user)
Users (1) ──< (M) Resume Reviews (as reviewer)
Users (1) ──< (M) Community Posts
Users (1) ──< (M) Post Comments
Users (1) ──< (M) Post Likes
Users (1) ──< (M) Notifications

Courses (1) ──< (M) Lessons
Courses (1) ──< (M) Course Enrollments

Community Posts (1) ──< (M) Post Comments
Community Posts (1) ──< (M) Post Likes
Post Comments (1) ──< (M) Post Comments (self-referencing for nested comments)
```

---

## Indexes Strategy

### Performance Indexes
- All foreign keys are indexed
- Frequently queried columns (status, created_at, user_id) are indexed
- Full-text search indexes on content fields
- Composite indexes for common query patterns

### Unique Constraints
- Email addresses (users)
- Course slugs
- User-course enrollment pairs
- User-post like pairs
- Stripe subscription IDs

---

## Data Types Notes

- **UUID**: Used for all primary keys for better security and distributed system support
- **JSONB**: Used for flexible, queryable JSON data (PostgreSQL specific)
- **TEXT**: Used for variable-length strings
- **DECIMAL**: Used for monetary values to avoid floating-point errors
- **TIMESTAMP**: All timestamps stored in UTC
- **Arrays**: PostgreSQL native array types for tags, skills, etc.

---

## Database Migration Strategy

1. Use migration tools (Prisma, TypeORM, or Flyway)
2. Version control all migrations
3. Create indexes after data insertion for better performance
4. Use transactions for multi-table operations
5. Implement rollback strategies

---

## Backup and Recovery

1. **Daily Backups**: Full database backups
2. **Transaction Logs**: Continuous WAL (Write-Ahead Logging) archiving
3. **Point-in-Time Recovery**: Ability to restore to any point in time
4. **Read Replicas**: For scaling read operations
5. **Backup Testing**: Regular restore tests

---

## Security Considerations

1. **Row-Level Security**: Implement RLS policies for multi-tenant data isolation
2. **Encryption**: Encrypt sensitive fields (email, payment info)
3. **Connection Pooling**: Use PgBouncer or similar
4. **Query Optimization**: Regular query analysis and optimization
5. **Access Control**: Limit database access to application servers only

