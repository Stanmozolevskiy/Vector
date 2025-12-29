-- Clear all peer interview related data
DELETE FROM "InterviewFeedbacks";
DELETE FROM "LiveInterviewParticipants";
DELETE FROM "LiveInterviewSessions";
DELETE FROM "InterviewMatchingRequests";
DELETE FROM "ScheduledInterviewSessions";

-- Reset sequences if needed (PostgreSQL auto-increments use sequences)
-- Note: These tables use GUIDs, so no sequences to reset

