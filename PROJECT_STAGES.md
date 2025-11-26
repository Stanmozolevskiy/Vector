# Project Implementation Stages

## Overview

This document outlines the staged approach for building the Exponent Alternative platform, prioritizing features in the following order:
1. **Stage 1**: User Management & Authentication
2. **Stage 2**: Mock Interviews System
3. **Stage 3**: Resume Review Service
4. **Stage 4**: Courses & Learning Content

---

## Platform Specifications

- **Frontend**: React 18+ with TypeScript (Browser-only, no mobile app)
- **Backend**: Node.js + Express + TypeScript
- **Database**: PostgreSQL + Redis
- **Cloud**: AWS
- **SMS**: Optional (to be added later)

---

## Stage 1: User Management & Authentication

### Goals
- Complete user registration and authentication system
- User profile management
- Role-based access control (student, coach, admin)
- Subscription management foundation
- Payment processing for subscriptions

### Features to Implement

#### 1.1 Authentication Service
- [ ] User registration (email/password)
- [ ] Email verification
- [ ] Login/logout
- [ ] Password reset flow
- [ ] JWT token generation and validation
- [ ] Refresh token rotation
- [ ] OAuth integration (Google, LinkedIn) - Optional
- [ ] Session management with Redis

#### 1.2 User Profile Management
- [ ] User profile CRUD operations
- [ ] Profile picture upload (S3)
- [ ] Bio and personal information
- [ ] Skills and job title
- [ ] Profile visibility settings

#### 1.3 Role Management
- [ ] Role-based access control (RBAC)
- [ ] Student role (default)
- [ ] Coach role (with application/approval)
- [ ] Admin role
- [ ] Role assignment and permissions

#### 1.4 Subscription System
- [ ] Subscription plan definitions
- [ ] Plan selection and upgrade/downgrade
- [ ] Subscription status tracking
- [ ] Expiration and renewal logic

#### 1.5 Payment Integration
- [ ] Stripe account setup
- [ ] Payment method management
- [ ] Subscription billing
- [ ] Invoice generation
- [ ] Payment webhook handling
- [ ] Refund processing

#### 1.6 Email Notifications
- [ ] Welcome emails
- [ ] Email verification emails
- [ ] Password reset emails
- [ ] Subscription confirmation emails
- [ ] Payment receipt emails

### Database Tables Required
- `users`
- `user_profiles`
- `roles`
- `user_roles`
- `subscriptions`
- `payments`
- `email_verifications`
- `password_resets`

### API Endpoints

#### Authentication
```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/logout
POST   /api/auth/refresh-token
POST   /api/auth/verify-email
POST   /api/auth/forgot-password
POST   /api/auth/reset-password
```

#### User Management
```
GET    /api/users/me
PUT    /api/users/me
DELETE /api/users/me
GET    /api/users/:id (public profile)
PUT    /api/users/me/password
POST   /api/users/me/profile-picture
DELETE /api/users/me/profile-picture
```

#### Subscriptions
```
GET    /api/subscriptions/plans
GET    /api/subscriptions/me
POST   /api/subscriptions/subscribe
PUT    /api/subscriptions/cancel
GET    /api/subscriptions/invoices
```

#### Payments
```
POST   /api/payments/methods
GET    /api/payments/methods
DELETE /api/payments/methods/:id
POST   /api/payments/webhook (Stripe webhook)
```

### Frontend Pages/Components

#### Authentication Pages
- Registration page
- Login page
- Email verification page
- Forgot password page
- Reset password page

#### User Dashboard
- Profile settings page
- Subscription management page
- Payment methods page
- Account settings page

#### Components
- Header/Navigation (with user menu)
- User profile card
- Subscription plan selector
- Payment method form

### Timeline: 4-6 weeks

### Success Criteria
- Users can register and log in
- Email verification works
- Password reset flow works
- Users can manage their profiles
- Subscription plans can be selected and paid for
- Role-based access is enforced
- All payment flows are secure and tested

---

## Stage 2: Mock Interviews System

### Goals
- Complete mock interview scheduling system
- Coach profile and availability management
- Video conferencing integration
- Interview feedback and rating system

### Features to Implement

#### 2.1 Coach Management
- [ ] Coach profile creation/editing
- [ ] Coach specialization selection (PM, SWE, DS, etc.)
- [ ] Coach availability management (weekly schedule)
- [ ] Coach hourly rate setting
- [ ] Coach bio and credentials
- [ ] Coach application/approval workflow
- [ ] Coach ratings and reviews display

#### 2.2 Interview Scheduling
- [ ] Browse available coaches
- [ ] Filter coaches by specialization, availability, rating
- [ ] View coach calendar and available slots
- [ ] Book interview slot
- [ ] Reschedule interviews
- [ ] Cancel interviews (with policies)
- [ ] Interview confirmation emails

#### 2.3 Video Conferencing
- [ ] Zoom API integration (or Google Meet)
- [ ] Automatic meeting creation on booking
- [ ] Meeting link delivery
- [ ] Meeting reminder notifications
- [ ] Join meeting functionality
- [ ] Recording consent and handling

#### 2.4 Interview Management
- [ ] Interview status tracking (scheduled, in-progress, completed, cancelled)
- [ ] Interview history view
- [ ] Upcoming interviews dashboard
- [ ] Interview details page

#### 2.5 Feedback System
- [ ] Post-interview feedback form (for coach)
- [ ] Student rating and feedback (for student)
- [ ] Feedback templates
- [ ] Feedback viewing (for both parties)
- [ ] Feedback analytics

#### 2.6 Notifications
- [ ] Interview booking confirmation (email)
- [ ] Interview reminder (24 hours before, 1 hour before)
- [ ] Interview cancellation notifications
- [ ] Feedback submission notifications

### Database Tables Required
- `coach_profiles` (already created in Stage 1)
- `mock_interviews`
- `coach_availability_slots`
- `interview_feedback`
- `coach_ratings`

### API Endpoints

#### Coach Management
```
GET    /api/coaches
GET    /api/coaches/:id
POST   /api/coaches/apply (become a coach)
PUT    /api/coaches/me
GET    /api/coaches/me/availability
PUT    /api/coaches/me/availability
GET    /api/coaches/:id/availability
```

#### Interview Scheduling
```
GET    /api/interviews
POST   /api/interviews
GET    /api/interviews/:id
PUT    /api/interviews/:id/reschedule
DELETE /api/interviews/:id
GET    /api/interviews/upcoming
GET    /api/interviews/history
```

#### Video Conferencing
```
POST   /api/interviews/:id/create-meeting
GET    /api/interviews/:id/meeting-link
```

#### Feedback
```
POST   /api/interviews/:id/feedback
GET    /api/interviews/:id/feedback
PUT    /api/interviews/:id/feedback/:feedbackId
POST   /api/interviews/:id/rating
```

### Frontend Pages/Components

#### Coach Pages
- Coach directory/browse page
- Coach profile detail page
- Become a coach application page
- Coach dashboard (for coaches)
- Availability management page

#### Interview Pages
- Book interview page (with calendar)
- Interview dashboard (upcoming/history)
- Interview detail page
- Join interview page
- Feedback submission page

#### Components
- Coach card component
- Availability calendar component
- Interview booking form
- Interview feedback form
- Rating component

### Timeline: 6-8 weeks

### Success Criteria
- Coaches can create profiles and set availability
- Students can browse and book interviews
- Video meetings are automatically created
- Interviews can be rescheduled/cancelled
- Feedback and ratings can be submitted
- All notifications are sent correctly

---

## Stage 3: Resume Review Service

### Goals
- Complete resume review request and management system
- Resume upload and storage
- Reviewer assignment
- Feedback delivery system

### Features to Implement

#### 3.1 Resume Upload & Storage
- [ ] Resume upload (PDF/DOCX)
- [ ] File validation (size, type)
- [ ] Virus scanning (optional)
- [ ] Secure storage in S3
- [ ] Resume version history
- [ ] Resume download/access

#### 3.2 Review Request System
- [ ] Create review request
- [ ] Select review type (format review, content review, comprehensive)
- [ ] Add specific questions or focus areas
- [ ] Review request status tracking

#### 3.3 Reviewer Assignment
- [ ] Automated reviewer assignment (by domain/expertise)
- [ ] Manual reviewer selection (optional)
- [ ] Reviewer availability check
- [ ] Review queue management

#### 3.4 Review Process
- [ ] Reviewer accepts/declines review
- [ ] Review in-progress status
- [ ] Reviewer feedback submission
- [ ] Structured feedback form
- [ ] Resume annotations (optional)
- [ ] Review completion

#### 3.5 Feedback Delivery
- [ ] Feedback notification email
- [ ] Feedback viewing interface
- [ ] Feedback download (PDF)
- [ ] Feedback rating (by user)
- [ ] Review revision requests

#### 3.6 Review History
- [ ] User's review history
- [ ] Reviewer's completed reviews
- [ ] Review analytics and statistics

### Database Tables Required
- `resume_reviews` (already created in Stage 1)
- `resume_files`
- `review_assignments`
- `review_feedback`

### API Endpoints

#### Resume Management
```
POST   /api/resumes/upload
GET    /api/resumes
GET    /api/resumes/:id
DELETE /api/resumes/:id
GET    /api/resumes/:id/download
```

#### Review Requests
```
POST   /api/resume-reviews
GET    /api/resume-reviews
GET    /api/resume-reviews/:id
PUT    /api/resume-reviews/:id
DELETE /api/resume-reviews/:id
GET    /api/resume-reviews/my-reviews
```

#### Review Process
```
POST   /api/resume-reviews/:id/assign
POST   /api/resume-reviews/:id/accept
POST   /api/resume-reviews/:id/decline
POST   /api/resume-reviews/:id/submit-feedback
GET    /api/resume-reviews/:id/feedback
```

### Frontend Pages/Components

#### Resume Pages
- Resume upload page
- My resumes page
- Resume detail page
- Request review page

#### Review Pages
- Review request page
- Review dashboard (for users)
- Reviewer dashboard (for reviewers)
- Review detail page
- Feedback view page

#### Components
- File upload component (drag & drop)
- Review request form
- Feedback display component
- Review status indicator

### Timeline: 4-5 weeks

### Success Criteria
- Users can upload resumes securely
- Review requests can be created
- Reviewers can be assigned automatically
- Feedback can be submitted and viewed
- All notifications work correctly

---

## Stage 4: Courses & Learning Content

### Goals
- Complete course management system
- Lesson delivery (video streaming)
- Progress tracking
- Interview question bank

### Features to Implement

#### 4.1 Course Management
- [ ] Course creation (admin/instructors)
- [ ] Course CRUD operations
- [ ] Course categorization (by domain)
- [ ] Course previews and descriptions
- [ ] Course pricing (free/premium)
- [ ] Course publishing workflow

#### 4.2 Lesson Management
- [ ] Lesson creation within courses
- [ ] Lesson ordering and organization
- [ ] Multiple lesson types (video, reading, quiz)
- [ ] Lesson content editing
- [ ] Lesson resources (downloadable materials)

#### 4.3 Video Streaming
- [ ] Video upload to S3
- [ ] Video transcoding (multiple qualities)
- [ ] Video streaming via CDN (CloudFront)
- [ ] Video player integration (Video.js)
- [ ] Progress tracking (watch time)
- [ ] Video subtitles/captions (optional)

#### 4.4 Enrollment System
- [ ] Course browsing and search
- [ ] Course enrollment (free/premium)
- [ ] Enrollment verification (subscription check)
- [ ] Course access control
- [ ] Enrollment history

#### 4.5 Progress Tracking
- [ ] Lesson completion tracking
- [ ] Course progress percentage
- [ ] Watch history
- [ ] Completion certificates (optional)
- [ ] Progress analytics

#### 4.6 Interview Question Bank
- [ ] Question creation (admin)
- [ ] Question categorization (domain, difficulty, type)
- [ ] Expert answer videos
- [ ] Question browsing and filtering
- [ ] Question search functionality
- [ ] Save/bookmark questions
- [ ] Practice statistics

#### 4.7 Content Delivery
- [ ] Responsive video player
- [ ] Reading content display
- [ ] Quiz/interactive elements (optional)
- [ ] Downloadable resources
- [ ] Mobile-responsive course pages

### Database Tables Required
- `courses`
- `lessons`
- `course_enrollments`
- `interview_questions`
- `lesson_completions`
- `question_bookmarks`

### API Endpoints

#### Course Management
```
GET    /api/courses
GET    /api/courses/:id
POST   /api/courses (admin)
PUT    /api/courses/:id (admin)
DELETE /api/courses/:id (admin)
GET    /api/courses/:id/lessons
```

#### Enrollment
```
POST   /api/courses/:id/enroll
GET    /api/courses/my-courses
GET    /api/courses/:id/enrollment-status
```

#### Lessons
```
GET    /api/courses/:id/lessons/:lessonId
POST   /api/lessons/:id/complete
GET    /api/lessons/:id/progress
```

#### Interview Questions
```
GET    /api/questions
GET    /api/questions/:id
POST   /api/questions (admin)
PUT    /api/questions/:id (admin)
POST   /api/questions/:id/bookmark
GET    /api/questions/bookmarked
GET    /api/questions/search
```

#### Progress
```
GET    /api/courses/:id/progress
GET    /api/me/progress
GET    /api/me/statistics
```

### Frontend Pages/Components

#### Course Pages
- Course catalog/browse page
- Course detail page
- Course player/lesson view page
- My courses dashboard
- Course progress page

#### Question Bank Pages
- Question bank browse page
- Question detail page
- My bookmarked questions
- Practice statistics page

#### Components
- Course card component
- Video player component
- Progress bar component
- Lesson list component
- Question card component
- Search and filter component

### Timeline: 8-10 weeks

### Success Criteria
- Courses can be created and published
- Video lessons stream correctly
- Users can enroll and track progress
- Interview questions are searchable and viewable
- All content is accessible based on subscription

---

## Summary Timeline

| Stage | Features | Timeline | Total Duration |
|-------|----------|----------|----------------|
| Stage 1 | User Management & Auth | 4-6 weeks | 4-6 weeks |
| Stage 2 | Mock Interviews | 6-8 weeks | 10-14 weeks |
| Stage 3 | Resume Reviews | 4-5 weeks | 14-19 weeks |
| Stage 4 | Courses & Content | 8-10 weeks | 22-29 weeks |

**Total Project Duration: 5.5 - 7 months**

---

## Dependencies Between Stages

### Stage 1 → Stage 2
- User authentication required
- User roles (coach role) required
- Subscription system needed for premium features

### Stage 1 → Stage 3
- User authentication required
- Reviewer role needed
- Payment system for paid reviews

### Stage 1 → Stage 4
- User authentication required
- Subscription system for course access
- Payment system for course purchases

### Stage 2 → Stage 4
- No direct dependency (can be developed in parallel after Stage 1)

### Stage 3 → Stage 4
- No direct dependency (can be developed in parallel after Stage 1)

---

## Risk Mitigation by Stage

### Stage 1 Risks
- **Payment Integration Complexity**: Start with basic Stripe integration, add features incrementally
- **Email Deliverability**: Use SendGrid from start, monitor bounce rates
- **Security Concerns**: Implement security best practices from beginning (JWT, password hashing, etc.)

### Stage 2 Risks
- **Video API Integration**: Choose one provider (Zoom) initially, keep abstraction layer for future changes
- **Scheduling Complexity**: Start with basic availability, add advanced features later
- **Timezone Handling**: Use UTC for all timestamps, convert in frontend

### Stage 3 Risks
- **File Upload Security**: Implement file validation and virus scanning from start
- **Reviewer Assignment**: Start with manual assignment, automate later

### Stage 4 Risks
- **Video Streaming Costs**: Optimize video quality and CDN usage
- **Content Creation Time**: Allow buffer time for content creation
- **Scalability**: Design for CDN from start

---

## Next Steps After Stage Completion

After completing all stages:
- Performance optimization
- Advanced analytics
- SEO optimization
- Marketing site
- Public beta testing
- Production launch

