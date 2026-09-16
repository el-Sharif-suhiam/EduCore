import Link from "next/link";
import { notFound } from "next/navigation";
import type { Metadata } from "next";
import { ArrowLeft, CalendarDays, LockKeyhole, PlayCircle, User } from "lucide-react";
import { Container } from "@/components/shared/container";
import { AddToCartButton } from "@/components/shared/add-to-cart-button";
import { Badge } from "@/components/ui/badge";
import { getLessonInfoServer, type LessonSummary } from "@/lib/courses";

const price = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
});

type Props = { params: Promise<{ id: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  try {
    const lesson = await getLessonInfoServer(Number(id));
    return { title: lesson.title };
  } catch {
    return { title: "Standalone lesson" };
  }
}

export default async function LessonDetailPage({ params }: Props) {
  const { id } = await params;
  const lessonId = Number(id);
  if (!Number.isInteger(lessonId) || lessonId <= 0) notFound();

  let lesson: LessonSummary;
  try {
    lesson = await getLessonInfoServer(lessonId);
  } catch {
    notFound();
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
              <Badge variant="secondary">Standalone lesson</Badge>
            </div>

            <h1 className="mt-4 font-display text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
              {lesson.title}
            </h1>

            <div className="mt-4 flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-muted-foreground">
              <span className="inline-flex items-center gap-1.5">
                <User className="size-4" />
                {lesson.instructorName}
              </span>
              <span className="inline-flex items-center gap-1.5">
                <CalendarDays className="size-4" />
                Added {new Date(lesson.createdAt).toLocaleDateString()}
              </span>
            </div>

            {lesson.summary && (
              <p className="mt-4 max-w-2xl text-lg leading-relaxed text-muted-foreground">
                {lesson.summary}
              </p>
            )}

            {/* cover */}
            <div className="relative mt-8 aspect-video w-full overflow-hidden rounded-xl bg-secondary ring-1 ring-foreground/10">
              {lesson.thumbnailUrl ? (
                // eslint-disable-next-line @next/next/no-img-element -- external user-defined URLs
                <img src={lesson.thumbnailUrl} alt="" className="size-full object-cover" />
              ) : (
                <div
                  aria-hidden="true"
                  className="flex size-full items-center justify-center"
                  style={{
                    background:
                      "linear-gradient(135deg, color-mix(in oklch, var(--highlight) 22%, var(--card)), color-mix(in oklch, var(--primary) 14%, var(--card)))",
                  }}
                >
                  <PlayCircle className="size-14 text-primary/50" />
                </div>
              )}
            </div>

            {/* content note */}
            <section
              className="mt-12 flex items-start gap-3 rounded-xl border border-dashed p-5 text-sm leading-relaxed text-muted-foreground"
              aria-labelledby="locked-heading"
            >
              <LockKeyhole className="mt-0.5 size-5 shrink-0 text-muted-foreground" />
              <div>
                <h2 id="locked-heading" className="font-medium text-foreground">
                  Content unlocks after purchase
                </h2>
                <p className="mt-1">
                  The lesson video and notes appear in your learning space as soon as payment
                  completes — no extra step needed.
                </p>
              </div>
            </section>
          </div>

          {/* purchase rail */}
          <aside className="lg:sticky lg:top-24 lg:self-start">
            <div className="rounded-xl bg-card p-6 ring-1 ring-foreground/10 shadow-rest">
              <p className="font-display text-3xl font-semibold">{price.format(lesson.basePrice)}</p>
              <p className="mt-1 text-xs text-muted-foreground">
                One lesson, one payment — yours forever
              </p>

              <AddToCartButton productId={lesson.productId} />

              <ul className="mt-5 space-y-2 text-sm text-muted-foreground">
                <li className="flex items-center gap-2">
                  <span className="size-1.5 rounded-full bg-highlight" />
                  Lifetime access after purchase
                </li>
                <li className="flex items-center gap-2">
                  <span className="size-1.5 rounded-full bg-highlight" />
                  Progress tracked
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