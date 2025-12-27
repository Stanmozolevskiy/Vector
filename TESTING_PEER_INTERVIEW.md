# Testing Guide: Peer Interview Real-time Collaboration & Video

This guide explains how to test the real-time collaboration and video chat features for peer interviews.

## Prerequisites

1. **Two User Accounts**: You need two separate user accounts to test the peer interview functionality
2. **Two Browser Windows/Devices**: Use two different browser windows (or devices) to simulate two users
3. **Local Development Environment**: Ensure the backend and frontend are running locally

## Setup Steps

### 1. Create Two User Accounts

1. Open the application in your first browser window
2. Register a new user account (e.g., `user1@test.com`)
3. Verify the email (or use the verification token from backend logs)
4. Log in with the first account

5. Open the application in a **second browser window** (or use incognito/private mode)
6. Register a second user account (e.g., `user2@test.com`)
7. Verify and log in with the second account

### 2. Schedule an Interview Session

1. **As User 1 (Interviewer)**:
   - Navigate to `/peer-interviews/find`
   - Click "Schedule peer mock interview"
   - Select interview type: "Data Structures & Algorithms"
   - Select practice type: "Practice with peers"
   - Choose interview level: "Beginner" (or any level)
   - Select a time slot (preferably a time that's soon or in the past for testing)
   - Complete the scheduling flow
   - Note the session ID from the confirmation page or URL

2. **As User 2 (Interviewee)**:
   - The interview should appear in the "Upcoming interviews" section
   - Wait for the interview time, or use the "Start interview" button if available (dev mode)

### 3. Start the Interview Session

1. **As User 1 (Interviewer)**:
   - Navigate to `/peer-interviews/find`
   - Find the scheduled interview in "Upcoming interviews"
   - Click "Start interview" button (or wait until 10 minutes before the session)
   - You will be redirected to the question detail page with `?session={sessionId}` in the URL

2. **As User 2 (Interviewee)**:
   - Navigate to `/peer-interviews/find`
   - Find the same interview in "Upcoming interviews"
   - Click "Start interview" button
   - You will be redirected to the same question detail page with `?session={sessionId}` in the URL

## Testing Real-time Code Collaboration (SignalR)

### Test 1: Code Synchronization

1. **As User 1 (Interviewer)**:
   - Open the question detail page with the session
   - You should see "Interviewer" role indicator in the navigation bar
   - Type some code in the code editor
   - Wait 1-2 seconds

2. **As User 2 (Interviewee)**:
   - Open the same question detail page with the same session
   - You should see "Interviewee" role indicator
   - **Expected**: The code you typed as User 1 should appear in User 2's editor within 1-2 seconds

3. **Reverse Test**:
   - As User 2, type some code
   - **Expected**: The code should appear in User 1's editor

### Test 2: User Presence Indicators

1. **As User 1**:
   - Check the top of the code editor
   - **Expected**: You should see a presence indicator showing User 2's name/avatar

2. **As User 2**:
   - Check the top of the code editor
   - **Expected**: You should see a presence indicator showing User 1's name/avatar

### Test 3: Connection Recovery

1. **As User 1**:
   - Type some code
   - Close the browser tab (simulating connection loss)
   - Reopen the question detail page

2. **As User 2**:
   - **Expected**: User 1's presence indicator should disappear, then reappear when User 1 reconnects
   - Code changes should resume syncing

## Testing Video Chat (WebRTC)

### Test 1: Video Connection

1. **As User 1**:
   - Start the interview session
   - **Expected**: A draggable video window should appear showing:
     - Your local video feed (muted, labeled "You")
     - Remote video feed (labeled "Peer") - initially empty until User 2 connects

2. **As User 2**:
   - Start the interview session
   - **Expected**: A draggable video window should appear showing:
     - Your local video feed (muted, labeled "You")
     - Remote video feed showing User 1's video (labeled "Peer")

### Test 2: Video Controls

1. **As User 1**:
   - Click the video toggle button (camera icon)
   - **Expected**: Your video should turn off/on
   - User 2 should see your video turn off/on

2. **As User 2**:
   - Click the audio toggle button (microphone icon)
   - **Expected**: Your audio should mute/unmute
   - User 1 should hear your audio mute/unmute

### Test 3: Screen Sharing

1. **As User 1**:
   - Click the screen share button
   - Select a window or screen to share
   - **Expected**: Your video feed should switch to the shared screen
   - User 2 should see your shared screen

2. **As User 1**:
   - Stop sharing (click the screen share button again or close the share dialog)
   - **Expected**: Video should switch back to your camera feed

### Test 4: Draggable Video Window

1. **As User 1**:
   - Click and drag the video window header
   - **Expected**: The video window should move around the screen
   - The window should stay within the viewport boundaries

2. **As User 1**:
   - Click the minimize button
   - **Expected**: The video window should minimize to a small header
   - Click the expand button to restore

## Testing Role-Based Features

### Test 1: Interviewee Cannot See Hints/Solutions

1. **As User 2 (Interviewee)**:
   - Open the question detail page with the session
   - Check the tabs in the description panel
   - **Expected**: Only "Description" tab should be visible
   - "Hints" and "Solution" tabs should NOT be visible

2. **As User 1 (Interviewer)**:
   - Open the same question detail page
   - Check the tabs
   - **Expected**: All three tabs should be visible: "Description", "Hints", "Solution"

### Test 2: Change Question (Interviewer Only)

1. **As User 1 (Interviewer)**:
   - Click the "Change Question" button (sync icon) in the navigation bar
   - **Expected**: A new question should be assigned and the page should reload

2. **As User 2 (Interviewee)**:
   - Check if the "Change Question" button is visible
   - **Expected**: The button should NOT be visible (only interviewer can change questions)

### Test 3: Switch Roles

1. **As User 1 (Interviewer)**:
   - Click the "Switch Role" button (exchange icon)
   - **Expected**: Roles should swap, User 1 becomes Interviewee, User 2 becomes Interviewer
   - A new question should be assigned to the new interviewer

2. **As User 2 (Now Interviewer)**:
   - Check the tabs
   - **Expected**: "Hints" and "Solution" tabs should now be visible
   - The "Change Question" button should now be visible

## Testing Edge Cases

### Test 1: Unauthorized Access

1. **As User 3 (Different User)**:
   - Try to access a session URL: `/questions/{questionId}?session={sessionId}`
   - **Expected**: Should be denied access or redirected (session should only be accessible to interviewer/interviewee)

### Test 2: Session Status Transitions

1. **As User 1**:
   - Start a "Scheduled" session
   - **Expected**: Session status should automatically change to "InProgress"

2. **As User 1**:
   - Cancel the session
   - **Expected**: Both users should be able to cancel "InProgress" sessions

### Test 3: Multiple Tabs

1. **As User 1**:
   - Open the same session in two browser tabs
   - **Expected**: Both tabs should connect, but only one should be active (last one)

## Troubleshooting

### Code Not Syncing

- **Check Browser Console**: Open DevTools (F12) and check for errors
- **Check Network Tab**: Verify WebSocket/SignalR connection is established
- **Check Backend Logs**: Look for SignalR connection errors
- **Verify Authentication**: Ensure both users are logged in with valid tokens

### Video Not Connecting

- **Check Browser Permissions**: Ensure camera/microphone permissions are granted
- **Check Browser Console**: Look for WebRTC errors
- **Check Network Tab**: Verify signaling endpoints are being called
- **Check Backend Logs**: Look for video session creation errors
- **Try Different Browsers**: Some browsers have different WebRTC support

### Connection Drops

- **Check Internet Connection**: WebRTC requires stable internet
- **Check Firewall**: Ensure WebRTC ports are not blocked
- **Check STUN Servers**: Verify STUN server connectivity
- **Check Backend Logs**: Look for connection timeout errors

## Expected Behavior Summary

| Feature | Interviewer | Interviewee |
|---------|------------|-------------|
| See Description Tab | ✅ | ✅ |
| See Hints Tab | ✅ | ❌ |
| See Solution Tab | ✅ | ❌ |
| Edit Code | ✅ | ✅ |
| See Code Changes | ✅ | ✅ |
| Change Question | ✅ | ❌ |
| Switch Roles | ✅ | ✅ |
| Video Chat | ✅ | ✅ |
| Screen Share | ✅ | ✅ |

## Next Steps

After testing, if you find any issues:
1. Check browser console for errors
2. Check backend logs for server-side errors
3. Verify database session records
4. Test with different browsers
5. Test with different network conditions

## Notes

- **Development Mode**: The "Start interview" button always appears in dev mode for easier testing
- **Session Time**: In production, sessions can only be started 10 minutes before the scheduled time
- **Authentication**: Both users must be authenticated to join a session
- **Session Ownership**: Only the interviewer and interviewee can access a session





