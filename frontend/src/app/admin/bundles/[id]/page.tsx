"use client";

// ============================================================
// /admin/bundles/[id] — BUNDLE BUILDER.
// Context-preserving like the course builder: edit identity in
// place, toggle publish state, and manage the member courses
// (add from the catalog, remove with confirmation).
// ============================================================

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import {
  ArrowLeft,
  CircleCheck,
  CirclePlus,
  LoaderCircle,
  Trash2,
} from "lucide-react";
import { AdminPageHeader } from "@/components/admin/data-table";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Select } from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { useToast } from "@/components/ui/toast";
import {
  getBundleDetail,
  updateBundle,
  publishBundle,
  unpublishBundle,
  addBundleItem,
  removeBundleItem,
  type BundleDetail,
} from "@/lib/admin";
import { getCoursesPaged, type CourseSummary } from "@/lib/admin";

export default function BundleBuilderPage() {
  const params = useParams<{ id: string }>();
  const bundleId = Number(params.id);
  const invalidId = !Number.isInteger(bundleId) || bundleId <= 0;

  const [bundle, setBundle] = useState<BundleDetail | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (invalidId) return;
    let cancelled = false;
    getBundleDetail(bundleId)
      .then((data) => !cancelled && setBundle(data))
      .catch((err: unknown) => {
        if (cancelled) return;
        if (err instanceof Error && "status" in err && (err as { status?: number }).status === 404) {
          setNotFound(true);
        } else {
          setLoadError("This bundle is unreachable right now.");
        }
      });
    return () => {
      cancelled = true;
    };
  }, [bundleId, invalidId]);

  if (invalidId || notFound) {
    return (
      <div className="mx-auto max-w-5xl">
        <BackLink />
        <p className="mt-6 rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground">
          That bundle doesn&apos;t exist (or was deleted).
        </p>
      </div>
    );
  }

  if (loadError || bundle === null) {
    return (
      <div className="mx-auto max-w-5xl">
        <BackLink />
        <p role="alert" className="mt-6 rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground">
          {loadError ?? "Loading…"}
        </p>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-5xl">
      <BackLink />

      <div className="mt-4">
        <AdminPageHeader
          title={bundle.name}
          description="Bundle identity, publish state, and contents."
          actions={
            <PublishToggle
              bundleId={bundle.id}
              published={bundle.isPublished}
              onChanged={(pub) => setBundle({ ...bundle, isPublished: pub })}
            />
          }
        />
      </div>

      <IdentityCard bundle={bundle} onSaved={(patch) => setBundle({ ...bundle, ...patch })} />

      <ContentsCard
        bundleId={bundle.id}
        courses={bundle.courses}
        onChanged={(courses) => {
          setBundle({ ...bundle, courses });
        }}
      />
    </div>
  );
}

function BackLink() {
  return (
    <Link
      href="/admin/bundles"
      className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
    >
      <ArrowLeft className="size-4" />
      All bundles
    </Link>
  );
}

/* ---------------- publish toggle ---------------- */

function PublishToggle({
  bundleId,
  published,
  onChanged,
}: {
  bundleId: number;
  published: boolean;
  onChanged: (published: boolean) => void;
}) {
  const { toast } = useToast();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function toggle() {
    if (busy) return;
    setBusy(true);
    setError(null);
    try {
      await (published ? unpublishBundle(bundleId) : publishBundle(bundleId));
      onChanged(!published);
      toast({
        title: published ? "Bundle unpublished" : "Bundle published",
        variant: published ? "default" : "success",
      });
    } catch (err) {
      const message = err instanceof Error ? err.message : "Action failed.";
      setError(message);
      toast({ title: "Could not change status", description: message, variant: "error" });
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-col items-end gap-1">
      <div className="flex items-center gap-2">
        {published ? (
          <Badge className="gap-1">
            <CircleCheck data-icon="inline-start" className="size-3.5" />
            Published
          </Badge>
        ) : (
          <Badge variant="outline">Draft</Badge>
        )}
        <Button variant={published ? "outline" : "default"} size="sm" onClick={toggle} disabled={busy}>
          {busy && <LoaderCircle aria-hidden="true" className="animate-spin" />}
          {published ? "Unpublish" : "Publish"}
        </Button>
      </div>
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}

/* ---------------- identity ---------------- */

function IdentityCard({
  bundle,
  onSaved,
}: {
  bundle: BundleDetail;
  onSaved: (patch: Partial<BundleDetail>) => void;
}) {
  const { toast } = useToast();
  const [name, setName] = useState(bundle.name);
  const [basePrice, setBasePrice] = useState(String(bundle.basePrice));
  const [summary, setSummary] = useState(bundle.summary ?? "");
  const [thumbnailUrl, setThumbnailUrl] = useState(bundle.thumbnailUrl ?? "");

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError(null);
    try {
      await updateBundle(bundle.id, {
        name: name.trim(),
        basePrice: Math.max(0, Number(basePrice)),
        summary: summary.trim(),
        thumbnailUrl: thumbnailUrl.trim(),
      });
      onSaved({
        name: name.trim(),
        basePrice: Math.max(0, Number(basePrice)),
        summary: summary.trim() || null,
        thumbnailUrl: thumbnailUrl.trim() || null,
      });
      toast({ title: "Bundle saved", variant: "success" });
    } catch (err) {
      const message = err instanceof Error ? err.message : "Save failed.";
      setError(message);
      toast({ title: "Save failed", description: message, variant: "error" });
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="mt-6" aria-labelledby="bundle-identity-heading">
      <h2 id="bundle-identity-heading" className="font-display text-xl font-semibold tracking-tight">
        Identity
      </h2>
      <form onSubmit={submit} className="mt-3 rounded-xl bg-card p-6 ring-1 ring-foreground/10 shadow-rest">
        <div className="grid gap-4 sm:grid-cols-[1fr_10rem]">
          <div className="space-y-2">
            <Label htmlFor="bb-name">Name</Label>
            <Input id="bb-name" required value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="bb-price">Base price</Label>
            <Input
              id="bb-price"
              type="number"
              min="0"
              step="0.01"
              required
              value={basePrice}
              onChange={(e) => setBasePrice(e.target.value)}
            />
          </div>
        </div>
        <div className="mt-4 space-y-2">
          <Label htmlFor="bb-summary">Summary</Label>
          <Textarea
            id="bb-summary"
            rows={3}
            value={summary}
            onChange={(e) => setSummary(e.target.value)}
          />
        </div>
        <div className="mt-4 space-y-2">
          <Label htmlFor="bb-thumb">Thumbnail URL</Label>
          <Input
            id="bb-thumb"
            placeholder="/covers/my-bundle.jpg"
            value={thumbnailUrl}
            onChange={(e) => setThumbnailUrl(e.target.value)}
          />
        </div>

        <div className="mt-5 flex items-center justify-end gap-3">
          {error && <p className="text-sm text-destructive">{error}</p>}
          <Button type="submit" disabled={busy}>
            Save changes
          </Button>
        </div>
      </form>
    </section>
  );
}

/* ---------------- contents ---------------- */

type CourseRef = { courseId: number; name: string; summary: string | null; thumbnailUrl: string | null };

function ContentsCard({
  bundleId,
  courses,
  onChanged,
}: {
  bundleId: number;
  courses: CourseRef[];
  onChanged: (courses: CourseRef[]) => void;
}) {
  const { toast } = useToast();
  const [pool, setPool] = useState<CourseSummary[] | null>(null);
  const [poolError, setPoolError] = useState(false);
  const [selected, setSelected] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmRemove, setConfirmRemove] = useState<CourseRef | null>(null);

  useEffect(() => {
    getCoursesPaged(1, 100)
      .then(setPool)
      .catch(() => setPoolError(true));
  }, []);

  const selectedCourse = pool?.find((c) => c.id === Number(selected));

  async function add() {
    if (!selectedCourse) return;
    setError(null);
    setBusy(true);
    try {
      await addBundleItem(bundleId, selectedCourse.id);
      onChanged([...courses, {
        courseId: selectedCourse.id,
        name: selectedCourse.title,
        summary: selectedCourse.summary,
        thumbnailUrl: selectedCourse.thumbnailUrl,
      }]);
      setSelected("");
      toast({ title: "Course added", description: selectedCourse.title, variant: "success" });
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to add the course.";
      setError(message);
      toast({ title: "Could not add course", description: message, variant: "error" });
    } finally {
      setBusy(false);
    }
  }

  async function remove(course: CourseRef) {
    setError(null);
    setBusy(true);
    try {
      await removeBundleItem(bundleId, course.courseId);
      onChanged(courses.filter((c) => c.courseId !== course.courseId));
      setConfirmRemove(null);
      toast({ title: "Course removed", description: course.name });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Removal failed.");
    } finally {
      setBusy(false);
    }
  }

  const alreadyAdded = new Set(courses.map((c) => c.courseId));

  return (
    <section className="mt-10" aria-labelledby="bundle-contents-heading">
      <h2 id="bundle-contents-heading" className="font-display text-xl font-semibold tracking-tight">
        Contents
      </h2>
      <p className="mt-1 text-sm text-muted-foreground">
        Courses students get with this bundle.
      </p>

      {error && (
        <p role="alert" className="mt-3 text-sm text-destructive">
          {error}
        </p>
      )}

      <ul className="mt-3 divide-y overflow-hidden rounded-xl bg-card ring-1 ring-foreground/10 shadow-rest">
        {courses.length === 0 ? (
          <li className="px-5 py-8 text-center text-sm text-muted-foreground">
            No courses in this bundle yet.
          </li>
        ) : (
          courses.map((c, i) => (
            <li key={c.courseId} className="flex items-center gap-4 px-5 py-3.5">
              <span className="font-mono text-xs text-muted-foreground">
                {String(i + 1).padStart(2, "0")}
              </span>
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium">{c.name}</p>
                {c.summary && (
                  <p className="truncate text-xs text-muted-foreground">{c.summary}</p>
                )}
              </div>
              <Button
                variant="ghost"
                size="icon"
                aria-label={`Remove ${c.name}`}
                onClick={() => setConfirmRemove(c)}
              >
                <Trash2 className="size-4 text-destructive" />
              </Button>
            </li>
          ))
        )}
      </ul>

      <div className="mt-3 flex flex-wrap items-center gap-2">
        <Select
          aria-label="Choose a course to add"
          value={selected}
          onChange={(e) => setSelected(e.target.value)}
          className="w-full max-w-xs"
        >
          <option value="">Add a course…</option>
          {pool && pool.length > 0 ? (
            pool
              .filter((c) => !alreadyAdded.has(c.id))
              .map((c) => (
                <option key={c.id} value={c.id}>
                  {c.title} — ${c.basePrice.toFixed(2)}
                </option>
              ))
          ) : (
            <option disabled>{poolError ? "Unavailable right now" : "Loading courses…"}</option>
          )}
        </Select>
        <Button variant="outline" size="sm" disabled={!selected || busy} onClick={add}>
          {busy && <LoaderCircle aria-hidden="true" className="animate-spin" />}
          <CirclePlus data-icon="inline-start" />
          Add
        </Button>
      </div>

      <Modal
        open={confirmRemove !== null}
        onClose={() => setConfirmRemove(null)}
        title="Remove course?"
        description={
          confirmRemove
            ? `${confirmRemove.name} will no longer be included in this bundle.`
            : ""
        }
      >
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={() => setConfirmRemove(null)}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={() => confirmRemove && remove(confirmRemove)}>
            Remove
          </Button>
        </div>
      </Modal>
    </section>
  );
}