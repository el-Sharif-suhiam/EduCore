"use client";

import { LogOut, UserRound, Gauge } from "lucide-react";
import Link from "next/link";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth-context";

// Header identity control. Shows the user's initial; the menu
// exposes console access (privileged roles) and sign-out.
export function UserMenu() {
  const { user, logout } = useAuth();

  const displayName = user?.name ?? user?.email ?? "Account";
  const initial = displayName.charAt(0).toUpperCase();
  const hasConsole =
    !!user && user.roles.some((r) => ["Admin", "SuperAdmin", "Instructor"].includes(r));

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" className="gap-2 px-2" aria-label="Account menu">
          <span
            aria-hidden="true"
            className="flex size-7 items-center justify-center rounded-full bg-primary font-display text-sm font-semibold text-primary-foreground"
          >
            {initial}
          </span>
          <span className="hidden max-w-28 truncate text-sm sm:inline">
            {displayName}
          </span>
        </Button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel className="flex flex-col">
          <span className="truncate">{displayName}</span>
          {user?.name && (
            <span className="truncate text-xs font-normal text-muted-foreground">
              {user.email}
            </span>
          )}
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem asChild>
          <Link href="/profile">
            <UserRound data-icon="inline-start" />
            Profile
          </Link>
        </DropdownMenuItem>
        {hasConsole && (
          <DropdownMenuItem asChild>
            <Link href="/admin">
              <Gauge data-icon="inline-start" />
              Console
            </Link>
          </DropdownMenuItem>
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem
          variant="destructive"
          onSelect={() => void logout()}
        >
          <LogOut data-icon="inline-start" />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
