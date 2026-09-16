"use client";

import { motion } from "motion/react";
import { Compass, PlayCircle, Award } from "lucide-react";
import { Container } from "@/components/shared/container";
import { Mark } from "@/components/shared/mark";
import { editorialReveal } from "@/lib/motion";

const STEPS = [
  {
    icon: Compass,
    step: "01",
    title: "Pick your path",
    body: "Browse courses and bundles, add them to your cart, and check out in seconds.",
  },
  {
    icon: PlayCircle,
    step: "02",
    title: "Learn at your pace",
    body: "A calm lesson player with focus mode. Your position is saved the moment you leave.",
  },
  {
    icon: Award,
    step: "03",
    title: "Prove what you know",
    body: "Complete every lesson and your certificate is generated — verifiable, shareable, yours.",
  },
];

export function HowItWorks() {
  return (
    <section
      id="how-it-works"
      className="scroll-mt-20 border-t bg-card/30 py-24 sm:py-28"
      aria-labelledby="how-heading"
    >
      <Container>
        <div className="max-w-2xl">
          <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
            How it works
          </p>
            <h2
              id="how-heading"
              className="mt-2 font-display text-3xl font-semibold tracking-tight sm:text-4xl"
            >
              Three steps. <Mark>No detours.</Mark>
            </h2>
          </div>

        <div className="mt-12 grid gap-5 md:grid-cols-3">
          {STEPS.map((s) => (
            <motion.div
              key={s.step}
              {...editorialReveal}
              className="index-card rounded-xl bg-card p-6 ring-1 ring-foreground/10 shadow-rest transition-shadow duration-200 ease-natural hover:shadow-lift"
            >
              <div className="flex items-center justify-between">
                <s.icon aria-hidden="true" className="size-5 text-primary" />
                <span className="-rotate-3 font-hand text-xl leading-none text-primary/75">
                  {s.step}
                </span>
              </div>
              <h3 className="mt-4 font-display text-lg font-semibold">
                {s.title}
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
                {s.body}
              </p>
            </motion.div>
          ))}
        </div>
      </Container>
    </section>
  );
}
