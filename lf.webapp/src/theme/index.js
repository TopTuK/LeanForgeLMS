import { ref } from 'vue';

const STORAGE_KEY = 'leanforge-theme';
const SUPPORTED_THEMES = ['light', 'dark'];

const getInitialTheme = () => {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved && SUPPORTED_THEMES.includes(saved)) return saved;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
};

export const currentTheme = ref(getInitialTheme());

export function setTheme(theme) {
    if (!SUPPORTED_THEMES.includes(theme)) return;
    currentTheme.value = theme;
    localStorage.setItem(STORAGE_KEY, theme);
    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;
}

export function toggleTheme() {
    setTheme(currentTheme.value === 'light' ? 'dark' : 'light');
}
