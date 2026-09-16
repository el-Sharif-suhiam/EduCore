import { Map, TrendingUp, BadgeCheck } from "lucide-react";
import { Container } from "@/components/shared/container";
import { Mark } from "@/components/shared/mark";
import { MarginNote } from "@/components/shared/margin-note";

const FEATURES = [
  {
    icon: Map,
    title: "Structured paths",
    body: "Courses aren't isolated videos. They connect into journeys — fundamentals, practice, mastery — so you always know what comes next.",
  },
  {
    icon: TrendingUp,
    title: "Honest progress",
    body: "Every completed lesson updates your position on the path. No vanity percentages — just a clear picture of where you stand.",
  },
  {
    icon: BadgeCheck,
    title: "Certificates that verify",
    body: "Finish a course and receive a certificate with built-in verification. Proof, not decoration.",
  },
];

export function WhatIs() {
  return (
    <section
      id="what-is"
      className="scroll-mt-20 border-t bg-card/30 py-24 sm:py-28"
      aria-labelledby="what-is-heading"
    >
      <Container>
        <div className="max-w-2xl">
          <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
            Why EduCore
          </p>
          <h2
            id="what-is-heading"
            className="mt-2 font-display text-3xl font-semibold tracking-tight text-balance sm:text-4xl"
          >
            Learning, <Mark>structured</Mark> like a craft
          </h2>
          <p className="mt-4 text-lg leading-relaxed text-muted-foreground">
            Most platforms hand you a search bar and wish you luck. EduCore is
            built around the way skills actually develop: in sequence, with
            feedback, and with proof at the end.
          </p>
          <p className="mt-3">
            <MarginNote arrow>like good notes — you can find your place</MarginNote>
          </p>
        </div>

        <div className="mt-12 grid gap-5 md:grid-cols-3">
          {FEATURES.map((f) => (
            <div
              key={f.title}
              className="index-card rounded-xl bg-card p-6 ring-1 ring-foreground/10 shadow-rest transition-shadow duration-200 ease-natural hover:shadow-lift"
            >
              <f.icon aria-hidden="true" className="size-5 text-primary" />
              <h3 className="mt-4 font-display text-lg font-semibold">
                {f.title}
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
                {f.body}
              </p>
            </div>
          ))}
        </div>
      </Container>
    </section>
  );
}
