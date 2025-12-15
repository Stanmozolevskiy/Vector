import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Navbar } from '../../components/layout/Navbar';
import { questionService } from '../../services/question.service';
import type { QuestionList, QuestionFilter } from '../../services/question.service';
import { ROUTES } from '../../utils/constants';
import '../../styles/questions.css';

const ROLES = ['Software Engineer', 'Product Manager', 'Data Engineer', 'Data Scientist', 'Technical Program Manager'];
const COMPANIES = ['Google', 'Meta', 'Amazon', 'Microsoft', 'Apple', 'Netflix'];
const CATEGORIES = ['Coding', 'System Design', 'Behavioral'];
const FILTER_OPTIONS = ['Expert Answers', 'Videos', 'Code Editor', 'Saved'];
const HOT_OPTIONS = ['Hot', 'Top', 'New'];

export const QuestionsPage = () => {
  const [questions, setQuestions] = useState<QuestionList[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [selectedCompanies, setSelectedCompanies] = useState<string[]>([]);
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
  const [selectedFilters, setSelectedFilters] = useState<string[]>([]);
  const [hotFilter, setHotFilter] = useState<string>('Hot');
  const [currentPage, setCurrentPage] = useState(1);
  const [openDropdown, setOpenDropdown] = useState<string | null>(null);

  useEffect(() => {
    loadQuestions();
  }, [searchTerm, selectedRoles, selectedCompanies, selectedCategories, selectedFilters, hotFilter, currentPage]);

  const loadQuestions = async () => {
    try {
      setLoading(true);
      const filter: QuestionFilter = {
        search: searchTerm || undefined,
        questionType: selectedCategories.length > 0 && selectedCategories.length < CATEGORIES.length 
          ? selectedCategories[0] 
          : undefined,
        companies: selectedCompanies.length > 0 ? selectedCompanies : undefined,
        page: currentPage,
        pageSize: 20,
      };
      const data = await questionService.getQuestions(filter);
      setQuestions(data);
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
      setSelectedCompanies(prev =>
        prev.includes(company)
          ? prev.filter(c => c !== company)
          : [...prev, company]
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
                          value={company.toLowerCase()}
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
                          value={category.toLowerCase()}
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
                              <img 
                                src={`https://logo.clearbit.com/${question.companyTags[0].toLowerCase()}.com`} 
                                alt={question.companyTags[0]} 
                                className="company-logo"
                                onError={(e) => {
                                  (e.target as HTMLImageElement).style.display = 'none';
                                }}
                              />
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
                      <img 
                        src={`https://logo.clearbit.com/${company.toLowerCase()}.com`} 
                        alt={company}
                        onError={(e) => {
                          (e.target as HTMLImageElement).style.display = 'none';
                        }}
                      />
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
