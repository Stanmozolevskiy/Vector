import { Link } from 'react-router-dom';
import { ROUTES } from '../../utils/constants';

interface FooterProps {
  variant?: 'full' | 'compact';
}

export const Footer = ({ variant = 'full' }: FooterProps) => {
  return (
    <footer className="footer">
      <div className="container">
        <div className="footer-grid">
          <div className="footer-col">
            <div className="footer-brand">
              <i className="fas fa-vector-square"></i>
              <span>Vector</span>
            </div>
            <p>
              Master your interview skills and land your dream job with expert guidance and realistic practice.
            </p>
            {variant === 'full' && (
              <div className="social-links">
                <a href="https://twitter.com" target="_blank" rel="noopener noreferrer" aria-label="Twitter">
                  <i className="fab fa-twitter"></i>
                </a>
                <a href="https://linkedin.com" target="_blank" rel="noopener noreferrer" aria-label="LinkedIn">
                  <i className="fab fa-linkedin"></i>
                </a>
                <a href="https://youtube.com" target="_blank" rel="noopener noreferrer" aria-label="YouTube">
                  <i className="fab fa-youtube"></i>
                </a>
                <a href="https://instagram.com" target="_blank" rel="noopener noreferrer" aria-label="Instagram">
                  <i className="fab fa-instagram"></i>
                </a>
              </div>
            )}
          </div>
          <div className="footer-col">
            <h4>Product</h4>
            <ul>
              <li><Link to={ROUTES.DASHBOARD}>Courses</Link></li>
              <li><Link to={ROUTES.QUESTIONS}>Question Bank</Link></li>
              <li><Link to={ROUTES.FIND_PEER}>Mock Interviews</Link></li>
              <li><Link to={ROUTES.SUBSCRIPTION_PLANS}>Pricing</Link></li>
            </ul>
          </div>
          <div className="footer-col">
            <h4>Company</h4>
            <ul>
              <li><Link to={ROUTES.ABOUT}>About Us</Link></li>
              <li><Link to={ROUTES.CAREERS}>Careers</Link></li>
              <li><Link to={ROUTES.BLOG}>Blog</Link></li>
              <li><Link to={ROUTES.CONTACT}>Contact</Link></li>
            </ul>
          </div>
          <div className="footer-col">
            <h4>Support</h4>
            <ul>
              <li><Link to={ROUTES.HELP}>Help Center</Link></li>
              <li><Link to={ROUTES.TERMS}>Terms of Service</Link></li>
              <li><Link to={ROUTES.PRIVACY}>Privacy Policy</Link></li>
              <li><Link to={ROUTES.COOKIES}>Cookie Policy</Link></li>
            </ul>
          </div>
        </div>
        <div className="footer-bottom">
          <p>&copy; 2025 Vector. All rights reserved.</p>
        </div>
      </div>
    </footer>
  );
};
