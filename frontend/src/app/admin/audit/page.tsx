"use client";

// ============================================================
// /admin/audit — the accountability trail (paged, no totals).
// Row click opens the full entry (IP / user agent) in a modal.
// ============================================================

import { useEffect, useState } from "react";
import { AdminPageHeader, DataTable, TableEmpty } from "@/components/admin/data-table";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import {
  getAudits,
  AUDIT_ACTION_LABELS,
  type AuditEntry,
} from "@/lib/admin";

const PAGE_SIZE = 20;

export default function AdminAuditPage() {
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<AuditEntry[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [detail, setDetail] = useState<AuditEntry | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const data = await getAudits(page, PAGE_SIZE);
        if (!cancelled) setRows(data);
      } catch {
        // keep prior rows hidden behind the inline error
        if (!cancelled) {
          setRows([]);
          setError("The audit feed is unreachable right now.");
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [page]);

  const canPrev = page > 1;
  const canNext = rows !== null && rows.length === PAGE_SIZE;

  return (
    <div className="mx-auto max-w-5xl">
      <AdminPageHeader
        title="Audit trail"
        description="Every privileged action on the platform, newest first."
      />

      <div className="mt-6">
        <DataTable head={["When", "Action", "Actor", "Entity", ""]} isLoading={rows === null && !error} error={error}>
          {rows !== null && rows.length === 0 && (
            <TableEmpty colSpan={5} message="No audit entries recorded yet." />
          )}
          {rows?.map((e) => (
            <tr
              key={e.id}
              tabIndex={0}
              onClick={() => setDetail(e)}
              onKeyDown={(ev) => ev.key === "Enter" && setDetail(e)}
              aria-label={`Audit entry ${e.id} — open details`}
              className="cursor-pointer border-b border-border/50 transition-colors last:border-0 hover:bg-accent/40"
            >
              <td className="px-5 py-3.5 text-muted-foreground">
                {new Date(e.doneAt).toLocaleString()}
              </td>
              <td className="px-5 py-3.5">
                <span className="inline-flex items-center rounded-full bg-accent px-2 py-0.5 font-mono text-[11px] uppercase tracking-wide text-accent-foreground">
                  {AUDIT_ACTION_LABELS[e.actionType] ?? `Action ${e.actionType}`}
                </span>
              </td>
              <td className="px-5 py-3.5 font-mono text-muted-foreground">#{e.userId}</td>
              <td className="px-5 py-3.5 text-muted-foreground">
                {e.entityType} #{e.entityId}
              </td>
              <td className="max-w-[16rem] truncate px-5 py-3.5 text-muted-foreground">
                {e.description || "—"}
              </td>
            </tr>
          ))}
        </DataTable>
      </div>

      <Pager page={page} canPrev={canPrev} canNext={canNext} onPage={setPage} />

      <Modal
        open={detail !== null}
        onClose={() => setDetail(null)}
        title={detail ? AUDIT_ACTION_LABELS[detail.actionType] ?? `Action ${detail.actionType}` : ""}
        description={detail?.description}
      >
        {detail && (
          <dl className="space-y-2 font-mono text-xs text-muted-foreground">
            <Detail label="Entry" value={`#${detail.id}`} />
            <Detail label="Actor" value={`user #${detail.userId}`} />
            <Detail label="Entity" value={`${detail.entityType} #${detail.entityId}`} />
            <Detail label="When" value={new Date(detail.doneAt).toLocaleString()} />
            <Detail label="IP" value={detail.ipAddress ?? "—"} />
            <Detail label="User agent" value={detail.userAgent ?? "—"} />
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

function Detail({ label, value }: { label: string; value: string }) {
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
