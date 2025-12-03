import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

const UnauthorizedPage = () => {
  const navigate = useNavigate();
  const { user } = useAuth();

  return (
    <div style={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: '100vh',
      padding: '2rem',
      backgroundColor: '#f5f5f5'
    }}>
      <div style={{
        textAlign: 'center',
        maxWidth: '500px',
        backgroundColor: 'white',
        padding: '3rem',
        borderRadius: '12px',
        boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)'
      }}>
        <h1 style={{
          fontSize: '4rem',
          color: '#ef4444',
          margin: '0 0 1rem 0'
        }}>403</h1>
        <h2 style={{
          fontSize: '1.5rem',
          color: '#333',
          margin: '0 0 1rem 0'
        }}>Access Denied</h2>
        <p style={{
          color: '#666',
          marginBottom: '2rem',
          lineHeight: '1.6'
        }}>
          You don't have permission to access this page. 
          {user && (
            <> Your current role is: <strong>{user.role}</strong></>
          )}
        </p>
        <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center' }}>
          <button
            onClick={() => navigate(-1)}
            style={{
              padding: '0.75rem 1.5rem',
              backgroundColor: '#6366f1',
              color: 'white',
              border: 'none',
              borderRadius: '8px',
              cursor: 'pointer',
              fontSize: '1rem',
              fontWeight: '600'
            }}
          >
            Go Back
          </button>
          <button
            onClick={() => navigate('/dashboard')}
            style={{
              padding: '0.75rem 1.5rem',
              backgroundColor: '#fff',
              color: '#6366f1',
              border: '2px solid #6366f1',
              borderRadius: '8px',
              cursor: 'pointer',
              fontSize: '1rem',
              fontWeight: '600'
            }}
          >
            Go to Dashboard
          </button>
        </div>
      </div>
    </div>
  );
};

export default UnauthorizedPage;

