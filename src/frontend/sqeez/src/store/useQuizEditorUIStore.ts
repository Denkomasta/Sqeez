import { create } from 'zustand'

interface QuizEditorUIState {
  /** Selected question id in the builder; null means the quiz settings panel is active. */
  activeQuestionId: number | null
  /** Legacy sidebar open flag kept for builder layouts that still read it. */
  isSidebarOpen: boolean
  /** True when backend rules prevent editing, usually because the quiz already has attempts. */
  isLocked: boolean

  actions: {
    /** Selects a question or the quiz settings view when questionId is null. */
    selectQuestion: (questionId: number | null) => void
    /** Toggles the legacy sidebar open flag. */
    toggleSidebar: () => void
    /** Stores the backend-derived edit lock for the current quiz. */
    setLocked: (isLocked: boolean) => void
    /** Clears selected question, sidebar state, and lock status when leaving the builder. */
    resetEditor: () => void
  }
}

/**
 * Keeps quiz-builder UI state separate from persisted quiz data.
 * `isLocked` is set when backend rules prevent editing an attempted quiz, while actual
 * quiz/question/option values continue to live in TanStack Query caches.
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
