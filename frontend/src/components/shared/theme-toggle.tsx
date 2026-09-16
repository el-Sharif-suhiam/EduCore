"use client";

import { useTheme } from "next-themes";
import { Moon, Sun } from "lucide-react";
import { Button } from "@/components/ui/button";

// Micro-motion tier: quick icon swap via CSS (.dark variant) —
// no mounted-state needed since the html class drives visibility.
export function ThemeToggle() {
  const { resolvedTheme, setTheme } = useTheme();

  return (
    <Button
      variant="ghost"
      size="icon"
      aria-label="Toggle color theme"
      onClick={() => setTheme(resolvedTheme === "dark" ? "light" : "dark")}
    >
      {/* visible in light mode */}
      <Moon className="size-4 transition-transform duration-200 ease-natural dark:hidden" />
      {/* visible in dark mode */}
      <Sun className="hidden size-4 transition-transform duration-200 ease-natural dark:block" />
    </Button>
  );
}
