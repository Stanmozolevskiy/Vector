import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { coachService, type CoachApplication } from '../../services/coach.service';
import '../../styles/style.css';

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
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to load application');
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
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to submit application');
    } finally {
      setSubmitting(false);
    }
  };

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      setError('Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed.');
      return;
    }

    // Validate file size (10MB max)
    if (file.size > 10 * 1024 * 1024) {
      setError('File size exceeds 10MB limit.');
      return;
    }

    setError('');
    setUploadingImage(true);

    try {
      const imageUrl = await coachService.uploadImage(file);
      setFormData({
        ...formData,
        imageUrls: [...formData.imageUrls, imageUrl],
      });
      setSuccess('Image uploaded successfully!');
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to upload image');
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
    <div className="container" style={{ maxWidth: '800px', margin: '2rem auto', padding: '2rem' }}>
      <h1>Become a Coach</h1>
      <p style={{ marginBottom: '2rem', color: '#666' }}>
        Share your expertise and help students prepare for their technical interviews.
      </p>

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

      {success && (
        <div style={{
          background: '#efe',
          color: '#3c3',
          padding: '1rem',
          borderRadius: '4px',
          marginBottom: '1rem',
        }}>
          {success}
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
                  <div key={index} style={{ position: 'relative' }}>
                    <img
                      src={url}
                      alt={`Portfolio ${index + 1}`}
                      style={{
                        width: '100%',
                        height: '150px',
                        objectFit: 'cover',
                        borderRadius: '8px',
                        border: '1px solid #ddd',
                      }}
                    />
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
                        width: '24px',
                        height: '24px',
                        cursor: 'pointer',
                        fontSize: '0.875rem',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}
                    >
                      ×
                    </button>
                  </div>
                ))}
              </div>
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
  );
};

export default CoachApplicationPage;

