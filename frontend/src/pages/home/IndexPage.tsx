import { useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import '../../styles/style.css';

const IndexPageNavbar = () => {
  const { user } = useAuth();
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
          <Link to={ROUTES.LOGIN} className="btn-secondary">Log In</Link>
          <Link to={ROUTES.REGISTER} className="btn-primary">Get Started</Link>
        </div>
      </div>
    </nav>
  );
};

export const IndexPage = () => {
  const { isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    // Redirect logged-in users to dashboard
    if (!isLoading && isAuthenticated) {
      navigate(ROUTES.DASHBOARD, { replace: true });
    }
  }, [isAuthenticated, isLoading, navigate]);

  // Show loading state while checking auth
  if (isLoading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh' 
      }}>
        <div className="loading-spinner">Loading...</div>
      </div>
    );
  }

  // Don't render the page if user is authenticated (will redirect)
  if (isAuthenticated) {
    return null;
  }

  return (
    <div className="landing-page">
      {/* Navigation */}
      <IndexPageNavbar />

      {/* Hero Section */}
      <section className="hero">
        <div className="container">
          <div className="hero-content">
            <div className="hero-text">
              <h1>Ace Your Next Interview with <span className="gradient-text">Vector</span></h1>
              <p className="hero-subtitle">Practice with real interviewers from top tech companies. Get expert feedback and land your dream job.</p>
              <div className="hero-cta">
                <Link to={ROUTES.REGISTER} className="btn-primary btn-large">Start Learning Free</Link>
                <a href="#how-it-works" className="btn-secondary btn-large">How It Works</a>
              </div>
              <div className="trust-indicators">
                <div className="trust-item">
                  <i className="fas fa-users"></i>
                  <span>50,000+ Students</span>
                </div>
                <div className="trust-item">
                  <i className="fas fa-star"></i>
                  <span>4.9/5 Rating</span>
                </div>
                <div className="trust-item">
                  <i className="fas fa-briefcase"></i>
                  <span>10,000+ Job Offers</span>
                </div>
              </div>
            </div>
            <div className="hero-image">
              <div className="hero-card">
                <div className="mock-interview-preview">
                  <video 
                    controls 
                    poster=""
                  >
                    <source 
                      src="https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/videos/mock-interviews/what-is-exponent.mp4" 
                      type="video/mp4" 
                    />
                    Your browser does not support the video tag.
                  </video>
                </div>
                <p style={{ 
                  marginTop: '1rem', 
                  textAlign: 'center', 
                  color: 'var(--text-secondary)',
                  fontSize: '0.875rem',
                  lineHeight: '1.4'
                }}>
                  What Is Exponent? - Introduction to Mock Interviews
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Companies Section */}
      <section className="companies">
        <div className="container">
          <p className="companies-title">Trusted by candidates who got offers from</p>
          <div className="companies-logos">
            <div className="company-logo">Google</div>
            <div className="company-logo">Meta</div>
            <div className="company-logo">Amazon</div>
            <div className="company-logo">Microsoft</div>
            <div className="company-logo">Apple</div>
            <div className="company-logo">Netflix</div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="features" id="how-it-works">
        <div className="container">
          <h2 className="section-title">Everything You Need to Succeed</h2>
          <p className="section-subtitle">Comprehensive interview preparation platform with expert guidance</p>
          <div className="features-grid">
            <div className="feature-card">
              <div className="feature-icon">
                <i className="fas fa-video"></i>
              </div>
              <h3>Live Mock Interviews</h3>
              <p>Practice with experienced interviewers from FAANG companies. Get real-time feedback and improve your performance.</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">
                <i className="fas fa-graduation-cap"></i>
              </div>
              <h3>Expert-Led Courses</h3>
              <p>Learn from industry professionals with structured courses covering technical, behavioral, and system design interviews.</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">
                <i className="fas fa-database"></i>
              </div>
              <h3>Question Bank</h3>
              <p>Access 1000+ interview questions with detailed solutions, organized by company, role, and difficulty level.</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">
                <i className="fas fa-calendar-alt"></i>
              </div>
              <h3>Flexible Scheduling</h3>
              <p>Book mock interviews at your convenience. Choose your preferred time, interviewer, and interview type.</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">
                <i className="fas fa-chart-line"></i>
              </div>
              <h3>Progress Tracking</h3>
              <p>Monitor your improvement with detailed analytics. Track your strengths and areas for development.</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">
                <i className="fas fa-code"></i>
              </div>
              <h3>Coding Environment</h3>
              <p>Practice coding questions in a real interview environment with support for 15+ programming languages.</p>
            </div>
          </div>
        </div>
      </section>

      {/* Testimonials */}
      <section className="testimonials">
        <div className="container">
          <h2 className="section-title">Success Stories</h2>
          <p className="section-subtitle">Join thousands of successful candidates</p>
          <div className="testimonials-grid">
            <div className="testimonial-card">
              <div className="testimonial-rating">
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
              </div>
              <p className="testimonial-text">"Vector's mock interviews were incredibly realistic. The feedback I received helped me land my dream job at Google!"</p>
              <div className="testimonial-author">
                <div className="author-avatar">JD</div>
                <div className="author-info">
                  <div className="author-name">Jessica Davis</div>
                  <div className="author-title">Software Engineer at Google</div>
                </div>
              </div>
            </div>
            <div className="testimonial-card">
              <div className="testimonial-rating">
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
              </div>
              <p className="testimonial-text">"The system design course was phenomenal. I went from nervous to confident and got offers from 3 top companies."</p>
              <div className="testimonial-author">
                <div className="author-avatar">MP</div>
                <div className="author-info">
                  <div className="author-name">Michael Park</div>
                  <div className="author-title">Senior Engineer at Meta</div>
                </div>
              </div>
            </div>
            <div className="testimonial-card">
              <div className="testimonial-rating">
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
                <i className="fas fa-star"></i>
              </div>
              <p className="testimonial-text">"Best investment in my career. The mock interviews and question bank gave me the edge I needed to succeed."</p>
              <div className="testimonial-author">
                <div className="author-avatar">SK</div>
                <div className="author-info">
                  <div className="author-name">Sarah Kim</div>
                  <div className="author-title">Product Manager at Amazon</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="cta-section">
        <div className="container">
          <div className="cta-content">
            <h2>Ready to Ace Your Next Interview?</h2>
            <p>Join 50,000+ students who have transformed their interview skills with Vector</p>
            <Link to={ROUTES.REGISTER} className="btn-primary btn-large">Get Started Free</Link>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="footer">
        <div className="container">
          <div className="footer-grid">
            <div className="footer-col">
              <div className="footer-brand">
                <i className="fas fa-vector-square"></i>
                <span>Vector</span>
              </div>
              <p>Master your interview skills and land your dream job with expert guidance and realistic practice.</p>
              <div className="social-links">
                <a href="#"><i className="fab fa-twitter"></i></a>
                <a href="#"><i className="fab fa-linkedin"></i></a>
                <a href="#"><i className="fab fa-youtube"></i></a>
                <a href="#"><i className="fab fa-instagram"></i></a>
              </div>
            </div>
            <div className="footer-col">
              <h4>Product</h4>
              <ul>
                <li><a href="#courses">Courses</a></li>
                <li><a href="#questions">Question Bank</a></li>
                <li><a href="#interviews">Mock Interviews</a></li>
                <li><a href="#pricing">Pricing</a></li>
              </ul>
            </div>
            <div className="footer-col">
              <h4>Company</h4>
              <ul>
                <li><a href="#about">About Us</a></li>
                <li><a href="#careers">Careers</a></li>
                <li><a href="#blog">Blog</a></li>
                <li><a href="#contact">Contact</a></li>
              </ul>
            </div>
            <div className="footer-col">
              <h4>Support</h4>
              <ul>
                <li><a href="#help">Help Center</a></li>
                <li><a href="#terms">Terms of Service</a></li>
                <li><a href="#privacy">Privacy Policy</a></li>
                <li><a href="#cookies">Cookie Policy</a></li>
              </ul>
            </div>
          </div>
          <div className="footer-bottom">
            <p>&copy; 2025 Vector. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
};

