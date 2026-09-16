"use client";

// ============================================================
// /admin/bundles — bundle management table.
// Lists published AND draft bundles (backend: GET /api/bundles/all,
// Instructor/SuperAdmin). Rows open the bundle builder.
// No paging needed — bundles are few; search filters client-side.
// ============================================================

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Plus } from "lucide-react";
import { AdminPageHeader, DataTable, TableEmpty } from "@/components/admin/data-table";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  getAdminBundles,
  createBundle,
  type AdminBundle,
} from "@/lib/admin";

export default function AdminBundlesPage() {
  const router = useRouter();

  const [rows, setRows] = useState<AdminBundle[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [newOpen, setNewOpen] = useState(false);

  useEffect(() => {
    let cancelled = false;
    getAdminBundles()
      .then((data) => !cancelled && setRows(data))
      .catch(() => !cancelled && setError("The bundle list is unreachable right now."));
    return () => {
      cancelled = true;
    };
  }, []);

  const filtered = useMemo(() => {
    if (!rows) return null;
    const q = search.trim().toLowerCase();
    if (!q) return rows;
    return rows.filter(
      (b) => b.name.toLowerCase().includes(q) || (b.summary ?? "").toLowerCase().includes(q)
    );
  }, [rows, search]);

  return (
    <div className="mx-auto max-w-5xl">
      <AdminPageHeader
        title="Bundles"
        description="Grouped learning paths — draft or live."
        actions={
          <Button onClick={() => setNewOpen(true)}>
            <Plus data-icon="inline-start" />
            New bundle
          </Button>
        }
      />

      <div className="mt-6 max-w-sm">
        <Input
          type="search"
          placeholder="Search bundles…"
          aria-label="Search bundles"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <div className="mt-4">
        <DataTable head={["Bundle", "Price", "Status", "Created", ""]} isLoading={rows === null && !error} error={error}>
          {filtered !== null && filtered.length === 0 && (
            <TableEmpty
              colSpan={5}
              message={search ? "No bundles match that search." : "No bundles yet — group your first courses into one."}
            />
          )}
          {filtered?.map((b) => (
            <tr key={b.id} className="border-b border-border/50 transition-colors last:border-0 hover:bg-accent/40">
              <td className="px-5 py-3.5">
                <Link
                  href={`/admin/bundles/${b.id}`}
                  className="font-medium text-foreground underline-offset-4 hover:text-primary hover:underline"
                >
                  {b.name}
                </Link>
                {b.summary && (
                  <p className="mt-0.5 line-clamp-1 text-xs text-muted-foreground">{b.summary}</p>
                )}
              </td>
              <td className="px-5 py-3.5 font-mono text-muted-foreground">
                ${b.basePrice.toFixed(2)}
              </td>
              <td className="px-5 py-3.5">
                {b.isPublished ? (
                  <Badge>Published</Badge>
                ) : (
                  <Badge variant="outline">Draft</Badge>
                )}
              </td>
              <td className="px-5 py-3.5 text-muted-foreground">
                {new Date(b.createdAt).toLocaleDateString()}
              </td>
              <td className="px-5 py-3.5 text-end">
                <Button variant="ghost" size="sm" asChild>
                  <Link href={`/admin/bundles/${b.id}`}>Manage</Link>
                </Button>
              </td>
            </tr>
          ))}
        </DataTable>
      </div>

      {newOpen && (
        <NewBundleModal
          open
          onClose={() => setNewOpen(false)}
          onCreated={(id) => router.push(`/admin/bundles/${id}`)}
        />
      )}
    </div>
  );
}

function NewBundleModal({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated: (bundleId: number) => void;
}) {
  const [name, setName] = useState("");
  const [price, setPrice] = useState("");
  const [summary, setSummary] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (busy) return;
    setError(null);

    const parsedPrice = Number(price);
    if (!name.trim() || !Number.isFinite(parsedPrice) || parsedPrice <= 0) {
      setError("Give the bundle a name and a price above zero.");
      return;
    }

    setBusy(true);
    try {
      const created = await createBundle({
        name: name.trim(),
        basePrice: parsedPrice,
        summary: summary.trim() || undefined,
      });
      onCreated(created.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create the bundle.");
      setBusy(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="New bundle"
      description="Creates an unpublished draft. Add courses to it once it exists."
    >
      <form onSubmit={submit} className="space-y-4" noValidate>
        <div className="space-y-2">
          <Label htmlFor="nb-name">Name</Label>
          <Input id="nb-name" required value={name} onChange={(e) => setName(e.target.value)} />
        </div>
        <div className="space-y-2">
          <Label htmlFor="nb-price">Base price (USD)</Label>
          <Input
            id="nb-price"
            type="number"
            min="0"
            step="0.01"
            required
            placeholder="89.00"
            value={price}
            onChange={(e) => setPrice(e.target.value)}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="nb-summary">Summary</Label>
          <Textarea
            id="nb-summary"
            rows={3}
            placeholder="Why this path, and who it's for."
            value={summary}
            onChange={(e) => setSummary(e.target.value)}
          />
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
            Create draft
          </Button>
        </div>
      </form>
    </Modal>
  );
}