import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { Container } from "@/components/shared/container";
import { CourseCard } from "@/components/shared/course-card";
import { Skeleton } from "@/components/ui/skeleton";
import { getCoursesServer, type CourseSummary } from "@/lib/courses";

// Server Component — fetches the REAL anonymous catalog endpoint.
// If the backend is offline it degrades to a friendly empty state
// instead of breaking the landing page.
export async function FeaturedCourses() {
  let courses: CourseSummary[] | null = null;

  try {
    courses = await getCoursesServer(1, 6);
  } catch {
    courses = null; // backend unreachable — handled below
  }

  return (
    <section id="courses" className="scroll-mt-20 py-24 sm:py-28" aria-labelledby="courses-heading">
      <Container>
        <div className="mb-10 flex items-end justify-between gap-6">
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
              The catalog
            </p>
            <h2
              id="courses-heading"
              className="mt-2 font-display text-3xl font-semibold tracking-tight sm:text-4xl"
            >
              Featured courses
            </h2>
          </div>
          <Link
            href="/courses"
            className="hidden items-center gap-1.5 text-sm font-medium text-primary hover:underline underline-offset-4 sm:inline-flex"
          >
            View all
            <ArrowRight className="size-4" />
          </Link>
        </div>

        {courses === null ? (
          <EmptyCatalog />
        ) : (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {courses.map((course) => (
              <CourseCard key={course.id} course={course} />
            ))}
          </div>
        )}
      </Container>
    </section>
  );
}

// Backend offline / no published products yet.
function EmptyCatalog() {
  return (
    <div
      className="rounded-xl border border-dashed p-8 sm:p-12"
      role="status"
      aria-live="polite"
    >
      <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {[0, 1, 2].map((i) => (
          <div key={i} className="space-y-3">
            <Skeleton className="aspect-video w-full rounded-lg" />
            <Skeleton className="h-5 w-3/4" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-1/2" />
          </div>
        ))}
      </div>
      <p className="mt-8 max-w-md text-sm leading-relaxed text-muted-foreground">
        Live courses will appear here. Nothing to show right now — the
        backend API may be offline, or no courses have been published yet.
        Start the API and refresh this page.
      </p>
    </div>
  );
}
