"use client";

// ============================================================
// ADMIN SHELL — sidebar + topbar + command palette.
// Sidebar personality (UiUxDesign §18): one continuous active
// indicator that slides between items (motion layoutId).
// Calm by design — the console is a working surface, not a show.
// ============================================================

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import dynamic from "next/dynamic";
import { usePathname } from "next/navigation";
import {
  BadgePercent,
  BookOpen,
  Box,
  CircleDollarSign,
  Gauge,
  Menu,
  ReceiptText,
  ScrollText,
  ShieldCheck,
  Users,
  X,
} from "lucide-react";
import { motion } from "motion/react";
import { Logo } from "@/components/shared/logo";
import { ThemeToggle } from "@/components/shared/theme-toggle";
import { UserMenu } from "@/components/shared/user-menu";
import { Button } from "@/components/ui/button";
// Palette is only ever opened on demand — split it out of the shell
// bundle so /admin first paint doesn't pay for its (small) JS.
const CommandPalette = dynamic(() =>
  import("@/components/admin/command-palette").then((m) => m.CommandPalette),
  { ssr: false }
);
import { useAuth } from "@/lib/auth-context";
import { useRouter } from "next/navigation";
import { cn } from "@/lib/utils";

const NAV: {
  href: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  match: (p: string) => boolean;
  roles: string[];
  palette: string;
}[] = [
  {
    href: "/admin",
    label: "Dashboard",
    icon: Gauge,
    match: (p: string) => p === "/admin",
    roles: ["Admin", "SuperAdmin", "Instructor"],
    palette: "Go to Dashboard",
  },
  {
    href: "/admin/courses",
    label: "Courses",
    icon: BookOpen,
    match: (p: string) => p.startsWith("/admin/courses"),
    roles: ["Admin", "SuperAdmin", "Instructor"],
    palette: "Go to Courses",
  },
  {
    href: "/admin/bundles",
    label: "Bundles",
    icon: Box,
    match: (p: string) => p.startsWith("/admin/bundles"),
    roles: ["Admin", "SuperAdmin", "Instructor"],
    palette: "Go to Bundles",
  },
  {
    href: "/admin/people",
    label: "People",
    icon: Users,
    match: (p: string) => p.startsWith("/admin/people"),
    roles: ["Admin", "SuperAdmin"],
    palette: "Go to People",
  },
  {
    href: "/admin/orders",
    label: "Orders",
    icon: ReceiptText,
    match: (p: string) => p.startsWith("/admin/orders"),
    roles: ["Admin", "SuperAdmin"],
    palette: "Go to Orders",
  },
  {
    href: "/admin/payments",
    label: "Payments",
    icon: CircleDollarSign,
    match: (p: string) => p.startsWith("/admin/payments"),
    roles: ["Admin", "SuperAdmin"],
    palette: "Go to Payments",
  },
  {
    href: "/admin/discounts",
    label: "Discount codes",
    icon: BadgePercent,
    match: (p: string) => p.startsWith("/admin/discounts"),
    roles: ["SuperAdmin"],
    palette: "Go to Discount codes",
  },
  {
    href: "/admin/audit",
    label: "Audit trail",
    icon: ShieldCheck,
    match: (p: string) => p.startsWith("/admin/audit"),
    roles: ["Admin", "SuperAdmin"],
    palette: "Go to Audit trail",
  },
  {
    href: "/admin/logs",
    label: "System logs",
    icon: ScrollText,
    match: (p: string) => p.startsWith("/admin/logs"),
    roles: ["Admin", "SuperAdmin"],
    palette: "Go to System logs",
  },
];

export function AdminShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { user } = useAuth();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);

  // Global Ctrl/Cmd + K
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        setPaletteOpen((v) => !v);
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  const items = useMemo(
    () => NAV.filter((n) => user?.roles.some((r) => n.roles.includes(r))),
    [user]
  );

  const paletteActions = items.map((n) => ({
    id: `go:${n.href}`,
    label: n.palette,
    hint: n.href === "/admin" ? undefined : "GOTO",
    run: () => router.push(n.href),
  }));

  const activeLabel =
    [...items].reverse().find((n) => n.match(pathname))?.label ?? "Console";

  return (
    <div className="flex min-h-svh">
      {/* ---------- desktop sidebar ---------- */}
      <aside className="fixed inset-y-0 start-0 z-40 hidden w-60 flex-col border-e bg-card/40 lg:flex">
        <div className="flex h-16 items-center border-b px-5">
          <Link href="/" aria-label="EduCore home">
            <Logo />
          </Link>
        </div>
        <SidebarNav items={items} pathname={pathname} />
        <p className="mt-auto border-t px-5 py-4 font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
          EduCore console
        </p>
      </aside>

      {/* ---------- mobile drawer ---------- */}
      {drawerOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div
            aria-hidden="true"
            onClick={() => setDrawerOpen(false)}
            className="animate-in fade-in duration-200 absolute inset-0 bg-foreground/40"
          />
          <aside className="animate-in slide-in-from-left duration-250 absolute inset-y-0 start-0 flex w-64 flex-col border-e bg-card">
            <div className="flex h-16 items-center justify-between border-b px-5">
              <Logo />
              <Button
                variant="ghost"
                size="icon"
                aria-label="Close menu"
                onClick={() => setDrawerOpen(false)}
              >
                <X className="size-5" />
              </Button>
            </div>
            <SidebarNav
              items={items}
              pathname={pathname}
              onNavigate={() => setDrawerOpen(false)}
            />
          </aside>
        </div>
      )}

      {/* ---------- main column ---------- */}
      <div className="flex min-h-svh flex-1 flex-col lg:ps-60">
        <header className="sticky top-0 z-30 flex h-16 items-center gap-2 border-b bg-background/85 px-4 shadow-rest backdrop-blur-md sm:px-6">
          <Button
            variant="ghost"
            size="icon"
            className="lg:hidden"
            aria-label="Open menu"
            onClick={() => setDrawerOpen(true)}
          >
            <Menu className="size-5" />
          </Button>

          <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
            {activeLabel}
          </p>

          <div className="ms-auto flex items-center gap-1.5">
            <Button
              variant="outline"
              size="sm"
              className="hidden gap-2 font-mono text-xs text-muted-foreground sm:inline-flex"
              onClick={() => setPaletteOpen(true)}
              aria-keyshortcuts="Control+K Meta+K"
            >
              Command…
              <kbd className="rounded border bg-secondary px-1 py-0.5 text-[10px]">Ctrl K</kbd>
            </Button>
            <ThemeToggle />
            <UserMenu />
          </div>
        </header>

        <main className="flex-1 px-4 py-8 sm:px-6 lg:px-10">{children}</main>
      </div>

      <CommandPalette
        key={paletteOpen ? "open" : "closed"}
        open={paletteOpen}
        onOpenChange={setPaletteOpen}
        actions={paletteActions}
      />
    </div>
  );
}

function SidebarNav({
  items,
  pathname,
  onNavigate,
}: {
  items: readonly {
    href: string;
    label: string;
    icon: React.ComponentType<{ className?: string }>;
    match: (p: string) => boolean;
  }[];
  pathname: string;
  onNavigate?: () => void;
}) {
  const activeIndex = [...items].reverse().findIndex((n) => n.match(pathname));
  const activeHref = activeIndex === -1 ? null : items[items.length - 1 - activeIndex].href;

  return (
    <nav aria-label="Admin" className="flex flex-col gap-0.5 overflow-y-auto p-3">
      {items.map((item) => {
        const isActive = item.href === activeHref;
        return (
          <Link
            key={item.href}
            href={item.href}
            onClick={onNavigate}
            aria-current={isActive ? "page" : undefined}
            className={cn(
              "relative flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm transition-colors duration-200",
              isActive
                ? "text-primary-foreground"
                : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
            )}
          >
            {isActive && (
              <motion.span
                layoutId="admin-nav-active"
                transition={{ type: "spring", stiffness: 420, damping: 34 }}
                className="absolute inset-0 rounded-lg bg-primary"
              />
            )}
            <item.icon className="relative size-4 shrink-0" />
            <span className="relative font-medium">{item.label}</span>
          </Link>
        );
      })}
    </nav>
  );
}
