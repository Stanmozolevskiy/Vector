# Peer Interview Matching Algorithm Documentation

## Current Matching Algorithm

### Overview
The matching algorithm is implemented in `PeerInterviewService.FindMatchingPeerAsync()` and is responsible for pairing users for peer interview sessions.

### Current Implementation

#### Step 1: Find Matching Requests
The algorithm searches for available matching requests with the following criteria:

```csharp
var availableRequests = await _context.InterviewMatchingRequests
    .Include(r => r.User)
    .Include(r => r.ScheduledSession)
    .Where(r => r.Status == "Pending"
        && r.UserId != userId
        && r.ExpiresAt > DateTime.UtcNow
        && (r.ScheduledSession.InterviewType == userSession.InterviewType || 
            (r.ScheduledSession.InterviewType == null && userSession.InterviewType == null)))
    .OrderBy(r => r.CreatedAt) // Match with oldest request first (FIFO)
    .FirstOrDefaultAsync();
```

#### Matching Criteria (Current)
1. **Primary Requirement: InterviewType**
   - Must match exactly (e.g., "data-structures-algorithms", "system-design")
   - If both are null, they match

2. **Secondary: InterviewLevel** (Flexible)
   - Currently not enforced - any level can match
   - The algorithm tries to match on InterviewType + InterviewLevel first, but falls back to InterviewType only

3. **Ordering: FIFO (First In, First Out)**
   - Matches with the oldest pending request first
   - Ensures fairness and prevents requests from waiting too long

4. **Expiration**
   - Requests expire after 5 minutes (`ExpiresAt > DateTime.UtcNow`)

### Matching Flow

1. **User 1** creates a session and clicks "Start interview"
   - Creates a `InterviewMatchingRequest` with status "Pending"
   - Request expires in 5 minutes

2. **User 2** creates a session and clicks "Start interview"
   - Creates a `InterviewMatchingRequest` with status "Pending"
   - `FindMatchingPeerAsync` is called, finds User 1's request
   - Both requests are linked and status changes to "Matched"

3. **Both users confirm**
   - `CompleteMatchAsync` merges the two sessions
   - Session1 becomes the primary session (status: "InProgress")
   - Session2 is cancelled
   - Both users are assigned roles (interviewer/interviewee)

## Proposed Matching Tuning System

### 1. Skill-Based Matching

#### Concept
Match users based on their skill level/rating to ensure:
- Strong candidates match with strong peers
- Beginners match with beginners
- Intermediate users match with intermediate users

#### Implementation Design

**Step 1: Add User Rating/Score System**

```csharp
// Add to User model or create UserRating model
public class UserRating
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    // Overall skill score (0-100)
    public double OverallScore { get; set; }
    
    // Category-specific scores
    public double DataStructuresScore { get; set; }
    public double SystemDesignScore { get; set; }
    public double BehavioralScore { get; set; }
    
    // Calculated from:
    // - Interview performance
    // - Code submission success rate
    // - Peer feedback ratings
    // - Session completion rate
    
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Step 2: Update Matching Algorithm**

```csharp
public async Task<InterviewMatchingRequest?> FindMatchingPeerAsync(
    Guid userId, 
    Guid sessionId,
    MatchingStrategy strategy = MatchingStrategy.SkillBased)
{
    var userSession = await _context.PeerInterviewSessions.FindAsync(sessionId);
    if (userSession == null || userSession.InterviewerId != userId)
    {
        return null;
    }

    // Get user's rating
    var userRating = await _context.UserRatings
        .FirstOrDefaultAsync(r => r.UserId == userId);
    
    var userScore = userRating?.OverallScore ?? 50.0; // Default to middle

    // Find available requests with matching InterviewType
    var baseQuery = _context.InterviewMatchingRequests
        .Include(r => r.User)
        .Include(r => r.ScheduledSession)
        .Where(r => r.Status == "Pending"
            && r.UserId != userId
            && r.ExpiresAt > DateTime.UtcNow
            && (r.ScheduledSession.InterviewType == userSession.InterviewType || 
                (r.ScheduledSession.InterviewType == null && userSession.InterviewType == null)));

    // Apply matching strategy
    InterviewMatchingRequest? match = null;
    
    switch (strategy)
    {
        case MatchingStrategy.SkillBased:
            // Match users with similar skill levels (±10 points)
            var skillBasedMatches = await baseQuery
                .Include(r => r.User)
                    .ThenInclude(u => u.Rating)
                .Where(r => r.User.Rating != null)
                .ToListAsync();
            
            match = skillBasedMatches
                .Where(r => Math.Abs((r.User.Rating.OverallScore ?? 50) - userScore) <= 10)
                .OrderBy(r => Math.Abs((r.User.Rating.OverallScore ?? 50) - userScore))
                .ThenBy(r => r.CreatedAt)
                .FirstOrDefault();
            
            // If no skill-based match, fall back to FIFO
            if (match == null)
            {
                match = await baseQuery
                    .OrderBy(r => r.CreatedAt)
                    .FirstOrDefaultAsync();
            }
            break;
            
        case MatchingStrategy.FIFO:
        default:
            match = await baseQuery
                .OrderBy(r => r.CreatedAt)
                .FirstOrDefaultAsync();
            break;
    }

    // ... rest of matching logic
}
```

**Step 3: Configuration**

```csharp
public enum MatchingStrategy
{
    FIFO,           // First In, First Out (current)
    SkillBased,     // Match by skill level
    Balanced,       // Mix of skill-based and FIFO
    Strict          // Only match exact skill levels
}

public class MatchingConfig
{
    public MatchingStrategy Strategy { get; set; } = MatchingStrategy.SkillBased;
    public double SkillTolerance { get; set; } = 10.0; // ±10 points
    public bool AllowFallbackToFIFO { get; set; } = true;
    public int MaxWaitTimeMinutes { get; set; } = 5;
}
```

### 2. User Blocking System

#### Concept
Temporarily block users who:
- Have low ratings (< 30/100)
- Frequently quit sessions
- Receive negative feedback from peers
- Have been reported for inappropriate behavior

#### Implementation Design

**Step 1: Add User Blocking Model**

```csharp
public class UserBlock
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public string Reason { get; set; } // "LowRating", "FrequentQuit", "Reported", etc.
    public DateTime BlockedUntil { get; set; }
    public bool IsPermanent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SessionQuit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public Guid SessionId { get; set; }
    public DateTime QuitAt { get; set; }
    public string Reason { get; set; }
}
```

**Step 2: Update Matching Query**

```csharp
// Exclude blocked users
var availableRequests = await _context.InterviewMatchingRequests
    .Include(r => r.User)
    .Include(r => r.ScheduledSession)
    .Where(r => r.Status == "Pending"
        && r.UserId != userId
        && r.ExpiresAt > DateTime.UtcNow
        && (r.ScheduledSession.InterviewType == userSession.InterviewType || 
            (r.ScheduledSession.InterviewType == null && userSession.InterviewType == null))
        // Exclude blocked users
        && !_context.UserBlocks.Any(b => 
            b.UserId == r.UserId && 
            (b.IsPermanent || b.BlockedUntil > DateTime.UtcNow)))
    .OrderBy(r => r.CreatedAt)
    .FirstOrDefaultAsync();
```

**Step 3: Auto-Blocking Logic**

```csharp
public async Task CheckAndBlockUserIfNeeded(Guid userId)
{
    // Check rating
    var rating = await _context.UserRatings
        .FirstOrDefaultAsync(r => r.UserId == userId);
    
    if (rating != null && rating.OverallScore < 30)
    {
        await BlockUserAsync(userId, "LowRating", DateTime.UtcNow.AddDays(7));
        return;
    }
    
    // Check quit rate (last 30 days)
    var quitCount = await _context.SessionQuits
        .CountAsync(q => q.UserId == userId && 
                        q.QuitAt > DateTime.UtcNow.AddDays(-30));
    
    var totalSessions = await _context.PeerInterviewSessions
        .CountAsync(s => (s.InterviewerId == userId || s.IntervieweeId == userId) &&
                        s.CreatedAt > DateTime.UtcNow.AddDays(-30));
    
    var quitRate = totalSessions > 0 ? (double)quitCount / totalSessions : 0;
    
    if (quitRate > 0.5) // More than 50% quit rate
    {
        await BlockUserAsync(userId, "FrequentQuit", DateTime.UtcNow.AddDays(14));
        return;
    }
}

private async Task BlockUserAsync(Guid userId, string reason, DateTime blockedUntil)
{
    var existingBlock = await _context.UserBlocks
        .FirstOrDefaultAsync(b => b.UserId == userId && 
                                 (b.IsPermanent || b.BlockedUntil > DateTime.UtcNow));
    
    if (existingBlock == null)
    {
        var block = new UserBlock
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Reason = reason,
            BlockedUntil = blockedUntil,
            IsPermanent = false,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.UserBlocks.Add(block);
        await _context.SaveChangesAsync();
        
        _logger.LogWarning("User {UserId} blocked until {BlockedUntil} for reason: {Reason}", 
            userId, blockedUntil, reason);
    }
}
```

### 3. Testing Strategy

#### Unit Tests for Skill-Based Matching

```csharp
[Fact]
public async Task FindMatchingPeerAsync_WithSkillBasedStrategy_MatchesSimilarSkillLevels()
{
    // Arrange
    var user1 = CreateTestUser("user1@test.com");
    var user2 = CreateTestUser("user2@test.com");
    var user3 = CreateTestUser("user3@test.com");
    
    // Create ratings
    await _context.UserRatings.AddRangeAsync(
        new UserRating { UserId = user1.Id, OverallScore = 80 },
        new UserRating { UserId = user2.Id, OverallScore = 85 }, // Close to user1
        new UserRating { UserId = user3.Id, OverallScore = 30 }  // Far from user1
    );
    await _context.SaveChangesAsync();
    
    var session1 = await CreateSessionAsync(user1.Id, "data-structures-algorithms");
    var session2 = await CreateSessionAsync(user2.Id, "data-structures-algorithms");
    var session3 = await CreateSessionAsync(user3.Id, "data-structures-algorithms");
    
    // User2 creates matching request first
    await _service.CreateMatchingRequestAsync(session2.Id, user2.Id);
    
    // User3 creates matching request second
    await _service.CreateMatchingRequestAsync(session3.Id, user3.Id);
    
    // Act: User1 tries to match (should match with user2 due to similar skill)
    var result = await _service.FindMatchingPeerAsync(user1.Id, session1.Id);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(user2.Id, result.MatchedUserId);
    Assert.NotEqual(user3.Id, result.MatchedUserId);
}

[Fact]
public async Task FindMatchingPeerAsync_WithSkillBasedStrategy_FallsBackToFIFO_WhenNoSkillMatch()
{
    // Arrange
    var user1 = CreateTestUser("user1@test.com");
    var user2 = CreateTestUser("user2@test.com");
    
    await _context.UserRatings.AddRangeAsync(
        new UserRating { UserId = user1.Id, OverallScore = 80 },
        new UserRating { UserId = user2.Id, OverallScore = 95 } // Too far (15 points)
    );
    await _context.SaveChangesAsync();
    
    var session1 = await CreateSessionAsync(user1.Id, "data-structures-algorithms");
    var session2 = await CreateSessionAsync(user2.Id, "data-structures-algorithms");
    
    // User2 creates matching request first
    var request2 = await _service.CreateMatchingRequestAsync(session2.Id, user2.Id);
    await Task.Delay(100); // Ensure different CreatedAt
    
    // Act: User1 tries to match (should fall back to FIFO and match with user2)
    var result = await _service.FindMatchingPeerAsync(user1.Id, session1.Id);
    
    // Assert: Falls back to FIFO
    Assert.NotNull(result);
    Assert.Equal(user2.Id, result.MatchedUserId);
}
```

#### Unit Tests for User Blocking

```csharp
[Fact]
public async Task FindMatchingPeerAsync_ExcludesBlockedUsers()
{
    // Arrange
    var user1 = CreateTestUser("user1@test.com");
    var user2 = CreateTestUser("user2@test.com");
    var user3 = CreateTestUser("user3@test.com");
    
    // Block user2
    await _context.UserBlocks.AddAsync(new UserBlock
    {
        Id = Guid.NewGuid(),
        UserId = user2.Id,
        Reason = "LowRating",
        BlockedUntil = DateTime.UtcNow.AddDays(7),
        IsPermanent = false,
        CreatedAt = DateTime.UtcNow
    });
    await _context.SaveChangesAsync();
    
    var session1 = await CreateSessionAsync(user1.Id, "data-structures-algorithms");
    var session2 = await CreateSessionAsync(user2.Id, "data-structures-algorithms");
    var session3 = await CreateSessionAsync(user3.Id, "data-structures-algorithms");
    
    // User2 and user3 create matching requests
    await _service.CreateMatchingRequestAsync(session2.Id, user2.Id);
    await _service.CreateMatchingRequestAsync(session3.Id, user3.Id);
    
    // Act: User1 tries to match (should match with user3, not blocked user2)
    var result = await _service.FindMatchingPeerAsync(user1.Id, session1.Id);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(user3.Id, result.MatchedUserId);
    Assert.NotEqual(user2.Id, result.MatchedUserId);
}

[Fact]
public async Task CheckAndBlockUserIfNeeded_BlocksUser_WhenRatingTooLow()
{
    // Arrange
    var user = CreateTestUser("user@test.com");
    await _context.UserRatings.AddAsync(new UserRating
    {
        UserId = user.Id,
        OverallScore = 25 // Below threshold
    });
    await _context.SaveChangesAsync();
    
    // Act
    await _service.CheckAndBlockUserIfNeeded(user.Id);
    
    // Assert
    var block = await _context.UserBlocks
        .FirstOrDefaultAsync(b => b.UserId == user.Id);
    
    Assert.NotNull(block);
    Assert.Equal("LowRating", block.Reason);
    Assert.True(block.BlockedUntil > DateTime.UtcNow);
}

[Fact]
public async Task CheckAndBlockUserIfNeeded_BlocksUser_WhenQuitRateTooHigh()
{
    // Arrange
    var user = CreateTestUser("user@test.com");
    
    // Create 6 sessions, user quit 4 of them (66% quit rate)
    var sessions = new List<PeerInterviewSession>();
    for (int i = 0; i < 6; i++)
    {
        var session = await CreateSessionAsync(user.Id, "data-structures-algorithms");
        sessions.Add(session);
    }
    
    // User quit 4 sessions
    for (int i = 0; i < 4; i++)
    {
        await _context.SessionQuits.AddAsync(new SessionQuit
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionId = sessions[i].Id,
            QuitAt = DateTime.UtcNow.AddDays(-10),
            Reason = "UserLeft"
        });
    }
    await _context.SaveChangesAsync();
    
    // Act
    await _service.CheckAndBlockUserIfNeeded(user.Id);
    
    // Assert
    var block = await _context.UserBlocks
        .FirstOrDefaultAsync(b => b.UserId == user.Id);
    
    Assert.NotNull(block);
    Assert.Equal("FrequentQuit", block.Reason);
}
```

## Implementation Roadmap

### Phase 1: User Rating System
1. Create `UserRating` model and migration
2. Implement rating calculation logic
3. Update rating after each interview session
4. Add unit tests

### Phase 2: Skill-Based Matching
1. Add `MatchingStrategy` enum and configuration
2. Update `FindMatchingPeerAsync` to support skill-based matching
3. Add fallback logic
4. Add unit tests

### Phase 3: User Blocking System
1. Create `UserBlock` and `SessionQuit` models
2. Implement blocking logic
3. Add auto-blocking checks
4. Update matching query to exclude blocked users
5. Add unit tests

### Phase 4: Configuration & Monitoring
1. Add admin interface for matching configuration
2. Add monitoring/logging for matching decisions
3. Add analytics for matching success rates
4. A/B testing framework for different strategies

## Configuration Example

```json
{
  "Matching": {
    "Strategy": "SkillBased",
    "SkillTolerance": 10.0,
    "AllowFallbackToFIFO": true,
    "MaxWaitTimeMinutes": 5,
    "Blocking": {
      "LowRatingThreshold": 30.0,
      "QuitRateThreshold": 0.5,
      "BlockDurationDays": 7,
      "PermanentBlockAfter": 3
    }
  }
}
```

## Benefits

1. **Better User Experience**: Users matched with similar skill levels have more productive sessions
2. **Quality Control**: Blocking problematic users improves overall platform quality
3. **Flexibility**: Easy to tune matching parameters without code changes
4. **Testability**: Comprehensive unit tests ensure matching logic works correctly
5. **Scalability**: Can add more matching strategies in the future




