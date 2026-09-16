// Typed access to order/cart endpoints.
// Shapes mirror backend OrderResponse.cs / DtoOrderItem.cs (camelCase JSON).

export type ProductType = "Lesson" | "Course" | "Bundle";

export type OrderItem = {
  id: number;
  orderId: number;
  productId: number;
  priceAtPurchase: number | null;
  name: string;
  summary: string | null;
  productType: ProductType | string;
};

export type Order = {
  id: number;
  userId: number;
  totalPrice: number;
  status: "Pending" | "Completed" | "Cancelled" | "Empty" | string;
  createdAt: string;
  items: OrderItem[] | null;
};

import { api } from "./api";

/** Create the user's pending cart, or return the existing one. */
export function createOrGetCart(): Promise<Order> {
  return api.post<Order>("/api/orders", undefined, true);
}

/** Pending cart for a user — 404s when none exists (ApiError.status === 404). */
export function getCart(userId: number): Promise<Order> {
  return api.get<Order>(`/api/orders/cart/${userId}`, true);
}

/** Returns the updated order after adding a PRODUCT id to the cart. */
export function addItemToCart(orderId: number, productId: number): Promise<Order> {
  return api.post<Order>(`/api/orders/${orderId}/items/${productId}`, undefined, true);
}

/** Returns the updated order after removing a product from the cart. */
export function removeItemFromCart(orderId: number, productId: number): Promise<Order> {
  return api.del<Order>(`/api/orders/${orderId}/items/${productId}`, true);
}
