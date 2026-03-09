import { Link } from 'react-router-dom';
import { ROUTES } from '../utils/constants';
import { Navbar } from '../components/layout/Navbar';
import '../styles/style.css';

interface ComingSoonPageProps {
  title?: string;
}

const PAGE_TITLES: Record<string, string> = {
  [ROUTES.ABOUT]: 'About Us',
  [ROUTES.CAREERS]: 'Careers',
  [ROUTES.BLOG]: 'Blog',
  [ROUTES.CONTACT]: 'Contact',
  [ROUTES.HELP]: 'Help Center',
  [ROUTES.TERMS]: 'Terms of Service',
  [ROUTES.PRIVACY]: 'Privacy Policy',
  [ROUTES.COOKIES]: 'Cookie Policy',
};

export const ComingSoonPage = ({ title }: ComingSoonPageProps) => {
  const path = window.location.pathname;
  const pageTitle = title ?? PAGE_TITLES[path] ?? 'This Page';

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <section
        style={{
          flex: 1,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 'var(--spacing-xl)',
          textAlign: 'center',
        }}
      >
        <div>
          <div style={{ marginBottom: '1.5rem', fontSize: '4rem', color: 'var(--primary-color)' }}>
            <i className="fas fa-construction"></i>
          </div>
          <h1 style={{ marginBottom: '0.5rem', fontSize: '1.75rem', color: 'var(--text-primary)' }}>
            {pageTitle}
          </h1>
          <p style={{ color: 'var(--text-secondary)', marginBottom: '2rem', maxWidth: '400px', margin: '0 auto 2rem' }}>
            We&apos;re working on it! This page will be available soon.
          </p>
          <Link to={ROUTES.HOME} className="btn-primary">
            Back to Home
          </Link>
        </div>
      </section>
      <footer className="footer" style={{ marginTop: 'auto' }}>
        <div className="container">
          <div className="footer-bottom">
            <p>&copy; 2025 Vector. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
};
