"use client";

// Catalog — search + incremental "Load more" pagination.
// NOTE (backend gap): list endpoints return no total count, so we
// deliberately use load-more instead of "page X of Y" UI.
//
// React 19 pattern: the list is a keyed child (<CatalogList
// key={search}>) so a new search REMOUNTS it with fresh state
// instead of resetting state synchronously inside an effect.

import { useCallback, useEffect, useRef, useState } from "react";
import { LoaderCircle, Search } from "lucide-react";
import { AppHeader } from "@/components/shared/app-header";
import { Container } from "@/components/shared/container";
import { CourseCard } from "@/components/shared/course-card";
import { BundleCard } from "@/components/shared/bundle-card";
import { LessonCard } from "@/components/shared/lesson-card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  getCourses,
  getBundles,
  getLessons,
  type CourseSummary,
  type BundleSummary,
  type LessonSummary,
} from "@/lib/courses";

const PAGE_SIZE = 9;

export default function CoursesPage() {
  const [rawSearch, setRawSearch] = useState("");
  const [search, setSearch] = useState("");

  // Debounce keystrokes into the committed `search`.
  useEffect(() => {
    const t = setTimeout(() => setSearch(rawSearch.trim()), 300);
    return () => clearTimeout(t);
  }, [rawSearch]);

  return (
    <div className="flex min-h-svh flex-col">
      <AppHeader />
      <main className="flex-1 py-10">
        <Container>
          <header className="max-w-2xl">
            <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
              The catalog
            </p>
            <h1 className="mt-2 font-display text-4xl font-semibold tracking-tight sm:text-5xl">
              Courses
            </h1>
            <p className="mt-3 text-lg leading-relaxed text-muted-foreground">
              Every course connects into a path. Find yours.
            </p>
          </header>

          {/* search */}
          <div className="relative mt-8 max-w-md">
            <Search
              aria-hidden="true"
              className="pointer-events-none absolute start-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
            />
            <Input
              type="search"
              value={rawSearch}
              onChange={(e) => setRawSearch(e.target.value)}
              placeholder="Search courses or instructors…"
              aria-label="Search courses"
              className="h-11 ps-9"
            />
          </div>

          <CatalogList key={search} search={search} />
          <BundlesSection />
          <LessonsSection />
        </Container>
        <div className="h-20" />
      </main>
    </div>
  );
}

function CatalogList({ search }: { search: string }) {
  const [courses, setCourses] = useState<CourseSummary[] | null>(null); // null = loading
  const [error, setError] = useState<string | null>(null);
  const [exhausted, setExhausted] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const pageRef = useRef(1);

  const requestId = useRef(0);

  const loadFirstPage = useCallback(async () => {
    const id = ++requestId.current;
    try {
      const first = await getCourses(1, PAGE_SIZE, search);
      if (requestId.current !== id) return; // stale response guard
      setCourses(first);
      if (first.length < PAGE_SIZE) setExhausted(true);
    } catch {
      if (requestId.current === id) {
        setError("The catalog is unreachable right now. Is the backend running?");
      }
    }
  }, [search]);

  // Async load on mount / remount (key change). All setState calls
  // happen asynchronously — never synchronously in the effect body.
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- fetch-on-mount; every setState runs after the awaited response
    void loadFirstPage();
  }, [loadFirstPage]);

  const loadMore = useCallback(async () => {
    if (loadingMore || exhausted || courses === null) return;
    setLoadingMore(true);
    try {
      const next = await getCourses(pageRef.current + 1, PAGE_SIZE, search);
      setCourses((prev) => {
        if (!prev) return prev;
        const seen = new Set(prev.map((c) => c.id));
        return [...prev, ...next.filter((c) => !seen.has(c.id))];
      });
      pageRef.current += 1;
      if (next.length < PAGE_SIZE) setExhausted(true);
    } catch {
      setExhausted(true); // stop retrying on failure
    } finally {
      setLoadingMore(false);
    }
  }, [courses, exhausted, loadingMore, search]);

  if (error) {
    return (
      <p
        role="alert"
        className="mt-10 rounded-xl border border-dashed p-8 text-sm leading-relaxed text-muted-foreground"
      >
        {error}
      </p>
    );
  }

  if (courses === null) {
    return (
      <div className="mt-10 grid gap-5 sm:grid-cols-2 lg:grid-cols-3" aria-busy="true">
        {Array.from({ length: 6 }, (_, i) => (
          <div key={i} className="space-y-3">
            <Skeleton className="aspect-video w-full rounded-lg" />
            <Skeleton className="h-5 w-3/4" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-1/2" />
          </div>
        ))}
      </div>
    );
  }

  if (courses.length === 0) {
    return (
      <div className="mt-10 rounded-xl border border-dashed p-10 text-center">
        <p className="font-display text-xl font-semibold">No courses found</p>
        <p className="mx-auto mt-2 max-w-sm text-sm leading-relaxed text-muted-foreground">
          {search
            ? `Nothing matches “${search}”. Try a different term.`
            : "No published courses yet. Check back soon."}
        </p>
      </div>
    );
  }

  return (
    <>
      <div className="mt-10 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {courses.map((course) => (
          <CourseCard key={course.id} course={course} />
        ))}
      </div>

      {!exhausted && (
        <div className="mt-10 flex justify-center">
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
    </>
  );
}

// ------------------------------------------------------------
// BUNDLES — published bundles beneath the course grid.
// Loads once (not per-search): bundles are few and the public
// endpoint has no search parameter.
// ------------------------------------------------------------
function BundlesSection() {
  const [bundles, setBundles] = useState<BundleSummary[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    getBundles()
      .then((data) => {
        if (!cancelled && data.length > 0) setBundles(data);
      })
      .catch(() => undefined); // bundles are optional chrome — stay silent
    return () => {
      cancelled = true;
    };
  }, []);

  if (bundles === null || bundles.length === 0) return null;

  return (
    <section className="mt-16" aria-labelledby="bundles-heading">
      <h2 id="bundles-heading" className="font-display text-2xl font-semibold tracking-tight">
        Bundles
      </h2>
      <p className="mt-1 text-sm text-muted-foreground">
        Grouped learning paths at a better price than buying separately.
      </p>
      <div className="mt-5 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {bundles.map((b) => (
          <BundleCard key={b.id} bundle={b} />
        ))}
      </div>
    </section>
  );
}

// ------------------------------------------------------------
// STANDALONE LESSONS — sold one at a time, so each card carries
// its own Add to cart (ProductId drives the cart API). Hidden
// entirely when the feed is empty or unreachable.
// ------------------------------------------------------------
const LESSON_PAGE_SIZE = 9;

function LessonsSection() {
  const [lessons, setLessons] = useState<LessonSummary[] | null>(null);
  const [exhausted, setExhausted] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const pageRef = useRef(1);
  const requestId = useRef(0);

  useEffect(() => {
    let cancelled = false;
    const id = ++requestId.current;
    getLessons(1, LESSON_PAGE_SIZE)
      .then((data) => {
        if (cancelled || requestId.current !== id) return;
        setLessons(data);
        if (data.length < LESSON_PAGE_SIZE) setExhausted(true);
      })
      .catch(() => undefined); // lessons are optional chrome — stay silent
    return () => {
      cancelled = true;
    };
  }, []);

  const loadMore = useCallback(async () => {
    if (loadingMore || exhausted || lessons === null) return;
    setLoadingMore(true);
    try {
      const next = await getLessons(pageRef.current + 1, LESSON_PAGE_SIZE);
      setLessons((prev) => {
        if (!prev) return prev;
        const seen = new Set(prev.map((l) => l.id));
        return [...prev, ...next.filter((l) => !seen.has(l.id))];
      });
      pageRef.current += 1;
      if (next.length < LESSON_PAGE_SIZE) setExhausted(true);
    } catch {
      setExhausted(true); // stop retrying on failure
    } finally {
      setLoadingMore(false);
    }
  }, [exhausted, lessons, loadingMore]);

  if (lessons === null || lessons.length === 0) return null;

  return (
    <section className="mt-16" aria-labelledby="lessons-heading">
      <h2 id="lessons-heading" className="font-display text-2xl font-semibold tracking-tight">
        Standalone lessons
      </h2>
      <p className="mt-1 text-sm text-muted-foreground">
        One focused skill, yours forever — no need to buy the whole course.
      </p>
      <div className="mt-5 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {lessons.map((l) => (
          <LessonCard key={l.id} lesson={l} />
        ))}
      </div>

      {!exhausted && (
        <div className="mt-8 flex justify-center">
          <Button
            variant="outline"
            size="lg"
            className="h-10 px-6"
            onClick={() => void loadMore()}
            disabled={loadingMore}
          >
            {loadingMore && <LoaderCircle data-icon="inline-start" className="animate-spin" />}
            Load more
          </Button>
        </div>
      )}
    </section>
  );
}
