import { onBeforeUnmount, ref } from 'vue';

/**
 * Blob-fetches cover images for a list of items (auth is required, so a plain <img src>
 * can't hit the endpoint directly) and manages the resulting object URLs' lifecycle.
 */
export function useCourseCoverImages(fetchCoverImageUrl) {
  const coverImageUrls = ref({});

  function clear() {
    Object.values(coverImageUrls.value).forEach((url) => URL.revokeObjectURL(url));
    coverImageUrls.value = {};
  }

  async function load(items, getCourseId) {
    clear();

    const imageItems = items.filter((item) => item.coverType === 'Image');
    await Promise.all(imageItems.map(async (item) => {
      const courseId = getCourseId(item);
      try {
        coverImageUrls.value[courseId] = await fetchCoverImageUrl(courseId);
      } catch {
        // No cover preview if the fetch fails; the card falls back to the no-cover state.
      }
    }));
  }

  onBeforeUnmount(clear);

  return { coverImageUrls, load };
}
