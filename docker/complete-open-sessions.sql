-- Mark all InProgress and Scheduled sessions as Completed
UPDATE "PeerInterviewSessions"
SET "Status" = 'Completed', "UpdatedAt" = NOW()
WHERE "Status" IN ('InProgress', 'Scheduled');

-- Mark all Active participants as Completed
UPDATE "UserSessionParticipants"
SET "Status" = 'Completed', "UpdatedAt" = NOW()
WHERE "Status" = 'Active';

-- Show results
SELECT COUNT(*) as completed_sessions FROM "PeerInterviewSessions" WHERE "Status" = 'Completed';
SELECT COUNT(*) as completed_participants FROM "UserSessionParticipants" WHERE "Status" = 'Completed';




