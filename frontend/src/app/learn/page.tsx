"use client";

// ============================================================
// /learn — the personal learning environment (UiUxDesign §13).
// Calm, focused: Continue Learning is always the loudest element;
// everything else is one level quieter.
// ============================================================

import { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  ArrowRight,
  Award,
  BookOpen,
  Box,
  CircleCheck,
  LoaderCircle,
  PlayCircle,
} from "lucide-react";
import { AppHeader } from "@/components/shared/app-header";
import { Container } from "@/components/shared/container";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuth } from "@/lib/auth-context";
import {
  getCourseProgress,
  getMyEnrollments,
  PRODUCT_TYPE,
  type CourseProgress,
  type Enrollment,
} from "@/lib/learning";

export default function LearnPage() {
  const { status, user } = useAuth();
  const router = useRouter();

  const [enrollments, setEnrollments] = useState<Enrollment[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [progressMap, setProgressMap] = useState<Record<number, CourseProgress>>({});
  const requestedProgressRef = useRef<Set<number>>(new Set());
  const [loadingMore, setLoadingMore] = useState(false);
  const [exhausted, setExhausted] = useState(false);
  const enrollmentsPageRef = useRef(1);

  // Auth gate.
  useEffect(() => {
    if (status === "unauthenticated") router.replace("/login?next=/learn");
  }, [status, router]);

  const loadEnrollments = useCallback(async (page: number, append: boolean): Promise<boolean> => {
    try {
      const list = await getMyEnrollments(page, 24);
      setEnrollments((prev) => {
        if (!append || !prev) return list;
        const seen = new Set(prev.map((e) => e.id));
        return [...prev, ...list.filter((e) => !seen.has(e.id))];
      });
      setExhausted(list.length < 24);
      return true;
    } catch {
      if (!append) setError("Your enrollments are unreachable right now. Is the backend running?");
      return false;
    }
  }, []);

  // Load first page once authenticated.
  useEffect(() => {
    if (status !== "authenticated") return;
    let cancelled = false;
    getMyEnrollments(1, 24)
      .then((list) => {
        if (cancelled) return;
        setEnrollments(list);
        setExhausted(list.length < 24);
      })
      .catch(() => {
        if (!cancelled) setError("Your enrollments are unreachable right now. Is the backend running?");
      });
    return () => {
      cancelled = true;
    };
  }, [status]);

  const loadMore = useCallback(async () => {
    if (loadingMore || exhausted) return;
    setLoadingMore(true);
    try {
      const nextPage = enrollmentsPageRef.current + 1;
      const ok = await loadEnrollments(nextPage, true);
      if (ok) enrollmentsPageRef.current = nextPage;
    } finally {
      setLoadingMore(false);
    }
  }, [loadingMore, exhausted, loadEnrollments]);

  // Lazily fetch progress for EVERY course-type enrollment (no cap).
  useEffect(() => {
    if (!enrollments) return;
    const courseIds = enrollments
      .filter((e) => e.productTypeId === PRODUCT_TYPE.Course && e.courseId)
      .map((e) => e.courseId!)
      .filter((id) => !requestedProgressRef.current.has(id));

    for (const courseId of courseIds) {
      requestedProgressRef.current.add(courseId);
      getCourseProgress(courseId)
        .then((p) => setProgressMap((prev) => ({ ...prev, [courseId]: p })))
        .catch(() => undefined); // card simply shows no progress bar
    }
  }, [enrollments]);

  if (status !== "authenticated") {
    return (
      <div className="flex min-h-svh items-center justify-center">
        <LoaderCircle aria-hidden="true" className="size-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  // Continue-learning candidate: most advanced incomplete course.
  const courseEnrollments =
    enrollments?.filter(
      (e) => e.productTypeId === PRODUCT_TYPE.Course && e.courseId
    ) ?? [];
  const continueTarget = (() => {
    let best: { enrollment: Enrollment; progress: CourseProgress } | null = null;
    for (const e of courseEnrollments) {
      const p = e.courseId ? progressMap[e.courseId] : undefined;
      if (!p) continue;
      if (p.progressPercentage >= 100) continue;
      if (!best || p.progressPercentage > best.progress.progressPercentage) {
        best = { enrollment: e, progress: p };
      }
    }
    return best ?? null;
  })();

  const resumeLessonId = continueTarget?.progress.lessons?.find((l) => !l.isComplete)?.lessonId
    ?? continueTarget?.progress.lessons?.at(-1)?.lessonId;

  return (
    <div className="flex min-h-svh flex-col">
      <AppHeader />
      <main className="flex-1 py-10">
        <Container>
          {/* welcome */}
          <header>
            <h1 className="font-display text-3xl font-semibold tracking-tight sm:text-4xl">
              Welcome back{user?.name ? `, ${user.name.split(" ")[0]}` : ""}
            </h1>
            <p className="mt-2 text-muted-foreground">
              Pick up exactly where you left off.
            </p>
          </header>

          {error && (
            <p role="alert" className="mt-8 rounded-xl border border-dashed p-6 text-sm text-muted-foreground">
              {error}
            </p>
          )}

          {/* continue learning */}
          {!error && enrollments !== null && (
            <section className="mt-8" aria-labelledby="continue-heading">
              <h2 id="continue-heading" className="sr-only">Continue learning</h2>
              {continueTarget && resumeLessonId ? (
                <Link
                  href={`/learn/lessons/${resumeLessonId}`}
                  className="group block rounded-xl bg-card p-6 ring-1 ring-foreground/10 shadow-rest transition-[transform,box-shadow] duration-200 ease-natural hover:-translate-y-0.5 hover:shadow-lift"
                >
                  <div className="flex flex-wrap items-end justify-between gap-4">
                    <div>
                      <p className="font-mono text-xs uppercase tracking-widest text-highlight-foreground/90">
                        <span className="inline-flex items-center gap-1.5 rounded-full bg-highlight px-2 py-0.5 font-semibold text-highlight-foreground">
                          Continue
                        </span>
                      </p>
                      <p className="mt-3 font-display text-2xl font-semibold tracking-tight">
                        {continueTarget.enrollment.productName}
                      </p>
                      <p className="mt-1 text-sm text-muted-foreground">
                        {continueTarget.progress.completedLessons} of{" "}
                        {continueTarget.progress.totalLessons} lessons complete ·{" "}
                        {Math.round(continueTarget.progress.progressPercentage)}%
                      </p>
                    </div>
                    <Button size="lg" className="h-11 px-6 text-base">
                      Resume
                      <ArrowRight data-icon="inline-end" className="transition-transform duration-200 group-hover:translate-x-0.5" />
                    </Button>
                  </div>

                  {/* progress line */}
                  <div
                    role="progressbar"
                    aria-valuenow={Math.round(continueTarget.progress.progressPercentage)}
                    aria-valuemin={0}
                    aria-valuemax={100}
                    className="mt-5 h-1.5 w-full overflow-hidden rounded-full bg-secondary"
                  >
                    <div
                      className="h-full rounded-full bg-primary transition-[width] duration-500 ease-natural"
                      style={{ width: `${Math.max(3, continueTarget.progress.progressPercentage)}%` }}
                    />
                  </div>
                </Link>
              ) : enrollments !== null && enrollments.length === 0 && !error ? (
                <div className="rounded-xl border border-dashed p-10 text-center">
                  <p className="font-display text-xl font-semibold">Your journey starts here</p>
                  <p className="mx-auto mt-2 max-w-sm text-sm leading-relaxed text-muted-foreground">
                    You have no active enrollments yet. Find a course and make it yours.
                  </p>
                  <Button size="lg" className="mt-6 h-10 px-6" asChild>
                    <Link href="/courses">
                      Explore courses
                      <ArrowRight data-icon="inline-end" />
                    </Link>
                  </Button>
                </div>
              ) : null}
            </section>
          )}

          {/* enrolled grid */}
          {enrollments === null && !error ? (
            <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3" aria-busy="true">
              {[0, 1, 2].map((i) => (
                <Skeleton key={i} className="h-28 rounded-xl" />
              ))}
            </div>
          ) : enrollments && enrollments.length > 0 ? (
            <section className="mt-10" aria-labelledby="all-heading">
              <h2 id="all-heading" className="font-display text-xl font-semibold tracking-tight">
                Your library
              </h2>
              <ul className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {enrollments.map((e) => (
                  <LibraryCard key={e.id} enrollment={e} progress={
                    e.courseId ? progressMap[e.courseId] : undefined
                  } />
                ))}
              </ul>
              {!exhausted && (
                <div className="mt-8 flex justify-center">
                  <Button
                    variant="outline"
                    size="lg"
                    className="h-10 px-6"
                    onClick={() => void loadMore()}
                    disabled={loadingMore}
                  >
                    {loadingMore && (
                      <LoaderCircle data-icon="inline-start" className="animate-spin" />
                    )}
                    Load more
                  </Button>
                </div>
              )}
            </section>
          ) : null}
        </Container>
        <div className="h-16" />
      </main>
    </div>
  );
}

function TypeIcon({ typeId }: { typeId: number }) {
  if (typeId === PRODUCT_TYPE.Bundle) return <Box aria-hidden="true" className="size-4" />;
  if (typeId === PRODUCT_TYPE.Lesson) return <PlayCircle aria-hidden="true" className="size-4" />;
  return <BookOpen aria-hidden="true" className="size-4" />;
}

function LibraryCard({
  enrollment,
  progress,
}: {
  enrollment: Enrollment;
  progress?: CourseProgress;
}) {
  const href =
    enrollment.productTypeId === PRODUCT_TYPE.Course && enrollment.courseId
      ? // jump into first incomplete lesson when we know it
        `/learn/lessons/${
          progress?.lessons?.find((l) => !l.isComplete)?.lessonId ??
          progress?.lessons?.[0]?.lessonId ?? ""
        }`
      : enrollment.productTypeId === PRODUCT_TYPE.Lesson && enrollment.lessonId
        ? `/learn/lessons/${enrollment.lessonId}`
        : null;

  const pct = progress ? Math.round(progress.progressPercentage) : null;
  const done = pct !== null && pct >= 100;

  const body = (
    <>
      <div className="flex items-center justify-between gap-2">
        <span className="flex size-9 items-center justify-center rounded-lg bg-accent text-accent-foreground">
          <TypeIcon typeId={enrollment.productTypeId} />
        </span>
        {done && (
          <span className="flex items-center gap-1.5">
            {enrollment.courseId && (
              <Button variant="ghost" size="sm" className="h-7 gap-1 px-2 text-xs" asChild>
                <Link href={`/learn/certificate/${enrollment.courseId}`}>
                  <Award data-icon="inline-start" className="size-3.5" />
                  Certificate
                </Link>
              </Button>
            )}
            <Badge className="gap-1">
              <CircleCheck data-icon="inline-start" className="size-3.5" />
              Completed
            </Badge>
          </span>
        )}
      </div>

      <p className="mt-3 font-display text-base leading-snug font-semibold text-balance">
        {enrollment.productName}
      </p>
      {enrollment.summary && (
        <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{enrollment.summary}</p>
      )}

      {pct !== null && !done && (
        <div className="mt-auto pt-4">
          <div
            role="progressbar"
            aria-valuenow={pct}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-label={`${enrollment.productName} progress`}
            className="h-1.5 w-full overflow-hidden rounded-full bg-secondary"
          >
            <div
              className="h-full rounded-full bg-primary transition-[width] duration-500 ease-natural"
              style={{ width: `${Math.max(3, pct)}%` }}
            />
          </div>
          <p className="mt-1.5 font-mono text-xs text-muted-foreground">{pct}%</p>
        </div>
      )}
    </>
  );

  const cls =
    "relative flex h-full flex-col rounded-xl bg-card p-5 ring-1 ring-foreground/10 shadow-rest transition-[transform,box-shadow] duration-200 ease-natural hover:-translate-y-0.5 hover:shadow-lift";

  return (
    <li>
      {href ? (
        <Link href={href} className={`block ${cls}`}>
          {body}
        </Link>
      ) : (
        <div className={cls}>{body}</div>
      )}
    </li>
  );
}
