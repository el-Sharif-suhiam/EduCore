"use client";

// Global error boundary — recovers from client render errors without
// losing the session. Inline, on-brand, honest.

import { useEffect } from "react";
import Link from "next/link";
import { TriangleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-5 px-5 text-center">
      <TriangleAlert aria-hidden="true" className="size-10 text-destructive/70" />
      <div>
        <h1 className="font-display text-4xl font-semibold tracking-tight">
          Something went wrong
        </h1>
        <p className="mx-auto mt-2 max-w-sm text-sm leading-relaxed text-muted-foreground">
          An unexpected error interrupted this page. Your session is still
          intact — try again, and if it persists the logs will have the story.
        </p>
      </div>
      <div className="flex gap-2">
        <Button onClick={reset}>Try again</Button>
        <Button variant="outline" asChild>
          <Link href="/">Back home</Link>
        </Button>
      </div>
    </div>
  );
}