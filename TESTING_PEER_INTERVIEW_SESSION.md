# Testing Guide: Peer Interview Session Page

## Overview
This guide explains how to test the new peer interview session page with video chat and collaborative code editing features.

## Prerequisites

1. **Backend Running**
   ```powershell
   cd docker
   docker-compose up -d backend
   ```

2. **Frontend Running**
   ```powershell
   cd frontend
   npm run dev
   ```
   Frontend should be available at `http://localhost:5173`

3. **Database Migrations Applied**
   - Migrations run automatically on backend startup
   - Ensure PostgreSQL container is running

4. **User Account**
   - You need at least one registered user account
   - For full testing, you may want two accounts (interviewer + interviewee)

## Testing Steps

### Step 1: Create a Peer Interview Session

1. **Navigate to Find Peer Page**
   - Go to: `http://localhost:5173/peer-interviews/find`
   - Or click "Mock Interviews" in the navigation bar

2. **Schedule an Interview**
   - Click "Schedule peer mock interview" button
   - Follow the multi-step modal:
     - **Step 1:** Select interview type (e.g., "Data Structures & Algorithms")
     - **Step 2:** Select practice type (e.g., "Practice with peers")
     - **Step 3:** Choose interview level (Beginner, Intermediate, or Advanced)
     - **Step 4:** Select a time slot
   - Complete the scheduling process

3. **View Scheduled Session**
   - After scheduling, you'll see the session in "Upcoming interviews" table
   - Note the session ID or click to view details

### Step 2: Access the Session Page

**Option A: From Find Peer Page**
- Find your session in "Upcoming interviews" table
- Click on the session (if there's a link) or navigate manually

**Option B: Direct URL**
- Format: `http://localhost:5173/peer-interviews/sessions/{sessionId}`
- Replace `{sessionId}` with your actual session ID (GUID)

**Option C: From Dashboard**
- Go to Dashboard
- Click "Schedule Interview" button in "Upcoming Mock Interviews" card
- This navigates to the find peer page

### Step 3: Test Session States

#### A. Scheduled State (Before Starting)
- **Expected Behavior:**
  - Session header shows "Scheduled" status
  - Question panel displays assigned question (if level was selected)
  - Video chat panel is **NOT visible**
  - Editor shows placeholder: "Start the interview to begin collaborative coding"
  - "Start Interview" button is visible

- **What to Test:**
  - Verify question is displayed correctly
  - Verify "View Full Question" button works
  - Verify placeholder message appears

#### B. InProgress State (After Starting)
1. **Click "Start Interview" button**
   - Session status should change to "InProgress"
   - Timer should start counting down

2. **Expected Behavior:**
   - Video chat panel appears at the top of right column
   - Collaborative code editor replaces placeholder
   - Collaboration status shows "Live collaboration" (green dot)
   - Timer displays remaining time

3. **What to Test:**

   **Video Chat:**
   - Camera and microphone should activate (browser will ask for permission)
   - Local video should appear in "You" box
   - Remote video should appear in "Peer" box (if peer is connected)
   - Test video toggle button (camera on/off)
   - Test audio toggle button (microphone mute/unmute)
   - Test screen sharing button
   - Check for error messages if camera/mic access is denied

   **Collaborative Code Editor:**
   - Type code in the editor
   - Code should sync in real-time (if WebSocket is connected)
   - User presence indicators should show connected users
   - Language selector should work
   - Check collaboration status indicator

   **Layout:**
   - Question panel on left
   - Video chat + Editor on right
   - Responsive design on mobile

#### C. Completed State
- Click "End Interview" button
- Session status changes to "Completed"
- Video and collaboration should stop

### Step 4: Test Error Handling

1. **Video Errors:**
   - Deny camera/microphone permission
   - Should show error message in video panel
   - Error should be user-friendly

2. **Collaboration Errors:**
   - WebSocket connection will fail (backend not implemented yet)
   - Should show "Collaboration offline" status
   - Editor should still work locally (just no real-time sync)

3. **Network Errors:**
   - Disconnect internet
   - Check error handling and user feedback

## Current Limitations

### ⚠️ Known Issues

1. **WebSocket Backend Not Implemented**
   - The `CollaborativeCodeEditor` tries to connect to `ws://localhost:5000/api/collaboration/{sessionId}`
   - This endpoint doesn't exist yet
   - **Expected Behavior:** Collaboration will show "offline" status
   - **Workaround:** Editor still works locally, just no real-time sync

2. **WebRTC Signaling Not Implemented**
   - The `VideoChat` component initializes WebRTC but needs signaling server
   - **Expected Behavior:** Local video works, remote video won't connect
   - **Workaround:** Test with two browser windows/tabs as different users

3. **Peer Connection**
   - For full testing, you need two users in the same session
   - Currently, you can test as a single user

## Testing with Two Users

### Setup
1. Create two user accounts (or use two browsers in incognito mode)
2. Schedule an interview with one account
3. Have the second user join the same session (if there's a way to do this)

### What to Test
- Video chat between two users
- Real-time code synchronization
- Cursor positions (when implemented)
- User presence indicators

## Next Steps for Full Implementation

### Backend Tasks (Required for Full Functionality)

1. **WebSocket/SignalR Hub for Collaboration**
   - Create `/api/collaboration/{sessionId}` WebSocket endpoint
   - Handle code change messages
   - Broadcast changes to all connected users
   - Handle user join/leave events

2. **WebRTC Signaling Server**
   - Create signaling endpoint for video chat
   - Handle ICE candidates exchange
   - Manage peer connections

3. **Session Management**
   - Ensure only session participants can join
   - Handle session state changes
   - Clean up connections on session end

### Frontend Tasks (Optional Improvements)

1. **Better Error Messages**
   - More specific error messages for different failure types
   - Retry mechanisms for failed connections

2. **Connection Status**
   - Better visual indicators for connection quality
   - Reconnection logic

3. **Cursor Tracking**
   - Implement cursor position tracking in CodeEditor
   - Display remote cursors in CollaborativeCodeEditor

## Quick Test Checklist

- [ ] Can navigate to find peer page
- [ ] Can schedule an interview
- [ ] Can access session page
- [ ] Question is displayed correctly
- [ ] "Start Interview" button works
- [ ] Video chat appears when session starts
- [ ] Camera/microphone permissions work
- [ ] Video controls (toggle video/audio) work
- [ ] Screen sharing works
- [ ] Collaborative editor appears when session starts
- [ ] Code editor works locally
- [ ] Language selector works
- [ ] Collaboration status shows correctly
- [ ] Error messages display appropriately
- [ ] "End Interview" button works
- [ ] Layout is responsive on mobile

## Troubleshooting

### Video Not Working
- Check browser permissions for camera/microphone
- Ensure HTTPS or localhost (required for getUserMedia)
- Check browser console for errors

### Collaboration Not Working
- **Expected:** WebSocket connection will fail (backend not implemented)
- Check browser console for WebSocket errors
- Collaboration status should show "offline"

### Session Not Loading
- Check backend is running
- Check database connection
- Verify session ID is correct
- Check browser console for API errors

### Layout Issues
- Clear browser cache
- Check CSS is loading correctly
- Verify responsive design on different screen sizes

## Support

If you encounter issues:
1. Check browser console for errors
2. Check backend logs
3. Verify all services are running (backend, frontend, database)
4. Review this guide for known limitations

