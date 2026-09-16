// Typed access to the anonymous catalog endpoints.
// Shape mirrors backend Common/ViewModels/CourseViewModel.cs
// (ASP.NET Core serializes camelCase).

export type CourseInstructor = {
  instructorId: number;
  instructorName: string;
};

export type CourseSummary = {
  id: number;
  /** Products-table id — this is what the cart API requires (NOT `id`). */
  productId: number;
  title: string;
  summary: string | null;
  basePrice: number;
  createdAt: string;
  thumbnailUrl: string | null;
  coverImageUrl: string | null;
  isDeleted: boolean;
  isPublished: boolean;
  courseInstructors: CourseInstructor[] | null;
};

// ------------------------------------------------------------
// SERVER-side fetcher (React Server Components).
// Node's fetch needs an ABSOLUTE url, so server components call
// the .NET API directly via SERVER_API_BASE_URL instead of the
// browser proxy. Throws on failure — callers must catch.
// ------------------------------------------------------------
export async function getCoursesServer(
  pageNumber = 1,
  pageSize = 6,
  search = ""
): Promise<CourseSummary[]> {
  const base = (
    process.env.SERVER_API_BASE_URL ?? "http://localhost:5087"
  ).replace(/\/$/, "");

  const qs = new URLSearchParams({
    PageNumber: String(pageNumber),
    PageSize: String(pageSize),
  });
  if (search) qs.set("search", search);

  const res = await fetch(`${base}/api/courses?${qs.toString()}`, {
    headers: { Accept: "application/json" },
    // Catalog data changes rarely; avoid caching surprises in dev.
    cache: "no-store",
  });
  if (!res.ok) throw new Error(`Catalog unavailable (${res.status})`);
  return (await res.json()) as CourseSummary[];
}

// ------------------------------------------------------------
// Session cache for anonymous catalog GETs.
// Cuts duplicate bandwidth (re-mounts, StrictMode double-effects,
// palette re-opens) while a short TTL keeps publish-state fresh.
// Concurrent identical calls share ONE pending promise.
// ------------------------------------------------------------
type CacheEntry<T> = { promise: Promise<T>; expires: number };
const sessionCache = new Map<string, CacheEntry<unknown>>();
const DEFAULT_TTL_MS = 60_000;

function cached<T>(key: string, ttlMs: number, loader: () => Promise<T>): Promise<T> {
  const hit = sessionCache.get(key);
  if (hit && hit.expires > Date.now()) return hit.promise as Promise<T>;

  const promise = loader().catch((err) => {
    sessionCache.delete(key); // don't hold failed responses
    throw err;
  });
  sessionCache.set(key, { promise, expires: Date.now() + ttlMs });
  return promise;
}

// ------------------------------------------------------------
// CLIENT-side fetcher — goes through native-fetch wrapper
// (same-origin proxy in dev). See src/lib/api.ts.
// ------------------------------------------------------------
const catalogTtl = DEFAULT_TTL_MS;

export function getCourses(
  pageNumber = 1,
  pageSize = 12,
  search = ""
): Promise<CourseSummary[]> {
  const qs = new URLSearchParams({ PageNumber: String(pageNumber), PageSize: String(pageSize) });
  if (search) qs.set("search", search);
  return cached(`courses:${qs.toString()}`, catalogTtl, async () => {
    const { api } = await import("./api");
    return api.get<CourseSummary[]>(`/api/courses?${qs.toString()}`, false);
  });
}

// ------------------------------------------------------------
// Standalone lesson — mirrors LessonsWithOutCoursesViewModel.cs
// ------------------------------------------------------------
export type LessonSummary = {
  id: number;
  /** Products-table id — this is what the cart API requires (NOT `id`). */
  productId: number;
  title: string;
  summary: string | null;
  basePrice: number;
  createdAt: string;
  thumbnailUrl: string | null;
  isPublished: boolean;
  instructorId: number;
  instructorName: string;
};

// ------------------------------------------------------------
// CLIENT-side fetcher for the public standalone-lessons feed.
// ------------------------------------------------------------
export function getLessons(
  pageNumber = 1,
  pageSize = 9,
  search = ""
): Promise<LessonSummary[]> {
  const qs = new URLSearchParams({ PageNumber: String(pageNumber), PageSize: String(pageSize) });
  if (search) qs.set("search", search);
  return cached(`lessons:${qs.toString()}`, catalogTtl, async () => {
    const { api } = await import("./api");
    return api.get<LessonSummary[]>(`/api/lessons?${qs.toString()}`, false);
  });
}

// ------------------------------------------------------------
// Single course detail — mirrors backend CourseResponse.cs
// ------------------------------------------------------------
export type CourseDetail = {
  id: number;
  /** Products-table id — this is what the cart API requires (NOT `id`). */
  productId: number;
  name: string;
  createdAt: string;
  updatedAt: string;
  basePrice: number;
  createdByUser: string;
  thumbnailUrl: string | null;
  isPublished: boolean;
  summary: string | null;
  coverImageUrl: string | null;
};

// Lesson inside a course — mirrors LessonsByCourseViewModel.cs
export type CourseLesson = {
  id: number;
  title: string;
  summary: string | null;
  basePrice: number;
  createdAt: string;
  thumbnailUrl: string | null;
  isPublished: boolean;
  instructorId: number;
  instructorName: string;
  courseId: number;
};

async function serverJson<T>(path: string): Promise<T> {
  const base = (
    process.env.SERVER_API_BASE_URL ?? "http://localhost:5087"
  ).replace(/\/$/, "");
  const res = await fetch(`${base}${path}`, {
    headers: { Accept: "application/json" },
    cache: "no-store",
  });
  if (!res.ok) throw new Error(`API ${res.status} for ${path}`);
  return (await res.json()) as T;
}

export function getCourseServer(id: number): Promise<CourseDetail> {
  return serverJson(`/api/courses/${id}`);
}

export function getCourseLessonsServer(courseId: number): Promise<CourseLesson[]> {
  return serverJson(`/api/courses/${courseId}/lessons`);
}

// Client variants for interactive pages
export async function getCourse(id: number): Promise<CourseDetail> {
  const { api } = await import("./api");
  return api.get<CourseDetail>(`/api/courses/${id}`, false);
}

export async function getCourseLessons(courseId: number): Promise<CourseLesson[]> {
  const { api } = await import("./api");
  return api.get<CourseLesson[]>(`/api/courses/${courseId}/lessons`, false);
}

// ------------------------------------------------------------
// BUNDLES (public catalog) — BundleViewModel / BundleItemsViewModel
// Cart add uses productId (Products.Id), same as courses.
// ------------------------------------------------------------
export type BundleSummary = {
  id: number;
  productId: number;
  name: string;
  createdAt: string;
  basePrice: number;
  thumbnailUrl: string | null;
  summary: string | null;
};

export type BundleItems = {
  id: number;
  bundleName: string;
  courses: {
    courseId: number;
    name: string;
    summary: string | null;
    thumbnailUrl: string | null;
  }[];
};

/** Published bundles only (public endpoint). */
export function getBundles(): Promise<BundleSummary[]> {
  return cached("bundles", catalogTtl, async () => {
    const { api } = await import("./api");
    return api.get<BundleSummary[]>("/api/bundles", false);
  });
}

export function getBundleItems(id: number): Promise<BundleItems> {
  return cached(`bundle-items:${id}`, catalogTtl, async () => {
    const { api } = await import("./api");
    return api.get<BundleItems>(`/api/bundles/${id}/items`, false);
  });
}

// ------------------------------------------------------------
// Standalone lesson detail — mirrors LessonPublicInfoViewModel.cs
// (public cover sheet; video/body stay enrolled/owner-gated).
// ------------------------------------------------------------
export type LessonPublicInfo = LessonSummary;

export function getLessonInfo(id: number): Promise<LessonSummary> {
  return cached(`lesson-info:${id}`, catalogTtl, async () => {
    const { api } = await import("./api");
    return api.get<LessonSummary>(`/api/lessons/${id}/info`, false);
  });
}

export function getLessonInfoServer(id: number): Promise<LessonSummary> {
  return serverJson(`/api/lessons/${id}/info`);
}

// ------------------------------------------------------------
// SERVER-side bundle fetchers (React Server Components).
// ------------------------------------------------------------
export function getBundleServer(id: number): Promise<BundleSummary & { isPublished: boolean }> {
  return serverJson(`/api/bundles/${id}`);
}

export function getBundleItemsServer(id: number): Promise<BundleItems> {
  return serverJson(`/api/bundles/${id}/items`);
}
