"use client";

// ============================================================
// /learn/certificate/[courseId] — the earned certificate.
//
// Honest by design: the backend's certificate endpoint marks
// issuance (audit trail) but returns no file yet (gap M14), so
// the document is rendered HERE from real progress data and
// saved via the browser's print-to-PDF. No fake downloads.
//
// UiUxDesign §28: a small polished reveal — not a celebration.
// ============================================================

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, LoaderCircle, Printer } from "lucide-react";
import { AppHeader } from "@/components/shared/app-header";
import { Container } from "@/components/shared/container";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth-context";
import {
  getCourseProgress,
  issueCertificate,
} from "@/lib/learning";
import { getCourse } from "@/lib/courses";

export default function CertificatePage() {
  const params = useParams<{ courseId: string }>();
  const router = useRouter();
  const { status, user } = useAuth();

  const courseId = Number(params.courseId);

  const [state, setState] = useState<
    | { kind: "loading" }
    | { kind: "not-complete" }
    | { kind: "ready"; courseName: string }
    | { kind: "error" }
  >({ kind: "loading" });

  useEffect(() => {
    if (status !== "authenticated") return;
    if (!Number.isInteger(courseId) || courseId <= 0) {
      router.replace("/learn");
      return;
    }

    let cancelled = false;
    (async () => {
      try {
        const [progress, course] = await Promise.all([
          getCourseProgress(courseId),
          getCourse(courseId),
        ]);
        if (cancelled) return;

        if (progress.progressPercentage < 100) {
          setState({ kind: "not-complete" });
          return;
        }

        // Mark issuance (audit trail). Fire-and-forget — viewing
        // must never depend on it succeeding.
        void issueCertificate(courseId).catch(() => undefined);

        setState({ kind: "ready", courseName: course.name });
      } catch {
        if (!cancelled) setState({ kind: "error" });
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [status, courseId, router]);

  if (status !== "authenticated") {
    return (
      <div className="flex min-h-svh flex-col">
        <AppHeader />
        <div className="flex flex-1 items-center justify-center">
          <LoaderCircle aria-hidden="true" className="size-6 animate-spin text-muted-foreground" />
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-svh flex-col bg-secondary/40">
      {/* print-only reset */}
      <style>{`
        @media print {
          .no-print { display: none !important; }
          .print-sheet {
            box-shadow: none !important;
            ring-width: 0 !important;
            margin: 0 !important;
            border-radius: 0 !important;
          }
          body { background: white !important; }
        }
      `}</style>

      <AppHeader />

      <main className="flex-1 py-10">
        <Container>
          <div className="mx-auto max-w-3xl">
            <div className="no-print flex items-center justify-between gap-3">
              <Link
                href="/learn"
                className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
              >
                <ArrowLeft className="size-4" />
                My learning
              </Link>
              {state.kind === "ready" && (
                <Button onClick={() => window.print()}>
                  <Printer data-icon="inline-start" />
                  Print / Save PDF
                </Button>
              )}
            </div>

            {state.kind === "loading" && (
              <p className="mt-10 text-center text-sm text-muted-foreground">Preparing…</p>
            )}

            {state.kind === "error" && (
              <p
                role="alert"
                className="no-print mt-10 rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground"
              >
                The certificate is unreachable right now. Is the backend running?
              </p>
            )}

            {state.kind === "not-complete" && (
              <div className="no-print mt-10 rounded-xl border border-dashed p-10 text-center">
                <p className="font-display text-xl font-semibold">Not quite yet</p>
                <p className="mx-auto mt-2 max-w-sm text-sm leading-relaxed text-muted-foreground">
                  Certificates unlock when every lesson in the course is
                  complete. You&apos;re close — keep going.
                </p>
                <Button className="mt-5" asChild>
                  <Link href="/learn">Back to my learning</Link>
                </Button>
              </div>
            )}

            {state.kind === "ready" && (
              <article
                className={cnPrintSheet()}
                aria-label={`Certificate of completion for ${state.courseName}`}
              >
                <div className="ruled-paper relative overflow-hidden p-8 sm:p-14">
                  {/* double frame */}
                  <div className="pointer-events-none absolute inset-3 rounded-lg border border-primary/25" />
                  <div className="pointer-events-none absolute inset-4 rounded-md border border-highlight/50" />

                  <div className="relative flex flex-col items-center text-center">
                    <svg viewBox="0 0 32 32" aria-hidden="true" className="size-12" fill="none">
                      <path
                        d="M6 24 L14 16 L18 20 L26 8"
                        stroke="currentColor"
                        strokeWidth="2.4"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        className="text-primary"
                      />
                      <circle cx="6" cy="24" r="3" className="fill-primary" />
                      <circle cx="14" cy="16" r="3" className="fill-primary/60" />
                      <circle cx="26" cy="8" r="3.4" className="fill-highlight" />
                    </svg>

                    <p className="mt-6 font-mono text-xs uppercase tracking-[0.3em] text-muted-foreground">
                      EduCore · Certificate of completion
                    </p>

                    <p className="mt-8 font-display text-xl text-muted-foreground">
                      This certifies that
                    </p>
                    <p className="mt-2 font-display text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
                      {user?.name ?? user?.email}
                    </p>

                    <p className="mt-6 font-display text-lg text-muted-foreground">
                      has completed every lesson of
                    </p>
                    <p className="relative mt-2 inline-block font-display text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                      <span
                        aria-hidden="true"
                        className="absolute inset-x-[-0.15em] bottom-1 -z-10 h-[0.45em] -rotate-1 rounded-sm bg-highlight/60"
                      />
                      {state.courseName}
                    </p>

                    <div className="mt-12 flex w-full max-w-md items-end justify-between gap-6 text-xs text-muted-foreground">
                      <div className="text-start">
                        <p className="border-t pt-2 font-mono uppercase tracking-widest">
                          Issued {new Date().toLocaleDateString(undefined, { year: "numeric", month: "long", day: "numeric" })}
                        </p>
                      </div>
                      <svg viewBox="0 0 120 120" aria-hidden="true" className="size-20 -rotate-6 opacity-90" fill="none">
                        <defs>
                          <path id="cert-ring" d="M 60 13 a 47 47 0 1 1 -0.01 0" />
                        </defs>
                        <circle cx="60" cy="60" r="56" stroke="currentColor" strokeWidth="2.4" className="text-primary/70" />
                        <circle cx="60" cy="60" r="40" stroke="currentColor" strokeWidth="1.2" className="text-primary/50" />
                        <text fontSize="10.2" letterSpacing="2.6" className="fill-current font-mono uppercase text-primary/75">
                          <textPath href="#cert-ring" startOffset="0">
                            EduCore · Verified Completion ·
                          </textPath>
                        </text>
                        <path d="M46 61 l10 10 l19 -22" stroke="currentColor" strokeWidth="4" strokeLinecap="round" strokeLinejoin="round" className="text-primary/80" />
                      </svg>
                    </div>
                  </div>
                </div>
              </article>
            )}
          </div>
        </Container>
        <div className="h-16 no-print" />
      </main>
    </div>
  );
}

function cnPrintSheet(): string {
  return "print-sheet mt-6 rounded-xl bg-card ring-1 ring-foreground/10 shadow-pop";
}
