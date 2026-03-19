import { Link } from 'react-router-dom';
import { ROUTES } from '../../utils/constants';
import '../../styles/style.css';

export const PrivacyPolicyPage = () => {
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
          Privacy Policy
        </h1>
        <p style={{ color: 'var(--text-secondary)', marginBottom: '2rem', fontSize: '0.9375rem' }}>
          Last updated: January 2025
        </p>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            1. Introduction
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            We are committed to protecting your privacy. This Privacy Policy explains how we collect, 
            use, disclose, and safeguard your information when you use our service. Please read this 
            policy carefully.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            2. Information We Collect
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7, marginBottom: '0.75rem' }}>
            We may collect the following types of information:
          </p>
          <ul style={{ color: 'var(--text-secondary)', lineHeight: 1.8, paddingLeft: '1.5rem', marginBottom: '1rem' }}>
            <li><strong>Account information:</strong> name, email address, and password when you register</li>
            <li><strong>Usage data:</strong> how you interact with the service, including pages visited and features used</li>
            <li><strong>Device information:</strong> browser type, IP address, and device identifiers</li>
            <li><strong>Content you provide:</strong> submissions, answers, and other content you create on the platform</li>
          </ul>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            3. How We Use Your Information
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7, marginBottom: '0.75rem' }}>
            We use the information we collect to:
          </p>
          <ul style={{ color: 'var(--text-secondary)', lineHeight: 1.8, paddingLeft: '1.5rem', marginBottom: '1rem' }}>
            <li>Provide, maintain, and improve our service</li>
            <li>Process your account and transactions</li>
            <li>Send you service-related communications</li>
            <li>Personalize your experience</li>
            <li>Analyze usage and trends to improve our platform</li>
            <li>Comply with legal obligations</li>
          </ul>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            4. Information Sharing
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            We do not sell your personal information. We may share your information with service 
            providers who assist us in operating our platform, subject to confidentiality agreements. 
            We may also disclose information when required by law or to protect our rights and safety.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            5. Data Security
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            We implement appropriate technical and organizational measures to protect your personal 
            information against unauthorized access, alteration, disclosure, or destruction. However, 
            no method of transmission over the Internet is 100% secure.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            6. Cookies and Tracking
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            We use cookies and similar technologies to maintain your session, remember your preferences, 
            and understand how you use our service. You can control cookie settings through your 
            browser preferences.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            7. Your Rights
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            Depending on your location, you may have the right to access, correct, or delete your 
            personal information. You may also have the right to object to or restrict certain 
            processing. Contact us to exercise these rights.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            8. Children&apos;s Privacy
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            Our service is not intended for users under the age of 13. We do not knowingly collect 
            personal information from children under 13. If you become aware that a child has 
            provided us with personal information, please contact us.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            9. Changes to This Policy
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            We may update this Privacy Policy from time to time. We will notify you of material 
            changes by posting the updated policy on this page and updating the &quot;Last updated&quot; date. 
            We encourage you to review this policy periodically.
          </p>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--text-primary)' }}>
            10. Contact Us
          </h2>
          <p style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
            If you have questions about this Privacy Policy or our privacy practices, please contact 
            us through our contact page or at the email address provided on our website.
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
