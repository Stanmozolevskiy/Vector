import { useState } from 'react';
import { useAuth } from '../hooks/useAuth';
import { commentsService } from '../services/comments.service';
import type { QuestionComment } from '../services/question.service';

interface QuestionDiscussionProps {
  questionId: string;
  comments: QuestionComment[];
  onCommentAdded: () => void;
}

type CommentType = 'feedback' | 'tip' | 'question';

// Helper function to get user initials
const getInitials = (name: string): string => {
  if (!name) return '?';
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
  }
  return name.substring(0, 2).toUpperCase();
};

export const QuestionDiscussion = ({ questionId, comments, onCommentAdded }: QuestionDiscussionProps) => {
  const { user } = useAuth();
  const [isExpanded, setIsExpanded] = useState(false);
  const [showRules, setShowRules] = useState(true);
  const [commentText, setCommentText] = useState('');
  const [commentType, setCommentType] = useState<CommentType | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  // Reply management
  const [openReplyForCommentId, setOpenReplyForCommentId] = useState<string | null>(null);
  const [replyDraftByCommentId, setReplyDraftByCommentId] = useState<Record<string, string>>({});
  const [isPostingReplyForCommentId, setIsPostingReplyForCommentId] = useState<string | null>(null);
  
  // Expanded replies tracking
  const [expandedRepliesByCommentId, setExpandedRepliesByCommentId] = useState<Record<string, boolean>>({});

  const handleSubmitComment = async () => {
    if (!user) {
      alert('Please log in to comment');
      return;
    }

    if (!commentText.trim()) {
      alert('Please enter a comment');
      return;
    }

    if (!commentType) {
      alert('Please choose a comment type');
      return;
    }

    console.log('Submitting comment:', { questionId, commentText, commentType });
    setIsSubmitting(true);
    try {
      const result = await commentsService.createComment(questionId, commentText, commentType);
      console.log('Comment created successfully:', result);
      setCommentText('');
      setCommentType(null);
      onCommentAdded();
    } catch (error) {
      console.error('Error submitting comment:', error);
      alert('Failed to submit comment: ' + (error as any)?.message || 'Unknown error');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleToggleUpvote = async (commentId: string) => {
    if (!user) {
      alert('Please log in to vote');
      return;
    }

    try {
      const comment = comments.find(c => c.id === commentId);
      if (!comment) return;

      if (comment.hasUpvoted) {
        await commentsService.removeUpvote(commentId);
      } else {
        await commentsService.upvoteComment(commentId);
      }
      onCommentAdded(); // Refresh comments
    } catch (error) {
      console.error('Error toggling upvote:', error);
      alert('Failed to update vote');
    }
  };

  const handleToggleDownvote = async (commentId: string) => {
    if (!user) {
      alert('Please log in to vote');
      return;
    }

    try {
      const comment = comments.find(c => c.id === commentId);
      if (!comment) return;

      // For now, we'll check if there's a downvote by checking the votes array
      // This is a simplified approach - you may need to adjust based on your backend
      await commentsService.downvoteComment(commentId);
      onCommentAdded(); // Refresh comments
    } catch (error) {
      console.error('Error toggling downvote:', error);
      alert('Failed to update vote');
    }
  };

  const handleOpenReply = (commentId: string) => {
    setOpenReplyForCommentId(commentId);
  };

  const handleAddReply = async (parentCommentId: string) => {
    if (!user) {
      alert('Please log in to reply');
      return;
    }

    const replyText = replyDraftByCommentId[parentCommentId]?.trim();
    if (!replyText) {
      alert('Please enter a reply');
      return;
    }

    setIsPostingReplyForCommentId(parentCommentId);
    try {
      await commentsService.createComment(questionId, replyText, 'feedback', parentCommentId);
      setReplyDraftByCommentId((prev) => ({ ...prev, [parentCommentId]: '' }));
      setOpenReplyForCommentId(null);
      onCommentAdded();
    } catch (error) {
      console.error('Error submitting reply:', error);
      alert('Failed to submit reply');
    } finally {
      setIsPostingReplyForCommentId(null);
    }
  };

  const handleToggleReplies = (commentId: string) => {
    setExpandedRepliesByCommentId((prev) => ({
      ...prev,
      [commentId]: !prev[commentId]
    }));
  };

  const getCommentTypeLabel = (type: string): { label: string; color: string } => {
    switch (type) {
      case 'feedback':
        return { label: 'Feedback', color: '#10B981' };
      case 'tip':
        return { label: 'Tip', color: '#3B82F6' };
      case 'question':
        return { label: 'Ask Question', color: '#F59E0B' };
      default:
        return { label: 'Comment', color: '#6B7280' };
    }
  };

  // Get top-level comments (not replies)
  const topLevelComments = comments.filter(c => !c.parentCommentId);

  // Get replies for a comment
  const getReplies = (commentId: string) => {
    return comments.filter(c => c.parentCommentId === commentId);
  };

  return (
    <div 
      id="discussion-section"
      className="stats-section collapsible-stats-section"
      style={{ 
        marginTop: '1rem',
        flexDirection: 'column',
        alignItems: 'stretch'
      }}
    >
      {/* Collapsible Header - Always visible */}
      <div 
        className="stat-item stat-item-collapsible"
        onClick={() => setIsExpanded(!isExpanded)}
        style={{ cursor: 'pointer', display: 'flex', flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}
      >
        <span className="stat-label" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <i className="fa-regular fa-comment" style={{ color: '#666' }}></i>
          Discussion ({comments.length})
        </span>
        <i className={`fa-solid fa-chevron-${isExpanded ? 'up' : 'down'} collapse-icon`}></i>
      </div>
        
      {/* Expanded Content */}
      {isExpanded && (
        <div 
          onClick={(e) => e.stopPropagation()} 
          style={{ 
            marginTop: '1rem',
            width: '100%',
            boxSizing: 'border-box'
          }}
        >
          {/* Discussion Rules Warning */}
          {showRules && (
            <div 
              onClick={(e) => e.stopPropagation()}
              style={{
                backgroundColor: '#FFF9E6',
                border: '1px solid #FFA000',
                borderRadius: '6px',
                padding: '1rem',
                marginBottom: '1rem',
                position: 'relative'
              }}
            >
              <button
                onClick={() => setShowRules(false)}
                style={{
                  position: 'absolute',
                  top: '0.75rem',
                  right: '0.75rem',
                  background: 'none',
                  border: 'none',
                  cursor: 'pointer',
                  color: '#666',
                  fontSize: '1.125rem'
                }}
                title="Close"
              >
                ×
              </button>
              <div style={{ display: 'flex', gap: '0.75rem' }}>
                <i className="fa-solid fa-lightbulb" style={{ color: '#FFA000', marginTop: '0.125rem' }}></i>
                <div style={{ flex: 1, fontSize: '0.875rem', color: '#333' }}>
                  <div style={{ fontWeight: '600', marginBottom: '0.5rem' }}>Discussion Rules</div>
                  <ol style={{ margin: 0, paddingLeft: '1.25rem', lineHeight: '1.6' }}>
                    <li>Please don't post <strong>any solutions</strong> in this discussion.</li>
                    <li>The problem discussion is for asking questions about the problem or for sharing tips - anything except for solutions.</li>
                    <li>If you'd like to share your solution for feedback and ideas, please head to the solutions tab and post it there.</li>
                  </ol>
                </div>
              </div>
            </div>
          )}

          {/* Comment Input */}
          <div className="qa-answer-form" style={{ marginBottom: '1.5rem' }}>
            <textarea
              value={commentText}
              onChange={(e) => setCommentText(e.target.value)}
              placeholder="Type comment here..."
              disabled={!user}
              rows={4}
              style={{
                width: '100%',
                padding: '0.75rem',
                border: '1px solid #e0e0e0',
                borderRadius: '6px',
                fontSize: '0.875rem',
                fontFamily: 'inherit',
                resize: 'vertical',
                boxSizing: 'border-box'
              }}
            />
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginTop: '0.75rem' }}>
              <select
                value={commentType || ''}
                onChange={(e) => setCommentType(e.target.value as CommentType)}
                disabled={!user}
                style={{
                  padding: '0.5rem',
                  border: '1px solid #e0e0e0',
                  borderRadius: '4px',
                  fontSize: '0.875rem',
                  flex: 1
                }}
              >
                <option value="">Choose a type</option>
                <option value="feedback">Feedback</option>
                <option value="tip">Tip</option>
                <option value="question">Ask Question</option>
              </select>
              <button
                onClick={handleSubmitComment}
                disabled={!user || !commentText.trim() || !commentType || isSubmitting}
                className="qa-submit-btn"
              >
                {isSubmitting ? 'Posting...' : 'Comment'}
              </button>
            </div>
          </div>

          {/* Comments List */}
          {topLevelComments.length > 0 ? (
            <div className="qa-answer-list">
              {topLevelComments.map((comment) => {
                const replies = getReplies(comment.id);
                const typeInfo = comment.commentType ? getCommentTypeLabel(comment.commentType) : null;
                
                return (
                  <div key={comment.id} className="qa-answer-card">
                    <div className="qa-answer-header">
                      <div className="qa-avatar qa-avatar--sm">
                        {comment.userProfilePictureUrl ? (
                          <img className="qa-avatar-img qa-avatar-img--sm" src={comment.userProfilePictureUrl} alt={comment.userName || 'User'} />
                        ) : (
                          <div className="qa-avatar-fallback qa-avatar-fallback--sm">{getInitials(comment.userName)}</div>
                        )}
                      </div>
                      <div className="qa-answer-meta">
                        <div className="qa-author-name">{comment.userName || 'User'}</div>
                        <div className="qa-date">
                          {new Date(comment.createdAt).toLocaleDateString()}
                          {typeInfo && (
                            <span style={{ 
                              marginLeft: '0.5rem', 
                              padding: '0.125rem 0.5rem', 
                              backgroundColor: typeInfo.color, 
                              color: '#fff', 
                              borderRadius: '4px', 
                              fontSize: '0.75rem',
                              fontWeight: '500'
                            }}>
                              {typeInfo.label}
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                    <div className="qa-answer-body" style={{ whiteSpace: 'pre-wrap' }}>{comment.content}</div>

                    <div className="qa-answer-actions">
                      <button
                        type="button"
                        className={`qa-action-btn ${comment.hasUpvoted ? 'is-active' : ''}`}
                        onClick={() => handleToggleUpvote(comment.id)}
                        aria-label="Upvote"
                      >
                        <i className="fa-solid fa-arrow-up"></i>
                        <span>{comment.upvoteCount ?? 0}</span>
                      </button>
                      <button
                        type="button"
                        className="qa-action-btn"
                        onClick={() => handleToggleDownvote(comment.id)}
                        aria-label="Downvote"
                      >
                        <i className="fa-solid fa-arrow-down"></i>
                      </button>
                      <button
                        type="button"
                        className="qa-action-btn"
                        onClick={() => handleOpenReply(comment.id)}
                        aria-label="Reply"
                      >
                        <i className="fa-regular fa-comment"></i>
                        <span>Reply</span>
                      </button>
                      {replies.length > 0 && (
                        <button
                          type="button"
                          className="qa-action-btn"
                          onClick={() => handleToggleReplies(comment.id)}
                          aria-label="Toggle replies"
                        >
                          <i className={`fa-solid fa-chevron-${expandedRepliesByCommentId[comment.id] ? 'up' : 'down'}`}></i>
                          <span>{expandedRepliesByCommentId[comment.id] ? 'Hide' : 'View'} {replies.length} {replies.length === 1 ? 'reply' : 'replies'}</span>
                        </button>
                      )}
                    </div>

                    {openReplyForCommentId === comment.id && (
                      <div className="qa-reply-editor">
                        <div className="qa-reply-editor-inner">
                          <textarea
                            value={replyDraftByCommentId[comment.id] || ''}
                            onChange={(e) => setReplyDraftByCommentId((prev) => ({ ...prev, [comment.id]: e.target.value }))}
                            placeholder="Write a reply..."
                            rows={3}
                            style={{
                              width: '100%',
                              padding: '0.5rem',
                              border: '1px solid #e0e0e0',
                              borderRadius: '4px',
                              fontSize: '0.875rem',
                              fontFamily: 'inherit',
                              resize: 'vertical',
                              boxSizing: 'border-box'
                            }}
                          />
                          <div className="qa-reply-actions">
                            <button
                              type="button"
                              className="qa-reply-cancel"
                              onClick={() => setOpenReplyForCommentId(null)}
                            >
                              Cancel
                            </button>
                            <button
                              type="button"
                              className="qa-submit-btn"
                              onClick={() => handleAddReply(comment.id)}
                              disabled={isPostingReplyForCommentId === comment.id || !(replyDraftByCommentId[comment.id] || '').trim()}
                            >
                              {isPostingReplyForCommentId === comment.id ? 'Submitting...' : 'Reply'}
                            </button>
                          </div>
                        </div>
                      </div>
                    )}

                    {replies.length > 0 && expandedRepliesByCommentId[comment.id] && (
                      <div className="qa-replies">
                        {replies.map((reply) => (
                          <div key={reply.id} className="qa-reply-card">
                            <div className="qa-answer-header">
                              <div className="qa-avatar qa-avatar--sm">
                                {reply.userProfilePictureUrl ? (
                                  <img className="qa-avatar-img qa-avatar-img--sm" src={reply.userProfilePictureUrl} alt={reply.userName || 'User'} />
                                ) : (
                                  <div className="qa-avatar-fallback qa-avatar-fallback--sm">{getInitials(reply.userName)}</div>
                                )}
                              </div>
                              <div className="qa-answer-meta">
                                <div className="qa-author-name">{reply.userName || 'User'}</div>
                                <div className="qa-date">{new Date(reply.createdAt).toLocaleDateString()}</div>
                              </div>
                            </div>
                            <div className="qa-answer-body" style={{ whiteSpace: 'pre-wrap' }}>{reply.content}</div>
                            <div className="qa-answer-actions">
                              <button
                                type="button"
                                className={`qa-action-btn ${reply.hasUpvoted ? 'is-active' : ''}`}
                                onClick={() => handleToggleUpvote(reply.id)}
                                aria-label="Upvote"
                              >
                                <i className="fa-solid fa-arrow-up"></i>
                                <span>{reply.upvoteCount ?? 0}</span>
                              </button>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          ) : (
            <div className="noncoding-muted">No comments yet. Be the first to comment!</div>
          )}
        </div>
      )}
    </div>
  );
};
