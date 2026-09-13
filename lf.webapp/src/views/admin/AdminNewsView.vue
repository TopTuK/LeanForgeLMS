<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { Plus } from 'lucide-vue-next';
import { fetchAdminNews, deleteNewsPost } from '@/services/adminService';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Dialog } from '@/components/ui/dialog';

const { t, locale } = useI18n();
const router = useRouter();

const PAGE_SIZE = 20;

const posts = ref([]);
const totalCount = ref(0);
const page = ref(1);
const loading = ref(false);
const errorMessage = ref('');

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / PAGE_SIZE)));

// Drafts have never been published, so they show their creation date instead.
function formatDate(post) {
  return new Date(post.publishedAt ?? post.createdAt).toLocaleDateString(locale.value);
}

async function loadPosts() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await fetchAdminNews({ page: page.value, pageSize: PAGE_SIZE });
    posts.value = result.items;
    totalCount.value = result.totalCount;
  } catch {
    errorMessage.value = t('admin.news.load_error');
  } finally {
    loading.value = false;
  }
}

watch(page, loadPosts);
onMounted(loadPosts);

function openCreate() {
  router.push({ name: 'AdminNewsCreate' });
}

function openEditor(post) {
  router.push({ name: 'AdminNewsEdit', params: { id: post.id } });
}

const deleteModalShown = ref(false);
const deleteTarget = ref(null);

function openDeleteModal(post) {
  deleteTarget.value = post;
  deleteModalShown.value = true;
}

async function confirmDelete() {
  errorMessage.value = '';
  try {
    await deleteNewsPost(deleteTarget.value.id);
    deleteModalShown.value = false;
    if (posts.value.length === 1 && page.value > 1) page.value -= 1;
    else await loadPosts();
  } catch {
    deleteModalShown.value = false;
    errorMessage.value = t('admin.news.delete_error');
  }
}
</script>

<template>
  <div class="admin-news">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="font-display text-2xl font-semibold tracking-tight text-ink">
          {{ $t('admin.news.title') }}
        </h1>
        <p class="mt-1 text-sm text-ink-muted">
          {{ $t('admin.news.subtitle') }}
        </p>
      </div>
      <Button @click="openCreate">
        <Plus class="size-4" />
        {{ $t('admin.news.add_action') }}
      </Button>
    </div>

    <p
      v-if="errorMessage"
      class="mt-4 rounded-md border border-accent-coral bg-accent-soft px-3 py-2 text-sm font-semibold text-accent-coral"
    >
      {{ errorMessage }}
    </p>

    <div class="mt-4 overflow-x-auto rounded-lg border border-border-subtle bg-card">
      <table class="w-full text-left text-sm">
        <thead class="border-b border-border-subtle text-ink-muted">
          <tr>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.news.col_title') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.news.col_visibility') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.news.col_status') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.news.col_date') }}
            </th>
            <th class="px-3 py-2 font-semibold">
              {{ $t('admin.news.col_images') }}
            </th>
            <th class="px-3 py-2" />
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="6"
            >
              {{ $t('courses.loading') }}
            </td>
          </tr>
          <tr v-else-if="!posts.length">
            <td
              class="px-3 py-6 text-ink-muted"
              colspan="6"
            >
              {{ $t('admin.news.empty') }}
            </td>
          </tr>
          <tr
            v-for="post in posts"
            v-else
            :key="post.id"
            class="border-t border-border-subtle"
          >
            <td class="px-3 py-2 text-ink">
              {{ post.title }}
            </td>
            <td class="px-3 py-2">
              <Badge :variant="post.visibility === 'MembersOnly' ? 'muted' : 'default'">
                {{ post.visibility === 'MembersOnly'
                  ? $t('admin.news.visibility_members')
                  : $t('admin.news.visibility_public') }}
              </Badge>
            </td>
            <td class="px-3 py-2">
              <span :class="post.isPublished ? 'font-semibold text-ink' : 'text-ink-muted'">
                {{ post.isPublished ? $t('admin.news.published') : $t('admin.news.draft') }}
              </span>
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ formatDate(post) }}
            </td>
            <td class="px-3 py-2 text-ink-muted">
              {{ post.images.length }}
            </td>
            <td class="px-3 py-2">
              <div class="flex flex-wrap justify-end gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  @click="openEditor(post)"
                >
                  {{ $t('admin.news.edit') }}
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  @click="openDeleteModal(post)"
                >
                  {{ $t('admin.news.delete') }}
                </Button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="mt-4 flex items-center justify-center gap-3 text-sm text-ink-muted">
      <Button
        variant="outline"
        size="sm"
        :disabled="page <= 1"
        @click="page -= 1"
      >
        ‹
      </Button>
      <span>{{ page }} / {{ totalPages }}</span>
      <Button
        variant="outline"
        size="sm"
        :disabled="page >= totalPages"
        @click="page += 1"
      >
        ›
      </Button>
    </div>

    <Dialog
      v-model:open="deleteModalShown"
      :title="$t('admin.news.delete_title')"
      :description="$t('admin.news.delete_confirm', { title: deleteTarget?.title ?? '' })"
      :confirm-label="$t('admin.news.delete')"
      :cancel-label="$t('admin.news.cancel')"
      danger
      @confirm="confirmDelete"
    />
  </div>
</template>
