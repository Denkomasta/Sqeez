import { create } from 'zustand'

interface QuizEditorUIState {
  activeQuestionId: number | null
  isSidebarOpen: boolean
  isLocked: boolean

  actions: {
    selectQuestion: (questionId: number | null) => void
    toggleSidebar: () => void
    setLocked: (isLocked: boolean) => void
    resetEditor: () => void
  }
}

/**
 * Keeps quiz-builder UI state separate from persisted quiz data.
 * `isLocked` is set when backend rules prevent editing an attempted quiz.
 */
export const useQuizEditorUIStore = create<QuizEditorUIState>((set) => ({
  activeQuestionId: null,
  isSidebarOpen: true,
  isLocked: false,

  actions: {
    selectQuestion: (questionId) => set({ activeQuestionId: questionId }),

    toggleSidebar: () =>
      set((state) => ({ isSidebarOpen: !state.isSidebarOpen })),

    setLocked: (isLocked) => set({ isLocked }),

    resetEditor: () =>
      set({
        activeQuestionId: null,
        isSidebarOpen: true,
        isLocked: false,
      }),
  },
}))
