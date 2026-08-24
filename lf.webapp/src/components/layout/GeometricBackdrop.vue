<script setup>
defineProps({
  dense: { type: Boolean, default: false },
});
</script>

<template>
  <div
    class="geo-backdrop"
    :class="{ 'geo-backdrop--dense': dense }"
    aria-hidden="true"
  >
    <span class="geo-backdrop__grid" />
    <span class="geo-backdrop__square geo-backdrop__square--lg" />
    <span class="geo-backdrop__square geo-backdrop__square--sm" />
    <span class="geo-backdrop__circle geo-backdrop__circle--lg" />
    <span class="geo-backdrop__circle geo-backdrop__circle--sm" />
    <span class="geo-backdrop__arc" />
  </div>
</template>

<style scoped>
.geo-backdrop {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
  z-index: 0;
}

.geo-backdrop__grid {
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(var(--industrial-grid) 1px, transparent 1px),
    linear-gradient(90deg, var(--industrial-grid) 1px, transparent 1px);
  background-size: 48px 48px;
  mask-image: radial-gradient(ellipse 80% 70% at 70% 20%, #000 20%, transparent 75%);
}

.geo-backdrop__square,
.geo-backdrop__circle,
.geo-backdrop__arc {
  position: absolute;
  border: 1px solid color-mix(in srgb, var(--color-accent-coral) 45%, var(--industrial-line));
}

.geo-backdrop__square--lg {
  width: 14rem;
  height: 14rem;
  top: 12%;
  right: 8%;
  transform: rotate(18deg);
  background: var(--industrial-accent-wash);
}

.geo-backdrop__square--sm {
  width: 4.5rem;
  height: 4.5rem;
  left: 6%;
  bottom: 18%;
  transform: rotate(-12deg);
}

.geo-backdrop__circle--lg {
  width: 22rem;
  height: 22rem;
  right: -8rem;
  bottom: -10rem;
  border-radius: 50%;
  border-color: var(--industrial-line);
}

.geo-backdrop__circle--sm {
  width: 7rem;
  height: 7rem;
  top: 18%;
  left: 18%;
  border-radius: 50%;
  opacity: 0.7;
}

.geo-backdrop__arc {
  width: 16rem;
  height: 16rem;
  left: -4rem;
  top: -5rem;
  border-radius: 50%;
  border-color: transparent;
  border-top-color: color-mix(in srgb, var(--color-accent-coral) 50%, transparent);
  border-right-color: color-mix(in srgb, var(--color-accent-coral) 50%, transparent);
  transform: rotate(-20deg);
}

.geo-backdrop--dense .geo-backdrop__square--lg {
  width: 8rem;
  height: 8rem;
  top: 1.5rem;
  right: 1.5rem;
}

@media (max-width: 767px) {
  .geo-backdrop__square--lg,
  .geo-backdrop__circle--sm {
    opacity: 0.45;
  }
}

@media (prefers-reduced-motion: no-preference) {
  .geo-backdrop__square--lg {
    animation: geo-drift 14s ease-in-out infinite alternate;
  }
}

@keyframes geo-drift {
  from { transform: rotate(18deg) translate(0, 0); }
  to { transform: rotate(22deg) translate(-8px, 10px); }
}
</style>
