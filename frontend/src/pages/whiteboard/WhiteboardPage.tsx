import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Navbar } from '../../components/layout/Navbar';
import { Whiteboard } from '../../components/whiteboard/Whiteboard';
import { SystemDesignQuestionModal } from '../../components/whiteboard/SystemDesignQuestionModal';
import { QuestionSidebar } from '../../components/whiteboard/QuestionSidebar';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import type { QuestionList } from '../../services/question.service';
import { whiteboardService } from '../../services/whiteboard.service';
import '../../components/whiteboard/Whiteboard.css';
import './WhiteboardPage.css';

// Define local type based on Excalidraw's structure
interface ExcalidrawInitialDataState {
  elements?: readonly any[];
  appState?: any;
  files?: Record<string, any>;
}

export const WhiteboardPage = () => {
  const { user, isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();
  // Initialize with default empty state so Excalidraw always has valid data
  const [whiteboardData, setWhiteboardData] = useState<ExcalidrawInitialDataState>({
    elements: [],
    appState: {
      viewBackgroundColor: '#fafafa',
      gridSize: 20,
      zoom: {
        value: 1,
      },
      scrollX: 0,
      scrollY: 0,
    },
    files: {},
  });
  const [isQuestionModalOpen, setIsQuestionModalOpen] = useState(false);
  const [selectedQuestion, setSelectedQuestion] = useState<QuestionList | null>(null);
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);
  const [isGridEnabled] = useState(false); // Grid off by default

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      navigate(ROUTES.LOGIN);
    }
  }, [isAuthenticated, isLoading, navigate]);

  // Load saved whiteboard data from backend API
  useEffect(() => {
    if (!user?.id) {
      // Reset to default if no user
      setWhiteboardData({
        elements: [],
        appState: {
          viewBackgroundColor: '#fafafa',
          zoom: {
            value: 1,
          },
          scrollX: 0,
          scrollY: 0,
        },
        files: {},
      });
      return;
    }

    const loadWhiteboardData = async () => {
      try {
        const questionId = selectedQuestion?.id || undefined;
        const savedData = await whiteboardService.getWhiteboardData(questionId);
        
        if (savedData) {
          try {
            // Parse JSON strings from backend
            const elements = JSON.parse(savedData.elements || '[]');
            const appState = JSON.parse(savedData.appState || '{}');
            const files = JSON.parse(savedData.files || '{}');
            
            // Sanitize appState - only keep safe, serializable fields
            const sanitized: ExcalidrawInitialDataState = {
              elements: Array.isArray(elements) ? elements : [],
              appState: {},
              files: files && typeof files === 'object' ? files : {},
            };
            
            // Only include safe appState fields that won't cause React error #185
            if (appState && typeof appState === 'object') {
              const safeFields: Record<string, any> = {};
              
              // Only copy primitive and safe object fields
              if (appState.viewBackgroundColor && typeof appState.viewBackgroundColor === 'string') {
                safeFields.viewBackgroundColor = appState.viewBackgroundColor;
              } else {
                safeFields.viewBackgroundColor = '#fafafa';
              }
              
              if (appState.zoom) {
                if (typeof appState.zoom === 'number') {
                  safeFields.zoom = { value: appState.zoom };
                } else if (appState.zoom.value && typeof appState.zoom.value === 'number') {
                  safeFields.zoom = { value: appState.zoom.value };
                } else {
                  safeFields.zoom = { value: 1 };
                }
              } else {
                safeFields.zoom = { value: 1 };
              }
              
              if (typeof appState.scrollX === 'number') {
                safeFields.scrollX = appState.scrollX;
              } else {
                safeFields.scrollX = 0;
              }
              
              if (typeof appState.scrollY === 'number') {
                safeFields.scrollY = appState.scrollY;
              } else {
                safeFields.scrollY = 0;
              }
              
              sanitized.appState = safeFields;
            } else {
              // Default appState if invalid
              sanitized.appState = {
                viewBackgroundColor: '#fafafa',
                zoom: { value: 1 },
                scrollX: 0,
                scrollY: 0,
              };
            }
            
            setWhiteboardData(sanitized);
          } catch (parseError) {
            console.error('Failed to parse whiteboard data from backend:', parseError);
            // Use default on parse error
            setWhiteboardData({
              elements: [],
              appState: {
                viewBackgroundColor: '#fafafa',
                zoom: { value: 1 },
                scrollX: 0,
                scrollY: 0,
              },
              files: {},
            });
          }
        } else {
          // No saved data - use default
          setWhiteboardData({
            elements: [],
            appState: {
              viewBackgroundColor: '#fafafa',
              zoom: { value: 1 },
              scrollX: 0,
              scrollY: 0,
            },
            files: {},
          });
        }
      } catch (error) {
        console.error('Failed to load whiteboard data from backend:', error);
        // On error, use default state
        setWhiteboardData({
          elements: [],
          appState: {
            viewBackgroundColor: '#fafafa',
            zoom: { value: 1 },
            scrollX: 0,
            scrollY: 0,
          },
          files: {},
        });
      }
    };

    loadWhiteboardData();
  }, [user?.id, selectedQuestion?.id]);

  // Save whiteboard data to backend API
  const handleSave = useCallback(
    async (data: ExcalidrawInitialDataState) => {
      if (!user?.id) return;

      try {
        // Sanitize data before saving - remove non-serializable fields
        const dataToSave: ExcalidrawInitialDataState = {
          elements: data.elements || [],
          appState: data.appState ? { ...data.appState } : {},
          files: data.files || {},
        };
        
        // Remove non-serializable fields from appState
        const sanitizedAppState = { ...dataToSave.appState };
        const fieldsToRemove = [
          'collaborators',
          'snapLines',
          'searchMatches',
          'editingLinearElement',
          'selectedLinearElement',
          'multiElement',
          'selectionElement',
        ];
        
        fieldsToRemove.forEach((field) => {
          if (field in sanitizedAppState) {
            delete sanitizedAppState[field];
          }
        });
        
        // Ensure zoom is serializable
        if (sanitizedAppState.zoom && typeof sanitizedAppState.zoom === 'object') {
          sanitizedAppState.zoom = {
            value: sanitizedAppState.zoom.value || 1,
          };
        }
        
        // Save to backend API
        await whiteboardService.saveWhiteboardData({
          questionId: selectedQuestion?.id,
          elements: JSON.stringify(dataToSave.elements),
          appState: JSON.stringify(sanitizedAppState),
          files: JSON.stringify(dataToSave.files || {}),
        });
        
        setWhiteboardData(dataToSave);
      } catch (error) {
        console.error('Failed to save whiteboard data to backend:', error);
      }
    },
    [user?.id, selectedQuestion?.id]
  );

  // Export whiteboard as JSON (simplified, not used but required by Whiteboard component)
  const handleExportJSON = useCallback((data: ExcalidrawInitialDataState) => {
    try {
      const jsonString = JSON.stringify(data, null, 2);
      const blob = new Blob([jsonString], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `whiteboard-${Date.now()}.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Failed to export whiteboard as JSON:', error);
    }
  }, []);

  const handleOpenQuestionModal = useCallback(() => {
    setIsQuestionModalOpen(true);
  }, []);

  const handleCloseQuestionModal = useCallback(() => {
    setIsQuestionModalOpen(false);
  }, []);

  const handleSelectQuestion = useCallback((question: QuestionList) => {
    setSelectedQuestion(question);
    console.log('Selected question:', question);
  }, []);

  if (isLoading) {
    return (
      <div className="whiteboard-page-loading">
        <div>Loading...</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  // Only render whiteboard when we have valid data (always true now, but keep check for safety)
  if (!whiteboardData) {
    return (
      <div className="whiteboard-page-loading">
        <div>Initializing whiteboard...</div>
      </div>
    );
  }

  const handleToggleSidebar = useCallback(() => {
    setIsSidebarOpen((prev) => !prev);
  }, []);

  return (
    <div className="whiteboard-page">
      <Navbar />
      <div className="whiteboard-page-content">
        <QuestionSidebar
          isOpen={isSidebarOpen}
          onToggle={handleToggleSidebar}
          selectedQuestion={selectedQuestion}
          onSelectQuestion={handleSelectQuestion}
          onOpenQuestionModal={handleOpenQuestionModal}
        />
        <div className={`whiteboard-main-area ${isSidebarOpen ? 'with-sidebar' : ''}`}>
          <div className="whiteboard-page-canvas">
            <Whiteboard
              initialData={whiteboardData}
              onSave={handleSave}
              onExport={handleExportJSON}
              gridEnabled={isGridEnabled}
            />
          </div>
        </div>
      </div>
      <SystemDesignQuestionModal
        isOpen={isQuestionModalOpen}
        onClose={handleCloseQuestionModal}
        onSelectQuestion={handleSelectQuestion}
      />
    </div>
  );
};
