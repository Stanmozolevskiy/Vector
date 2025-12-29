# Confirmation and Redirect Flow - Step by Step

## Overview
This document explains the complete flow from when a user clicks "Join your interview" to when both users are redirected to the live session page.

---

## FRONTEND FLOW

### Step 1: User Clicks "Join your interview" Button
**Location:** `FindPeerPage.tsx` line 1614
```typescript
onClick={() => {
  console.log('Join button clicked');
  handleConfirmMatch();
}}
```
**What happens:**
- User clicks the green "✓ You're ready!" button in the matching modal
- Triggers `handleConfirmMatch()` function

---

### Step 2: Frontend - handleConfirmMatch Function
**Location:** `FindPeerPage.tsx` lines 775-920

**Step 2.1: Validation (lines 776-779)**
```typescript
if (!matchingStatus?.id) {
  console.error('Cannot confirm match: matchingStatus.id is missing');
  return;
}
```
- Checks if matching status ID exists
- If missing, logs error and returns

**Step 2.2: Clear Timers (lines 781-787)**
```typescript
if (confirmationTimeout) {
  clearTimeout(confirmationTimeout);
  setConfirmationTimeout(null);
}
setConfirmationCountdown(null);
setMatchStartTime(null);
```
- Clears the 15-second confirmation countdown timer
- Resets countdown state

**Step 2.3: Set Loading State (line 789)**
```typescript
setIsConfirmingMatch(true);
```
- Shows loading spinner on the button

**Step 2.4: Call Backend API (line 791)**
```typescript
const result = await peerInterviewService.confirmMatch(matchingStatus.id);
console.log('Confirm match result:', result);
```
- Calls `peerInterviewService.confirmMatch()` with the matching request ID
- This sends a POST request to `/api/peer-interviews/matching/{matchingRequestId}/confirm`

**Step 2.5: Update Local State (line 795)**
```typescript
setMatchingStatus(result.matchingRequest);
```
- Updates the local matching status with the response from backend

**Step 2.6: Check if Both Users Confirmed (lines 798-843)**
```typescript
if (result.completed && result.session) {
  // Both users confirmed, session is ready - navigate immediately
  // ... redirect logic
}
```
- If `result.completed === true` AND `result.session` exists:
  - Both users have confirmed
  - Live session has been created
  - Redirects immediately to the question page

**Step 2.7: Check if Both Confirmed but Session Not in Response (lines 844-870)**
```typescript
else if (result.completed && result.matchingRequest?.liveSessionId) {
  // Both users confirmed but session not in response, get it using liveSessionId
  // ... fetch session and redirect
}
```
- If `result.completed === true` but `result.session` is null:
  - Both users confirmed
  - Live session exists (has `liveSessionId`)
  - Fetches the session and redirects

**Step 2.8: Only One User Confirmed (lines 871-920)**
```typescript
else {
  // Only this user confirmed - waiting for other user
  // ... continue polling
}
```
- If `result.completed === false`:
  - Only the current user has confirmed
  - Other user hasn't confirmed yet
  - Continues polling to wait for the other user

---

## BACKEND FLOW

### Step 3: Backend - ConfirmMatchAsync Method
**Location:** `PeerInterviewService.cs` lines 191-506

**Step 3.1: Find Matching Request (lines 195-199)**
```csharp
var matchingRequest = await _context.InterviewMatchingRequests
    .Include(m => m.User)
    .Include(m => m.MatchedUser)
    .Include(m => m.ScheduledSession)
    .FirstOrDefaultAsync(m => m.Id == matchingRequestId);
```
- Loads the matching request from database
- Includes related entities (User, MatchedUser, ScheduledSession)

**Step 3.2: Validate Request Exists (lines 201-205)**
```csharp
if (matchingRequest == null)
{
    throw new KeyNotFoundException("Matching request not found.");
}
```
- If request doesn't exist, throws exception

**Step 3.3: Validate Status (lines 210-215)**
```csharp
if (matchingRequest.Status != "Matched" && matchingRequest.Status != "Confirmed")
{
    throw new InvalidOperationException("Cannot confirm a match that hasn't been matched yet.");
}
```
- Only allows confirmation if status is "Matched" or "Confirmed"

**Step 3.4: Determine Which User is Confirming (lines 217-226)**
```csharp
bool isRequestingUser = matchingRequest.UserId == userId;
bool isMatchedUser = matchingRequest.MatchedUserId.HasValue && matchingRequest.MatchedUserId.Value == userId;
```
- Checks if the current user is:
  - The requesting user (scheduler)
  - OR the matched user

**Step 3.5: Update Confirmation Status (lines 228-292)**

**If requesting user (lines 232-244):**
```csharp
if (isRequestingUser)
{
    if (matchingRequest.UserConfirmed)
    {
        // Already confirmed
        wasAlreadyConfirmed = true;
    }
    else
    {
        matchingRequest.UserConfirmed = true;
        // User confirmed on their own request
    }
}
```

**If matched user (lines 246-291):**
```csharp
else
{
    // Find the matched user's OWN matching request
    var userOwnRequest = await _context.InterviewMatchingRequests
        .FirstOrDefaultAsync(m => m.UserId == userId 
            && m.MatchedUserId == matchingRequest.UserId 
            && m.Status == "Matched");  // ⚠️ ISSUE: Only checks "Matched" status!
    
    if (userOwnRequest != null)
    {
        userOwnRequest.UserConfirmed = true;
        matchingRequest = userOwnRequest; // Switch to user's own request
    }
}
```
**⚠️ ISSUE FOUND:** Line 256 only checks for `Status == "Matched"`, but should also check for `Status == "Confirmed"`!

**Step 3.6: Check if Both Users Confirmed (lines 309-364)**

**If requesting user (lines 313-333):**
```csharp
if (isRequestingUser && matchingRequest.MatchedUserId.HasValue)
{
    // Find the matched user's request
    var matchedUserRequest = await _context.InterviewMatchingRequests
        .FirstOrDefaultAsync(m => m.UserId == matchingRequest.MatchedUserId.Value 
            && m.MatchedUserId == matchingRequest.UserId 
            && (m.Status == "Matched" || m.Status == "Confirmed"));
    
    if (matchedUserRequest != null)
    {
        bothConfirmed = matchingRequest.UserConfirmed && matchedUserRequest.UserConfirmed;
    }
}
```

**If matched user (lines 335-356):**
```csharp
else if (!isRequestingUser && matchingRequest.MatchedUserId.HasValue)
{
    // Find the other user's request
    var otherUserRequest = await _context.InterviewMatchingRequests
        .FirstOrDefaultAsync(m => m.UserId == matchingRequest.MatchedUserId.Value 
            && m.MatchedUserId == matchingRequest.UserId 
            && (m.Status == "Matched" || m.Status == "Confirmed"));
    
    if (otherUserRequest != null)
    {
        bothConfirmed = matchingRequest.UserConfirmed && otherUserRequest.UserConfirmed;
    }
}
```

**Step 3.7: Create Live Session if Both Confirmed (lines 366-468)**
```csharp
if (bothConfirmed && !matchingRequest.LiveSessionId.HasValue)
{
    // Create live session
    var liveSession = await CreateLiveSessionAsync(sessionCreatorRequest);
    matchingRequest.LiveSessionId = liveSession.Id;
    
    // Update both matching requests with the same live session ID
    otherUserRequest.LiveSessionId = liveSession.Id;
    otherUserRequest.Status = "Confirmed";
}
```
- If both users confirmed AND no live session exists:
  - Creates a new `LiveInterviewSession`
  - Assigns questions to the session
  - Creates `LiveInterviewParticipant` records (Interviewer and Interviewee)
  - Updates BOTH matching requests with the same `LiveSessionId`
  - Updates status to "Confirmed" on both requests

**Step 3.8: Save Changes (line 470)**
```csharp
await _context.SaveChangesAsync();
```
- Saves all changes to database

**Step 3.9: Return Response (lines 500-505)**
```csharp
return new ConfirmMatchResponseDto
{
    MatchingRequest = await MapToMatchingRequestDtoAsync(matchingRequest),
    Completed = bothConfirmed,
    Session = liveSessionDto
};
```
- Returns the updated matching request
- `Completed = true` if both users confirmed
- `Session` contains the live session if it was created

---

## POLLING FLOW (Background)

### Step 4: Polling Function - startMatchingPoll
**Location:** `FindPeerPage.tsx` lines 484-682

**Step 4.1: Poll Every 2 Seconds (line 508)**
```typescript
const interval = setInterval(async () => {
  const status = await peerInterviewService.getMatchingStatus(sessionId);
  // ... check status
}, 2000);
```
- Polls the backend every 2 seconds to check matching status

**Step 4.2: Check if Both Confirmed (lines 537-544)**
```typescript
const bothConfirmed = status?.status === 'Confirmed' || (status?.liveSessionId != null);
```
- Checks if:
  - Status is "Confirmed", OR
  - `liveSessionId` is present (indicates live session was created)

**Step 4.3: Redirect if Both Confirmed (lines 546-640)**
```typescript
if (bothConfirmed) {
  // Clear intervals
  // Get liveSessionId
  // Fetch live session
  // Redirect to question page
}
```
- If both confirmed:
  - Stops polling
  - Gets the live session
  - Redirects to `/questions/{questionId}?session={liveSessionId}`

---

## THE PROBLEM

Based on the console log:
```
Status: Matched
UserConfirmed: true
MatchedUserConfirmed: false
LiveSessionId: null
BothConfirmed: false
```

**Root Cause:**
1. When the matched user confirms, the backend tries to find their own matching request (line 256)
2. The query only checks for `Status == "Matched"`, but doesn't check for `Status == "Confirmed"`
3. If the other user already confirmed and the status changed, the query might fail
4. The `bothConfirmed` check might not be working correctly because it can't find the other user's request

**Fix Required:**
- Update line 256 to also check for `Status == "Confirmed"`
- Ensure the `bothConfirmed` logic correctly finds both matching requests

