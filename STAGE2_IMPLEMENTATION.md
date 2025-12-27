# Stage 2: Courses & Learning Content with Peer Mock Interviews - Detailed Implementation Guide

## Overview

Stage 2 focuses on building a comprehensive learning platform with LeetCode-style problem solving, interview question management, and peer-to-peer mock interview capabilities. This builds upon the foundation established in Stage 1.

**Timeline: 8-10 weeks**

**Technology Stack:**
- Backend: .NET 8.0 + ASP.NET Core Web API (C#)
- Frontend: React 18+ with TypeScript + Tailwind CSS
- Database: PostgreSQL + Redis
- Code Execution: Judge0 API (Docker-based) for code execution (Python, JavaScript, Java, C++, C#, Go) - **Judge0 Compatible**
- Video Conferencing: WebRTC or Zoom API for peer interviews
- Storage: AWS S3 for code submissions and recordings
- Email: SendGrid

---

## Prerequisites

Before starting, ensure you have:

- [x] Stage 1 completed (User Management & Authentication) ✅
- [x] .NET 8.0 SDK installed ✅
- [x] PostgreSQL 15+ installed or access to a PostgreSQL database ✅
- [x] Redis 7+ installed or access to a Redis instance ✅
- [x] Docker Desktop installed (for local development and code execution) ✅
- [x] Code execution service setup (Docker-based) ✅ (Already implemented)
- [x] WebRTC or video conferencing API credentials ✅ (Zoom API already configured)

---

## Week 1: Interview Question Bank & Management

**Status: Day 1-2 ✅ COMPLETED | Day 3-4 In Progress**

### Day 1-2: Question Database & Models ✅ COMPLETED

#### Backend Implementation
- [x] Create InterviewQuestion model ✅
  - Id, Title, Description, Difficulty (Easy/Medium/Hard)
  - Category (Arrays, Strings, Trees, Graphs, DP, etc.)
  - Company tags (JSON array)
  - Constraints, Examples
  - Hints (progressive hints array)
  - TimeComplexityHint, SpaceComplexityHint
  - IsActive, CreatedBy (UserId), CreatedAt, UpdatedAt
- [x] Create QuestionTestCase model ✅
  - QuestionId (foreign key)
  - Input (JSON)
  - ExpectedOutput (JSON)
  - IsHidden (bool) - for test cases vs examples
  - TestCaseNumber
- [x] Create QuestionSolution model ✅
  - QuestionId (foreign key)
  - Language (Python, JavaScript, Java, C++, etc.)
  - Code (string)
  - Explanation (string)
  - TimeComplexity, SpaceComplexity
  - IsOfficial (bool) - official solution vs user solutions
  - CreatedBy (UserId)
- [x] Create database migration ✅
  - AddInterviewQuestions table
  - AddQuestionTestCases table
  - AddQuestionSolutions table
  - Indexes for Category, Difficulty, Company tags
- [x] Update ApplicationDbContext ✅
- [x] Create IQuestionService interface ✅
- [x] Create QuestionService implementation ✅
  - GetQuestionByIdAsync
  - GetQuestionsAsync (with filters)
  - CreateQuestionAsync (admin/coach only)
  - UpdateQuestionAsync (admin/coach only)
  - DeleteQuestionAsync (admin only)
  - GetTestCasesAsync
  - GetSolutionsAsync
  - AddTestCaseAsync
  - AddSolutionAsync

#### Frontend Implementation
- [x] Create QuestionsPage component ✅
  - Search functionality
  - Filter by category, difficulty, company
  - Sort by difficulty, popularity
  - Pagination
- [x] Create QuestionDetailPage component ✅
  - Question description
  - Examples and constraints
  - Test cases (visible ones)
  - Solutions tab
  - Code editor with language selection
- [x] Create question service methods ✅
  - getQuestions
  - getQuestionById
  - getTestCases
  - getSolutions
  - createQuestion (admin/coach)
  - updateQuestion (admin/coach)

#### Seed Data
- [x] Added seed data for 10 interview questions ✅
- [x] Added test cases for questions ✅
- [x] Added solutions (JavaScript, Python) for questions ✅
- [x] Integrated seed data into DbSeeder ✅

#### Deployment
- [x] Database migration deployed to local Docker ✅
- [x] Seed data populated successfully ✅
- [x] Backend API endpoints tested ✅
- [x] Frontend pages accessible ✅

#### Testing
- [ ] Unit tests for QuestionService (TODO: Week 7)
- [ ] Unit tests for QuestionController (TODO: Week 7)
- [ ] Integration tests for question endpoints (TODO: Week 7)

---

### Day 3-4: Question Management (Add/Edit Questions) ✅ COMPLETED

#### Backend Implementation
- [x] Create QuestionController endpoints ✅ (Completed in Day 1-2)
  - GET /api/question (list with filters)
  - GET /api/question/{id} (get question details)
  - POST /api/question (create question - admin/coach only)
  - PUT /api/question/{id} (update question - admin/coach only)
  - DELETE /api/question/{id} (delete question - admin only)
  - GET /api/question/{id}/test-cases (get test cases)
  - POST /api/question/{id}/test-cases (add test case - admin/coach)
  - GET /api/question/{id}/solutions (get solutions)
  - POST /api/question/{id}/solutions (add solution - admin/coach)
  - POST /api/questions/{id}/solutions (add solution - admin/coach)
- [x] Create DTOs ✅ (Completed in Day 1-2)
  - InterviewQuestionDto
  - QuestionListDto
  - CreateQuestionDto
  - UpdateQuestionDto
  - QuestionTestCaseDto
  - CreateTestCaseDto ✅ (Added)
  - QuestionSolutionDto
  - CreateSolutionDto
  - QuestionFilterDto
- [x] Implement question validation ✅
  - Required fields
  - Valid difficulty level (Easy, Medium, Hard)
  - Valid question type (Coding, System Design, Behavioral)
  - Valid category validation
  - Test cases validation (input/output required, test case number > 0)
- [x] Implement question approval workflow ✅
  - [x] Pending approval status ✅
  - [x] Admin approval endpoint ✅
  - [x] Coach-created questions require approval ✅
  - [x] Admin-created questions auto-approved ✅

#### Frontend Implementation
- [x] Create AddQuestionPage component ✅
  - Question form (title, description, difficulty, category)
  - Company tags input
  - Constraints input
  - Examples input
  - Test cases management (add/remove)
  - Hints input
  - Submit button
- [x] Create EditQuestionPage component ✅
  - Pre-filled form
  - Update functionality
  - Add new test cases
- [x] Update navigation ✅
  - "Add Question" link (admin/coach only) - Added to navbar
  - Edit button on QuestionDetailPage (admin/coach only)
- [x] Create QuestionForm component (reusable) ✅
  - [x] Form validation ✅
  - [x] Dynamic examples, test cases, and hints management ✅
  - [x] Judge0-compatible test case format ✅
  - [x] Used in AddQuestionPage and EditQuestionPage ✅

#### Testing
- [ ] Unit tests for question creation (TODO)
- [ ] Unit tests for question updates (TODO)
- [ ] Integration tests for question management endpoints (TODO)
- [ ] Frontend tests for question forms (TODO)
- [x] Manual testing completed ✅

---

## Week 2: Code Editor & Execution Environment

### Day 5-6: Code Editor Integration

#### Backend Implementation
- [x] Research and select code editor library ✅ (Monaco Editor selected)
- [x] Create code execution service interface ✅
  - ICodeExecutionService interface created
  - ExecutionRequestDto, ExecutionResultDto, TestResultDto, SupportedLanguageDto created
- [x] Design code execution API ✅
  - POST /api/codeexecution/execute (execute code)
  - POST /api/codeexecution/validate/{questionId} (validate against test cases)
  - GET /api/codeexecution/languages (get supported languages)
  - CodeExecutionController created (implementation pending for Day 7-8)

#### Frontend Implementation
- [x] Install and configure code editor library ✅
  - @monaco-editor/react installed
  - monaco-editor installed
- [x] Create CodeEditor component ✅
  - Syntax highlighting
  - Language selection
  - Line numbers
  - Code formatting
  - Auto-completion
  - Dark theme support
- [x] Integrate code editor with question detail page ✅
  - Replaced textarea with Monaco Editor
  - Integrated with existing language selection
- [x] Add language templates ✅
  - Python template
  - JavaScript template
  - Java template
  - C++ template
  - C# template
  - Go template
- [x] Create code execution service (frontend) ✅
  - executeCode method
  - validateSolution method
  - getSupportedLanguages method

#### Testing
- [ ] Test code editor functionality
- [ ] Test language switching
- [ ] Test code formatting

---

### Day 7-8: Code Execution Service

#### Backend Implementation
- [x] Create CodeExecutionService ✅
  - Judge0 API integration ✅
  - Support for Python, JavaScript, Java, C++, C#, Go ✅
  - Timeout handling (5 seconds CPU, 10 seconds wall time) ✅
  - Memory limit handling (128 MB) ✅
  - Security sandboxing (via Judge0) ✅
- [x] Add Judge0 to docker-compose.yml ✅
  - Judge0 service configured ✅
  - RabbitMQ service for Judge0 queue ✅
  - PostgreSQL database for Judge0 ✅
- [x] Implement code execution controller ✅
  - POST /api/codeexecution/execute ✅
    - Input: code, language, input data ✅
    - Output: result, execution time, memory used ✅
  - POST /api/codeexecution/validate/{questionId} ✅
    - Input: code, language, questionId ✅
    - Output: test results, passed/failed ✅
  - GET /api/codeexecution/languages ✅
- [x] Implement security measures ✅
  - Code execution timeout (5 seconds CPU, 10 seconds wall time) ✅
  - Memory limits (128 MB) ✅
  - File system restrictions (via Judge0 sandbox) ✅
  - Network restrictions (via Judge0 sandbox) ✅
  - Resource cleanup (automatic via Judge0) ✅
- [x] Create ExecutionResultDto ✅
  - Status (Accepted, Wrong Answer, Time Limit Exceeded, etc.) ✅
  - Output ✅
  - Error message ✅
  - Execution time ✅
  - Memory used ✅

#### Frontend Implementation
- [x] Create CodeExecutionService ✅
  - executeCode method ✅
  - validateSolution method ✅
  - getSupportedLanguages method ✅
- [x] Update QuestionDetailPage ✅
  - Run button functionality ✅
  - Submit button functionality ✅
  - Display execution results ✅
  - Display test case results ✅
  - Show execution time and memory ✅
- [x] ExecutionResultPanel component ✅
  - Success/error display (integrated in test result tab) ✅
  - Test case results ✅
  - Execution metrics ✅

#### Testing
- [ ] Unit tests for code execution service
- [ ] Integration tests for code execution
- [ ] Test timeout handling
- [ ] Test memory limit handling
- [ ] Test security restrictions

### Code Auto-Save Feature

#### Auto-Save Implementation
- [x] Implement automatic code saving ✅
  - [x] Save code to localStorage on every change (debounced 1s) ✅
  - [x] Save code per question ID ✅
  - [x] Save code per language ✅
  - [x] Restore code when user returns to question ✅
  - [x] Restore code on page refresh ✅
  - [x] Clear saved code on successful submission ✅
  - [x] Add visual indicator for unsaved changes ✅
  - [x] Handle storage quota exceeded errors gracefully ✅

### Code Editor Improvements & Fixes

#### Code Editor Fixes
- [ ] Fix Monaco Editor configuration and styling
  - [ ] Ensure proper LeetCode-style formatting
  - [ ] Fix indent guides visibility and styling
  - [ ] Fix JSDoc parameter highlighting
  - [ ] Ensure consistent 2-space indentation
  - [ ] Fix line height and spacing issues
  - [ ] Remove unnecessary horizontal/vertical lines
  - [ ] Ensure all scope lines are visible

#### Question Page Tabs Fixes
- [ ] Fix Description, Editorial, and Solutions tabs
  - [ ] Ensure proper tab switching functionality
  - [ ] Fix content rendering in each tab
  - [ ] Fix scrolling and layout issues
  - [ ] Ensure proper styling and spacing

#### Question Page Formatting & CSS Fixes
- [ ] Fix formatting and CSS of question page
  - [ ] Align boxes and containers properly
  - [ ] Fix gaps and spacing between elements
  - [ ] Fix text formatting and code block formatting
  - [ ] Ensure consistent styling across all sections
  - [ ] Fix responsive design issues
  - [ ] Improve overall visual consistency

---

## Week 3: Solution Submission & Tracking

### Day 9-10: Solution Submission System

#### Backend Implementation
- [x] Create UserSolution model ✅
  - UserId (foreign key) ✅
  - QuestionId (foreign key) ✅
  - Language ✅
  - Code ✅
  - Status (Accepted, Wrong Answer, Time Limit Exceeded, etc.) ✅
  - ExecutionTime ✅
  - MemoryUsed ✅
  - SubmittedAt ✅
- [x] Create SolutionSubmission model ✅
  - UserSolutionId (foreign key) ✅
  - TestCaseId (foreign key) ✅
  - Status (Passed, Failed) ✅
  - Output ✅
  - ExpectedOutput ✅
  - ErrorMessage ✅
- [x] Create ISolutionService interface ✅
- [x] Create SolutionService implementation ✅
  - SubmitSolutionAsync ✅
  - GetUserSolutionsAsync ✅
  - GetSolutionByIdAsync ✅
  - GetSolutionStatisticsAsync ✅
- [x] Create SolutionController endpoints ✅
  - POST /api/solutions (submit solution) ✅
  - GET /api/solutions/me (get user's solutions) ✅
  - GET /api/solutions/{id} (get solution details) ✅
  - GET /api/solutions/question/{questionId} (get solutions for question) ✅
  - GET /api/solutions/statistics (get user statistics) ✅
- [x] Create database migration ✅
  - AddUserSolutions table ✅
  - AddSolutionSubmissions table ✅
  - Indexes for UserId, QuestionId, Status ✅

#### Frontend Implementation
- [x] Create SolutionSubmissionService ✅
  - submitSolution method ✅
  - getUserSolutions method ✅
  - getSolutionById method ✅
  - getSolutionsForQuestion method ✅
  - getStatistics method ✅
- [x] Update QuestionDetailPage ✅
  - Submit solution functionality ✅
  - Show submission status ✅
  - Display submission results ✅
  - Save solution to database on submit ✅
- [x] Create SolutionHistoryPage component ✅
  - List of user's solutions ✅
  - Filter by question, language, status ✅
  - View solution details ✅
  - Pagination support ✅
- [ ] Create SolutionDetailPage component (Optional - can view from question detail page)
  - Solution code
  - Test case results
  - Execution metrics
  - Comparison with other solutions

#### Testing
- [ ] Unit tests for SolutionService
- [ ] Unit tests for SolutionController
- [ ] Integration tests for solution submission
- [ ] Test solution validation

---

### Day 11-12: Progress Tracking & Analytics

#### Backend Implementation
- [x] Create LearningAnalytics model ✅
  - UserId (foreign key) ✅
  - QuestionsSolved (int) ✅
  - QuestionsByCategory (JSON) ✅
  - QuestionsByDifficulty (JSON) ✅
  - AverageExecutionTime ✅
  - SuccessRate ✅
  - CurrentStreak ✅
  - LongestStreak ✅
  - LastActivityDate ✅
  - TotalSubmissions ✅
  - SolutionsByLanguage (JSON) ✅
- [x] Create IAnalyticsService interface ✅
- [x] Create AnalyticsService implementation ✅
  - UpdateAnalyticsAsync (on solution submission) ✅
  - GetUserAnalyticsAsync ✅
  - GetCategoryProgressAsync ✅
  - GetDifficultyProgressAsync ✅
  - CalculateStreakAsync ✅
- [x] Create AnalyticsController endpoints ✅
  - GET /api/analytics/me (get user analytics) ✅
  - GET /api/analytics/category/{category} (get category progress) ✅
  - GET /api/analytics/difficulty/{difficulty} (get difficulty progress) ✅
- [x] Create database migration ✅
  - AddLearningAnalytics table ✅
  - Indexes for UserId ✅
- [x] Integrate analytics updates in SolutionService ✅
  - Analytics updated automatically on solution submission ✅

#### Frontend Implementation
- [x] Create AnalyticsDashboard component ✅
  - Total problems solved ✅
  - Problems by category (chart) ✅
  - Problems by difficulty (chart) ✅
  - Success rate ✅
  - Current streak ✅
  - Weak areas identification ✅
- [x] Create ProgressChart component ✅
  - Progress over time ✅
  - Category breakdown ✅
  - Difficulty breakdown ✅
  - Bar, pie, and line chart types ✅
- [x] Update DashboardPage ✅
  - Add analytics section ✅
  - Show learning progress ✅
  - Display problems solved and streak ✅
  - Show difficulty progress with percentages ✅
- [x] Create ProgressPage component ✅
  - Detailed analytics ✅
  - Progress charts ✅
  - Category and difficulty progress cards ✅
  - Performance metrics ✅

#### Testing
- [ ] Unit tests for AnalyticsService
- [ ] Integration tests for analytics endpoints
- [ ] Test analytics calculations

---

## Week 4: Peer Mock Interview System

### Day 13-14: Peer Matching & Session Management

#### Backend Implementation
- [x] Create PeerInterviewSession model ✅
  - Id
  - InterviewerId (foreign key to User)
  - IntervieweeId (foreign key to User)
  - QuestionId (foreign key, selected question - auto-assigned based on interview level)
  - Status (Scheduled, InProgress, Completed, Cancelled)
  - ScheduledTime
  - Duration (minutes, default 45)
  - SessionRecordingUrl (optional)
  - InterviewType (e.g., "data-structures-algorithms", "system-design")
  - PracticeType (e.g., "peers", "friend", "expert")
  - InterviewLevel (e.g., "beginner", "intermediate", "advanced")
  - CreatedAt, UpdatedAt
- [x] Create PeerInterviewMatch model ✅
  - UserId (foreign key)
  - PreferredDifficulty
  - PreferredCategories
  - Availability (JSON - time slots)
  - IsAvailable (bool)
  - LastMatchDate
- [x] Create IPeerInterviewService interface ✅
- [x] Create PeerInterviewService implementation ✅
  - FindMatchAsync (find available peer)
  - CreateSessionAsync (create interview session with automatic question assignment)
  - GetSessionByIdAsync
  - GetUserSessionsAsync
  - UpdateSessionStatusAsync
  - CancelSessionAsync
  - UpdateMatchPreferencesAsync
  - GetMatchPreferencesAsync
  - MapInterviewLevelToDifficulty (private helper)
  - AssignQuestionByLevelAsync (private helper - assigns question based on interview level)
- [x] Create PeerInterviewController endpoints ✅
  - POST /api/peer-interviews/find-match (find peer match)
  - POST /api/peer-interviews/sessions (create session)
  - GET /api/peer-interviews/sessions/me (get user's sessions)
  - GET /api/peer-interviews/sessions/{id} (get session details)
  - PUT /api/peer-interviews/sessions/{id}/status (update status)
  - PUT /api/peer-interviews/sessions/{id}/cancel (cancel session)
  - PUT /api/peer-interviews/match-preferences (update match preferences)
  - GET /api/peer-interviews/match-preferences (get match preferences)
- [x] Create database migration ✅
  - AddPeerInterviewSessions table
  - AddPeerInterviewMatches table
  - Indexes for InterviewerId, IntervieweeId, Status
  - Email confirmation on session creation (SendGrid integration)

#### Frontend Implementation
- [x] Create FindPeerPage component ✅
  - Set preferences (difficulty, categories)
  - Set availability
  - Find match button
  - Match results display
- [x] Create PeerInterviewSessionPage component ✅
  - Session details
  - Question display
  - Code editor (shared)
  - Timer
  - Role indicator (interviewer/interviewee)
- [x] Create PeerInterviewService ✅
  - findMatch method
  - createSession method
  - getSessions method
- [x] Update navigation ✅
  - "Find Peer Interview" link
  - "My Interviews" link

#### Testing
- [x] Unit tests for PeerInterviewService ✅
  - 48 comprehensive tests covering all service methods
  - Edge cases: null parameters, invalid IDs, unauthorized access, empty results
  - Business logic: question assignment by level, matching algorithm, cancellation rules
  - Data validation: user authorization, session ownership, status transitions
  - Error handling: exceptions, null returns, forbidden access
  - Timestamp verification: UpdatedAt changes, CreatedAt ordering
- [x] Unit tests for PeerInterviewController ✅
  - 25 comprehensive tests covering all controller endpoints
  - Authorization tests: user as interviewer/interviewee, unauthorized access
  - Request validation: valid data, invalid IDs, missing parameters
  - Error handling: service exceptions, NotFound, Forbid responses
  - Status code verification: Created, Ok, BadRequest, NotFound, Forbid, InternalServerError
- [x] Unit tests for matching algorithm ✅
  - Available peer matching with preferences
  - No available peers handling
  - User availability checks
  - Recently matched peer exclusion
  - Difficulty and category preference matching
- [ ] Integration tests for peer interview endpoints (TODO: Week 7)

---

### Day 15-16: Real-time Collaboration & Video ✅ COMPLETED

#### Backend Implementation ✅
- [x] Research WebRTC or video API integration ✅
- [x] Create video session management ✅
  - Generate session tokens ✅
  - Session signaling ✅
  - Session cleanup ✅
- [x] Implement screen sharing support ✅
- [x] Create VideoSessionController ✅
  - POST /api/video-sessions/create (create video session) ✅
  - GET /api/video-sessions/{id}/token (get session token) ✅
  - POST /api/video-sessions/{id}/end (end session) ✅
  - POST /api/video-sessions/{id}/offer (handle WebRTC offer) ✅
  - POST /api/video-sessions/{id}/answer (handle WebRTC answer) ✅
  - POST /api/video-sessions/{id}/ice-candidate (handle ICE candidates) ✅
  - GET /api/video-sessions/{id}/signaling (get signaling data) ✅
- [x] Install and configure SignalR ✅
  - Microsoft.AspNetCore.SignalR package installed ✅
  - CollaborationHub created ✅
  - JWT authentication configured for SignalR ✅
  - Hub endpoint mapped: /api/collaboration/{sessionId} ✅

#### Frontend Implementation ✅
- [x] Integrate WebRTC or video API ✅
- [x] Create VideoChat component ✅
  - Video display ✅
  - Audio controls ✅
  - Screen sharing button ✅
  - WebRTC signaling integration ✅
- [x] Create CollaborativeCodeEditor component ✅
  - Real-time code synchronization using SignalR ✅
  - Cursor positions ✅
  - User presence indicators ✅
  - Automatic reconnection ✅
- [x] Update QuestionDetailPage ✅
  - Add video chat panel (DraggableVideo component) ✅
  - Add collaborative code editor (replaces regular CodeEditor when session is InProgress) ✅
  - Add screen sharing (via VideoChat component) ✅
  - Layout: Question panel (left), Video chat + Collaborative editor (right) ✅
  - Collaboration status indicators (live/offline) ✅
  - Error handling for video and collaboration failures ✅
  - Role-based tab visibility (interviewee cannot see Hints/Solution) ✅

#### Testing
- [x] Test video connection ✅ (See TESTING_PEER_INTERVIEW.md)
- [x] Test code synchronization ✅ (See TESTING_PEER_INTERVIEW.md)
- [x] Test screen sharing ✅ (See TESTING_PEER_INTERVIEW.md)

---

## Week 5: Interview Session Features

### Day 17-18: Interview Timer & Question Selection

#### Backend Implementation
- [ ] Create InterviewTimer model
  - SessionId (foreign key)
  - StartTime
  - Duration (minutes)
  - RemainingTime
  - IsPaused
- [ ] Implement timer service
  - Start timer
  - Pause timer
  - Resume timer
  - Get remaining time
- [ ] Create question selection logic
  - Random question selection based on difficulty
  - Question filtering by category
  - Avoid recently used questions
- [ ] Update PeerInterviewController
  - POST /api/peer-interviews/sessions/{id}/start (start interview)
  - POST /api/peer-interviews/sessions/{id}/pause (pause timer)
  - POST /api/peer-interviews/sessions/{id}/resume (resume timer)
  - GET /api/peer-interviews/sessions/{id}/timer (get timer status)
  - POST /api/peer-interviews/sessions/{id}/select-question (select question)

#### Frontend Implementation
- [ ] Create InterviewTimer component
  - Countdown display
  - Pause/resume buttons
  - Time warnings (5 min, 1 min remaining)
- [ ] Create QuestionSelector component
  - Question selection interface
  - Difficulty filter
  - Category filter
  - Random selection button
- [ ] Update PeerInterviewSessionPage
  - Add timer display
  - Add question selector
  - Add start interview button

#### Testing
- [ ] Test timer functionality
- [ ] Test question selection
- [ ] Test timer warnings

---

### Day 19-20: Session Recording & Feedback

#### Backend Implementation
- [ ] Create InterviewFeedback model
  - SessionId (foreign key)
  - FeedbackFrom (UserId)
  - FeedbackTo (UserId)
  - Rating (1-5)
  - Strengths (string)
  - AreasForImprovement (string)
  - OverallComments (string)
  - SubmittedAt
- [ ] Create IFeedbackService interface
- [ ] Create FeedbackService implementation
  - SubmitFeedbackAsync
  - GetFeedbackAsync
  - GetFeedbackBySessionAsync
- [ ] Create FeedbackController endpoints
  - POST /api/peer-interviews/sessions/{id}/feedback (submit feedback)
  - GET /api/peer-interviews/sessions/{id}/feedback (get feedback)
- [ ] Implement session recording (optional)
  - Store recording URL
  - Access control for recordings
- [ ] Create database migration
  - AddInterviewFeedback table
  - Indexes for SessionId, FeedbackFrom, FeedbackTo

#### Frontend Implementation
- [ ] Create FeedbackForm component
  - Rating selector
  - Strengths textarea
  - Areas for improvement textarea
  - Overall comments textarea
  - Submit button
- [ ] Create FeedbackView component
  - Display feedback
  - Show ratings
  - Show comments
- [ ] Update PeerInterviewSessionPage
  - Add feedback section (after session ends)
  - Show feedback if submitted
- [ ] Create SessionRecording component (if implemented)
  - Play recording
  - Download recording

#### Testing
- [ ] Unit tests for FeedbackService
- [ ] Integration tests for feedback endpoints
- [ ] Test feedback validation

---

## Week 6: Advanced Features & Polish

### Day 21-22: Bookmarks & Favorites

#### Backend Implementation
- [ ] Create QuestionBookmark model
  - UserId (foreign key)
  - QuestionId (foreign key)
  - CreatedAt
- [ ] Update QuestionService
  - AddBookmarkAsync
  - RemoveBookmarkAsync
  - GetBookmarksAsync
- [ ] Update QuestionController
  - POST /api/questions/{id}/bookmark (add bookmark)
  - DELETE /api/questions/{id}/bookmark (remove bookmark)
  - GET /api/questions/bookmarks (get bookmarked questions)
- [ ] Create database migration
  - AddQuestionBookmarks table
  - Unique index on UserId + QuestionId

#### Frontend Implementation
- [ ] Create BookmarkButton component
  - Bookmark/unbookmark toggle
  - Bookmark icon
- [ ] Create BookmarkedQuestionsPage component
  - List of bookmarked questions
  - Remove bookmark functionality
- [ ] Update QuestionCard component
  - Add bookmark button
- [ ] Update QuestionDetailPage
  - Add bookmark button

#### Testing
- [ ] Unit tests for bookmark functionality
- [ ] Integration tests for bookmark endpoints

---

### Day 23-24: Daily Challenges & Recommendations

#### Backend Implementation
- [ ] Create DailyChallenge model
  - Date
  - QuestionId (foreign key)
  - Difficulty
  - Category
- [ ] Create ChallengeService
  - GetDailyChallengeAsync
  - GetChallengeHistoryAsync
- [ ] Create recommendation algorithm
  - Based on user's weak areas
  - Based on user's progress
  - Based on difficulty progression
- [ ] Create RecommendationService
  - GetRecommendedQuestionsAsync
  - GetPersonalizedSetAsync
- [ ] Create ChallengeController endpoints
  - GET /api/challenges/daily (get today's challenge)
  - GET /api/challenges/history (get challenge history)
  - GET /api/recommendations (get recommended questions)

#### Frontend Implementation
- [ ] Create DailyChallengePage component
  - Today's challenge display
  - Challenge history
  - Solve challenge button
- [ ] Create RecommendationsPanel component
  - Recommended questions list
  - Personalized problem set
- [ ] Update DashboardPage
  - Add daily challenge widget
  - Add recommendations section

#### Testing
- [ ] Unit tests for challenge service
- [ ] Unit tests for recommendation algorithm
- [ ] Integration tests for challenge endpoints

---

## Week 7-8: Testing, Polish & Deployment

### Day 25-28: Testing & Bug Fixes

#### Testing
- [ ] Unit tests for all services
- [ ] Integration tests for all endpoints
- [ ] E2E tests for critical flows
  - Solve problem flow
  - Submit solution flow
  - Peer interview flow
  - Add question flow
- [ ] Performance testing
  - Code execution performance
  - Database query optimization
  - Real-time collaboration performance
- [ ] Security testing
  - Code execution sandbox security
  - Input validation
  - Authorization checks

#### Bug Fixes
- [ ] Fix any identified bugs
- [ ] Optimize code execution
- [ ] Improve error handling
- [ ] Enhance user experience
- [ ] Fix code editor issues (see Code Editor Improvements & Fixes section under Week 2)
- [ ] Fix question page tabs (see Code Editor Improvements & Fixes section under Week 2)
- [ ] Fix question page formatting and CSS (see Code Editor Improvements & Fixes section under Week 2)

---

### Day 29-30: Documentation & Deployment

#### Documentation
- [ ] API documentation (Swagger/OpenAPI)
  - Document all question endpoints
  - Document code execution endpoints
  - Document peer interview endpoints
- [ ] Code comments
  - XML documentation for controllers
  - Service method comments
- [ ] README updates
  - Update backend/README.md
  - Update frontend/README.md
- [ ] User guide
  - How to solve problems
  - How to add questions
  - How to conduct peer interviews
- [ ] Developer guide
  - Code execution setup
  - Adding new languages
  - Question format guide

#### Deployment
- [ ] Deploy to staging environment
- [ ] Test all features in staging
- [ ] Fix any staging issues
- [ ] Deploy to production (if ready)
- [ ] Monitor production deployment

---

## Success Criteria Checklist

### Question Bank
- [ ] Users can browse and search questions
- [ ] Questions can be filtered by category, difficulty, company
- [ ] Admins/coaches can add new questions
- [ ] Questions have test cases and solutions
- [ ] Question management system works

### Code Editor & Execution
- [ ] Code editor works with multiple languages
- [ ] Code can be executed and validated
- [ ] Test cases run correctly
- [ ] Execution results are accurate
- [ ] Security measures are in place

### Solution Submission
- [ ] Users can submit solutions
- [ ] Solutions are validated against test cases
- [ ] Solution history is tracked
- [ ] Solution statistics are accurate

### Peer Mock Interviews
- [ ] Users can find peer interview partners
- [ ] Interview sessions can be created
- [ ] Real-time collaboration works
- [ ] Video chat works (if implemented)
- [ ] Feedback can be submitted
- [ ] Session recordings work (if implemented)

### Progress Tracking
- [ ] User progress is tracked accurately
- [ ] Analytics provide meaningful insights
- [ ] Progress charts display correctly
- [ ] Recommendations are relevant

### Question Management
- [ ] Admins/coaches can add questions
- [ ] Questions can be edited and deleted
- [ ] Question approval workflow works (if implemented)
- [ ] Bulk import works (if implemented)

---

## Progress Summary

**Status:** ⏳ Planning Complete - Ready to Start Development

### Key Features to Implement:
1. ✅ Interview Question Bank & Management
2. ✅ Code Editor & Execution Environment
3. ✅ Solution Submission & Tracking
4. ✅ Peer Mock Interview System
5. ✅ Progress Tracking & Analytics
6. ✅ Advanced Features (Bookmarks, Challenges, Recommendations)

---

## Implementation Notes

### Backend Architecture
- Controllers handle HTTP requests/responses
- Services contain business logic
- Code execution in isolated Docker containers
- Real-time collaboration via WebRTC or similar
- DTOs for data transfer
- Models represent database entities

### Frontend Architecture
- Pages for route components
- Components for reusable UI
- Code editor integration
- Real-time collaboration components
- Services for API calls
- Context for global state

### Key Implementation Files

**Backend:**
- `QuestionController.cs` - Question management endpoints
- `QuestionService.cs` - Question business logic
- `CodeExecutionService.cs` - Code execution logic
- `PeerInterviewService.cs` - Peer interview management
- `AnalyticsService.cs` - Progress tracking
- `SolutionService.cs` - Solution submission

**Frontend:**
- `QuestionListPage.tsx` - Browse questions
- `ProblemSolvingPage.tsx` - Solve problems
- `CodeEditor.tsx` - Code editor component
- `PeerInterviewSessionPage.tsx` - Peer interview session
- `AnalyticsDashboard.tsx` - Progress analytics
- `AddQuestionPage.tsx` - Add new questions

---

## Current Implementation Status

### ✅ Completed Features (Week 1-2)

#### Question Bank & Management
- [x] Database models (InterviewQuestion, QuestionTestCase, QuestionSolution) ✅
- [x] Question CRUD operations ✅
- [x] Question list page with filtering and search ✅
- [x] Question detail page with description, examples, constraints ✅
- [x] Test case management (visible/hidden test cases) ✅
- [x] Solution viewing and submission ✅
- [x] Database seeding with 10 sample questions ✅

#### Code Editor & Execution
- [x] Monaco Editor integration with LeetCode-style configuration ✅
- [x] Multi-language support (JavaScript, Python, Java, C++, C#, Go) ✅
- [x] Code execution via Judge0 API ✅
- [x] Line-based test case input format ✅
- [x] Test case parsing and validation ✅
- [x] Code wrapping service for test case execution ✅
- [x] Test result display with per-case subtabs ✅
- [x] Runtime error handling and display ✅
- [x] Output vs Expected comparison ✅
- [x] Stdout separation from output ✅
- [x] Question-specific code templates ✅

#### UI/UX Improvements
- [x] Resizable panels for question description and code editor ✅
- [x] Clean test result header design ✅
- [x] Borderless case tabs with visual status indicators ✅
- [x] Toast notification system for success/error messages ✅
- [x] Removed console logs and debug output ✅
- [x] Compact JSON output formatting ✅

#### Testing
- [x] Unit tests for TestCaseParserService (11 tests) ✅
- [x] Unit tests for CodeWrapperService (15 tests) ✅
- [x] Unit tests for CodeExecutionController (9 tests) ✅
- [x] Unit tests for PeerInterviewService (48 tests) ✅
- [x] Unit tests for PeerInterviewController (25 tests) ✅
- [x] Total: 117 unit tests covering Stage 2 implementation ✅

---

## Next Steps

### Week 3: Solution Submission & Progress Tracking (In Progress)

#### Immediate Tasks
1. **Solution History & Management**
   - [ ] Display user's solution history for each question
   - [ ] Allow users to view and compare previous solutions
   - [ ] Implement solution versioning
   - [ ] Add solution sharing capabilities

2. **Progress Tracking**
   - [ ] Track solved questions per user
   - [ ] Calculate completion percentage by difficulty
   - [ ] Track submission statistics (attempts, success rate)
   - [ ] Implement streak tracking (daily problem solving)

3. **User Dashboard**
   - [ ] Create user progress dashboard
   - [ ] Display solved/attempted questions
   - [ ] Show statistics (total solved, accuracy, favorite topics)
   - [ ] Display recent activity

4. **Code Auto-Save**
   - [ ] Implement automatic code saving to localStorage
   - [ ] Restore code on page refresh
   - [ ] Add manual save option
   - [ ] Sync across browser tabs

### Week 4: Peer Mock Interviews

1. **Peer Matching System**
   - [ ] Create matching algorithm (skill level, availability)
   - [ ] Implement interview scheduling
   - [ ] Add interview preferences (difficulty, topics)
   - [ ] Create interview queue system

2. **Real-time Collaboration**
   - [ ] Integrate WebRTC or Zoom API
   - [ ] Implement shared code editor
   - [ ] Add real-time cursor tracking
   - [ ] Create interview session management

3. **Interview Features**
   - [ ] Interview timer and countdown
   - [ ] Question selection for interviewer
   - [ ] Code execution during interview
   - [ ] Interview feedback system

### Week 5-6: Advanced Features

1. **Bookmarks & Favorites**
   - [ ] Allow users to bookmark questions
   - [ ] Create favorite questions list
   - [ ] Add notes to questions
   - [ ] Implement question collections

2. **Challenges & Competitions**
   - [ ] Create weekly/monthly challenges
   - [ ] Implement leaderboards
   - [ ] Add achievement system
   - [ ] Create coding contests

3. **Recommendations**
   - [ ] AI-powered question recommendations
   - [ ] Suggest questions based on solved problems
   - [ ] Difficulty progression suggestions
   - [ ] Topic-based recommendations

4. **Editorial & Solutions**
   - [ ] Display official solutions with explanations
   - [ ] Show user-submitted solutions
   - [ ] Add solution voting/rating
   - [ ] Implement solution discussions

### Week 7-8: Testing, Polish & Deployment

1. **Testing**
   - [ ] Integration tests for code execution
   - [ ] End-to-end tests for user flows
   - [ ] Performance testing
   - [ ] Load testing for concurrent users

2. **Performance Optimization**
   - [ ] Code splitting and lazy loading
   - [ ] Optimize database queries
   - [ ] Implement caching strategies
   - [ ] Reduce bundle size

3. **Documentation**
   - [ ] API documentation (Swagger)
   - [ ] User guide and tutorials
   - [ ] Developer documentation
   - [ ] Deployment guides

4. **Deployment**
   - [ ] Set up CI/CD pipeline
   - [ ] Configure production environment
   - [ ] Set up monitoring and logging
   - [ ] Deploy to production

---

## Implementation Notes

### Recent Changes (Latest Update)

1. **Toast Notification System**
   - Added `Toast.tsx` component for user feedback
   - Integrated success/error messages for code submission
   - Toast notifications appear in top-right corner
   - Auto-dismiss after 5 seconds

2. **Test Result UI Improvements**
   - Removed borders from case tabs
   - Updated visual styling to match LeetCode design
   - Improved status badge appearance
   - Better hover states and transitions

3. **Error Handling**
   - Improved error messages for submission failures
   - Better validation error display
   - User-friendly error messages

### Key Implementation Files

**Backend:**
- `CodeExecutionController.cs` - Code execution endpoints
- `CodeExecutionService.cs` - Judge0 integration and code execution
- `CodeWrapperService.cs` - Code wrapping for test case execution
- `TestCaseParserService.cs` - Line-based test case parsing
- `QuestionService.cs` - Question management
- `SolutionService.cs` - Solution submission

**Frontend:**
- `QuestionDetailPage.tsx` - Main problem solving interface
- `CodeEditor.tsx` - Monaco Editor wrapper component
- `Toast.tsx` - Toast notification component
- `questionTemplates.ts` - Question-specific code templates
- `editorStyle.ts` - Monaco Editor styling configuration

---

## Optimized Solved Questions Tracking

**Implementation Date:** December 2024

### Overview
A dedicated `UserSolvedQuestions` table was created to optimize the lookup of whether a user has solved a specific question. This replaces the previous approach of querying all `UserSolutions` records.

### Database Schema
- **Table:** `UserSolvedQuestions`
- **Columns:**
  - `Id` (Guid, Primary Key)
  - `UserId` (Guid, Foreign Key to Users)
  - `QuestionId` (Guid, Foreign Key to InterviewQuestions)
  - `SolvedAt` (DateTime)
  - `Language` (string, nullable, max 50 chars)
- **Indexes:**
  - Unique composite index on `(UserId, QuestionId)` - ensures one record per user-question pair
  - Index on `UserId` - for querying user's solved questions
  - Index on `QuestionId` - for question statistics
  - Index on `SolvedAt` - for sorting by solve date

### Backend Implementation
- **Model:** `UserSolvedQuestion.cs`
- **Service Method:** `HasUserSolvedQuestionAsync(Guid userId, Guid questionId)` - optimized boolean lookup
- **Controller Endpoint:** `GET /api/solutions/question/{questionId}/solved` - returns `{ solved: boolean }`
- **Auto-Update:** When a solution is submitted successfully, a record is automatically created/updated in `UserSolvedQuestions`

### Frontend Implementation
- **Service Method:** `solutionService.hasSolvedQuestion(questionId: string): Promise<boolean>`
- **Usage:** Non-blocking async call in `QuestionDetailPage.tsx` to check solved status
- **Performance:** Much faster than querying all solutions - single indexed lookup

### Benefits
1. **Performance:** O(1) lookup instead of O(n) query through all solutions
2. **Scalability:** Indexed table scales better as users solve more questions
3. **Simplicity:** Single boolean check instead of filtering solution arrays
4. **Non-blocking:** Frontend uses `.then()/.catch()` to avoid blocking page load

### Migration
- **Migration Name:** `AddUserSolvedQuestionsTable`
- **Auto-applied:** Runs automatically on container startup via `DbInitializer.cs`

---

**Last Updated:** January 2025  
**Status:** Week 4 Day 13-14 Complete (Peer Matching & Session Management) - Testing Complete ✅

### Week 4 Day 13-14: Testing Summary

**Test Coverage:**
- **PeerInterviewServiceTests.cs:** 48 comprehensive unit tests
  - All service methods covered with edge cases
  - Business logic validation (question assignment, matching algorithm)
  - Error handling and data validation
  - Timestamp verification
  
- **PeerInterviewControllerTests.cs:** 25 comprehensive unit tests
  - All controller endpoints covered
  - Authorization and access control
  - Request validation and error handling
  - Status code verification

**Total Tests:** 73 tests (all passing ✅)

**Key Test Scenarios:**
1. **Session Creation:** Valid data, automatic question assignment by level, null/optional parameters, default values
2. **Question Assignment:** Beginner→Easy, Intermediate→Medium, Advanced→Hard mapping
3. **Matching Algorithm:** Available peers, preference matching, recently matched exclusion
4. **Session Management:** Status updates, cancellation rules, authorization checks
5. **Edge Cases:** Invalid IDs, unauthorized access, empty results, service exceptions
