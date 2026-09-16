"use client";

// ============================================================
// /admin/orders — the commerce ledger (paged, no totals).
// Search by buyer name/email; row click opens the full order.
// ============================================================

import { useEffect, useState } from "react";
import { AdminPageHeader, DataTable, TableEmpty } from "@/components/admin/data-table";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import {
  getAdminOrders,
  ORDER_STATUS_LABELS,
  type AdminOrder,
} from "@/lib/admin";

const PAGE_SIZE = 20;

export default function AdminOrdersPage() {
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<AdminOrder[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [detail, setDetail] = useState<AdminOrder | null>(null);

  // Debounced fetch; a new search re-reads from page one.
  useEffect(() => {
    let cancelled = false;
    const t = setTimeout(async () => {
      try {
        const data = await getAdminOrders(page, PAGE_SIZE, search.trim());
        if (cancelled) return;
        setRows(data);
      } catch {
        if (!cancelled) {
          setRows([]);
          setError("The orders feed is unreachable right now.");
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
        title="Orders"
        description="Every order placed on the platform, newest first. Search by buyer name or email."
      />

      <div className="mt-6 max-w-sm">
        <Input
          type="search"
          placeholder="Search by name or email…"
          aria-label="Search orders"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
      </div>

      <div className="mt-4">
        <DataTable head={["Order", "Buyer", "Amount", "Status", "Placed"]} isLoading={rows === null && !error} error={error}>
          {rows !== null && rows.length === 0 && (
            <TableEmpty
              colSpan={5}
              message={search ? "No orders match that search." : "No orders yet."}
            />
          )}
          {rows?.map((o) => (
            <tr
              key={o.id}
              tabIndex={0}
              onClick={() => setDetail(o)}
              onKeyDown={(ev) => ev.key === "Enter" && setDetail(o)}
              aria-label={`Order ${o.id} — open details`}
              className="cursor-pointer border-b border-border/50 transition-colors last:border-0 hover:bg-accent/40"
            >
              <td className="px-5 py-3.5 font-mono text-muted-foreground">#{o.id}</td>
              <td className="px-5 py-3.5">
                <p className="font-medium">{o.userName}</p>
                <p className="text-xs text-muted-foreground">{o.userEmail}</p>
              </td>
              <td className="px-5 py-3.5 font-mono">${o.totalPrice.toFixed(2)}</td>
              <td className="px-5 py-3.5">
                <Badge variant={o.status === 1 ? "secondary" : "outline"}>
                  {ORDER_STATUS_LABELS[o.status] ?? `Status ${o.status}`}
                </Badge>
              </td>
              <td className="px-5 py-3.5 text-muted-foreground">
                {new Date(o.createdAt).toLocaleString()}
              </td>
            </tr>
          ))}
        </DataTable>
      </div>

      <Pager page={page} canPrev={canPrev} canNext={canNext} onPage={setPage} />

      <Modal
        open={detail !== null}
        onClose={() => setDetail(null)}
        title={detail ? `Order #${detail.id}` : ""}
        description={detail ? `Placed by ${detail.userName} (${detail.userEmail})` : ""}
      >
        {detail && (
          <dl className="space-y-2 font-mono text-xs text-muted-foreground">
            <Row label="Buyer" value={`${detail.userName} — ${detail.userEmail}`} />
            <Row label="User" value={`#${detail.userId}`} />
            <Row label="Amount" value={`$${detail.totalPrice.toFixed(2)}`} />
            <Row label="Status" value={ORDER_STATUS_LABELS[detail.status] ?? `Status ${detail.status}`} />
            <Row label="Placed" value={new Date(detail.createdAt).toLocaleString()} />
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