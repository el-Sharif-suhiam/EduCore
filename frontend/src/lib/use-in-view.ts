"use client";

// Lightweight IntersectionObserver hook used to defer non-critical
// catalog sections (bundles, standalone lessons) until they’re near
// the viewport — zero requests and zero UI cost for content the user
// never scrolls to.

import { useEffect, useRef, useState } from "react";

export function useInView<T extends HTMLElement = HTMLDivElement>(rootMargin = "480px") {
  const ref = useRef<T | null>(null);
  const [inView, setInView] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el || inView) return;

    if (typeof IntersectionObserver === "undefined") {
      // No IO support — degrade to eager loading without a sync setState.
      const t = setTimeout(() => setInView(true), 0);
      return () => clearTimeout(t);
    }

    const io = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting)) {
          setInView(true);
          io.disconnect();
        }
      },
      { rootMargin }
    );
    io.observe(el);
    return () => io.disconnect();
  }, [inView, rootMargin]);

  return { ref, inView };
}