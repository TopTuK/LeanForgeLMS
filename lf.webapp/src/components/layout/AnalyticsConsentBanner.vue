<script setup>
import { computed } from 'vue';
import { Button } from '@/components/ui/button';
import { useAnalyticsConsent } from '@/lib/analytics';

const { consent, isAvailable, grantConsent, denyConsent } = useAnalyticsConsent();

const isVisible = computed(() => isAvailable && consent.value === null);
</script>

<template>
  <Teleport to="body">
    <section
      v-if="isVisible"
      class="consent-banner"
      :aria-label="$t('consent.label')"
    >
      <p class="consent-banner__text">
        {{ $t('consent.text') }}
        <router-link
          :to="{ name: 'Cookies' }"
          class="consent-banner__link"
        >
          {{ $t('consent.policy_link') }}
        </router-link>
      </p>
      <div class="consent-banner__actions">
        <Button
          variant="outline"
          size="sm"
          @click="denyConsent"
        >
          {{ $t('consent.decline') }}
        </Button>
        <Button
          variant="outline"
          size="sm"
          @click="grantConsent"
        >
          {{ $t('consent.accept') }}
        </Button>
      </div>
    </section>
  </Teleport>
</template>

<style scoped>
.consent-banner {
  position: fixed;
  right: 1rem;
  bottom: 1rem;
  left: 1rem;
  z-index: 60;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem 1.25rem;
  max-width: 44rem;
  margin-inline: auto;
  padding: 1rem 1.25rem;
  border: 1px solid var(--color-border-subtle);
  border-left: 3px solid var(--color-accent-coral);
  border-radius: var(--radius-card);
  background: var(--color-card);
  box-shadow: 0 1rem 3rem color-mix(in srgb, var(--color-surface-900) 22%, transparent);
}

.consent-banner__text {
  flex: 1 1 18rem;
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 0.85rem;
  line-height: 1.6;
}

.consent-banner__link {
  color: var(--color-ink);
  text-decoration: underline;
  text-underline-offset: 3px;
}

.consent-banner__link:hover {
  color: var(--color-accent-coral);
}

.consent-banner__actions {
  display: flex;
  gap: 0.5rem;
}
</style>
