"use client";

// ============================================================
// MARK — the amber highlighter stroke, EduCore's signature
// device. Wraps a phrase with a marker-pen block behind the
// text (like a real highlighter: slightly rotated, imperfect).
//
//   <Mark>Remember</Mark>            → animates on scroll into view
//   <Mark animated={false}>…</Mark>  → static stroke
// ============================================================

import { motion, useReducedMotion } from "motion/react";
import { duration, ease } from "@/lib/motion";
import { cn } from "@/lib/utils";

export function Mark({
  children,
  className,
  animated = true,
}: {
  children: React.ReactNode;
  className?: string;
  animated?: boolean;
}) {
  const reduce = useReducedMotion();
  const draw = animated && !reduce;

  return (
    <span className={cn("relative inline-block", className)}>
      {draw ? (
        <motion.span
          aria-hidden="true"
          initial={{ scaleX: 0 }}
          whileInView={{ scaleX: 1 }}
          viewport={{ once: true, margin: "-40px" }}
          transition={{ duration: duration.slow, ease: ease.natural }}
          className="absolute inset-x-[-0.08em] bottom-[0.06em] -z-10 h-[0.46em] origin-left -rotate-1 rounded-sm bg-highlight/60"
        />
      ) : (
        <span
          aria-hidden="true"
          className="absolute inset-x-[-0.08em] bottom-[0.06em] -z-10 h-[0.46em] origin-left -rotate-1 rounded-sm bg-highlight/60"
        />
      )}
      {children}
    </span>
  );
}
