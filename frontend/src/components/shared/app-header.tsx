"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { Menu, ShoppingCart, X } from "lucide-react";
import { Logo } from "@/components/shared/logo";
import { ThemeToggle } from "@/components/shared/theme-toggle";
import { UserMenu } from "@/components/shared/user-menu";
import { Button } from "@/components/ui/button";
import { Container } from "@/components/shared/container";
import { useAuth } from "@/lib/auth-context";
import { useCart } from "@/lib/cart-context";
import { cn } from "@/lib/utils";

const NAV = [
  { href: "/courses", label: "Courses" },
  { href: "/learn", label: "My Learning" },
];

// Static (non-fixed) shell header for application pages —
// calmer than the landing header and never overlaps content.
export function AppHeader() {
  const pathname = usePathname();
  const { isAuthenticated, status } = useAuth();
  const { count } = useCart();
  const [open, setOpen] = useState(false);

  return (
    <header className="border-b bg-background">
      <Container>
        <div className="flex h-14 items-center justify-between gap-4">
          <div className="flex items-center gap-6">
            <Link href="/" aria-label="EduCore home">
              <Logo />
            </Link>
            <nav className="hidden items-center gap-1 sm:flex" aria-label="Application">
              {NAV.map((link) => (
                <NavLink key={link.href} link={link} pathname={pathname} isAuthenticated={isAuthenticated} />
              ))}
            </nav>
          </div>

          <div className="flex items-center gap-1.5">
            {isAuthenticated ? (
              <Button variant="ghost" size="icon" className="relative" asChild>
                <Link href="/cart" aria-label={`Cart, ${count} item${count === 1 ? "" : "s"}`}>
                  <ShoppingCart className="size-4" />
                  {count > 0 && (
                    <span className="absolute -end-0.5 -top-0.5 flex size-4 items-center justify-center rounded-full bg-highlight font-mono text-[10px] font-semibold text-highlight-foreground">
                      {count > 9 ? "9+" : count}
                    </span>
                  )}
                </Link>
              </Button>
            ) : (
              status === "unauthenticated" && (
                <>
                  <Button variant="ghost" size="sm" asChild>
                    <Link href="/login">Sign in</Link>
                  </Button>
                  <Button size="sm" className="hidden sm:inline-flex" asChild>
                    <Link href="/register">Get started</Link>
                  </Button>
                </>
              )
            )}

            <ThemeToggle />
            {isAuthenticated && <UserMenu />}

            {/* mobile nav toggle */}
            <Button
              variant="ghost"
              size="icon"
              className="sm:hidden"
              aria-expanded={open}
              aria-controls="app-mobile-nav"
              aria-label={open ? "Close menu" : "Open menu"}
              onClick={() => setOpen((v) => !v)}
            >
              {open ? <X className="size-5" /> : <Menu className="size-5" />}
            </Button>
          </div>
        </div>

        {/* mobile panel */}
        {open && (
          <nav
            id="app-mobile-nav"
            aria-label="Application"
            className="flex flex-col gap-1 border-t pb-4 pt-2 sm:hidden"
          >
            {NAV.map((link) => {
              if (link.href === "/learn" && !isAuthenticated) return null;
              const active =
                pathname === link.href || pathname.startsWith(link.href + "/");
              return (
                <Link
                  key={link.href}
                  href={link.href}
                  onClick={() => setOpen(false)}
                  aria-current={active ? "page" : undefined}
                  className={cn(
                    "rounded-md px-3 py-3 text-base transition-colors duration-200",
                    active
                      ? "bg-accent font-medium text-accent-foreground"
                      : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                  )}
                >
                  {link.label}
                </Link>
              );
            })}
          </nav>
        )}
      </Container>
    </header>
  );
}

function NavLink({
  link,
  pathname,
  isAuthenticated,
}: {
  link: { href: string; label: string };
  pathname: string;
  isAuthenticated: boolean;
}) {
  // My Learning requires auth — hide when signed out.
  if (link.href === "/learn" && !isAuthenticated) return null;
  const active = pathname === link.href || pathname.startsWith(link.href + "/");

  return (
    <Link
      href={link.href}
      aria-current={active ? "page" : undefined}
      className={cn(
        "rounded-md px-3 py-1.5 text-sm transition-colors duration-200",
        active
          ? "bg-accent font-medium text-accent-foreground"
          : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
      )}
    >
      {link.label}
    </Link>
  );
}
