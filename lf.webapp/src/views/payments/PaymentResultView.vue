<script setup>
import { onMounted, onUnmounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import { fetchPaymentOrder } from '@/services/paymentService';

const props = defineProps({
  outcome: { type: String, required: true },
});

const route = useRoute();
const router = useRouter();

const MAX_ATTEMPTS = 6;
const POLL_INTERVAL_MS = 2000;

// 'checking' | 'done' | 'pending' | 'error'
const state = ref(props.outcome === 'fail' ? 'fail' : 'checking');
let timer = null;
let attempts = 0;

const orderId = Number(route.query.InvId ?? route.query.invId);

async function poll() {
  if (!Number.isInteger(orderId) || orderId <= 0) {
    state.value = 'error';
    return;
  }

  attempts += 1;
  try {
    const order = await fetchPaymentOrder(orderId);
    if (order.status === 'Paid') {
      state.value = 'done';
      router.replace({ name: 'CourseLearn', params: { enrollmentId: order.enrollmentId } });
      return;
    }
  } catch {
    if (attempts >= 2) {
      state.value = 'error';
      return;
    }
  }

  if (attempts >= MAX_ATTEMPTS) {
    state.value = 'pending';
    return;
  }

  timer = window.setTimeout(poll, POLL_INTERVAL_MS);
}

function goToCourses() {
  router.push({ name: 'CoursesActive' });
}

function goToCatalog() {
  router.push({ name: 'CoursesAvailable' });
}

onMounted(() => {
  if (props.outcome !== 'fail') poll();
});

onUnmounted(() => {
  if (timer) window.clearTimeout(timer);
});
</script>

<template>
  <section class="container mx-auto flex max-w-xl flex-col items-center px-6 py-20 text-center">
    <template v-if="state === 'fail'">
      <h1 class="font-display text-2xl font-semibold tracking-tight text-ink">
        {{ $t('payments.fail.title') }}
      </h1>
      <p class="mt-4 text-ink-muted">
        {{ $t('payments.fail.body') }}
      </p>
      <button
        type="button"
        class="mt-8 rounded-lg border border-border-subtle bg-card px-5 py-2 text-ink hover:bg-card-hover"
        @click="goToCatalog"
      >
        {{ $t('payments.fail.to_catalog') }}
      </button>
    </template>

    <template v-else-if="state === 'checking' || state === 'done'">
      <h1 class="font-display text-2xl font-semibold tracking-tight text-ink">
        {{ state === 'done' ? $t('payments.success.done') : $t('payments.success.checking') }}
      </h1>
    </template>

    <template v-else-if="state === 'pending'">
      <h1 class="font-display text-2xl font-semibold tracking-tight text-ink">
        {{ $t('payments.success.pending_title') }}
      </h1>
      <p class="mt-4 text-ink-muted">
        {{ $t('payments.success.pending_body') }}
      </p>
      <button
        type="button"
        class="mt-8 rounded-lg border border-border-subtle bg-card px-5 py-2 text-ink hover:bg-card-hover"
        @click="goToCourses"
      >
        {{ $t('payments.success.to_courses') }}
      </button>
    </template>

    <template v-else>
      <p class="text-ink-muted">
        {{ $t('payments.success.error') }}
      </p>
      <button
        type="button"
        class="mt-8 rounded-lg border border-border-subtle bg-card px-5 py-2 text-ink hover:bg-card-hover"
        @click="goToCourses"
      >
        {{ $t('payments.success.to_courses') }}
      </button>
    </template>
  </section>
</template>
