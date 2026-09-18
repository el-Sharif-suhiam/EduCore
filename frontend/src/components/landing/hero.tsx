"use client";

// ============================================================
// HERO — EduCore's signature moment (UiUxDesign §6–9).
// Study-notes atmosphere. Layered composition responding to
// BOTH mouse and scroll:
//
//   layer 0  static gradient atmosphere + paper grain (CSS)
//   layer 1  knowledge network    — medium parallax
//   layer 2  light glow           — strongest, follows cursor
//   layer 3  typography/content   — subtle counter-motion
//
// Performance: springs + transforms only; pointer input feeds
// motion values (no React state per move); everything respects
// prefers-reduced-motion.
// ============================================================

import { useRef } from "react";
import {
  motion,
  useMotionValue,
  useSpring,
  useScroll,
  useTransform,
  useReducedMotion,
} from "motion/react";
import Link from "next/link";
import { ArrowRight, MousePointer2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Container } from "@/components/shared/container";
import { Mark } from "@/components/shared/mark";
import { KnowledgeNetwork } from "@/components/landing/knowledge-network";
import { duration, ease } from "@/lib/motion";

export function Hero() {
  const sectionRef = useRef<HTMLElement>(null);
  const reduce = useReducedMotion();

  // ---- mouse parallax --------------------------------------
  // Normalized cursor position (-0.5 … 0.5), springed for that
  // "the interface is aware of me" feel without shaking.
  const mx = useMotionValue(0);
  const my = useMotionValue(0);
  const sx = useSpring(mx, { stiffness: 55, damping: 18, mass: 0.7 });
  const sy = useSpring(my, { stiffness: 55, damping: 18, mass: 0.7 });

  // Layer multipliers (different strengths → depth)
  const networkX = useTransform(sx, [-0.5, 0.5], [16, -16]);
  const networkY = useTransform(sy, [-0.5, 0.5], [12, -12]);
  const glowX = useTransform(sx, [-0.5, 0.5], [110, -110]);
  const glowY = useTransform(sy, [-0.5, 0.5], [70, -70]);
  const contentX = useTransform(sx, [-0.5, 0.5], [-6, 6]);

  const onPointerMove = (e: React.PointerEvent<HTMLElement>) => {
    if (reduce) return;
    const rect = e.currentTarget.getBoundingClientRect();
    mx.set((e.clientX - rect.left) / rect.width - 0.5);
    my.set((e.clientY - rect.top) / rect.height - 0.5);
  };

  const onPointerLeave = () => {
    mx.set(0);
    my.set(0);
  };

  // ---- scroll choreography ---------------------------------
  // Scrolling away: content drifts up and fades while the world
  // expands slightly — a scene change, not a fade-out.
  const { scrollYProgress } = useScroll({
    target: sectionRef,
    offset: ["start start", "end start"],
  });

  const contentScrollY = useTransform(scrollYProgress, [0, 1], [0, -90]);
  const contentOpacity = useTransform(scrollYProgress, [0, 0.75], [1, 0]);
  const networkScale = useTransform(scrollYProgress, [0, 1], [1, 1.1]);
  const networkOpacity = useTransform(scrollYProgress, [0, 1], [1, 0.25]);

  return (
    <section
      ref={sectionRef}
      onPointerMove={onPointerMove}
      onPointerLeave={onPointerLeave}
      className="paper-grain relative flex min-h-[92svh] items-center overflow-hidden pt-16"
      aria-label="Introduction"
    >
      {/* layer 0 — static atmosphere */}
      <div
        aria-hidden="true"
        className="absolute inset-0"
        style={{
          background:
            "radial-gradient(60rem 30rem at 85% 8%, var(--accent) / 0.55, transparent 65%)," +
            "radial-gradient(45rem 28rem at 8% 90%, color-mix(in oklch, var(--primary) 14%, transparent), transparent 70%)",
        }}
      />

      {/* layer 2 — light glow following the cursor */}
      <motion.div
        aria-hidden="true"
        style={
          reduce
            ? undefined
            : { x: glowX, y: glowY }
        }
        className="pointer-events-none absolute left-1/2 top-1/3 size-[34rem] -translate-x-1/2 -translate-y-1/2 rounded-full opacity-25 blur-3xl"
      >
        <div className="size-full rounded-full bg-highlight/40" />
      </motion.div>

      {/* layer 1 — the learning world */}
      <motion.div
        aria-hidden="true"
        style={
          reduce
            ? { opacity: 1 }
            : {
                x: networkX,
                y: networkY,
                scale: networkScale,
                opacity: networkOpacity,
              }
        }
        className="absolute inset-0 flex items-center justify-end text-primary md:pe-[4vw]"
      >
        <KnowledgeNetwork className="h-auto w-[min(54rem,90vw)] translate-x-[4%] opacity-90 sm:translate-x-0" />
      </motion.div>

      {/* layer 3 — typography & actions */}
      <Container className="relative z-10">
        <motion.div
          style={
            reduce
              ? { opacity: contentOpacity }
              : { x: contentX, y: contentScrollY, opacity: contentOpacity }
          }
          className="max-w-2xl"
        >
          <motion.div
            initial={reduce ? false : { opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: duration.slow, ease: ease.natural }}
          >
          <p className="paper-grain relative inline-flex items-center gap-2 rounded-full border bg-card/70 px-3 py-1 font-mono text-xs uppercase tracking-widest text-muted-foreground backdrop-blur-sm">
            <span className="size-1.5 rounded-full bg-highlight" />
            The learning platform
          </p>
          </motion.div>

          <motion.h1
            initial={reduce ? false : { opacity: 0, y: 26 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{
              duration: duration.cinematic,
              ease: ease.emerge,
              delay: 0.08,
            }}
            className="mt-6 font-display text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl lg:text-7xl"
          >
            Learn deliberately.
            <br />
            <Mark>Remember</Mark> forever.
          </motion.h1>

          <motion.p
            initial={reduce ? false : { opacity: 0, y: 22 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: duration.slow, ease: ease.natural, delay: 0.2 }}
            className="mt-6 max-w-xl text-lg leading-relaxed text-muted-foreground"
          >
            Courses connect into structured paths. Every lesson you finish
            moves you visibly forward — until the day it becomes a
            certificate you can prove.
          </motion.p>

          <motion.div
            initial={reduce ? false : { opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: duration.slow, ease: ease.natural, delay: 0.32 }}
            className="mt-9 flex flex-wrap items-center gap-3"
          >
            <Button
              size="lg"
              className="h-11 px-6 text-base"
              asChild
            >
              <Link href="/register">
                Start learning
                <ArrowRight data-icon="inline-end" />
              </Link>
            </Button>
            <Button
              variant="outline"
              size="lg"
              className="h-11 bg-card/60 px-6 text-base backdrop-blur-sm"
              asChild
            >
              <Link href="#courses">Browse courses</Link>
            </Button>
          </motion.div>
        </motion.div>
      </Container>

      {/* scroll cue — micro hint, hides immediately on scroll */}
      <motion.div
        aria-hidden="true"
        style={{ opacity: contentOpacity }}
        className="absolute bottom-6 left-1/2 hidden -translate-x-1/2 sm:block"
      >
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          <MousePointer2 className="size-3.5 animate-bounce" />
          Scroll to explore
        </div>
      </motion.div>
    </section>
  );
}
