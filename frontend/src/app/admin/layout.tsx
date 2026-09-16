"use client";

// ============================================================
// /admin LAYOUT — role gate + console shell.
// Admin/SuperAdmin see everything; Instructors get Courses and
// their own standalone Lessons (matching backend [Authorize(Roles)]
// and the ownership-scoped course/lesson feeds). Unauthorized users
// get an explanatory screen, never a blank redirect.
// ============================================================

import { useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { LoaderCircle } from "lucide-react";
import { AdminShell } from "@/components/admin/admin-shell";
import { Button } from "@/components/ui/button";
import { Logo } from "@/components/shared/logo";
import { useAuth } from "@/lib/auth-context";

const ALLOWED = ["Admin", "SuperAdmin", "Instructor"];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const { status, user } = useAuth();
  const router = useRouter();

  const allowed = !!user && user.roles.some((r) => ALLOWED.includes(r));

  useEffect(() => {
    if (status === "unauthenticated") router.replace("/login?next=/admin");
  }, [status, router]);

  if (status !== "authenticated" || !allowed) {
    return (
      <div className="flex min-h-svh flex-col items-center justify-center gap-6 px-5">
        <Link href="/" aria-label="EduCore home">
          <Logo />
        </Link>
        {status === "loading" || status === "unauthenticated" ? (
          <LoaderCircle aria-hidden="true" className="size-6 animate-spin text-muted-foreground" />
        ) : (
          <div className="max-w-sm text-center">
            <h1 className="font-display text-2xl font-semibold">Not authorized</h1>
            <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
              The console is available to instructors and administrators. Your
              account doesn&apos;t have one of those roles.
            </p>
            <Button variant="outline" className="mt-5" asChild>
              <Link href="/">Back to site</Link>
            </Button>
          </div>
        )}
      </div>
    );
  }

  return <AdminShell>{children}</AdminShell>;
}
