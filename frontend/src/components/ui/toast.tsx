"use client";

// ============================================================
// TOAST — lightweight notification system (no dependencies).
// Provider renders a fixed stack bottom-right; useToast() fires
// toasts from anywhere inside the tree. Auto-dismiss + manual
// close, role="status" for screen readers, reduced-motion safe.
// ============================================================

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
} from "react";
import { CircleCheck, CircleX, Info, X } from "lucide-react";
import { cn } from "@/lib/utils";

type ToastVariant = "default" | "success" | "error";

type ToastItem = {
  id: number;
  title: string;
  description?: string;
  variant: ToastVariant;
};

type ToastInput = {
  title: string;
  description?: string;
  variant?: ToastVariant;
};

type ToastContextValue = {
  toast: (input: ToastInput) => void;
};

const ToastContext = createContext<ToastContextValue | null>(null);

let nextId = 1;

const DEFAULT_DURATION = 4000;

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const dismiss = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const toast = useCallback(
    (input: ToastInput) => {
      const id = nextId++;
      setToasts((prev) => [...prev, { id, ...input, variant: input.variant ?? "default" }]);
      window.setTimeout(() => dismiss(id), DEFAULT_DURATION);
    },
    [dismiss]
  );

  const value = useMemo(() => ({ toast }), [toast]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div
        aria-live="polite"
        className="pointer-events-none fixed inset-x-0 bottom-4 z-[90] flex flex-col items-center gap-2 px-4 sm:items-end"
      >
        {toasts.map((t) => (
          <ToastCard key={t.id} toast={t} onClose={() => dismiss(t.id)} />
        ))}
      </div>
    </ToastContext.Provider>
  );
}

function ToastCard({ toast, onClose }: { toast: ToastItem; onClose: () => void }) {
  const Icon =
    toast.variant === "success"
      ? CircleCheck
      : toast.variant === "error"
        ? CircleX
        : Info;

  return (
    <div
      role="status"
      className="animate-in fade-in slide-in-from-bottom-2 duration-250 pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-xl bg-card p-4 ring-1 ring-foreground/10 shadow-pop"
    >
      <Icon
        aria-hidden="true"
        className={cn(
          "mt-0.5 size-4 shrink-0",
          toast.variant === "success"
            ? "text-success"
            : toast.variant === "error"
              ? "text-destructive"
              : "text-primary"
        )}
      />
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium">{toast.title}</p>
        {toast.description && (
          <p className="mt-0.5 text-sm leading-relaxed text-muted-foreground">
            {toast.description}
          </p>
        )}
      </div>
      <button
        type="button"
        onClick={onClose}
        aria-label="Dismiss"
        className="shrink-0 rounded-md p-1 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
      >
        <X className="size-3.5" />
      </button>
    </div>
  );
}

export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error("useToast must be used inside <ToastProvider>");
  return ctx;
}