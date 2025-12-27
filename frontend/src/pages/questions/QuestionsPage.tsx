import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Navbar } from '../../components/layout/Navbar';
import { questionService } from '../../services/question.service';
import type { QuestionList, QuestionFilter } from '../../services/question.service';
import { solutionService } from '../../services/solution.service';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import '../../styles/questions.css';

const ROLES = ['Software Engineer', 'Product Manager', 'Data Engineer', 'Data Scientist', 'Technical Program Manager'];
const COMPANIES = ['Google', 'Meta', 'Amazon', 'Microsoft', 'Apple', 'Netflix'];
const CATEGORIES = ['Arrays', 'Strings', 'Trees', 'Graphs', 'Dynamic Programming', 'Backtracking', 'Greedy', 'Math', 'Bit Manipulation', 'Sorting', 'Searching', 'Hash Tables', 'Linked Lists', 'Stacks', 'Queues', 'Heaps'];
const DIFFICULTIES = ['Easy', 'Medium', 'Hard'];
const FILTER_OPTIONS = ['Expert Answers', 'Videos', 'Code Editor', 'Saved'];
const HOT_OPTIONS = ['Hot', 'Top', 'New'];

export const QuestionsPage = () => {
  const { user } = useAuth();
  const [questions, setQuestions] = useState<QuestionList[]>([]);
  const [solvedQuestionIds, setSolvedQuestionIds] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [selectedCompanies, setSelectedCompanies] = useState<string[]>([]);
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
  const [selectedDifficulties, setSelectedDifficulties] = useState<string[]>([]);
  const [selectedFilters, setSelectedFilters] = useState<string[]>([]);
  const [hotFilter, setHotFilter] = useState<string>('Hot');
  const [currentPage, setCurrentPage] = useState(1);
  const [openDropdown, setOpenDropdown] = useState<string | null>(null);

  useEffect(() => {
    loadQuestions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchTerm, selectedCategories, selectedDifficulties, selectedCompanies, currentPage]);

  const loadQuestions = async () => {
    try {
      setLoading(true);
      const filter: QuestionFilter = {
        search: searchTerm || undefined,
        categories: selectedCategories.length > 0 ? selectedCategories : undefined,
        difficulties: selectedDifficulties.length > 0 ? selectedDifficulties : undefined,
        companies: selectedCompanies.length > 0 ? selectedCompanies : undefined,
        page: currentPage,
        pageSize: 20,
      };
      const data = await questionService.getQuestions(filter);
      setQuestions(data);
      
      // Load solved status for all questions
      if (user?.id) {
        const solvedIds = new Set<string>();
        await Promise.all(
          data.map(async (question) => {
            try {
              const isSolved = await solutionService.hasSolvedQuestion(question.id);
              if (isSolved) {
                solvedIds.add(question.id);
              }
            } catch (error) {
              // Silently fail - don't block page load
            }
          })
        );
        setSolvedQuestionIds(solvedIds);
      }
    } catch (error) {
      console.error('Failed to load questions:', error);
    } finally {
      setLoading(false);
    }
  };

  const toggleDropdown = (dropdownId: string) => {
    setOpenDropdown(openDropdown === dropdownId ? null : dropdownId);
  };

  const handleRoleToggle = (role: string) => {
    if (role === 'all') {
      setSelectedRoles([]);
    } else {
      setSelectedRoles(prev =>
        prev.includes(role)
          ? prev.filter(r => r !== role)
          : [...prev, role]
      );
    }
  };

  const handleCompanyToggle = (company: string) => {
    if (company === 'all') {
      setSelectedCompanies([]);
    } else {
      // Normalize company name for comparison (use original case from COMPANIES array)
      const normalizedCompany = COMPANIES.find(c => c.toLowerCase() === company.toLowerCase()) || company;
      setSelectedCompanies(prev =>
        prev.includes(normalizedCompany)
          ? prev.filter(c => c !== normalizedCompany)
          : [...prev, normalizedCompany]
      );
    }
  };

  const handleCategoryToggle = (category: string) => {
    if (category === 'all') {
      setSelectedCategories([]);
    } else {
      setSelectedCategories(prev =>
        prev.includes(category)
          ? prev.filter(c => c !== category)
          : [...prev, category]
      );
    }
  };

  const handleDifficultyToggle = (difficulty: string) => {
    if (difficulty === 'all') {
      setSelectedDifficulties([]);
    } else {
      setSelectedDifficulties(prev =>
        prev.includes(difficulty)
          ? prev.filter(d => d !== difficulty)
          : [...prev, difficulty]
      );
    }
  };

  const handleFilterToggle = (filter: string) => {
    setSelectedFilters(prev =>
      prev.includes(filter)
        ? prev.filter(f => f !== filter)
        : [...prev, filter]
    );
  };

  const getDifficultyClass = (difficulty: string) => {
    return difficulty.toLowerCase();
  };

  // Close dropdowns when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as HTMLElement;
      if (!target.closest('.dropdown-filter')) {
        setOpenDropdown(null);
      }
    };

    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, []);

  return (
    <div className="questions-page" style={{ overflowY: 'auto', height: '100vh' }}>
      <Navbar />
      
      <section className="questions-section">
        <div className="container-wide">
          <div className="questions-header">
            <div className="breadcrumb">
              <Link to={ROUTES.HOME}>Home</Link>
              <i className="fas fa-chevron-right"></i>
              <span>Questions</span>
            </div>
            <h1>Interview Questions</h1>
            <p className="subtitle">Review this list of interview questions and answers verified by hiring managers and candidates.</p>
          </div>

          <div className="content-layout">
            <div className="main-content">
              <div className="inline-filters">
                <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
                  <div className="dropdown-filter">
                    <button 
                      className={`filter-btn ${openDropdown === 'role' ? 'active' : ''}`}
                      onClick={(e) => {
                        e.stopPropagation();
                        toggleDropdown('role');
                      }}
                    >
                      <span>Role</span>
                      <i className="fas fa-chevron-down"></i>
                    </button>
                  <div className={`filter-dropdown scrollable ${openDropdown === 'role' ? 'show' : ''}`}>
                    <label className="filter-option">
                      <input
                        type="checkbox"
                        name="role"
                        value="all"
                        checked={selectedRoles.length === 0}
                        onChange={() => handleRoleToggle('all')}
                      />
                      <span>All Roles</span>
                    </label>
                    {ROLES.map(role => (
                      <label key={role} className="filter-option">
                        <input
                          type="checkbox"
                          name="role"
                          value={role.toLowerCase()}
                          checked={selectedRoles.includes(role)}
                          onChange={() => handleRoleToggle(role)}
                        />
                        <span>{role}</span>
                      </label>
                    ))}
                  </div>
                  </div>

                <div className="dropdown-filter">
                  <button 
                    className={`filter-btn ${openDropdown === 'company' ? 'active' : ''}`}
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleDropdown('company');
                    }}
                  >
                    <span>Company</span>
                    <i className="fas fa-chevron-down"></i>
                  </button>
                  <div className={`filter-dropdown scrollable ${openDropdown === 'company' ? 'show' : ''}`}>
                    <label className="filter-option">
                      <input
                        type="checkbox"
                        name="company"
                        value="all"
                        checked={selectedCompanies.length === 0}
                        onChange={() => handleCompanyToggle('all')}
                      />
                      <span>All Companies</span>
                    </label>
                    {COMPANIES.map(company => (
                      <label key={company} className="filter-option">
                        <input
                          type="checkbox"
                          name="company"
                          value={company}
                          checked={selectedCompanies.includes(company)}
                          onChange={() => handleCompanyToggle(company)}
                        />
                        <span>{company}</span>
                      </label>
                    ))}
                  </div>
                </div>

                <div className="dropdown-filter">
                  <button 
                    className={`filter-btn ${openDropdown === 'category' ? 'active' : ''}`}
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleDropdown('category');
                    }}
                  >
                    <span>Category</span>
                    <i className="fas fa-chevron-down"></i>
                  </button>
                  <div className={`filter-dropdown scrollable ${openDropdown === 'category' ? 'show' : ''}`}>
                    <label className="filter-option">
                      <input
                        type="checkbox"
                        name="category"
                        value="all"
                        checked={selectedCategories.length === 0}
                        onChange={() => handleCategoryToggle('all')}
                      />
                      <span>All Categories</span>
                    </label>
                    {CATEGORIES.map(category => (
                      <label key={category} className="filter-option">
                        <input
                          type="checkbox"
                          name="category"
                          value={category}
                          checked={selectedCategories.includes(category)}
                          onChange={() => handleCategoryToggle(category)}
                        />
                        <span>{category}</span>
                      </label>
                    ))}
                  </div>
                </div>

                <div className="dropdown-filter">
                  <button 
                    className={`filter-btn ${openDropdown === 'difficulty' ? 'active' : ''}`}
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleDropdown('difficulty');
                    }}
                  >
                    <span>Difficulty</span>
                    <i className="fas fa-chevron-down"></i>
                  </button>
                  <div className={`filter-dropdown scrollable ${openDropdown === 'difficulty' ? 'show' : ''}`}>
                    <label className="filter-option">
                      <input
                        type="checkbox"
                        name="difficulty"
                        value="all"
                        checked={selectedDifficulties.length === 0}
                        onChange={() => handleDifficultyToggle('all')}
                      />
                      <span>All Difficulties</span>
                    </label>
                    {DIFFICULTIES.map(difficulty => (
                      <label key={difficulty} className="filter-option">
                        <input
                          type="checkbox"
                          name="difficulty"
                          value={difficulty}
                          checked={selectedDifficulties.includes(difficulty)}
                          onChange={() => handleDifficultyToggle(difficulty)}
                        />
                        <span>{difficulty}</span>
                      </label>
                    ))}
                  </div>
                </div>

                <div className="dropdown-filter">
                  <button 
                    className={`filter-btn ${openDropdown === 'filter' ? 'active' : ''}`}
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleDropdown('filter');
                    }}
                  >
                    <span>Filter</span>
                    <i className="fas fa-chevron-down"></i>
                  </button>
                  <div className={`filter-dropdown ${openDropdown === 'filter' ? 'show' : ''}`}>
                    {FILTER_OPTIONS.map(filter => (
                      <label key={filter} className="filter-option">
                        <input
                          type="checkbox"
                          name="filter"
                          value={filter.toLowerCase()}
                          checked={selectedFilters.includes(filter)}
                          onChange={() => handleFilterToggle(filter)}
                        />
                        <span>{filter}</span>
                      </label>
                    ))}
                  </div>
                </div>
                </div>

                <div className="dropdown-filter">
                  <button 
                    className={`btn-hot ${openDropdown === 'hot' ? 'active' : ''}`}
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleDropdown('hot');
                    }}
                  >
                    <i className="fas fa-fire"></i>
                    {hotFilter}
                    <i className="fas fa-chevron-down"></i>
                  </button>
                  <div className={`filter-dropdown ${openDropdown === 'hot' ? 'show' : ''}`}>
                    {HOT_OPTIONS.map(option => (
                      <label key={option} className="filter-option">
                        <input
                          type="radio"
                          name="hot"
                          value={option.toLowerCase()}
                          checked={hotFilter === option}
                          onChange={() => setHotFilter(option)}
                        />
                        <span>{option}</span>
                      </label>
                    ))}
                  </div>
                </div>
              </div>

              {loading ? (
                <div className="text-center py-8">
                  <i className="fas fa-spinner fa-spin text-2xl text-blue-600"></i>
                  <p className="mt-4 text-gray-600">Loading questions...</p>
                </div>
              ) : (
                <>
                  <div className="questions-list">
                    {questions.map((question) => (
                      <div key={question.id} className="question-card">
                        <div className="question-header">
                          <div className="question-source">
                          {question.companyTags && question.companyTags.length > 0 && (
                            <div className="company-logo-placeholder">
                              <span className="company-initial">{question.companyTags[0].charAt(0).toUpperCase()}</span>
                            </div>
                          )}
                            <span>
                              {question.companyTags && question.companyTags.length > 0
                                ? `Asked at ${question.companyTags.join(', ')}`
                                : 'Interview Question'}
                            </span>
                          </div>
                        </div>
                        <h3 className="question-title">
                          <Link to={`${ROUTES.QUESTIONS}/${question.id}`}>
                            {question.title}
                          </Link>
                          {solvedQuestionIds.has(question.id) && (
                            <span className="question-solved-badge">
                              <i className="fas fa-check-circle"></i>
                              <span>Solved</span>
                            </span>
                          )}
                        </h3>
                        <div className="question-meta">
                          {question.difficulty && (
                            <span className={`badge difficulty ${getDifficultyClass(question.difficulty)}`}>
                              {question.difficulty}
                            </span>
                          )}
                          {question.tags && question.tags.slice(0, 2).map((tag, idx) => (
                            <span key={idx} className="badge topic">{tag}</span>
                          ))}
                        </div>
                        <div className="question-footer">
                          <button className="action-btn">
                            <i className="far fa-bookmark"></i>
                            Save
                          </button>
                          <div className="question-stats">
                            {question.acceptanceRate && (
                              <span>{question.acceptanceRate}% acceptance</span>
                            )}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>

                  <div className="pagination">
                    <button
                      className="page-btn disabled"
                      disabled={currentPage === 1}
                      onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                    >
                      <i className="fas fa-chevron-left"></i>
                    </button>
                    <button
                      className={`page-btn ${currentPage === 1 ? 'active' : ''}`}
                      onClick={() => setCurrentPage(1)}
                    >
                      1
                    </button>
                    <button
                      className="page-btn"
                      onClick={() => setCurrentPage(p => p + 1)}
                    >
                      <i className="fas fa-chevron-right"></i>
                    </button>
                  </div>
                </>
              )}
            </div>

            <aside className="sidebar">
              <div className="sidebar-search">
                <i className="fas fa-search"></i>
                <input
                  type="text"
                  placeholder="Search for questions, companies..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
              </div>

              <div className="sidebar-section">
                <h3>Popular roles</h3>
                <div className="role-tags">
                  {ROLES.map(role => (
                    <a key={role} href="#" className="role-tag">{role}</a>
                  ))}
                </div>
              </div>

              <div className="sidebar-section">
                <h3>Interviewed recently?</h3>
                <p className="section-description">Help improve our question database (and earn karma) by telling us about your experience</p>
                <button className="btn-share">
                  <i className="fas fa-plus"></i>
                  Share interview experience
                </button>
              </div>

              <div className="sidebar-section">
                <h3>Trending companies</h3>
                <div className="company-list">
                  {COMPANIES.slice(0, 4).map(company => (
                    <a key={company} href="#" className="company-item">
                      <div className="company-logo-placeholder">
                        <span className="company-initial">{company.charAt(0).toUpperCase()}</span>
                      </div>
                      <span>{company}</span>
                    </a>
                  ))}
                </div>
              </div>
            </aside>
          </div>
        </div>
      </section>
    </div>
  );
};
