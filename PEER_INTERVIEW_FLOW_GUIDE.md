# Peer Interview System - Step-by-Step Flow Guide

## Table of Contents
1. [Scheduling Process](#1-scheduling-process)
2. [Interview Matching Process](#2-interview-matching-process)
3. [Post-Interview Process](#3-post-interview-process)

---

## 1. Scheduling Process

### Overview
The scheduling process allows users to create a peer interview session with their preferences and schedule a time slot.

### Technologies Used
- **Backend**: ASP.NET Core 8.0 (C#)
- **Frontend**: React 18+ with TypeScript
- **Database**: PostgreSQL 15+
- **ORM**: Entity Framework Core 8.0
- **API Style**: RESTful API

### Data Models

#### PeerInterviewSession
```csharp
public class PeerInterviewSession
{
    public Guid Id { get; set; }
    public Guid InterviewerId { get; set; }      // User who created the session
    public Guid? IntervieweeId { get; set; }     // Null until matched
    public Guid? QuestionId { get; set; }        // First question (for interviewer)
    public Guid? SecondQuestionId { get; set; }  // Second question (for interviewee)
    public string Status { get; set; }           // Scheduled, InProgress, Completed, Cancelled
    public DateTime? ScheduledTime { get; set; }
    public int Duration { get; set; } = 45;
    public string? InterviewType { get; set; }   // DSA, System Design, etc.
    public string? PracticeType { get; set; }    // peers, friend, expert
    public string? InterviewLevel { get; set; }  // Beginner, Intermediate, Advanced
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Step-by-Step Flow

#### Frontend (React/TypeScript)

1. **User Navigates to Find Peer Page**
   - Route: `/peer-interviews/find-peer`
   - Component: `FindPeerPage.tsx`
   - Location: `frontend/src/pages/peer-interviews/FindPeerPage.tsx`

2. **User Clicks "Schedule Interview" Button**
   - Opens scheduling modal
   - State: `showScheduleModal = true`

3. **Step 1: Select Interview Type**
   - Options: Data Structures & Algorithms, System Design, Behavioral, Product Management, SQL, Data Science & ML
   - User selects type
   - State: `scheduleData.interviewType = selectedType`

4. **Step 2: Select Practice Type**
   - Options: Practice with peers, Practice with a friend, Expert mock interview
   - User selects type
   - State: `scheduleData.practiceType = selectedType`

5. **Step 3: Select Interview Level**
   - Options: Beginner, Intermediate, Advanced
   - User selects level
   - State: `scheduleData.interviewLevel = selectedLevel`

6. **Step 4: Select Time Slot**
   - System generates available time slots (every 2 hours, 9 AM - 11 PM, 7 days ahead)
   - User selects a time slot
   - State: `scheduleData.selectedTime = selectedDateTime`

7. **User Clicks "Confirm"**
   - Function: `handleScheduleConfirm()`
   - Calls: `peerInterviewService.createSession()`

#### Backend API Request

**Endpoint**: `POST /api/peer-interviews/sessions`

**Request Body**:
```json
{
  "interviewerId": "guid",
  "intervieweeId": null,
  "questionId": null,
  "scheduledTime": "2024-01-15T10:00:00Z",
  "duration": 45,
  "interviewType": "data-structures-algorithms",
  "practiceType": "peers",
  "interviewLevel": "intermediate"
}
```

**Controller**: `PeerInterviewController.CreateSession()`
- Location: `backend/Vector.Api/Controllers/PeerInterviewController.cs`

#### Backend Processing

1. **Service Method**: `PeerInterviewService.CreateSessionAsync()`
   - Location: `backend/Vector.Api/Services/PeerInterviewService.cs`
   - Validates user authorization
   - Creates new `PeerInterviewSession` entity
   - Assigns a question based on `InterviewLevel`:
     - Maps level to difficulty (Beginner → Easy, Intermediate → Medium, Advanced → Hard)
     - Calls `AssignQuestionByLevelAsync()` to get random question from database
   - Sets status to "Scheduled"
   - Saves to database via Entity Framework Core

2. **Question Assignment Logic**
   ```csharp
   private async Task<Guid?> AssignQuestionByLevelAsync(string interviewLevel, Guid? excludeQuestionId = null)
   {
       // Map interview level to difficulty
       string difficulty = MapInterviewLevelToDifficulty(interviewLevel);
       
       // Query InterviewQuestion table
       var questions = await _context.InterviewQuestions
           .Where(q => q.Difficulty == difficulty && q.IsActive)
           .Where(q => excludeQuestionId == null || q.Id != excludeQuestionId)
           .ToListAsync();
       
       // Return random question
       return questions[random.Next(questions.Count)].Id;
   }
   ```

3. **Database Operations**
   - Entity Framework Core inserts into `PeerInterviewSessions` table
   - Foreign key constraints:
     - `InterviewerId` → `Users.Id` (Restrict on delete)
     - `QuestionId` → `InterviewQuestions.Id` (SetNull on delete)
     - `SecondQuestionId` → `InterviewQuestions.Id` (SetNull on delete)

4. **Email Notification** (Optional)
   - Sends confirmation email to interviewer via `EmailService`
   - Uses SendGrid for email delivery

#### Response

**Status Code**: `201 Created`

**Response Body**:
```json
{
  "id": "session-guid",
  "interviewerId": "user-guid",
  "intervieweeId": null,
  "questionId": "question-guid",
  "status": "Scheduled",
  "scheduledTime": "2024-01-15T10:00:00Z",
  "duration": 45,
  "interviewType": "data-structures-algorithms",
  "practiceType": "peers",
  "interviewLevel": "intermediate",
  "createdAt": "2024-01-10T12:00:00Z"
}
```

#### Frontend After Scheduling

- Session appears in "Upcoming Sessions" list
- User can see session details: time, type, level
- User can click "Start Interview" button (appears 10 minutes before scheduled time, or always in dev mode)

### Key Methods/Endpoints

- **Frontend Service**: `peerInterviewService.createSession()`
  - Location: `frontend/src/services/peerInterview.service.ts`
  
- **Backend Controller**: `PeerInterviewController.CreateSession()`
  - Endpoint: `POST /api/peer-interviews/sessions`
  
- **Backend Service**: `PeerInterviewService.CreateSessionAsync()`
  - Location: `backend/Vector.Api/Services/PeerInterviewService.cs`

---

## 2. Interview Matching Process

### Overview
The matching process connects two users who want to practice together. It creates matching requests, finds compatible peers, and merges sessions when both users confirm readiness.

### Technologies Used
- **Backend**: ASP.NET Core 8.0 (C#)
- **Frontend**: React 18+ with TypeScript
- **Database**: PostgreSQL 15+
- **Real-time Updates**: Polling (setInterval) - could be upgraded to SignalR
- **Matching Algorithm**: FIFO (First-In-First-Out) with InterviewType matching

### Data Models

#### InterviewMatchingRequest
```csharp
public class InterviewMatchingRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }                    // User who created the request
    public Guid ScheduledSessionId { get; set; }        // Session to match
    public string Status { get; set; }                  // Pending, Matched, Confirmed, Cancelled, Expired
    public Guid? MatchedUserId { get; set; }            // Matched peer user
    public Guid? MatchedRequestId { get; set; }         // Linked matching request (circular reference)
    public bool UserConfirmed { get; set; }             // Interviewer confirmed
    public bool MatchedUserConfirmed { get; set; }      // Interviewee confirmed
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }            // Expires after 5 minutes
}
```

### Step-by-Step Flow

#### Phase 1: User 1 Starts Matching

1. **User 1 Clicks "Start Interview"**
   - Component: `FindPeerPage.tsx`
   - Function: `handleStartInterview(sessionId)`
   - Calls: `peerInterviewService.startMatching(sessionId)`

2. **Backend API Request**
   - **Endpoint**: `POST /api/peer-interviews/sessions/{sessionId}/start-matching`
   - **Controller**: `PeerInterviewController.StartMatching()`

3. **Backend Processing - Create Matching Request**
   - **Service**: `PeerInterviewService.CreateMatchingRequestAsync()`
   - Creates `InterviewMatchingRequest`:
     ```csharp
     var matchingRequest = new InterviewMatchingRequest
     {
         Id = Guid.NewGuid(),
         UserId = userId,
         ScheduledSessionId = sessionId,
         Status = "Pending",
         CreatedAt = DateTime.UtcNow,
         ExpiresAt = DateTime.UtcNow.AddMinutes(5)  // Expires after 5 minutes
     };
     ```
   - Saves to database
   - Returns matching request

4. **Backend Processing - Find Matching Peer**
   - **Service**: `PeerInterviewService.FindMatchingPeerAsync()`
   - Searches for available matching requests:
     ```csharp
     var availableRequests = await _context.InterviewMatchingRequests
         .Include(r => r.User)
         .Include(r => r.ScheduledSession)
         .Where(r => r.Status == "Pending"
             && r.UserId != userId                    // Not same user
             && r.ExpiresAt > DateTime.UtcNow         // Not expired
             && r.ScheduledSession.InterviewType == userSession.InterviewType)  // Same type
         .OrderBy(r => r.CreatedAt)                   // FIFO - oldest first
         .FirstOrDefaultAsync();
     ```
   - If match found:
     - Links both requests: `userRequest.MatchedRequestId = availableRequests.Id`
     - Sets both statuses to "Matched"
     - Updates `MatchedUserId` on both requests
     - Saves to database
   - If no match found, returns null (User 1 waits)

5. **Response to Frontend**
   ```json
   {
     "matchingRequest": {
       "id": "request-guid",
       "status": "Pending" or "Matched",
       "userId": "user1-guid",
       "scheduledSessionId": "session1-guid"
     },
     "matched": true/false,
     "sessionComplete": false
   }
   ```

6. **Frontend Polling**
   - If status is "Pending", starts polling:
     ```typescript
     const pollInterval = setInterval(async () => {
       const status = await peerInterviewService.getMatchingStatus(sessionId);
       setMatchingStatus(status);
       if (status.status === 'Matched') {
         clearInterval(pollInterval);
         // Show confirmation modal
       }
     }, 2000);  // Poll every 2 seconds
     ```
   - If status is "Matched", shows confirmation modal

#### Phase 2: User 2 Starts Matching (Same Process)

1. User 2 clicks "Start Interview" on their session
2. System creates matching request for User 2
3. System finds User 1's pending request
4. Links the two requests (status → "Matched")
5. Both users now have matched status

#### Phase 3: Both Users Confirm Readiness

1. **User 1 Confirms**
   - Component: `FindPeerPage.tsx`
   - Function: `handleConfirmMatch()`
   - Calls: `peerInterviewService.confirmMatch(matchingRequestId)`

2. **Backend API Request**
   - **Endpoint**: `POST /api/peer-interviews/matching-requests/{id}/confirm`
   - **Controller**: `PeerInterviewController.ConfirmMatch()`

3. **Backend Processing - Confirm Match**
   - **Service**: `PeerInterviewService.ConfirmMatchAsync()`
   - Updates `UserConfirmed` or `MatchedUserConfirmed` based on which user is confirming
   - Updates both linked requests (circular reference)
   - Checks if both confirmed:
     ```csharp
     if (request.UserConfirmed && request.MatchedUserConfirmed)
     {
         // Both confirmed - complete the match
         var session = await CompleteMatchAsync(request.Id);
     }
     ```
   - If both confirmed, calls `CompleteMatchAsync()`

4. **Complete Match - Session Merging**
   - **Service**: `PeerInterviewService.CompleteMatchAsync()`
   - **Critical Logic**:
     - Finds primary request (oldest `CreatedAt`) to ensure consistency
     - Gets both sessions: `session1` (from primary request), `session2` (from matched request)
     - Merges sessions:
       ```csharp
       session1.IntervieweeId = session2.InterviewerId;  // User 2 becomes interviewee
       session1.Status = "InProgress";
       
       // Assign BOTH questions
       session1.QuestionId = session1.QuestionId ?? assignQuestion();        // First question (interviewer)
       session1.SecondQuestionId = session2.QuestionId ?? assignQuestion();  // Second question (interviewee)
       
       // If both questions are same, assign different second question
       if (session1.QuestionId == session1.SecondQuestionId) {
           session1.SecondQuestionId = assignDifferentQuestion();
       }
       
       session2.Status = "Cancelled";  // Mark session2 as cancelled
       ```
     - Updates matching request statuses to "Confirmed"
     - Saves to database

5. **Response to Frontend**
   ```json
   {
     "matchingRequest": { ... },
     "session": {
       "id": "session1-guid",           // PRIMARY SESSION (both users go here)
       "questionId": "question1-guid",  // FIRST question (both users redirect here)
       "secondQuestionId": "question2-guid",
       "interviewerId": "user1-guid",
       "intervieweeId": "user2-guid",
       "status": "InProgress"
     },
     "completed": true
   }
   ```

6. **Frontend Redirect**
   - Both users redirect to same session and question:
     ```typescript
     window.location.href = `/questions/${result.session.questionId}?session=${result.session.id}`;
     ```
   - User 1 = Interviewer, User 2 = Interviewee
   - Both see the same question initially

### Key Methods/Endpoints

- **Frontend Service**: 
  - `peerInterviewService.startMatching(sessionId)`
  - `peerInterviewService.getMatchingStatus(sessionId)`
  - `peerInterviewService.confirmMatch(matchingRequestId)`
  - Location: `frontend/src/services/peerInterview.service.ts`

- **Backend Endpoints**:
  - `POST /api/peer-interviews/sessions/{id}/start-matching`
  - `GET /api/peer-interviews/sessions/{id}/matching-status`
  - `POST /api/peer-interviews/matching-requests/{id}/confirm`

- **Backend Services**:
  - `PeerInterviewService.CreateMatchingRequestAsync()`
  - `PeerInterviewService.FindMatchingPeerAsync()`
  - `PeerInterviewService.ConfirmMatchAsync()`
  - `PeerInterviewService.CompleteMatchAsync()`
  - `PeerInterviewService.GetSessionForMatchedRequestAsync()`
  - Location: `backend/Vector.Api/Services/PeerInterviewService.cs`

### Important Notes

- **Primary Session Selection**: Always uses the session from the oldest matching request to ensure both users join the same session
- **Two Questions**: Session stores both `QuestionId` (first question) and `SecondQuestionId` (for role switching)
- **FIFO Matching**: Matches oldest pending request first
- **Expiration**: Matching requests expire after 5 minutes if not matched
- **Circular Reference**: Matching requests reference each other via `MatchedRequestId`

---

## 3. Post-Interview Process

### Overview
The post-interview process handles session completion, role switching, question changes, and session cancellation/ending.

### Technologies Used
- **Backend**: ASP.NET Core 8.0 (C#)
- **Frontend**: React 18+ with TypeScript
- **Database**: PostgreSQL 15+
- **Real-time Collaboration**: SignalR (for code sync, role switching, question changes)
- **Video**: WebRTC/DraggableVideo component

### Data Models

#### UserSessionParticipant
```csharp
public class UserSessionParticipant
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; }        // Interviewer or Interviewee
    public string Status { get; set; }      // Active, Left, Completed
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
}
```

### Step-by-Step Flow

#### During Interview Session

1. **Question Detail Page Loads**
   - Component: `QuestionDetailPage.tsx`
   - Route: `/questions/{questionId}?session={sessionId}`
   - Checks for active session via URL parameter
   - Loads session details: `peerInterviewService.getSession(sessionId)`

2. **Session Initialization**
   - Connects to SignalR hub: `/api/collaboration/{sessionId}`
   - Hub handles:
     - Real-time code synchronization
     - Test result sharing
     - Role switching notifications
     - Question change notifications

3. **Timer Starts**
   - Timer starts when both users join (status = "InProgress" AND intervieweeId is set)
   - Stored in localStorage: `session_start_{sessionId}`
   - Default duration: 45 minutes
   - Displays elapsed time

#### Role Switching

1. **User Clicks "Switch Role" Button**
   - Component: `QuestionDetailPage.tsx`
   - Function: `handleSwitchRole()`
   - Calls: `peerInterviewService.switchRoles(sessionId)`

2. **Backend API Request**
   - **Endpoint**: `PUT /api/peer-interviews/sessions/{id}/switch-roles`
   - **Controller**: `PeerInterviewController.SwitchRoles()`

3. **Backend Processing**
   - **Service**: `PeerInterviewService.SwitchRolesAsync()`
   - Swaps roles:
     ```csharp
     var temp = session.InterviewerId;
     session.InterviewerId = session.IntervieweeId.Value;
     session.IntervieweeId = temp;
     ```
   - Swaps questions:
     ```csharp
     var tempQuestion = session.QuestionId;
     session.QuestionId = session.SecondQuestionId;
     session.SecondQuestionId = tempQuestion;
     ```
   - Updates database
   - Sends SignalR notification: `RoleSwitched`

4. **Frontend Updates**
   - SignalR receives `RoleSwitched` event
   - Reloads session data
   - Redirects to new question if question changed
   - Updates UI (role indicators, question display)

#### Change Question

1. **Interviewer Clicks "Switch Question" Button**
   - Only interviewer can change question
   - Function: `handleChangeQuestion()`
   - Calls: `peerInterviewService.changeQuestion(sessionId)`

2. **Backend API Request**
   - **Endpoint**: `PUT /api/peer-interviews/sessions/{id}/change-question`
   - **Controller**: `PeerInterviewController.ChangeQuestion()`

3. **Backend Processing**
   - **Service**: `PeerInterviewService.ChangeQuestionAsync()`
   - Assigns new question (excludes current question):
     ```csharp
     var newQuestionId = await AssignQuestionByLevelAsync(
         session.InterviewLevel, 
         session.QuestionId  // Exclude current question
     );
     session.QuestionId = newQuestionId;
     ```
   - Updates database
   - Sends SignalR notification: `QuestionChanged`

4. **Frontend Updates**
   - SignalR receives `QuestionChanged` event
   - Navigates to new question: `/questions/{newQuestionId}?session={sessionId}`
   - Reloads question data

#### End Session / Complete Interview

1. **User Clicks "End Session" or "Finish Interview"**
   - Component: `QuestionDetailPage.tsx`
   - Function: `handleEndSession()`
   - Calls: `peerInterviewService.updateSessionStatus(sessionId, "Completed")`

2. **Backend API Request**
   - **Endpoint**: `PUT /api/peer-interviews/sessions/{id}/status`
   - **Controller**: `PeerInterviewController.UpdateSessionStatus()`
   - Request body: `{ "status": "Completed" }`

3. **Backend Processing**
   - **Service**: `PeerInterviewService.UpdateSessionStatusAsync()`
   - Updates session status:
     ```csharp
     session.Status = "Completed";
     session.UpdatedAt = DateTime.UtcNow;
     await _context.SaveChangesAsync();
     ```
   - Updates participant records:
     ```csharp
     var participants = await _context.UserSessionParticipants
         .Where(p => p.SessionId == sessionId)
         .ToListAsync();
     foreach (var p in participants) {
         p.Status = "Completed";
         p.LeftAt = DateTime.UtcNow;
     }
     ```

4. **Frontend Redirect**
   - Shows survey modal (`InterviewSurvey` component)
   - After survey, redirects to: `/peer-interviews/find-peer?session={sessionId}&showSurvey=true`
   - Or redirects to feedback page (if implemented)

#### Cancel Session

1. **User Clicks "Cancel" Button**
   - Component: `FindPeerPage.tsx`
   - Function: `handleCancelSession()`
   - Calls: `peerInterviewService.cancelSession(sessionId)`

2. **Backend API Request**
   - **Endpoint**: `PUT /api/peer-interviews/sessions/{id}/cancel`
   - **Controller**: `PeerInterviewController.CancelSession()`

3. **Backend Processing**
   - **Service**: `PeerInterviewService.CancelSessionAsync()`
   - Validates user authorization (must be interviewer or interviewee)
   - Updates participant status (not session status):
     ```csharp
     var participant = await _context.UserSessionParticipants
         .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId);
     participant.Status = "Cancelled";
     participant.LeftAt = DateTime.UtcNow;
     ```
   - If matching request exists, marks it as "Cancelled"
   - Note: Session status remains unchanged (allows other user to continue)

4. **Frontend Updates**
   - Removes session from "Upcoming Sessions" list
   - Shows in "Past Sessions" list

### Key Methods/Endpoints

- **Frontend Service**: 
  - `peerInterviewService.switchRoles(sessionId)`
  - `peerInterviewService.changeQuestion(sessionId)`
  - `peerInterviewService.updateSessionStatus(sessionId, status)`
  - `peerInterviewService.cancelSession(sessionId)`
  - Location: `frontend/src/services/peerInterview.service.ts`

- **Backend Endpoints**:
  - `PUT /api/peer-interviews/sessions/{id}/switch-roles`
  - `PUT /api/peer-interviews/sessions/{id}/change-question`
  - `PUT /api/peer-interviews/sessions/{id}/status`
  - `PUT /api/peer-interviews/sessions/{id}/cancel`

- **Backend Services**:
  - `PeerInterviewService.SwitchRolesAsync()`
  - `PeerInterviewService.ChangeQuestionAsync()`
  - `PeerInterviewService.UpdateSessionStatusAsync()`
  - `PeerInterviewService.CancelSessionAsync()`
  - Location: `backend/Vector.Api/Services/PeerInterviewService.cs`

- **SignalR Hub**: `CollaborationHub`
  - Location: `backend/Vector.Api/Hubs/CollaborationHub.cs`
  - Methods: `JoinSession`, `SendCodeChange`, `SendTestResults`
  - Events: `RoleSwitched`, `QuestionChanged`, `CodeUpdated`, `TestResultsUpdated`

### Important Notes

- **Session Status Flow**: `Scheduled` → `InProgress` → `Completed` or `Cancelled`
- **Participant Tracking**: Each user has a `UserSessionParticipant` record for independent tracking
- **Real-time Updates**: SignalR ensures both users see changes immediately (role switches, question changes)
- **Question Storage**: Session stores both questions for seamless role switching
- **Timer Management**: Timer stored in localStorage to persist across page refreshes
- **Session Completion**: When session is completed, both users can access feedback (if implemented)

### Future Enhancements (Not Yet Implemented)

- **Feedback System**: Post-interview feedback exchange
- **Session Recording**: Optional recording of interview sessions
- **Analytics**: Track session duration, questions used, role switches
- **Notifications**: Email/SMS reminders for upcoming sessions

---

## Summary

### Scheduling
1. User selects preferences (type, level, time)
2. Backend creates `PeerInterviewSession` with assigned question
3. Session appears in "Upcoming Sessions"

### Matching
1. User clicks "Start Interview"
2. Backend creates `InterviewMatchingRequest` (status: "Pending")
3. System finds compatible peer (FIFO matching by InterviewType)
4. Links requests (status: "Matched")
5. Both users confirm readiness
6. Backend merges sessions (session1 = primary, session2 cancelled)
7. Both users redirect to same session and question

### Post-Interview
1. Users collaborate in session (real-time via SignalR)
2. Can switch roles (swaps questions)
3. Can change question (interviewer only)
4. Can end session (status: "Completed")
5. Can cancel session (participant status: "Cancelled")
6. Survey/Feedback (future implementation)

### Database Tables
- `PeerInterviewSessions`: Main session data
- `InterviewMatchingRequests`: Matching logic
- `UserSessionParticipants`: Per-user session tracking
- `InterviewQuestions`: Question bank
- `Users`: User data

### Key Design Decisions
- **Two Sessions Merged**: When matched, session2 is cancelled, session1 becomes primary
- **Primary Session Selection**: Always uses oldest request's session for consistency
- **Two Questions**: Session stores both questions for role switching
- **FIFO Matching**: Matches oldest pending request first
- **Participant Independence**: Each user has separate participant record
- **Real-time Updates**: SignalR for collaboration features

