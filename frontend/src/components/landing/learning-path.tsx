"use client";

// ============================================================
// LEARNING PATH — EduCore's hero metaphor (study-notes DNA).
// A hand-drawn winding trail that draws itself upward through
// five milestones (Start → Foundations → Practice → Mastery →
// Certified), annotated like a tutor's notebook page.
//
// Animates ONLY stroke-dashoffset-equivalent (pathLength),
// transform, and opacity. Reduced motion → fully drawn static.
// ============================================================

import { motion, useReducedMotion } from "motion/react";
import { duration, ease } from "@/lib/motion";

const PATH_D =
  "M 60 560 C 130 548 200 470 285 400 C 360 340 420 352 478 332 C 545 308 585 275 655 207 C 705 158 760 128 810 92";

// Node positions sit ON the trail; each carries a small subject
// doodle + handwritten label.
type Node = {
  x: number;
  y: number;
  label: string;
  delay: number;
};

const NODES: Node[] = [
  { x: 62, y: 558, label: "start", delay: 0.12 },
  { x: 287, y: 398, label: "foundations", delay: 0.42 },
  { x: 479, y: 331, label: "practice", delay: 0.72 },
  { x: 656, y: 206, label: "mastery", delay: 1.02 },
  { x: 811, y: 91, label: "certified!", delay: 1.32 },
];

/** Small hand-drawn subject doodles, stroke-only. */
function Doodle({ index }: { index: number }) {
  const common = {
    stroke: "currentColor",
    strokeWidth: 1.9,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    fill: "none",
  } as const;

  switch (index) {
    case 0: // start — flag
      return (
        <g {...common}>
          <path d="M-7 10 V-10" />
          <path d="M-7 -10 c4 -2 7 2 12 0 l-3 5 3 5 c-5 2 -8 -2 -12 0 Z" />
        </g>
      );
    case 1: // foundations — building blocks
      return (
        <g {...common}>
          <rect x="-10" y="1" width="8" height="7" rx="1" />
          <rect x="1" y="1" width="8" height="7" rx="1" />
          <rect x="-4" y="-8" width="8" height="7" rx="1" />
        </g>
      );
    case 2: // practice — pencil
      return (
        <g {...common}>
          <path d="M-10 10 L4 -8 l5 4 -14 18 Z" transform="rotate(8)" />
          <path d="M-10 10 l-3 6 6 -3" transform="rotate(8)" />
        </g>
      );
    case 3: // mastery — mountain + star
      return (
        <g {...common}>
          <path d="M-11 9 L-3 -6 L2 2 L6 -3 L11 9 Z" />
          <path d="M4 -9 l1.2 2.4 2.6 0.3 -1.9 1.8 0.5 2.6 -2.4 -1.2 -2.4 1.2 0.5 -2.6 -1.9 -1.8 2.6 -0.3 Z" />
        </g>
      );
    default: // certified — rosette check
      return (
        <g {...common}>
          <circle cx="0" cy="-2" r="7" />
          <path d="M-3 -2 l2.4 2.4 4 -4.6" />
          <path d="M-3 4 l-2 6 M3 4 l2 6" />
        </g>
      );
  }
}

export function LearningPath({
  className,
  animated = true,
}: {
  className?: string;
  animated?: boolean;
}) {
  const reduce = useReducedMotion();
  const draw = animated && !reduce;

  return (
    <svg
      viewBox="0 0 900 620"
      aria-hidden="true"
      className={className}
      fill="none"
    >
      {/* unwalked trail — faint dashed base */}
      <path
        d={PATH_D}
        stroke="currentColor"
        strokeWidth="1.6"
        strokeDasharray="2 10"
        strokeLinecap="round"
        className="text-primary/25"
      />

      {/* the drawn trail */}
      {draw ? (
        <motion.path
          d={PATH_D}
          stroke="currentColor"
          strokeWidth="2.6"
          strokeLinecap="round"
          className="text-primary"
          initial={{ pathLength: 0 }}
          animate={{ pathLength: 1 }}
          transition={{
            duration: duration.cinematic * 1.8,
            ease: ease.natural,
            delay: 0.15,
          }}
        />
      ) : (
        <path
          d={PATH_D}
          stroke="currentColor"
          strokeWidth="2.6"
          strokeLinecap="round"
          className="text-primary"
        />
      )}

      {/* milestones */}
      {NODES.map((n, i) => {
        const last = i === NODES.length - 1;
        const glyph = (
          <>
            {/* halo ring */}
            <circle
              cx={n.x}
              cy={n.y}
              r={last ? 22 : 19}
              className={
                last ? "stroke-highlight/60" : "stroke-primary/30"
              }
              strokeWidth="1.2"
              strokeDasharray={last ? undefined : "3 5"}
            />
            {/* ink dot */}
            <circle
              cx={n.x}
              cy={n.y}
              r={last ? 13 : 10}
              className={last ? "fill-highlight" : "fill-card"}
              stroke={last ? undefined : "var(--rule-strong)"}
              strokeWidth="1.2"
            />
            <g
              transform={`translate(${n.x} ${n.y})`}
              className={last ? "text-highlight-foreground" : "text-primary"}
            >
              <Doodle index={i} />
            </g>
            {/* handwritten label */}
            <text
              x={n.x + (i === 0 ? 26 : i === NODES.length - 1 ? -30 : 26)}
              y={n.y + (i === NODES.length - 1 ? 42 : -24)}
              textAnchor={i === NODES.length - 1 ? "end" : "start"}
              fontSize="26"
              className="fill-current font-hand text-primary/80"
            >
              {n.label}
            </text>
          </>
        );

        return draw ? (
          <motion.g
            key={n.label}
            initial={{ opacity: 0, scale: 0.6 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{
              duration: duration.normal,
              ease: ease.spring,
              delay: n.delay,
            }}
            style={{ transformOrigin: `${n.x}px ${n.y}px` }}
          >
            {glyph}
          </motion.g>
        ) : (
          <g key={n.label}>{glyph}</g>
        );
      })}
    </svg>
  );
}
