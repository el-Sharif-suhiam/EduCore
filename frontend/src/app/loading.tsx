// Global route loading state — a quiet centered indicator.

import { LoaderCircle } from "lucide-react";

export default function Loading() {
  return (
    <div className="flex min-h-svh items-center justify-center">
      <LoaderCircle
        aria-hidden="true"
        className="size-6 animate-spin text-muted-foreground"
      />
      <span className="sr-only">Loading…</span>
    </div>
  );
}