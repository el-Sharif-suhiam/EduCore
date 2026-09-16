"use client";

// ============================================================
// /profile — account settings (UiUxDesign: calm app surface).
// Two cards: identity details and password change.
//
// Email changes sign the user out afterwards ON PURPOSE: the
// refresh-token flow is keyed to the stored email, so keeping
// the old session would break silent refresh. A clean re-login
// is the honest, safe path.
// ============================================================

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, LoaderCircle, KeyRound } from "lucide-react";
import { AppHeader } from "@/components/shared/app-header";
import { Container } from "@/components/shared/container";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/lib/auth-context";
import {
  updateProfile,
  changePassword,
  type ProfileResponse,
} from "@/lib/profile";

export default function ProfilePage() {
  const { status, user } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (status === "unauthenticated") router.replace("/login?next=/profile");
  }, [status, router]);

  if (status !== "authenticated" || !user) {
    return (
      <div className="flex min-h-svh flex-col">
        <AppHeader />
        <div className="flex flex-1 items-center justify-center">
          <LoaderCircle aria-hidden="true" className="size-6 animate-spin text-muted-foreground" />
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-svh flex-col">
      <AppHeader />
      <main className="flex-1 py-10">
        <Container>
          <div className="mx-auto max-w-2xl">
            <Link
              href="/learn"
              className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
            >
              <ArrowLeft className="size-4" />
              My learning
            </Link>

            <h1 className="mt-4 font-display text-3xl font-semibold tracking-tight sm:text-4xl">
              Account settings
            </h1>

            <DetailsCard />
            <PasswordCard />

            <p className="mt-8 rounded-xl border border-dashed p-5 text-xs leading-relaxed text-muted-foreground">
              Birth date is used for account recovery verification only — it is
              never shown publicly.
            </p>
          </div>
        </Container>
        <div className="h-16" />
      </main>
    </div>
  );
}

function DetailsCard() {
  const { user } = useAuth();
  const router = useRouter();

  const [name, setName] = useState(user?.name ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [birthDate, setBirthDate] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load the authoritative record (JWT claims don't carry birthDate).
  useEffect(() => {
    let cancelled = false;
    if (!user) return;
    import("@/lib/api").then(({ api }) =>
      api
        .get<ProfileResponse>(`/api/users/${user.id}`)
        .then((p) => {
          if (cancelled) return;
          setName(p.name ?? "");
          setEmail(p.email ?? "");
          setBirthDate(p.birthDate ? p.birthDate.slice(0, 10) : "");
          setLoading(false);
        })
        .catch(() => !cancelled && setLoading(false))
    );
    return () => {
      cancelled = true;
    };
  }, [user]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (busy || !user) return;
    setError(null);
    setSaved(false);

    if (!name.trim() || !email.trim()) {
      setError("Name and email are required.");
      return;
    }

    setBusy(true);
    try {
      await updateProfile(user.id, {
        name: name.trim(),
        email: email.trim(),
        birthDate: birthDate || undefined,
      });

      if (email.trim().toLowerCase() !== user.email.toLowerCase()) {
        // Email changed → session is keyed to the old address; re-login cleanly.
        router.replace("/login?next=/profile&reauth=1");
        return;
      }
      setSaved(true);
      setTimeout(() => setSaved(false), 2500);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Save failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="mt-6" aria-labelledby="details-heading">
      <h2 id="details-heading" className="font-display text-xl font-semibold tracking-tight">
        Profile details
      </h2>
      <form onSubmit={submit} className="mt-3 rounded-xl bg-card p-6 ring-1 ring-foreground/10 shadow-rest">
        {loading ? (
          <p className="text-sm text-muted-foreground">Loading…</p>
        ) : (
          <>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="pf-name">Full name</Label>
                <Input id="pf-name" required value={name} onChange={(e) => setName(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="pf-birth">Birth date</Label>
                <Input id="pf-birth" type="date" value={birthDate} onChange={(e) => setBirthDate(e.target.value)} />
              </div>
            </div>
            <div className="mt-4 space-y-2">
              <Label htmlFor="pf-email">Email</Label>
              <Input
                id="pf-email"
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
              <p className="text-xs text-muted-foreground">
                Changing your email signs you out — sign back in with the new address.
              </p>
            </div>

            {error && (
              <p role="alert" className="mt-4 text-sm leading-relaxed text-destructive">
                {error}
              </p>
            )}

            <div className="mt-5 flex items-center justify-end gap-3">
              {saved && (
                <p role="status" className="text-sm text-success">
                  Saved.
                </p>
              )}
              <Button type="submit" disabled={busy}>
                {busy && <LoaderCircle data-icon="inline-start" className="animate-spin" />}
                Save changes
              </Button>
            </div>
          </>
        )}
      </form>
    </section>
  );
}

function PasswordCard() {
  const { user } = useAuth();

  const [oldPassword, setOldPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [busy, setBusy] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (busy || !user) return;
    setError(null);
    setSaved(false);

    if (newPassword.length < 6) {
      setError("New password must be at least 6 characters.");
      return;
    }
    if (newPassword !== confirm) {
      setError("New passwords do not match.");
      return;
    }

    setBusy(true);
    try {
      await changePassword(user.id, oldPassword, newPassword);
      setSaved(true);
      setOldPassword("");
      setNewPassword("");
      setConfirm("");
      setTimeout(() => setSaved(false), 2500);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not change the password.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="mt-8" aria-labelledby="password-heading">
      <h2 id="password-heading" className="font-display text-xl font-semibold tracking-tight">
        Password
      </h2>
      <form onSubmit={submit} className="mt-3 rounded-xl bg-card p-6 ring-1 ring-foreground/10 shadow-rest">
        <div className="grid gap-4 sm:grid-cols-3">
          <div className="space-y-2">
            <Label htmlFor="pw-old">Current</Label>
            <Input
              id="pw-old"
              type="password"
              autoComplete="current-password"
              required
              value={oldPassword}
              onChange={(e) => setOldPassword(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="pw-new">New</Label>
            <Input
              id="pw-new"
              type="password"
              autoComplete="new-password"
              required
              minLength={6}
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="pw-confirm">Confirm new</Label>
            <Input
              id="pw-confirm"
              type="password"
              autoComplete="new-password"
              required
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
            />
          </div>
        </div>

        {error && (
          <p role="alert" className="mt-4 text-sm leading-relaxed text-destructive">
            {error}
          </p>
        )}

        <div className="mt-5 flex items-center justify-end gap-3">
          {saved && (
            <p role="status" className="flex items-center gap-1.5 text-sm text-success">
              <KeyRound className="size-4" /> Password updated.
            </p>
          )}
          <Button type="submit" variant="outline" disabled={busy}>
            {busy && <LoaderCircle data-icon="inline-start" className="animate-spin" />}
            Update password
          </Button>
        </div>
      </form>
    </section>
  );
}
