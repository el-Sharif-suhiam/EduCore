"use client";

// ============================================================
// LEARNING JOURNEY preview (UiUxDesign §14) — second signature
// moment. The connecting line DRAWS itself as you scroll
// through the section; each stage lights up as it's reached.
// Transform/opacity only. Reduced motion → fully static.
// ============================================================

import { useRef } from "react";
import {
  motion,
  useScroll,
  useTransform,
  useReducedMotion,
  type MotionValue,
} from "motion/react";
import { Check } from "lucide-react";
import { Container } from "@/components/shared/container";
import { MarginNote } from "@/components/shared/margin-note";
import { cn } from "@/lib/utils";

type Stage = { key: string; title: string; note: string };

const STAGES: Stage[] = [
  { key: "start", title: "Start", note: "Pick a goal — any subject, any level." },
  { key: "foundations", title: "Foundations", note: "Core concepts, in the right order." },
  { key: "practice", title: "Practice", note: "Apply skills through exercises and projects." },
  { key: "mastery", title: "Mastery", note: "Depth, nuance, and real understanding." },
  { key: "goal", title: "Certificate & beyond", note: "Prove it, share it, keep growing." },
];

export function Journey() {
  const listRef = useRef<HTMLOListElement>(null);
  const reduce = useReducedMotion();

  const { scrollYProgress } = useScroll({
    target: listRef,
    offset: ["start 0.75", "end 0.55"],
  });

  // The line grows downward as you scroll.
  const lineScale = useTransform(scrollYProgress, [0, 1], [0, 1]);

  return (
    <section
      id="journey"
      className="scroll-mt-20 py-24 sm:py-28"
      aria-labelledby="journey-heading"
    >
      <Container>
        <div className="max-w-2xl">
          <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
            The learning journey
          </p>
          <h2
            id="journey-heading"
            className="mt-2 font-display text-3xl font-semibold tracking-tight sm:text-4xl"
          >
            You always know where you are
          </h2>
          <p className="mt-4 text-lg leading-relaxed text-muted-foreground">
            Progress isn&apos;t a percentage buried in a profile page.
            It&apos;s a path you can see — completed stages behind you, the
            next step ahead.
          </p>
          <p className="mt-3">
            <MarginNote>— you are always somewhere on this trail</MarginNote>
          </p>
        </div>

        <ol ref={listRef} className="relative mt-14 max-w-xl space-y-10 ps-8">
          {/* rail — dashed like an unwalked trail */}
          <span
            aria-hidden="true"
            className="absolute inset-y-1 start-[11px] w-0 border-s border-dashed border-border"
          />
          {/* drawing line */}
          <motion.span
            aria-hidden="true"
            style={reduce ? { scaleY: 1 } : { scaleY: lineScale }}
            className="absolute inset-y-1 start-[11px] w-px origin-top bg-primary"
          />

          {STAGES.map((stage, i) => (
            <StageItem
              key={stage.key}
              stage={stage}
              index={i}
              total={STAGES.length}
              progress={scrollYProgress}
              reduced={!!reduce}
            />
          ))}
        </ol>
      </Container>
    </section>
  );
}

function StageItem({
  stage,
  index,
  total,
  progress,
  reduced,
}: {
  stage: Stage;
  index: number;
  total: number;
  progress: MotionValue<number>;
  reduced: boolean;
}) {
  // Stages activate one after another across the shared progress.
  const opacity = useTransform(
    progress,
    [index / total - 0.08, index / total],
    [0.35, 1]
  );

  return (
    <motion.li
      style={reduced ? undefined : { opacity }}
      className="relative flex items-start gap-4"
    >
      <span
        className={cn(
          "absolute -start-8 top-0.5 flex size-6 items-center justify-center rounded-full ring-1",
          index === 0
            ? "bg-highlight text-highlight-foreground ring-highlight/50"
            : "bg-card text-primary ring-primary/40"
        )}
        aria-hidden="true"
      >
        {index === 0 ? (
          <Check className="size-3.5" />
        ) : (
          <span className="-rotate-6 font-hand text-sm leading-none">{index}</span>
        )}
      </span>
      <div>
        <h3 className="font-display text-lg font-semibold">{stage.title}</h3>
        <p className="mt-1 text-sm leading-relaxed text-muted-foreground">
          {stage.note}
        </p>
      </div>
    </motion.li>
  );
}
