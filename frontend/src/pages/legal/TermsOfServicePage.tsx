import { Link } from 'react-router-dom';
import { ROUTES } from '../../utils/constants';
import '../../styles/style.css';

export const TermsOfServicePage = () => {
  return (
    <div className="min-h-screen flex flex-col">
      <main
        style={{
          flex: 1,
          maxWidth: 800,
          margin: '0 auto',
          padding: 'var(--spacing-xl)',
        }}
      >
        <h1 style={{ marginBottom: '1.5rem', fontSize: '1.75rem', color: 'var(--text-primary)' }}>
          Terms of Service
        </h1>
        <p style={{ color: 'var(--text-secondary)', marginBottom: '2rem', fontSize: '0.9375rem' }}>
          Last updated: January 2025
        </p>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            1. Acceptance of Terms
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            By accessing or using this service, you agree to be bound by these Terms of Service. 
            If you do not agree to these terms, please do not use our service.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            2. Description of Service
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            We provide an online platform for interview preparation, including practice questions, 
            mock interviews, and educational content. The service is provided as-is and we reserve 
            the right to modify, suspend, or discontinue any part of the service at any time.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            3. User Accounts
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            You must create an account to access certain features. You are responsible for 
            maintaining the confidentiality of your account credentials and for all activities 
            that occur under your account. You must provide accurate and complete information 
            when registering.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            4. Acceptable Use
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7, marginBottom: '0.75rem' }}>
            You agree not to use the service to:
          </p>
          <ul style={{ color: 'var(--text-secondary)', lineHeight: 1.8, paddingLeft: '1.5rem', marginBottom: '1rem' }}>
            <li>Violate any applicable laws or regulations</li>
            <li>Infringe on the rights of others</li>
            <li>Distribute malware or harmful content</li>
            <li>Attempt to gain unauthorized access to our systems or other user accounts</li>
            <li>Use the service for any fraudulent or abusive purpose</li>
          </ul>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            We reserve the right to suspend or terminate accounts that violate these terms.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            5. Intellectual Property
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            The service, including its content, features, and functionality, is owned by us and 
            is protected by intellectual property laws. You may not copy, modify, or distribute 
            our content without prior written permission.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            6. Disclaimer of Warranties
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            The service is provided on an &quot;as is&quot; and &quot;as available&quot; basis. We make no warranties, 
            express or implied, regarding the reliability, accuracy, or completeness of the content 
            or the service.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            7. Limitation of Liability
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            To the maximum extent permitted by law, we shall not be liable for any indirect, 
            incidental, special, consequential, or punitive damages arising from your use of 
            the service.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            8. Changes to Terms
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            We may update these terms from time to time. We will notify users of material changes 
            by posting the updated terms on this page and updating the &quot;Last updated&quot; date. 
            Continued use of the service after changes constitutes acceptance of the new terms.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            9. Contact
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            If you have questions about these Terms of Service, please contact us through our 
            contact page or at the email address provided on our website.
          </p>
        </section>

        <Link to={ROUTES.HOME} className="btn-primary" style={{ display: 'inline-block', marginTop: '1rem' }}>
          Back to Home
        </Link>
      </main>
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
