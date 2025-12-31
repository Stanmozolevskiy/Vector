# Peer Interview Matching and Live Session Formation Process

## Overview

This document provides a detailed, step-by-step explanation of how users are matched and live interview sessions are created in the peer interview system.

---

## Database Models

### 1. ScheduledInterviewSession

Represents a scheduled interview that a user has created.

```csharp
public class ScheduledInterviewSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public string InterviewType { get; set; }          // "data-structures-algorithms", "system-design", etc.
    public string PracticeType { get; set; }           // "peers", "friend", etc.
    public string InterviewLevel { get; set; }         // "beginner", "intermediate", "advanced"
    public DateTime ScheduledStartAt { get; set; }
    public string Status { get; set; }                 // "Scheduled", "InProgress", "Completed", "Cancelled"
    public Guid? AssignedQuestionId { get; set; }      // Question assigned during scheduling (for data-structures-algorithms)
    public InterviewQuestion? AssignedQuestion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Key Points:**
- Created when user schedules an interview
- For "data-structures-algorithms" type, a question is assigned during scheduling
- Status transitions: `Scheduled` → `InProgress` → `Completed`

### 2. InterviewMatchingRequest

Represents a user's request to be matched with another user. This is created when a user clicks "Start Interview".

```csharp
public class InterviewMatchingRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public Guid ScheduledSessionId { get; set; }
    public ScheduledInterviewSession ScheduledSession { get; set; }
    public string InterviewType { get; set; }          // Copied from ScheduledSession
    public string PracticeType { get; set; }           // Copied from ScheduledSession
    public string InterviewLevel { get; set; }         // Copied from ScheduledSession
    public DateTime ScheduledStartAt { get; set; }     // Copied from ScheduledSession
    public string Status { get; set; }                 // "Pending", "Matched", "Confirmed", "Expired", "Cancelled"
    public Guid? MatchedUserId { get; set; }           // Set when matched
    public User? MatchedUser { get; set; }
    public Guid? LiveSessionId { get; set; }           // Set when live session is created
    public LiveInterviewSession? LiveSession { get; set; }
    public bool UserConfirmed { get; set; }            // Has this user confirmed readiness?
    public bool MatchedUserConfirmed { get; set; }     // Has the matched user confirmed readiness?
    public DateTime ExpiresAt { get; set; }            // 10 minutes after creation
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Key Points:**
- Status transitions: `Pending` → `Matched` → `Confirmed`
- Created when user clicks "Start Interview" button
- Expires 10 minutes after creation if not matched
- `LiveSessionId` is set immediately when a match is found (before confirmation)

### 3. LiveInterviewSession

Represents an active live interview session between two users. Created immediately when a match is found.

```csharp
public class LiveInterviewSession
{
    public Guid Id { get; set; }
    public Guid? ScheduledSessionId { get; set; }
    public ScheduledInterviewSession? ScheduledSession { get; set; }
    public Guid? FirstQuestionId { get; set; }         // First question for the session
    public InterviewQuestion? FirstQuestion { get; set; }
    public Guid? SecondQuestionId { get; set; }        // Second question (for role switching)
    public InterviewQuestion? SecondQuestion { get; set; }
    public Guid? ActiveQuestionId { get; set; }        // Currently active question
    public string Status { get; set; }                 // "Pending", "InProgress", "Completed", "Cancelled"
    public DateTime? StartedAt { get; set; }           // Set when both users confirm
    public DateTime? EndedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<LiveInterviewParticipant> Participants { get; set; }
    public ICollection<InterviewFeedback> Feedbacks { get; set; }
}
```

**Key Points:**
- Created immediately when two users are matched (before confirmation)
- Status starts as `Pending`, changes to `InProgress` when both users confirm
- Contains two questions: one for each user's assigned question (if data-structures-algorithms)
- `ScheduledSessionId` points to the scheduler's scheduled session

### 4. LiveInterviewParticipant

Represents a participant in a live interview session.

```csharp
public class LiveInterviewParticipant
{
    public Guid Id { get; set; }
    public Guid LiveSessionId { get; set; }
    public LiveInterviewSession LiveSession { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public string Role { get; set; }                   // "Interviewer" or "Interviewee"
    public bool IsActive { get; set; }
    public DateTime? JoinedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Key Points:**
- Two participants per session (one Interviewer, one Interviewee)
- Roles can be switched during the session
- Created when the live session is created

---

## Step-by-Step Process

### Phase 1: User Schedules Interview

**Step 1.1:** User creates a scheduled interview session
- User selects: Interview Type, Practice Type, Interview Level, and Scheduled Start Time
- **Backend:** `ScheduleInterviewSessionAsync` method creates a `ScheduledInterviewSession`
- **For "data-structures-algorithms" type:** A random question is assigned and stored in `AssignedQuestionId`
- Status: `"Scheduled"`

**Database State:**
```
ScheduledInterviewSession:
  - Id: {guid}
  - UserId: {user1_id}
  - InterviewType: "data-structures-algorithms"
  - PracticeType: "peers"
  - InterviewLevel: "beginner"
  - ScheduledStartAt: {datetime}
  - Status: "Scheduled"
  - AssignedQuestionId: {question_id_1}  // For data-structures-algorithms
  - CreatedAt: {datetime}
```

---

### Phase 2: User Starts Matching (Clicks "Start Interview")

**Step 2.1:** User clicks "Start Interview" button on their scheduled session
- **Frontend:** Calls `POST /api/peer-interviews/sessions/{scheduledSessionId}/start-matching`
- **Backend:** `StartMatchingAsync` method is called

**Step 2.2:** Check for existing matching request
- Backend checks if there's already a matching request for this scheduled session
- If exists and status is `"Matched"` with a `LiveSessionId`, return existing live session
- If exists and status is `"Pending"` or `"Matched"`, return existing request

**Step 2.3:** Create new matching request
- If no existing request, create a new `InterviewMatchingRequest`
- Copy interview details from `ScheduledInterviewSession`
- Set `ExpiresAt` to 10 minutes from now
- Status: `"Pending"`
- `UserConfirmed`: `false`
- `MatchedUserId`: `null`
- `LiveSessionId`: `null`

**Database State:**
```
InterviewMatchingRequest:
  - Id: {matching_request_1_id}
  - UserId: {user1_id}
  - ScheduledSessionId: {scheduled_session_1_id}
  - InterviewType: "data-structures-algorithms"
  - PracticeType: "peers"
  - InterviewLevel: "beginner"
  - ScheduledStartAt: {datetime}
  - Status: "Pending"
  - ExpiresAt: {datetime + 10 minutes}
  - UserConfirmed: false
  - MatchedUserId: null
  - LiveSessionId: null
  - CreatedAt: {datetime}
```

**Step 2.4:** Attempt immediate match
- Backend calls `TryMatchAsync(matchingRequest)` to find a match
- If match found, proceed to Phase 3
- If no match found, user waits in queue (status remains `"Pending"`)

---

### Phase 3: Matching Process

**Step 3.1:** `TryMatchAsync` searches for potential matches
- Query criteria (all must match):
  - `Status == "Pending"`
  - `ExpiresAt > DateTime.UtcNow` (not expired)
  - `InterviewType == request.InterviewType` (hard match)
  - `PracticeType == request.PracticeType` (hard match)
  - `ScheduledStartAt.Date == request.ScheduledStartAt.Date` (same day)
  - `UserId != request.UserId` (not the same user)
- Order by `CreatedAt` (FIFO - oldest first)

**Step 3.2:** Best match selection
- First, try to find exact level match (`InterviewLevel == request.InterviewLevel`)
- If no exact match, accept any match (soft match on level)
- If `bestMatch` is found, proceed to Step 3.3
- If no match found, return `false` (user stays in queue)

**Step 3.3:** Create live session immediately
- Call `CreateLiveSessionForMatchAsync(request, bestMatch)`
- This creates the live session BEFORE user confirmation
- See Phase 4 for details

**Step 3.4:** Update both matching requests
- Set `request.MatchedUserId = bestMatch.UserId`
- Set `bestMatch.MatchedUserId = request.UserId`
- Set both `Status = "Matched"`
- Set both `LiveSessionId = liveSession.Id` (immediately!)
- Set both `UpdatedAt = DateTime.UtcNow`
- Save to database

**Database State (after match found):**
```
InterviewMatchingRequest (User 1):
  - Id: {matching_request_1_id}
  - UserId: {user1_id}
  - Status: "Matched"
  - MatchedUserId: {user2_id}
  - LiveSessionId: {live_session_id}
  - UserConfirmed: false
  - UpdatedAt: {datetime}

InterviewMatchingRequest (User 2):
  - Id: {matching_request_2_id}
  - UserId: {user2_id}
  - Status: "Matched"
  - MatchedUserId: {user1_id}
  - LiveSessionId: {live_session_id}
  - UserConfirmed: false
  - UpdatedAt: {datetime}
```

---

### Phase 4: Creating Live Session

**Step 4.1:** Determine roles
- User who scheduled first (older `CreatedAt` on matching request) = **Interviewer**
- User who scheduled second = **Interviewee**

**Step 4.2:** Get scheduled sessions
- Load both users' `ScheduledInterviewSession` records
- Include `AssignedQuestion` navigation property

**Step 4.3:** Select questions
- **For "data-structures-algorithms" type:**
  - Use `schedulerSession.AssignedQuestionId` as `FirstQuestionId`
  - Use `matchedSession.AssignedQuestionId` as `SecondQuestionId`
  - **If both questions are the same:**
    - Select a different question with the same difficulty
    - Use `SelectRandomQuestionByDifficultyAsync` to find a different question
- **For other types:**
  - Select random questions using `SelectRandomQuestionAsync`
  - Ensure `SecondQuestionId != FirstQuestionId`

**Step 4.4:** Create `LiveInterviewSession`
```csharp
var liveSession = new LiveInterviewSession
{
    Id = Guid.NewGuid(),
    ScheduledSessionId = schedulerRequest.ScheduledSessionId,  // Points to scheduler's scheduled session
    FirstQuestionId = firstQuestionId,
    SecondQuestionId = secondQuestionId,
    ActiveQuestionId = firstQuestionId,  // Start with first question
    Status = "Pending",  // Will change to "InProgress" when both confirm
    StartedAt = null,    // Will be set when both confirm
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};
```

**Step 4.5:** Create participants
- Create `LiveInterviewParticipant` for scheduler (role: "Interviewer")
- Create `LiveInterviewParticipant` for matched user (role: "Interviewee")

**Database State:**
```
LiveInterviewSession:
  - Id: {live_session_id}
  - ScheduledSessionId: {scheduled_session_1_id}
  - FirstQuestionId: {question_id_1}
  - SecondQuestionId: {question_id_2}
  - ActiveQuestionId: {question_id_1}
  - Status: "Pending"
  - StartedAt: null
  - CreatedAt: {datetime}

LiveInterviewParticipant:
  - Id: {participant_1_id}
  - LiveSessionId: {live_session_id}
  - UserId: {user1_id}
  - Role: "Interviewer"
  - IsActive: true
  - JoinedAt: {datetime}

LiveInterviewParticipant:
  - Id: {participant_2_id}
  - LiveSessionId: {live_session_id}
  - UserId: {user2_id}
  - Role: "Interviewee"
  - IsActive: true
  - JoinedAt: {datetime}
```

**Step 4.6:** Save to database
- Add `LiveInterviewSession` to context
- Add both `LiveInterviewParticipant` records to context
- Save changes

---

### Phase 5: User Confirmation

**Step 5.1:** Both users see "Join your interview" button
- Frontend polls matching status every 2 seconds
- When status is `"Matched"` and `LiveSessionId` exists, show confirmation button
- 15-second countdown starts

**Step 5.2:** User clicks "Join your interview"
- **Frontend:** Calls `POST /api/peer-interviews/matching-requests/{matchingRequestId}/confirm`
- **Backend:** `ConfirmMatchAsync` method is called

**Step 5.3:** Mark user as confirmed
- Find the matching request (must belong to the user)
- Check: `Status == "Matched"` and `LiveSessionId != null`
- Set `UserConfirmed = true`
- Update `UpdatedAt = DateTime.UtcNow`

**Step 5.4:** Check if both users confirmed
- Find the other user's matching request
- Check: `matchingRequest.UserConfirmed && otherUserRequest.UserConfirmed`
- If both confirmed, proceed to Step 5.5
- If only one confirmed, return response with `Completed = false`

**Step 5.5:** Start the session (both confirmed)
- Update `LiveInterviewSession`:
  - `Status = "InProgress"`
  - `StartedAt = DateTime.UtcNow`
- Update both `InterviewMatchingRequest` records:
  - `Status = "Confirmed"`
- Update `ScheduledInterviewSession`:
  - `Status = "InProgress"`
- Save to database

**Step 5.6:** Redirect both users
- Backend returns `ConfirmMatchResponseDto` with `Completed = true` and `Session`
- Frontend redirects both users to: `/questions/{firstQuestionId}?session={liveSessionId}`

**Database State (after both confirmed):**
```
LiveInterviewSession:
  - Status: "InProgress"
  - StartedAt: {datetime}

InterviewMatchingRequest (User 1):
  - Status: "Confirmed"
  - UserConfirmed: true

InterviewMatchingRequest (User 2):
  - Status: "Confirmed"
  - UserConfirmed: true

ScheduledInterviewSession:
  - Status: "InProgress"
```

---

### Phase 6: Session Expiration (If Not Confirmed)

**Step 6.1:** 15-second timeout expires
- If one or both users don't confirm within 15 seconds
- Frontend calls `ExpireMatchIfNotConfirmedAsync` (or backend auto-expires)

**Step 6.2:** Cleanup expired match
- Delete the `LiveInterviewSession` (and its participants)
- Reset both `InterviewMatchingRequest` records:
  - `Status = "Pending"`
  - `MatchedUserId = null`
  - `LiveSessionId = null`
  - `UserConfirmed = false`
  - `MatchedUserConfirmed = false`
- Save to database

**Step 6.3:** Re-queue users
- Both users are put back into the matching queue
- Status is `"Pending"` again
- `TryMatchAsync` is called again for both users (they can match with other users or each other)

---

## Status Transitions Summary

### ScheduledInterviewSession
```
Scheduled → InProgress → Completed
           ↓
        Cancelled
```

### InterviewMatchingRequest
```
Pending → Matched → Confirmed
         ↓
      Expired/Cancelled
```

### LiveInterviewSession
```
Pending → InProgress → Completed
         ↓
      Cancelled
```

---

## Key Design Decisions

1. **Live Session Created Immediately:** The `LiveInterviewSession` is created as soon as a match is found, not after confirmation. This ensures questions are assigned and roles are set before users confirm.

2. **Question Assignment:** For "data-structures-algorithms" interviews, questions are assigned during scheduling and reused in the live session. If both users have the same assigned question, a different question of the same difficulty is selected.

3. **Role Assignment:** The user who scheduled first (older `CreatedAt`) becomes the Interviewer. The matched user becomes the Interviewee. Roles can be switched during the session.

4. **FIFO Matching:** Matches are made using First-In-First-Out (FIFO) ordering based on `CreatedAt`. This ensures fairness.

5. **Hard vs Soft Matching:**
   - **Hard matches (required):** Interview Type, Practice Type, Same Day
   - **Soft match (preferred):** Interview Level (will accept different levels if needed)

6. **Confirmation Window:** Users have 15 seconds to confirm readiness. If both don't confirm, the match is expired and both users are requeued.

---

## API Endpoints Used

1. **POST** `/api/peer-interviews/scheduled` - Schedule an interview
2. **POST** `/api/peer-interviews/sessions/{id}/start-matching` - Start matching process
3. **GET** `/api/peer-interviews/sessions/{id}/matching-status` - Get matching status (polled)
4. **POST** `/api/peer-interviews/matching-requests/{id}/confirm` - Confirm readiness
5. **GET** `/api/peer-interviews/sessions/{id}` - Get live session details
6. **POST** `/api/peer-interviews/matching-requests/{id}/expire` - Expire match (if timeout)

