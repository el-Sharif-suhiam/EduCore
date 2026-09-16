import Link from "next/link";
import { notFound } from "next/navigation";
import type { Metadata } from "next";
import { ArrowLeft, GraduationCap, Lock } from "lucide-react";
import { Container } from "@/components/shared/container";
import { AddToCartButton } from "@/components/shared/add-to-cart-button";
import { Badge } from "@/components/ui/badge";
import {
  getCourseServer,
  getCourseLessonsServer,
  type CourseDetail,
  type CourseLesson,
} from "@/lib/courses";

const price = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
});

type Props = { params: Promise<{ id: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  try {
    const course = await getCourseServer(Number(id));
    return { title: course.name };
  } catch {
    return { title: "Course" };
  }
}

export default async function CourseDetailPage({ params }: Props) {
  const { id } = await params;
  const courseId = Number(id);
  if (!Number.isInteger(courseId) || courseId <= 0) notFound();

  let course: CourseDetail;
  try {
    course = await getCourseServer(courseId);
  } catch {
    notFound();
  }

  let lessons: CourseLesson[] | null = null;
  try {
    lessons = await getCourseLessonsServer(courseId);
  } catch {
    lessons = null; // non-fatal: curriculum section degrades
  }

  // Unique instructor names across the curriculum.
  const instructors = [
    ...new Set(
      (lessons ?? [])
        .map((l) => l.instructorName)
        .filter((n): n is string => Boolean(n))
    ),
  ];
  if (instructors.length === 0 && course.createdByUser) {
    instructors.push(course.createdByUser);
  }

  return (
    <div className="pt-24">
      <Container>
        <Link
          href="/courses"
          className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
        >
          <ArrowLeft className="size-4" />
          All courses
        </Link>

        <div className="mt-6 grid gap-10 lg:grid-cols-[1fr_20rem]">
          {/* main column */}
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant="secondary">Course</Badge>
              {course.isPublished ? (
                <Badge>Published</Badge>
              ) : (
                <Badge variant="outline">Unpublished</Badge>
              )}
            </div>

            <h1 className="mt-4 font-display text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
              {course.name}
            </h1>

            {course.summary && (
              <p className="mt-4 max-w-2xl text-lg leading-relaxed text-muted-foreground">
                {course.summary}
              </p>
            )}

            {instructors.length > 0 && (
              <p className="mt-5 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-muted-foreground">
                <GraduationCap aria-hidden="true" className="size-4" />
                Taught by
                {instructors.map((name) => (
                  <span key={name} className="font-medium text-foreground">
                    {name}
                  </span>
                ))}
              </p>
            )}

            {/* cover */}
            <div className="relative mt-8 aspect-video w-full overflow-hidden rounded-xl bg-secondary ring-1 ring-foreground/10">
              {course.coverImageUrl || course.thumbnailUrl ? (
                // eslint-disable-next-line @next/next/no-img-element -- external user-defined URLs
                <img
                  src={(course.coverImageUrl ?? course.thumbnailUrl)!}
                  alt=""
                  className="size-full object-cover"
                />
              ) : (
                <div
                  aria-hidden="true"
                  className="flex size-full items-center justify-center"
                  style={{
                    background:
                      "linear-gradient(135deg, color-mix(in oklch, var(--primary) 16%, var(--card)), color-mix(in oklch, var(--highlight) 22%, var(--card)))",
                  }}
                >
                  <span className="font-display text-7xl font-semibold text-primary/50">
                    {course.name.charAt(0).toUpperCase()}
                  </span>
                </div>
              )}
            </div>

            {/* curriculum */}
            <section className="mt-12" aria-labelledby="curriculum-heading">
              <h2
                id="curriculum-heading"
                className="font-display text-2xl font-semibold tracking-tight"
              >
                Curriculum
              </h2>

              {lessons === null ? (
                <p className="mt-4 rounded-xl border border-dashed p-6 text-sm text-muted-foreground">
                  The curriculum list is unavailable right now.
                </p>
              ) : lessons.length === 0 ? (
                <p className="mt-4 rounded-xl border border-dashed p-6 text-sm text-muted-foreground">
                  No lessons have been added to this course yet.
                </p>
              ) : (
                <ol className="mt-5 divide-y overflow-hidden rounded-xl ring-1 ring-foreground/10 bg-card">
                  {lessons.map((lesson, i) => (
                    <li
                      key={lesson.id}
                      className="flex items-center gap-4 px-5 py-4 transition-colors duration-200 hover:bg-accent/50"
                    >
                      <span className="font-mono text-xs text-muted-foreground">
                        {String(i + 1).padStart(2, "0")}
                      </span>
                      <div className="min-w-0 flex-1">
                        <p className="truncate font-medium">{lesson.title}</p>
                        {lesson.summary && (
                          <p className="mt-0.5 line-clamp-1 text-sm text-muted-foreground">
                            {lesson.summary}
                          </p>
                        )}
                      </div>
                      <Lock
                        aria-label="Enroll to unlock"
                        className="size-4 shrink-0 text-muted-foreground"
                      />
                    </li>
                  ))}
                </ol>
              )}
            </section>
          </div>

          {/* purchase rail */}
          <aside className="lg:sticky lg:top-24 lg:self-start">
            <div className="rounded-xl bg-card p-6 ring-1 ring-foreground/10 shadow-rest">
              <p className="font-display text-3xl font-semibold">
                {price.format(course.basePrice)}
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                One-time payment · lifetime access to this path
              </p>

              {/* ============================================================
                  COMMERCE FLOW: this adds the course's PRODUCT to the cart
                  (POST /api/orders/.../items/{productId}). Checkout +
                  payment gateway live ONLY in src/lib/payments.ts
                  (see its STRIPE CHANGE HERE banners).
                  ============================================================ */}
              <AddToCartButton productId={course.productId} />

              <ul className="mt-5 space-y-2 text-sm text-muted-foreground">
                <li className="flex items-center gap-2">
                  <span className="size-1.5 rounded-full bg-highlight" />
                  {(lessons?.length ?? 0)} lesson{((lessons?.length ?? 0)) === 1 ? "" : "s"}
                </li>
                <li className="flex items-center gap-2">
                  <span className="size-1.5 rounded-full bg-highlight" />
                  Certificate on completion
                </li>
                <li className="flex items-center gap-2">
                  <span className="size-1.5 rounded-full bg-highlight" />
                  Progress saved automatically
                </li>
              </ul>
            </div>
          </aside>
        </div>
      </Container>
      <div className="h-20" />
    </div>
  );
}
