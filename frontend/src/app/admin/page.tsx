"use client";

// ============================================================
// /admin DASHBOARD (UiUxDesign §21) — hierarchy over volume.
// The backend exposes no aggregate stats endpoints, so instead
// of fake numbers: fast navigation, the live audit trail, and
// honest pointers. Secondary info is one click away.
// ============================================================

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  ArrowRight,
  BadgePercent,
  BookOpen,
  ScrollText,
  ShieldCheck,
  Users,
} from "lucide-react";
import { useAuth } from "@/lib/auth-context";
import {
  getAudits,
  AUDIT_ACTION_LABELS,
  type AuditEntry,
} from "@/lib/admin";
import { AdminPageHeader } from "@/components/admin/data-table";

const QUICK_LINKS: {
  href: string;
  label: string;
  body: string;
  icon: React.ComponentType<{ className?: string }>;
  roles: string[];
}[] = [
  {
    href: "/admin/courses",
    label: "Courses",
    body: "Create courses, build curricula, publish.",
    icon: BookOpen,
    roles: ["Admin", "SuperAdmin", "Instructor"],
  },
  {
    href: "/admin/people",
    label: "People",
    body: "Students, instructors, admins & roles.",
    icon: Users,
    roles: ["Admin", "SuperAdmin"],
  },
  {
    href: "/admin/discounts",
    label: "Discount codes",
    body: "Create and manage promotional codes.",
    icon: BadgePercent,
    roles: ["SuperAdmin"],
  },
  {
    href: "/admin/audit",
    label: "Audit trail",
    body: "Who did what, across the platform.",
    icon: ShieldCheck,
    roles: ["Admin", "SuperAdmin"],
  },
  {
    href: "/admin/logs",
    label: "System logs",
    body: "Errors and diagnostics from the API.",
    icon: ScrollText,
    roles: ["Admin", "SuperAdmin"],
  },
];

export default function AdminDashboard() {
  const { user } = useAuth();
  const isPrivileged = !!user?.roles.some((r) =>
    ["Admin", "SuperAdmin"].includes(r)
  );

  const links = QUICK_LINKS.filter((l) =>
    user?.roles.some((r) => l.roles.includes(r))
  );

  return (
    <div className="mx-auto max-w-5xl">
      <AdminPageHeader
        title={
          user?.name ? `Welcome back, ${user.name.split(" ")[0]}` : "Console"
        }
        description="The working surface for courses, people, and platform health."
      />

      {/* quick links */}
      <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {links.map((link) => (
          <Link
            key={link.href}
            href={link.href}
            className="index-card group rounded-xl bg-card p-5 ring-1 ring-foreground/10 shadow-rest transition-[transform,box-shadow] duration-200 ease-natural hover:-translate-y-0.5 hover:shadow-lift"
          >
            <div className="flex items-center justify-between">
              <span className="flex size-9 items-center justify-center rounded-lg bg-accent text-accent-foreground">
                <link.icon aria-hidden="true" className="size-4" />
              </span>
              <ArrowRight
                aria-hidden="true"
                className="size-4 text-muted-foreground transition-transform duration-200 group-hover:translate-x-0.5 group-hover:text-primary"
              />
            </div>
            <p className="mt-3 font-display text-lg font-semibold">{link.label}</p>
            <p className="mt-1 text-sm leading-relaxed text-muted-foreground">
              {link.body}
            </p>
          </Link>
        ))}
      </div>

      {/* recent activity — real data */}
      {isPrivileged && (
        <section className="mt-12" aria-labelledby="recent-heading">
          <div className="flex items-end justify-between gap-4">
            <h2 id="recent-heading" className="font-display text-xl font-semibold tracking-tight">
              Recent activity
            </h2>
            <Link
              href="/admin/audit"
              className="text-sm font-medium text-primary underline-offset-4 hover:underline"
            >
              Full trail
            </Link>
          </div>
          <RecentAudit />
        </section>
      )}
    </div>
  );
}

function RecentAudit() {
  const [entries, setEntries] = useState<AuditEntry[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    getAudits(1, 8)
      .then((rows) => !cancelled && setEntries(rows))
      .catch(() => !cancelled && setError("The audit feed is unreachable right now."));
    return () => {
      cancelled = true;
    };
  }, []);

  if (error) {
    return (
      <p role="alert" className="mt-4 rounded-xl border border-dashed p-6 text-sm text-muted-foreground">
        {error}
      </p>
    );
  }

  return (
    <ol className="mt-4 divide-y overflow-hidden rounded-xl bg-card ring-1 ring-foreground/10 shadow-rest">
      {entries === null
        ? [0, 1, 2].map((i) => (
            <li key={i} className="animate-pulse px-5 py-4">
              <div className="h-4 w-2/3 rounded bg-secondary" />
            </li>
          ))
        : entries.length === 0
          ? (
            <li className="px-5 py-10 text-center text-sm text-muted-foreground">
              No recorded activity yet.
            </li>
          )
          : entries.map((e) => (
            <li key={e.id} className="flex flex-wrap items-baseline gap-x-3 gap-y-0.5 px-5 py-3.5">
              <span className="font-mono text-xs text-muted-foreground">
                {new Date(e.doneAt).toLocaleString()}
              </span>
              <Badge>{AUDIT_ACTION_LABELS[e.actionType] ?? `Action ${e.actionType}`}</Badge>
              <span className="min-w-0 truncate text-sm text-muted-foreground">
                {e.description || `${e.entityType} #${e.entityId}`}
              </span>
            </li>
          ))}
    </ol>
  );
}

function Badge({ children }: { children: React.ReactNode }) {
  return (
    <span className="inline-flex shrink-0 items-center rounded-full bg-accent px-2 py-0.5 font-mono text-[11px] uppercase tracking-wide text-accent-foreground">
      {children}
    </span>
  );
}
