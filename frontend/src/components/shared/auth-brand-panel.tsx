// ============================================================
// AUTH BRAND PANEL — split-screen study-notes companion shown
// beside the login/register forms (hidden below lg). Ruled
// paper, an editorial line, and the static learning trail.
// ============================================================

import { Logo } from "@/components/shared/logo";
import { MarginNote } from "@/components/shared/margin-note";
import { LearningPath } from "@/components/landing/learning-path";

export function AuthBrandPanel({
  quote,
  note,
}: {
  quote: string;
  note: string;
}) {
  return (
    <aside
      aria-hidden="true"
      className="paper-grain ruled-paper relative hidden overflow-hidden border-e bg-card/40 lg:flex lg:flex-col lg:justify-between lg:p-12"
    >
      <Logo />

      <div>
        <p className="max-w-md font-display text-3xl font-medium leading-snug tracking-tight text-balance text-foreground">
          &ldquo;{quote}&rdquo;
        </p>
        <p className="mt-3 ps-1">
          <MarginNote>{note}</MarginNote>
        </p>
      </div>

      <div className="flex items-end justify-between gap-8">
        <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
          Structured paths · Honest progress · Verifiable certificates
        </p>
      </div>

      {/* static trail across the lower half */}
      <div className="pointer-events-none absolute inset-x-0 bottom-0 top-auto h-[46%] text-primary">
        <LearningPath
          animated={false}
          className="h-full w-auto min-w-full translate-x-[8%] opacity-70 [mask-image:linear-gradient(to_right,transparent,black_12%,black_88%,transparent)]"
        />
      </div>
    </aside>
  );
}
