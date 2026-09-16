import Link from "next/link";
import { Logo } from "@/components/shared/logo";
import { Container } from "@/components/shared/container";

const COLUMNS: { title: string; links: { label: string; href: string }[] }[] = [
  {
    title: "Platform",
    links: [
      { label: "Courses", href: "/#courses" },
      { label: "Learning journey", href: "/#journey" },
      { label: "Certificates", href: "/#outcomes" },
    ],
  },
  {
    title: "Learn",
    links: [
      { label: "How it works", href: "/#how-it-works" },
      { label: "Why EduCore", href: "/#what-is" },
      { label: "Sign up", href: "/register" },
    ],
  },
];

export function SiteFooter() {
  return (
    <footer className="relative border-t bg-card/40 paper-grain">
      {/* ruled strip — notebook lines fading out of the footer top */}
      <div
        aria-hidden="true"
        className="ruled-paper pointer-events-none absolute inset-x-0 -top-px h-16 opacity-70 [mask-image:linear-gradient(to_bottom,black,transparent)]"
      />
      <Container className="py-12">
        <div className="grid gap-10 sm:grid-cols-2 lg:grid-cols-4">
          <div className="lg:col-span-2">
            <Logo />
            <p className="mt-4 max-w-xs text-sm leading-relaxed text-muted-foreground">
              A learning platform built around structured paths, honest
              progress, and certificates you can trust.
            </p>
          </div>

          {COLUMNS.map((col) => (
            <nav key={col.title} aria-label={col.title}>
              <h3 className="text-sm font-semibold">{col.title}</h3>
              <ul className="mt-3 space-y-2">
                {col.links.map((link) => (
                  <li key={link.label}>
                    <Link
                      href={link.href}
                      className="text-sm text-muted-foreground transition-colors duration-200 hover:text-foreground"
                    >
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </nav>
          ))}
        </div>

        <div className="mt-10 flex flex-col gap-2 border-t pt-6 text-xs text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
          <p>© {new Date().getFullYear()} EduCore. All rights reserved.</p>
          <p>Built for focus. Designed for the long run.</p>
        </div>
      </Container>
    </footer>
  );
}
