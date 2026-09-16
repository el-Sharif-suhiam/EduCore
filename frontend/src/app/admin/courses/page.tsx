"use client";

// ============================================================
// /admin/courses — catalog management table.
// Search + load-more paging (API exposes no totals). Rows open
// the course builder. "New course" creates a draft immediately.
// ============================================================

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Plus } from "lucide-react";
import { AdminPageHeader, DataTable, TableEmpty } from "@/components/admin/data-table";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  getCoursesPaged,
  createCourse,
  type CourseSummary,
} from "@/lib/admin";

const PAGE_SIZE = 12;

export default function AdminCoursesPage() {
  const router = useRouter();

  const [rows, setRows] = useState<CourseSummary[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);

  const [newOpen, setNewOpen] = useState(false);
  const searchParams = useSearchParamsShim();

  // Debounced fetch; search edits reset to page 1 via the handler.
  useEffect(() => {
    let cancelled = false;
    const t = setTimeout(async () => {
      try {
        const data = await getCoursesPaged(page, PAGE_SIZE, search.trim());
        if (cancelled) return; // stale response guard
        setRows((prev) => (page === 1 ? data : [...(prev ?? []), ...data]));
        setHasMore(data.length === PAGE_SIZE);
      } catch {
        if (!cancelled) setError("The catalog is unreachable right now. Is the backend running?");
      }
    }, 250);
    return () => {
      cancelled = true;
      clearTimeout(t);
    };
  }, [search, page]);

  return (
    <div className="mx-auto max-w-5xl">
      <AdminPageHeader
        title="Courses"
        description="Everything in the catalog — published or still in draft."
        actions={
          <Button onClick={() => setNewOpen(true)}>
            <Plus data-icon="inline-start" />
            New course
          </Button>
        }
      />

      <div className="mt-6 max-w-sm">
        <Input
          type="search"
          placeholder="Search by title or instructor…"
          aria-label="Search courses"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
      </div>

      <div className="mt-4">
        <DataTable head={["Course", "Instructors", "Price", "Created", ""]} isLoading={rows === null && !error} error={error}>
          {rows !== null && rows.length === 0 && (
            <TableEmpty
              colSpan={5}
              message={search ? "No courses match that search." : "No courses yet — create the first one."}
            />
          )}
          {rows?.map((c) => (
            <tr key={c.id} className="border-b border-border/50 transition-colors last:border-0 hover:bg-accent/40">
              <td className="px-5 py-3.5">
                <Link
                  href={`/admin/courses/${c.id}`}
                  className="font-medium text-foreground underline-offset-4 hover:text-primary hover:underline"
                >
                  {c.title}
                </Link>
                <p className="mt-0.5 line-clamp-1 text-xs text-muted-foreground">
                  {c.summary ?? "—"}
                </p>
              </td>
              <td className="px-5 py-3.5 text-muted-foreground">
                {c.courseInstructors?.map((i) => i.instructorName).join(", ") || "—"}
              </td>
              <td className="px-5 py-3.5 font-mono text-muted-foreground">
                ${c.basePrice.toFixed(2)}
              </td>
              <td className="px-5 py-3.5 text-muted-foreground">
                {new Date(c.createdAt).toLocaleDateString()}
              </td>
              <td className="px-5 py-3.5 text-end">
                <Button variant="ghost" size="sm" asChild>
                  <Link href={`/admin/courses/${c.id}`}>Manage</Link>
                </Button>
              </td>
            </tr>
          ))}
        </DataTable>
      </div>

      {hasMore && rows !== null && (
        <div className="mt-4 text-center">
          <Button variant="outline" onClick={() => setPage((p) => p + 1)}>
            Load more
          </Button>
        </div>
      )}

      {/* deep-link support: /admin/courses?new=1 (command palette) */}
      {(newOpen || searchParams) && (
        <NewCourseModal
          open={newOpen || !!searchParams}
          onClose={() => {
            setNewOpen(false);
            if (searchParams) window.history.replaceState(null, "", "/admin/courses");
          }}
          onCreated={(id) => router.push(`/admin/courses/${id}`)}
        />
      )}
    </div>
  );
}

// Tiny helper so `?new=1` opens the modal without another hook import dance.
function useSearchParamsShim(): boolean {
  const [isNew] = useState(() =>
    typeof window !== "undefined"
      ? new URLSearchParams(window.location.search).get("new") === "1"
      : false
  );
  return isNew;
}

function NewCourseModal({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated: (courseId: number) => void;
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
      setError("Give the course a name and a price above zero.");
      return;
    }

    setBusy(true);
    try {
      const created = await createCourse({
        name: name.trim(),
        basePrice: parsedPrice,
        summary: summary.trim() || undefined,
      });
      onCreated(created.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create the course.");
      setBusy(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="New course"
      description="Creates an unpublished draft you can build out before going live."
    >
      <form onSubmit={submit} className="space-y-4" noValidate>
        <div className="space-y-2">
          <Label htmlFor="nc-name">Name</Label>
          <Input id="nc-name" required value={name} onChange={(e) => setName(e.target.value)} />
        </div>
        <div className="space-y-2">
          <Label htmlFor="nc-price">Base price (USD)</Label>
          <Input
            id="nc-price"
            type="number"
            min="0"
            step="0.01"
            required
            placeholder="49.00"
            value={price}
            onChange={(e) => setPrice(e.target.value)}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="nc-summary">Summary</Label>
          <Input
            id="nc-summary"
            placeholder="One honest sentence about the outcome."
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
