import { useEffect, useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { Navbar } from '../../components/layout/Navbar';
import { AnalyticsDashboard } from '../../components/analytics/AnalyticsDashboard';
import { analyticsService, type CategoryProgress, type DifficultyProgress } from '../../services/analytics.service';
import { ROUTES } from '../../utils/constants';
import '../../styles/style.css';

export const ProgressPage = () => {
  const [categoryProgress, setCategoryProgress] = useState<Record<string, CategoryProgress>>({});
  const [difficultyProgress, setDifficultyProgress] = useState<Record<string, DifficultyProgress>>({});
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [selectedDifficulty, setSelectedDifficulty] = useState<string | null>(null);

  // Common categories and difficulties
  const categories = ['Arrays', 'Strings', 'Trees', 'Graphs', 'Dynamic Programming', 'Backtracking', 'Greedy', 'Math'];
  const difficulties = ['Easy', 'Medium', 'Hard'];

  const loadProgress = useCallback(async () => {
    try {
      // Load progress for all categories
      const categoryPromises = categories.map(async (category) => {
        try {
          const progress = await analyticsService.getCategoryProgress(category);
          return { category, progress };
        } catch (err) {
          return { category, progress: null };
        }
      });

      const categoryResults = await Promise.all(categoryPromises);
      const categoryMap: Record<string, CategoryProgress> = {};
      categoryResults.forEach(({ category, progress }) => {
        if (progress) {
          categoryMap[category] = progress;
        }
      });
      setCategoryProgress(categoryMap);

      // Load progress for all difficulties
      const difficultyPromises = difficulties.map(async (difficulty) => {
        try {
          const progress = await analyticsService.getDifficultyProgress(difficulty);
          return { difficulty, progress };
        } catch (err) {
          return { difficulty, progress: null };
        }
      });

      const difficultyResults = await Promise.all(difficultyPromises);
      const difficultyMap: Record<string, DifficultyProgress> = {};
      difficultyResults.forEach(({ difficulty, progress }) => {
        if (progress) {
          difficultyMap[difficulty] = progress;
        }
      });
      setDifficultyProgress(difficultyMap);
    } catch (err) {
      console.error('Error loading progress:', err);
    }
  }, []);

  // Initial load
  useEffect(() => {
    loadProgress();
  }, [loadProgress]);

  // Listen for storage events (when solution is submitted from another tab)
  useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'solutionSubmitted') {
        // Reload progress data when solution is submitted
        loadProgress();
      }
    };

    const handleCustomStorage = () => {
      // Handle same-tab storage events
      loadProgress();
    };

    window.addEventListener('storage', handleStorageChange);
    window.addEventListener('solutionSubmitted', handleCustomStorage);
    
    return () => {
      window.removeEventListener('storage', handleStorageChange);
      window.removeEventListener('solutionSubmitted', handleCustomStorage);
    };
  }, [loadProgress]);

  // Refresh on focus (when user comes back to tab)
  useEffect(() => {
    const handleFocus = () => {
      // Reload progress data when page regains focus
      loadProgress();
    };

    window.addEventListener('focus', handleFocus);
    return () => window.removeEventListener('focus', handleFocus);
  }, [loadProgress]);

  return (
    <div className="progress-page">
      <Navbar />
      
      <section className="progress-section" style={{ padding: '2rem 0', minHeight: 'calc(100vh - 80px)' }}>
        <div className="container-wide">
          <div style={{ marginBottom: '2rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <div>
                <h1 style={{ fontSize: '2rem', fontWeight: 700, color: '#111827', marginBottom: '0.5rem' }}>
                  Your Progress
                </h1>
                <p style={{ color: '#6b7280', fontSize: '1rem' }}>
                  Track your learning journey and identify areas for improvement
                </p>
              </div>
              <Link to={ROUTES.QUESTIONS} className="btn-primary">
                <i className="fas fa-code" style={{ marginRight: '0.5rem' }}></i>
                Solve Problems
              </Link>
            </div>
          </div>

          {/* Analytics Dashboard */}
          <div style={{ marginBottom: '3rem' }}>
            <AnalyticsDashboard />
          </div>

          {/* Category Progress */}
          <div style={{ marginBottom: '3rem' }}>
            <h2 style={{ fontSize: '1.5rem', fontWeight: 600, marginBottom: '1.5rem', color: '#111827' }}>
              Progress by Category
            </h2>
            <div style={{ 
              display: 'grid', 
              gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', 
              gap: '1rem' 
            }}>
              {categories.map((category) => {
                const progress = categoryProgress[category];
                if (!progress) return null;

                return (
                  <div
                    key={category}
                    className="progress-card"
                    style={{
                      backgroundColor: '#fff',
                      borderRadius: '8px',
                      padding: '1.5rem',
                      boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
                      cursor: 'pointer',
                      transition: 'transform 0.2s, box-shadow 0.2s',
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.transform = 'translateY(-2px)';
                      e.currentTarget.style.boxShadow = '0 4px 6px rgba(0,0,0,0.1)';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.transform = 'translateY(0)';
                      e.currentTarget.style.boxShadow = '0 1px 3px rgba(0,0,0,0.1)';
                    }}
                    onClick={() => setSelectedCategory(selectedCategory === category ? null : category)}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem' }}>
                      <h3 style={{ fontSize: '1rem', fontWeight: 600, color: '#111827' }}>
                        {category}
                      </h3>
                      <span style={{ 
                        fontSize: '0.875rem', 
                        color: '#6b7280',
                        fontWeight: 500
                      }}>
                        {progress.completionPercentage.toFixed(1)}%
                      </span>
                    </div>
                    <div style={{ marginBottom: '0.75rem' }}>
                      <div style={{ 
                        width: '100%', 
                        height: '8px', 
                        backgroundColor: '#e5e7eb', 
                        borderRadius: '4px',
                        overflow: 'hidden'
                      }}>
                        <div style={{
                          width: `${progress.completionPercentage}%`,
                          height: '100%',
                          backgroundColor: '#3b82f6',
                          transition: 'width 0.3s ease',
                        }}></div>
                      </div>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem', color: '#6b7280' }}>
                      <span>{progress.questionsSolved} / {progress.totalQuestions} solved</span>
                      {progress.averageExecutionTime > 0 && (
                        <span>Avg: {progress.averageExecutionTime.toFixed(0)}ms</span>
                      )}
                    </div>
                    {selectedCategory === category && (
                      <div style={{ 
                        marginTop: '1rem', 
                        paddingTop: '1rem', 
                        borderTop: '1px solid #e5e7eb',
                        fontSize: '0.875rem',
                        color: '#6b7280'
                      }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                          <span>Average Execution Time:</span>
                          <span style={{ fontWeight: 600 }}>
                            {progress.averageExecutionTime > 0 
                              ? `${progress.averageExecutionTime.toFixed(0)} ms`
                              : 'N/A'}
                          </span>
                        </div>
                        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                          <span>Average Memory Used:</span>
                          <span style={{ fontWeight: 600 }}>
                            {progress.averageMemoryUsed > 0 
                              ? `${(progress.averageMemoryUsed / 1024).toFixed(2)} MB`
                              : 'N/A'}
                          </span>
                        </div>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>

          {/* Difficulty Progress */}
          <div>
            <h2 style={{ fontSize: '1.5rem', fontWeight: 600, marginBottom: '1.5rem', color: '#111827' }}>
              Progress by Difficulty
            </h2>
            <div style={{ 
              display: 'grid', 
              gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', 
              gap: '1rem' 
            }}>
              {difficulties.map((difficulty) => {
                const progress = difficultyProgress[difficulty];
                if (!progress) return null;

                const difficultyColor = difficulty === 'Easy' ? '#10b981' : difficulty === 'Medium' ? '#f59e0b' : '#ef4444';

                return (
                  <div
                    key={difficulty}
                    className="progress-card"
                    style={{
                      backgroundColor: '#fff',
                      borderRadius: '8px',
                      padding: '1.5rem',
                      boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
                      cursor: 'pointer',
                      transition: 'transform 0.2s, box-shadow 0.2s',
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.transform = 'translateY(-2px)';
                      e.currentTarget.style.boxShadow = '0 4px 6px rgba(0,0,0,0.1)';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.transform = 'translateY(0)';
                      e.currentTarget.style.boxShadow = '0 1px 3px rgba(0,0,0,0.1)';
                    }}
                    onClick={() => setSelectedDifficulty(selectedDifficulty === difficulty ? null : difficulty)}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem' }}>
                      <h3 style={{ 
                        fontSize: '1rem', 
                        fontWeight: 600, 
                        color: difficultyColor,
                        padding: '0.25rem 0.75rem',
                        borderRadius: '4px',
                        backgroundColor: difficulty === 'Easy' ? '#d1fae5' : difficulty === 'Medium' ? '#fef3c7' : '#fee2e2',
                        display: 'inline-block'
                      }}>
                        {difficulty}
                      </h3>
                      <span style={{ 
                        fontSize: '0.875rem', 
                        color: '#6b7280',
                        fontWeight: 500
                      }}>
                        {progress.completionPercentage.toFixed(1)}%
                      </span>
                    </div>
                    <div style={{ marginBottom: '0.75rem' }}>
                      <div style={{ 
                        width: '100%', 
                        height: '8px', 
                        backgroundColor: '#e5e7eb', 
                        borderRadius: '4px',
                        overflow: 'hidden'
                      }}>
                        <div style={{
                          width: `${progress.completionPercentage}%`,
                          height: '100%',
                          backgroundColor: difficultyColor,
                          transition: 'width 0.3s ease',
                        }}></div>
                      </div>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem', color: '#6b7280' }}>
                      <span>{progress.questionsSolved} / {progress.totalQuestions} solved</span>
                      {progress.averageExecutionTime > 0 && (
                        <span>Avg: {progress.averageExecutionTime.toFixed(0)}ms</span>
                      )}
                    </div>
                    {selectedDifficulty === difficulty && (
                      <div style={{ 
                        marginTop: '1rem', 
                        paddingTop: '1rem', 
                        borderTop: '1px solid #e5e7eb',
                        fontSize: '0.875rem',
                        color: '#6b7280'
                      }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                          <span>Average Execution Time:</span>
                          <span style={{ fontWeight: 600 }}>
                            {progress.averageExecutionTime > 0 
                              ? `${progress.averageExecutionTime.toFixed(0)} ms`
                              : 'N/A'}
                          </span>
                        </div>
                        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                          <span>Average Memory Used:</span>
                          <span style={{ fontWeight: 600 }}>
                            {progress.averageMemoryUsed > 0 
                              ? `${(progress.averageMemoryUsed / 1024).toFixed(2)} MB`
                              : 'N/A'}
                          </span>
                        </div>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      </section>
    </div>
  );
};

