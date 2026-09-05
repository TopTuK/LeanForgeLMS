import { defineStore } from 'pinia';
import { ref } from 'vue';
import { fetchPlatformConfig } from '@/services/platformService';

export const usePlatformStore = defineStore('platform', () => {
  // Fail-safe default: assume enrollment is off until the config probe says otherwise.
  const studentEnrollmentEnabled = ref(false);
  const loaded = ref(false);

  let loadPromise = null;

  const ensureLoaded = () => {
    if (loaded.value) return Promise.resolve();
    if (loadPromise) return loadPromise;

    loadPromise = (async () => {
      try {
        const config = await fetchPlatformConfig();
        studentEnrollmentEnabled.value = Boolean(config.studentEnrollmentEnabled);
      } catch {
        studentEnrollmentEnabled.value = false;
      } finally {
        loaded.value = true;
        loadPromise = null;
      }
    })();

    return loadPromise;
  };

  const refresh = async () => {
    loaded.value = false;
    loadPromise = null;
    await ensureLoaded();
  };

  return { studentEnrollmentEnabled, loaded, ensureLoaded, refresh };
});
