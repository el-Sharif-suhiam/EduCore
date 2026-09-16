"use client";

import { useState } from "react";
import Link from "next/link";
import { Check, LoaderCircle, ShoppingCart } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useCart } from "@/lib/cart-context";
import { ApiError } from "@/lib/api";

// Add-to-cart for a purchasable product (productId = Products.Id).
// States reflect cart membership; errors surface inline.
export function AddToCartButton({
  productId,
  className,
}: {
  productId: number;
  className?: string;
}) {
  const { cart, addItem, mutating } = useCart();
  const [justAdded, setJustAdded] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const inCart = !!cart?.items?.some((i) => i.productId === productId);

  async function onAdd() {
    setError(null);
    setJustAdded(false);
    try {
      await addItem(productId);
      setJustAdded(true);
      setTimeout(() => setJustAdded(false), 2500);
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "Could not reach the server. Is the backend running?"
      );
    }
  }

  if (inCart) {
    return (
      <Button variant="secondary" size="lg" className={`h-11 text-base ${className ?? ""}`} asChild>
        <Link href="/cart">
          <Check data-icon="inline-start" />
          In your cart
        </Link>
      </Button>
    );
  }

  return (
    <div className={className}>
      <Button
        size="lg"
        className="h-11 w-full text-base"
        onClick={() => void onAdd()}
        disabled={mutating}
      >
        {mutating ? (
          <LoaderCircle data-icon="inline-start" className="animate-spin" />
        ) : (
          <ShoppingCart data-icon="inline-start" />
        )}
        {justAdded ? "Added to cart" : "Add to cart"}
      </Button>
      {error && (
        <p role="alert" className="mt-2 text-sm leading-relaxed text-destructive">
          {error}
        </p>
      )}
    </div>
  );
}
