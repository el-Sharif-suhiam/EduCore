// ============================================================
// ============================================================
// ======                                          ============
// ======   PAYMENT GATEWAY: STRIPE  (FRONTEND)    ============
// ======   >>> CHANGE THIS FILE ONLY to switch    ============
// ======   gateway or adjust the flow. <<<        ============
// ======                                          ============
// ============================================================
// ============================================================
//
// FLOW (live end-to-end when backend Stripe keys are set):
//   1. initPayment()            POST /api/payments          → real
//   2. startCheckoutSession()   POST /api/payments/{id}/stripe-checkout-session → real
//   3. Browser redirected to Stripe hosted checkout            → real
//   4. User returns to /cart?checkout=success|cancelled&paymentId={id}
//   5. waitForPaymentSuccess()  polls GET /api/payments/{id}
//   6. SOURCE OF TRUTH = verified webhook on the backend
//      (EduCoreAPI/Controllers/StripeWebhookController.cs), which runs
//      the transactional completion chain. The browser can NEVER mark
//      a payment as succeeded.
//
// Backend files that belong to this integration:
//   EduCore_BusinessLayer/clsStripeGateway.cs      (REST + signatures)
//   EduCoreAPI/Controllers/PaymentsController.cs   (session endpoint)
//   EduCoreAPI/Controllers/StripeWebhookController.cs
//   EduCoreAPI/.env                                (STRIPE_* keys)
// ============================================================

export type PaymentInitResponse = {
  id: number;
  finalPrice: number;
  idempotencyKey: string;
};

export type PaymentStatus =
  | "Pending"
  | "Succeeded"
  | "Failed"
  | "Expired"
  | "Cancelled";

/**
 * Step 1 — create the local payment record.
 * Real endpoint. Idempotency key is generated client-side so retries
 * never duplicate payments; the same key also scopes the Stripe session.
 */
export async function initPayment(
  orderId: number,
  discountCode?: string
): Promise<PaymentInitResponse> {
  const { api } = await import("./api");
  return api.post<PaymentInitResponse>(
    "/api/payments",
    {
      orderId,
      discountCode,
      idempotencyKey: crypto.randomUUID(),
      paymentMethod: "Stripe",
    },
    true
  );
}

// ============================================================
// ====== STRIPE: CHANGE HERE (steps 2–3) =====================
// ============================================================
/**
 * Steps 2–3 — ask OUR backend for a hosted-checkout URL and send
 * the browser there. The page unloads on success; control resumes
 * at the ?checkout=… handler in /cart after Stripe redirects back.
 *
 * To switch provider: replace this call + keep the contract
 * `{ url }` and the rest of the app is untouched.
 */
export async function startCheckoutSession(paymentId: number): Promise<string> {
  const { api } = await import("./api");
  const res = await api.post<{ url: string; sessionId: string }>(
    `/api/payments/${paymentId}/stripe-checkout-session`,
    undefined,
    true
  );

  // Leave the app → Stripe hosted checkout page.
  window.location.href = res.url;
  return res.url;
}
// ============================================================

/**
 * Step 5 — poll payment status after returning from checkout.
 * The webhook is what actually flips status server-side; polling
 * just observes it.
 */
export async function waitForPaymentSuccess(
  paymentId: number,
  { intervalMs = 2000, timeoutMs = 60000 } = {}
): Promise<PaymentStatus> {
  const { api } = await import("./api");
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    try {
      const p = await api.get<{
        status: PaymentStatus;
        isPending: boolean;
        isSucceeded: boolean;
      }>(`/api/payments/${paymentId}`, true);

      if (!p.isPending) return p.status;
    } catch {
      // transient network errors — keep polling until deadline
    }
    await new Promise((r) => setTimeout(r, intervalMs));
  }
  return "Pending";
}
