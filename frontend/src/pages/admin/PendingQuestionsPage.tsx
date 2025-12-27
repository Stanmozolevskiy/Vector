import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Navbar } from '../../components/layout/Navbar';
import { questionService } from '../../services/question.service';
import type { QuestionList } from '../../services/question.service';
import { ROUTES } from '../../utils/constants';
import { useAuth } from '../../hooks/useAuth';
import '../../styles/admin.css';

export const PendingQuestionsPage = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [questions, setQuestions] = useState<QuestionList[]>([]);
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState<string | null>(null);
  const [rejectionReason, setRejectionReason] = useState<Record<string, string>>({});
  const [showRejectForm, setShowRejectForm] = useState<string | null>(null);

  // Check authorization - admin only
  useEffect(() => {
    if (user?.role !== 'admin') {
      navigate(ROUTES.ADMIN);
    }
  }, [user, navigate]);

  useEffect(() => {
    if (user?.role === 'admin') {
      loadPendingQuestions();
    }
  }, [user]);

  const loadPendingQuestions = async () => {
    try {
      setLoading(true);
      const pendingQuestions = await questionService.getPendingQuestions();
      setQuestions(pendingQuestions);
    } catch (error) {
      console.error('Failed to load pending questions:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleApprove = async (questionId: string) => {
    try {
      setProcessing(questionId);
      await questionService.approveQuestion(questionId);
      await loadPendingQuestions();
    } catch (error) {
      console.error('Failed to approve question:', error);
      alert('Failed to approve question');
    } finally {
      setProcessing(null);
    }
  };

  const handleReject = async (questionId: string) => {
    try {
      setProcessing(questionId);
      await questionService.rejectQuestion(questionId, rejectionReason[questionId] || undefined);
      setRejectionReason({ ...rejectionReason, [questionId]: '' });
      setShowRejectForm(null);
      await loadPendingQuestions();
    } catch (error) {
      console.error('Failed to reject question:', error);
      alert('Failed to reject question');
    } finally {
      setProcessing(null);
    }
  };

  if (user?.role !== 'admin') {
    return null;
  }

  if (loading) {
    return (
      <div className="admin-dashboard">
        <Navbar />
        <div className="container-wide" style={{ padding: '2rem' }}>
          <div style={{ textAlign: 'center', padding: '4rem' }}>
            <i className="fas fa-spinner fa-spin text-2xl text-blue-600"></i>
            <p className="mt-4 text-gray-600">Loading pending questions...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="admin-dashboard">
      <Navbar />
      <div className="container-wide" style={{ padding: '2rem', maxWidth: '1400px', margin: '0 auto' }}>
        <div style={{ marginBottom: '2rem' }}>
          <Link to={ROUTES.ADMIN} style={{ color: '#6366f1', textDecoration: 'none', marginBottom: '1rem', display: 'inline-block' }}>
            <i className="fas fa-arrow-left"></i> Back to Admin Dashboard
          </Link>
          <h1 style={{ marginTop: '1rem' }}>Pending Questions</h1>
          <p className="subtitle">Review and approve questions submitted by coaches</p>
        </div>

        {questions.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '4rem', background: 'white', borderRadius: '12px' }}>
            <i className="fas fa-check-circle text-4xl text-green-600 mb-4"></i>
            <p style={{ fontSize: '1.125rem', color: '#6b7280' }}>No pending questions</p>
            <p style={{ color: '#9ca3af', marginTop: '0.5rem' }}>All questions have been reviewed</p>
          </div>
        ) : (
          <div style={{ display: 'grid', gap: '1.5rem' }}>
            {questions.map((question) => (
              <div key={question.id} style={{ background: 'white', padding: '1.5rem', borderRadius: '12px', border: '1px solid #e5e7eb' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', marginBottom: '1rem' }}>
                  <div style={{ flex: 1 }}>
                    <h3 style={{ marginBottom: '0.5rem' }}>
                      <Link to={`${ROUTES.QUESTIONS}/${question.id}`} style={{ color: '#111827', textDecoration: 'none' }}>
                        {question.title}
                      </Link>
                    </h3>
                    <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', marginBottom: '0.5rem' }}>
                      <span style={{ 
                        padding: '0.25rem 0.75rem', 
                        borderRadius: '4px', 
                        fontSize: '0.875rem',
                        background: question.difficulty === 'Easy' ? '#d1fae5' : question.difficulty === 'Medium' ? '#fef3c7' : '#fee2e2',
                        color: question.difficulty === 'Easy' ? '#065f46' : question.difficulty === 'Medium' ? '#92400e' : '#991b1b'
                      }}>
                        {question.difficulty}
                      </span>
                      <span style={{ 
                        padding: '0.25rem 0.75rem', 
                        borderRadius: '4px', 
                        fontSize: '0.875rem',
                        background: '#e0e7ff',
                        color: '#3730a3'
                      }}>
                        {question.category}
                      </span>
                      <span style={{ 
                        padding: '0.25rem 0.75rem', 
                        borderRadius: '4px', 
                        fontSize: '0.875rem',
                        background: '#f3f4f6',
                        color: '#6b7280'
                      }}>
                        {question.questionType}
                      </span>
                    </div>
                  </div>
                  <div style={{ display: 'flex', gap: '0.5rem', flexDirection: 'column' }}>
                    <button
                      onClick={() => handleApprove(question.id)}
                      disabled={processing === question.id}
                      style={{
                        padding: '0.5rem 1rem',
                        background: '#10b981',
                        color: 'white',
                        border: 'none',
                        borderRadius: '6px',
                        cursor: processing === question.id ? 'not-allowed' : 'pointer',
                        opacity: processing === question.id ? 0.6 : 1,
                        fontSize: '0.875rem',
                        fontWeight: 500
                      }}
                    >
                      {processing === question.id ? 'Processing...' : 'Approve'}
                    </button>
                    {showRejectForm === question.id ? (
                      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', marginTop: '0.5rem' }}>
                        <textarea
                          value={rejectionReason[question.id] || ''}
                          onChange={(e) => setRejectionReason({ ...rejectionReason, [question.id]: e.target.value })}
                          placeholder="Rejection reason (optional)"
                          rows={2}
                          style={{ padding: '0.5rem', border: '1px solid #e5e7eb', borderRadius: '4px', fontSize: '0.875rem' }}
                        />
                        <div style={{ display: 'flex', gap: '0.5rem' }}>
                          <button
                            onClick={() => handleReject(question.id)}
                            disabled={processing === question.id}
                            style={{
                              padding: '0.5rem 1rem',
                              background: '#ef4444',
                              color: 'white',
                              border: 'none',
                              borderRadius: '6px',
                              cursor: processing === question.id ? 'not-allowed' : 'pointer',
                              opacity: processing === question.id ? 0.6 : 1,
                              fontSize: '0.875rem',
                              fontWeight: 500,
                              flex: 1
                            }}
                          >
                            Confirm Reject
                          </button>
                          <button
                            onClick={() => {
                              setShowRejectForm(null);
                              setRejectionReason({ ...rejectionReason, [question.id]: '' });
                            }}
                            style={{
                              padding: '0.5rem 1rem',
                              background: '#f3f4f6',
                              color: '#374151',
                              border: 'none',
                              borderRadius: '6px',
                              cursor: 'pointer',
                              fontSize: '0.875rem'
                            }}
                          >
                            Cancel
                          </button>
                        </div>
                      </div>
                    ) : (
                      <button
                        onClick={() => setShowRejectForm(question.id)}
                        disabled={processing === question.id}
                        style={{
                          padding: '0.5rem 1rem',
                          background: '#ef4444',
                          color: 'white',
                          border: 'none',
                          borderRadius: '6px',
                          cursor: processing === question.id ? 'not-allowed' : 'pointer',
                          opacity: processing === question.id ? 0.6 : 1,
                          fontSize: '0.875rem',
                          fontWeight: 500
                        }}
                      >
                        Reject
                      </button>
                    )}
                  </div>
                </div>
                <div style={{ marginTop: '1rem', paddingTop: '1rem', borderTop: '1px solid #e5e7eb' }}>
                  <Link 
                    to={`${ROUTES.QUESTIONS}/${question.id}`}
                    style={{ color: '#6366f1', textDecoration: 'none', fontSize: '0.875rem' }}
                  >
                    View Full Question <i className="fas fa-arrow-right"></i>
                  </Link>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

