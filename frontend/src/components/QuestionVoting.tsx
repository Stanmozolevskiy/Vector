import { useState, useEffect } from 'react';
import { questionVotesService } from '../services/questionVotes.service';
import { useAuth } from '../hooks/useAuth';

interface QuestionVotingProps {
  questionId: string;
  commentCount?: number;
  onCommentsClick?: () => void;
}

export const QuestionVoting = ({ questionId, commentCount = 0, onCommentsClick }: QuestionVotingProps) => {
  const { user } = useAuth();
  const [voteCount, setVoteCount] = useState<number>(0);
  const [myVote, setMyVote] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isStarred, setIsStarred] = useState(false);

  useEffect(() => {
    loadVotingData();
  }, [questionId]);

  const loadVotingData = async () => {
    try {
      const [count, vote] = await Promise.all([
        questionVotesService.getVoteCount(questionId),
        user ? questionVotesService.getMyVote(questionId) : Promise.resolve(null)
      ]);
      setVoteCount(count);
      setMyVote(vote);
    } catch (error) {
      console.error('Error loading voting data:', error);
    }
  };

  const handleVote = async (voteType: number) => {
    if (!user) {
      alert('Please log in to vote');
      return;
    }

    if (isLoading) return;

    setIsLoading(true);
    try {
      if (myVote === voteType) {
        // Remove vote if clicking same button
        await questionVotesService.removeVote(questionId);
        setMyVote(null);
        setVoteCount(prev => prev - voteType);
      } else {
        // Vote or change vote
        const result = await questionVotesService.voteQuestion(questionId, voteType);
        setMyVote(voteType);
        setVoteCount(result.voteCount);
      }
    } catch (error: any) {
      console.error('Error voting:', error);
      alert(error.response?.data?.message || 'Failed to vote');
    } finally {
      setIsLoading(false);
    }
  };

  const formatCount = (count: number): string => {
    if (count >= 1000000) {
      return `${(count / 1000000).toFixed(1)}M`;
    }
    if (count >= 1000) {
      return `${(count / 1000).toFixed(1)}K`;
    }
    return count.toString();
  };

  const handleStar = () => {
    if (!user) {
      alert('Please log in to star questions');
      return;
    }
    setIsStarred(!isStarred);
    // TODO: Implement backend API for starring questions
  };

  const handleShare = async () => {
    const url = window.location.href;
    try {
      if (navigator.share) {
        await navigator.share({
          title: 'Share Question',
          url: url
        });
      } else {
        await navigator.clipboard.writeText(url);
        alert('Link copied to clipboard!');
      }
    } catch (error) {
      console.error('Error sharing:', error);
    }
  };

  return (
    <div className="question-voting-actions" style={{
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      gap: '0.5rem',
      padding: '0.25rem 1rem',
      backgroundColor: '#fff',
      borderTop: '1px solid #e0e0e0',
      flexShrink: 0
    }}>
      {/* Left side: voting and action buttons */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
        {/* Thumbs Up/Down Group */}
        <div style={{ display: 'flex', borderRadius: '6px', overflow: 'hidden', border: '1px solid #e0e0e0' }}>
          {/* Thumbs Up */}
          <button
            onClick={() => handleVote(1)}
            disabled={isLoading || !user}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.4rem 0.75rem',
              border: 'none',
              borderRight: '1px solid #e0e0e0',
              borderRadius: 0,
              backgroundColor: myVote === 1 ? '#e8f5e9' : 'transparent',
              cursor: user ? 'pointer' : 'not-allowed',
              fontSize: '0.875rem',
              fontWeight: '500',
              color: myVote === 1 ? '#4CAF50' : '#666',
              transition: 'all 0.2s',
              opacity: !user ? 0.6 : 1
            }}
            title="Upvote this question"
            onMouseEnter={(e) => {
              if (user && myVote !== 1) {
                e.currentTarget.style.backgroundColor = '#f5f5f5';
              }
            }}
            onMouseLeave={(e) => {
              if (myVote !== 1) {
                e.currentTarget.style.backgroundColor = 'transparent';
              }
            }}
          >
            <i className="far fa-thumbs-up" style={{ fontSize: '0.875rem' }}></i>
            <span>{formatCount(Math.max(0, voteCount))}</span>
          </button>

          {/* Thumbs Down */}
          <button
            onClick={() => handleVote(-1)}
            disabled={isLoading || !user}
            style={{
              display: 'flex',
              alignItems: 'center',
              padding: '0.4rem 0.75rem',
              border: 'none',
              borderRadius: 0,
              backgroundColor: myVote === -1 ? '#ffebee' : 'transparent',
              cursor: user ? 'pointer' : 'not-allowed',
              fontSize: '0.875rem',
              color: myVote === -1 ? '#f44336' : '#666',
              transition: 'all 0.2s',
              opacity: !user ? 0.6 : 1
            }}
            title="Downvote this question"
            onMouseEnter={(e) => {
              if (user && myVote !== -1) {
                e.currentTarget.style.backgroundColor = '#f5f5f5';
              }
            }}
            onMouseLeave={(e) => {
              if (myVote !== -1) {
                e.currentTarget.style.backgroundColor = 'transparent';
              }
            }}
          >
            <i className="far fa-thumbs-down"></i>
          </button>
        </div>

        {/* Comments */}
        <button
          onClick={onCommentsClick}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.35rem',
            padding: '0.4rem 0.75rem',
            border: 'none',
            borderRadius: '6px',
            backgroundColor: 'transparent',
            cursor: 'pointer',
            fontSize: '0.875rem',
            fontWeight: '500',
            color: '#666',
            transition: 'all 0.2s'
          }}
          title="View comments"
          onMouseEnter={(e) => {
            e.currentTarget.style.backgroundColor = '#f5f5f5';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.backgroundColor = 'transparent';
          }}
        >
          <i className="far fa-comment" style={{ fontSize: '0.875rem' }}></i>
          <span>{formatCount(commentCount)}</span>
        </button>

        {/* Separator */}
        <div style={{ height: '1rem', width: '1px', backgroundColor: '#e0e0e0' }}></div>

        {/* Star */}
        <button
          onClick={handleStar}
          disabled={!user}
          style={{
            display: 'flex',
            alignItems: 'center',
            padding: '0.4rem 0.75rem',
            border: 'none',
            borderRadius: '6px',
            backgroundColor: isStarred ? '#fff9e6' : 'transparent',
            cursor: user ? 'pointer' : 'not-allowed',
            fontSize: '0.875rem',
            color: isStarred ? '#FFA000' : '#666',
            transition: 'all 0.2s',
            opacity: !user ? 0.6 : 1
          }}
          title="Star this question"
          onMouseEnter={(e) => {
            if (user) {
              e.currentTarget.style.backgroundColor = isStarred ? '#fff9e6' : '#f5f5f5';
            }
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.backgroundColor = isStarred ? '#fff9e6' : 'transparent';
          }}
        >
          <i className={`fa${isStarred ? 's' : 'r'} fa-star`}></i>
        </button>

        {/* Share */}
        <button
          onClick={handleShare}
          style={{
            display: 'flex',
            alignItems: 'center',
            padding: '0.4rem 0.75rem',
            border: 'none',
            borderRadius: '6px',
            backgroundColor: 'transparent',
            cursor: 'pointer',
            fontSize: '0.875rem',
            color: '#666',
            transition: 'all 0.2s'
          }}
          title="Share this question"
          onMouseEnter={(e) => {
            e.currentTarget.style.backgroundColor = '#f5f5f5';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.backgroundColor = 'transparent';
          }}
        >
          <i className="far fa-arrow-up-right-from-square"></i>
        </button>

        {/* Info */}
        <button
          style={{
            display: 'flex',
            alignItems: 'center',
            padding: '0.4rem 0.75rem',
            border: 'none',
            borderRadius: '6px',
            backgroundColor: 'transparent',
            cursor: 'pointer',
            fontSize: '0.875rem',
            color: '#666',
            transition: 'all 0.2s'
          }}
          title="Question information"
          onMouseEnter={(e) => {
            e.currentTarget.style.backgroundColor = '#f5f5f5';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.backgroundColor = 'transparent';
          }}
        >
          <i className="far fa-circle-question"></i>
        </button>
      </div>

      {/* Right side: Online indicator */}
      <div style={{ 
        display: 'flex', 
        alignItems: 'center', 
        gap: '0.35rem',
        fontSize: '0.75rem',
        color: '#666',
        fontWeight: '500'
      }}>
        <div style={{ 
          width: '0.375rem', 
          height: '0.375rem', 
          borderRadius: '50%', 
          backgroundColor: '#4CAF50' 
        }}></div>
        <span>Online</span>
      </div>
    </div>
  );
};
