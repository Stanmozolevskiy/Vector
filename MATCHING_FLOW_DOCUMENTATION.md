# Peer Interview Matching Flow - Step by Step

## Overview
This document describes the complete flow of scheduling and accepting a peer interview session, including the matching process.

## Step-by-Step Process

### Phase 1: Scheduling (Both Users)

#### Step 1: User 1 Schedules Interview
1. User 1 navigates to `/peer-interviews/find`
2. Clicks "Schedule peer mock interview"
3. Selects:
   - Interview Type (e.g., "data-structures-algorithms")
   - Practice Type (e.g., "Practice with peers")
   - Interview Level (e.g., "Beginner")
   - Date and Time
4. Frontend calls: `POST /api/peer-interviews/sessions`
5. Backend creates `PeerInterviewSession`:
   - `InterviewerId` = User 1's ID
   - `IntervieweeId` = NULL (not assigned yet)
   - `Status` = "Scheduled"
   - `QuestionId` = Auto-assigned based on interview level
   - `InterviewType`, `InterviewLevel`, `ScheduledTime` set
6. Session appears in User 1's "Upcoming interviews" list

#### Step 2: User 2 Schedules Interview
1. User 2 navigates to `/peer-interviews/find`
2. Clicks "Schedule peer mock interview"
3. Selects:
   - Interview Type (e.g., "data-structures-algorithms") - **MUST MATCH User 1's type**
   - Practice Type (e.g., "Practice with peers")
   - Interview Level (e.g., "Beginner") - **Flexible, can differ**
   - Date and Time
4. Frontend calls: `POST /api/peer-interviews/sessions`
5. Backend creates `PeerInterviewSession`:
   - `InterviewerId` = User 2's ID
   - `IntervieweeId` = NULL (not assigned yet)
   - `Status` = "Scheduled"
   - `QuestionId` = Auto-assigned based on interview level
   - `InterviewType`, `InterviewLevel`, `ScheduledTime` set
6. Session appears in User 2's "Upcoming interviews" list

### Phase 2: Matching (Both Users Start Interview)

#### Step 3: User 1 Clicks "Start Interview"
1. Frontend: `handleStartInterview(sessionId)` called
2. Frontend checks if session has `intervieweeId`:
   - If yes → Navigate directly to question page
   - If no → Continue to matching
3. Frontend calls: `GET /api/peer-interviews/sessions/{id}/matching-status`
   - Checks if there's already a matching request
   - If status is "Matched" → Show confirmation UI
4. Frontend calls: `POST /api/peer-interviews/sessions/{id}/start-matching`
5. Backend `StartMatching` endpoint:
   - Checks if session already has `intervieweeId` → Returns `sessionComplete: true`
   - Calls `CreateMatchingRequestAsync(sessionId, userId)`:
     - Verifies session exists and user is interviewer
     - Checks if session has `intervieweeId` → Throws exception
     - Checks for existing request (Pending or Matched) → Returns it
     - Creates new `InterviewMatchingRequest`:
       - `UserId` = User 1's ID
       - `ScheduledSessionId` = User 1's session ID
       - `Status` = "Pending"
       - `ExpiresAt` = Now + 5 minutes
   - Calls `FindMatchingPeerAsync(userId, sessionId)`:
     - Checks if user already has matched request → Returns it
     - Searches for other pending requests with:
       - Same `InterviewType` (REQUIRED)
       - Same `InterviewLevel` (preferred, but flexible)
       - `ExpiresAt` > Now
       - `UserId` != current user
     - If found:
       - Creates/gets User 1's request
       - Links the two requests:
         - User 1's request: `MatchedUserId` = User 2's ID, `MatchedRequestId` = User 2's request ID
         - User 2's request: `MatchedUserId` = User 1's ID, `MatchedRequestId` = User 1's request ID
       - Sets both requests to `Status` = "Matched"
     - Returns matched request
6. Frontend receives response:
   - If `matched: true` → Shows "Match Found!" modal
   - Starts polling: `startMatchingPoll(sessionId)` every 2 seconds

#### Step 4: User 2 Clicks "Start Interview" (THE PROBLEM)
1. Frontend: `handleStartInterview(sessionId)` called
2. Frontend checks if session has `intervieweeId`:
   - If yes → Navigate directly
   - If no → Continue to matching
3. Frontend calls: `GET /api/peer-interviews/sessions/{id}/matching-status`
   - **PROBLEM**: This endpoint checks if user is the interviewer of the session
   - If User 2's session is matched, the matching request might be on User 1's session
   - The endpoint only looks for requests where `ScheduledSessionId == id`
   - It doesn't check if the user is the `MatchedUserId` in another request
4. Frontend calls: `POST /api/peer-interviews/sessions/{id}/start-matching`
5. Backend `StartMatching` endpoint:
   - Calls `CreateMatchingRequestAsync(sessionId, userId)`:
     - Checks for existing request → Finds User 2's request (Status = "Matched")
     - Returns the matched request
   - Calls `FindMatchingPeerAsync(userId, sessionId)`:
     - Checks if user already has matched request → Returns it
   - Returns matched request
6. Frontend receives response and shows "Match Found!" modal

### Phase 3: Confirmation (Both Users)

#### Step 5: User 1 Confirms Match
1. User 1 clicks "Confirm & Start Interview" button
2. Frontend calls: `POST /api/peer-interviews/matching-requests/{id}/confirm`
3. Backend `ConfirmMatch` endpoint:
   - Calls `ConfirmMatchAsync(matchingRequestId, userId)`:
     - Finds the matching request
     - Verifies user is part of the match (userId or matchedUserId)
     - Sets `UserConfirmed = true` for User 1
     - Also updates the matched request's confirmation status
   - Reloads request to check if both confirmed
   - If both confirmed:
     - Calls `CompleteMatchAsync(matchingRequestId)`:
       - Finds both sessions (session1 and session2)
       - Sets `session1.IntervieweeId = session2.InterviewerId` (User 2 becomes interviewee)
       - Sets `session1.Status = "InProgress"`
       - Sets `session2.Status = "Cancelled"`
       - Sets both matching requests to `Status = "Confirmed"`
       - Returns session1 (the primary session)
     - Returns response with `completed: true` and session data
4. Frontend receives response:
   - If `completed: true` → Navigates to question page
   - If not → Updates UI to show "Waiting for peer to confirm"

#### Step 6: User 2 Confirms Match
1. User 2 clicks "Confirm & Start Interview" button
2. Frontend calls: `POST /api/peer-interviews/matching-requests/{id}/confirm`
3. Backend `ConfirmMatch` endpoint:
   - Same process as Step 5
   - When both confirmed, completes the match
4. Frontend receives response:
   - If `completed: true` → Navigates to question page using `window.location.href`
   - Both users should now be on the same question page with `?session={session1Id}`

### Phase 4: Interview Session

#### Step 7: Both Users in Session
1. Both users are on the question detail page
2. URL: `/questions/{questionId}?session={session1Id}`
3. Frontend checks for active session:
   - Calls `GET /api/peer-interviews/sessions/{sessionId}`
   - Verifies user is either interviewer or interviewee
4. Collaborative features enabled:
   - Shared code editor (SignalR)
   - Video chat (WebRTC)
   - Role indicators
   - Switch roles functionality
   - Change question (interviewer only)

## Current Issues

### Issue 1: GetMatchingStatus for Second User
**Problem**: When User 2 clicks "Start interview" after User 1 has already matched, the `GetMatchingStatus` endpoint fails because:
- It only checks requests where `ScheduledSessionId == sessionId`
- It doesn't check if the user is the `MatchedUserId` in another request
- It requires the user to be the interviewer, but User 2 might need to check status for a request where they are the matched user

**Solution**: Update `GetMatchingStatus` to:
1. Check requests where this session is the ScheduledSession
2. Check requests where this session's interviewer is the MatchedUserId
3. Allow access if user is either the interviewer or the matched user

### Issue 2: Network Error on Second User
**Problem**: When User 2 tries to start matching, they might get a network error because:
- The matching request might be on User 1's session
- The endpoint might return 403 Forbid if User 2 is not the interviewer
- The frontend might not handle the case where User 2 is the matched user

**Solution**: 
1. Fix `GetMatchingStatus` to handle both scenarios
2. Update frontend to handle matched user scenario
3. Add better error handling and logging

## API Endpoints Used

1. `POST /api/peer-interviews/sessions` - Create session
2. `GET /api/peer-interviews/sessions/{id}` - Get session details
3. `POST /api/peer-interviews/sessions/{id}/start-matching` - Start matching process
4. `GET /api/peer-interviews/sessions/{id}/matching-status` - Get matching status
5. `POST /api/peer-interviews/matching-requests/{id}/confirm` - Confirm match

## Database Tables

- `PeerInterviewSessions` - Stores interview sessions
- `InterviewMatchingRequests` - Stores matching requests and their status




