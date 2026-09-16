"use client";

// ============================================================
// /admin/lessons — standalone-lessons console (Admin/SuperAdmin).
// List all independent lessons including drafts, create/edit the
// identity cards, flip publish state, remove from the catalog.
// Backend gating: the feed only includes drafts for privileged
// callers (GET /api/lessons?includeUnpublished=true), so this
// page hides behind the same roles the endpoint trusts.
// ============================================================

import { useEffect, useState } from "react";
import Link from "next/link";
import { ExternalLink, Eye, EyeOff, LoaderCircle, PenLine, Plus, Trash2 } from "lucide-react";
import { AdminPageHeader, DataTable, TableEmpty } from "@/components/admin/data-table";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { useToast } from "@/components/ui/toast";
import { Logo } from "@/components/shared/logo";
import { useAuth } from "@/lib/auth-context";
import {
  createStandaloneLesson,
  deleteLesson,
  getLessonDetail,
  getLessonsPaged,
  publishLesson,
  unpublishLesson,
  updateStandaloneLesson,
  type AdminLessonSummary,
  type LessonInput,
} from "@/lib/admin";

const PAGE_SIZE = 12;

export default function AdminLessonsPage() {
  const { user } = useAuth();
  const { toast } = useToast();

  const isStaff = user?.roles.some((r) => r === "Admin" || r === "SuperAdmin") === true;

  const [rows, setRows] = useState<AdminLessonSummary[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const [busyId, setBusyId] = useState<number | null>(null);
  const [refresh, setRefresh] = useState(0);

  const [newOpen, setNewOpen] = useState(false);
  const [editing, setEditing] = useState<AdminLessonSummary | null>(null);
  const [deleting, setDeleting] = useState<AdminLessonSummary | null>(null);

  // Debounced fetch; search edits reset to page 1 via the handler.
  useEffect(() => {
    if (!isStaff) return;
    let cancelled = false;
    const t = setTimeout(async () => {
      try {
        const data = await getLessonsPaged(page, PAGE_SIZE, search.trim());
        if (cancelled) return; // stale response guard
        setRows((prev) => (page === 1 ? data : [...(prev ?? []), ...data]));
        setHasMore(data.length === PAGE_SIZE);
      } catch {
        if (!cancelled) setError("The lessons feed is unreachable right now. Is the backend running?");
      }
    }, 250);
    return () => {
      cancelled = true;
      clearTimeout(t);
    };
  }, [search, page, refresh, isStaff]);

  async function togglePublish(lesson: AdminLessonSummary) {
    if (busyId !== null) return;
    setBusyId(lesson.id);
    try {
      if (lesson.isPublished) {
        await unpublishLesson(lesson.id);
        toast({ title: "Lesson unpublished", description: `"${lesson.title}" is now a draft.`, variant: "success" });
      } else {
        await publishLesson(lesson.id);
        toast({ title: "Lesson published", description: `"${lesson.title}" is now on sale.`, variant: "success" });
      }
      setRows((prev) =>
        (prev ?? []).map((row) => (row.id === lesson.id ? { ...row, isPublished: !lesson.isPublished } : row))
      );
    } catch (err) {
      toast({
        title: "Could not change publish state",
        description: err instanceof Error ? err.message : "Try again.",
        variant: "error",
      });
    } finally {
      setBusyId(null);
    }
  }

  async function performDelete() {
    if (!deleting) return;
    try {
      await deleteLesson(deleting.id);
      toast({ title: "Lesson removed", description: `"${deleting.title}" left the catalog.`, variant: "success" });
      setRows((prev) => (prev ?? []).filter((row) => row.id !== deleting.id));
      setDeleting(null);
    } catch (err) {
      setDeleting(null);
      toast({
        title: "Could not delete the lesson",
        description: err instanceof Error ? err.message : "Try again.",
        variant: "error",
      });
    }
  }

  if (!isStaff) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-6 px-5 text-center">
        <Logo />
        <div className="max-w-sm">
          <h1 className="font-display text-2xl font-semibold">Not authorized</h1>
          <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
            Managing standalone lessons is reserved for administrators.
          </p>
          <Button variant="outline" className="mt-5" asChild>
            <Link href="/admin/courses">Back to courses</Link>
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-5xl">
      <AdminPageHeader
        title="Lessons"
        description="Standalone lessons — sold one at a time, drafts included."
        actions={
          <Button onClick={() => setNewOpen(true)}>
            <Plus data-icon="inline-start" />
            New standalone lesson
          </Button>
        }
      />

      <div className="max-w-sm">
        <Input
          type="search"
          placeholder="Search by title or instructor…"
          aria-label="Search lessons"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
      </div>

      <div className="mt-4">
        <DataTable head={["Lesson", "Instructor", "Price", "Status", "Created", "Actions"]} isLoading={rows === null && !error} error={error}>
          {rows !== null && rows.length === 0 && (
            <TableEmpty
              colSpan={6}
              message={search ? "No lessons match that search." : "No standalone lessons yet — create the first one."}
            />
          )}
          {rows?.map((lesson) => (
            <tr key={lesson.id} className="border-b border-border/50 transition-colors last:border-0 hover:bg-accent/40">
              <td className="px-5 py-3.5">
                <Link
                  href={`/lessons/${lesson.id}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 font-medium text-foreground underline-offset-4 hover:text-primary hover:underline"
                >
                  {lesson.title}
                  <ExternalLink data-icon="inline-end" className="size-3.5 text-muted-foreground" />
                </Link>
                <p className="mt-0.5 line-clamp-1 text-xs text-muted-foreground">
                  {lesson.summary ?? "—"}
                </p>
              </td>
              <td className="px-5 py-3.5 text-muted-foreground">{lesson.instructorName}</td>
              <td className="px-5 py-3.5 font-mono text-muted-foreground">
                ${lesson.basePrice.toFixed(2)}
              </td>
              <td className="px-5 py-3.5">
                {lesson.isPublished ? (
                  <Badge variant="secondary">Published</Badge>
                ) : (
                  <Badge variant="outline">Draft</Badge>
                )}
              </td>
              <td className="px-5 py-3.5 text-muted-foreground">
                {new Date(lesson.createdAt).toLocaleDateString()}
              </td>
              <td className="px-5 py-3.5 text-end">
                <div className="flex items-center justify-end gap-1">
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={busyId !== null}
                    title={lesson.isPublished ? "Unpublish" : "Publish"}
                    aria-label={lesson.isPublished ? "Unpublish lesson" : "Publish lesson"}
                    onClick={() => togglePublish(lesson)}
                  >
                    {lesson.isPublished ? <EyeOff data-icon="inline-start" /> : <Eye data-icon="inline-start" />}
                    {lesson.isPublished ? "Hide" : "Publish"}
                  </Button>
                  <Button variant="ghost" size="sm" onClick={() => setEditing(lesson)}>
                    <PenLine data-icon="inline-start" />
                    Edit
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-destructive hover:bg-destructive/10 hover:text-destructive"
                    onClick={() => setDeleting(lesson)}
                  >
                    <Trash2 data-icon="inline-start" />
                  </Button>
                </div>
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

      <LessonModal
        open={newOpen}
        editing={null}
        onClose={() => setNewOpen(false)}
        onSaved={() => {
          setNewOpen(false);
          setPage(1);
          setRefresh((n) => n + 1);
        }}
      />
      <LessonModal
        key={editing?.id ?? "none"}
        open={editing !== null}
        editing={editing}
        onClose={() => setEditing(null)}
        onSaved={() => {
          setEditing(null);
          setRefresh((n) => n + 1);
        }}
      />

      <Modal
        open={deleting !== null}
        onClose={() => setDeleting(null)}
        title="Delete lesson"
        description={`"${deleting?.title ?? ""}" will be removed from the catalog. This is a soft delete — it stops being listed but the data is kept on the backend.`}
      >
        <div className="flex justify-end gap-2 pt-1">
          <Button variant="ghost" onClick={() => setDeleting(null)}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={() => void performDelete()}>
            <Trash2 data-icon="inline-start" />
            Delete lesson
          </Button>
        </div>
      </Modal>
    </div>
  );
}

// ------------------------------------------------------------
// LessonModal — shared create/edit form.
// Edit mode lazily fetches the full detail (video/body are not
// part of the list feed) and prefills the form once it arrives.
// ------------------------------------------------------------
function LessonModal({
  open,
  editing,
  onClose,
  onSaved,
}: {
  open: boolean;
  editing: AdminLessonSummary | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { toast } = useToast();
  const [name, setName] = useState("");
  const [title, setTitle] = useState("");
  const [price, setPrice] = useState("");
  const [summary, setSummary] = useState("");
  const [bodyText, setBodyText] = useState("");
  const [videoUrl, setVideoUrl] = useState("");
  const [thumbnailUrl, setThumbnailUrl] = useState("");
  const [loadingDetail, setLoadingDetail] = useState(() => open && editing !== null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Lazy prefill when the edit target is set (component remounts via
  // a key per id, so initial state is derived — no sync setState here).
  useEffect(() => {
    if (!open || !editing) return;
    let cancelled = false;
    getLessonDetail(editing.id)
      .then((detail) => {
        if (cancelled) return;
        setName(detail.name ?? detail.title ?? "");
        setTitle(detail.title ?? "");
        setPrice(detail.basePrice > 0 ? String(detail.basePrice) : "");
        setSummary(detail.summary ?? "");
        setBodyText(detail.bodyText ?? "");
        setVideoUrl(detail.videoUrl ?? "");
        setThumbnailUrl(detail.thumbnailUrl ?? "");
        setLoadingDetail(false);
      })
      .catch((err) => {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : "Could not load this lesson.");
        setLoadingDetail(false);
      });
    return () => {
      cancelled = true;
    };
  }, [open, editing]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (busy || loadingDetail) return;
    setError(null);

    const parsedPrice = Number(price);
    if (!name.trim() || !title.trim() || !Number.isFinite(parsedPrice) || parsedPrice < 0) {
      setError("Name, title and a valid price (0 or above) are required.");
      return;
    }

    const payload: LessonInput = {
      name: name.trim(),
      title: title.trim(),
      basePrice: parsedPrice,
      bodyText: bodyText.trim(),
      summary: summary.trim() || undefined,
      videoUrl: videoUrl.trim() || undefined,
      thumbnailUrl: thumbnailUrl.trim() || undefined,
    };

    setBusy(true);
    try {
      if (editing) {
        await updateStandaloneLesson(editing.id, payload);
        toast({ title: "Lesson updated", description: `"${payload.title}" is saved.`, variant: "success" });
      } else {
        await createStandaloneLesson(payload);
        toast({ title: "Lesson created", description: `"${payload.title}" is ready to publish.`, variant: "success" });
      }
      onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save the lesson.");
      setBusy(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={editing ? "Edit lesson" : "New standalone lesson"}
      description={
        editing
          ? "Edit the identity card and content. Publish state is untouched here."
          : "Creates an unpublished draft. Toggle it live with Publish once it's ready."
      }
      className="max-w-xl"
    >
      {loadingDetail ? (
        <div className="space-y-3" aria-busy="true">
          <Skeleton className="h-9 w-full" />
          <Skeleton className="h-9 w-full" />
          <Skeleton className="h-9 w-full" />
          <Skeleton className="h-28 w-full" />
        </div>
      ) : (
        <form onSubmit={submit} className="space-y-4" noValidate>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="l-name">Name</Label>
              <Input id="l-name" required value={name} onChange={(e) => setName(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="l-title">Title</Label>
              <Input id="l-title" required value={title} onChange={(e) => setTitle(e.target.value)} />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="l-price">Base price (USD)</Label>
            <Input
              id="l-price"
              type="number"
              min="0"
              step="0.01"
              required
              placeholder="9.00"
              value={price}
              onChange={(e) => setPrice(e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="l-summary">Summary</Label>
            <Input
              id="l-summary"
              placeholder="One honest sentence about the outcome."
              value={summary}
              onChange={(e) => setSummary(e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="l-body">Body text</Label>
            <Textarea
              id="l-body"
              rows={5}
              placeholder="The lesson body — shown to owners after purchase."
              value={bodyText}
              onChange={(e) => setBodyText(e.target.value)}
            />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="l-video">Video URL</Label>
              <Input
                id="l-video"
                placeholder="https://youtube.com/…"
                value={videoUrl}
                onChange={(e) => setVideoUrl(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="l-thumb">Thumbnail URL</Label>
              <Input
                id="l-thumb"
                placeholder="https://…/cover.jpg"
                value={thumbnailUrl}
                onChange={(e) => setThumbnailUrl(e.target.value)}
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
            <Button type="submit" disabled={busy || loadingDetail}>
              {busy && <LoaderCircle data-icon="inline-start" className="animate-spin" />}
              {editing ? "Save changes" : "Create draft"}
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}