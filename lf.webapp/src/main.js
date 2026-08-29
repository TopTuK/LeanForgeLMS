import '@fontsource-variable/unbounded';
import '@fontsource/ibm-plex-sans/400.css';
import '@fontsource/ibm-plex-sans/500.css';
import '@fontsource/ibm-plex-sans/600.css';
import '@fontsource/ibm-plex-sans/700.css';
import '@fontsource/ibm-plex-mono/400.css';
import '@fontsource/ibm-plex-mono/500.css';
import './main.css';

import { createApp } from 'vue';
import { MotionPlugin } from '@vueuse/motion';
import App from './App.vue';

import { i18n, setLocale } from './i18n/index.js';
import { currentTheme, setTheme } from './theme/index.js';

import router from '@/router/index.js';

import { createPinia } from 'pinia';

setLocale(i18n.global.locale.value);
setTheme(currentTheme.value);

const app = createApp(App);
const pinia = createPinia();

app.use(i18n);
app.use(pinia);
app.use(router);
app.use(MotionPlugin);

app.mount('#app');
