"use client";

// ============================================================
// /admin/discounts — SuperAdmin only (matches backend roles).
// Valid codes table + create/edit/delete. Deletion is a HARD
// delete and is blocked by the backend once payments reference
// the code — the confirm modal says so plainly.
// ============================================================

import { useCallback, useEffect, useState } from "react";
import { LoaderCircle, Pencil, Trash2 } from "lucide-react";
import { AdminPageHeader, DataTable, TableEmpty } from "@/components/admin/data-table";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useToast } from "@/components/ui/toast";
import { useAuth } from "@/lib/auth-context";
import {
  getValidDiscountCodes,
  createDiscountCode,
  updateDiscountCode,
  deleteDiscountCode,
  type DiscountCode,
} from "@/lib/admin";

type Draft = {
  id: number | null;
  discountCode: string;
  discountRate: string;
  expireAt: string;
  allowedUseNumber: string;
};

const EMPTY: Draft = { id: null, discountCode: "", discountRate: "", expireAt: "", allowedUseNumber: "" };

export default function AdminDiscountsPage() {
  const { user } = useAuth();
  const { toast } = useToast();
  const isSuper = !!user?.roles.includes("SuperAdmin");

  const [rows, setRows] = useState<DiscountCode[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [editing, setEditing] = useState<Draft | null>(null);
  const [deleting, setDeleting] = useState<DiscountCode | null>(null);

  const reload = useCallback(() => {
    getValidDiscountCodes()
      .then(setRows)
      .catch(() => setError("The codes list is unreachable right now."));
  }, []);

  useEffect(() => reload(), [reload]);

  if (!isSuper) {
    return (
      <div className="mx-auto max-w-5xl">
        <AdminPageHeader title="Discount codes" />
        <p role="alert" className="mt-6 rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground">
          Only SuperAdmins manage discount codes.
        </p>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-5xl">
      <AdminPageHeader
        title="Discount codes"
        description="Codes currently valid at checkout. Usage counts update as payments succeed."
        actions={
          <Button onClick={() => setEditing({ ...EMPTY })}>
            New code
          </Button>
        }
      />

      <div className="mt-6">
        <DataTable head={["Code", "Rate", "Expires", "Allowed uses", "Total used", ""]} isLoading={rows === null && !error} error={error}>
          {rows !== null && rows.length === 0 && (
            <TableEmpty colSpan={6} message="No valid codes right now." />
          )}
          {rows?.map((d) => (
            <tr key={d.id} className="border-b border-border/50 last:border-0">
              <td className="px-5 py-3.5 font-mono font-medium">{d.discountCode}</td>
              <td className="px-5 py-3.5 text-muted-foreground">
                {d.discountRate != null ? `${d.discountRate}%` : "—"}
              </td>
              <td className="px-5 py-3.5 text-muted-foreground">
                {d.expireAt ? new Date(d.expireAt).toLocaleDateString() : "Never"}
              </td>
              <td className="px-5 py-3.5 text-muted-foreground">{d.allowedUseNumber ?? "∞"}</td>
              <td className="px-5 py-3.5 text-muted-foreground">{d.totalUsedNumber ?? 0}</td>
              <td className="px-5 py-3.5">
                <div className="flex items-center justify-end gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label={`Edit ${d.discountCode}`}
                    onClick={() =>
                      setEditing({
                        id: d.id,
                        discountCode: d.discountCode,
                        discountRate: d.discountRate?.toString() ?? "",
                        expireAt: d.expireAt ? d.expireAt.slice(0, 10) : "",
                        allowedUseNumber: d.allowedUseNumber?.toString() ?? "",
                      })
                    }
                  >
                    <Pencil className="size-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label={`Delete ${d.discountCode}`}
                    onClick={() => setDeleting(d)}
                  >
                    <Trash2 className="size-4 text-destructive" />
                  </Button>
                </div>
              </td>
            </tr>
          ))}
        </DataTable>
      </div>

      {/* create / edit */}
      {editing !== null && (
        <CodeModal
          key={editing.id ?? "new"}
          open
          draft={editing}
          onClose={() => setEditing(null)}
          onSaved={(id, name) => {
            setEditing(null);
            reload();
            toast({
              title: id === null ? "Code created" : "Code saved",
              description: name,
              variant: "success",
            });
          }}
        />
      )}

      {/* delete */}
      <Modal
        open={deleting !== null}
        onClose={() => setDeleting(null)}
        title={`Delete ${deleting?.discountCode ?? ""}?`}
        description="This is a permanent delete. If any payment already used this code, the backend will refuse — nothing else is affected."
      >
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={() => setDeleting(null)}>
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={async () => {
              if (!deleting) return;
              try {
                await deleteDiscountCode(deleting.id);
                setDeleting(null);
                reload();
                toast({
                  title: "Code deleted",
                  description: deleting.discountCode,
                });
              } catch (err) {
                setDeleting(null);
                setError(err instanceof Error ? err.message : "Delete failed.");
                toast({
                  title: "Could not delete",
                  description: err instanceof Error ? err.message : "Delete failed.",
                  variant: "error",
                });
              }
            }}
          >
            Delete code
          </Button>
        </div>
      </Modal>
    </div>
  );
}

function CodeModal({
  open,
  draft,
  onClose,
  onSaved,
}: {
  open: boolean;
  draft: Draft;
  onClose: () => void;
  onSaved: (id: number | null, name: string) => void;
}) {
  const [form, setForm] = useState<Draft>(draft);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (busy) return;

    const body = {
      discountCode: form.discountCode.trim(),
      discountRate: form.discountRate ? Number(form.discountRate) : undefined,
      expireAt: form.expireAt || undefined,
      allowedUseNumber: form.allowedUseNumber ? Number(form.allowedUseNumber) : undefined,
    };
    if (!body.discountCode) {
      setError("The code itself is required.");
      return;
    }

    setBusy(true);
    setError(null);
    try {
      if (form.id === null) await createDiscountCode(body);
      else await updateDiscountCode(form.id, body);
      onSaved(form.id, body.discountCode);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Save failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={form.id === null ? "New discount code" : `Edit ${draft.discountCode}`}
      description="Leave fields empty for unlimited."
    >
      <form onSubmit={submit} className="space-y-4" noValidate>
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="dc-code">Code</Label>
            <Input
              id="dc-code"
              required
              placeholder="SPRING2027"
              value={form.discountCode}
              onChange={(e) => setForm({ ...form, discountCode: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="dc-rate">Discount rate (%)</Label>
            <Input
              id="dc-rate"
              type="number"
              min="0"
              max="100"
              step="0.01"
              placeholder="15"
              value={form.discountRate}
              onChange={(e) => setForm({ ...form, discountRate: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="dc-expire">Expires</Label>
            <Input
              id="dc-expire"
              type="date"
              value={form.expireAt}
              onChange={(e) => setForm({ ...form, expireAt: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="dc-uses">Allowed uses</Label>
            <Input
              id="dc-uses"
              type="number"
              min="0"
              placeholder="100"
              value={form.allowedUseNumber}
              onChange={(e) => setForm({ ...form, allowedUseNumber: e.target.value })}
            />
          </div>
        </div>

        {error && (
          <p role="alert" className="text-sm leading-relaxed text-destructive">
            {error}
          </p>
        )}

        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" disabled={busy}>
            {busy && <LoaderCircle aria-hidden="true" className="animate-spin" />}
            Save code
          </Button>
        </div>
      </form>
    </Modal>
  );
}
