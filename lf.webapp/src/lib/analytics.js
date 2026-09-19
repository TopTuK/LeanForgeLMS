import { readonly, ref } from 'vue';
import { addGtag, configure, event, optIn, optOut, set } from 'vue-gtag';
import { GA_MEASUREMENT_ID } from '@/config';

const STORAGE_KEY = 'leanforge-analytics-consent';

// Campaign attribution needs utm_*/gclid; every other query param (Robokassa's signed
// InvId/OutSum/SignatureValue, login redirectTo, ...) must never reach Google.
const TRACKED_QUERY_PARAM = /^(utm_[a-z]+|gclid)$/;

function readConsent() {
  try {
    const value = window.localStorage.getItem(STORAGE_KEY);
    return value === 'granted' || value === 'denied' ? value : null;
  } catch {
    return null;
  }
}

function writeConsent(value) {
  try {
    if (value) window.localStorage.setItem(STORAGE_KEY, value);
    else window.localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Storage blocked: the choice still applies for this page load.
  }
}

const consent = ref(readConsent());
let isLoaded = false;
let userRole = null;

export const isAnalyticsAvailable = () => import.meta.env.PROD;

const isTracking = () => isAnalyticsAvailable() && consent.value === 'granted';

export function pageViewParams(route) {
  const location = new URL(route.path, window.location.origin);
  for (const [key, value] of Object.entries(route.query ?? {})) {
    if (TRACKED_QUERY_PARAM.test(key) && typeof value === 'string') location.searchParams.set(key, value);
  }

  return {
    page_path: route.path,
    page_location: location.toString(),
    page_title: route.name ? String(route.name) : route.path,
  };
}

function applyUserRole() {
  if (userRole) set('user_properties', { role: userRole });
}

function load() {
  if (isLoaded) return;
  isLoaded = true;
  applyUserRole();
  addGtag();
}

function clearGaCookies() {
  const labels = window.location.hostname.split('.');
  const domains = labels.map((_, i) => labels.slice(i).join('.'));
  for (const cookie of document.cookie.split(';')) {
    const name = cookie.split('=')[0].trim();
    if (!name.startsWith('_ga')) continue;
    document.cookie = `${name}=; Max-Age=0; path=/`;
    for (const domain of domains) document.cookie = `${name}=; Max-Age=0; path=/; domain=${domain}`;
  }
}

export function installAnalytics(router) {
  if (!isAnalyticsAvailable()) return;

  configure({
    tagId: GA_MEASUREMENT_ID,
    initMode: 'manual',
    pageTracker: { router, template: pageViewParams },
  });

  if (consent.value === 'granted') load();
}

export function grantConsent() {
  writeConsent('granted');
  consent.value = 'granted';
  if (!isAnalyticsAvailable()) return;
  if (isLoaded) optIn();
  else load();
}

export function denyConsent() {
  writeConsent('denied');
  consent.value = 'denied';
  if (!isLoaded) return;
  optOut();
  clearGaCookies();
}

export function resetConsent() {
  writeConsent(null);
  consent.value = null;
}

export function track(name, params = {}) {
  if (!isTracking()) return;
  event(name, params);
}

const LOGIN_METHOD_KEY = 'leanforge-login-method';

// External sign-in leaves the SPA, so the method is carried across the OAuth round-trip
// and the `login` event is only sent once the session is actually established.
export function trackLoginStarted(method) {
  track('login_start', { method });
  try {
    window.sessionStorage.setItem(LOGIN_METHOD_KEY, method);
  } catch {
    // Storage blocked: the completed sign-in simply goes unreported.
  }
}

export function trackLoginCompleted() {
  let method;
  try {
    method = window.sessionStorage.getItem(LOGIN_METHOD_KEY);
    window.sessionStorage.removeItem(LOGIN_METHOD_KEY);
  } catch {
    return;
  }
  if (method) track('login', { method });
}

export function setUserRole(role) {
  userRole = role ?? null;
  if (isLoaded) applyUserRole();
}

export function useAnalyticsConsent() {
  return {
    consent: readonly(consent),
    isAvailable: isAnalyticsAvailable(),
    grantConsent,
    denyConsent,
    resetConsent,
  };
}
