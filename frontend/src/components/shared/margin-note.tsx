import { cn } from "@/lib/utils";

// ============================================================
// MARGIN NOTE — handwritten tutor annotation in the margins.
// Caveat typeface, slight rotation, optional doodle arrow
// pointing back toward the heading it comments on.
// ============================================================

export function MarginNote({
  children,
  className,
  arrow = false,
}: {
  children: React.ReactNode;
  className?: string;
  arrow?: boolean;
}) {
  return (
    <span
      className={cn(
        "inline-flex -rotate-2 items-baseline gap-1 font-hand text-lg leading-none text-primary/75",
        className
      )}
    >
      {arrow && (
        <svg
          aria-hidden="true"
          viewBox="0 0 24 24"
          className="inline-block size-4 shrink-0 -translate-y-0.5 text-primary/60"
          fill="none"
        >
          {/* hand-drawn curly arrow */}
          <path
            d="M20 18c-5-1-9-4-11-9m0 0-0.6 4.2M9.4 9l4.3 0.8"
            stroke="currentColor"
            strokeWidth="1.6"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      )}
      {children}
    </span>
  );
}
