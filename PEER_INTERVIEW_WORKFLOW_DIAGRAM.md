# Peer Interview Scheduling, Matching & Rematching Workflow

## Complete Workflow Diagram

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


Matching Queue Logic:
───────────────────────────────────────────────────────────────────────────────────
1. Requests ordered by CreatedAt ASC (oldest first)
2. TryMatchAsync() finds first compatible match
3. After expiration, new requests have CreatedAt = Now
4. New requests go to end of queue (FIFO)
5. GetMatchingStatusAsync() automatically calls TryMatchAsync() for "Pending" requests
6. This ensures automatic rematching without user action

═══════════════════════════════════════════════════════════════════════════════════
