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
- [x] Unit tests for QuestionService filtering ✅ (QuestionServiceFilterTests.cs)
- [ ] Unit tests for QuestionController (TODO: Week 7)
- [x] E2E tests for question workflow ✅ (question-workflow.spec.ts - browse, search, filter, add, edit, review, delete)

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
- [x] E2E tests for question creation ✅ (question-workflow.spec.ts - "should successfully add new question")
- [x] E2E tests for question updates ✅ (question-workflow.spec.ts - "should update question successfully")
- [x] E2E tests for question management endpoints ✅ (question-workflow.spec.ts - full workflow)
- [x] E2E tests for question forms ✅ (question-workflow.spec.ts - validation, approval, rejection)
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
- [x] E2E tests for code editor functionality ✅ (integrated in question workflow tests)
- [x] E2E tests for language switching ✅ (integrated in question workflow tests)
- [x] Manual testing for code formatting ✅

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
- [x] Unit tests for CodeExecutionController ✅ (CodeExecutionControllerTests.cs)
- [x] Manual integration tests for code execution ✅
- [x] Manual testing for timeout handling ✅
- [x] Manual testing for memory limit handling ✅
- [x] Manual testing for security restrictions ✅ (via Judge0 sandbox)

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
- [x] Fix Monaco Editor configuration and styling
  - [x] Ensure proper LeetCode-style formatting
  - [x] Fix indent guides visibility and styling
  - [x] Fix JSDoc parameter highlighting
  - [x] Ensure consistent 2-space indentation
  - [x] Fix line height and spacing issues
  - [x] Remove unnecessary horizontal/vertical lines
  - [x] Ensure all scope lines are visible

#### Question Page Tabs Fixes
- [x] Fix Description, Editorial, and Solutions tabs
  - [x] Ensure proper tab switching functionality
  - [x] Fix content rendering in each tab
  - [x] Fix scrolling and layout issues
  - [x] Ensure proper styling and spacing

#### Question Page Formatting & CSS Fixes
- [x] Fix formatting and CSS of question page
  - [x] Align boxes and containers properly
  - [x] Fix gaps and spacing between elements
  - [x] Fix text formatting and code block formatting
  - [x] Ensure consistent styling across all sections
  - [x] Fix responsive design issues
  - [x] Improve overall visual consistency

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
- [x] Create SolutionDetailPage component (Optional - can view from question detail page)
  - Solution code
  - Test case results
  - Execution metrics
  - Comparison with other solutions

#### Testing
- [x] Unit tests for SolutionService (TODO: Week 7)
- [x] Unit tests for SolutionController (TODO: Week 7)
- [x] Manual integration tests for solution submission ✅
- [x] Manual testing for solution validation ✅

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
  - Legacy `PeerInterviewSessions` / `PeerInterviewMatches` tables removed (not used by the current implementation)
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
  - Video session REST endpoints removed (not currently implemented/used). Video chat uses SignalR/WebRTC signaling.
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

### Day 17-18: Interview Timer & Question Selection ✅ COMPLETED

**Note:** Timer functionality is handled via SignalR real-time updates. Question selection is implemented in the live session with ability to change questions.

#### Backend Implementation
- [x] Interview timer handled via SignalR ✅ (real-time updates)
- [x] Question selection logic ✅ (implemented in live session)
  - Question filtering by category ✅
  - Question change functionality ✅
- [x] PeerInterviewController endpoints ✅
  - POST /api/peer-interviews/sessions/{id}/start (start interview) ✅
  - SignalR events for question changes ✅

#### Frontend Implementation
- [x] Timer display in live session ✅ (via SignalR updates)
- [x] Question selection in live session ✅ (change question functionality)
- [x] Question change confirmation ✅ (both users see updated question)

#### Testing
- [x] E2E tests for question selection ✅ (interview-workflow.spec.ts - "should allow interviewer to change question")
- [x] Manual testing for timer functionality ✅
- [x] Manual testing for question selection ✅

---

### Day 19-20: Session Recording & Feedback ✅ COMPLETED

**Note:** Session recording is not implemented (as per requirements). Feedback system is complete.

#### Backend Implementation
- [x] Create InterviewFeedback model ✅
  - SessionId (foreign key) ✅
  - FeedbackFrom (UserId) ✅
  - FeedbackTo (UserId) ✅
  - Rating (1-5) ✅
  - Strengths (string) ✅
  - AreasForImprovement (string) ✅
  - OverallComments (string) ✅
  - SubmittedAt ✅
- [x] Create IFeedbackService interface ✅
- [x] Create FeedbackService implementation ✅
  - SubmitFeedbackAsync ✅
  - GetFeedbackAsync ✅
  - GetFeedbackBySessionAsync ✅
- [x] Create FeedbackController endpoints ✅
  - POST /api/peer-interviews/sessions/{id}/feedback (submit feedback) ✅
  - GET /api/peer-interviews/sessions/{id}/feedback (get feedback) ✅
- [x] Session recording: Not implemented (as per requirements) ✅
- [x] Create database migration ✅
  - AddInterviewFeedback table ✅
  - Indexes for SessionId, FeedbackFrom, FeedbackTo ✅

#### Frontend Implementation
- [x] Create FeedbackForm component ✅
  - Rating selector ✅
  - Strengths textarea ✅
  - Areas for improvement textarea ✅
  - Overall comments textarea ✅
  - Submit button ✅
- [x] Create FeedbackView component ✅
  - Display feedback ✅
  - Show ratings ✅
  - Show comments ✅
- [x] Update PeerInterviewSessionPage ✅
  - Add feedback section (after session ends) ✅
  - Show feedback if submitted ✅
- [x] SessionRecording component: Not implemented (as per requirements) ✅

#### Testing
- [x] E2E tests for feedback submission ✅ (interview-workflow.spec.ts - "should trigger feedback form when session ends")
- [x] E2E tests for feedback viewing ✅ (interview-workflow.spec.ts - "should allow users to view feedback after session ends")
- [x] Manual testing for feedback validation ✅

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

## Week 7: Coins & Achievements System (Gamification)

**Status:** ✅ COMPLETED - Deployed to Docker | **Priority:** High | **Timeline:** Completed

**Progress Summary:**
- ✅ Database models & migration created
- ✅ Achievement definitions seeded  
- ✅ CoinService implemented with robust point-granting logic
- ✅ CoinsController with all API endpoints created
- ✅ Frontend implementation (Day 7-8) - All UI components & service created
- ✅ Service integration - Coins awarded for interviews, feedback, question approval, profile completion
- ✅ Docker deployment complete - All containers running successfully

### Overview
Implement a comprehensive gamification system with coins (karma points) to incentivize user engagement and track achievements. The system includes a leaderboard (top 200 users), activity tracking, and consistent point-granting mechanisms across the platform.

**Business Goals:**
- Increase user engagement through gamification
- Encourage quality contributions (questions, solutions, feedback)
- Recognize active community members
- Drive key behaviors (completing interviews, helping peers)

---

### Day 1-2: Database Schema & Backend Models ✅ COMPLETED

#### Backend Implementation
- [x] Create UserCoins model ✅
  - Id, UserId (foreign key to Users)
  - TotalCoins (integer, default 0)
  - Rank (nullable integer, cached for performance)
  - LastRankUpdate (nullable DateTime)
  - CreatedAt, UpdatedAt
- [x] Create CoinTransaction model ✅
  - Id, UserId (foreign key to Users)
  - Amount (positive for earning, negative for spending)
  - ActivityType (e.g., 'InterviewCompleted', 'QuestionPublished')
  - Description (optional string)
  - RelatedEntityId (nullable Guid - reference to question, interview, etc.)
  - RelatedEntityType (nullable string - e.g., 'Question', 'Interview')
  - CreatedAt
- [x] Create AchievementDefinition model ✅
  - Id, ActivityType (unique string identifier)
  - DisplayName, Description
  - CoinsAwarded (integer)
  - Icon (emoji or icon identifier, e.g., "🪙", "🌟", "🤝")
  - IsActive (bool, default true)
  - MaxOccurrences (nullable integer - null for unlimited, 1 for one-time rewards)
  - CreatedAt, UpdatedAt
- [x] Create database migration ✅
  - AddUserCoins table
  - AddCoinTransactions table
  - AddAchievementDefinitions table
  - Indexes for UserId, TotalCoins (DESC for leaderboard), ActivityType, CreatedAt
  - Unique index on UserCoins.UserId
  - Unique index on AchievementDefinitions.ActivityType
- [x] Update ApplicationDbContext ✅
  - Add DbSet<UserCoins>
  - Add DbSet<CoinTransaction>
  - Add DbSet<AchievementDefinition>
  - Configure relationships and indexes

#### Achievement Types & Coin Values
- [x] Define achievement type constants ✅
  - **Interview Activities:**
    - InterviewCompleted: 🪙 10 coins - "You completed a mock interview"
    - GreatMockInterviewPartner: 🪙 15 coins - "You are a great mock interview partner"
    - JoinMockInterview: 🪙 10 coins - "You join a mock interview"
  - **Question Activities:**
    - QuestionPublished: 🪙 25 coins - "Your interview question is published"
    - QuestionUpvoted: 🪙 5 coins - "Your interview question is upvoted"
    - QuestionInAnotherInterview: 🪙 5 coins - "Your question appears in another interview"
  - **Engagement:**
    - LessonCompleted: 🪙 1 coin - "You complete a lesson"
    - CommentUpvoted: 🪙 5 coins - "Your comment is upvoted"
    - ProfileCompleted: 🪙 10 coins - "You fill out your profile" (one-time)
  - **Referral:**
    - ReferralSuccess: 🪙 100 coins - "You refer someone to Exponent"
  - **Feedback:**
    - FeedbackSubmitted: 🪙 10 coins - "You submit feedback to Exponent"

#### DTOs Creation
- [x] Create UserCoinsDto ✅
  - UserId, TotalCoins, DisplayCoins (formatted: "2.3k", "500")
  - Rank, DisplayRank (e.g., "#108")
- [x] Create CoinTransactionDto ✅
  - Id, Amount, ActivityType, Description
  - CreatedAt, TimeAgo (formatted: "2h ago", "1d ago")
- [x] Create LeaderboardEntryDto ✅
  - Rank, UserId, FirstName, LastName, ProfilePictureUrl
  - TotalCoins, DisplayCoins
- [x] Create AchievementDefinitionDto ✅
  - ActivityType, DisplayName, Description
  - CoinsAwarded, Icon
- [x] Create AwardCoinsRequest ✅
  - UserId, ActivityType, Description (optional)
  - RelatedEntityId (optional), RelatedEntityType (optional)

---

### Day 3-4: Core Coin Service & Business Logic ✅ COMPLETED

#### Backend Implementation - ICoinService Interface
- [x] Create ICoinService interface with methods: ✅
  - **Core Points Granting (Robust & Consistent):**
    - AwardCoinsAsync - Single source of truth for all coin grants
      - Parameters: userId, activityType, description (optional), relatedEntityId (optional), relatedEntityType (optional)
      - Returns: CoinTransaction
  - **User Coins:**
    - GetUserCoinsAsync - Get user's total coins and rank
    - GetUserTransactionsAsync - Get user's transaction history (paginated)
  - **Leaderboard:**
    - GetLeaderboardAsync - Get top users by coins (default limit: 200)
    - GetUserRankAsync - Get specific user's rank
    - RefreshLeaderboardRanksAsync - Background job to update cached ranks
  - **Achievements:**
    - GetAchievementDefinitionsAsync - Get all active achievements
    - CreateOrUpdateAchievementAsync - Admin method to manage achievement definitions

#### Backend Implementation - CoinService
- [x] Implement CoinService class ✅
  - **AwardCoinsAsync - Robust Points Granting (SINGLE SOURCE OF TRUTH):**
    - Step 1: Validate user exists
    - Step 2: Get achievement definition from database
    - Step 3: Check max occurrences if limited (prevent duplicate one-time rewards)
    - Step 4: Create CoinTransaction record
    - Step 5: Update or create UserCoins record (atomic operation)
    - Save changes to database
    - Log success/errors for monitoring
  - **GetUserCoinsAsync:**
    - Query UserCoins by userId
    - Return formatted coins display (e.g., "2.3k" for 2328)
    - Include rank and display rank ("#108")
    - Return zero coins if user has no record yet
  - **GetUserTransactionsAsync:**
    - Query CoinTransactions for user
    - Order by CreatedAt descending (most recent first)
    - Implement pagination (default 50 per page)
    - Format TimeAgo strings ("2h ago", "1d ago", "just now")
  - **GetLeaderboardAsync:**
    - Query UserCoins with User navigation
    - Order by TotalCoins descending
    - Take top N users (default 200, max 200)
    - Calculate rank based on position (1, 2, 3, ...)
    - Format coin display for each entry
  - **GetUserRankAsync:**
    - Get user's total coins
    - Count how many users have more coins
    - Return position + 1 as rank
  - **RefreshLeaderboardRanksAsync:**
    - Query all UserCoins ordered by TotalCoins
    - Update cached Rank field for each user
    - Update LastRankUpdate timestamp
    - Save in batch (background job)
  - **GetAchievementDefinitionsAsync:**
    - Query all active achievements
    - Order by CoinsAwarded descending
    - Return as DTOs with icon and description
  - **CreateOrUpdateAchievementAsync:**
    - Admin method to manage achievement definitions
    - Update existing or create new achievement
    - Set IsActive, MaxOccurrences, Icon, etc.
  - **Helper Methods:**
    - FormatCoins: Display formatting (1.5M, 2.3k, 500)
    - FormatTimeAgo: Relative time display (2h ago, 1d ago)

#### Deployment
- [x] Run database migration ✅
- [x] Seed initial achievement definitions ✅
- [x] Register services in Program.cs ✅
- [ ] Test endpoints with Swagger/Postman (pending controller creation)

---

### Day 5-6: API Endpoints & Service Integration ✅ COMPLETED

#### Backend Implementation - CoinsController
- [x] Create CoinsController with endpoints: ✅
  - **GET /api/coins/me** - Get current user's coins (authenticated)
    - Return UserCoinsDto with formatted display
  - **GET /api/coins/user/{userId}** - Get specific user's coins (public)
    - Allow anonymous access for profile pages
  - **GET /api/coins/me/transactions** - Get current user's transaction history (authenticated)
    - Query parameters: page (default 1), pageSize (default 50)
    - Return paginated list of transactions
  - **GET /api/coins/leaderboard** - Get top users leaderboard (public)
    - Query parameter: limit (default 200, max 200)
    - Return top N users ordered by coins
  - **GET /api/coins/me/rank** - Get current user's rank (authenticated)
    - Return rank number and display format
  - **GET /api/coins/achievements** - Get all ways to earn coins (public)
    - Return list of active achievement definitions
    - Show display name, description, icon, coins awarded
  - **POST /api/coins/award** - Award coins to user (internal, admin only)
    - Body: AwardCoinsRequest
    - Authorize: Admin or System role only
    - Used internally by other services
    - Return CoinTransactionDto on success
- [x] Add error handling for all endpoints ✅
  - User not found
  - Achievement not found
  - Max occurrences reached
  - Invalid parameters

**Files Created:**
- `Controllers/CoinsController.cs` - 8 endpoints for coins management
- `Services/ICoinService.cs` - Service interface
- `Services/CoinService.cs` - Complete service implementation
- `Models/UserCoins.cs` - User coins model
- `Models/CoinTransaction.cs` - Transaction model
- `Models/AchievementDefinition.cs` - Achievement model
- `Constants/AchievementTypes.cs` - Achievement type constants
- `DTOs/Coins/*.cs` - 5 DTO files
- `Data/DbInitializer.cs` - Updated with seed data
- `Data/ApplicationDbContext.cs` - Updated with new DbSets
- `Migrations/*AddCoinsAndAchievementsSystem.cs` - Database migration

#### Integration with Existing Services
- [x] Integrate CoinService into PeerInterviewService ✅
  - **EndInterviewAsync:** Award coins to all participants when interview ends
    - Activity: InterviewCompleted (🪙 10 coins)
    - Award to both interviewer and interviewee
    - Include sessionId as RelatedEntityId
    - Handle errors gracefully (don't fail interview if coins fail)
  - **SubmitFeedbackAsync:** Award coins when user submits feedback
    - Activity: FeedbackSubmitted (🪙 10 coins)
    - Include feedbackId as RelatedEntityId
    - Log errors if coin grant fails
  - **Check Partner Rating:** Award bonus coins for great partners
    - Activity: GreatMockInterviewPartner (🪙 15 coins)
    - Trigger when partner rates user highly (4-5 stars)
    - Additional coins on top of interview completion
- [x] Integrate CoinService into QuestionService ✅
  - **ApproveQuestionAsync:** Award coins when admin approves question
    - Activity: QuestionPublished (🪙 25 coins)
    - Award to question creator (CreatedBy userId)
    - Include questionId as RelatedEntityId
  - **Question used in interview:** Award coins when question appears in interview ✅
    - Activity: QuestionInAnotherInterview (🪙 5 coins)
    - Award to original question creator
    - Implemented in PeerInterviewService when questions are assigned to sessions
  - **Question upvote:** Award coins when question receives upvote (future)
    - Activity: QuestionUpvoted (🪙 5 coins)
- [x] Integrate CoinService into UserService (profile completion) ✅
  - Check profile completeness when user updates profile
  - Award ProfileCompleted (🪙 10 coins) one-time when profile is 100% filled
  - Fields to check: FirstName, LastName, Email, ProfilePicture, Bio, PhoneNumber, Location
- [ ] Integrate CoinService into ReferralService (future)
  - Activity: ReferralSuccess (🪙 100 coins)
  - Award when referred user signs up and completes first interview
- [x] Add try-catch error handling for all integrations ✅
  - Log errors but don't fail primary operations
  - Coin granting should be non-blocking

#### Deployment
- [x] Run database migration ✅
  - Create AddCoinsAndAchievementsSystem migration
  - Test migration locally before deploying
  - Verify all tables, indexes, and foreign keys created correctly
- [x] Seed initial achievement definitions ✅
  - Add SeedAchievementDefinitionsAsync method to DbInitializer
  - Seed all 11 achievement types with proper icons and values
  - Verify seeding in Docker environment
- [x] Register services in Program.cs ✅
  - Add ICoinService and CoinService to dependency injection
  - Ensure service lifetime is scoped
- [x] Test endpoints with Swagger/Postman ✅
  - Test all GET endpoints
  - Test POST /api/coins/award (admin only)
  - Verify error handling and validation

---

### Day 7-8: Frontend Implementation ✅ COMPLETED

#### Frontend Service Layer
- [x] Create coinsService.ts ✅
  - **TypeScript interfaces:**
    - UserCoins (userId, totalCoins, displayCoins, rank, displayRank)
    - CoinTransaction (id, amount, activityType, description, createdAt, timeAgo)
    - LeaderboardEntry (rank, userId, firstName, lastName, profilePictureUrl, totalCoins, displayCoins)
    - AchievementDefinition (activityType, displayName, description, coinsAwarded, icon)
  - **Service methods:**
    - getMyCoins() - Get current user's coins
    - getUserCoins(userId) - Get specific user's coins
    - getMyTransactions(page, pageSize) - Get transaction history with pagination
    - getLeaderboard(limit) - Get top users (default 200)
    - getMyRank() - Get current user's rank
    - getAchievements() - Get all ways to earn coins
  - Configure API base URL
  - Add error handling for network failures

#### Header Component Update
- [x] Update existing Header component ✅
  - Add useState for coins data
  - Add useEffect to load user's coins on mount
  - Display coin icon (🪙) and formatted count in header
  - Make coins display clickable, linking to /leaderboard
  - Format coins display (e.g., "2.3k" for 2328)
  - Add hover effect for better UX
  - Handle loading and error states
  - Position coins display before profile menu
  - Style with existing design system (Tailwind CSS)

#### Leaderboard Page Component
- [x] Create LeaderboardPage.tsx (new page) ✅
  - **Page Header:**
    - Title: "Leaderboard"
    - Subtitle: "Top 200 contributors with the most karma points"
    - Show current user's rank prominently (e.g., "Your all-time rank is #108")
    - Style with large heading and purple accent color
  - **Leaderboard Table:**
    - Three columns: Rank, User, Karma Points
    - Display top 200 users ordered by coins
    - Show medals for top 3: 🥇 (1st), 🥈 (2nd), 🥉 (3rd)
    - Show "#rank" for positions 4-200
    - User column: Profile picture (or initials), full name, clickable link to profile
    - Karma column: Coin icon (🪙) + formatted count (e.g., "2.3k")
    - Hover effect on rows
    - Responsive design (mobile-friendly)
  - **Loading State:**
    - Show spinner while fetching leaderboard
    - Center spinner on page
  - **Empty State:**
    - Handle case where leaderboard is empty
  - **Error Handling:**
    - Show error message if API fails
  - Route: /leaderboard

#### How to Earn Points Page Component
- [x] Create HowToEarnPage.tsx (new page) ✅
  - **Page Header:**
    - Title: "How to earn karma"
    - Description: "Karma is a rough measurement of your contributions to the Exponent community. The better your contributions, the more votes and karma you'll receive."
    - Max width container (4xl) for readability
  - **Achievement List:**
    - Display all active achievements as cards
    - Each card contains:
      - Left: Large icon (4xl size) from achievement definition
      - Middle: Display name (bold) and description (smaller text)
      - Right: Coins awarded badge (purple pill with coin icon 🪙 and number)
    - Card styling: White background, shadow, rounded corners, padding
    - Space between cards for readability
    - Order by coins awarded (highest first)
  - **Achievement Examples:**
    - 🪙 25: "Your interview question is published"
    - 🪙 15: "You are a great mock interview partner"
    - 🪙 10: "You completed a mock interview"
    - 🪙 10: "You join a mock interview"
    - 🪙 10: "You fill out your profile"
    - 🪙 10: "You submit feedback to Exponent"
    - 🪙 5: "Your interview question is upvoted"
    - 🪙 5: "Your question appears in another interview"
    - 🪙 5: "Your comment is upvoted"
    - 🪙 1: "You complete a lesson"
    - 🪙 100: "You refer someone to Exponent"
  - **Loading State:**
    - Show spinner while fetching achievements
  - **Responsive Design:**
    - Stack elements vertically on mobile
    - Horizontal layout on desktop
  - Route: /how-to-earn or /karma

#### User Profile Page Update
- [x] Update existing ProfilePage component ✅
  - **Profile Header Section:**
    - Add coins display below user name and role
    - Show coin icon (🪙) + formatted count + rank in parentheses
    - Example: "🪙 2.3k (#108)"
    - Make it prominent with larger font size
    - Load coins data on component mount
  - **Activity Tab:**
    - Add new "Activity" tab to existing tab navigation
    - Show karma transaction history when selected
    - Load transactions on tab click (paginated, 50 per page)
    - Each transaction card displays:
      - Left: Circular badge with coin icon (🪙)
      - Middle: Description text (e.g., "Completed a mock interview") + relative time ("2h ago")
      - Right: Coins earned (green +10, red -5)
    - Transaction styling: Gray background, rounded corners, padding
    - Stack transactions vertically with spacing
  - **Empty State:**
    - Show message: "No activity yet. Start earning karma by participating in interviews!"
    - Display when transactions list is empty
  - **Loading State:**
    - Show spinner while loading transactions
    - Don't block rest of page
  - **Pagination:**
    - Load more button at bottom (future enhancement)
    - Initially show first 50 transactions
  - Route: /profile/:userId (existing route, update component)

#### Routing & Navigation
- [x] Update App.tsx routing configuration ✅
  - Add route: /leaderboard → LeaderboardPage
  - Add route: /how-to-earn → HowToEarnPage
  - Update existing: /profile/:userId → ProfilePage (enhanced with coins)
- [x] Update navigation menu ✅
  - Add "Leaderboard" link to main navigation (optional)
  - Add "How to Earn" link to footer or help menu
  - Coins in header already link to leaderboard
- [x] Update routing tests ✅
  - Test new routes render correctly
  - Test protected routes (profile requires auth)

**Files Created/Modified:**
- `services/coins.service.ts` - Service for coins API with all TypeScript interfaces
- `pages/leaderboard/LeaderboardPage.tsx` - Leaderboard page showing top 200 users
- `pages/leaderboard/HowToEarnPage.tsx` - Achievement definitions page
- `pages/profile/ProfilePage.tsx` - Updated with Activity & Coins tab
- `components/layout/Navbar.tsx` - Updated with coins display
- `utils/constants.ts` - Updated with new route constants
- `App.tsx` - Updated with new routes for leaderboard and how-to-earn
- `styles/dashboard.css` - Added coins display and leaderboard table styles

---

### Day 9-10: Testing & Quality Assurance

#### Backend Unit Tests
- [ ] Create CoinServiceTests.cs
  - Test AwardCoinsAsync with valid activity (creates transaction, updates total)
  - Test AwardCoinsAsync with max occurrences (throws exception for one-time rewards)
  - Test AwardCoinsAsync with invalid user (throws exception)
  - Test AwardCoinsAsync with invalid activity type (throws exception)
  - Test GetUserCoinsAsync returns correct data and formatting
  - Test GetUserTransactionsAsync with pagination
  - Test GetLeaderboardAsync returns top users ordered by coins
  - Test GetUserRankAsync calculates rank correctly
  - Test RefreshLeaderboardRanksAsync updates cached ranks
  - Test GetAchievementDefinitionsAsync returns active achievements only
- [ ] Create CoinsControllerTests.cs
  - Test all GET endpoints return correct data
  - Test POST /api/coins/award requires admin authorization
  - Test error handling for invalid requests
  - Test query parameter validation (limits, pagination)
- [ ] Integration tests for coin awarding
  - Test coins awarded after interview completion
  - Test coins awarded after feedback submission
  - Test coins awarded after question approval
  - Test one-time rewards (profile completion) can't be duplicated

#### Frontend Tests
- [ ] Test coinsService.ts API calls
- [ ] Test LeaderboardPage renders correctly
- [ ] Test HowToEarnPage displays all achievements
- [ ] Test ProfilePage Activity tab shows transactions
- [ ] Test Header displays coins correctly
- [ ] Test responsive design on mobile/tablet/desktop
- [ ] Test error states (API failures, network errors)
- [ ] Test loading states (spinners, skeleton screens)

---

### Deployment Checklist

- [ ] Run database migration in production
- [ ] Seed achievement definitions
- [ ] Deploy backend API changes
- [ ] Deploy frontend UI changes
- [ ] Test all endpoints in production
- [ ] Verify coins display in header
- [ ] Verify leaderboard loads correctly
- [ ] Monitor application logs for errors
- [ ] Set up alerts for coin service failures

---

### Performance Optimizations

- [ ] **Leaderboard Caching:**
  - Cache leaderboard results for 5-10 minutes
  - Reduce database load for frequently accessed data
  - Use Redis for caching layer
- [ ] **Rank Calculation:**
  - Background job to update cached ranks every hour
  - Avoid real-time rank calculation on every request
  - Use Hangfire or similar for scheduled jobs
- [ ] **Database Indexing:**
  - Index on UserCoins.TotalCoins (DESC) for leaderboard
  - Index on CoinTransactions.UserId for user history
  - Index on CoinTransactions.CreatedAt for time-based queries
- [ ] **Pagination:**
  - Implement cursor-based pagination for large transaction lists
  - Default page size: 50 transactions
- [ ] **Async Operations:**
  - Coin granting should not block main operations
  - Use try-catch to handle failures gracefully
  - Log errors but don't fail primary features (interviews, feedback)

---

### Future Enhancements

- [ ] **Badges & Milestones:**
  - Visual badges for milestones (100, 500, 1000, 5000 coins)
  - Display badges on profile
  - Achievement showcase page
- [ ] **Coin Shop:**
  - Allow users to spend coins on premium features
  - Unlock advanced analytics, priority matching, etc.
  - Deduct coins via CoinService (negative amounts)
- [ ] **Daily Streaks:**
  - Track daily login streaks
  - Bonus coins for consecutive days (1 coin per day, +5 for 7-day streak)
- [ ] **Leaderboard Filters:**
  - Monthly leaderboard (reset each month)
  - Yearly leaderboard
  - All-time leaderboard (current implementation)
  - Filter by interview type or activity
- [ ] **Team Leaderboards:**
  - Company-specific leaderboards
  - Department or group rankings
  - Team competitions and challenges
- [ ] **Achievement Notifications:**
  - Toast notification when coins are earned
  - Email digest of weekly earnings
  - Push notifications for milestones
- [ ] **Social Features:**
  - Share achievements on social media
  - Challenge friends to earn more coins
  - Gift coins to other users

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

---

## Known Bugs & Issues to Fix

### High Priority Bugs

#### 1. **Coding Interview Test Results Not Synchronized Between Users** 🐛
**Status:** To Fix | **Priority:** High | **Component:** Live Interview - Coding

**Description:**
During a live coding interview, test results are not synchronized properly between the two participants. One user may see different test results (pass/fail status) than their partner for the same code execution.

**Observed Behavior:**
- User A sees: Some tests passing (Pass 1, Pass 2) and some failing with error messages
- User B sees: All tests showing as passed with checkmarks
- Both users are in the same interview session viewing the same code

**Expected Behavior:**
- Both users should see identical test results in real-time
- Test execution results should be synchronized via SignalR
- Pass/fail status should match exactly for all test cases

**Impact:**
- Confusing user experience during interviews
- Users cannot collaborate effectively on debugging
- Affects interview quality and assessment accuracy

**Potential Root Cause:**
- SignalR message not broadcasting test results to all participants
- Race condition in test result updates
- Client-side state not updating correctly when receiving results
- Different result formatting/parsing between participants

**Files to Investigate:**
- `backend/Vector.Api/Hubs/CollaborationHub.cs` - SignalR hub for real-time updates
- `backend/Vector.Api/Services/CodeExecutionService.cs` - Test execution and result broadcasting
- `frontend/src/pages/interview/coding/CodingInterviewPage.tsx` - Client-side result handling
- `frontend/src/services/signalr.service.ts` - SignalR client connection

**Proposed Fix:**
1. Ensure `ExecuteCodeAsync` broadcasts results to all session participants via SignalR
2. Add explicit test result synchronization after execution completes
3. Verify all clients subscribe to the correct SignalR events
4. Add logging to track result broadcasting and reception
5. Test with multiple concurrent users in same session

---

### Medium Priority Issues

#### 2. **Behavioral Interview Redirect After Feedback Submission** 🐛
**Status:** To Fix | **Priority:** Medium | **Component:** Behavioral Interview

**Description:**
After completing a behavioral interview and submitting feedback, the URL changes but the page content does not update without a manual refresh.

**Observed Behavior:**
- User fills out and submits feedback survey
- URL changes to post-interview page
- Screen remains on feedback form
- Requires manual page refresh to see updated content

**Expected Behavior:**
- After feedback submission, page should automatically update/redirect
- User should see post-interview summary or next steps
- No manual refresh required

**Impact:**
- Poor user experience
- Users may not realize interview is complete
- May submit duplicate feedback

**Potential Root Cause:**
- React Router navigation not triggering component re-render
- Missing `useEffect` dependency for URL change detection
- Feedback submission response not triggering navigation properly

**Files to Investigate:**
- `frontend/src/pages/interview/behavioral/BehavioralInterviewPage.tsx`
- `frontend/src/pages/interview/behavioral/FeedbackForm.tsx`

**Proposed Fix:**
1. Use `navigate()` with `{ replace: true }` after feedback submission
2. Add loading state during submission
3. Ensure component remounts or updates after navigation
4. Consider using `window.location.reload()` as last resort if state is complex

---

#### 3. **Coding Interview Timer Not Synchronized** 🐛
**Status:** To Fix | **Priority:** Medium | **Component:** Live Interview - Coding

**Description:**
The interview timer displays different remaining times for different users in the same coding interview session.

**Observed Behavior:**
- User A sees: 45:23 remaining
- User B sees: 44:58 remaining  
- Both users joined at the same time
- Time difference grows over the session

**Expected Behavior:**
- All participants see the exact same timer countdown
- Timer synchronized via server time, not client time
- Timer should persist across page refreshes

**Impact:**
- Confusing during timed interviews
- Unfair advantage/disadvantage perception
- Difficult to coordinate time management

**Potential Root Cause:**
- Timer calculated from client-side timestamps
- Not using server-provided start time consistently
- Clock skew between different clients
- Timer not synchronized via SignalR

**Files to Investigate:**
- `frontend/src/pages/interview/coding/CodingInterviewPage.tsx`
- `backend/Vector.Api/Models/LiveInterviewSession.cs` - StartedAt timestamp
- `backend/Vector.Api/Hubs/CollaborationHub.cs` - Timer synchronization

**Proposed Fix:**
1. Calculate remaining time based on server-provided StartedAt timestamp
2. Broadcast timer updates periodically via SignalR (every minute)
3. Use server time instead of `Date.now()` for calculations
4. Add timer sync check on reconnection

---

#### 4. **Practice with a Friend - No Join Link Access** 🐛
**Status:** To Fix | **Priority:** Medium | **Component:** Friend Interview

**Description:**
When a user selects "Practice with a Friend" option, they cannot access the shareable join link after creation. There's no UI to copy or share the link with their friend.

**Observed Behavior:**
- User clicks "Practice with a Friend"
- Session is created
- User is redirected to interview page
- No visible share link or copy button
- Friend has no way to join

**Expected Behavior:**
- After creating friend interview, user should see:
  - Shareable join link
  - Copy to clipboard button
  - Option to share via email/messaging
  - QR code (optional)
- Link should remain accessible during the interview

**Impact:**
- Feature is unusable
- Users cannot practice with friends
- Poor user experience

**Potential Root Cause:**
- Frontend component missing share link UI
- Join URL not being generated or passed to frontend
- Modal/section for sharing not implemented

**Files to Investigate:**
- `frontend/src/pages/dashboard/DashboardPage.tsx` - Friend interview creation
- `frontend/src/pages/interview/[type]/InterviewPage.tsx` - Join link display
- `backend/Vector.Api/Controllers/PeerInterviewController.cs` - CreateFriendInterviewAsync

**Proposed Fix:**
1. Add modal after friend interview creation showing:
   - Join URL (e.g., `/interview/join/{token}`)
   - Copy to clipboard button
   - Share via email option
2. Display join link in interview page header
3. Store join token in session and make it accessible
4. Add "Invite Friend" button that reopens share modal

---

### Low Priority Issues

#### 5. **Excessive Console Logging in Production** 🧹
**Status:** To Fix | **Priority:** Low | **Component:** All

**Description:**
Too many console.log statements in production code, cluttering browser console and potentially exposing sensitive information.

**Expected Behavior:**
- Remove or disable debug logging in production
- Use proper logging levels (debug, info, warn, error)
- Sensitive data should never be logged

**Proposed Fix:**
1. Remove unnecessary console.log statements
2. Use environment-based logging configuration
3. Implement proper logging service with log levels
4. Review and sanitize all log messages

---

#### 6. **Interview Feedback Forms Not Interview-Type Specific** 🐛
**Status:** To Fix | **Priority:** Low | **Component:** Interview Feedback

**Description:**
The feedback forms shown at the end of interviews are generic and not tailored to the specific interview type. Each interview type (Behavioral, System Design, Coding/SQL/ML) should have customized feedback questions relevant to that interview format.

**Observed Behavior:**
- All interview types show the same generic feedback form
- Feedback questions don't match the interview activities
- Users can't provide specific feedback relevant to the interview type

**Expected Behavior:**
- **Behavioral Interview Feedback:**
  - Rate interviewer's listening skills
  - Rate quality of follow-up questions
  - Rate comfort level during conversation
  - Rate relevance of scenarios discussed
  - Comment on STAR method usage

- **System Design Interview Feedback:**
  - Rate clarity of requirements gathering
  - Rate depth of system architecture discussion
  - Rate trade-off analysis quality
  - Rate scalability considerations
  - Rate diagram/visual communication
  - Comment on problem-solving approach

- **Coding/SQL/ML Interview Feedback:**
  - Rate problem explanation clarity
  - Rate hint quality and timing
  - Rate code review feedback quality
  - Rate test case coverage discussion
  - Rate time management
  - Comment on technical depth and debugging guidance

**Impact:**
- Less actionable feedback for users
- Missed opportunity for interview-type specific improvements
- Generic experience reduces value

**Potential Root Cause:**
- Feedback form component uses same questions for all interview types
- No conditional rendering based on interview type
- Backend doesn't validate feedback against interview type

**Files to Investigate:**
- `frontend/src/pages/interview/feedback/FeedbackForm.tsx` - Generic feedback form
- `backend/Vector.Api/Models/InterviewFeedback.cs` - Feedback model
- `backend/Vector.Api/Controllers/PeerInterviewController.cs` - SubmitFeedbackAsync

**Proposed Fix:**
1. Create interview-type specific feedback form components:
   - `BehavioralFeedbackForm.tsx`
   - `SystemDesignFeedbackForm.tsx`
   - `CodingFeedbackForm.tsx`
2. Add conditional rendering based on `interviewType` prop
3. Update backend model to support type-specific feedback fields (optional JSON field)
4. Update API validation to ensure feedback matches interview type
5. Display type-specific feedback in feedback history/analytics

---

## Testing Checklist for Bug Fixes

When fixing these bugs, ensure you test:

- [ ] Test with 2+ users in the same session simultaneously
- [ ] Test with different network conditions (slow, fast, intermittent)
- [ ] Test session persistence across page refreshes
- [ ] Test on different browsers (Chrome, Firefox, Safari, Edge)
- [ ] Test with different timezones
- [ ] Verify SignalR connection and reconnection handling
- [ ] Check database consistency after operations
- [ ] Verify error handling and user feedback
- [ ] Test mobile responsiveness
- [ ] Performance test with multiple concurrent sessions
