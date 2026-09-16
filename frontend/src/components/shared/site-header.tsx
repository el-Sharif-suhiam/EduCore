"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Menu, ShoppingCart, X } from "lucide-react";
import { Logo } from "@/components/shared/logo";
import { ThemeToggle } from "@/components/shared/theme-toggle";
import { UserMenu } from "@/components/shared/user-menu";
import { Button } from "@/components/ui/button";
import { Container } from "@/components/shared/container";
import { useAuth } from "@/lib/auth-context";
import { useCart } from "@/lib/cart-context";
import { cn } from "@/lib/utils";

const NAV_LINKS = [
  { href: "/#what-is", label: "Why EduCore" },
  { href: "/#journey", label: "Learning journey" },
  { href: "/courses", label: "Courses" },
  { href: "/#how-it-works", label: "How it works" },
];

// Header behavior: transparent at top, gains hairline + blur after scroll.
export function SiteHeader() {
  const [scrolled, setScrolled] = useState(false);
  const [open, setOpen] = useState(false);
  const { isAuthenticated, status } = useAuth();
  const { count } = useCart();

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 8);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <header
      className={cn(
        "fixed inset-x-0 top-0 z-50 transition-[background-color,border-color,box-shadow] duration-300 ease-natural",
        scrolled
          ? "border-b bg-background/85 shadow-rest backdrop-blur-md"
          : "border-b border-transparent"
      )}
    >
      <Container>
        <div className="flex h-16 items-center justify-between gap-4">
          <Link href="/" aria-label="EduCore home" onClick={() => setOpen(false)}>
            <Logo />
          </Link>

          {/* Desktop nav — highlighter swipe on hover (study-notes DNA) */}
          <nav className="hidden items-center gap-1 md:flex" aria-label="Main">
            {NAV_LINKS.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className="relative rounded-md px-3 py-2 text-sm text-muted-foreground transition-colors duration-200 hover:text-accent-foreground after:pointer-events-none after:absolute after:inset-x-2 after:bottom-1 after:h-[0.5em] after:-rotate-1 after:origin-left after:scale-x-0 after:rounded-sm after:bg-highlight/50 after:transition-transform after:duration-200 after:ease-natural hover:after:scale-x-100"
              >
                {link.label}
              </Link>
            ))}
          </nav>

          <div className="flex items-center gap-1.5">
            {/* Cart — live count once authenticated */}
            {isAuthenticated ? (
              <Button
                variant="ghost"
                size="icon"
                className="relative"
                aria-label={`Cart, ${count} item${count === 1 ? "" : "s"}`}
                asChild
              >
                <Link href="/cart">
                  <ShoppingCart className="size-4" />
                  {count > 0 && (
                    <span className="absolute -end-0.5 -top-0.5 flex size-4 items-center justify-center rounded-full bg-highlight font-mono text-[10px] font-semibold text-highlight-foreground">
                      {count > 9 ? "9+" : count}
                    </span>
                  )}
                </Link>
              </Button>
            ) : (
              <Button
                variant="ghost"
                size="icon"
                aria-label="Sign in to use the cart"
                disabled={status !== "unauthenticated"}
                asChild={status === "unauthenticated"}
              >
                {status === "unauthenticated" ? (
                  <Link href="/login?next=/cart">
                    <ShoppingCart className="size-4" />
                  </Link>
                ) : (
                  <ShoppingCart className="size-4" />
                )}
              </Button>
            )}

            <ThemeToggle />

            {isAuthenticated ? (
              <UserMenu />
            ) : (
              <>
                {/* Auth — real pages arrive in milestone 2; placeholder pages exist now */}
                <Button variant="ghost" className="hidden sm:inline-flex" asChild>
                  <Link href="/login">Sign in</Link>
                </Button>
                <Button className="hidden sm:inline-flex" asChild>
                  <Link href="/register">Get started</Link>
                </Button>
              </>
            )}

            {/* Mobile menu toggle */}
            <Button
              variant="ghost"
              size="icon"
              className="md:hidden"
              aria-expanded={open}
              aria-controls="mobile-nav"
              aria-label={open ? "Close menu" : "Open menu"}
              onClick={() => setOpen((v) => !v)}
            >
              {open ? <X className="size-5" /> : <Menu className="size-5" />}
            </Button>
          </div>
        </div>

        {/* Mobile panel */}
        {open && (
          <nav
            id="mobile-nav"
            aria-label="Mobile"
            className="flex flex-col gap-1 border-t pb-4 pt-2 md:hidden"
          >
            {NAV_LINKS.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                onClick={() => setOpen(false)}
                className="rounded-md px-3 py-2.5 text-sm text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              >
                {link.label}
              </Link>
            ))}
            <div className="mt-2 flex gap-2 px-3 sm:hidden">
              {isAuthenticated ? (
                <span className="text-sm text-muted-foreground">
                  Signed in — use the account menu to sign out.
                </span>
              ) : (
                <>
                  <Button variant="outline" className="flex-1" asChild>
                    <Link href="/login" onClick={() => setOpen(false)}>
                      Sign in
                    </Link>
                  </Button>
                  <Button className="flex-1" asChild>
                    <Link href="/register" onClick={() => setOpen(false)}>
                      Get started
                    </Link>
                  </Button>
                </>
              )}
            </div>
          </nav>
        )}
      </Container>
    </header>
  );
}
