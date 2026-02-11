import { useState, useEffect } from 'react';
import { questionVotesService } from '../services/questionVotes.service';
import { useAuth } from '../hooks/useAuth';

interface QuestionVotingProps {
  questionId: string;
}

export const QuestionVoting = ({ questionId }: QuestionVotingProps) => {
  const { user } = useAuth();
  const [voteCount, setVoteCount] = useState<number>(0);
  const [myVote, setMyVote] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(false);

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

  return (
    <div className="question-voting" style={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      gap: '0.5rem',
      padding: '0.5rem',
      border: '1px solid #e0e0e0',
      borderRadius: '8px',
      backgroundColor: '#f9f9f9'
    }}>
      <button
        onClick={() => handleVote(1)}
        disabled={isLoading || !user}
        className={`vote-button ${myVote === 1 ? 'active' : ''}`}
        style={{
          background: 'none',
          border: 'none',
          cursor: user ? 'pointer' : 'not-allowed',
          fontSize: '1.5rem',
          padding: '0.25rem',
          color: myVote === 1 ? '#4CAF50' : '#666',
          transition: 'color 0.2s'
        }}
        title="Upvote this question"
      >
        ▲
      </button>
      
      <div style={{
        fontSize: '1.25rem',
        fontWeight: 'bold',
        color: voteCount > 0 ? '#4CAF50' : voteCount < 0 ? '#f44336' : '#666'
      }}>
        {voteCount}
      </div>
      
      <button
        onClick={() => handleVote(-1)}
        disabled={isLoading || !user}
        className={`vote-button ${myVote === -1 ? 'active' : ''}`}
        style={{
          background: 'none',
          border: 'none',
          cursor: user ? 'pointer' : 'not-allowed',
          fontSize: '1.5rem',
          padding: '0.25rem',
          color: myVote === -1 ? '#f44336' : '#666',
          transition: 'color 0.2s'
        }}
        title="Downvote this question"
      >
        ▼
      </button>
    </div>
  );
};
