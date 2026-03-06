import { useEffect, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { coachService, type CoachApplication } from '../../services/coach.service';
import { ROUTES } from '../../utils/constants';
import { Navbar } from '../../components/layout/Navbar';
import '../../styles/style.css';
import '../../styles/dashboard.css';

const CoachApplicationPage = () => {
  const { user, isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();
  const [application, setApplication] = useState<CoachApplication | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [formData, setFormData] = useState({
    motivation: '',
    experience: '',
    specialization: '',
    imageUrls: [] as string[],
  });
  const [uploadingImage, setUploadingImage] = useState(false);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      navigate('/login');
      return;
    }

    if (user?.role === 'coach') {
      navigate('/dashboard');
      return;
    }

    if (user?.role === 'admin') {
      navigate('/admin');
      return;
    }

    fetchApplication();
  }, [isAuthenticated, isLoading, user, navigate]);

  const fetchApplication = async () => {
    try {
      const app = await coachService.getMyApplication();
      setApplication(app);
      if (app) {
        setFormData({
          motivation: app.motivation,
          experience: app.experience || '',
          specialization: app.specialization || '',
          imageUrls: app.imageUrls || [],
        });
      }
    } catch (err: unknown) {
      // 404 is expected when no application exists yet - don't show error
      const error = err as { response?: { status?: number; data?: { error?: string } } };
      if (error.response?.status !== 404) {
        setError(error.response?.data?.error || 'Failed to load application');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setSubmitting(true);

    if (formData.motivation.length < 50) {
      setError('Motivation must be at least 50 characters long');
      setSubmitting(false);
      return;
    }

    try {
      const result = await coachService.submitApplication({
        motivation: formData.motivation,
        experience: formData.experience || undefined,
        specialization: formData.specialization || undefined,
        imageUrls: formData.imageUrls.length > 0 ? formData.imageUrls : undefined,
      });
      setApplication(result);
      setSuccess('Your application has been submitted successfully! You will receive an email once it has been reviewed.');
    } catch (err: unknown) {
      const error = err as { response?: { data?: { error?: string } } };
      setError(error.response?.data?.error || 'Failed to submit application');
    } finally {
      setSubmitting(false);
    }
  };

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Check image limit (10 images max)
    if (formData.imageUrls.length >= 10) {
      setError('Maximum 10 images allowed. Please remove an image before adding a new one.');
      e.target.value = '';
      return;
    }

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      setError('Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed.');
      e.target.value = '';
      return;
    }

    // Validate file size (10MB max)
    if (file.size > 10 * 1024 * 1024) {
      setError('File size exceeds 10MB limit.');
      e.target.value = '';
      return;
    }

    setError('');
    setUploadingImage(true);

    try {
      const imageUrl = await coachService.uploadImage(file);
      console.log('Uploaded image URL:', imageUrl);
      
      if (!imageUrl || typeof imageUrl !== 'string') {
        throw new Error('Invalid image URL received');
      }
      
      setFormData((prev) => ({
        ...prev,
        imageUrls: [...prev.imageUrls, imageUrl],
      }));
      setSuccess('Image uploaded successfully!');
      setTimeout(() => setSuccess(''), 3000); // Clear success message after 3 seconds
    } catch (err: unknown) {
      console.error('Image upload error:', err);
      const error = err as { response?: { data?: { error?: string } }; message?: string };
      setError(error.response?.data?.error || error.message || 'Failed to upload image');
    } finally {
      setUploadingImage(false);
      // Reset input
      e.target.value = '';
    }
  };

  const handleRemoveImage = (index: number) => {
    setFormData({
      ...formData,
      imageUrls: formData.imageUrls.filter((_, i) => i !== index),
    });
  };

  const getStatusBadge = (status: string) => {
    const badges: Record<string, { text: string; className: string }> = {
      pending: { text: 'Pending Review', className: 'status-pending' },
      approved: { text: 'Approved ✓', className: 'status-approved' },
      rejected: { text: 'Rejected', className: 'status-rejected' },
    };
    const badge = badges[status] || badges.pending;
    return <span className={`status-badge ${badge.className}`}>{badge.text}</span>;
  };

  if (loading || isLoading) {
    return <div className="container" style={{ padding: '2rem', textAlign: 'center' }}>Loading...</div>;
  }

  return (
    <div className="dashboard-page">
      <Navbar />
      <section className="dashboard-section">
        <div className="container" style={{ maxWidth: '900px', margin: '0 auto', padding: '2rem' }}>
      {/* Header Section */}
      <div style={{ 
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        color: 'white',
        padding: '2rem',
        borderRadius: '12px',
        marginBottom: '2rem',
        textAlign: 'center'
      }}>
        <h1 style={{ margin: '0 0 0.5rem 0', fontSize: '2.5rem', fontWeight: 'bold' }}>Become a Coach</h1>
        <p style={{ margin: '0', fontSize: '1.1rem', opacity: 0.95 }}>
          Share your expertise and help students prepare for their technical interviews.
        </p>
      </div>

      {success && success.includes('submitted successfully') && (
        <div style={{
          background: 'white',
          padding: '3rem 2rem',
          borderRadius: '12px',
          marginBottom: '2rem',
          boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)',
          textAlign: 'center',
          border: '2px solid #d1fae5'
        }}>
          <div style={{
            width: '80px',
            height: '80px',
            margin: '0 auto 1.5rem',
            borderRadius: '50%',
            background: 'linear-gradient(135deg, #10b981 0%, #059669 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 4px 12px rgba(16, 185, 129, 0.3)'
          }}>
            <i className="fas fa-check" style={{ fontSize: '2.5rem', color: 'white' }}></i>
          </div>
          <h2 style={{
            margin: '0 0 1rem 0',
            fontSize: '1.75rem',
            fontWeight: 'bold',
            color: '#065f46'
          }}>
            Application Submitted Successfully!
          </h2>
          <p style={{
            margin: '0 0 2rem 0',
            fontSize: '1.1rem',
            color: '#047857',
            lineHeight: '1.6',
            maxWidth: '600px',
            marginLeft: 'auto',
            marginRight: 'auto'
          }}>
            Thank you for your interest in becoming a coach! Your application has been received and is now under review. 
            You will receive an email notification once your application has been reviewed by our team.
          </p>
          <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center', flexWrap: 'wrap' }}>
            <Link 
              to={ROUTES.PROFILE}
              className="btn-primary"
              style={{ 
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.5rem',
                textDecoration: 'none',
                padding: '0.75rem 1.5rem',
                borderRadius: '8px',
                fontWeight: '600',
                transition: 'all 0.2s'
              }}
            >
              <i className="fas fa-user"></i>
              View Profile
            </Link>
            <Link 
              to={ROUTES.DASHBOARD}
              className="btn-secondary"
              style={{ 
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.5rem',
                textDecoration: 'none',
                padding: '0.75rem 1.5rem',
                borderRadius: '8px',
                fontWeight: '600',
                background: 'var(--bg-gray-50)',
                color: 'var(--text-primary)',
                border: '1px solid var(--border-color)',
                transition: 'all 0.2s'
              }}
            >
              <i className="fas fa-tachometer-alt"></i>
              Go to Dashboard
            </Link>
          </div>
        </div>
      )}

      {success && !success.includes('submitted successfully') && (
        <div style={{
          background: '#d1fae5',
          borderLeft: '4px solid #10b981',
          padding: '1rem',
          borderRadius: '4px',
          marginBottom: '1rem',
          color: '#065f46'
        }}>
          <i className="fas fa-check-circle" style={{ marginRight: '0.5rem' }}></i>
          {success}
        </div>
      )}

      {error && (
        <div style={{
          background: '#fee',
          color: '#c33',
          padding: '1rem',
          borderRadius: '4px',
          marginBottom: '1rem',
        }}>
          {error}
        </div>
      )}

      {application && (
        <div style={{
          background: '#f5f5f5',
          padding: '1.5rem',
          borderRadius: '8px',
          marginBottom: '2rem',
        }}>
          <h3>Your Application Status</h3>
          <div style={{ marginTop: '1rem' }}>
            {getStatusBadge(application.status)}
          </div>
          {application.adminNotes && (
            <div style={{ marginTop: '1rem', padding: '1rem', background: 'white', borderRadius: '4px' }}>
              <strong>Admin Notes:</strong>
              <p style={{ marginTop: '0.5rem' }}>{application.adminNotes}</p>
            </div>
          )}
          {application.reviewedAt && (
            <p style={{ marginTop: '1rem', fontSize: '0.9rem', color: '#666' }}>
              Reviewed on: {new Date(application.reviewedAt).toLocaleDateString()}
            </p>
          )}
        </div>
      )}

      {(!application || application.status === 'rejected') && (
        <form onSubmit={handleSubmit} style={{ background: 'white', padding: '2rem', borderRadius: '8px', boxShadow: '0 2px 4px rgba(0,0,0,0.1)' }}>
          <div style={{ marginBottom: '1.5rem' }}>
            <label htmlFor="motivation" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
              Why do you want to become a coach? *
            </label>
            <textarea
              id="motivation"
              value={formData.motivation}
              onChange={(e) => setFormData({ ...formData, motivation: e.target.value })}
              required
              minLength={50}
              maxLength={500}
              rows={6}
              style={{
                width: '100%',
                padding: '0.75rem',
                border: '1px solid #ddd',
                borderRadius: '4px',
                fontSize: '1rem',
                fontFamily: 'inherit',
              }}
              placeholder="Tell us about your motivation to help students prepare for interviews (minimum 50 characters)..."
            />
            <p style={{ fontSize: '0.85rem', color: '#666', marginTop: '0.25rem' }}>
              {formData.motivation.length}/500 characters
            </p>
          </div>

          <div style={{ marginBottom: '1.5rem' }}>
            <label htmlFor="experience" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
              Your Experience (Optional)
            </label>
            <textarea
              id="experience"
              value={formData.experience}
              onChange={(e) => setFormData({ ...formData, experience: e.target.value })}
              maxLength={1000}
              rows={5}
              style={{
                width: '100%',
                padding: '0.75rem',
                border: '1px solid #ddd',
                borderRadius: '4px',
                fontSize: '1rem',
                fontFamily: 'inherit',
              }}
              placeholder="Describe your professional experience, technical background, or interview experience..."
            />
            <p style={{ fontSize: '0.85rem', color: '#666', marginTop: '0.25rem' }}>
              {formData.experience.length}/1000 characters
            </p>
          </div>

          <div style={{ marginBottom: '1.5rem' }}>
            <label htmlFor="specialization" style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
              Specialization (Optional)
            </label>
            <input
              id="specialization"
              type="text"
              value={formData.specialization}
              onChange={(e) => setFormData({ ...formData, specialization: e.target.value })}
              maxLength={500}
              style={{
                width: '100%',
                padding: '0.75rem',
                border: '1px solid #ddd',
                borderRadius: '4px',
                fontSize: '1rem',
              }}
              placeholder="e.g., System Design, Data Structures, Behavioral Interviews..."
            />
          </div>

          <div style={{ marginBottom: '1.5rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
              Portfolio Images (Optional)
            </label>
            <p style={{ fontSize: '0.85rem', color: '#666', marginBottom: '0.75rem' }}>
              Upload images of your portfolio, certificates, or achievements (JPEG, PNG, GIF, WebP - Max 10MB each)
            </p>
            <input
              type="file"
              accept="image/jpeg,image/jpg,image/png,image/gif,image/webp"
              onChange={handleImageUpload}
              disabled={uploadingImage}
              style={{
                width: '100%',
                padding: '0.75rem',
                border: '1px solid #ddd',
                borderRadius: '4px',
                fontSize: '1rem',
                marginBottom: '1rem',
              }}
            />
            {uploadingImage && (
              <p style={{ color: '#667eea', fontSize: '0.9rem' }}>Uploading image...</p>
            )}
            {formData.imageUrls.length > 0 && (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))', gap: '1rem', marginTop: '1rem' }}>
                {formData.imageUrls.map((url, index) => (
                  <div key={index} style={{ position: 'relative', width: '100%', paddingTop: '100%' }}>
                    <div style={{ 
                      position: 'absolute', 
                      top: 0, 
                      left: 0, 
                      width: '100%', 
                      height: '100%',
                      borderRadius: '8px',
                      overflow: 'hidden',
                      border: '1px solid #ddd',
                      backgroundColor: '#f5f5f5',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center'
                    }}>
                      <img
                        src={url}
                        alt={`Portfolio ${index + 1}`}
                        style={{
                          width: '100%',
                          height: '100%',
                          objectFit: 'cover',
                          display: 'block'
                        }}
                        onError={(e) => {
                          // Show placeholder if image fails to load
                          const target = e.target as HTMLImageElement;
                          target.style.display = 'none';
                          const parent = target.parentElement;
                          if (parent) {
                            parent.innerHTML = '<i class="fas fa-image" style="font-size: 2rem; color: #ccc;"></i>';
                          }
                        }}
                        onLoad={() => {
                          // Image loaded successfully - force re-render
                          console.log('Image loaded:', url);
                        }}
                        crossOrigin="anonymous"
                      />
                    </div>
                    <button
                      type="button"
                      onClick={() => handleRemoveImage(index)}
                      style={{
                        position: 'absolute',
                        top: '0.5rem',
                        right: '0.5rem',
                        background: 'rgba(220, 53, 69, 0.9)',
                        color: 'white',
                        border: 'none',
                        borderRadius: '50%',
                        width: '28px',
                        height: '28px',
                        cursor: 'pointer',
                        fontSize: '1rem',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        zIndex: 10,
                        boxShadow: '0 2px 4px rgba(0,0,0,0.2)'
                      }}
                      title="Remove image"
                    >
                      ×
                    </button>
                  </div>
                ))}
              </div>
            )}
            {formData.imageUrls.length >= 10 && (
              <p style={{ color: '#ff9800', fontSize: '0.85rem', marginTop: '0.5rem' }}>
                Maximum of 10 images reached. Remove an image to add a new one.
              </p>
            )}
          </div>

          <button
            type="submit"
            disabled={submitting || application?.status === 'pending'}
            style={{
              width: '100%',
              padding: '0.75rem',
              background: submitting ? '#ccc' : 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              fontSize: '1rem',
              fontWeight: 'bold',
              cursor: submitting ? 'not-allowed' : 'pointer',
            }}
          >
            {submitting ? 'Submitting...' : application?.status === 'pending' ? 'Application Submitted' : 'Submit Application'}
          </button>
        </form>
      )}
        </div>
      </section>
    </div>
  );
};

export default CoachApplicationPage;

