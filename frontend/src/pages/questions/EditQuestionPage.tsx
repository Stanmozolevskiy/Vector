import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Navbar } from '../../components/layout/Navbar';
import { QuestionForm } from '../../components/questions/QuestionForm';
import { questionService } from '../../services/question.service';
import type { CreateQuestionDto, QuestionTestCase } from '../../services/question.service';
import { ROUTES } from '../../utils/constants';
import { useAuth } from '../../hooks/useAuth';
import '../../styles/questions.css';

export const EditQuestionPage = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const [loading, setLoading] = useState(false);
  const [loadingQuestion, setLoadingQuestion] = useState(true);
  const [initialData, setInitialData] = useState<CreateQuestionDto | null>(null);
  const [initialTestCases, setInitialTestCases] = useState<Omit<QuestionTestCase, 'id'>[]>([]);

  // Check authorization - admin only
  if (user?.role !== 'admin') {
    navigate(ROUTES.QUESTIONS);
    return null;
  }

  // Load question data
  useEffect(() => {
    const loadQuestion = async () => {
      if (!id) {
        navigate(ROUTES.QUESTIONS);
        return;
      }

      try {
        setLoadingQuestion(true);
        const question = await questionService.getQuestionById(id);
        const loadedTestCases = await questionService.getTestCases(id, true);

        // Populate form with question data
        setInitialData({
          title: question.title,
          description: question.description,
          difficulty: question.difficulty,
          questionType: question.questionType,
          category: question.category,
          companyTags: question.companyTags || [],
          tags: question.tags || [],
          constraints: question.constraints || '',
          examples: question.examples || [],
          hints: question.hints || [],
          timeComplexityHint: question.timeComplexityHint || '',
          spaceComplexityHint: question.spaceComplexityHint || '',
        });

        // Convert test cases to format expected by QuestionForm
        const testCasesForForm = loadedTestCases.map(tc => ({
          testCaseNumber: tc.testCaseNumber,
          input: tc.input,
          expectedOutput: tc.expectedOutput,
          isHidden: tc.isHidden,
          explanation: tc.explanation || '',
        }));
        setInitialTestCases(testCasesForForm);
      } catch (err: any) {
        console.error('Failed to load question:', err);
      } finally {
        setLoadingQuestion(false);
      }
    };

    loadQuestion();
  }, [id, navigate]);

  const handleSubmit = async (formData: CreateQuestionDto, testCases: Omit<QuestionTestCase, 'id'>[]) => {
    if (!id) {
      throw new Error('Question ID is missing');
    }

    setLoading(true);

    try {
      // Update question
      await questionService.updateQuestion(id, formData);
      
      // Add new test cases (existing ones are kept)
      for (const testCase of testCases) {
        // Only add test cases that aren't already in initialTestCases
        const exists = initialTestCases.some(
          itc => itc.testCaseNumber === testCase.testCaseNumber &&
                 itc.input === testCase.input &&
                 itc.expectedOutput === testCase.expectedOutput
        );
        if (!exists) {
          await questionService.addTestCase(id, testCase);
        }
      }

      navigate(`${ROUTES.QUESTIONS}/${id}`);
    } catch (err: any) {
      throw err; // Let QuestionForm handle the error
    } finally {
      setLoading(false);
    }
  };

  if (loadingQuestion || !initialData) {
    return (
      <div className="questions-page">
        <Navbar />
        <div className="container-wide" style={{ padding: '2rem', maxWidth: '1200px', margin: '0 auto' }}>
          <div style={{ textAlign: 'center', padding: '4rem' }}>
            <i className="fas fa-spinner fa-spin text-2xl text-blue-600"></i>
            <p className="mt-4 text-gray-600">Loading question...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="questions-page">
      <Navbar />
      <div className="container-wide" style={{ padding: '2rem', maxWidth: '1200px', margin: '0 auto' }}>
        <div className="questions-header">
          <h1>Edit Question</h1>
          <p className="subtitle">Update the interview question details</p>
        </div>

        <QuestionForm
          initialData={initialData}
          testCases={initialTestCases}
          onSubmit={handleSubmit}
          onCancel={() => navigate(id ? `${ROUTES.QUESTIONS}/${id}` : ROUTES.QUESTIONS)}
          submitLabel="Update Question"
          loading={loading}
        />
      </div>
    </div>
  );
};

