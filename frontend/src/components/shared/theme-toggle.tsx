"use client";

import { useSyncExternalStore } from "react";
import { useTheme } from "next-themes";
import { Moon, Sun } from "lucide-react";
import { Button } from "@/components/ui/button";

// SSR-safe "has the client mounted?" flag (no setState-in-effect).
const emptySubscribe = () => () => {};
const useIsClient = () =>
  useSyncExternalStore(
    emptySubscribe,
    () => true,
    () => false
  );

// The icon mirrors the ACTUAL applied theme (resolvedTheme), so it can
// never disagree with the page; the client gate keeps SSR output stable
// (no hydration mismatch).
export function ThemeToggle() {
  const { resolvedTheme, setTheme } = useTheme();
  const isMounted = useIsClient();
  const isDark = resolvedTheme === "dark";

  return (
    <Button
      variant="ghost"
      size="icon"
      aria-label={
        isMounted
          ? isDark
            ? "Switch to light theme"
            : "Switch to dark theme"
          : "Toggle color theme"
      }
      onClick={() => setTheme(isDark ? "light" : "dark")}
    >
      {!isMounted ? (
        // SSR/placeholder — fixed size so the header never shifts.
        <span aria-hidden="true" className="size-4" />
      ) : isDark ? (
        <Sun className="size-4" />
      ) : (
        <Moon className="size-4" />
      )}
    </Button>
  );
}