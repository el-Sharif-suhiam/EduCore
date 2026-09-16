// Global 404 — calm, on-brand, with a way back.

import Link from "next/link";
import { ArrowLeft, Compass } from "lucide-react";
import { Button } from "@/components/ui/button";

export default function NotFound() {
  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-5 px-5 text-center">
      <Compass aria-hidden="true" className="size-10 text-primary/60" />
      <div>
        <h1 className="font-display text-4xl font-semibold tracking-tight">
          Page not found
        </h1>
        <p className="mx-auto mt-2 max-w-sm text-sm leading-relaxed text-muted-foreground">
          That page doesn&apos;t exist, or it may have moved. The path is long —
          this turn just wanders off it.
        </p>
      </div>
      <Button asChild>
        <Link href="/">
          <ArrowLeft data-icon="inline-start" />
          Back home
        </Link>
      </Button>
    </div>
  );
}