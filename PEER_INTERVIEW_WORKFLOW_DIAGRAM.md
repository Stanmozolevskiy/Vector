# Peer Interview Scheduling, Matching & Rematching Workflow

## Complete Workflow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           FRONTEND (React/TypeScript)                            │
└─────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────┐
│                           BACKEND (.NET/C#)                                     │
└─────────────────────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════════════════════
PHASE 1: SCHEDULING
═══════════════════════════════════════════════════════════════════════════════════

[User] → FindPeerPage.tsx
    │
    ├─→ scheduleInterview()
    │   └─→ peerInterviewService.scheduleInterview()
    │       └─→ POST /api/peer-interviews/scheduled
    │           │
    │           └─→ PeerInterviewController.ScheduleSession()
    │               └─→ PeerInterviewService.ScheduleInterviewSessionAsync()
    │                   │
    │                   ├─→ Creates: ScheduledInterviewSession
    │                   │   ├─ UserId
    │                   │   ├─ InterviewType
    │                   │   ├─ PracticeType
    │                   │   ├─ InterviewLevel
    │                   │   ├─ ScheduledStartAt
    │                   │   ├─ Status = "Scheduled"
    │                   │   └─ AssignedQuestionId (if data-structures-algorithms)
    │                   │
    │                   └─→ Returns: ScheduledInterviewSessionDto
    │
    └─→ Updates UI: Shows session in "Upcoming interviews"

═══════════════════════════════════════════════════════════════════════════════════
PHASE 2: STARTING MATCHING
═══════════════════════════════════════════════════════════════════════════════════

[User clicks "Start interview"] → FindPeerPage.tsx
    │
    ├─→ handleStartInterview(sessionId)
    │   └─→ peerInterviewService.startMatching(sessionId)
    │       └─→ POST /api/peer-interviews/sessions/{sessionId}/start-matching
    │           │
    │           └─→ PeerInterviewController.StartMatching()
    │               └─→ PeerInterviewService.StartMatchingAsync()
    │                   │
    │                   ├─→ Checks: ScheduledInterviewSession exists
    │                   │
    │                   ├─→ Checks: Existing InterviewMatchingRequest?
    │                   │   └─→ If exists & Confirmed: Return existing
    │                   │
    │                   ├─→ Creates: InterviewMatchingRequest
    │                   │   ├─ UserId
    │                   │   ├─ ScheduledSessionId
    │                   │   ├─ InterviewType
    │                   │   ├─ PracticeType
    │                   │   ├─ InterviewLevel
    │                   │   ├─ Status = "Pending"
    │                   │   ├─ ExpiresAt = Now + 10 minutes
    │                   │   └─ CreatedAt = Now
    │                   │
    │                   └─→ Calls: TryMatchAsync(matchingRequest)
    │                       │
    │                       ├─→ CleanupExpiredRequestsAsync()
    │                       │
    │                       ├─→ Finds: Potential matches (FIFO queue)
    │                       │   └─ Filters: Same InterviewType, PracticeType, Date
    │                       │       └─ Orders: CreatedAt ASC (oldest first)
    │                       │
    │                       ├─→ If match found:
    │                       │   ├─ Updates: Request1.Status = "Matched"
    │                       │   ├─ Updates: Request2.Status = "Matched"
    │                       │   ├─ Sets: Request1.MatchedUserId = User2.Id
    │                       │   ├─ Sets: Request2.MatchedUserId = User1.Id
    │                       │   ├─ Sets: Request1.LiveSessionId = null (not created yet)
    │                       │   └─ Sets: Request2.LiveSessionId = null
    │                       │
    │                       └─→ Returns: bool (matched or not)
    │
    └─→ Updates UI: Shows "Waiting for your partner..." modal
        └─→ Starts: startMatchingPoll(sessionId)

═══════════════════════════════════════════════════════════════════════════════════
PHASE 3: POLLING FOR MATCH STATUS
═══════════════════════════════════════════════════════════════════════════════════

[Every 2 seconds] → FindPeerPage.tsx
    │
    └─→ startMatchingPoll(sessionId)
        └─→ peerInterviewService.getMatchingStatus(sessionId)
            └─→ GET /api/peer-interviews/sessions/{sessionId}/matching-status
                │
                └─→ PeerInterviewController.GetMatchingStatus()
                    └─→ PeerInterviewService.GetMatchingStatusAsync()
                        │
                        ├─→ Finds: InterviewMatchingRequest (Status != "Expired", != "Cancelled")
                        │
                        ├─→ If Status == "Pending":
                        │   └─→ Calls: TryMatchAsync(matchingRequest)
                        │       └─→ Attempts to match with other pending requests
                        │
                        ├─→ If Status == "Matched":
                        │   └─→ Returns: MatchingRequestDto
                        │       ├─ status = "Matched"
                        │       ├─ matchedUserId
                        │       ├─ userConfirmed = false
                        │       └─ matchedUserConfirmed = false
                        │
                        └─→ If Status == "Confirmed":
                            └─→ Returns: MatchingRequestDto
                                ├─ status = "Confirmed"
                                ├─ liveSessionId
                                └─ Both confirmed = true

═══════════════════════════════════════════════════════════════════════════════════
PHASE 4: CONFIRMING MATCH
═══════════════════════════════════════════════════════════════════════════════════

[User clicks "Confirm"] → FindPeerPage.tsx
    │
    └─→ handleConfirmMatch()
        └─→ peerInterviewService.confirmMatch(matchingRequestId)
            └─→ POST /api/peer-interviews/matching-requests/{requestId}/confirm
                │
                └─→ PeerInterviewController.ConfirmMatch()
                    └─→ PeerInterviewService.ConfirmMatchAsync()
                        │
                        ├─→ Finds: InterviewMatchingRequest (user's own request)
                        │
                        ├─→ Sets: matchingRequest.UserConfirmed = true
                        │
                        ├─→ Finds: Other user's InterviewMatchingRequest
                        │   └─→ Where: MatchedUserId == current user
                        │
                        ├─→ Checks: Both confirmed?
                        │   └─→ If YES:
                        │       ├─→ Creates: LiveInterviewSession
                        │       │   ├─ Status = "InProgress"
                        │       │   ├─ StartedAt = Now
                        │       │   ├─ FirstQuestionId (from scheduler's session)
                        │       │   ├─ SecondQuestionId (from matched user's session)
                        │       │   └─ ActiveQuestionId = FirstQuestionId
                        │       │
                        │       ├─→ Creates: LiveInterviewParticipant (Interviewer)
                        │       │   └─ Role = "Interviewer" (scheduler)
                        │       │
                        │       ├─→ Creates: LiveInterviewParticipant (Interviewee)
                        │       │   └─ Role = "Interviewee" (matched user)
                        │       │
                        │       ├─→ Updates: Request1.Status = "Confirmed"
                        │       ├─→ Updates: Request2.Status = "Confirmed"
                        │       ├─→ Sets: Request1.LiveSessionId = liveSession.Id
                        │       └─→ Sets: Request2.LiveSessionId = liveSession.Id
                        │
                        └─→ Returns: ConfirmMatchResponseDto
                            ├─ completed = true/false
                            └─ session = LiveInterviewSessionDto (if completed)

═══════════════════════════════════════════════════════════════════════════════════
PHASE 5: EXPIRATION & REMATCHING
═══════════════════════════════════════════════════════════════════════════════════

[15 seconds timeout] → FindPeerPage.tsx
    │
    └─→ handleMatchTimeout()
        └─→ peerInterviewService.expireMatch(matchingRequestId)
            └─→ POST /api/peer-interviews/matching-requests/{requestId}/expire
                │
                └─→ PeerInterviewController.ExpireMatch()
                    └─→ PeerInterviewService.ExpireMatchIfNotConfirmedAsync()
                        │
                        ├─→ Finds: InterviewMatchingRequest
                        ├─→ Checks: Status == "Matched"
                        ├─→ Checks: Match age >= 15 seconds
                        │
                        ├─→ Finds: Other user's InterviewMatchingRequest
                        │
                        ├─→ Checks: Both confirmed?
                        │   └─→ If YES: Return false (don't expire)
                        │
                        ├─→ If NOT both confirmed:
                        │   ├─→ Sets: Request1.Status = "Expired"
                        │   ├─→ Sets: Request2.Status = "Expired"
                        │   │
                        │   ├─→ Creates: NEW InterviewMatchingRequest (User1)
                        │   │   ├─ Status = "Pending"
                        │   │   ├─ ExpiresAt = Now + 10 minutes
                        │   │   └─ CreatedAt = Now
                        │   │
                        │   └─→ Creates: NEW InterviewMatchingRequest (User2)
                        │       ├─ Status = "Pending"
                        │       ├─ ExpiresAt = Now + 10 minutes
                        │       └─ CreatedAt = Now
                        │
                        └─→ Returns: true (expired)

[Next poll cycle] → FindPeerPage.tsx
    │
    └─→ getMatchingStatus() finds new "Pending" request
        └─→ GetMatchingStatusAsync() detects Status == "Pending"
            └─→ Calls: TryMatchAsync() automatically
                └─→ Attempts to match with other pending requests
                    └─→ If match found: Status becomes "Matched"
                        └─→ Frontend shows "You got matched!" modal again

═══════════════════════════════════════════════════════════════════════════════════
KEY OBJECTS & MODELS
═══════════════════════════════════════════════════════════════════════════════════

BACKEND MODELS:
───────────────────────────────────────────────────────────────────────────────────
ScheduledInterviewSession
├─ Id: Guid
├─ UserId: Guid
├─ InterviewType: string
├─ PracticeType: string
├─ InterviewLevel: string
├─ ScheduledStartAt: DateTime
├─ Status: "Scheduled" | "Cancelled" | "Completed" | "InProgress"
├─ AssignedQuestionId: Guid?
└─ LiveSessionId: Guid?

InterviewMatchingRequest
├─ Id: Guid
├─ UserId: Guid
├─ ScheduledSessionId: Guid
├─ InterviewType: string
├─ PracticeType: string
├─ InterviewLevel: string
├─ ScheduledStartAt: DateTime
├─ Status: "Pending" | "Matched" | "Confirmed" | "Expired" | "Cancelled"
├─ MatchedUserId: Guid?
├─ LiveSessionId: Guid?
├─ UserConfirmed: bool
├─ ExpiresAt: DateTime
├─ CreatedAt: DateTime
└─ UpdatedAt: DateTime

LiveInterviewSession
├─ Id: Guid
├─ ScheduledSessionId: Guid?
├─ FirstQuestionId: Guid?
├─ SecondQuestionId: Guid?
├─ ActiveQuestionId: Guid?
├─ Status: "InProgress" | "Completed" | "Cancelled" | "Aborted"
├─ StartedAt: DateTime?
├─ EndedAt: DateTime?
└─ Participants: List<LiveInterviewParticipant>

LiveInterviewParticipant
├─ Id: Guid
├─ LiveSessionId: Guid
├─ UserId: Guid
├─ Role: "Interviewer" | "Interviewee"
└─ IsActive: bool

FRONTEND OBJECTS:
───────────────────────────────────────────────────────────────────────────────────
ScheduledInterviewSession (DTO)
├─ id: string
├─ userId: string
├─ interviewType: string
├─ practiceType: string
├─ interviewLevel: string
├─ scheduledStartAt: string
├─ status: "Scheduled" | "Cancelled" | "Completed" | "InProgress"
└─ assignedQuestionId?: string

MatchingRequest (DTO)
├─ id: string
├─ userId: string
├─ scheduledSessionId: string
├─ status: "Pending" | "Matched" | "Confirmed" | "Expired" | "Cancelled"
├─ matchedUserId?: string
├─ liveSessionId?: string
├─ userConfirmed: boolean
├─ matchedUserConfirmed: boolean
└─ expiresAt: string

LiveInterviewSession (DTO)
├─ id: string
├─ scheduledSessionId?: string
├─ firstQuestionId?: string
├─ secondQuestionId?: string
├─ activeQuestionId?: string
├─ status: "InProgress" | "Completed" | "Cancelled"
├─ startedAt?: string
└─ participants?: Participant[]

═══════════════════════════════════════════════════════════════════════════════════
KEY METHODS SUMMARY
═══════════════════════════════════════════════════════════════════════════════════

BACKEND (PeerInterviewService):
───────────────────────────────────────────────────────────────────────────────────
ScheduleInterviewSessionAsync(userId, dto)
  → Creates ScheduledInterviewSession
  → Assigns question if data-structures-algorithms
  → Returns ScheduledInterviewSessionDto

StartMatchingAsync(scheduledSessionId, userId)
  → Creates InterviewMatchingRequest (Status: "Pending")
  → Calls TryMatchAsync() immediately
  → Returns StartMatchingResponseDto

GetMatchingStatusAsync(scheduledSessionId, userId)
  → Finds InterviewMatchingRequest
  → If Status == "Pending": Calls TryMatchAsync() automatically
  → Returns MatchingRequestDto

TryMatchAsync(request) [PRIVATE]
  → Finds matching requests in queue (FIFO)
  → Matches based on InterviewType, PracticeType, Date
  → Updates both requests to Status: "Matched"
  → Returns bool (matched or not)

ConfirmMatchAsync(matchingRequestId, userId)
  → Sets UserConfirmed = true
  → Checks if both confirmed
  → If both confirmed: Creates LiveInterviewSession
  → Returns ConfirmMatchResponseDto

ExpireMatchIfNotConfirmedAsync(matchingRequestId, userId)
  → Checks match age >= 15 seconds
  → Sets both requests to Status: "Expired"
  → Creates new "Pending" requests for both users
  → Returns bool

FRONTEND (FindPeerPage.tsx):
───────────────────────────────────────────────────────────────────────────────────
handleScheduleInterview()
  → Calls peerInterviewService.scheduleInterview()
  → Updates UI with new session

handleStartInterview(sessionId)
  → Calls peerInterviewService.startMatching()
  → Shows "Waiting for your partner..." modal
  → Starts polling: startMatchingPoll()

startMatchingPoll(sessionId)
  → Polls every 2 seconds: getMatchingStatus()
  → Handles status changes:
    - "Pending" → Continue waiting
    - "Matched" → Show "You got matched!" modal, start 15s countdown
    - "Confirmed" → Redirect to live session

handleConfirmMatch()
  → Calls peerInterviewService.confirmMatch()
  → Updates UI state

handleMatchTimeout()
  → Calls peerInterviewService.expireMatch()
  → Resets UI state
  → Polling continues, detects new match

FRONTEND (peerInterview.service.ts):
───────────────────────────────────────────────────────────────────────────────────
scheduleInterview(request)
  → POST /api/peer-interviews/scheduled

startMatching(scheduledSessionId)
  → POST /api/peer-interviews/sessions/{id}/start-matching

getMatchingStatus(scheduledSessionId)
  → GET /api/peer-interviews/sessions/{id}/matching-status

confirmMatch(matchingRequestId)
  → POST /api/peer-interviews/matching-requests/{id}/confirm

expireMatch(matchingRequestId)
  → POST /api/peer-interviews/matching-requests/{id}/expire

═══════════════════════════════════════════════════════════════════════════════════
STATUS TRANSITIONS
═══════════════════════════════════════════════════════════════════════════════════

InterviewMatchingRequest Status Flow:
───────────────────────────────────────────────────────────────────────────────────
"Pending" 
  → TryMatchAsync() finds match
  → "Matched"
    → User confirms
    → UserConfirmed = true
    → Other user confirms
    → "Confirmed" + LiveInterviewSession created
  → OR: 15 seconds expire
    → "Expired" + New "Pending" request created
    → TryMatchAsync() called on new request
    → "Matched" (cycle repeats)

LiveInterviewSession Status Flow:
───────────────────────────────────────────────────────────────────────────────────
Created (Status: "InProgress")
  → Both users confirmed
  → Session active
  → User ends interview
  → "Completed"

═══════════════════════════════════════════════════════════════════════════════════
QUEUE MANAGEMENT (FIFO)
═══════════════════════════════════════════════════════════════════════════════════

Matching Queue Logic:
───────────────────────────────────────────────────────────────────────────────────
1. Requests ordered by CreatedAt ASC (oldest first)
2. TryMatchAsync() finds first compatible match
3. After expiration, new requests have CreatedAt = Now
4. New requests go to end of queue (FIFO)
5. GetMatchingStatusAsync() automatically calls TryMatchAsync() for "Pending" requests
6. This ensures automatic rematching without user action

═══════════════════════════════════════════════════════════════════════════════════
