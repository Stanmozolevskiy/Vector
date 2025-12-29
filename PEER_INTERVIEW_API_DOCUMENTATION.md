# Peer Interview API Documentation

## Overview

The Peer Interview API enables users to schedule, match, and conduct live peer interview sessions. The system supports scheduling interviews, matching users based on preferences, conducting live sessions with role switching, and providing feedback.

## Base URL

```
/api/peer-interviews
```

All endpoints require authentication via JWT Bearer token.

## Endpoints

### Scheduling

#### POST /api/peer-interviews/scheduled

Schedule a new interview session.

**Request Body:**
```json
{
  "interviewType": "data-structures-algorithms",
  "practiceType": "peers",
  "interviewLevel": "beginner",
  "scheduledStartAt": "2024-12-29T14:00:00Z"
}
```

**Response:** `201 Created`
```json
{
  "id": "guid",
  "userId": "guid",
  "interviewType": "data-structures-algorithms",
  "practiceType": "peers",
  "interviewLevel": "beginner",
  "scheduledStartAt": "2024-12-29T14:00:00Z",
  "status": "Scheduled",
  "liveSessionId": null,
  "createdAt": "2024-12-28T12:00:00Z",
  "updatedAt": "2024-12-28T12:00:00Z",
  "user": { ... },
  "liveSession": null
}
```

#### GET /api/peer-interviews/scheduled/upcoming

Get upcoming scheduled sessions for the current user.

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "userId": "guid",
    "interviewType": "data-structures-algorithms",
    "practiceType": "peers",
    "interviewLevel": "beginner",
    "scheduledStartAt": "2024-12-29T14:00:00Z",
    "status": "Scheduled",
    ...
  }
]
```

#### GET /api/peer-interviews/scheduled/{sessionId}

Get a specific scheduled session by ID.

**Response:** `200 OK` or `404 Not Found`

#### POST /api/peer-interviews/scheduled/{sessionId}/cancel

Cancel a scheduled session.

**Response:** `200 OK` or `404 Not Found`

### Matching

#### POST /api/peer-interviews/sessions/{sessionId}/start-matching

Start the matching process for a scheduled session. This creates a matching request and attempts to find a match immediately.

**Parameters:**
- `sessionId` - The scheduled session ID

**Response:** `200 OK`
```json
{
  "matchingRequest": {
    "id": "guid",
    "userId": "guid",
    "scheduledSessionId": "guid",
    "interviewType": "data-structures-algorithms",
    "practiceType": "peers",
    "interviewLevel": "beginner",
    "status": "Pending" | "Matched",
    "matchedUserId": "guid" | null,
    "expiresAt": "2024-12-28T12:10:00Z",
    ...
  },
  "matched": true | false,
  "sessionComplete": false,
  "session": null | { ... }
}
```

#### GET /api/peer-interviews/sessions/{sessionId}/matching-status

Get the current matching status for a scheduled session.

**Response:** `200 OK` or `404 Not Found`
```json
{
  "id": "guid",
  "status": "Pending" | "Matched" | "Confirmed" | "Expired",
  "matchedUserId": "guid" | null,
  "userConfirmed": false,
  "matchedUserConfirmed": false,
  "expiresAt": "2024-12-28T12:10:00Z",
  ...
}
```

#### POST /api/peer-interviews/matching-requests/{matchingRequestId}/confirm

Confirm readiness for a matched interview. Both users must confirm before the live session is created.

**Response:** `200 OK`
```json
{
  "matchingRequest": { ... },
  "completed": true | false,
  "session": { ... } | null
}
```

### Live Sessions

#### GET /api/peer-interviews/sessions/{sessionId}

Get a live interview session by ID. Returns role-aware session data.

**Response:** `200 OK` or `404 Not Found`
```json
{
  "id": "guid",
  "scheduledSessionId": "guid",
  "firstQuestionId": "guid",
  "secondQuestionId": "guid",
  "activeQuestionId": "guid",
  "status": "InProgress" | "Completed" | "Cancelled",
  "startedAt": "2024-12-28T12:00:00Z",
  "endedAt": null,
  "firstQuestion": { ... },
  "secondQuestion": { ... },
  "activeQuestion": { ... },
  "participants": [
    {
      "id": "guid",
      "userId": "guid",
      "role": "Interviewer" | "Interviewee",
      "isActive": true,
      "user": { ... }
    }
  ]
}
```

#### POST /api/peer-interviews/sessions/{sessionId}/switch-roles

Switch roles between interviewer and interviewee. Either participant can initiate.

**Response:** `200 OK`
```json
{
  "session": { ... },
  "yourNewRole": "Interviewer" | "Interviewee",
  "partnerNewRole": "Interviewee" | "Interviewer"
}
```

#### POST /api/peer-interviews/sessions/{sessionId}/change-question

Change the active question in a session. Only the interviewer can change questions.

**Request Body (Optional):**
```json
{
  "questionId": "guid"  // Optional - if not provided, a random question is selected
}
```

**Response:** `200 OK`
```json
{
  "session": { ... },
  "newActiveQuestion": {
    "id": "guid",
    "title": "Two Sum",
    "difficulty": "Easy",
    "questionType": "Coding"
  }
}
```

#### POST /api/peer-interviews/sessions/{sessionId}/end

End an interview session. Either participant can end the session.

**Response:** `200 OK`
```json
{
  "id": "guid",
  "status": "Completed",
  "endedAt": "2024-12-28T13:00:00Z",
  ...
}
```

### Feedback

#### POST /api/peer-interviews/feedback

Submit feedback for a session participant.

**Request Body:**
```json
{
  "liveSessionId": "guid",
  "revieweeId": "guid",
  "problemSolvingRating": 4,
  "problemSolvingDescription": "Excellent problem-solving approach",
  "codingSkillsRating": 5,
  "codingSkillsDescription": "Strong coding skills",
  "communicationRating": 4,
  "communicationDescription": "Clear communication",
  "thingsDidWell": "Great use of data structures",
  "areasForImprovement": "Could improve time complexity",
  "interviewerPerformanceRating": 5,
  "interviewerPerformanceDescription": "Very helpful interviewer"
}
```

**Response:** `201 Created`

#### GET /api/peer-interviews/sessions/{sessionId}/feedback

Get all feedback for a session.

**Response:** `200 OK`
```json
[
  {
    "id": "guid",
    "liveSessionId": "guid",
    "reviewerId": "guid",
    "revieweeId": "guid",
    "problemSolvingRating": 4,
    ...
    "reviewer": { ... },
    "reviewee": { ... }
  }
]
```

#### GET /api/peer-interviews/feedback/{feedbackId}

Get a specific feedback by ID.

**Response:** `200 OK` or `404 Not Found`

## Data Models

### Interview Types
- `data-structures-algorithms`
- `system-design`
- `behavioral`
- `product-management`
- `sql`
- `data-science-ml`

### Practice Types
- `peers` - Practice with peers (free)
- `friend` - Practice with a friend
- `expert` - Expert mock interview

### Interview Levels
- `beginner`
- `intermediate`
- `advanced`

### Session Statuses
- `Scheduled` - Session is scheduled but not started
- `InProgress` - Live session is active
- `Completed` - Session has ended
- `Cancelled` - Session was cancelled

### Matching Request Statuses
- `Pending` - Waiting for a match
- `Matched` - Match found, waiting for confirmations
- `Confirmed` - Both users confirmed, live session created
- `Expired` - Matching request expired (10 minutes)
- `Cancelled` - Matching request was cancelled

## Matching Algorithm

The system uses a FIFO (First In, First Out) matching algorithm:

1. **Hard Matches (Required):**
   - Interview type must match exactly
   - Practice type must match exactly
   - Scheduled date must be the same day

2. **Soft Matches (Preferred):**
   - Interview level should match if possible, but any level is acceptable

3. **Expiration:**
   - Matching requests expire after 10 minutes
   - Expired requests are automatically cleaned up

4. **Matching Process:**
   - When a user clicks "Start Interview", a matching request is created
   - The system immediately searches for compatible pending requests
   - If a match is found, both requests are updated with matched user IDs
   - Both users receive a notification to confirm readiness
   - Once both users confirm, a live session is created

## Role-Based Permissions

### Interviewer
- Can view hints and solutions
- Can change the active question
- Can see all test cases

### Interviewee
- Cannot view hints and solutions
- Cannot change questions
- Can see public test cases only

## Frontend Integration

The frontend service (`peerInterview.service.ts`) provides methods that map to these endpoints:

```typescript
// Schedule an interview
await peerInterviewService.scheduleInterview({
  interviewType: 'data-structures-algorithms',
  practiceType: 'peers',
  interviewLevel: 'beginner',
  scheduledStartAt: '2024-12-29T14:00:00Z'
});

// Get upcoming sessions
const sessions = await peerInterviewService.getUpcomingSessions();

// Start matching
const result = await peerInterviewService.startMatching(scheduledSessionId);

// Confirm match
const confirmed = await peerInterviewService.confirmMatch(matchingRequestId);

// Get live session
const session = await peerInterviewService.getSession(sessionId);

// Switch roles
const switched = await peerInterviewService.switchRoles(sessionId);

// Change question
const changed = await peerInterviewService.changeQuestion(sessionId, questionId);

// End interview
const ended = await peerInterviewService.endInterview(sessionId);

// Submit feedback
await peerInterviewService.submitFeedback({
  liveSessionId: sessionId,
  revieweeId: partnerId,
  // ... feedback details
});
```

## Error Responses

All endpoints may return standard HTTP error codes:

- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - User not authorized for this operation
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

Error response format:
```json
{
  "message": "Error description",
  "error": "Error details (development only)"
}
```

## Testing

### Manual Testing Flow

1. **Schedule Interview:**
   ```bash
   POST /api/peer-interviews/scheduled
   ```

2. **Get Upcoming Sessions:**
   ```bash
   GET /api/peer-interviews/scheduled/upcoming
   ```

3. **Start Matching (as User 1):**
   ```bash
   POST /api/peer-interviews/sessions/{sessionId}/start-matching
   ```

4. **Start Matching (as User 2 with compatible preferences):**
   ```bash
   POST /api/peer-interviews/sessions/{sessionId2}/start-matching
   ```

5. **Confirm Match (User 1):**
   ```bash
   POST /api/peer-interviews/matching-requests/{matchingRequestId}/confirm
   ```

6. **Confirm Match (User 2):**
   ```bash
   POST /api/peer-interviews/matching-requests/{matchingRequestId}/confirm
   ```

7. **Get Live Session:**
   ```bash
   GET /api/peer-interviews/sessions/{sessionId}
   ```

8. **Switch Roles:**
   ```bash
   POST /api/peer-interviews/sessions/{sessionId}/switch-roles
   ```

9. **Change Question:**
   ```bash
   POST /api/peer-interviews/sessions/{sessionId}/change-question
   ```

10. **End Interview:**
    ```bash
    POST /api/peer-interviews/sessions/{sessionId}/end
    ```

11. **Submit Feedback:**
    ```bash
    POST /api/peer-interviews/feedback
    ```

## Deployment Notes

- All database migrations must be applied before deployment
- The service is registered in `Program.cs` as `IPeerInterviewService`
- Frontend service provides backward compatibility with legacy methods
- Matching requests expire after 10 minutes (cleanup job pending)

