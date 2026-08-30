// Runs before first paint to avoid a theme flash. Kept as an external file (not inline)
// so the production Content-Security-Policy can use script-src 'self' with no 'unsafe-inline'.
(() => {
  const saved = localStorage.getItem('leanforge-theme');
  const theme = ['light', 'dark'].includes(saved)
    ? saved
    : (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  document.documentElement.dataset.theme = theme;
  document.documentElement.style.colorScheme = theme;
})();
