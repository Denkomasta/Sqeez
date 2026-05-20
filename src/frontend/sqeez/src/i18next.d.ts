import 'i18next'
import { ParseKeys } from 'i18next'
import translation from '../public/locales/en/translation.json'

/** Type-safe key union derived from the English base translation file. */
export type TranslationKey = ParseKeys

/** Adds project translation resources to i18next's TypeScript module shape. */
declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation'
    resources: {
      translation: typeof translation
    }
  }
}
