import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { solutionService, type UserSolution, type SolutionFilter } from '../../services/solution.service';
import { ROUTES } from '../../utils/constants';

export const SolutionHistoryPage = () => {
  const [solutions, setSolutions] = useState<UserSolution[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<SolutionFilter>({
    page: 1,
    pageSize: 20,
  });
  const [totalCount, setTotalCount] = useState(0);
  const [selectedStatus, setSelectedStatus] = useState<string>('');
  const [selectedLanguage, setSelectedLanguage] = useState<string>('');

  useEffect(() => {
    loadSolutions();
  }, [filter.page, selectedStatus, selectedLanguage]);

  const loadSolutions = async () => {
    try {
      setLoading(true);
      const currentFilter: SolutionFilter = {
        ...filter,
        status: selectedStatus || undefined,
        language: selectedLanguage || undefined,
      };
      const result = await solutionService.getMySolutions(currentFilter);
      setSolutions(result.solutions || []);
      setTotalCount(result.totalCount || 0);
    } catch (error: any) {
      console.error('Failed to load solutions:', error);
      const errorMessage = error?.response?.data?.error || 
                          error?.response?.data?.message || 
                          error?.message || 
                          'Failed to load solutions. Please try again.';
      alert(errorMessage);
      setSolutions([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'accepted':
        return 'text-green-600 bg-green-50';
      case 'wrong answer':
        return 'text-red-600 bg-red-50';
      case 'time limit exceeded':
        return 'text-orange-600 bg-orange-50';
      case 'runtime error':
        return 'text-purple-600 bg-purple-50';
      case 'compilation error':
        return 'text-yellow-600 bg-yellow-50';
      default:
        return 'text-gray-600 bg-gray-50';
    }
  };

  const formatMemory = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(2)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  if (loading && solutions.length === 0) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <i className="fas fa-spinner fa-spin text-4xl text-blue-600 mb-4"></i>
          <p className="text-gray-600">Loading solutions...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900">My Solutions</h1>
          <p className="mt-2 text-gray-600">View and manage your submitted solutions</p>
        </div>

        {/* Filters */}
        <div className="bg-white rounded-lg shadow-sm p-4 mb-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Status
              </label>
              <select
                value={selectedStatus}
                onChange={(e) => {
                  setSelectedStatus(e.target.value);
                  setFilter({ ...filter, page: 1 });
                }}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">All Statuses</option>
                <option value="Accepted">Accepted</option>
                <option value="Wrong Answer">Wrong Answer</option>
                <option value="Time Limit Exceeded">Time Limit Exceeded</option>
                <option value="Runtime Error">Runtime Error</option>
                <option value="Compilation Error">Compilation Error</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Language
              </label>
              <select
                value={selectedLanguage}
                onChange={(e) => {
                  setSelectedLanguage(e.target.value);
                  setFilter({ ...filter, page: 1 });
                }}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">All Languages</option>
                <option value="python">Python</option>
                <option value="javascript">JavaScript</option>
                <option value="java">Java</option>
                <option value="cpp">C++</option>
                <option value="csharp">C#</option>
                <option value="go">Go</option>
              </select>
            </div>
            <div className="flex items-end">
              <button
                onClick={() => {
                  setSelectedStatus('');
                  setSelectedLanguage('');
                  setFilter({ ...filter, page: 1 });
                }}
                className="w-full px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
              >
                Clear Filters
              </button>
            </div>
          </div>
        </div>

        {/* Solutions List */}
        {solutions.length === 0 ? (
          <div className="bg-white rounded-lg shadow-sm p-12 text-center">
            <i className="fas fa-code text-6xl text-gray-300 mb-4"></i>
            <h3 className="text-xl font-semibold text-gray-900 mb-2">No solutions found</h3>
            <p className="text-gray-600 mb-6">
              {selectedStatus || selectedLanguage
                ? 'Try adjusting your filters'
                : "You haven't submitted any solutions yet."}
            </p>
            <Link
              to={ROUTES.QUESTIONS}
              className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
            >
              <i className="fas fa-arrow-left mr-2"></i>
              Browse Questions
            </Link>
          </div>
        ) : (
          <div className="space-y-4">
            {solutions.map((solution) => (
              <div
                key={solution.id}
                className="bg-white rounded-lg shadow-sm p-6 hover:shadow-md transition-shadow"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <Link
                        to={`${ROUTES.QUESTIONS}/${solution.questionId}`}
                        className="text-lg font-semibold text-blue-600 hover:text-blue-800"
                      >
                        {solution.questionTitle}
                      </Link>
                      <span
                        className={`px-2 py-1 text-xs font-medium rounded-full ${getStatusColor(
                          solution.status
                        )}`}
                      >
                        {solution.status}
                      </span>
                    </div>
                    <div className="flex items-center gap-4 text-sm text-gray-600 mb-3">
                      <span className="flex items-center gap-1">
                        <i className="fas fa-code"></i>
                        {solution.language}
                      </span>
                      <span className="flex items-center gap-1">
                        <i className="fas fa-clock"></i>
                        {solution.executionTime.toFixed(3)}s
                      </span>
                      <span className="flex items-center gap-1">
                        <i className="fas fa-memory"></i>
                        {formatMemory(solution.memoryUsed)}
                      </span>
                      <span className="flex items-center gap-1">
                        <i className="fas fa-check-circle"></i>
                        {solution.testCasesPassed}/{solution.totalTestCases} passed
                      </span>
                    </div>
                    <p className="text-sm text-gray-500">
                      Submitted {formatDate(solution.submittedAt)}
                    </p>
                  </div>
                  <Link
                    to={`${ROUTES.QUESTIONS}/${solution.questionId}`}
                    className="ml-4 px-4 py-2 text-sm font-medium text-blue-600 bg-blue-50 rounded-md hover:bg-blue-100"
                  >
                    View
                  </Link>
                </div>
              </div>
            ))}

            {/* Pagination */}
            {totalCount > filter.pageSize! && (
              <div className="flex items-center justify-between bg-white rounded-lg shadow-sm p-4">
                <div className="text-sm text-gray-700">
                  Showing {(filter.page! - 1) * filter.pageSize! + 1} to{' '}
                  {Math.min(filter.page! * filter.pageSize!, totalCount)} of {totalCount} solutions
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => setFilter({ ...filter, page: filter.page! - 1 })}
                    disabled={filter.page === 1}
                    className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Previous
                  </button>
                  <button
                    onClick={() => setFilter({ ...filter, page: filter.page! + 1 })}
                    disabled={filter.page! * filter.pageSize! >= totalCount}
                    className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

