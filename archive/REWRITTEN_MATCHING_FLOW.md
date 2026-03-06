# Rewritten Matching and Confirmation Flow

## Overview
The matching and confirmation system has been completely rewritten to be simpler and more reliable.

## New Flow

### 1. User Schedules Interview
- User creates a scheduled interview with specific time and interview type
- **Status:** `Scheduled`
- **Already implemented** ✅

### 2. User Clicks "Start Interview" → Joins Queue
**Backend:** `StartMatchingAsync`
- Creates `InterviewMatchingRequest` with `Status = "Pending"`
- User is now in the matching queue
- Immediately tries to match with another user
- **Returns:** Matching request (Pending or Matched)

### 3. System Matches 2 Users → Creates Live Session IMMEDIATELY
**Backend:** `TryMatchAsync` → `CreateLiveSessionForMatchAsync`
- When 2 users match:
  - **IMMEDIATELY creates `LiveInterviewSession`** with:
    - 2 questions (FirstQuestionId, SecondQuestionId)
    - ActiveQuestionId = FirstQuestionId
    - Status = "Pending" (will change to "InProgress" when both confirm)
  - **Creates `LiveInterviewParticipant` records:**
    - Scheduler (older CreatedAt) = "Interviewer"
    - Matched user = "Interviewee"
  - **Updates both matching requests:**
    - Status = "Matched"
    - LiveSessionId = [new session ID]
    - MatchedUserId = [other user's ID]
- **Returns:** `true` if matched, `false` if still waiting

### 4. Frontend Detects Match → Shows Confirmation Popup
**Frontend:** `startMatchingPoll` polling
- Polls every 2 seconds
- Detects match when: `status === 'Matched' && liveSessionId != null`
- Shows "You got matched!" popup
- Starts 15-second countdown timer

### 5. User Confirms Readiness
**Backend:** `ConfirmMatchAsync` (SIMPLIFIED)
- User confirms on their OWN matching request
- Sets `UserConfirmed = true` on their request
- Checks if OTHER user has confirmed on THEIR request
- If both confirmed:
  - Updates live session: `Status = "InProgress"`, `StartedAt = DateTime.UtcNow`
  - Updates both matching requests: `Status = "Confirmed"`
- **Returns:** `Completed = true` if both confirmed, `false` if only one confirmed

### 6. Both Users Confirmed → Redirect
**Frontend:** Polling detects `status === 'Confirmed'` OR both `userConfirmed && matchedUserConfirmed`
- Stops polling
- Gets live session using `liveSessionId`
- Redirects to: `/questions/{questionId}?session={liveSessionId}`

### 7. 15-Second Timeout Handling
**If one user doesn't confirm within 15 seconds:**
- Frontend calls `expireMatch(matchingRequestId)`
- Backend `ExpireMatchIfNotConfirmedAsync`:
  - Deletes the live session
  - Sets both matching requests back to `Status = "Pending"`
  - Clears `MatchedUserId`, `LiveSessionId`, confirmation flags
  - Re-queues both users
  - Tries to match them again immediately

**If both users don't confirm:**
- Same as above - both users go back to queue

## Key Changes from Previous Implementation

1. **Live Session Created IMMEDIATELY** when match is found (before confirmation)
2. **Simpler Confirmation Logic** - just check if both users confirmed
3. **15-Second Timeout** - expires match and re-queues users
4. **No Complex Status Tracking** - just Pending → Matched → Confirmed
5. **Frontend Polling** - checks for `liveSessionId` presence to detect match

## Database State Transitions

### InterviewMatchingRequest
- `Pending` → User in queue
- `Matched` → Match found, live session created, waiting for confirmation
- `Confirmed` → Both users confirmed, session started

### LiveInterviewSession
- `Pending` → Created when match found, waiting for both confirmations
- `InProgress` → Both users confirmed, interview started
- `Completed` → Interview ended

## API Endpoints

1. `POST /api/peer-interviews/sessions/{sessionId}/start-matching` - Join queue
2. `GET /api/peer-interviews/sessions/{sessionId}/matching-status` - Get status
3. `POST /api/peer-interviews/matching-requests/{matchingRequestId}/confirm` - Confirm readiness
4. `POST /api/peer-interviews/matching-requests/{matchingRequestId}/expire` - Expire match (timeout)
5. `GET /api/peer-interviews/sessions/{sessionId}` - Get live session

