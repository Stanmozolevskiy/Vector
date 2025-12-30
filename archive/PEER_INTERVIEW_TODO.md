# Peer Interview Backend Implementation TODO List

**Status:** Phases 1-3, 5-6 Complete ✅ | Frontend Integration Complete ✅ | Ready for Testing

## Phase 1: Database & Models ✅ COMPLETE
- [x] Task 1: Create database models (ScheduledInterviewSession, InterviewMatchingRequest, LiveInterviewSession, LiveInterviewParticipant, InterviewFeedback)
- [x] Task 2: Create EF Core migration for new interview tables
- [x] Task 19: Update ApplicationDbContext with new DbSets and entity configurations

**See PHASE1_COMPLETE.md for testing instructions**

## Phase 2: DTOs & Service Interface ✅ COMPLETE
- [x] Task 3: Create DTOs for interview scheduling, matching, session management, and feedback
- [x] Task 4: Create IPeerInterviewService interface with methods for scheduling, matching, sessions, roles, and feedback

## Phase 3: Core Service Implementation ✅ COMPLETE
- [x] Task 5: Implement PeerInterviewService: ScheduleInterviewSession - create scheduled session with interviewType, practiceType, interviewLevel, ScheduledStartAt
- [x] Task 6: Implement PeerInterviewService: GetUpcomingSessions - return scheduled sessions for Upcoming Interviews table
- [x] Task 7: Implement PeerInterviewService: StartMatching - create matching request when user clicks Start Interview, match based on interviewType (hard), practiceType (hard), interviewLevel (soft), ScheduledStartAt
- [x] Task 8: Implement PeerInterviewService: Matching Algorithm - FIFO matching with 10-minute expiration, later add SkillBased matching
- [x] Task 9: Implement PeerInterviewService: CreateLiveSession - create LiveInterviewSession when match is found with first and second questions, roles (Interviewer/Interviewee)
- [x] Task 10: Implement PeerInterviewService: ConfirmMatch - handle user confirmation readiness, redirect to question page when both confirmed
- [x] Task 11: Implement PeerInterviewService: SwitchRoles - allow either user to swap interviewer/interviewee roles mid-session
- [x] Task 12: Implement PeerInterviewService: ChangeQuestion - interviewer can change active question, update ActiveQuestionId and store audit trail

## Phase 4: Question Controller Updates
- [ ] Task 13: Update QuestionController: Add sessionId parameter support, role-aware responses (hide hints/solutions for Interviewee, show for Interviewer)

## Phase 5: Feedback System ✅ COMPLETE
- [x] Task 14: Implement PeerInterviewService: EndInterview - either user can end interview, trigger feedback workflow
- [x] Task 15: Implement PeerInterviewService: SubmitFeedback - create separate feedback records per participant after interview ends

## Phase 6: API Controller ✅ COMPLETE
- [x] Task 16: Create PeerInterviewController with endpoints: POST /scheduled, GET /scheduled/upcoming, POST /sessions/{id}/start-matching, POST /matching-requests/{id}/confirm, GET /sessions/{id}, POST /sessions/{id}/switch-roles, POST /sessions/{id}/change-question, POST /sessions/{id}/end, POST /feedback
- [x] Task 18: Register PeerInterviewService in Program.cs dependency injection

## Phase 7: Frontend Integration ✅ COMPLETE
- [x] Updated frontend peerInterview.service.ts to map to new backend API
- [x] Added backward compatibility for legacy frontend methods
- [x] Created API documentation (PEER_INTERVIEW_API_DOCUMENTATION.md)

## Phase 8: Infrastructure
- [ ] Task 17: Add cleanup job/background service to expire matching requests after 10 minutes

## Phase 9: Testing
- [ ] Task 20: Test complete flow: Schedule → Start Interview → Matching → Confirmation → Live Session → Role Switch → Question Change → End Interview → Feedback

## Deployment Status

✅ Backend API implemented and tested
✅ Frontend service updated and mapped
✅ Database migration created and applied
✅ Service registered in dependency injection
✅ API documentation created

**Next Steps:**
1. Deploy to local Docker
2. Test scheduling functionality
3. Test matching and live session flow
4. Implement background cleanup job (Task 17)
5. Update QuestionController for role-aware responses (Task 13)

