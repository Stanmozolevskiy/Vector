# Coins & Achievements System - Summary & User Guide

## Overview
The Coins & Achievements System is a gamification feature that rewards users with points (coins) for completing various activities on the Vector platform. This document explains how to earn coins and what achievements are currently implemented.

---

## How to Earn Coins

### 🪙 **Profile Completed** - 10 coins (One-time)
**What you need to do:**
Complete ALL of the following fields in your profile:
- ✅ First Name
- ✅ Last Name  
- ✅ Email (auto-filled during registration)
- ✅ Profile Picture (upload a photo)
- ✅ Bio (write something about yourself)
- ✅ Phone Number
- ✅ Location

**When you get coins:** Automatically awarded when your profile reaches 100% completion.

**Note:** This is a one-time reward. You can only earn it once.

---

### 🤝 **Completed Interview** - 10 coins
**What you need to do:**
- Complete a scheduled peer interview (matched with another user)
- Complete an interview with an expert

**When you get coins:** Automatically awarded when the interview ends.

**Note:** This does NOT apply to "Practice with a Friend" interviews. Only scheduled interviews count.

---

### 🎯 **Join Mock Interview** - 10 coins
**What you need to do:**
- Schedule an interview and wait for a match
- Confirm when matched with another user
- Both users must confirm for the session to start

**When you get coins:** Automatically awarded when both users confirm the match and the live interview session is created.

**Note:** This does NOT apply to "Practice with a Friend" interviews. Only matched interviews count.

---

### 🌟 **Great Mock Interview Partner** - 15 coins (Bonus)
**What you need to do:**
- Complete a mock interview
- Receive a **5-star rating** from your interview partner

**When you get coins:** Automatically awarded after your partner submits feedback with a 5-star interviewer performance rating.

**Note:** You must receive exactly 5 stars (not 4 stars) to earn this bonus.

---

### 📝 **Question Published** - 25 coins
**What you need to do:**
- Create and submit an interview question
- Wait for admin approval

**When you get coins:** Automatically awarded when an admin approves your question.

---

### 🔄 **Question Used in Another Interview** - 5 coins
**What you need to do:**
- Create and publish a question
- Your question gets selected for use in someone else's interview

**When you get coins:** Automatically awarded each time your question is used in a scheduled interview.

**Note:** You can earn this multiple times for the same question.

---

### 💡 **Feedback Submitted** - 10 coins
**What you need to do:**
- Complete an interview
- Submit feedback about your interview partner

**When you get coins:** Automatically awarded immediately after submitting feedback.

---

## Not Yet Implemented

The following achievements are defined but not yet integrated into the system:

### 📚 **Lesson Completed** - 1 coin
*Coming soon: Will be awarded when the lesson/course system is implemented.*

### 👍 **Question Upvoted** - 5 coins
*Coming soon: Will be awarded when question voting is implemented.*

### 💬 **Comment Upvoted** - 5 coins
*Coming soon: Will be awarded when comment system is implemented.*

### 🎁 **Referral Success** - 100 coins
*Coming soon: Will be awarded when referral system is implemented.*

---

## Where to See Your Coins

### 1. **Header Navigation Bar**
- Your current coin count is displayed in the top-right corner next to your profile menu
- Format: "2.3k 🪙" (rounded to thousands/hundreds)
- Click on the coin display to go to the Leaderboard

### 2. **Profile Page - Activity Tab**
- Go to your profile settings
- Click on "Activity & Coins" in the sidebar
- View:
  - Your total coins
  - Your global rank
  - Recent coin transactions (what you earned and when)

### 3. **Leaderboard Page**
- Click on your coins in the header OR
- Navigate to `/leaderboard`
- See:
  - Top 200 users with the most coins
  - Your rank among all users
  - How to earn more points

---

## Recent Changes & Fixes

### Email Verification
**Problem:** SendGrid account exceeded free tier credits, preventing email verification.

**Solution:** Implemented auto-verify feature for development:
- New users are automatically verified without requiring email confirmation
- Controlled by `Development:AutoVerifyEmails` configuration (set to `true`)
- Users can register and log in immediately

**To disable:** Set `"AutoVerifyEmails": false` in `appsettings.Development.json` and provide a valid SendGrid API key.

---

### Coin Awarding Fixes

#### 1. Interview Completion Coins - Scheduled Only
**Changed:** "Completed Interview" coins are now only awarded for scheduled peer/expert interviews.

**Reason:** "Practice with a Friend" sessions are informal and shouldn't count towards achievements.

**Impact:** If you practice with a friend, you won't earn completion coins, but you can still practice freely.

---

#### 2. Join Interview Coins - Scheduled Only  
**Changed:** "Join Mock Interview" coins are now only awarded when matched users join a scheduled interview.

**Reason:** Friend practice sessions don't go through the matching system.

**Impact:** Only users who go through the queue, get matched, and confirm will earn these coins.

---

#### 3. Great Interview Partner - 5 Stars Only
**Changed:** Bonus coins for being a great interview partner now require a **5-star rating** (previously 4 or 5 stars).

**Reason:** Make the achievement more prestigious and reward truly excellent interview partners.

**Impact:** You must deliver an exceptional interview experience to earn the 15-coin bonus.

---

## Known Bugs & Issues

### Bug #6: Interview-Type Specific Feedback
**Status:** Documented, not yet fixed

**Issue:** Feedback forms are generic across all interview types.

**Expected:** Each interview type should have customized feedback questions:
- **Behavioral:** STAR method usage, listening skills, follow-up questions
- **System Design:** Requirements gathering, architecture discussion, trade-off analysis
- **Coding/SQL/ML:** Problem explanation, hint quality, code review feedback

**Impact:** Less actionable feedback, missed improvement opportunities.

---

## Testing Your Coins

### Test Scenarios:

#### 1. Complete Your Profile
1. Go to Profile Settings
2. Fill in all required fields:
   - First Name, Last Name
   - Upload Profile Picture
   - Add Bio
   - Add Phone Number
   - Add Location
3. Save changes
4. Check your coin count in the header (should increase by 10)
5. Go to Activity tab and verify the transaction appears

#### 2. Join a Scheduled Interview
1. Schedule an interview (choose "Find a peer" or "Expert")
2. Wait for a match
3. Confirm the match when prompted
4. Check your coins (should increase by 10 for joining)
5. Complete the interview (should increase by another 10)
6. Submit feedback
7. Check your coins (should increase by another 10)
8. **Total earned:** 30 coins (if partner rates you 5 stars: +15 bonus = 45 coins!)

#### 3. Create a Question
1. Navigate to Questions page
2. Create a new interview question
3. Submit for approval
4. Wait for admin to approve
5. Check your coins (should increase by 25)

---

## API Endpoints

### Public Endpoints (Require Authentication)
- `GET /api/coins/my-coins` - Get your coins and rank
- `GET /api/coins/my-transactions` - Get your coin transaction history
- `GET /api/coins/leaderboard` - Get top 200 users
- `GET /api/coins/my-rank` - Get your current rank
- `GET /api/coins/achievements` - Get list of ways to earn coins

### Admin Endpoints
- `POST /api/coins/award` - Manually award coins to a user (admin only)
- `GET /api/coins/user/{userId}` - Get another user's coins (admin only)
- `POST /api/coins/refresh-ranks` - Recalculate leaderboard ranks (internal only)

---

## Database Schema

### UserCoins Table
Stores each user's total coins and cached rank:
- `UserId` (FK to Users)
- `TotalCoins` (int)
- `Rank` (int, nullable)
- `LastRankUpdate` (timestamp)

### CoinTransactions Table
Records every coin award:
- `UserId` (FK to Users)
- `Amount` (int, coins awarded)
- `ActivityType` (string, e.g., "InterviewCompleted")
- `Description` (string, human-readable)
- `RelatedEntityId` (Guid, optional - links to interview/question)
- `RelatedEntityType` (string, e.g., "LiveInterviewSession")
- `CreatedAt` (timestamp)

### AchievementDefinitions Table
Defines all achievement types:
- `ActivityType` (string, unique)
- `DisplayName` (string)
- `Description` (string)
- `CoinsAwarded` (int)
- `Icon` (string, emoji)
- `IsActive` (bool)
- `MaxOccurrences` (int, null = unlimited)

---

## Future Enhancements (Planned but Not Implemented)

### 1. Question Upvoting
Users can upvote questions they find valuable. Question creators earn 5 coins per upvote.

### 2. Comment System
Users can leave comments on questions/solutions and earn 5 coins when others upvote their comments.

### 3. Lesson/Course System
Users can complete learning modules and earn 1 coin per lesson.

### 4. Referral System
Users can invite friends and earn 100 coins when the friend successfully completes their first interview.

### 5. Badges & Milestones
Visual badges for achieving certain milestones (e.g., "Completed 10 interviews", "Top 100 on leaderboard").

### 6. Coin Shop
Spend coins on premium features, profile customization, or priority matching.

### 7. Daily Streaks
Bonus coins for consecutive days of activity.

### 8. Leaderboard Time Filters
- Last 7 days
- Last 30 days
- All-time (currently implemented)

### 9. Achievement Notifications
Real-time toast notifications when earning coins.

### 10. Background Rank Refresh
Scheduled job to periodically recalculate leaderboard ranks (currently calculated on-demand).

---

## Configuration

### Backend Configuration (appsettings.Development.json)
```json
{
  "Development": {
    "AutoVerifyEmails": true
  }
}
```

### Docker Environment Variables (docker-compose.yml)
```yaml
environment:
  - Development__AutoVerifyEmails=true
```

---

## Files Modified/Created

### Backend Files Created:
- `Models/UserCoins.cs`
- `Models/CoinTransaction.cs`
- `Models/AchievementDefinition.cs`
- `Constants/AchievementTypes.cs`
- `Services/ICoinService.cs`
- `Services/CoinService.cs`
- `Controllers/CoinsController.cs`
- `DTOs/Coins/UserCoinsDto.cs`
- `DTOs/Coins/CoinTransactionDto.cs`
- `DTOs/Coins/LeaderboardEntryDto.cs`
- `DTOs/Coins/AchievementDefinitionDto.cs`
- `DTOs/Coins/AwardCoinsRequest.cs`
- Database migration for coins tables

### Backend Files Modified:
- `Data/ApplicationDbContext.cs` - Added DbSets and entity configurations
- `Data/DbInitializer.cs` - Added achievement seeding
- `Data/DbSeeder.cs` - Integrated achievement seeding
- `Services/PeerInterviewService.cs` - Award coins on interview completion, feedback submission, question usage
- `Services/InterviewMatchingService.cs` - Award coins when joining matched interviews
- `Services/QuestionService.cs` - Award coins on question approval
- `Services/UserService.cs` - Award coins on profile completion
- `Services/AuthService.cs` - Auto-verify emails in development
- `Program.cs` - Registered CoinService
- `appsettings.Development.json` - Added AutoVerifyEmails setting

### Frontend Files Created:
- `services/coins.service.ts`
- `pages/leaderboard/LeaderboardAndEarnPage.tsx` (combined page)

### Frontend Files Modified:
- `components/layout/Navbar.tsx` - Display coin count
- `pages/profile/ProfilePage.tsx` - Added Activity & Coins tab
- `utils/constants.ts` - Added leaderboard routes
- `App.tsx` - Added leaderboard routing
- `styles/dashboard.css` - Coins display styling

### Documentation:
- `STAGE2_IMPLEMENTATION.md` - Added Week 7 plan, progress tracking, and bugs
- `COINS_SYSTEM_SUMMARY.md` - This document

---

## Support & Questions

If you encounter any issues or have questions about the coins system:
1. Check the "Known Bugs & Issues to Fix" section in `STAGE2_IMPLEMENTATION.md`
2. Review this summary document
3. Check Docker logs: `docker logs vector-backend --tail 100`
4. Verify database state using PostgreSQL client

---

**Last Updated:** January 20, 2026  
**Version:** 1.0.0  
**Status:** Deployed to local Docker ✅
