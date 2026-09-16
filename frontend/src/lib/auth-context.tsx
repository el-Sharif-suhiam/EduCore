"use client";

// ============================================================
// AUTH CONTEXT — session state for the whole app.
// ============================================================
// Storage lives in src/lib/api.ts (localStorage). This provider:
//   * restores the session on mount (decodes JWT claims),
//   * refreshes the access token if expired,
//   * fetches the user profile (name) via GET /api/users/{id},
//   * exposes login / register / logout.
//
// Usage: const { status, user, login, register, logout } = useAuth();
// ============================================================

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { apiFetch, clearStoredAuth, getStoredAuth, setStoredAuth, type StoredAuth } from "@/lib/api";
import { decodeJwt, type JwtClaims } from "@/lib/jwt";

export type AuthUser = {
  id: number;
  email: string;
  name: string | null;
  roles: string[];
};

type AuthStatus = "loading" | "authenticated" | "unauthenticated";

type AuthContextValue = {
  status: AuthStatus;
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterInput) => Promise<void>;
  logout: () => Promise<void>;
};

export type RegisterInput = {
  name: string;
  email: string;
  birthDate: string; // ISO yyyy-mm-dd
  password: string;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [user, setUser] = useState<AuthUser | null>(null);

  // Build user state from stored tokens (+ optional profile fetch).
  const hydrateFromStorage = useCallback(async (stored: StoredAuth) => {
    let claims: JwtClaims | null = decodeJwt(stored.accessToken);
    let tokens = stored;

    // Access token expired → try one silent refresh before giving up.
    if (claims?.expiresAtMs && claims.expiresAtMs <= Date.now()) {
      try {
        const fresh = await apiFetch<{ accessToken: string; refreshToken: string }>(
          "/api/auth/refresh",
          { method: "POST", auth: false, body: { email: stored.email, refreshToken: stored.refreshToken } }
        );
        tokens = {
          accessToken: fresh.accessToken,
          refreshToken: fresh.refreshToken,
          email: stored.email,
        };
        setStoredAuth(tokens);
        claims = decodeJwt(tokens.accessToken);
      } catch {
        clearStoredAuth();
        return null;
      }
    }

    if (!claims || !claims.userId) {
      clearStoredAuth();
      return null;
    }

    const baseUser: AuthUser = {
      id: claims.userId,
      email: claims.email,
      name: null,
      roles: claims.roles,
    };

    // Profile gives us the display name. Non-fatal on failure.
    try {
      const profile = await apiFetch<{
        id: number;
        name: string;
        email: string;
        roles: string[];
      }>(`/api/users/${claims.userId}`, { method: "GET", auth: true });
      baseUser.name = profile.name;
    } catch {
      /* keep null name — UI falls back to email */
    }

    return baseUser;
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const stored = getStoredAuth();
      // All state updates below run AFTER awaits — never sync in effect.
      const restored = stored ? await hydrateFromStorage(stored) : null;
      if (cancelled) return;
      setUser(restored);
      setStatus(restored ? "authenticated" : "unauthenticated");
    })();
    return () => {
      cancelled = true;
    };
  }, [hydrateFromStorage]);

  const login = useCallback(
    async (email: string, password: string) => {
      const res = await apiFetch<{ accessToken: string; refreshToken: string }>(
        "/api/auth/login",
        { method: "POST", auth: false, body: { email, password } }
      );

      const stored: StoredAuth = {
        accessToken: res.accessToken,
        refreshToken: res.refreshToken,
        email,
      };
      setStoredAuth(stored);

      const restored = await hydrateFromStorage(stored);
      if (!restored) throw new Error("Login succeeded but the session could not be started.");
      setUser(restored);
      setStatus("authenticated");
    },
    [hydrateFromStorage]
  );

  const register = useCallback(
    async (data: RegisterInput) => {
      // Backend auto-assigns the Student role and returns 201 with basic fields.
      await apiFetch<{ id: number }>("/api/users/students", {
        method: "POST",
        auth: false,
        body: data,
      });
      // Then sign straight in.
      await login(data.email, data.password);
    },
    [login]
  );

  const logout = useCallback(async () => {
    const stored = getStoredAuth();
    if (stored) {
      // Best-effort server-side revocation; local state clears regardless.
      try {
        await apiFetch<string>("/api/auth/logout", {
          method: "POST",
          auth: false,
          body: { email: stored.email, refreshToken: stored.refreshToken },
        });
      } catch {
        /* ignore */
      }
    }
    clearStoredAuth();
    setUser(null);
    setStatus("unauthenticated");
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      isAuthenticated: status === "authenticated",
      login,
      register,
      logout,
    }),
    [status, user, login, register, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}
