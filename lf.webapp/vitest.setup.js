import { afterEach, vi } from 'vitest';
import { cleanup } from '@testing-library/vue';
import '@testing-library/jest-dom/vitest';

// The i18n and theme modules read these at import time; pin the locale so
// component assertions can rely on English strings.
window.localStorage.setItem('leanforge-locale', 'en');
window.localStorage.setItem('leanforge-theme', 'light');

// jsdom implements neither object-URL helper; several stores/services call them.
if (typeof URL.createObjectURL !== 'function') {
  URL.createObjectURL = vi.fn(() => 'blob:mock/00000000-0000-0000-0000-000000000000');
}
if (typeof URL.revokeObjectURL !== 'function') {
  URL.revokeObjectURL = vi.fn();
}

// src/theme/index.js calls matchMedia during module evaluation.
if (typeof window.matchMedia !== 'function') {
  window.matchMedia = vi.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  }));
}

// jsdom lacks IntersectionObserver; @vueuse/motion's visible-once directives use it.
if (typeof window.IntersectionObserver !== 'function') {
  class IntersectionObserverStub {
    observe() {}
    unobserve() {}
    disconnect() {}
    takeRecords() { return []; }
  }
  window.IntersectionObserver = IntersectionObserverStub;
  globalThis.IntersectionObserver = IntersectionObserverStub;
}

if (!globalThis.crypto?.randomUUID) {
  globalThis.crypto = { ...globalThis.crypto, randomUUID: () => `uuid-${Math.random().toString(16).slice(2)}` };
}

afterEach(() => {
  cleanup();
});
