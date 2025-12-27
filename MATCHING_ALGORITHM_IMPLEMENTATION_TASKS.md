# Matching Algorithm Implementation Tasks

## Overview
This document outlines the tasks for implementing and testing the matching algorithm improvements, including skill-based matching and user blocking system.

## Task 1: Create Unit Tests for Current Matching Algorithm

### Description
Create comprehensive unit tests for the existing matching algorithm to ensure it works correctly before adding new features.

### Subtasks
- [ ] Test `FindMatchingPeerAsync` with various scenarios:
  - [ ] Matching on InterviewType only
  - [ ] Matching on InterviewType + InterviewLevel
  - [ ] Fallback to InterviewType when no InterviewLevel match
  - [ ] FIFO ordering (oldest request first)
  - [ ] Request expiration handling
  - [ ] No available matches
  - [ ] Concurrent matching requests

### Files to Create/Modify
- `backend/Vector.Api.Tests/Services/PeerInterviewServiceTests.cs` - Add matching tests

### Acceptance Criteria
- All existing matching scenarios are covered by tests
- Tests pass consistently
- Edge cases are handled

---

## Task 2: Implement User Rating System

### Description
Create a user rating/score system that tracks user performance and skill levels.

### Subtasks
- [ ] Create `UserRating` model with:
  - [ ] OverallScore (0-100)
  - [ ] Category-specific scores (DataStructuresScore, SystemDesignScore, etc.)
  - [ ] LastUpdated timestamp
- [ ] Create database migration
- [ ] Implement rating calculation logic:
  - [ ] Based on interview performance
  - [ ] Based on code submission success rate
  - [ ] Based on peer feedback ratings
  - [ ] Based on session completion rate
- [ ] Create `IUserRatingService` interface
- [ ] Implement `UserRatingService`:
  - [ ] `CalculateRatingAsync(Guid userId)` - Calculate overall rating
  - [ ] `UpdateRatingAfterSessionAsync(Guid userId, Guid sessionId)` - Update after session
  - [ ] `GetRatingAsync(Guid userId)` - Get current rating
- [ ] Add unit tests for rating calculation

### Files to Create/Modify
- `backend/Vector.Api/Models/UserRating.cs` (new)
- `backend/Vector.Api/Services/IUserRatingService.cs` (new)
- `backend/Vector.Api/Services/UserRatingService.cs` (new)
- `backend/Vector.Api/Controllers/UserRatingController.cs` (new, optional)
- `backend/Vector.Api.Tests/Services/UserRatingServiceTests.cs` (new)
- Migration: `AddUserRating`

### Acceptance Criteria
- User ratings are calculated and stored correctly
- Ratings update after each interview session
- Unit tests pass
- Migration runs successfully

---

## Task 3: Implement Skill-Based Matching

### Description
Update the matching algorithm to match users based on their skill levels.

### Subtasks
- [ ] Add `MatchingStrategy` enum (FIFO, SkillBased, Balanced, Strict)
- [ ] Add `MatchingConfig` class for configuration
- [ ] Update `FindMatchingPeerAsync` to accept `MatchingStrategy` parameter
- [ ] Implement skill-based matching logic:
  - [ ] Get user's rating
  - [ ] Find peers with similar skill levels (±tolerance)
  - [ ] Fallback to FIFO if no skill match found
- [ ] Add configuration to `appsettings.json`
- [ ] Update `PeerInterviewController` to use strategy from config
- [ ] Add unit tests for skill-based matching:
  - [ ] Match users with similar skill levels
  - [ ] Fallback to FIFO when no skill match
  - [ ] Tolerance boundary testing
  - [ ] Edge cases (no rating, very high/low ratings)

### Files to Create/Modify
- `backend/Vector.Api/Models/MatchingStrategy.cs` (new enum)
- `backend/Vector.Api/Models/MatchingConfig.cs` (new)
- `backend/Vector.Api/Services/PeerInterviewService.cs` - Update `FindMatchingPeerAsync`
- `backend/Vector.Api/Controllers/PeerInterviewController.cs` - Use config
- `backend/Vector.Api/appsettings.json` - Add matching config
- `backend/Vector.Api.Tests/Services/PeerInterviewServiceTests.cs` - Add skill-based tests

### Acceptance Criteria
- Skill-based matching works correctly
- Falls back to FIFO when appropriate
- Configuration is easily adjustable
- Unit tests pass
- Documentation updated

---

## Task 4: Implement User Blocking System

### Description
Create a system to temporarily or permanently block users who have low ratings, frequently quit sessions, or receive negative feedback.

### Subtasks
- [ ] Create `UserBlock` model:
  - [ ] UserId, Reason, BlockedUntil, IsPermanent
  - [ ] CreatedAt, UpdatedAt
- [ ] Create `SessionQuit` model:
  - [ ] UserId, SessionId, QuitAt, Reason
- [ ] Create database migrations
- [ ] Implement blocking logic:
  - [ ] `CheckAndBlockUserIfNeededAsync(Guid userId)` - Auto-blocking
  - [ ] `BlockUserAsync(Guid userId, string reason, DateTime blockedUntil)` - Manual blocking
  - [ ] `IsUserBlockedAsync(Guid userId)` - Check if blocked
- [ ] Update `FindMatchingPeerAsync` to exclude blocked users
- [ ] Implement quit tracking:
  - [ ] Track when users leave sessions early
  - [ ] Calculate quit rate
  - [ ] Auto-block if quit rate > threshold
- [ ] Add unit tests:
  - [ ] Blocking users with low ratings
  - [ ] Blocking users with high quit rates
  - [ ] Excluding blocked users from matching
  - [ ] Temporary vs permanent blocks

### Files to Create/Modify
- `backend/Vector.Api/Models/UserBlock.cs` (new)
- `backend/Vector.Api/Models/SessionQuit.cs` (new)
- `backend/Vector.Api/Services/IUserBlockingService.cs` (new)
- `backend/Vector.Api/Services/UserBlockingService.cs` (new)
- `backend/Vector.Api/Services/PeerInterviewService.cs` - Exclude blocked users
- `backend/Vector.Api/Controllers/PeerInterviewController.cs` - Check blocks
- `backend/Vector.Api.Tests/Services/UserBlockingServiceTests.cs` (new)
- Migrations: `AddUserBlock`, `AddSessionQuit`

### Acceptance Criteria
- Users can be blocked automatically or manually
- Blocked users are excluded from matching
- Quit rate is tracked and calculated correctly
- Unit tests pass
- Documentation updated

---

## Task 5: Integration Tests

### Description
Create integration tests that test the complete matching flow with all new features.

### Subtasks
- [ ] Test complete matching flow with skill-based matching
- [ ] Test matching with blocked users
- [ ] Test rating updates after sessions
- [ ] Test early exit detection and blocking
- [ ] Test fallback scenarios

### Files to Create/Modify
- `backend/Vector.Api.Tests/Integration/PeerInterviewMatchingTests.cs` (new)

### Acceptance Criteria
- All integration tests pass
- Tests cover end-to-end scenarios
- Performance is acceptable

---

## Task 6: Configuration & Monitoring

### Description
Add configuration options and monitoring for the matching system.

### Subtasks
- [ ] Add admin interface for matching configuration (optional)
- [ ] Add logging for matching decisions
- [ ] Add analytics for matching success rates
- [ ] Add metrics for:
  - [ ] Average time to match
  - [ ] Match success rate
  - [ ] Skill-based vs FIFO match distribution

### Files to Create/Modify
- `backend/Vector.Api/Controllers/AdminController.cs` - Add matching config endpoints (optional)
- `backend/Vector.Api/Services/PeerInterviewService.cs` - Add logging

### Acceptance Criteria
- Configuration is easily adjustable
- Matching decisions are logged
- Analytics are available

---

## Testing Strategy

### Unit Tests
- Test each component in isolation
- Mock dependencies
- Cover edge cases

### Integration Tests
- Test complete flows
- Use test database
- Clean up after tests

### Manual Testing
- Test with real users
- Monitor matching behavior
- Adjust configuration as needed

---

## Implementation Order

1. **Task 1**: Create unit tests for current algorithm (foundation)
2. **Task 2**: Implement user rating system (prerequisite for skill-based matching)
3. **Task 3**: Implement skill-based matching (uses ratings)
4. **Task 4**: Implement user blocking system (independent feature)
5. **Task 5**: Integration tests (validate everything works together)
6. **Task 6**: Configuration & monitoring (polish)

---

## Estimated Effort

- Task 1: 4-6 hours
- Task 2: 8-12 hours
- Task 3: 6-8 hours
- Task 4: 6-8 hours
- Task 5: 4-6 hours
- Task 6: 4-6 hours

**Total**: ~32-46 hours

---

## Notes

- Start with Task 1 to ensure current functionality is well-tested
- Task 2 and Task 3 are dependent (ratings needed for skill-based matching)
- Task 4 is independent and can be done in parallel with Task 2/3
- All tasks should include comprehensive unit tests
- Configuration should be easily adjustable without code changes




