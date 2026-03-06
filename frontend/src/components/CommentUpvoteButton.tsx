import { useState } from 'react';
import { commentsService } from '../services/comments.service';
import { useAuth } from '../hooks/useAuth';

interface CommentUpvoteButtonProps {
  commentId: string;
  initialUpvotes: number;
  onUpvoteChange?: () => void;
}

export const CommentUpvoteButton = ({ 
  commentId, 
  initialUpvotes, 
  onUpvoteChange 
}: CommentUpvoteButtonProps) => {
  const { user } = useAuth();
  const [upvotes, setUpvotes] = useState(initialUpvotes);
  const [isUpvoted, setIsUpvoted] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleUpvote = async () => {
    if (!user) {
      alert('Please log in to upvote comments');
      return;
    }

    if (isLoading) return;

    setIsLoading(true);
    try {
      if (isUpvoted) {
        await commentsService.removeUpvote(commentId);
        setUpvotes(prev => prev - 1);
        setIsUpvoted(false);
      } else {
        await commentsService.upvoteComment(commentId);
        setUpvotes(prev => prev + 1);
        setIsUpvoted(true);
      }
      onUpvoteChange?.();
    } catch (error: any) {
      console.error('Error upvoting comment:', error);
      if (error.response?.status === 400) {
        // Already upvoted - toggle state
        setIsUpvoted(!isUpvoted);
      } else {
        alert(error.response?.data?.message || 'Failed to upvote comment');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <button
      onClick={handleUpvote}
      disabled={isLoading || !user}
      className={`comment-upvote-btn ${isUpvoted ? 'upvoted' : ''}`}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '0.25rem',
        padding: '0.25rem 0.5rem',
        border: '1px solid #ddd',
        borderRadius: '4px',
        backgroundColor: isUpvoted ? '#e3f2fd' : 'white',
        color: isUpvoted ? '#1976d2' : '#666',
        cursor: user ? 'pointer' : 'not-allowed',
        fontSize: '0.875rem',
        transition: 'all 0.2s'
      }}
      title={user ? (isUpvoted ? 'Remove upvote' : 'Upvote this comment') : 'Login to upvote'}
    >
      <span style={{ fontSize: '1rem' }}>👍</span>
      <span>{upvotes}</span>
    </button>
  );
};
