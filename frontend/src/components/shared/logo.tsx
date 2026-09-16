import { cn } from "@/lib/utils";

// EduCore mark: three nodes joined by an upward path —
// the "learning journey" abstraction (UiUxDesign §9, §14).
export function Logo({
  className,
  markOnly = false,
}: {
  className?: string;
  markOnly?: boolean;
}) {
  return (
    <span className={cn("inline-flex items-center gap-2.5", className)}>
      <svg
        viewBox="0 0 32 32"
        aria-hidden="true"
        className="size-7 shrink-0"
        fill="none"
      >
        {/* path */}
        <path
          d="M6 24 L14 16 L18 20 L26 8"
          stroke="currentColor"
          strokeWidth="2.4"
          strokeLinecap="round"
          strokeLinejoin="round"
          className="text-primary"
        />
        {/* nodes */}
        <circle cx="6" cy="24" r="3" className="fill-primary" />
        <circle cx="14" cy="16" r="3" className="fill-primary/60" />
        <circle cx="26" cy="8" r="3.4" className="fill-highlight" />
      </svg>
      {!markOnly && (
        <span className="font-display text-xl font-semibold tracking-tight">
          EduCore
        </span>
      )}
      {markOnly && <span className="sr-only">EduCore</span>}
    </span>
  );
}
