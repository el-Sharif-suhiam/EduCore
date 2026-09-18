"use client";

// ============================================================
// KNOWLEDGE NETWORK — EduCore's hero metaphor (UiUxDesign §9).
// An abstract "learning world": topics as a constellation of
// nodes joined by ink lines, floating course-fragment cards
// (index-card DNA), and one highlighted milestone — certified!.
// It is NOT a literal graph; it reads as a hand-annotated
// notebook map of a mind that got there deliberately.
//
// Animates ONLY pathLength, transform, and opacity — the same
// choreography language as LearningPath. Reduced motion → static.
// ============================================================

import { motion, useReducedMotion } from "motion/react";
import { duration, ease } from "@/lib/motion";

const INK_PATH = "M445,330 L560,230 L660,320 L745,235";

type Pt = { x: number; y: number };

// Solid ink links between the hubs (the implied "progress" line
// from start → … → certified is drawn as INK_PATH on top of them).
const INK_EDGES: [Pt, Pt][] = [
  [{ x: 445, y: 330 }, { x: 385, y: 205 }],
  [{ x: 560, y: 230 }, { x: 660, y: 320 }],
  [{ x: 660, y: 320 }, { x: 620, y: 440 }],
  [{ x: 445, y: 330 }, { x: 350, y: 430 }],
  [{ x: 350, y: 430 }, { x: 480, y: 505 }],
];

// Faint dashed links — the wider conceptual network.
const FAINT_EDGES: [Pt, Pt][] = [
  [{ x: 385, y: 205 }, { x: 320, y: 295 }],
  [{ x: 745, y: 235 }, { x: 790, y: 350 }],
  [{ x: 745, y: 235 }, { x: 805, y: 265 }],
  [{ x: 560, y: 230 }, { x: 512, y: 130 }],
  [{ x: 512, y: 130 }, { x: 615, y: 110 }],
  [{ x: 560, y: 230 }, { x: 700, y: 190 }],
  [{ x: 700, y: 190 }, { x: 745, y: 235 }],
  [{ x: 660, y: 320 }, { x: 620, y: 440 }],
  [{ x: 620, y: 440 }, { x: 540, y: 545 }],
];

const HUB: { x: number; y: number; r: number }[] = [
  { x: 560, y: 230, r: 9 },
  { x: 445, y: 330, r: 7 },
  { x: 660, y: 320, r: 7 },
];

const NODES: (Pt & { r: number })[] = [
  { x: 385, y: 205, r: 4.5 },
  { x: 512, y: 130, r: 4.5 },
  { x: 620, y: 440, r: 4.5 },
  { x: 350, y: 430, r: 3.6 },
  { x: 790, y: 350, r: 3.4 },
  { x: 480, y: 505, r: 3.6 },
  { x: 805, y: 265, r: 2.8 },
  { x: 615, y: 110, r: 2.8 },
  { x: 540, y: 545, r: 2.8 },
  { x: 700, y: 190, r: 3.2 },
  { x: 320, y: 295, r: 2.8 },
];

const LABELS: (Pt & { t: string; className: string; fontSize: number })[] = [
  { x: 516, y: 88, t: "curiosity", className: "text-primary/70", fontSize: 22 },
  { x: 455, y: 392, t: "practice", className: "text-primary/70", fontSize: 22 },
  { x: 676, y: 384, t: "mastery", className: "text-primary/70", fontSize: 22 },
  { x: 486, y: 590, t: "start", className: "text-primary/50", fontSize: 20 },
  { x: 745, y: 288, t: "certified!", className: "text-highlight-foreground", fontSize: 25 },
];

/** Index-card fragment glyph: a mini paper note with text lines. */
function NoteCard({
  x,
  y,
  accent = false,
  rotate = 0,
  play = false,
}: {
  x: number;
  y: number;
  accent?: boolean;
  rotate?: number;
  play?: boolean;
}) {
  const w = 86;
  const h = 58;
  return (
    <g
      transform={`translate(${x} ${y}) rotate(${rotate})`}
      stroke="var(--rule-strong)"
      strokeWidth="1.3"
      className="fill-card"
    >
      <rect x={-w / 2} y={-h / 2} width={w} height={h} rx="10" />
      {accent && (
        <rect
          x={-w / 2}
          y={-h / 2}
          width={w}
          height="9"
          rx="4"
          className="fill-highlight"
          stroke="none"
        />
      )}
      {[0, 1, 2].map((i) => (
        <rect
          key={i}
          x={-w / 2 + 14}
          y={-h / 2 + 22 + i * 11}
          width={i === 1 ? w - 52 : i === 0 ? w - 40 : w - 28}
          height="4.5"
          rx="2.25"
          className="stroke-primary/25"
          strokeWidth="1.1"
        />
      ))}
      {play && (
        <g
          transform={`translate(${w / 2 - 16} ${0})`}
          stroke="var(--rule-strong)"
          strokeWidth="1.2"
          fill="none"
        >
          <circle r="7" />
          <path d="M-2.2 -3.6 L-2.2 3.6 L3.6 0 Z" className="fill-highlight" stroke="none" />
        </g>
      )}
    </g>
  );
}

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
      {/* halo orbit behind the certified node */}
      <circle
        cx={745}
        cy={235}
        r={30}
        strokeWidth="1.2"
        strokeDasharray="3 6"
        className="stroke-highlight/60"
      />

      {/* faint network */}
      {FAINT_EDGES.map(([a, b], i) => (
        <line
          key={i}
          x1={a.x}
          y1={a.y}
          x2={b.x}
          y2={b.y}
          strokeWidth="1.3"
          strokeDasharray="2 7"
          strokeLinecap="round"
          className="stroke-primary/25"
        />
      ))}

      {/* solid ink links */}
      {INK_EDGES.map(([a, b], i) => (
        <line
          key={i}
          x1={a.x}
          y1={a.y}
          x2={b.x}
          y2={b.y}
          strokeWidth="1.8"
          strokeLinecap="round"
          className="stroke-primary/50"
        />
      ))}

      {/* the drawn progress line start → … → certified */}
      {draw ? (
        <motion.path
          d={INK_PATH}
          fill="none"
          strokeWidth="2.4"
          strokeLinecap="round"
          className="stroke-primary"
          initial={{ pathLength: 0 }}
          animate={{ pathLength: 1 }}
          transition={{
            duration: duration.cinematic * 1.6,
            ease: ease.natural,
            delay: 0.15,
          }}
        />
      ) : (
        <path
          d={INK_PATH}
          fill="none"
          strokeWidth="2.4"
          strokeLinecap="round"
          className="stroke-primary"
        />
      )}

      {/* hubs */}
      {HUB.map((n, i) => (
        <circle
          key={`hub-${i}`}
          cx={n.x}
          cy={n.y}
          r={n.r}
          strokeWidth="1.6"
          className="fill-card stroke-primary/60"
        />
      ))}

      {/* certified milestone */}
      {draw ? (
        <motion.g
          initial={{ opacity: 0, scale: 0.6 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: duration.normal, ease: ease.spring, delay: 0.4 }}
          style={{ transformOrigin: "745px 235px" }}
        >
          <circle cx={745} cy={235} r={13} className="fill-highlight" />
          <g
            transform="translate(745 235)"
            strokeWidth="1.7"
            strokeLinecap="round"
            strokeLinejoin="round"
            className="text-highlight-foreground"
          >
            <path d="M-3.5 0 l2.6 2.6 4.4 -5" fill="none" />
          </g>
        </motion.g>
      ) : (
        <g>
          <circle cx={745} cy={235} r={13} className="fill-highlight" />
          <g
            transform="translate(745 235)"
            strokeWidth="1.7"
            strokeLinecap="round"
            strokeLinejoin="round"
            className="text-highlight-foreground"
          >
            <path d="M-3.5 0 l2.6 2.6 4.4 -5" fill="none" stroke="currentColor" />
          </g>
        </g>
      )}

      {/* ordinary nodes */}
      {NODES.map((n, i) => (
        <circle
          key={`node-${i}`}
          cx={n.x}
          cy={n.y}
          r={n.r}
          strokeWidth="1.2"
          className="fill-card"
          stroke="var(--rule-strong)"
        />
      ))}

      {/* the ink "progress" dots on the drawn line */}
      {draw ? (
        <motion.g
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: duration.normal, delay: 0.35 }}
        >
          {[{ x: 502, y: 280 }, { x: 610, y: 275 }].map((n, i) => (
            <circle
              key={`ink-${i}`}
              cx={n.x}
              cy={n.y}
              r={3}
              className="fill-primary"
              stroke="none"
            />
          ))}
        </motion.g>
      ) : (
        <g>
          {[{ x: 502, y: 280 }, { x: 610, y: 275 }].map((n, i) => (
            <circle
              key={`ink-${i}`}
              cx={n.x}
              cy={n.y}
              r={3}
              className="fill-primary"
              stroke="none"
            />
          ))}
        </g>
      )}

      {/* course-fragment notes */}
      {draw ? (
        <motion.g
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: duration.slow, ease: ease.natural, delay: 0.55 }}
        >
          <NoteCard x={255} y={120} rotate={-6} />
        </motion.g>
      ) : (
        <NoteCard x={255} y={120} rotate={-6} />
      )}
      {draw ? (
        <motion.g
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: duration.slow, ease: ease.natural, delay: 0.75 }}
        >
          <NoteCard x={735} y={452} rotate={5} accent play />
        </motion.g>
      ) : (
        <NoteCard x={735} y={452} rotate={5} accent play />
      )}

      {/* handwritten labels */}
      {LABELS.map((l, i) => (
        <text
          key={`label-${i}`}
          x={l.x}
          y={l.y}
          fontSize={l.fontSize}
          textAnchor="middle"
          className={`fill-current font-hand ${l.className}`}
        >
          {l.t}
        </text>
      ))}
    </svg>
  );
}