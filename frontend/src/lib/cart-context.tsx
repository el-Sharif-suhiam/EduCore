"use client";

// ============================================================
// CART CONTEXT — the pending order (cart) for the signed-in user.
// ============================================================
// Loads on authentication, exposes add/remove/refresh and a
// derived item count for the header badge. All HTTP goes through
// src/lib/orders.ts (native fetch only).
//
// React 19 / Compiler notes:
//   * No synchronous setState inside effects — the loader runs as
//     an async IIFE and every state update happens after an await.
//   * Cart is cleared by RENDER-TIME DERIVATION when signed out
//     (no effect needed).
//   * Manual memo deps always match what the functions actually
//     read (full objects, no `x?.id` shortcuts).
// ============================================================

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { ApiError } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import {
  addItemToCart,
  createOrGetCart,
  getCart,
  removeItemFromCart,
  type Order,
} from "@/lib/orders";

type CartContextValue = {
  cart: Order | null;
  /** number of distinct products in the cart */
  count: number;
  loading: boolean;
  /** true while an add/remove request is in flight */
  mutating: boolean;
  refresh: () => Promise<void>;
  addItem: (productId: number) => Promise<void>;
  removeItem: (productId: number) => Promise<void>;
};

const CartContext = createContext<CartContextValue | null>(null);

export function CartProvider({ children }: { children: React.ReactNode }) {
  const { status, user } = useAuth();
  const [internalCart, setInternalCart] = useState<Order | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [mutating, setMutating] = useState(false);

  // Reload whenever the signed-in identity changes.
  useEffect(() => {
    if (status !== "authenticated" || !user) return;

    let cancelled = false;
    (async () => {
      try {
        const pending = await getCart(user.id).catch((err) => {
          // No pending cart yet is normal — not an error state.
          if (err instanceof ApiError && err.status === 404) return null;
          throw err;
        });
        if (cancelled) return;
        setInternalCart(pending);
      } catch {
        if (!cancelled) setInternalCart(null);
      } finally {
        if (!cancelled) setLoaded(true);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [status, user]);

  const refresh = useCallback(async (): Promise<void> => {
    if (!user) return;
    const pending = await getCart(user.id).catch((err) => {
      if (err instanceof ApiError && err.status === 404) return null;
      throw err;
    });
    setInternalCart(pending);
  }, [user]);

  const addItem = useCallback(
    async (productId: number): Promise<void> => {
      setMutating(true);
      try {
        let target = internalCart;
        if (!target || target.status !== "Pending") {
          target = await createOrGetCart();
          setInternalCart(target);
        }
        const updated = await addItemToCart(target.id, productId);
        setInternalCart(updated);
      } finally {
        setMutating(false);
      }
    },
    [internalCart]
  );

  const removeItem = useCallback(
    async (productId: number): Promise<void> => {
      if (!internalCart) return;
      setMutating(true);
      try {
        // Optimistic removal, then reconcile with the server response.
        setInternalCart({
          ...internalCart,
          items:
            internalCart.items?.filter((i) => i.productId !== productId) ?? [],
        });
        const updated = await removeItemFromCart(internalCart.id, productId);
        setInternalCart(updated);
      } catch (err) {
        // Roll back to server truth on failure.
        await refresh().catch(() => undefined);
        throw err;
      } finally {
        setMutating(false);
      }
    },
    [internalCart, refresh]
  );

  // Signed out ⇒ no cart, derived at render time (no effect).
  const cart = status === "authenticated" ? internalCart : null;
  const loading = status === "authenticated" && !loaded;
  const count = cart?.items?.length ?? 0;

  const value = useMemo<CartContextValue>(
    () => ({ cart, count, loading, mutating, refresh, addItem, removeItem }),
    [cart, count, loading, mutating, refresh, addItem, removeItem]
  );

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart(): CartContextValue {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error("useCart must be used inside <CartProvider>");
  return ctx;
}
