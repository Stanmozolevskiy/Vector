# Vector - Interview Preparation Platform

Vector is a comprehensive frontend website for live mock interview preparation, similar to Exponent. The platform helps candidates prepare for technical, behavioral, and system design interviews with expert guidance and realistic practice.

## 🌟 Project Overview

**Project Name:** Vector  
**Type:** Static Website (HTML, CSS, JavaScript)  
**Purpose:** Live mock interview preparation platform for tech industry candidates

## ✅ Completed Features

### 1. User Authentication & Authorization
- ✅ **Login Page** (`login.html`) - Email/password login with social auth options (Google, LinkedIn)
- ✅ **Registration Page** (`register.html`) - New user signup with form validation
- ✅ **Password Recovery** (`forgot-password.html`) - Password reset flow
- ✅ **Form Validation** - Client-side email and password validation
- ✅ **Session Management** - LocalStorage-based authentication state (simulated)
- ✅ **Protected Routes** - Route protection logic for authenticated pages

### 2. Course Catalog & Enrollment
- ✅ **Course Listing** (`courses.html`) - Browse courses by category with filters
- ✅ **Course Details** (`course-detail.html`) - Comprehensive course information with:
  - Course curriculum with expandable sections
  - Instructor profiles and ratings
  - Student reviews and testimonials
  - Learning objectives and requirements
  - Enrollment CTA and pricing
- ✅ **Filters & Search** - Filter by category, level, and price
- ✅ **Course Cards** - Display pricing, ratings, duration, and enrollment count
- ✅ **Progress Tracking** - Visual progress indicators for enrolled courses

### 3. Video Streaming for Lessons
- ✅ **Video Player** (`lesson-player.html`) - Full-featured video lesson interface with:
  - Video controls (play, pause, volume, speed, fullscreen)
  - Progress bar with seek functionality
  - Lesson navigation sidebar
  - Playback speed controls (0.5x - 2x)
- ✅ **Lesson Content Tabs** - Overview, Notes, Q&A, Resources
- ✅ **Note Taking** - Students can add timestamped notes
- ✅ **Practice Problems** - Integrated coding exercises
- ✅ **Curriculum Navigation** - Expandable section tree with completion status

### 4. Interview Question Bank
- ✅ **Question Browser** (`questions.html`) - Searchable database with:
  - 1000+ curated interview questions across 3 types
  - **Question Type Filter** - Coding (642), Whiteboard Design (218), Behavioral (140)
  - **Difficulty Filter** - Easy (334), Medium (456), Hard (210)
  - **Topic Filter** - Array, String, Hash Table, Dynamic Programming, Tree, Graph, Binary Search, Two Pointers
  - **Company Filter** - Google, Meta, Amazon, Microsoft, Apple, Netflix, Uber, Airbnb
  - **Status Tracking** - Solved (234), Attempted (67), To Do (699)
  - Scrollable filter sections with custom checkboxes
  - Question ID display in monospace font
  - Type badges with icons (Coding, Design, Behavioral)
- ✅ **Question Table** - Enhanced display with:
  - Status icon (solved/attempted/todo)
  - Question ID in monospace
  - Title with tags
  - Type badge (Coding/Design/Behavioral)
  - Difficulty badge with color coding
  - Acceptance rate
  - Solve button with navigation
- ✅ **Three Question Types with Custom Templates:**
  - **Coding Questions** (`question-detail-coding.html`) - LeetCode-style interface:
    - Split panel layout (problem description + code editor)
    - Dark theme code editor with syntax highlighting
    - Multiple language support (JavaScript, Python, Java, C++, C#, Go)
    - Test case input and console output
    - Run Code and Submit buttons
    - Problem description with examples, constraints, and hints
    - Resizable panels for custom workspace
  - **Whiteboard Design Questions** (`question-detail-design.html`) - System design template:
    - Problem statement and requirements (functional/non-functional)
    - Scale estimation with metrics cards
    - Whiteboard canvas for drawing architecture diagrams
    - Notes area for calculations and key points
    - Hints and solution tabs with component diagrams
    - Database schema examples
    - API endpoint definitions
    - Discussion points and trade-offs
  - **Behavioral Questions** (`question-detail-behavioral.html`) - STAR method template:
    - Question variations and alternative phrasings
    - STAR framework guide (Situation, Task, Action, Result)
    - Tips for answering with expert advice
    - Sample answers with analysis
    - Interactive answer builder with character counters
    - Timer for practice sessions
    - Quick tips sidebar
    - Category selection (Leadership, Conflict, Failure, etc.)
- ✅ **Statistics Dashboard** - Track progress by question type and difficulty
- ✅ **Search & Filters** - Real-time question search with advanced filtering
- ✅ **Professional Pagination** - 20 questions per page with modern controls

### 5. Mock Interview Scheduling
- ✅ **Booking Interface** (`mock-interviews.html`) - Complete scheduling system with:
  - Interview type selection (Technical, System Design, Behavioral, Product)
  - Interviewer profiles with ratings and specialties
  - Interactive calendar for date selection
  - Available time slot picker
  - Booking summary with pricing
- ✅ **Interviewer Selection** - Choose from experienced interviewers from FAANG companies
- ✅ **Calendar Interface** - Month view with available dates
- ✅ **Time Slots** - Display and select available time slots

### 6. Payment Processing
- ✅ **Pricing Page** (`pricing.html`) - Three-tier pricing structure:
  - **Free Plan** - Access to 50 questions, 3 courses, community support
  - **Pro Plan** - $29/month - 1000+ questions, all courses, 2 mock interviews/month
  - **Enterprise Plan** - $99/month - Unlimited interviews, team management, SSO
- ✅ **Billing Toggle** - Switch between monthly and annual pricing (30% discount)
- ✅ **Feature Comparison** - Detailed feature lists for each plan
- ✅ **FAQ Section** - Collapsible accordion with common questions
- ✅ **Checkout Flow UI** - Payment form styling (Stripe-style design)

### 7. User Profiles & Dashboard
- ✅ **Dashboard** (`dashboard.html`) - Comprehensive user overview with:
  - Learning statistics (courses enrolled, problems solved, streak)
  - Continue learning section with course progress
  - Activity chart (Chart.js integration)
  - Problem-solving progress by difficulty
  - Upcoming mock interviews
  - Recent achievements and badges
  - Weekly learning goals with checkboxes
- ✅ **Profile Settings** (`profile.html`) - Complete profile management with:
  - Personal information editor (name, email, bio, location)
  - Profile picture upload and management
  - Password change functionality
  - Two-factor authentication setup
  - Active session management
  - Subscription management with cancel option
  - Payment method updates
  - Billing history table
  - Notification preferences with toggles
  - Privacy settings
  - Data export and account deletion
- ✅ **User Menu** - Avatar, profile dropdown, logout functionality
- ✅ **Progress Visualization** - Charts and progress bars for learning metrics

### 8. Coach Role & Features
- ✅ **Browse Coaches** (`coaches.html`) - Discover and filter expert coaches:
  - Search by name, company, or expertise
  - Filter by specialization, rating, price range, company
  - Grid/List view toggle
  - Sort by recommended, rating, price, experience
  - Coach cards with rating, rate, specializations
  - Pagination support
  - 50+ sample coaches
- ✅ **Coach Detail** (`coach-detail.html`) - View individual coach profiles:
  - Complete coach information
  - Bio and specializations
  - Experience and statistics
  - Student reviews and ratings
  - Booking card with session types
  - Quick facts and availability
  - Secure booking system
- ✅ **Coach Profile** (`coach-profile.html`) - Manage coach profile (Coach role):
  - View/Edit mode toggle
  - Profile information (name, title, company, bio)
  - Coaching information (specializations, rate, availability)
  - Professional links (LinkedIn, GitHub, website)
  - Profile preview
  - Form validation
  - Character count for bio
- ✅ **Coach Dashboard** (`coach-dashboard.html`) - Overview for coaches with:
  - Course statistics and performance metrics
  - Student engagement and completion rates
  - Upcoming sessions and schedule
  - Revenue and earnings tracking
  - Recent student feedback
- ✅ **Course Creation** (`coach-course-create.html`) - Full course builder with:
  - Course information (title, description, pricing)
  - Curriculum builder with sections and lessons
  - Video upload and management
  - Pricing and accessibility settings
  - Course preview functionality
  
### 9. Administrator Role & Features
- ✅ **Admin Dashboard** (`admin-dashboard.html`) - Platform overview with:
  - Platform-wide statistics
  - User growth and engagement metrics
  - Revenue and subscription analytics
  - System health monitoring
  - Quick actions panel
- ✅ **User Management** (`admin-users.html`) - Complete user administration:
  - User list with search and filters
  - User status management (active, suspended, banned)
  - Role assignment (User, Coach, Admin)
  - Subscription management
  - User activity logs
  - Bulk actions
- ✅ **Question Management** (`admin-questions.html`) - LeetCode-style question database:
  - Question list with 1,234+ practice questions
  - Advanced filters (difficulty, category, status)
  - Full CRUD operations (Create, Read, Update, Delete)
  - Question editor with:
    - Problem description and constraints
    - Multiple examples with explanations
    - Solution code and hints
    - Time and space complexity
    - Tags and company associations
    - Publishing status (Draft/Published)
  - Bulk operations and CSV export
  - Statistics by difficulty level

## 🎨 Design Features

### Modern, Professional Aesthetic
- **Color Scheme:** 
  - Primary: Indigo (#6366f1) to Purple (#8b5cf6) gradients
  - Clean, minimalist design with excellent readability
  - Professional tech industry aesthetic
- **Typography:** Inter font family for modern, clean look
- **Responsive Design:** Mobile-first approach, works on all devices
- **UI Components:** Cards, modals, dropdowns, tabs, accordions
- **Icons:** Font Awesome 6.4.0 for consistent iconography
- **Animations:** Smooth transitions and hover effects

### Navigation & Layout
- **Sticky Navigation Bar** - Persistent header with branding and main menu
- **Breadcrumbs** - Clear navigation hierarchy on detail pages
- **Footer** - Company info, product links, social media links
- **User-Friendly** - Intuitive navigation and clear CTAs throughout

## 📁 Project Structure

```
vector/
├── index.html                    # Landing page with hero, features, testimonials
├── login.html                    # User login page
├── register.html                 # User registration page
├── forgot-password.html          # Password recovery page
├── courses.html                  # Course catalog with filters
├── course-detail.html            # Individual course details
├── lesson-player.html            # Video lesson player
├── questions.html                # Interview question bank with type filters
├── question-detail.html          # Original question detail (legacy)
├── question-detail-coding.html   # Coding question template (LeetCode-style)
├── question-detail-design.html   # Whiteboard design question template
├── question-detail-behavioral.html # Behavioral question template (STAR method)
├── mock-interviews.html          # Mock interview booking
├── pricing.html                  # Pricing plans and FAQ
├── dashboard.html                # User dashboard
├── profile.html                  # User profile settings
├── coaches.html                  # Browse coaches page
├── coach-detail.html             # Individual coach profile view
├── coach-profile.html            # Coach profile management (coach role)
├── coach-dashboard.html          # Coach overview and statistics
├── coach-course-create.html      # Course creation tool for coaches
├── admin-dashboard.html          # Administrator dashboard
├── admin-users.html              # User management interface
├── admin-questions.html          # Question database management
├── css/
│   ├── style.css                # Global styles and components
│   ├── auth.css                 # Authentication page styles
│   ├── courses.css              # Course page styles
│   ├── coaches.css              # Coach browsing and profile styles
│   ├── player.css               # Video player styles
│   ├── questions.css            # Question bank styles with type filters
│   ├── question-detail.css      # Base question detail and code editor styles
│   ├── question-design.css      # Whiteboard design question styles
│   ├── question-behavioral.css  # Behavioral question styles
│   ├── mock.css                 # Mock interview styles
│   ├── pricing.css              # Pricing page styles
│   ├── dashboard.css            # Dashboard styles
│   ├── profile.css              # Profile page styles
│   ├── admin.css                # Admin interface styles
│   └── admin-questions.css      # Admin question management styles
├── js/
│   ├── main.js                  # Core functionality, navigation, utilities
│   ├── auth.js                  # Authentication logic
│   ├── courses.js               # Course page interactions
│   ├── coaches.js               # Coach browsing and filtering
│   ├── player.js                # Video player controls
│   ├── questions.js             # Question filtering, search, and type support
│   ├── question-detail.js       # Code editor and test execution (coding)
│   ├── question-design.js       # Whiteboard canvas and design question logic
│   ├── question-behavioral.js   # STAR framework and behavioral question logic
│   ├── mock.js                  # Mock interview booking logic
│   ├── pricing.js               # Pricing toggle and FAQ
│   ├── dashboard.js             # Dashboard charts and interactions
│   ├── profile.js               # Profile settings and account management
│   └── admin-questions.js       # Admin question management logic
└── README.md                    # Project documentation
```

## 🚀 Getting Started

### Prerequisites
- Modern web browser (Chrome, Firefox, Safari, Edge)
- No server or build tools required - pure static HTML/CSS/JS

### Installation & Running

1. **Download/Clone the project files**

2. **Open in browser**
   - Simply open `index.html` in your web browser
   - Or use a local server (recommended):
     ```bash
     # Using Python 3
     python -m http.server 8000
     
     # Using Node.js http-server
     npx http-server
     ```

3. **Navigate the site**
   - Start at the landing page (index.html)
   - Click "Get Started" to view registration
   - Explore courses, questions, and mock interviews
   - "Log in" to access the dashboard

### Demo Credentials
Since this is a frontend demo, any email/password combination will work:
- Email: `demo@vector.com`
- Password: `password123` (minimum 8 characters)

## 🔗 Functional Entry Points

### Public Pages (No Authentication Required)
- **/** (`index.html`) - Landing page with platform overview
- **/courses** (`courses.html`) - Browse all available courses
- **/questions** (`questions.html`) - View question bank with type filters (Coding, Design, Behavioral)
- **/question/coding/:id** (`question-detail-coding.html`) - LeetCode-style coding challenge
- **/question/design/:id** (`question-detail-design.html`) - System design whiteboard problem
- **/question/behavioral/:id** (`question-detail-behavioral.html`) - Behavioral interview question with STAR framework
- **/coaches** (`coaches.html`) - Browse expert coaches with advanced filters
- **/coach/:id** (`coach-detail.html`) - View individual coach profiles
- **/mock-interviews** (`mock-interviews.html`) - View mock interview options
- **/pricing** (`pricing.html`) - View pricing plans and FAQ
- **/login** (`login.html`) - User login
- **/register** (`register.html`) - New user signup
- **/forgot-password** (`forgot-password.html`) - Password recovery

### Protected Pages (Authentication Required)
- **/dashboard** (`dashboard.html`) - User dashboard with statistics and progress
- **/profile** (`profile.html`) - User profile settings and account management
- **/lesson-player** (`lesson-player.html`) - Video lesson player for enrolled courses
- **/course-detail** (`course-detail.html`) - Full course details and enrollment

### Coach Pages (Coach Role Required)
- **/coach-dashboard** (`coach-dashboard.html`) - Coach overview with course and student metrics
- **/coach-course-create** (`coach-course-create.html`) - Create and manage courses
- **/coach-profile** (`coach-profile.html`) - Manage public coach profile with edit mode

### Administrator Pages (Admin Role Required)
- **/admin-dashboard** (`admin-dashboard.html`) - Platform-wide statistics and management
- **/admin-users** (`admin-users.html`) - User management and administration
- **/admin-questions** (`admin-questions.html`) - Question database management with full CRUD

## 🎯 Key User Flows

### 1. New User Registration Flow
1. Land on homepage → Click "Get Started"
2. Fill registration form with name, email, password
3. Accept terms of service
4. Submit → Auto-login → Redirect to dashboard

### 2. Course Enrollment Flow
1. Browse courses → Click on course card
2. View course details, curriculum, reviews
3. Click "Enroll Now" → Process enrollment
4. Access course from dashboard → Start learning

### 3. Mock Interview Booking Flow
1. Navigate to Mock Interviews page
2. Select interview type (Technical, System Design, etc.)
3. Choose interviewer from available professionals
4. Pick date from calendar
5. Select time slot
6. Review booking summary → Confirm booking
7. Receive confirmation

### 4. Problem Solving Flow

#### Coding Questions
1. Go to Questions page
2. Filter by type (Coding), difficulty, topic, or company
3. Search for specific questions
4. Click "Solve" → Open coding question (LeetCode-style)
5. View problem description, examples, and constraints
6. Write solution in code editor (multi-language support)
7. Run test cases and submit solution

#### Whiteboard Design Questions
1. Navigate to Questions → Filter by "Whiteboard Design"
2. Select a system design problem
3. Read requirements (functional, non-functional, scale estimation)
4. Use whiteboard canvas to draw architecture diagrams
5. Take notes on key decisions and trade-offs
6. Review hints and solution approaches
7. Save progress or submit design

#### Behavioral Questions
1. Browse Questions → Filter by "Behavioral"
2. Choose a behavioral question
3. Read the STAR framework guide
4. Fill in Situation, Task, Action, Result sections
5. Use timer to practice answering aloud
6. Get feedback on answer structure and completeness
7. Save draft or submit answer for review

## 🎨 Technologies Used

### Frontend
- **HTML5** - Semantic markup
- **CSS3** - Modern styling with CSS Grid and Flexbox
- **JavaScript (ES6+)** - Interactive functionality
- **Chart.js** - Data visualization for dashboard
- **Font Awesome 6.4.0** - Icon library
- **Google Fonts (Inter)** - Typography

### Design Patterns
- **Mobile-first responsive design**
- **Component-based CSS architecture**
- **BEM-like naming conventions**
- **CSS custom properties (variables)**
- **Modular JavaScript**

## ⚠️ Important Notes

### Frontend-Only Limitations
This is a **static frontend website** without backend functionality:
- **No real authentication** - Uses localStorage simulation
- **No database** - Data is hardcoded or simulated
- **No video streaming** - Video players are UI placeholders
- **No payment processing** - Payment forms are UI only
- **No API calls** - All data is client-side

### For Production Deployment
To make this a fully functional application, you would need to:
1. **Backend API** - Node.js/Python/Ruby server for business logic
2. **Database** - PostgreSQL/MongoDB for data persistence
3. **Authentication** - JWT tokens, OAuth2, or session management
4. **Video Hosting** - AWS S3, Vimeo, or custom video CDN
5. **Payment Integration** - Stripe, PayPal API integration
6. **Email Service** - SendGrid, AWS SES for notifications
7. **Hosting** - Deploy to Vercel, Netlify, AWS, or similar

## 📋 Features Not Yet Implemented

### Phase 2 Features (Recommended)
- [ ] Live mock interview video interface with real-time collaboration
- [ ] Advanced analytics dashboard with detailed metrics
- [ ] Discussion forums/Q&A boards for community interaction
- [ ] Certificate generation and achievement system
- [ ] Email notifications and automated reminders
- [ ] Calendar integration (Google Calendar, Outlook)
- [ ] Payment gateway integration (Stripe, PayPal)
- [ ] Progress export and detailed reporting
- [ ] Mobile app (React Native/Flutter)
- [ ] Real-time collaborative code editor
- [ ] AI-powered interview feedback and suggestions
- [ ] Video recording and playback for mock interviews

## 🎓 Development Best Practices Used

- ✅ Semantic HTML5 structure
- ✅ Accessible design (ARIA labels, proper heading hierarchy)
- ✅ Responsive design (mobile, tablet, desktop)
- ✅ Cross-browser compatibility
- ✅ Performance optimization (minimal external dependencies)
- ✅ Clean, maintainable code
- ✅ Consistent naming conventions
- ✅ Modular CSS architecture
- ✅ Progressive enhancement
- ✅ User feedback (toasts, loading states)

## 🛠️ Customization Guide

### Changing Colors
Edit CSS variables in `css/style.css`:
```css
:root {
    --primary-color: #6366f1;
    --secondary-color: #8b5cf6;
    --accent-color: #ec4899;
    /* ... */
}
```

### Adding New Pages
1. Create new HTML file
2. Copy header/footer from existing page
3. Link appropriate CSS files
4. Add navigation link
5. Create corresponding JS file if needed

### Modifying Course Content
Edit course data in `courses.html` and `course-detail.html`:
- Course titles, descriptions, pricing
- Instructor information
- Curriculum sections and lessons

## 📞 Support & Contact

For questions or issues:
- Email: support@vector.com (placeholder)
- Documentation: This README file
- Community: Forum link (to be implemented)

## 📄 License

This is a demonstration project. All rights reserved.

## 🙏 Acknowledgments

- Inspired by Exponent and similar interview preparation platforms
- Icons by Font Awesome
- Fonts by Google Fonts
- Design patterns from modern web best practices

---

**Vector** - Master Your Interviews, Land Your Dream Job

*Built with ❤️ for aspiring software engineers and tech professionals*