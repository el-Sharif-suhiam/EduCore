"use client";

// ============================================================
// LESSON CARD — catalog card for a standalone lesson.
// Standalone lessons are sold one at a time, so the card leads
// straight into the cart (ProductId drives AddToCartButton) —
// owned lessons already live in /learn.
// ============================================================

import { PlayCircle, User } from "lucide-react";
import Link from "next/link";
import { AddToCartButton } from "@/components/shared/add-to-cart-button";
import type { LessonSummary } from "@/lib/courses";
import { cn } from "@/lib/utils";

export function LessonCard({ lesson }: { lesson: LessonSummary }) {
  return (
    <article
      className={cn(
        "group relative flex flex-col overflow-hidden rounded-xl bg-card ring-1 ring-foreground/10 shadow-rest",
        "transition-[transform,box-shadow] duration-200 ease-natural",
        "hover:-translate-y-0.5 hover:shadow-lift"
      )}
    >
      {/* cover */}
      <div className="relative aspect-video w-full overflow-hidden bg-secondary">
        {lesson.thumbnailUrl ? (
          // eslint-disable-next-line @next/next/no-img-element -- external URLs are user-defined; next/image remotePatterns cannot be known ahead
          <img
            src={lesson.thumbnailUrl}
            alt=""
            loading="lazy"
            className="size-full object-cover transition-transform duration-300 ease-natural group-hover:scale-[1.02]"
          />
        ) : (
          <div
            aria-hidden="true"
            className="flex size-full items-center justify-center"
            style={{
              background:
                "linear-gradient(135deg, color-mix(in oklch, var(--highlight) 20%, var(--card)), color-mix(in oklch, var(--primary) 14%, var(--card)))",
            }}
          >
            <PlayCircle
              className="size-10 text-primary/60 transition-transform duration-300 ease-natural group-hover:scale-110"
            />
          </div>
        )}
        <span className="absolute start-3 top-3 rounded-full bg-card/90 px-2 py-0.5 font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
          Standalone lesson
        </span>
      </div>

      {/* body */}
      <div className="flex flex-1 flex-col gap-1.5 p-4">
        <h3 className="font-display text-lg leading-snug font-semibold text-balance">
          <Link
            href={`/lessons/${lesson.id}`}
            className="transition-colors duration-200 hover:text-primary"
          >
            {lesson.title}
          </Link>
        </h3>
        {lesson.summary && (
          <p className="line-clamp-2 text-sm leading-relaxed text-muted-foreground">
            {lesson.summary}
          </p>
        )}

        <p className="mt-auto flex min-w-0 items-center gap-1.5 pt-3 text-xs text-muted-foreground">
          <User className="size-3.5 shrink-0" />
          <span className="truncate">{lesson.instructorName}</span>
        </p>

        <AddToCartButton productId={lesson.productId} />
      </div>
    </article>
  );
}