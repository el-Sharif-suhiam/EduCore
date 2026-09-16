"use client";

// ============================================================
// /admin/payments — money in and out (paged, no totals).
// Search by buyer name/email; row click opens the full payment.
// ============================================================

import { useEffect, useState } from "react";
import { AdminPageHeader, DataTable, TableEmpty } from "@/components/admin/data-table";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import {
  getAdminPayments,
  PAYMENT_STATUS_LABELS,
  type AdminPayment,
} from "@/lib/admin";

const PAGE_SIZE = 20;

export default function AdminPaymentsPage() {
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<AdminPayment[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [detail, setDetail] = useState<AdminPayment | null>(null);

  // Debounced fetch; a new search re-reads from page one.
  useEffect(() => {
    let cancelled = false;
    const t = setTimeout(async () => {
      try {
        const data = await getAdminPayments(page, PAGE_SIZE, search.trim());
        if (cancelled) return;
        setRows(data);
      } catch {
        if (!cancelled) {
          setRows([]);
          setError("The payments feed is unreachable right now.");
        }
      }
    }, 250);
    return () => {
      cancelled = true;
      clearTimeout(t);
    };
  }, [search, page]);

  const canPrev = page > 1;
  const canNext = rows !== null && rows.length === PAGE_SIZE;

  return (
    <div className="mx-auto max-w-5xl">
      <AdminPageHeader
        title="Payments"
        description="Every charge and its outcome, newest first. Search by buyer name or email."
      />

      <div className="mt-6 max-w-sm">
        <Input
          type="search"
          placeholder="Search by name or email…"
          aria-label="Search payments"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
      </div>

      <div className="mt-4">
        <DataTable head={["Payment", "Buyer", "Amount", "Status", "Paid"]} isLoading={rows === null && !error} error={error}>
          {rows !== null && rows.length === 0 && (
            <TableEmpty
              colSpan={5}
              message={search ? "No payments match that search." : "No payments yet."}
            />
          )}
          {rows?.map((p) => (
            <tr
              key={p.id}
              tabIndex={0}
              onClick={() => setDetail(p)}
              onKeyDown={(ev) => ev.key === "Enter" && setDetail(p)}
              aria-label={`Payment ${p.id} — open details`}
              className="cursor-pointer border-b border-border/50 transition-colors last:border-0 hover:bg-accent/40"
            >
              <td className="px-5 py-3.5 font-mono text-muted-foreground">#{p.id}</td>
              <td className="px-5 py-3.5">
                <p className="font-medium">{p.userName}</p>
                <p className="text-xs text-muted-foreground">{p.userEmail}</p>
              </td>
              <td className="px-5 py-3.5 font-mono">${p.finalPrice.toFixed(2)}</td>
              <td className="px-5 py-3.5">
                <Badge variant={p.status === 1 ? "secondary" : "outline"}>
                  {PAYMENT_STATUS_LABELS[p.status] ?? `Status ${p.status}`}
                </Badge>
              </td>
              <td className="px-5 py-3.5 text-muted-foreground">
                {p.paidAt ? new Date(p.paidAt).toLocaleString() : "—"}
              </td>
            </tr>
          ))}
        </DataTable>
      </div>

      <Pager page={page} canPrev={canPrev} canNext={canNext} onPage={setPage} />

      <Modal
        open={detail !== null}
        onClose={() => setDetail(null)}
        title={detail ? `Payment #${detail.id}` : ""}
        description={detail ? `Order #${detail.orderId} by ${detail.userName} (${detail.userEmail})` : ""}
      >
        {detail && (
          <dl className="space-y-2 font-mono text-xs text-muted-foreground">
            <Row label="Order" value={`#${detail.orderId}`} />
            <Row label="Status" value={PAYMENT_STATUS_LABELS[detail.status] ?? `Status ${detail.status}`} />
            <Row label="Price" value={`$${detail.price.toFixed(2)}`} />
            <Row label="Discount" value={detail.discountPrice != null ? `$${detail.discountPrice.toFixed(2)}` : "—"} />
            <Row label="Final" value={`$${detail.finalPrice.toFixed(2)}`} />
            <Row label="Method" value={detail.paymentMethod ?? "—"} />
            <Row label="Created" value={new Date(detail.createdAt).toLocaleString()} />
            <Row label="Paid" value={detail.paidAt ? new Date(detail.paidAt).toLocaleString() : "—"} />
            <Row label="Transaction" value={detail.transactionId ?? "—"} />
          </dl>
        )}
        <div className="mt-5 flex justify-end">
          <Button variant="ghost" onClick={() => setDetail(null)}>
            Close
          </Button>
        </div>
      </Modal>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex gap-3">
      <dt className="w-24 shrink-0 uppercase tracking-widest">{label}</dt>
      <dd className="min-w-0 break-all text-foreground/80">{value}</dd>
    </div>
  );
}

function Pager({
  page,
  canPrev,
  canNext,
  onPage,
}: {
  page: number;
  canPrev: boolean;
  canNext: boolean;
  onPage: (page: number) => void;
}) {
  return (
    <div className="mt-4 flex items-center justify-between">
      <Button variant="outline" size="sm" disabled={!canPrev} onClick={() => onPage(page - 1)}>
        Previous
      </Button>
      <p className="font-mono text-xs text-muted-foreground">Page {page}</p>
      <Button variant="outline" size="sm" disabled={!canNext} onClick={() => onPage(page + 1)}>
        Next
      </Button>
    </div>
  );
}