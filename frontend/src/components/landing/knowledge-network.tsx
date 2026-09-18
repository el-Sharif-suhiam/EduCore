"use client";

// ============================================================
// KNOWLEDGE NETWORK — EduCore's hero metaphor (UiUxDesign §9).
// A quiet, machine-consistent "learning world": a hub-and-spoke
// network of topics (nodes) that read as an abstract knowledge
// graph — symmetric geometry, two link styles (ink spokes vs
// faint dashed reach), a hand-drawn progress spine climbing to
// the single highlighted milestone (a checked "certified!") and
// two floating course-fragment notes anchored outside the graph.
//
// Positions are COMPUTED (polar geometry, not hand-picked magic
// numbers) so the composition stays balanced by construction.
// Animates ONLY pathLength/transform/opacity; reduced-motion safe.
// ============================================================

import { motion, useReducedMotion } from "motion/react";
import { duration, ease } from "@/lib/motion";

const CX = 430;
const CY = 318;
const INNER_R = 150;
const OUTER_R = 235;

const polar = (r: number, deg: number) => {
  const rad = (deg * Math.PI) / 180;
  return {
    x: Math.round(CX + r * Math.sin(rad)),
    y: Math.round(CY - r * Math.cos(rad)),
  };
};

// Six evenly spaced positions (clockwise from 12 o'clock).
const ANGLES = [0, 60, 120, 180, 240, 300];
const INNER = ANGLES.map((a) => polar(INNER_R, a));
const OUTER = ANGLES.map((a) => polar(OUTER_R, a));

// The accent milestone = outer 60° (upper-right).
const CERT = OUTER[1]; // (634, 200)

const INK_PATH = `M${OUTER[3].x},${OUTER[3].y} L${INNER[3].x},${INNER[3].y} L${CX},${CY} L${INNER[1].x},${INNER[1].y} L${CERT.x},${CERT.y}`;

const LABELS = [
  { x: 430, y: 602, t: "start", className: "text-primary/60", size: 22, anchor: "middle" },
  { x: 160, y: 210, t: "curiosity", className: "text-primary/70", size: 22, anchor: "middle" },
  { x: 150, y: 446, t: "mastery", className: "text-primary/70", size: 22, anchor: "middle" },
  { x: 708, y: 442, t: "practice", className: "text-primary/70", size: 22, anchor: "middle" },
  { x: 748, y: 210, t: "certified!", className: "text-highlight-foreground", size: 25, anchor: "middle" },
] as const;

export function KnowledgeNetwork({
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
      {/* constellation boundary — the network's "world" */}
      <circle
        cx={CX}
        cy={CY}
        r={270}
        strokeWidth="1.2"
        strokeDasharray="1 8"
        className="stroke-primary/15"
      />

      {/* ink spokes: hub → inner ring */}
      {INNER.map((n, i) => (
        <line
          key={`spoke-${i}`}
          x1={CX}
          y1={CY}
          x2={n.x}
          y2={n.y}
          strokeWidth="1.8"
          strokeLinecap="round"
          className="stroke-primary/45"
        />
      ))}

      {/* faint reach: inner → outer ring */}
      {INNER.map((n, i) => (
        <line
          key={`reach-${i}`}
          x1={n.x}
          y1={n.y}
          x2={OUTER[i].x}
          y2={OUTER[i].y}
          strokeWidth="1.3"
          strokeDasharray="2 7"
          strokeLinecap="round"
          className="stroke-primary/25"
        />
      ))}

      {/* a couple of outer chords for graph character */}
      <line x1={OUTER[0].x} y1={OUTER[0].y} x2={OUTER[5].x} y2={OUTER[5].y}
        strokeWidth="1.3" strokeDasharray="2 7" strokeLinecap="round" className="stroke-primary/25" />
      <line x1={OUTER[0].x} y1={OUTER[0].y} x2={OUTER[1].x} y2={OUTER[1].y}
        strokeWidth="1.3" strokeDasharray="2 7" strokeLinecap="round" className="stroke-primary/25" />

      {/* inner ring nodes */}
      {INNER.map((n, i) => (
        <circle
          key={`inner-${i}`}
          cx={n.x}
          cy={n.y}
          r={10}
          strokeWidth="1.6"
          className="fill-card stroke-primary/60"
        />
      ))}

      {/* outer ring nodes */}
      {OUTER.map((n, i) => (
        <circle
          key={`outer-${i}`}
          cx={n.x}
          cy={n.y}
          r={5}
          strokeWidth="1.3"
          className="fill-card"
          stroke="var(--rule-strong)"
        />
      ))}

      {/* halo for the certified milestone */}
      <circle
        cx={CERT.x}
        cy={CERT.y}
        r={30}
        strokeWidth="1.2"
        strokeDasharray="3 6"
        className="stroke-highlight/60"
      />

      {/* the drawn progress spine: start → … → certified */}
      {draw ? (
        <motion.path
          d={INK_PATH}
          strokeWidth="2.6"
          strokeLinecap="round"
          className="stroke-primary"
          initial={{ pathLength: 0 }}
          animate={{ pathLength: 1 }}
          transition={{ duration: duration.cinematic * 1.6, ease: ease.natural, delay: 0.15 }}
        />
      ) : (
        <path
          d={INK_PATH}
          strokeWidth="2.6"
          strokeLinecap="round"
          className="stroke-primary"
        />
      )}

      {/* certified check-mark */}
      <g transform={`translate(${CERT.x} ${CERT.y})`}>
        <circle r={13} className="fill-highlight" />
        <g
          strokeWidth="1.8"
          strokeLinecap="round"
          strokeLinejoin="round"
          className="text-highlight-foreground"
        >
          <path d="M-3.6 0 l2.6 2.6 4.6 -5.2" fill="none" stroke="currentColor" />
        </g>
      </g>

      {/* course-fragment notes, anchored outside the graph */}
      <g transform="translate(150 520) rotate(-5)" stroke="var(--rule-strong)" strokeWidth="1.3" className="fill-card">
        <rect x={-43} y={-29} width={86} height={58} rx="10" />
        {[0, 1, 2].map((i) => (
          <rect key={i} x={-29} y={-7 + i * 11} width={i === 0 ? 46 : i === 1 ? 34 : 58} height="4.5" rx="2.25"
            className="stroke-primary/25" strokeWidth="1.1" />
        ))}
      </g>
      <g transform="translate(735 505) rotate(5)" stroke="var(--rule-strong)" strokeWidth="1.3" className="fill-card">
        <rect x={-43} y={-29} width={86} height={58} rx="10" />
        <rect x={-43} y={-29} width={86} height="9" rx="4" className="fill-highlight" stroke="none" />
        {[0, 1].map((i) => (
          <rect key={i} x={-29} y={-7 + i * 11} width={i === 0 ? 46 : 34} height="4.5" rx="2.25"
            className="stroke-primary/25" strokeWidth="1.1" />
        ))}
        <g transform="translate(27 0)" stroke="var(--rule-strong)" strokeWidth="1.2" fill="none">
          <circle r={8} />
          <path d="M-2.6 -4 L-2.6 4 L4 0 Z" className="fill-highlight" stroke="none" />
        </g>
      </g>

      {/* handwritten labels */}
      {LABELS.map((l) => (
        <text
          key={l.t}
          x={l.x}
          y={l.y}
          fontSize={l.size}
          textAnchor={l.anchor}
          className={`fill-current font-hand ${l.className}`}
        >
          {l.t}
        </text>
      ))}
    </svg>
  );
}