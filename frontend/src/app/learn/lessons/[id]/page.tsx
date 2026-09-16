"use client";

// ============================================================
// /learn/lessons/[id] — the LESSON PLAYER (UiUxDesign §16).
// One of the calmest pages in the product: content first,
// completion always visible, prev/next at the bottom, and a
// Focus Mode that removes everything non-essential.
// ============================================================

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft,
  ArrowRight,
  CircleCheck,
  Circle,
  LoaderCircle,
  Maximize2,
  Minimize2,
} from "lucide-react";
import { AppHeader } from "@/components/shared/app-header";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuth } from "@/lib/auth-context";
import {
  getCourseProgress,
  getLesson,
  markLessonComplete,
  markLessonIncomplete,
  type CourseProgress,
  type LessonDetail,
} from "@/lib/learning";
import { ApiError } from "@/lib/api";
import { cn } from "@/lib/utils";

export default function LessonPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { status } = useAuth();

  const lessonId = Number(id);

  const [lesson, setLesson] = useState<LessonDetail | null>(null);
  const [progress, setProgress] = useState<CourseProgress | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [forbidden, setForbidden] = useState(false);
  const [focusMode, setFocusMode] = useState(false);
  const [savingProgress, setSavingProgress] = useState(false);
  const loadedForRef = useRef<number | null>(null);

  // Auth gate.
  useEffect(() => {
    if (status === "unauthenticated") router.replace(`/login?next=/learn/lessons/${id}`);
  }, [status, router, id]);

  // Load lesson (+ course progress when part of a course).
  // Re-runs whenever the route's lesson id changes (prev/next uses
  // router.push, so the component stays mounted).
  useEffect(() => {
    if (status !== "authenticated" || !Number.isInteger(lessonId) || lessonId <= 0) return;
    if (loadedForRef.current === lessonId) return;
    loadedForRef.current = lessonId;

    void (async () => {
      await Promise.resolve();
      setLesson(null);
      setProgress(null);
      setForbidden(false);
      setError(null);
      try {
        const l = await getLesson(lessonId);
        setLesson(l);

        if (l.courseId) {
          getCourseProgress(l.courseId)
            .then(setProgress)
            .catch(() => undefined); // rail degrades gracefully
        }
      } catch (err) {
        if (err instanceof ApiError && (err.status === 403 || err.status === 404)) {
          setForbidden(true);
        } else {
          setError(
            err instanceof ApiError
              ? err.message
              : "Could not reach the server. Is the backend running?"
          );
        }
      }
    })();
  }, [status, lessonId]);

  // Focus Mode: Esc exits.
  useEffect(() => {
    if (!focusMode) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setFocusMode(false);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [focusMode]);

  const lessons = useMemo(() => progress?.lessons ?? [], [progress]);

  const currentIndex = useMemo(() => {
    const idx = lessons.findIndex((l) => l.lessonId === lessonId);
    return idx >= 0 ? idx : -1;
  }, [lessons, lessonId]);

  const prev = currentIndex > 0 ? lessons[currentIndex - 1] : undefined;
  const next = currentIndex >= 0 && currentIndex < lessons.length - 1 ? lessons[currentIndex + 1] : undefined;

  const currentEntry = currentIndex >= 0 ? lessons[currentIndex] : undefined;
  const isComplete = currentEntry?.isComplete ?? false;

  const navigate = useCallback(
    (target?: { lessonId: number }) => {
      if (!target) return;
      router.push(`/learn/lessons/${target.lessonId}`);
    },
    [router]
  );

  const onToggleComplete = useCallback(async () => {
    if (!lesson || savingProgress) return;
    setSavingProgress(true);
    const wasComplete = isComplete;
    // Optimistic flip.
    setProgress((p) =>
      p && p.lessons
        ? {
            ...p,
            completedLessons: p.completedLessons + (wasComplete ? -1 : 1),
            lessons: p.lessons.map((l) =>
              l.lessonId === lesson.id
                ? { ...l, isComplete: !wasComplete, completedDate: wasComplete ? null : new Date().toISOString() }
                : l
            ),
          }
        : p
    );
    try {
      if (wasComplete) await markLessonIncomplete(lesson.id);
      else await markLessonComplete(lesson.id);
    } catch {
      // Roll back on failure.
      setProgress((p) =>
        p && p.lessons
          ? {
              ...p,
              completedLessons: p.completedLessons + (wasComplete ? 1 : -1),
              lessons: p.lessons.map((l) =>
                l.lessonId === lesson.id ? { ...l, isComplete: wasComplete } : l
              ),
            }
          : p
      );
    } finally {
      setSavingProgress(false);
    }
  }, [lesson, isComplete, savingProgress]);

  if (status !== "authenticated") {
    return (
      <div className="flex min-h-svh items-center justify-center">
        <LoaderCircle aria-hidden="true" className="size-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (forbidden) {
    return (
      <div className="flex min-h-svh flex-col">
        <AppHeader />
        <main className="flex flex-1 items-center justify-center px-5 py-20">
          <div className="max-w-md rounded-xl border border-dashed p-10 text-center">
            <p className="font-display text-xl font-semibold">This lesson is locked</p>
            <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
              Enrollment gives access. Find it in the catalog or check your library.
            </p>
            <div className="mt-6 flex justify-center gap-2">
              <Button variant="outline" asChild>
                <Link href="/learn">My library</Link>
              </Button>
              <Button asChild>
                <Link href="/courses">Browse courses</Link>
              </Button>
            </div>
          </div>
        </main>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex min-h-svh flex-col">
        <AppHeader />
        <main className="flex flex-1 items-start justify-center px-5 pt-24">
          <p role="alert" className="rounded-xl border border-dashed p-6 text-sm text-muted-foreground">
            {error}
          </p>
        </main>
      </div>
    );
  }

  const pct = progress ? Math.round(progress.progressPercentage) : null;

  return (
    <div className="flex min-h-svh flex-col bg-background">
      {!focusMode && <AppHeader />}

      <main className="mx-auto w-full max-w-5xl flex-1 px-5 py-6 sm:px-8">
        {/* top row */}
        <div className="flex items-center justify-between gap-3">
          <div className="flex min-w-0 items-center gap-2">
            <Button variant="ghost" size="icon-sm" aria-label="Back to My Learning" asChild>
              <Link href="/learn">
                <ArrowLeft />
              </Link>
            </Button>
            {lesson?.courseId && (
              <span className="truncate font-mono text-xs uppercase tracking-widest text-muted-foreground">
                Lesson {currentIndex >= 0 ? currentIndex + 1 : "·"}
                {progress ? ` of ${progress.totalLessons}` : ""}
              </span>
            )}
          </div>

          <Button
            variant="ghost"
            size="sm"
            onClick={() => setFocusMode((v) => !v)}
            aria-pressed={focusMode}
            title={focusMode ? "Exit Focus Mode (Esc)" : "Focus Mode"}
          >
            {focusMode ? (
              <Minimize2 data-icon="inline-start" />
            ) : (
              <Maximize2 data-icon="inline-start" />
            )}
            {focusMode ? "Exit focus" : "Focus"}
          </Button>
        </div>

        {/* thin progress line */}
        {pct !== null && (
          <div
            role="progressbar"
            aria-valuenow={pct}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-label="Course progress"
            className="mt-4 h-1 w-full overflow-hidden rounded-full bg-secondary"
          >
            <div
              className="h-full rounded-full bg-primary transition-[width] duration-500 ease-natural"
              style={{ width: `${Math.max(2, pct)}%` }}
            />
          </div>
        )}

        {lesson === null ? (
          <div className="mt-8 space-y-4" aria-busy="true">
            <Skeleton className="h-9 w-3/4" />
            <Skeleton className="aspect-video w-full rounded-xl" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-2/3" />
          </div>
        ) : (
          <div className={cn("mt-6 grid gap-8", !focusMode && "lg:grid-cols-[1fr_17rem]")}>
            {/* ---------------- content ---------------- */}
            <article>
              <h1 className="font-display text-2xl font-semibold tracking-tight sm:text-3xl">
                {lesson.title || lesson.name}
              </h1>

              {lesson.videoUrl && (
                <div className="mt-5 overflow-hidden rounded-xl ring-1 ring-foreground/10 bg-black">
                  <VideoEmbed url={lesson.videoUrl} title={lesson.title || lesson.name} />
                </div>
              )}

              {lesson.bodyText && (
                <div className="mt-6 whitespace-pre-wrap text-[15px] leading-relaxed text-foreground/90">
                  {lesson.bodyText}
                </div>
              )}

              {/* completion */}
              <div className="mt-8 border-t pt-6">
                <Button
                  size="lg"
                  variant={isComplete ? "secondary" : "default"}
                  className="h-11 px-6 text-base"
                  onClick={() => void onToggleComplete()}
                  disabled={savingProgress}
                  aria-pressed={isComplete}
                >
                  {savingProgress ? (
                    <LoaderCircle data-icon="inline-start" className="animate-spin" />
                  ) : isComplete ? (
                    <CircleCheck data-icon="inline-start" className="text-success" />
                  ) : (
                    <Circle data-icon="inline-start" />
                  )}
                  {isComplete ? "Completed — undo?" : "Mark as complete"}
                </Button>
              </div>

              {/* prev / next */}
              <nav
                aria-label="Lesson navigation"
                className="mt-8 flex items-stretch justify-between gap-3 border-t pt-6"
              >
                {prev ? (
                  <Button variant="outline" className="max-w-[48%]" onClick={() => navigate(prev)}>
                    <ArrowLeft data-icon="inline-start" />
                    <span className="truncate">{prev.title}</span>
                  </Button>
                ) : (
                  <span />
                )}
                {next ? (
                  <Button className="max-w-[48%]" onClick={() => navigate(next)}>
                    <span className="truncate">{next.title}</span>
                    <ArrowRight data-icon="inline-end" />
                  </Button>
                ) : (
                  <span />
                )}
              </nav>
            </article>

            {/* ---------------- curriculum rail ---------------- */}
            {!focusMode && lessons.length > 0 && (
              <aside className="lg:sticky lg:top-6 lg:self-start">
                <h2 className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
                  Curriculum
                </h2>
                <ol className="mt-3 space-y-1">
                  {lessons.map((l, i) => {
                    const active = l.lessonId === lessonId;
                    return (
                      <li key={l.lessonId}>
                        <button
                          type="button"
                          onClick={() => navigate(l)}
                          aria-current={active ? "page" : undefined}
                          className={cn(
                            "flex w-full items-center gap-2.5 rounded-md px-3 py-2 text-start text-sm transition-colors duration-200",
                            active
                              ? "bg-accent font-medium text-accent-foreground"
                              : "text-muted-foreground hover:bg-accent/60 hover:text-foreground"
                          )}
                        >
                          {l.isComplete ? (
                            <CircleCheck aria-hidden="true" className="size-4 shrink-0 text-success" />
                          ) : (
                            <Circle aria-hidden="true" className="size-4 shrink-0 opacity-50" />
                          )}
                          <span className="truncate">
                            <span className="me-1.5 font-mono text-xs opacity-70">
                              {String(i + 1).padStart(2, "0")}
                            </span>
                            {l.title}
                          </span>
                        </button>
                      </li>
                    );
                  })}
                </ol>
              </aside>
            )}
          </div>
        )}
      </main>
    </div>
  );
}

// ------------------------------------------------------------
// Minimal embed handling: YouTube/Vimeo → iframe, else native video.
// (No external SDKs — minimum tech.)
// ------------------------------------------------------------
function VideoEmbed({ url, title }: { url: string; title: string }) {
  const youtube = url.match(
    /(?:youtube\.com\/(?:watch\?v=|embed\/)|youtu\.be\/)([\w-]{11})/
  );
  if (youtube) {
    return (
      <iframe
        src={`https://www.youtube-nocookie.com/embed/${youtube[1]}`}
        title={title}
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; picture-in-picture"
        allowFullScreen
        loading="lazy"
        className="aspect-video w-full"
      />
    );
  }

  const vimeo = url.match(/vimeo\.com\/(\d+)/);
  if (vimeo) {
    return (
      <iframe
        src={`https://player.vimeo.com/video/${vimeo[1]}`}
        title={title}
        allow="autoplay; fullscreen; picture-in-picture"
        allowFullScreen
        loading="lazy"
        className="aspect-video w-full"
      />
    );
  }

  return <video src={url} controls preload="metadata" className="aspect-video w-full" />;
}
