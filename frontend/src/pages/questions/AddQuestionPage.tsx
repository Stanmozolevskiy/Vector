import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Navbar } from '../../components/layout/Navbar';
import { QuestionForm } from '../../components/questions/QuestionForm';
import { questionService } from '../../services/question.service';
import type { CreateQuestionDto, QuestionTestCase } from '../../services/question.service';
import { ROUTES } from '../../utils/constants';
import { useAuth } from '../../hooks/useAuth';
import '../../styles/questions.css';

export const AddQuestionPage = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!user) return;
    // Anyone logged in can submit a question for review; only admin/coach can create directly.
  }, [user]);

  const handleSubmit = async (formData: CreateQuestionDto, testCases: Omit<QuestionTestCase, 'id'>[]) => {
    setLoading(true);

    try {
      const isAdmin = user?.role === 'admin';
      const isCoach = user?.role === 'coach';

      const question = isAdmin || isCoach
        ? await questionService.createQuestion(formData)
        : await questionService.submitQuestion(formData);
      
      // Add test cases (admin/coach only)
      if ((isAdmin || isCoach) && testCases.length > 0) {
        for (const testCase of testCases) {
          await questionService.addTestCase(question.id, testCase);
        }
      }

      // Show success message based on user role
      if (isAdmin) {
        navigate(`${ROUTES.QUESTIONS}/${question.id}`);
        return;
      }

      alert('Question submitted successfully! It will be reviewed by an admin before being published.');
      navigate(ROUTES.QUESTIONS);
    } catch (err: any) {
      throw err; // Let QuestionForm handle the error
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="questions-page">
      <Navbar />
      <div className="container-wide" style={{ padding: '2rem', maxWidth: '1200px', margin: '0 auto' }}>
        <div className="questions-header">
          <h1>Add New Question</h1>
          <p className="subtitle">
            {user?.role === 'admin'
              ? 'Create a new interview question for the platform (will be automatically approved)'
              : 'Submit a new interview question for review (admin approval required before publishing)'}
          </p>
        </div>

        <QuestionForm
          onSubmit={handleSubmit}
          onCancel={() => navigate(ROUTES.QUESTIONS)}
          submitLabel="Create Question"
          loading={loading}
        />
      </div>
    </div>
  );
};

