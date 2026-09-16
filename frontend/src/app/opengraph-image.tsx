import { ImageResponse } from "next/og";

// ============================================================
// OPEN GRAPH IMAGE — generated at build time (static, cached).
// Study-notes composition: warm paper, ruled lines, red margin,
// the learning-path mark, and the amber highlighter stroke.
// Satori renders a limited CSS subset: flexbox + absolute only.
// ============================================================

export const alt = "EduCore — Learn deliberately. Remember forever.";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

const PAPER = "#FAF7F0";
const INK = "#22303C";
const TEAL = "#2F6B85";
const AMBER = "#EDB94F";
const MARGIN_RED = "#D96A6A";

// Learning-path mark as a data-URI SVG (Satori has no inline <svg>)
const MARK_SVG = encodeURIComponent(
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" width="96" height="96">
    <path d="M8 52 L26 33 L36 42 L56 14" fill="none" stroke="${TEAL}" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/>
    <circle cx="8" cy="52" r="6" fill="${TEAL}"/>
    <circle cx="26" cy="33" r="4.5" fill="${TEAL}" opacity="0.55"/>
    <circle cx="56" cy="14" r="7.5" fill="${AMBER}"/>
  </svg>`
);

export default function OgImage() {
  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          flexDirection: "column",
          justifyContent: "space-between",
          background: PAPER,
          padding: "64px 80px",
          position: "relative",
        }}
      >
        {/* notebook margin line */}
        <div
          style={{
            position: "absolute",
            left: 56,
            top: 0,
            bottom: 0,
            width: 3,
            background: MARGIN_RED,
            opacity: 0.5,
          }}
        />

        {/* ruled hairlines */}
        {[0, 1, 2, 3, 4, 5, 6].map((i) => (
          <div
            key={i}
            style={{
              position: "absolute",
              left: 88,
              right: 88,
              top: 168 + i * 54,
              height: 2,
              background: INK,
              opacity: 0.055,
            }}
          />
        ))}

        {/* header — mark + wordmark */}
        <div style={{ display: "flex", alignItems: "center", gap: 20 }}>
          {/* eslint-disable-next-line @next/next/no-img-element -- data URI for satori */}
          <img src={`data:image/svg+xml,${MARK_SVG}`} width={72} height={72} alt="" />
          <div
            style={{
              fontSize: 44,
              color: INK,
              letterSpacing: -1,
              display: "flex",
            }}
          >
            EduCore
          </div>
        </div>

        {/* headline with highlighter stroke behind line two */}
        <div style={{ display: "flex", flexDirection: "column", gap: 18 }}>
          <div style={{ fontSize: 74, color: INK, display: "flex" }}>
            Learn deliberately.
          </div>
          <div style={{ display: "flex", position: "relative" }}>
            <div
              style={{
                position: "absolute",
                left: -10,
                right: 340,
                bottom: 8,
                height: 34,
                background: AMBER,
                opacity: 0.65,
                transform: "rotate(-1deg)",
                borderRadius: 6,
              }}
            />
            <div style={{ fontSize: 74, color: TEAL, display: "flex" }}>
              Remember forever.
            </div>
          </div>
        </div>

        {/* footer caption */}
        <div
          style={{
            fontSize: 24,
            color: INK,
            opacity: 0.62,
            letterSpacing: 3,
            textTransform: "uppercase",
            display: "flex",
          }}
        >
          Any subject · Structured paths · Verifiable certificates
        </div>
      </div>
    ),
    { ...size }
  );
}
