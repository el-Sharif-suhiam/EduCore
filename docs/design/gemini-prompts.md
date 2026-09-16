# EduCore × Gemini — Image Prompt Pack

Ready-to-paste prompts for generating on-brand imagery with Gemini
(gemini.google.com → image generation, or the Gemini API). Every prompt
below is locked to the **Ink & Amber / Study Notes** design language so
results slot straight into the UI.

---

## Global style block

Paste this **before every prompt** — it carries the brand:

> Flat vector editorial illustration in a warm "study notebook" style.
> Palette strictly limited to: petrol-teal ink #2F6B85, warm amber #EDB94F,
> soft slate-navy #22303C, muted red accent #D96A6A on warm paper
> background #FAF7F0. Thin confident ink outlines, generous negative
> space, subtle hand-drawn imperfection, notebook margin line and faint
> ruled lines allowed as background texture. Calm, premium, educational.
> Absolutely NO text, letters, numbers, or words in the image. No neon,
> no gradients-heavy glow, no 3D render, no photorealism, no clutter.

**Aspect ratios:** course covers `16:9` · auth panel `4:5` or square ·
texture tile `1:1`.

---

## 1. Course covers (one consistent series, 16:9)

Generate all six with the same style block for a coherent catalog row.

### Photography Fundamentals
> A vintage film camera drawn as a minimal flat vector icon, centered,
> with three small light rays sketched around the lens and a single amber
> highlight dot. Faint ruled notebook lines behind. Wide composition,
> camera occupying the right third, left third mostly empty paper.
> [style block] — aspect ratio 16:9

### Personal Finance Essentials
> A small potted plant growing from a stack of coins, flat vector,
> thin teal ink outlines, one leaf tinted amber. A pencil rests beside
> the pot. Wide composition, subject right of center, airy negative
> space on the left. [style block] — 16:9

### Conversational Spanish: Beginner
> Two overlapping speech bubbles, one containing a tiny stylized sun,
> the other a tiny bird — conversation made visual. Teal outlines,
> one bubble edge highlighted amber. Wide composition, bubbles right
> of center. [style block] — 16:9

### Music Theory I: Reading & Rhythm
> A short fragment of a musical staff WITHOUT any readable notes — just
> five flowing hand-drawn lines curving like a path, ending in a single
> amber eighth-note glyph shape. No letters or clefs. Wide composition,
> staff entering from the left edge toward the right third.
> [style block] — 16:9

### Graphic Design Basics
> An arrangement of simple geometric shapes — circle, triangle, square —
> slightly overlapping like cut paper on a desk, one shape solid amber,
> others outlined in teal. A ruler and pencil peek from the corner.
> [style block] — 16:9

### Creative Writing Workshop
> An open notebook with blank pages, a fountain pen resting in the fold,
> and two or three small doodle-stars rising from the page suggesting an
> idea taking flight. One star filled amber. [style block] — 16:9

---

## 2. Auth panel illustration (~square or 4:5)

> A winding hand-drawn trail crossing the frame from bottom-left to
> top-right, passing five tiny milestone doodles: a flag, stacked blocks,
> a pencil, a mountain, and a rosette medal (medal filled amber). The
> trail itself is a single confident teal line with a dashed ghost of it
> trailing behind. Notebook margin line on the left, faint ruled lines.
> Lots of empty paper. [style block] — portrait 4:5

*(This mirrors the hero's LearningPath graphic — used large it makes the
auth pages feel like part of the same world.)*

---

## 3. Seamless paper-grain texture tile (1:1) — optional

> A seamless repeating tile of very subtle warm paper texture: extremely
> faint fiber noise on #FAF7F0, almost invisible, no lines, no objects,
> no vignette. Flat even lighting. [style block minus objects/lines] —
> square, seamless/tileable.

*(The site already ships a pure-CSS grain (`paper-grain` class); use this
only if you want richer grain. Must be truly seamless.)*

---

## Integration guide

1. Generate, review, and export as `.jpg` (covers) into:
   `frontend/public/covers/` using these slugs:
   - `photography-fundamentals.jpg`
   - `personal-finance-essentials.jpg`
   - `conversational-spanish.jpg`
   - `music-theory-one.jpg`
   - `graphic-design-basics.jpg`
   - `creative-writing-workshop.jpg`
2. Attach them to the seeded courses (they're served from `/covers/...`
   and picked up by `CourseCard` via `ThumbnailUrl`):

```sql
-- Run against EduCore after placing files in frontend/public/covers/
UPDATE dbo.Products SET ThumbnailUrl = N'/covers/photography-fundamentals.jpg'
WHERE Name = N'Photography Fundamentals';

UPDATE dbo.Products SET ThumbnailUrl = N'/covers/personal-finance-essentials.jpg'
WHERE Name = N'Personal Finance Essentials';

UPDATE dbo.Products SET ThumbnailUrl = N'/covers/conversational-spanish.jpg'
WHERE Name = N'Conversational Spanish: Beginner';

UPDATE dbo.Products SET ThumbnailUrl = N'/covers/music-theory-one.jpg'
WHERE Name = N'Music Theory I: Reading & Rhythm';

UPDATE dbo.Products SET ThumbnailUrl = N'/covers/graphic-design-basics.jpg'
WHERE Name = N'Graphic Design Basics';

UPDATE dbo.Products SET ThumbnailUrl = N'/covers/creative-writing-workshop.jpg'
WHERE Name = N'Creative Writing Workshop';
```

3. For new real courses, upload covers through your admin flow and store
   absolute or root-relative URLs in `Products.ThumbnailUrl`.

## Review checklist before shipping a generated image

- [ ] Zero text/letters in the image (regenerate if any appear)
- [ ] Colors read as teal/amber/paper — not blue/purple/white
- [ ] Style matches the existing covers when shown side-by-side
- [ ] Reads clearly at card size (~400px wide)
