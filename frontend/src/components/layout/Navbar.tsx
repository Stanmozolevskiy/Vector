import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import '../../styles/style.css';
import '../../styles/dashboard.css';

export const Navbar = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate(ROUTES.HOME);
  };

  const getUserInitials = () => {
    if (user?.firstName && user?.lastName) {
      return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
    }
    if (user?.email) {
      return user.email.substring(0, 2).toUpperCase();
    }
    return 'U';
  };

  return (
    <nav className="navbar">
      <div className="container">
        <div className="nav-brand">
          <Link to={user ? ROUTES.DASHBOARD : ROUTES.HOME}>
            <i className="fas fa-vector-square"></i>
            <span>Vector</span>
          </Link>
        </div>
        <div className="nav-menu">
          <Link to={ROUTES.QUESTIONS}>Questions</Link>
          <Link to={ROUTES.FIND_PEER}>Mock Interviews</Link>
          {(user?.role === 'admin' || user?.role === 'coach') && (
            <Link to={ROUTES.ADD_QUESTION}>Add Question</Link>
          )}
          <div className="user-menu">
            <div className="user-avatar">
              {user?.profilePictureUrl ? (
                <img src={user.profilePictureUrl} alt="Profile" />
              ) : (
                <span>{getUserInitials()}</span>
              )}
            </div>
            <span>{user?.firstName || user?.email?.split('@')[0] || 'User'}</span>
            <i className="fas fa-chevron-down"></i>
            <div className="dropdown-menu">
              <Link to={ROUTES.DASHBOARD}><i className="fas fa-tachometer-alt"></i> Dashboard</Link>
              <Link to={ROUTES.PROGRESS}><i className="fas fa-chart-line"></i> Progress</Link>
              <Link to={ROUTES.PROFILE}><i className="fas fa-user"></i> Profile</Link>
              {user?.role === 'admin' && (
                <Link to={ROUTES.ADMIN}><i className="fas fa-shield-alt"></i> Admin Panel</Link>
              )}
              <button onClick={handleLogout}>
                <i className="fas fa-sign-out-alt"></i> Logout
              </button>
            </div>
          </div>
        </div>
      </div>
    </nav>
  );
};

