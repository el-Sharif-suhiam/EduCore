import Link from "next/link";
import { notFound } from "next/navigation";
import type { Metadata } from "next";
import { ArrowLeft, Box, GraduationCap } from "lucide-react";
import { Container } from "@/components/shared/container";
import { AddToCartButton } from "@/components/shared/add-to-cart-button";
import { Badge } from "@/components/ui/badge";
import {
  getBundleServer,
  getBundleItemsServer,
  type BundleSummary,
  type BundleItems,
} from "@/lib/courses";

const price = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
});

type Props = { params: Promise<{ id: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  try {
    const bundle = await getBundleServer(Number(id));
    return { title: bundle.name };
  } catch {
    return { title: "Bundle" };
  }
}

export default async function BundleDetailPage({ params }: Props) {
  const { id } = await params;
  const bundleId = Number(id);
  if (!Number.isInteger(bundleId) || bundleId <= 0) notFound();

  let bundle: BundleSummary & { isPublished: boolean };
  try {
    bundle = await getBundleServer(bundleId);
  } catch {
    notFound();
  }

  let items: BundleItems["courses"] | null = null;
  try {
    const data = await getBundleItemsServer(bundleId);
    items = data.courses;
  } catch {
    items = null; // non-fatal: contents section degrades
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
              <Badge variant="secondary">Bundle</Badge>
              {bundle.isPublished ? (
                <Badge>Published</Badge>
              ) : (
                <Badge variant="outline">Unpublished</Badge>
              )}
            </div>

            <h1 className="mt-4 font-display text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
              {bundle.name}
            </h1>

            {bundle.summary && (
              <p className="mt-4 max-w-2xl text-lg leading-relaxed text-muted-foreground">
                {bundle.summary}
              </p>
            )}

            {/* cover */}
            <div className="relative mt-8 aspect-video w-full overflow-hidden rounded-xl bg-secondary ring-1 ring-foreground/10">
              {bundle.thumbnailUrl ? (
                // eslint-disable-next-line @next/next/no-img-element -- external user-defined URLs
                <img
                  src={bundle.thumbnailUrl}
                  alt=""
                  className="size-full object-cover"
                />
              ) : (
                <div
                  aria-hidden="true"
                  className="flex size-full items-center justify-center"
                  style={{
                    background:
                      "linear-gradient(135deg, color-mix(in oklch, var(--highlight) 26%, var(--card)), color-mix(in oklch, var(--primary) 14%, var(--card)))",
                  }}
                >
                  <Box className="size-14 text-primary/50" />
                </div>
              )}
            </div>

            {/* contents */}
            <section className="mt-12" aria-labelledby="inside-heading">
              <h2
                id="inside-heading"
                className="font-display text-2xl font-semibold tracking-tight"
              >
                What&apos;s inside
              </h2>

              {items === null ? (
                <p className="mt-4 rounded-xl border border-dashed p-6 text-sm text-muted-foreground">
                  The contents list is unavailable right now.
                </p>
              ) : items.length === 0 ? (
                <p className="mt-4 rounded-xl border border-dashed p-6 text-sm text-muted-foreground">
                  No courses in this bundle yet.
                </p>
              ) : (
                <ol className="mt-5 divide-y overflow-hidden rounded-xl ring-1 ring-foreground/10 bg-card">
                  {items.map((c, i) => (
                    <li
                      key={c.courseId}
                      className="flex items-center gap-4 px-5 py-4 transition-colors duration-200 hover:bg-accent/50"
                    >
                      <span className="font-mono text-xs text-muted-foreground">
                        {String(i + 1).padStart(2, "0")}
                      </span>
                      <div className="min-w-0 flex-1">
                        <Link
                          href={`/courses/${c.courseId}`}
                          className="truncate font-medium underline-offset-4 hover:text-primary hover:underline"
                        >
                          {c.name}
                        </Link>
                        {c.summary && (
                          <p className="mt-0.5 line-clamp-1 text-sm text-muted-foreground">
                            {c.summary}
                          </p>
                        )}
                      </div>
                      <GraduationCap
                        aria-hidden="true"
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
                {price.format(bundle.basePrice)}
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                One payment for the whole path · cheaper than buying separately
              </p>

              <AddToCartButton productId={bundle.productId} />

              <ul className="mt-5 space-y-2 text-sm text-muted-foreground">
                <li className="flex items-center gap-2">
                  <span className="size-1.5 rounded-full bg-highlight" />
                  {(items?.length ?? 0)} course{((items?.length ?? 0)) === 1 ? "" : "s"} included
                </li>
                <li className="flex items-center gap-2">
                  <span className="size-1.5 rounded-full bg-highlight" />
                  Progress tracked per course
                </li>
                <li className="flex items-center gap-2">
                  <span className="size-1.5 rounded-full bg-highlight" />
                  Certificates on each completed course
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