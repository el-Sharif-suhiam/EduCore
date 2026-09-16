"use client";

// ============================================================
// COMMAND PALETTE (UiUxDesign §23) — Ctrl/Cmd + K.
// Hand-rolled on purpose: zero new dependencies, full keyboard
// support, instant filtering. Actions = navigation across the
// console. Fast, small, and part of the product's character.
// ============================================================

import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import { Search } from "lucide-react";
import { cn } from "@/lib/utils";

type Action = { id: string; label: string; hint?: string; run: () => void };

export function CommandPalette({
  open,
  onOpenChange,
  actions,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  actions: Action[];
}) {
  const router = useRouter();
  const [query, setQuery] = useState("");
  const [index, setIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLUListElement>(null);

  const results = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return actions;
    return actions.filter((a) => a.label.toLowerCase().includes(q));
  }, [actions, query]);

  // State resets come from the parent's `key` remount on toggle,
  // so no reset-effect is needed here.

  const runAt = useCallback(
    (i: number) => {
      const action = results[i];
      if (!action) return;
      onOpenChange(false);
      action.run();
      if (action.id.startsWith("go:")) router.refresh();
    },
    [results, onOpenChange, router]
  );

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Escape") {
      e.preventDefault();
      onOpenChange(false);
    } else if (e.key === "ArrowDown") {
      e.preventDefault();
      setIndex((i) => Math.min(i + 1, results.length - 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setIndex((i) => Math.max(i - 1, 0));
    } else if (e.key === "Enter") {
      e.preventDefault();
      runAt(index);
    }
  };

  // keep active row visible
  useEffect(() => {
    listRef.current
      ?.querySelectorAll("li")
      [index]?.scrollIntoView({ block: "nearest" });
  }, [index]);

  if (!open || typeof document === "undefined") return null;

  return (
    <div className="fixed inset-0 z-[80] flex items-start justify-center px-4 pt-[12vh]">
      <div
        aria-hidden="true"
        onClick={() => onOpenChange(false)}
        className="animate-in fade-in duration-150 absolute inset-0 bg-foreground/40"
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Command palette"
        onKeyDown={onKeyDown}
        className="animate-in fade-in zoom-in-95 duration-200 relative w-full max-w-xl overflow-hidden rounded-xl bg-card ring-1 ring-foreground/10 shadow-pop"
      >
        <div className="flex items-center gap-3 border-b px-4">
          <Search aria-hidden="true" className="size-4 shrink-0 text-muted-foreground" />
          <input
            ref={inputRef}
            autoFocus
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
              setIndex(0);
            }}
            placeholder="Type a command or page…"
            aria-label="Search commands"
            className="h-12 w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground"
          />
          <kbd className="rounded border bg-secondary px-1.5 py-0.5 font-mono text-[10px] text-muted-foreground">
            Esc
          </kbd>
        </div>

        <ul ref={listRef} className="max-h-80 overflow-y-auto p-2">
          {results.length === 0 ? (
            <li className="px-3 py-8 text-center text-sm text-muted-foreground">
              No matching commands.
            </li>
          ) : (
            results.map((a, i) => (
              <li key={a.id}>
                <button
                  type="button"
                  onMouseEnter={() => setIndex(i)}
                  onClick={() => runAt(i)}
                  className={cn(
                    "flex w-full items-center justify-between rounded-lg px-3 py-2.5 text-start text-sm transition-colors",
                    i === index ? "bg-accent text-accent-foreground" : "text-foreground"
                  )}
                >
                  <span>{a.label}</span>
                  {a.hint && (
                    <span className="font-mono text-xs text-muted-foreground">{a.hint}</span>
                  )}
                </button>
              </li>
            ))
          )}
        </ul>

        <div className="flex items-center gap-4 border-t bg-secondary/40 px-4 py-2 font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
          <span>↑↓ navigate</span>
          <span>↵ select</span>
        </div>
      </div>
    </div>
  );
}
