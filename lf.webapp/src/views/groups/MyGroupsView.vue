<script setup>
import { onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { MessageSquare } from 'lucide-vue-next';
import { fetchMyGroups } from '@/services/groupService';
import { useGroupChatStore } from '@/stores/groupChatStore';
import { formatLectureDateTime } from '@/lib/lectures';
import GroupsPageShell from '@/components/groups/GroupsPageShell.vue';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

const { t, locale } = useI18n();
const chatStore = useGroupChatStore();

const groups = ref([]);
const loading = ref(true);
const loadFailed = ref(false);

async function load() {
  loading.value = true;
  loadFailed.value = false;
  try {
    groups.value = await fetchMyGroups();
  } catch {
    loadFailed.value = true;
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  load();
  chatStore.refreshUnreadCounts();
});
</script>

<template>
  <GroupsPageShell
    :eyebrow="t('groups.eyebrow')"
    :title="t('groups.title')"
    :subtitle="t('groups.subtitle')"
  >
    <p
      v-if="loadFailed"
      class="lf-page__error"
      role="alert"
    >
      {{ $t('groups.load_error') }}
    </p>
    <p
      v-else-if="loading"
      class="lf-page__hint"
    >
      {{ $t('courses.loading') }}
    </p>
    <p
      v-else-if="!groups.length"
      class="lf-page__hint"
    >
      {{ $t('groups.empty') }}
    </p>

    <ul
      v-else
      class="lf-page__list"
    >
      <li
        v-for="group in groups"
        :key="group.id"
        class="lf-page__card my-group"
      >
        <div class="my-group__body">
          <p class="lf-page__meta">
            {{ group.courseTitle }}
          </p>
          <h2 class="my-group__name">
            {{ group.name }}
            <Badge
              v-if="group.isTeaching"
              variant="muted"
            >
              {{ $t('groups.teaching_badge') }}
            </Badge>
          </h2>
          <p
            v-if="group.description"
            class="my-group__description"
          >
            {{ group.description }}
          </p>
          <p class="lf-page__meta">
            {{ $t('groups.members_count', { count: group.memberCount }) }}
            ·
            {{ group.nextLectureStartsAt
              ? $t('groups.next_lecture', { date: formatLectureDateTime(group.nextLectureStartsAt, locale) })
              : $t('groups.no_upcoming') }}
          </p>
        </div>

        <Button
          as-child
          variant="outline"
        >
          <router-link :to="{ name: 'GroupChat', params: { groupId: group.id } }">
            <MessageSquare class="size-4" />
            {{ $t('groups.open_chat') }}
            <Badge
              v-if="chatStore.unreadByGroup[group.id]"
              variant="coral"
            >
              {{ $t('groups.unread', { count: chatStore.unreadByGroup[group.id] }) }}
            </Badge>
          </router-link>
        </Button>
      </li>
    </ul>
  </GroupsPageShell>
</template>

<style scoped>
.my-group {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.my-group__body {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  min-width: 0;
}

.my-group__name {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1.15rem;
  font-weight: 600;
  color: var(--color-ink);
}

.my-group__description {
  color: var(--color-ink-muted);
  line-height: 1.5;
}
</style>
