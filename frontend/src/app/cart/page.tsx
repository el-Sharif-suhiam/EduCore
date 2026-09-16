"use client";

import { Suspense, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import {
  ArrowRight,
  BookOpen,
  Box,
  CircleCheck,
  CircleX,
  Clock,
  LoaderCircle,
  PlayCircle,
  Trash2,
} from "lucide-react";
import { Container } from "@/components/shared/container";
import { AppHeader } from "@/components/shared/app-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuth } from "@/lib/auth-context";
import { useCart } from "@/lib/cart-context";
import { ApiError } from "@/lib/api";
import { waitForPaymentSuccess, type PaymentStatus } from "@/lib/payments";

const price = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
});

function TypeIcon({ type }: { type: string }) {
  if (type === "Bundle") return <Box aria-hidden="true" className="size-4" />;
  if (type === "Lesson") return <PlayCircle aria-hidden="true" className="size-4" />;
  return <BookOpen aria-hidden="true" className="size-4" />;
}

export default function CartPage() {
  // useSearchParams requires a Suspense boundary during prerender.
  return (
    <Suspense fallback={<CartSkeleton />}>
      <CartContent />
    </Suspense>
  );
}

function CartContent() {
  const { status } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const { cart, loading, mutating, removeItem, refresh } = useCart();

  const [discountCode, setDiscountCode] = useState("");
  const [checkingOut, setCheckingOut] = useState(false);
  const [checkoutError, setCheckoutError] = useState<string | null>(null);

  // Banner shown after returning from the hosted checkout page.
  type Banner =
    | { kind: "processing"; paymentId: number }
    | { kind: "success" }
    | { kind: "failed" | "expired" | "timeout"; paymentId?: number }
    | { kind: "cancelled" };
  const [banner, setBanner] = useState<Banner | null>(null);

  // Auth gate.
  useEffect(() => {
    if (status === "unauthenticated") {
      router.replace("/login?next=/cart");
    }
  }, [status, router]);

  // ============================================================
  // Return trip from Stripe hosted checkout:
  //   ?checkout=success&paymentId=N   → poll until webhook lands
  //   ?checkout=cancelled&paymentId=N → cart preserved
  // Runs ONCE on mount; URL is cleaned immediately after reading.
  // ============================================================
  const handledReturnRef = useRef(false);
  useEffect(() => {
    if (handledReturnRef.current) return;
    handledReturnRef.current = true;

    // Async IIFE + microtask boundary: every state update below happens
    // AFTER an await (React-compiler-friendly), never in the sync pass.
    void (async () => {
      await Promise.resolve();

      const checkout = searchParams.get("checkout");
      if (!checkout) return;

      const paymentId = Number(searchParams.get("paymentId"));
      router.replace("/cart", { scroll: false }); // strip query params

      if (checkout === "cancelled") {
        setBanner({ kind: "cancelled" });
        void refresh().catch(() => undefined);
        return;
      }

      if (checkout !== "success" || !Number.isInteger(paymentId) || paymentId <= 0) {
        return;
      }

      setBanner({ kind: "processing", paymentId });
      const result = await waitForPaymentSuccess(paymentId, { timeoutMs: 60000 });
      await refresh().catch(() => undefined);
      switch (result satisfies PaymentStatus) {
        case "Succeeded":
          setBanner({ kind: "success" });
          break;
        case "Failed":
        case "Cancelled":
          setBanner({ kind: "failed", paymentId });
          break;
        case "Expired":
          setBanner({ kind: "expired", paymentId });
          break;
        default:
          setBanner({ kind: "timeout", paymentId });
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- read-once-on-mount semantics are intentional
  }, []);

  // ============================================================
  // ====== PAYMENT GATEWAY: STRIPE =============================
  // ============================================================
  // Checkout: create the payment record (real), then redirect to
  // the hosted checkout page (real). The browser never declares
  // success — the verified webhook on the backend does.
  // Flow lives in src/lib/payments.ts (STRIPE CHANGE HERE file).
  // ============================================================
  async function onCheckout() {
    if (!cart || checkingOut) return;
    setCheckingOut(true);
    setCheckoutError(null);

    try {
      const { initPayment, startCheckoutSession } = await import("@/lib/payments");
      const payment = await initPayment(cart.id, discountCode.trim() || undefined);
      await startCheckoutSession(payment.id); // redirects away on success
    } catch (err) {
      setCheckingOut(false);
      setCheckoutError(
        err instanceof ApiError
          ? err.message
          : "Could not reach the server. Is the backend running?"
      );
    }
    // No finally-reset: successful startCheckoutSession navigates away.
  }

  if (status !== "authenticated") {
    return (
      <div className="flex min-h-svh items-center justify-center">
        <LoaderCircle aria-hidden="true" className="size-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const items = cart?.items ?? [];
  const isEmpty = !loading && items.length === 0;

  return (
    <div className="flex min-h-svh flex-col">
      <AppHeader />
      <main className="flex-1 py-10">
      <Container>
        <header className="max-w-2xl">
          <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
            Your cart
          </p>
          <h1 className="mt-2 font-display text-4xl font-semibold tracking-tight sm:text-5xl">
            Almost yours
          </h1>
        </header>

        {/* post-checkout banner */}
        {banner && (
          <div
            role="status"
            aria-live="polite"
            className={
              banner.kind === "success"
                ? "mt-6 max-w-2xl rounded-xl bg-success/10 p-4 text-sm leading-relaxed ring-1 ring-success/30"
                : banner.kind === "failed"
                  ? "mt-6 max-w-2xl rounded-xl bg-destructive/10 p-4 text-sm leading-relaxed ring-1 ring-destructive/30"
                  : "mt-6 max-w-2xl rounded-xl bg-info/10 p-4 text-sm leading-relaxed ring-1 ring-info/30"
            }
          >
            {banner.kind === "processing" && (
              <p className="flex items-center gap-2">
                <LoaderCircle className="size-4 animate-spin shrink-0" />
                Confirming your payment… this finishes automatically once our
                payment provider confirms it.
              </p>
            )}
            {banner.kind === "success" && (
              <p className="flex items-start gap-2">
                <CircleCheck className="size-4 mt-0.5 shrink-0 text-success" />
                Payment confirmed — you&apos;re enrolled! Your courses now appear in
                your learning area.
              </p>
            )}
            {banner.kind === "cancelled" && (
              <p className="flex items-start gap-2">
                <CircleX className="size-4 mt-0.5 shrink-0 text-muted-foreground" />
                Checkout was cancelled. Nothing was charged and your cart is intact.
              </p>
            )}
            {(banner.kind === "failed" || banner.kind === "expired") && (
              <p className="flex items-start gap-2">
                <CircleX className="size-4 mt-0.5 shrink-0 text-destructive" />
                The payment didn&apos;t go through
                {banner.kind === "expired" ? " (the session expired)" : ""}. You can
                try again whenever you&apos;re ready.
              </p>
            )}
            {banner.kind === "timeout" && (
              <p className="flex items-start gap-2">
                <Clock className="size-4 mt-0.5 shrink-0 text-muted-foreground" />
                Still waiting for final confirmation — refresh this page in a
                moment{banner.paymentId ? ` or check payment #${banner.paymentId}` : ""}.
              </p>
            )}
          </div>
        )}

        {loading ? (
          <CartSkeleton />
        ) : isEmpty ? (
          <div className="mt-10 max-w-lg rounded-xl border border-dashed p-10 text-center">
            <p className="font-display text-xl font-semibold">Your cart is empty</p>
            <p className="mx-auto mt-2 max-w-sm text-sm leading-relaxed text-muted-foreground">
              Browse the catalog and add a course — your path is waiting.
            </p>
            <Button size="lg" className="mt-6 h-10 px-6" asChild>
              <Link href="/courses">
                Explore courses
                <ArrowRight data-icon="inline-end" />
              </Link>
            </Button>
          </div>
        ) : (
          <div className="mt-10 grid gap-10 lg:grid-cols-[1fr_20rem]">
            {/* items */}
            <ul className="divide-y overflow-hidden rounded-xl bg-card ring-1 ring-foreground/10">
              {items.map((item) => (
                <li key={item.id} className="flex items-center gap-4 p-4 sm:p-5">
                  <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-accent text-accent-foreground">
                    <TypeIcon type={String(item.productType)} />
                  </span>

                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium">{item.name}</p>
                    <div className="mt-1 flex items-center gap-2">
                      <Badge variant="secondary" className="capitalize">
                        {String(item.productType).toLowerCase()}
                      </Badge>
                      {item.summary && (
                        <p className="hidden truncate text-xs text-muted-foreground sm:block">
                          {item.summary}
                        </p>
                      )}
                    </div>
                  </div>

                  <p className="shrink-0 font-mono text-sm font-medium">
                    {price.format(item.priceAtPurchase ?? 0)}
                  </p>

                  <Button
                    variant="ghost"
                    size="icon-sm"
                    aria-label={`Remove ${item.name} from cart`}
                    disabled={mutating}
                    onClick={() => void removeItem(item.productId).catch(() => undefined)}
                  >
                    <Trash2 className="text-muted-foreground" />
                  </Button>
                </li>
              ))}
            </ul>

            {/* summary rail */}
            <aside className="lg:sticky lg:top-24 lg:self-start">
              <div className="rounded-xl bg-card p-6 ring-1 ring-foreground/10 shadow-rest">
                <h2 className="font-display text-lg font-semibold">Summary</h2>

                <dl className="mt-4 space-y-2 text-sm">
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">Items</dt>
                    <dd>{items.length}</dd>
                  </div>
                  <div className="flex justify-between border-t pt-2 text-base font-semibold">
                    <dt>Total</dt>
                    <dd className="font-mono">{price.format(cart?.totalPrice ?? 0)}</dd>
                  </div>
                </dl>

                <div className="mt-5 space-y-2">
                  <Label htmlFor="discount">Discount code</Label>
                  <Input
                    id="discount"
                    placeholder="e.g. WELCOME10"
                    value={discountCode}
                    onChange={(e) => setDiscountCode(e.target.value)}
                    className="uppercase"
                  />
                  <p className="text-xs text-muted-foreground">
                    Validated when the payment record is created.
                  </p>
                </div>

                <Button
                  size="lg"
                  className="mt-5 h-11 w-full text-base"
                  onClick={() => void onCheckout()}
                  disabled={checkingOut || mutating || banner?.kind === "processing"}
                >
                  {checkingOut && (
                    <LoaderCircle data-icon="inline-start" className="animate-spin" />
                  )}
                  Proceed to checkout
                </Button>

                {checkoutError && (
                  <p role="alert" className="mt-3 text-sm leading-relaxed text-destructive">
                    {checkoutError}
                  </p>
                )}

                <p className="mt-4 text-xs leading-relaxed text-muted-foreground">
                  Secure checkout via Stripe. Enrollment activates automatically
                  the moment your payment succeeds.
                </p>
              </div>
            </aside>
          </div>
        )}
      </Container>
      </main>
    </div>
  );
}

function CartSkeleton() {
  return (
    <div className="mt-10 max-w-2xl space-y-3" aria-busy="true">
      {[0, 1].map((i) => (
        <Skeleton key={i} className="h-20 w-full rounded-xl" />
      ))}
    </div>
  );
}
