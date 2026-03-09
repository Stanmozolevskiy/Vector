import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Component } from 'react';
import type { ErrorInfo, ReactNode } from 'react';
import { AuthProvider } from './hooks/useAuth.tsx';
import ProtectedRoute from './components/ProtectedRoute';
import { IndexPage } from './pages/home/IndexPage';
import { LoginPage } from './pages/auth/LoginPage';
import { RegisterPage } from './pages/auth/RegisterPage';
import { VerifyEmailPage } from './pages/auth/VerifyEmailPage';
import { ForgotPasswordPage } from './pages/auth/ForgotPasswordPage';
import { ResetPasswordPage } from './pages/auth/ResetPasswordPage';
import { ResendVerificationPage } from './pages/auth/ResendVerificationPage';
import { DashboardPage } from './pages/dashboard/DashboardPage';
import { ProfilePage } from './pages/profile/ProfilePage';
import AdminDashboardPage from './pages/admin/AdminDashboardPage';
import { PendingQuestionsPage } from './pages/admin/PendingQuestionsPage';
import CoachApplicationPage from './pages/coach/CoachApplicationPage';
import SubscriptionPlansPage from './pages/subscription/SubscriptionPlansPage';
import { QuestionsPage } from './pages/questions/QuestionsPage';
import { QuestionDetailPage } from './pages/questions/QuestionDetailPage';
import { AddQuestionPage } from './pages/questions/AddQuestionPage';
import { EditQuestionPage } from './pages/questions/EditQuestionPage';
import BookmarkedQuestionsPage from './pages/questions/BookmarkedQuestionsPage';
import { DailyChallengePage } from './pages/challenges/DailyChallengePage';
import { SolutionHistoryPage } from './pages/solutions/SolutionHistoryPage';
import { ProgressPage } from './pages/progress/ProgressPage';
import FindPeerPage from './pages/peer-interviews/FindPeerPage';
import { LeaderboardAndEarnPage } from './pages/leaderboard/LeaderboardAndEarnPage';
import PeerInterviewSessionPage from './pages/peer-interviews/PeerInterviewSessionPage';
import { FriendInvitePage } from './pages/peer-interviews/FriendInvitePage';
import { lazy, Suspense } from 'react';

// Lazy load WhiteboardPage to avoid breaking the app if Excalidraw fails
const WhiteboardPage = lazy(() => import('./pages/whiteboard/WhiteboardPage').then(module => ({ default: module.WhiteboardPage })));
const SystemDesignInterviewPage = lazy(() => import('./pages/system-design-interview/SystemDesignInterviewPage').then(module => ({ default: module.SystemDesignInterviewPage })));
import UnauthorizedPage from './pages/UnauthorizedPage';
import { ComingSoonPage } from './pages/ComingSoonPage';
import { SessionNotificationManager } from './components/SessionNotificationManager';
import { ROUTES } from './utils/constants';

// Error Boundary to catch rendering errors
class ErrorBoundary extends Component<
  { children: ReactNode },
  { hasError: boolean; error: Error | null }
> {
  constructor(props: { children: ReactNode }) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('App Error Boundary caught an error:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div style={{ padding: '20px', fontFamily: 'sans-serif' }}>
          <h1>Application Error</h1>
          <p>Something went wrong. Please refresh the page.</p>
          <details style={{ marginTop: '20px' }}>
            <summary>Error Details</summary>
            <pre style={{ background: '#f0f0f0', padding: '10px', borderRadius: '4px', overflow: 'auto' }}>
              {this.state.error?.stack || this.state.error?.message || 'Unknown error'}
            </pre>
          </details>
        </div>
      );
    }

    return this.props.children;
  }
}

function App() {
  return (
    <ErrorBoundary>
      <AuthProvider>
        <BrowserRouter>
          <SessionNotificationManager />
          <Routes>
        {/* Public routes - no protection */}
        <Route path={ROUTES.HOME} element={<IndexPage />} />
        <Route path={ROUTES.VERIFY_EMAIL} element={<VerifyEmailPage />} />
        <Route path="/friend-invite/:liveSessionId" element={<FriendInvitePage />} />
        
        {/* Auth routes - accessible to everyone, but redirect if already logged in */}
        <Route path={ROUTES.LOGIN} element={<LoginPage />} />
        <Route path={ROUTES.REGISTER} element={<RegisterPage />} />
        <Route path={ROUTES.FORGOT_PASSWORD} element={<ForgotPasswordPage />} />
        <Route path={ROUTES.RESET_PASSWORD} element={<ResetPasswordPage />} />
        <Route path={ROUTES.RESEND_VERIFICATION} element={<ResendVerificationPage />} />
        
        {/* Protected routes (require authentication) */}
        <Route path={ROUTES.DASHBOARD} element={
          <ProtectedRoute requireAuth>
            <DashboardPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.PROFILE} element={
          <ProtectedRoute requireAuth>
            <ProfilePage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.COACH_APPLY} element={
          <ProtectedRoute requireAuth>
            <CoachApplicationPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.SUBSCRIPTION_PLANS} element={
          <ProtectedRoute requireAuth>
            <SubscriptionPlansPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.QUESTIONS} element={
          <ProtectedRoute requireAuth>
            <QuestionsPage />
          </ProtectedRoute>
        } />
        <Route path="/questions/bookmarks" element={
          <ProtectedRoute requireAuth>
            <BookmarkedQuestionsPage />
          </ProtectedRoute>
        } />
        <Route path="/challenges/daily" element={
          <ProtectedRoute requireAuth>
            <DailyChallengePage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.SOLUTION_HISTORY} element={
          <ProtectedRoute requireAuth>
            <SolutionHistoryPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.PROGRESS} element={
          <ProtectedRoute requireAuth>
            <ProgressPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.LEADERBOARD} element={
          <ProtectedRoute requireAuth>
            <LeaderboardAndEarnPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.HOW_TO_EARN} element={
          <ProtectedRoute requireAuth>
            <LeaderboardAndEarnPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.FIND_PEER} element={
          <ProtectedRoute requireAuth>
            <FindPeerPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.PEER_INTERVIEW_SESSION} element={
          <ProtectedRoute requireAuth>
            <PeerInterviewSessionPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.WHITEBOARD} element={
          <ProtectedRoute requireAuth>
            <Suspense fallback={<div style={{ padding: '20px', textAlign: 'center' }}>Loading whiteboard...</div>}>
              <WhiteboardPage />
            </Suspense>
          </ProtectedRoute>
        } />
        <Route path={ROUTES.SYSTEM_DESIGN_INTERVIEW} element={
          <ProtectedRoute requireAuth>
            <Suspense fallback={<div style={{ padding: '20px', textAlign: 'center' }}>Loading interview session...</div>}>
              <SystemDesignInterviewPage />
            </Suspense>
          </ProtectedRoute>
        } />
        <Route path={`${ROUTES.QUESTIONS}/:id`} element={
          <ProtectedRoute requireAuth>
            <QuestionDetailPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.ADD_QUESTION} element={
          <ProtectedRoute requireAuth>
            <AddQuestionPage />
          </ProtectedRoute>
        } />
        <Route path={`${ROUTES.EDIT_QUESTION}/:id`} element={
          <ProtectedRoute requireAuth requiredRole="admin">
            <EditQuestionPage />
          </ProtectedRoute>
        } />
        
        {/* Admin routes (require admin role) */}
        <Route path={ROUTES.ADMIN} element={
          <ProtectedRoute requireAuth requiredRole="admin">
            <AdminDashboardPage />
          </ProtectedRoute>
        } />
        <Route path={ROUTES.PENDING_QUESTIONS} element={
          <ProtectedRoute requireAuth requiredRole="admin">
            <PendingQuestionsPage />
          </ProtectedRoute>
        } />
        
        {/* Unauthorized page */}
        <Route path="/unauthorized" element={<UnauthorizedPage />} />

        {/* Coming soon placeholder pages */}
        <Route path={ROUTES.ABOUT} element={<ComingSoonPage />} />
        <Route path={ROUTES.CAREERS} element={<ComingSoonPage />} />
        <Route path={ROUTES.BLOG} element={<ComingSoonPage />} />
        <Route path={ROUTES.CONTACT} element={<ComingSoonPage />} />
        <Route path={ROUTES.HELP} element={<ComingSoonPage />} />
        <Route path={ROUTES.TERMS} element={<ComingSoonPage />} />
        <Route path={ROUTES.PRIVACY} element={<ComingSoonPage />} />
        <Route path={ROUTES.COOKIES} element={<ComingSoonPage />} />
        </Routes>
      </BrowserRouter>
      </AuthProvider>
    </ErrorBoundary>
  );
}

export default App;
