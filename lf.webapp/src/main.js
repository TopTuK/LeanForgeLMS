import './main.css';

import { createApp } from 'vue';
import App from './App.vue';

import { createVuestic } from "vuestic-ui";
import 'vuestic-ui/styles/essential.css';
import 'vuestic-ui/styles/typography.css';

import { i18n, setLocale } from './i18n/index.js';
import { currentTheme, setTheme } from './theme/index.js';

import router from "@/router/index.js";

import { createPinia } from 'pinia';

setLocale(i18n.global.locale.value)
setTheme(currentTheme.value);

const app = createApp(App);
const pinia = createPinia()

// Using section
app.use(createVuestic());
app.use(i18n);
app.use(pinia);
app.use(router);

app.mount('#app');
