# Next Steps: Peer Interview System Implementation

## Current Status ✅

### Completed
- ✅ Day 13-14: Peer Matching & Session Management (Backend + Frontend)
- ✅ Day 15-16: Real-time Collaboration & Video (Frontend UI Complete)
- ✅ Unit Tests: 73 comprehensive tests (all passing)

### Completed
- ✅ Day 15-16: Backend WebSocket/SignalR (COMPLETED)
- ✅ Day 15-16: WebRTC Signaling Server (COMPLETED)
- ✅ Role-based tab visibility (Interviewee cannot see Hints/Solution)

## Testing Guide

See `TESTING_PEER_INTERVIEW.md` for detailed instructions on how to test the real-time collaboration and video chat features.

### Quick Test Steps:
1. Create two user accounts
2. Schedule an interview session as User 1
3. Start the interview from both browser windows
4. Test code synchronization by typing in one window
5. Test video chat by checking camera feeds
6. Verify role-based features (interviewee cannot see hints/solutions)

## Immediate Next Steps

### Priority 1: Backend WebSocket/SignalR Implementation

**Why:** The frontend `CollaborativeCodeEditor` is ready but needs a backend WebSocket endpoint to function.

**Tasks:**
1. **Install SignalR** (Recommended for ASP.NET Core)
   ```powershell
   cd backend/Vector.Api
   dotnet add package Microsoft.AspNetCore.SignalR
   ```

2. **Create CollaborationHub**
   - Location: `backend/Vector.Api/Hubs/CollaborationHub.cs`
   - Handle:
     - User joining session
     - Code change broadcasts
     - Cursor position updates
     - User presence management

3. **Update Program.cs**
   - Register SignalR services
   - Map hub endpoint: `/api/collaboration/{sessionId}`

4. **Update Frontend**
   - Replace WebSocket with SignalR client
   - Install: `npm install @microsoft/signalr`

**Estimated Time:** 2-4 hours

### Priority 2: WebRTC Signaling Server

**Why:** Video chat needs signaling to establish peer connections.

**Tasks:**
1. **Create Signaling Endpoint**
   - POST `/api/video-sessions/{id}/offer` - Handle WebRTC offer
   - POST `/api/video-sessions/{id}/answer` - Handle WebRTC answer
   - POST `/api/video-sessions/{id}/ice-candidate` - Handle ICE candidates

2. **Update VideoSessionService**
   - Store signaling data (offers, answers, ICE candidates)
   - Retrieve signaling data for peer connection

3. **Update VideoChat Component**
   - Connect to signaling endpoints
   - Exchange SDP offers/answers
   - Exchange ICE candidates

**Estimated Time:** 3-5 hours

### Priority 3: Day 17-18: Interview Timer & Question Selection

**Tasks:**
1. **Backend:**
   - Create `InterviewTimer` model
   - Implement timer service
   - Add timer endpoints to `PeerInterviewController`

2. **Frontend:**
   - Enhance timer display (already partially implemented)
   - Add pause/resume functionality
   - Add time warnings (5 min, 1 min remaining)

3. **Question Selection:**
   - Random question selection (already implemented for scheduling)
   - Question selection during interview
   - Question filtering by category

**Estimated Time:** 4-6 hours

### Priority 4: Day 19-20: Session Recording & Feedback

**Tasks:**
1. **Backend:**
   - Create `InterviewFeedback` model
   - Implement `FeedbackService`
   - Create `FeedbackController` endpoints

2. **Frontend:**
   - Create `FeedbackForm` component (already exists as `FeedbackView`)
   - Integrate feedback submission
   - Display feedback after session

3. **Session Recording (Optional):**
   - Integrate recording service
   - Store recording URLs
   - Access control for recordings

**Estimated Time:** 6-8 hours

## Recommended Implementation Order

### Week 1: Complete Real-time Features
1. **Day 1-2:** Implement SignalR for code collaboration
2. **Day 3-4:** Implement WebRTC signaling
3. **Day 5:** Testing and bug fixes

### Week 2: Interview Features
1. **Day 1-2:** Interview timer enhancements
2. **Day 3-4:** Question selection during interview
3. **Day 5:** Testing

### Week 3: Feedback & Polish
1. **Day 1-2:** Feedback system
2. **Day 3-4:** Session recording (optional)
3. **Day 5:** Testing and documentation

## Testing Strategy

### Unit Tests
- [ ] SignalR hub tests
- [ ] Signaling service tests
- [ ] Timer service tests
- [ ] Feedback service tests

### Integration Tests
- [ ] WebSocket connection tests
- [ ] Video signaling tests
- [ ] End-to-end session flow tests

### Manual Testing
- [ ] Two-user video chat
- [ ] Real-time code synchronization
- [ ] Timer functionality
- [ ] Feedback submission

## Technical Decisions Needed

### 1. WebSocket vs SignalR
- **Current:** Frontend uses WebSocket
- **Recommendation:** Use SignalR (better ASP.NET Core integration)
- **Action:** Update frontend to use SignalR client

### 2. WebRTC vs Third-party Service
- **Current:** WebRTC implementation started
- **Options:**
  - Continue with WebRTC (more control, more complex)
  - Use third-party service (Twilio, Agora, etc.) - easier but costs money
- **Recommendation:** Continue with WebRTC for MVP, consider third-party for production

### 3. Session Recording
- **Options:**
  - Record on client (browser MediaRecorder API)
  - Record on server (more complex)
  - Use third-party service
- **Recommendation:** Start with client-side recording, upgrade later if needed

## Dependencies to Add

### Backend
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="8.0.0" />
```

### Frontend
```json
{
  "@microsoft/signalr": "^8.0.0"
}
```

## Documentation Updates Needed

1. Update `STAGE2_IMPLEMENTATION.md` with:
   - SignalR implementation details
   - WebRTC signaling architecture
   - Testing procedures

2. Create API documentation for:
   - SignalR hub methods
   - WebRTC signaling endpoints
   - Timer endpoints
   - Feedback endpoints

## Questions to Answer

1. **Authentication:** How to authenticate WebSocket/SignalR connections?
   - Use JWT token in query string
   - Use cookie-based auth

2. **Authorization:** Who can join a session?
   - Only interviewer and interviewee
   - How to verify session ownership?

3. **Scalability:** How to handle multiple sessions?
   - One hub per session?
   - Groups within single hub?

4. **Error Recovery:** What happens on connection loss?
   - Auto-reconnect?
   - State synchronization?

## Success Criteria

### Phase 1: Real-time Collaboration (Week 1)
- [ ] Code changes sync in real-time between users
- [ ] Video chat works between two users
- [ ] Screen sharing works
- [ ] Connection errors handled gracefully

### Phase 2: Interview Features (Week 2)
- [ ] Timer works correctly
- [ ] Timer warnings appear at 5 min and 1 min
- [ ] Question selection works during interview
- [ ] Session state transitions work correctly

### Phase 3: Feedback (Week 3)
- [ ] Feedback can be submitted after session
- [ ] Feedback is displayed correctly
- [ ] Feedback validation works
- [ ] Session recording works (if implemented)

## Getting Help

- Check `TESTING_PEER_INTERVIEW_SESSION.md` for testing procedures
- Review SignalR documentation: https://docs.microsoft.com/en-us/aspnet/core/signalr
- Review WebRTC documentation: https://webrtc.org/

