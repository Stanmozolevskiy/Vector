import { useEffect, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import subscriptionService from '../../services/subscription.service';
import type { SubscriptionPlan, Subscription } from '../../services/subscription.service';
import { ROUTES } from '../../utils/constants';
import '../../styles/style.css';
import '../../styles/pricing.css';

const SubscriptionPlansPage = () => {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [currentSubscription, setCurrentSubscription] = useState<Subscription | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [isAnnual, setIsAnnual] = useState(false);
  const [activeFaq, setActiveFaq] = useState<number | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const [plansData, subscriptionData] = await Promise.all([
          subscriptionService.getPlans(),
          isAuthenticated ? subscriptionService.getCurrentSubscription().catch(() => null) : Promise.resolve(null)
        ]);
        
        // Filter out lifetime plan and keep only 3 plans (free, monthly, annual)
        const filteredPlans = plansData.filter(plan => 
          plan.id === 'free' || plan.id === 'monthly' || plan.id === 'annual'
        );
        
        setPlans(filteredPlans);
        setCurrentSubscription(subscriptionData);
      } catch (err: unknown) {
        const error = err as { response?: { data?: { error?: string } } };
        setError(error.response?.data?.error || 'Failed to load subscription plans');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [isAuthenticated]);

  const handleSelectPlan = async (planId: string) => {
    if (!isAuthenticated) {
      navigate(ROUTES.LOGIN);
      return;
    }

    // Don't allow selecting the current plan
    if (currentSubscription?.planType === planId) {
      return;
    }

    try {
      // Update subscription
      await subscriptionService.updateSubscription(planId);
      
      // Refresh subscription data
      const updatedSubscription = await subscriptionService.getCurrentSubscription();
      setCurrentSubscription(updatedSubscription);
      
      // Show success message
      alert(`Successfully updated to ${updatedSubscription.plan?.name || planId} plan!`);
      
      // Optionally navigate back to profile
      // navigate(ROUTES.PROFILE);
    } catch (err: unknown) {
      const error = err as { response?: { data?: { error?: string } } };
      alert(error.response?.data?.error || 'Failed to update subscription. Please try again.');
    }
  };

  const formatPrice = (price: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
    }).format(price);
  };

  const toggleFaq = (index: number) => {
    setActiveFaq(activeFaq === index ? null : index);
  };

  const faqs = [
    {
      question: 'Can I cancel my subscription anytime?',
      answer: "Yes! You can cancel your subscription at any time. If you cancel, you'll continue to have access until the end of your billing period."
    },
    {
      question: 'What payment methods do you accept?',
      answer: 'We accept all major credit cards (Visa, MasterCard, American Express), PayPal, and for Enterprise plans, we can arrange for invoicing.'
    },
    {
      question: 'Do you offer student discounts?',
      answer: 'Yes! We offer a 50% discount for students with a valid .edu email address. Contact our support team to apply for the discount.'
    },
    {
      question: 'How do mock interviews work?',
      answer: 'Mock interviews are 1-on-1 video sessions with experienced interviewers from top tech companies. You\'ll receive detailed feedback after each session to help you improve.'
    },
    {
      question: 'Is there a free trial?',
      answer: 'Yes! We offer a 7-day free trial for the Pro plan. No credit card required to start. You can upgrade at any time.'
    },
    {
      question: 'Can I switch plans?',
      answer: 'Absolutely! You can upgrade or downgrade your plan at any time. Changes will be reflected in your next billing cycle.'
    }
  ];

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <i className="fas fa-spinner fa-spin text-4xl text-blue-600 mb-4"></i>
          <p className="text-gray-600">Loading subscription plans...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <i className="fas fa-exclamation-triangle text-4xl text-red-600 mb-4"></i>
          <p className="text-gray-600">{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-white">
      {/* Pricing Header */}
      <section className="pricing-header">
        <div className="container">
          <h1>Choose Your Plan</h1>
          <p>Get started for free or upgrade for unlimited access</p>
          <div className="billing-toggle">
            <span>Monthly</span>
            <label className="toggle-switch">
              <input 
                type="checkbox" 
                checked={isAnnual}
                onChange={(e) => setIsAnnual(e.target.checked)}
              />
              <span className="slider"></span>
            </label>
            <span>Annual <span className="save-badge">Save 30%</span></span>
          </div>
        </div>
      </section>

      {/* Pricing Plans */}
      <section className="pricing-section">
        <div className="container">
          <div className="pricing-grid">
            {plans.map((plan) => {
              const isCurrentPlan = currentSubscription?.planType === plan.id;
              return (
                <div
                  key={plan.id}
                  className={`pricing-card ${plan.isPopular ? 'featured' : ''} ${isCurrentPlan ? 'current-plan' : ''}`}
                  style={isCurrentPlan ? { border: '2px solid var(--primary)', position: 'relative' } : {}}
                >
                  {isCurrentPlan && (
                    <div style={{
                      position: 'absolute',
                      top: '1rem',
                      right: '1rem',
                      background: 'var(--primary)',
                      color: 'white',
                      padding: '0.25rem 0.75rem',
                      borderRadius: '4px',
                      fontSize: '0.875rem',
                      fontWeight: 'bold'
                    }}>
                      Current Plan
                    </div>
                  )}
                  {plan.isPopular && !isCurrentPlan && (
                    <div className="popular-badge">Most Popular</div>
                  )}
                  
                  <div className="plan-header">
                    <h3>{plan.name}</h3>
                    <div className="plan-price">
                      <span className="price">
                        {formatPrice(plan.price, plan.currency)}
                      </span>
                      {plan.billingPeriod !== 'free' && plan.billingPeriod !== 'one-time' && (
                        <span className="period">/{plan.billingPeriod}</span>
                      )}
                    </div>
                    <p>{plan.description}</p>
                  </div>

                  {!isCurrentPlan ? (
                    <button
                      onClick={() => handleSelectPlan(plan.id)}
                      className={plan.isPopular ? 'btn-primary btn-full' : 'btn-outline btn-full'}
                    >
                      Subscribe Now
                    </button>
                  ) : (
                    <button
                      disabled
                      className="btn-outline btn-full"
                      style={{ opacity: 0.6, cursor: 'not-allowed' }}
                    >
                      Current Plan
                    </button>
                  )}

                  <div className="plan-features">
                    <h4>Features:</h4>
                    <ul>
                      {plan.features.map((feature, index) => (
                        <li key={index}>
                          <i className="fas fa-check"></i>
                          {feature}
                        </li>
                      ))}
                    </ul>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* FAQ Section */}
      <section className="faq-section">
        <div className="container">
          <h2>Frequently Asked Questions</h2>
          <div className="faq-grid">
            {faqs.map((faq, index) => (
              <div
                key={index}
                className={`faq-item ${activeFaq === index ? 'active' : ''}`}
              >
                <div className="faq-question" onClick={() => toggleFaq(index)}>
                  <h3>{faq.question}</h3>
                  <i className="fas fa-chevron-down"></i>
                </div>
                <div className="faq-answer">
                  <p>{faq.answer}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="cta-section">
        <div className="container">
          <div className="cta-content">
            <h2>Ready to Land Your Dream Job?</h2>
            <p>Join thousands of students who have transformed their interview skills</p>
            {!isAuthenticated ? (
              <Link to={ROUTES.REGISTER} className="btn-primary btn-large">
                Start Your Free Trial
              </Link>
            ) : (
              <Link to={ROUTES.DASHBOARD} className="btn-primary btn-large">
                Go to Dashboard
              </Link>
            )}
          </div>
        </div>
      </section>
    </div>
  );
};

export default SubscriptionPlansPage;

