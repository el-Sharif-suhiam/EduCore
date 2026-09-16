// ============================================================
// MOTION TOKENS — single source of truth for animation feel.
// ============================================================
// UiUxDesign.md §33: motion must be one coherent system, not
// random durations per component. CSS twins of these live in
// globals.css (--ease-* via @theme → `ease-natural` etc.).
//
// Tiers (UiUxDesign §11):
//   signature  — hero / journey reveals only
//   editorial  — section transitions
//   micro      — hover, buttons, tabs, state changes
// ============================================================

export const duration = {
  instant: 0.12,
  fast: 0.18,
  normal: 0.26,
  slow: 0.42,
  cinematic: 0.72,
} as const;

export const ease = {
  natural: [0.25, 1, 0.5, 1], // ease-out-quart — default
  emerge: [0.16, 1, 0.3, 1], // ease-out-expo — entrances
  spring: [0.34, 1.3, 0.64, 1], // gentle overshoot — micro only
} as const;

export const stagger = {
  tight: 0.05,
  normal: 0.09,
} as const;

/** Standard editorial entrance used across landing sections. */
export const editorialReveal = {
  initial: { opacity: 0, y: 24 },
  whileInView: { opacity: 1, y: 0 },
  viewport: { once: true, margin: "-80px" },
  transition: { duration: duration.slow, ease: ease.natural },
} as const;
