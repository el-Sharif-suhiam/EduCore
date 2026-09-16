import Link from "next/link";
import { Users } from "lucide-react";
import type { CourseSummary } from "@/lib/courses";
import { cn } from "@/lib/utils";

const price = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
});

// Restrained card language (UiUxDesign §34): hover lifts 2px, never more.
export function CourseCard({
  course,
  className,
}: {
  course: CourseSummary;
  className?: string;
}) {
  const cover = course.coverImageUrl ?? course.thumbnailUrl ?? null;
  const instructors =
    course.courseInstructors?.map((i) => i.instructorName).join(", ") ?? null;

  return (
    <article
      className={cn(
        "group relative flex flex-col overflow-hidden rounded-xl bg-card ring-1 ring-foreground/10 shadow-rest",
        "transition-[transform,box-shadow] duration-200 ease-natural",
        "hover:-translate-y-0.5 hover:shadow-lift",
        className
      )}
    >
      {/* cover */}
      <div className="relative aspect-video w-full overflow-hidden bg-secondary">
        {cover ? (
          // eslint-disable-next-line @next/next/no-img-element -- external URLs are user-defined; next/image remotePatterns cannot be known ahead
          <img
            src={cover}
            alt=""
            loading="lazy"
            className="size-full object-cover transition-transform duration-300 ease-natural group-hover:scale-[1.02]"
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
            <span className="font-display text-5xl font-semibold text-primary/50">
              {course.title.charAt(0).toUpperCase()}
            </span>
          </div>
        )}
      </div>

      {/* body */}
      <div className="flex flex-1 flex-col gap-1.5 p-4">
        <h3 className="font-display text-lg leading-snug font-semibold text-balance">
          <Link
            href={`/courses/${course.id}`}
            className="transition-colors duration-200 after:absolute after:inset-0 hover:text-primary"
          >
            {course.title}
          </Link>
        </h3>
        {course.summary && (
          <p className="line-clamp-2 text-sm leading-relaxed text-muted-foreground">
            {course.summary}
          </p>
        )}

        <div className="mt-auto flex items-center justify-between gap-3 pt-3">
          {instructors ? (
            <p className="flex min-w-0 items-center gap-1.5 truncate text-xs text-muted-foreground">
              <Users className="size-3.5 shrink-0" />
              <span className="truncate">{instructors}</span>
            </p>
          ) : (
            <span />
          )}
          <p className="shrink-0 font-mono text-sm font-medium">
            {price.format(course.basePrice)}
          </p>
        </div>
      </div>
    </article>
  );
}
