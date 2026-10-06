<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ArrowLeft, Send, Trash2 } from 'lucide-vue-next';
import { fetchGroup } from '@/services/groupService';
import {
  fetchGroupMessages,
  markGroupChatRead,
  postGroupMessage,
  removeGroupMessage,
} from '@/services/groupChatService';
import { GroupChatEvents, joinGroupChat, leaveGroupChat, onGroupChatEvent } from '@/services/groupChatHub';
import { useGroupChatStore } from '@/stores/groupChatStore';
import { formatLectureDateTime } from '@/lib/lectures';
import GroupsPageShell from '@/components/groups/GroupsPageShell.vue';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';

const MAX_BODY_LENGTH = 4000;

const route = useRoute();
const { t, locale } = useI18n();
const chatStore = useGroupChatStore();

const groupId = computed(() => Number(route.params.groupId));

const group = ref(null);
const messages = ref([]);
const hasMore = ref(false);
const viewerUserId = ref(null);
const loading = ref(true);
const loadingMore = ref(false);
const errorMessage = ref('');
const liveUnavailable = ref(false);
const draft = ref('');
const sending = ref(false);
const scroller = ref(null);

const unsubscribers = [];

const canSend = computed(() => draft.value.trim().length > 0 && draft.value.length <= MAX_BODY_LENGTH && !sending.value);

// Pushes are viewer-neutral; "mine" is derived from the id the history endpoint reported.
function normalize(message) {
  return { ...message, isMine: message.isMine || message.authorUserId === viewerUserId.value };
}

function upsert(message) {
  const index = messages.value.findIndex((m) => m.id === message.id);
  if (index === -1) {
    messages.value = [...messages.value, normalize(message)].sort((a, b) => a.id - b.id);
  } else {
    messages.value.splice(index, 1, normalize(message));
  }
}

function isNearBottom() {
  const el = scroller.value;
  return !el || el.scrollHeight - el.scrollTop - el.clientHeight < 120;
}

async function scrollToBottom() {
  await nextTick();
  if (scroller.value) scroller.value.scrollTop = scroller.value.scrollHeight;
}

async function markRead() {
  const last = messages.value.at(-1);
  if (!last) return;
  try {
    await markGroupChatRead(groupId.value, last.id);
    chatStore.clearGroup(groupId.value);
  } catch {
    // Read markers only drive badges; a failure here must not disturb the conversation.
  }
}

async function load() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const [detail, page] = await Promise.all([fetchGroup(groupId.value), fetchGroupMessages(groupId.value)]);
    group.value = detail;
    viewerUserId.value = page.viewerUserId;
    hasMore.value = page.hasMore;
    messages.value = page.items.map(normalize);
    await scrollToBottom();
    markRead();
  } catch (err) {
    errorMessage.value = err?.response?.status === 403 ? t('chat.forbidden') : t('chat.load_error');
  } finally {
    loading.value = false;
  }
}

async function loadMore() {
  if (!messages.value.length || loadingMore.value) return;
  loadingMore.value = true;
  const el = scroller.value;
  const previousHeight = el?.scrollHeight ?? 0;
  try {
    const page = await fetchGroupMessages(groupId.value, { beforeId: messages.value[0].id });
    hasMore.value = page.hasMore;
    messages.value = [...page.items.map(normalize), ...messages.value];
    // Keep the reader's place instead of jumping to the newly prepended top.
    await nextTick();
    if (el) el.scrollTop = el.scrollHeight - previousHeight;
  } catch {
    errorMessage.value = t('chat.load_error');
  } finally {
    loadingMore.value = false;
  }
}

async function send() {
  if (!canSend.value) return;
  sending.value = true;
  errorMessage.value = '';
  try {
    const message = await postGroupMessage(groupId.value, draft.value.trim());
    draft.value = '';
    upsert(message);
    await scrollToBottom();
    markRead();
  } catch {
    errorMessage.value = t('chat.send_error');
  } finally {
    sending.value = false;
  }
}

function onKeydown(event) {
  if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
    event.preventDefault();
    send();
  }
}

async function remove(message) {
  try {
    upsert(await removeGroupMessage(groupId.value, message.id));
  } catch {
    errorMessage.value = t('chat.load_error');
  }
}

async function connectLive() {
  unsubscribers.push(onGroupChatEvent(GroupChatEvents.messagePosted, async (message) => {
    if (message.groupId !== groupId.value) return;
    const stickToBottom = isNearBottom();
    upsert(message);
    if (stickToBottom) await scrollToBottom();
    if (document.visibilityState === 'visible') markRead();
  }));

  unsubscribers.push(onGroupChatEvent(GroupChatEvents.messageDeleted, ({ groupId: id, messageId }) => {
    if (id !== groupId.value) return;
    const existing = messages.value.find((m) => m.id === messageId);
    if (existing) upsert({ ...existing, isDeleted: true, body: null });
  }));

  try {
    await joinGroupChat(groupId.value);
  } catch {
    // REST still works without the socket; the reader just won't see others' messages live.
    liveUnavailable.value = true;
  }
}

onMounted(async () => {
  await load();
  if (!errorMessage.value) connectLive();
});

onBeforeUnmount(() => {
  unsubscribers.forEach((off) => off());
  leaveGroupChat(groupId.value).catch(() => {});
});
</script>

<template>
  <GroupsPageShell
    :eyebrow="group?.courseTitle ?? ''"
    :title="group?.name ?? $t('chat.members')"
    wide
  >
    <template #before-heading>
      <router-link
        :to="{ name: 'Groups' }"
        class="chat-back"
      >
        <ArrowLeft class="size-4" />
        {{ $t('chat.back') }}
      </router-link>
    </template>

    <p
      v-if="errorMessage"
      class="lf-page__error"
      role="alert"
    >
      {{ errorMessage }}
    </p>
    <p
      v-if="liveUnavailable"
      class="lf-page__hint"
      role="status"
    >
      {{ $t('chat.reconnecting') }}
    </p>

    <div
      v-if="group"
      class="chat-layout"
    >
      <div class="lf-page__card chat-panel">
        <div
          ref="scroller"
          class="chat-messages"
          aria-live="polite"
        >
          <div
            v-if="hasMore"
            class="chat-messages__more"
          >
            <Button
              variant="ghost"
              size="sm"
              :disabled="loadingMore"
              @click="loadMore"
            >
              {{ $t('chat.load_more') }}
            </Button>
          </div>

          <p
            v-if="!loading && !messages.length"
            class="lf-page__hint chat-messages__empty"
          >
            {{ $t('chat.empty') }}
          </p>

          <div
            v-for="message in messages"
            :key="message.id"
            class="chat-message"
            :class="{ 'is-mine': message.isMine }"
          >
            <div class="chat-message__meta">
              <span class="chat-message__author">{{ message.authorName }}</span>
              <Badge
                v-if="message.authorIsStaff"
                variant="coral"
              >
                {{ $t('chat.staff_badge') }}
              </Badge>
              <time :datetime="message.sentAt">{{ formatLectureDateTime(message.sentAt, locale) }}</time>
              <button
                v-if="!message.isDeleted && (message.isMine || group.canManage)"
                type="button"
                class="chat-message__delete"
                :aria-label="$t('chat.delete')"
                @click="remove(message)"
              >
                <Trash2 class="size-3.5" />
              </button>
            </div>
            <p
              class="chat-message__body"
              :class="{ 'is-deleted': message.isDeleted }"
            >
              {{ message.isDeleted ? $t('chat.deleted') : message.body }}
            </p>
          </div>
        </div>

        <form
          class="chat-compose"
          @submit.prevent="send"
        >
          <Textarea
            v-model="draft"
            :rows="2"
            :placeholder="$t('chat.placeholder')"
            :maxlength="MAX_BODY_LENGTH"
            @keydown="onKeydown"
          />
          <Button
            type="submit"
            :disabled="!canSend"
            :aria-label="$t('chat.send')"
          >
            <Send class="size-4" />
            {{ sending ? $t('chat.sending') : $t('chat.send') }}
          </Button>
        </form>
      </div>

      <aside class="lf-page__card chat-members">
        <h2 class="chat-members__title">
          {{ $t('chat.members') }} · {{ group.members.length }}
        </h2>
        <ul>
          <li
            v-for="member in group.members"
            :key="member.userId"
          >
            {{ member.firstName }} {{ member.lastName }}
          </li>
        </ul>
      </aside>
    </div>
  </GroupsPageShell>
</template>

<style scoped>
.chat-back {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  margin-bottom: 1.25rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-ink-muted);
}

.chat-back:hover {
  color: var(--color-ink);
}

.chat-layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 15rem;
  gap: 1rem;
  margin-top: 2rem;
}

.chat-panel {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 0;
  overflow: hidden;
}

.chat-messages {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  height: min(60vh, 36rem);
  overflow-y: auto;
  padding: 1rem;
}

.chat-messages__more {
  display: flex;
  justify-content: center;
}

.chat-messages__empty {
  margin: auto;
  text-align: center;
}

.chat-message {
  align-self: flex-start;
  max-width: min(36rem, 85%);
  padding: 0.55rem 0.8rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: 0.85rem 0.85rem 0.85rem 0.25rem;
  background: var(--color-surface-900);
}

.chat-message.is-mine {
  align-self: flex-end;
  border-radius: 0.85rem 0.85rem 0.25rem 0.85rem;
  background: var(--color-accent-soft);
}

.chat-message__meta {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  flex-wrap: wrap;
  font-size: 0.75rem;
  color: var(--color-ink-muted);
}

.chat-message__author {
  font-weight: 600;
  color: var(--color-ink);
}

.chat-message__delete {
  margin-left: auto;
  color: var(--color-ink-faint);
}

.chat-message__delete:hover {
  color: var(--color-accent-coral);
}

.chat-message__body {
  margin-top: 0.2rem;
  color: var(--color-ink);
  line-height: 1.5;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.chat-message__body.is-deleted {
  font-style: italic;
  color: var(--color-ink-muted);
}

.chat-compose {
  display: flex;
  gap: 0.5rem;
  align-items: flex-end;
  padding: 0 1rem 1rem;
}

.chat-members__title {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--color-ink);
}

.chat-members ul {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin: 0.75rem 0 0;
  padding: 0;
  list-style: none;
  font-size: 0.85rem;
  color: var(--color-ink-muted);
}

@media (max-width: 800px) {
  .chat-layout {
    grid-template-columns: 1fr;
  }
}
</style>
