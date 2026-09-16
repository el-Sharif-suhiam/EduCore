import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { Container } from "@/components/shared/container";
import { Button } from "@/components/ui/button";

export function OutcomesCta() {
  return (
    <section id="outcomes" className="scroll-mt-20 py-24 sm:py-32" aria-labelledby="outcomes-heading">
      <Container>
        <div className="mx-auto max-w-2xl text-center">
          {/* certificate stamp — ink-blue, slightly rotated, like the
              real seal on a finished course (UiUxDesign §28) */}
          <div className="mx-auto mb-8 w-fit -rotate-[8deg] opacity-90">
            <svg
              aria-hidden="true"
              viewBox="0 0 120 120"
              className="size-24 sm:size-28"
              fill="none"
            >
              <defs>
                <path
                  id="stamp-ring"
                  d="M 60 13 a 47 47 0 1 1 -0.01 0"
                />
              </defs>
              <circle cx="60" cy="60" r="56" stroke="currentColor" strokeWidth="2.4" className="text-primary/70" />
              <circle cx="60" cy="60" r="40" stroke="currentColor" strokeWidth="1.2" className="text-primary/50" />
              <text fontSize="10.2" letterSpacing="2.6" className="fill-current font-mono uppercase text-primary/75">
                <textPath href="#stamp-ring" startOffset="0">
                  EduCore · Verified Completion ·
                </textPath>
              </text>
              <path
                d="M46 61 l10 10 l19 -22"
                stroke="currentColor"
                strokeWidth="4"
                strokeLinecap="round"
                strokeLinejoin="round"
                className="text-primary/80"
              />
            </svg>
          </div>
          <span className="sr-only">Verified certificate</span>

          <h2
            id="outcomes-heading"
            className="font-display text-4xl font-semibold tracking-tight text-balance sm:text-5xl"
          >
            Finish what you start
          </h2>
          <p className="mx-auto mt-4 max-w-xl text-lg leading-relaxed text-muted-foreground">
            EduCore is designed so the last lesson is always reachable: clear
            path, visible progress, and a certificate waiting at the end.
          </p>

          <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
            <Button size="lg" className="h-11 px-6 text-base" asChild>
              <Link href="/register">
                Start learning today
                <ArrowRight data-icon="inline-end" />
              </Link>
            </Button>
            <Button
              variant="ghost"
              size="lg"
              className="h-11 px-6 text-base"
              asChild
            >
              <Link href="#courses">See the catalog</Link>
            </Button>
          </div>
        </div>
      </Container>
    </section>
  );
}
