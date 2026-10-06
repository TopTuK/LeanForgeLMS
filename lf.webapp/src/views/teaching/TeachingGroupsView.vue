<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { MessageSquare, Pencil, Plus, Trash2, UserPlus, X } from 'lucide-vue-next';
import {
  addGroupMembers,
  createGroup,
  fetchCourseGroups,
  fetchEligibleStudents,
  fetchGroup,
  removeGroup,
  removeGroupMember,
  updateGroup,
} from '@/services/groupService';
import GroupsPageShell from '@/components/groups/GroupsPageShell.vue';
import TeachingCourseTabs from '@/components/groups/TeachingCourseTabs.vue';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Dialog } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';

const route = useRoute();
const { t } = useI18n();

const courseId = computed(() => Number(route.params.courseId));

const groups = ref([]);
const selected = ref(null);
const eligible = ref([]);
const loading = ref(true);
const errorMessage = ref('');

const formOpen = ref(false);
const editing = ref(null);
const form = ref({ name: '', description: '' });
const saving = ref(false);

const deleteOpen = ref(false);

const pickerOpen = ref(false);
const pickerSearch = ref('');
const picked = ref([]);

const memberIds = computed(() => new Set(selected.value?.members.map((m) => m.userId) ?? []));

const candidates = computed(() => {
  const query = pickerSearch.value.trim().toLowerCase();
  return eligible.value
    .filter((s) => !memberIds.value.has(s.userId))
    .filter((s) => !query || `${s.firstName} ${s.lastName} ${s.email}`.toLowerCase().includes(query));
});

// The API answers 409 with a readable reason (duplicate name, student not enrolled).
function describeError(err, fallbackKey) {
  return err?.response?.status === 409 && typeof err.response.data === 'string'
    ? err.response.data
    : t(fallbackKey);
}

async function loadGroups() {
  groups.value = await fetchCourseGroups(courseId.value);
}

async function select(groupId) {
  errorMessage.value = '';
  try {
    selected.value = await fetchGroup(groupId);
  } catch {
    errorMessage.value = t('teaching.groups.load_error');
  }
}

async function load() {
  loading.value = true;
  errorMessage.value = '';
  try {
    await loadGroups();
    eligible.value = await fetchEligibleStudents(courseId.value);
    if (groups.value.length) await select(groups.value[0].id);
  } catch {
    errorMessage.value = t('teaching.groups.load_error');
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  editing.value = null;
  form.value = { name: '', description: '' };
  formOpen.value = true;
}

function openEdit() {
  editing.value = selected.value;
  form.value = { name: selected.value.name, description: selected.value.description ?? '' };
  formOpen.value = true;
}

async function save() {
  if (!form.value.name.trim() || saving.value) return;
  saving.value = true;
  errorMessage.value = '';
  try {
    const payload = { name: form.value.name.trim(), description: form.value.description.trim() || null };
    const saved = editing.value
      ? await updateGroup(editing.value.id, payload)
      : await createGroup(courseId.value, payload);
    formOpen.value = false;
    await loadGroups();
    selected.value = saved;
  } catch (err) {
    errorMessage.value = describeError(err, 'teaching.groups.save_error');
  } finally {
    saving.value = false;
  }
}

async function confirmDelete() {
  deleteOpen.value = false;
  try {
    await removeGroup(selected.value.id);
    selected.value = null;
    await loadGroups();
    if (groups.value.length) await select(groups.value[0].id);
  } catch {
    errorMessage.value = t('teaching.groups.save_error');
  }
}

function openPicker() {
  picked.value = [];
  pickerSearch.value = '';
  pickerOpen.value = true;
}

async function addPicked() {
  if (!picked.value.length) return;
  try {
    selected.value = await addGroupMembers(selected.value.id, picked.value);
    pickerOpen.value = false;
    await loadGroups();
  } catch (err) {
    errorMessage.value = describeError(err, 'teaching.groups.save_error');
  }
}

async function removeMember(member) {
  try {
    selected.value = await removeGroupMember(selected.value.id, member.userId);
    await loadGroups();
  } catch {
    errorMessage.value = t('teaching.groups.save_error');
  }
}

onMounted(load);
</script>

<template>
  <GroupsPageShell
    :title="t('teaching.groups.title')"
    :subtitle="t('teaching.groups.subtitle')"
    wide
  >
    <template #before-heading>
      <TeachingCourseTabs :course-id="courseId" />
    </template>

    <template #actions>
      <Button @click="openCreate">
        <Plus class="size-4" />
        {{ $t('teaching.groups.create') }}
      </Button>
    </template>

    <p
      v-if="errorMessage"
      class="lf-page__error"
      role="alert"
    >
      {{ errorMessage }}
    </p>
    <p
      v-if="loading"
      class="lf-page__hint"
    >
      {{ $t('courses.loading') }}
    </p>
    <p
      v-else-if="!groups.length"
      class="lf-page__hint"
    >
      {{ $t('teaching.groups.empty') }}
    </p>

    <div
      v-else
      class="teaching-groups"
    >
      <ul class="teaching-groups__list">
        <li
          v-for="group in groups"
          :key="group.id"
        >
          <button
            type="button"
            class="teaching-groups__item"
            :class="{ 'is-active': selected?.id === group.id }"
            @click="select(group.id)"
          >
            <span class="teaching-groups__item-name">{{ group.name }}</span>
            <span class="lf-page__meta">{{ $t('groups.members_count', { count: group.memberCount }) }}</span>
          </button>
        </li>
      </ul>

      <section
        v-if="selected"
        class="lf-page__card teaching-groups__detail"
      >
        <header class="teaching-groups__detail-head">
          <div>
            <h2 class="teaching-groups__detail-title">
              {{ selected.name }}
            </h2>
            <p
              v-if="selected.description"
              class="lf-page__meta"
            >
              {{ selected.description }}
            </p>
          </div>
          <div class="lf-page__actions">
            <Button
              as-child
              variant="outline"
              size="sm"
            >
              <router-link :to="{ name: 'GroupChat', params: { groupId: selected.id } }">
                <MessageSquare class="size-4" />
                {{ $t('teaching.groups.open_chat') }}
              </router-link>
            </Button>
            <Button
              variant="ghost"
              size="sm"
              :aria-label="$t('teaching.groups.edit')"
              @click="openEdit"
            >
              <Pencil class="size-4" />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              :aria-label="$t('teaching.groups.delete')"
              @click="deleteOpen = true"
            >
              <Trash2 class="size-4" />
            </Button>
          </div>
        </header>

        <div class="teaching-groups__members-head">
          <h3>{{ $t('teaching.groups.members_title') }}</h3>
          <Button
            size="sm"
            variant="outline"
            @click="openPicker"
          >
            <UserPlus class="size-4" />
            {{ $t('teaching.groups.add_members') }}
          </Button>
        </div>

        <p
          v-if="!selected.members.length"
          class="lf-page__meta"
        >
          {{ $t('teaching.groups.no_members') }}
        </p>
        <ul
          v-else
          class="teaching-groups__members"
        >
          <li
            v-for="member in selected.members"
            :key="member.userId"
          >
            <span class="teaching-groups__member-name">{{ member.firstName }} {{ member.lastName }}</span>
            <span class="lf-page__meta teaching-groups__member-email">{{ member.email }}</span>
            <Badge
              v-if="!member.isEnrolled"
              variant="muted"
            >
              {{ $t('teaching.groups.not_enrolled') }}
            </Badge>
            <Button
              variant="ghost"
              size="sm"
              :aria-label="$t('teaching.groups.remove_member')"
              @click="removeMember(member)"
            >
              <X class="size-4" />
            </Button>
          </li>
        </ul>
      </section>
    </div>

    <Dialog
      v-model:open="formOpen"
      :title="editing ? $t('teaching.groups.edit') : $t('teaching.groups.create')"
      :confirm-label="$t('teaching.groups.save')"
      :cancel-label="$t('teaching.groups.cancel')"
      @confirm="save"
    >
      <form @submit.prevent="save">
        <div class="lf-page__field">
          <label for="group-name">{{ $t('teaching.groups.name_label') }}</label>
          <Input
            id="group-name"
            v-model="form.name"
            maxlength="100"
          />
        </div>
        <div class="lf-page__field">
          <label for="group-description">{{ $t('teaching.groups.description_label') }}</label>
          <Textarea
            id="group-description"
            v-model="form.description"
            :rows="3"
            maxlength="1000"
          />
        </div>
      </form>
    </Dialog>

    <Dialog
      v-model:open="deleteOpen"
      :title="$t('teaching.groups.delete_confirm_title')"
      :description="selected ? $t('teaching.groups.delete_confirm_body', { name: selected.name }) : ''"
      :confirm-label="$t('teaching.groups.delete')"
      :cancel-label="$t('teaching.groups.cancel')"
      danger
      @confirm="confirmDelete"
    />

    <Dialog
      v-model:open="pickerOpen"
      :title="$t('teaching.groups.add_members')"
      :confirm-label="$t('teaching.groups.add_selected')"
      :cancel-label="$t('teaching.groups.cancel')"
      @confirm="addPicked"
    >
      <Input
        v-model="pickerSearch"
        :placeholder="$t('teaching.groups.search_students')"
      />
      <p
        v-if="!candidates.length"
        class="lf-page__meta mt-4"
      >
        {{ $t('teaching.groups.no_eligible') }}
      </p>
      <ul
        v-else
        class="teaching-groups__picker"
      >
        <li
          v-for="student in candidates"
          :key="student.userId"
        >
          <label>
            <input
              v-model="picked"
              type="checkbox"
              :value="student.userId"
            >
            <span>{{ student.firstName }} {{ student.lastName }}</span>
            <span class="lf-page__meta">{{ student.email }}</span>
          </label>
        </li>
      </ul>
    </Dialog>
  </GroupsPageShell>
</template>

<style scoped>
.teaching-groups {
  display: grid;
  grid-template-columns: 16rem minmax(0, 1fr);
  gap: 1rem;
  margin-top: 2rem;
}

.teaching-groups__list {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.teaching-groups__item {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  width: 100%;
  padding: 0.65rem 0.85rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
  background: var(--color-card);
  text-align: left;
}

.teaching-groups__item.is-active {
  border-color: var(--color-accent-coral);
}

.teaching-groups__item-name {
  font-weight: 600;
  color: var(--color-ink);
}

.teaching-groups__detail-head {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.teaching-groups__detail-title {
  font-size: 1.2rem;
  font-weight: 600;
  color: var(--color-ink);
}

.teaching-groups__members-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 1.5rem 0 0.75rem;
}

.teaching-groups__members-head h3 {
  font-weight: 600;
  color: var(--color-ink);
}

.teaching-groups__members,
.teaching-groups__picker {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.teaching-groups__members li {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
  padding: 0.45rem 0.75rem;
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-card);
}

.teaching-groups__member-name {
  font-weight: 600;
  color: var(--color-ink);
}

.teaching-groups__member-email {
  margin-right: auto;
}

.teaching-groups__picker {
  max-height: 20rem;
  margin-top: 1rem;
  overflow-y: auto;
}

.teaching-groups__picker label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
  padding: 0.35rem 0.25rem;
  color: var(--color-ink);
  cursor: pointer;
}

@media (max-width: 800px) {
  .teaching-groups {
    grid-template-columns: 1fr;
  }
}
</style>
