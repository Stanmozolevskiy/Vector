# Whiteboard Role Switch Logic - System Design Interview

## How It Works

### 1. Initial Setup
**When system design interview starts:**

1. Live session is created with two users assigned roles:
   - User A → Interviewer
   - User B → Interviewee

2. Two separate whiteboards are created (one per user):
   - **User A's whiteboard**: `UserId = UserA.id, SessionId = sessionId, QuestionId = NULL`
   - **User B's whiteboard**: `UserId = UserB.id, SessionId = sessionId, QuestionId = NULL`

3. **Initial active board**: Both users see the **INTERVIEWEE's whiteboard** (User B's board)
   - This is the board that will be actively edited during the interview

### 2. Display Logic
**File**: `SystemDesignInterviewPage.tsx` → `loadWhiteboardData()`

```typescript
// Always load the INTERVIEWEE's whiteboard
const intervieweeId = sessionToUse.intervieweeId;
const data = await whiteboardService.getWhiteboardData(sessionQuestionId, intervieweeId);
```

- **Both users always see the same board**: The INTERVIEWEE's whiteboard
- Initially: Both see User B's (interviewee's) board
- After role switch: Both see the NEW interviewee's board (which may be empty/clear)

### 3. Saving Logic
**File**: `SystemDesignInterviewPage.tsx` → `handleSaveWhiteboard()`

```typescript
// Save to CURRENT USER's own whiteboard (not the displayed board)
whiteboardService.saveWhiteboardData({...}, user.id);
```

- **Each user saves to their OWN whiteboard** (not the displayed board)
- User A edits → saves to User A's whiteboard
- User B edits → saves to User B's whiteboard
- This allows users to come back later and see their own implementation

### 4. Real-time Collaboration
**File**: `SystemDesignInterviewPage.tsx` → SignalR `WhiteboardUpdate` listener

- Only the **current interviewee** broadcasts updates when they edit
- Other users (interviewer) receive updates and see changes in real-time
- Updates only apply when viewing the interviewee's board

### 5. Role Switch Flow
**File**: `SystemDesignInterviewPage.tsx` → `handleSwitchRole()`

1. User clicks "Switch Roles" button
2. Backend swaps `interviewerId` and `intervieweeId` in the session
3. Both users reload to see the **NEW INTERVIEWEE's whiteboard**
4. The new interviewee's board may be empty/clear initially
5. Both users can now edit the new interviewee's board

**Example**:
- **Before switch**: User A (interviewer), User B (interviewee)
  - Both see: User B's whiteboard
- **After switch**: User A (interviewee), User B (interviewer)
  - Both see: User A's whiteboard (which may be empty initially)

### 6. Board Persistence
- Each user's whiteboard is saved separately per session
- Users can come back later and see their own implementation
- Boards are identified by: `UserId + SessionId`

## Backend Storage

**Table**: `WhiteboardData`
- `UserId`: The owner of the whiteboard (each user has their own)
- `SessionId`: The interview session ID
- `QuestionId`: NULL (for session-based whiteboards)

**Storage Pattern**:
- User A's whiteboard: `UserId = UserA.id, SessionId = sessionId, QuestionId = NULL`
- User B's whiteboard: `UserId = UserB.id, SessionId = sessionId, QuestionId = NULL`

## Key Points

1. **Two separate whiteboards**: One per user, stored independently
2. **Active board is always the interviewee's**: Both users see the same board (interviewee's)
3. **Saving is to user's own board**: Each user saves to their own whiteboard for persistence
4. **Role switch changes the active board**: After switch, both see the new interviewee's board
5. **New board may be empty**: When roles switch, the new interviewee's board might be clear/empty initially
