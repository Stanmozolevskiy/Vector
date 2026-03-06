import React, { useState, useEffect } from 'react';
import bookmarkService from '../../services/bookmark.service';
import '../../styles/bookmarkButton.css';

interface BookmarkButtonProps {
  questionId: string;
  showLabel?: boolean;
  size?: 'small' | 'medium' | 'large';
  onBookmarkChange?: (isBookmarked: boolean) => void;
}

const BookmarkButton: React.FC<BookmarkButtonProps> = ({
  questionId,
  showLabel = false,
  size = 'medium',
  onBookmarkChange,
}) => {
  const [isBookmarked, setIsBookmarked] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    checkBookmarkStatus();
  }, [questionId]);

  const checkBookmarkStatus = async () => {
    try {
      const bookmarked = await bookmarkService.isQuestionBookmarked(questionId);
      setIsBookmarked(bookmarked);
    } catch (err) {
      console.error('Error checking bookmark status:', err);
    }
  };

  const handleToggleBookmark = async (e: React.MouseEvent) => {
    e.stopPropagation();
    e.preventDefault();

    if (loading) return;

    setLoading(true);
    setError(null);

    try {
      if (isBookmarked) {
        await bookmarkService.removeBookmark(questionId);
        setIsBookmarked(false);
        onBookmarkChange?.(false);
      } else {
        await bookmarkService.bookmarkQuestion(questionId);
        setIsBookmarked(true);
        onBookmarkChange?.(true);
      }
    } catch (err: any) {
      console.error('Error toggling bookmark:', err);
      setError(err.response?.data?.error || 'Failed to update bookmark');
      setTimeout(() => setError(null), 3000);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="bookmark-button-container">
      <button
        onClick={handleToggleBookmark}
        className={`bookmark-button ${size} ${isBookmarked ? 'bookmarked' : ''} ${loading ? 'loading' : ''}`}
        disabled={loading}
        title={isBookmarked ? 'Remove bookmark' : 'Add bookmark'}
      >
        <i className={`fa-${isBookmarked ? 'solid' : 'regular'} fa-bookmark`}></i>
        {showLabel && (
          <span className="bookmark-label">
            {isBookmarked ? 'Bookmarked' : 'Bookmark'}
          </span>
        )}
      </button>
      {error && <div className="bookmark-error">{error}</div>}
    </div>
  );
};

export default BookmarkButton;
