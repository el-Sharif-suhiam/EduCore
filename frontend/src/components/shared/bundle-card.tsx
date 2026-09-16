"use client";

// ============================================================
// BUNDLE CARD — catalog card for a course bundle. "What's
// inside" opens a lightweight modal listing member courses
// (lazy-fetched from the public items endpoint).
// ============================================================

import { useState } from "react";
import Link from "next/link";
import { Box } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Modal } from "@/components/ui/modal";
import { AddToCartButton } from "@/components/shared/add-to-cart-button";
import {
  getBundleItems,
  type BundleSummary,
  type BundleItems,
} from "@/lib/courses";

export function BundleCard({ bundle }: { bundle: BundleSummary }) {
  const [insideOpen, setInsideOpen] = useState(false);
  const [items, setItems] = useState<BundleItems["courses"] | null>(null);
  const [itemsError, setItemsError] = useState(false);

  async function openInside() {
    setInsideOpen(true);
    if (items === null && !itemsError) {
      try {
        const data = await getBundleItems(bundle.id);
        setItems(data.courses);
      } catch {
        setItemsError(true);
      }
    }
  }

  return (
    <article className="group relative flex flex-col overflow-hidden rounded-xl bg-card ring-1 ring-highlight/40 shadow-rest transition-[transform,box-shadow] duration-200 ease-natural hover:-translate-y-0.5 hover:shadow-lift">
      {/* header band — bundles are visually distinct from courses */}
      <div className="relative flex aspect-[21/9] w-full items-center justify-center overflow-hidden bg-secondary">
        <div
          aria-hidden="true"
          className="absolute inset-0"
          style={{
            background:
              "linear-gradient(135deg, color-mix(in oklch, var(--highlight) 26%, var(--card)), color-mix(in oklch, var(--primary) 14%, var(--card)))",
          }}
        />
        <Box
          aria-hidden="true"
          className="relative size-10 text-primary/70 transition-transform duration-300 ease-natural group-hover:scale-105"
        />
        <span className="absolute start-3 top-3 rounded-full bg-card/90 px-2 py-0.5 font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
          Bundle · save vs. buying separately
        </span>
      </div>

      <div className="flex flex-1 flex-col gap-1.5 p-4">
        <h3 className="font-display text-lg leading-snug font-semibold text-balance">
          <Link
            href={`/bundles/${bundle.id}`}
            className="underline-offset-4 transition-colors hover:text-primary hover:underline"
          >
            {bundle.name}
          </Link>
        </h3>
        {bundle.summary && (
          <p className="line-clamp-2 text-sm leading-relaxed text-muted-foreground">
            {bundle.summary}
          </p>
        )}

        <button
          type="button"
          onClick={() => void openInside()}
          className="mt-1 w-fit text-sm font-medium text-primary underline-offset-4 hover:underline"
        >
          See what&apos;s inside
        </button>

        <div className="mt-auto flex items-center justify-between gap-3 pt-3">
          <p className="shrink-0 font-display text-xl font-semibold">
            ${bundle.basePrice.toFixed(2)}
          </p>
        </div>
        <AddToCartButton productId={bundle.productId} />
      </div>

      <Modal
        open={insideOpen}
        onClose={() => setInsideOpen(false)}
        title={bundle.name}
        description="Everything included in this bundle."
      >
        {itemsError ? (
          <p role="alert" className="text-sm text-muted-foreground">
            Couldn&apos;t load the contents right now.
          </p>
        ) : items === null ? (
          <p className="py-6 text-center text-sm text-muted-foreground">Loading…</p>
        ) : items.length === 0 ? (
          <p className="py-6 text-center text-sm text-muted-foreground">
            No courses attached to this bundle yet.
          </p>
        ) : (
          <ol className="divide-y">
            {items.map((c) => (
              <li key={c.courseId} className="flex items-baseline gap-3 py-3 first:pt-0 last:pb-0">
                <span className="font-display text-base font-semibold">{c.name}</span>
              </li>
            ))}
          </ol>
        )}
        <div className="mt-4 flex justify-end">
          <Button variant="ghost" onClick={() => setInsideOpen(false)}>
            Close
          </Button>
        </div>
      </Modal>
    </article>
  );
}
