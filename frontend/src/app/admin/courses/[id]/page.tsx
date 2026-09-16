"use client";

// ============================================================
// /admin/courses/[id] — COURSE BUILDER (UiUxDesign §24–25).
// Three stacked contexts, one page — no modal chains:
//   1. Identity   (name/price/summary/images + publish state)
//   2. Curriculum (lessons: add, edit inline, ordered list)
//   3. Instructors (Admin/SuperAdmin only — backend rule)
// Context-preserving: everything edits in place; saves never
// navigate away.
// ============================================================

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { ArrowLeft, CircleCheck, CirclePlus, LoaderCircle, Pencil, Trash2 } from "lucide-react";
import { AdminPageHeader } from "@/components/admin/data-table";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { useAuth } from "@/lib/auth-context";
import {
  getCourse,
  getCourseLessons,
  type CourseDetail,
  type CourseLesson,
} from "@/lib/courses";
import {
  updateCourse,
  publishCourse,
  unpublishCourse,
  addLessonToCourse,
  updateCourseLesson,
  getLessonDetail,
  getCourseInstructors,
  assignCourseInstructor,
  removeCourseInstructor,
  getUsers,
  type CourseInstructorRef,
  type AdminUser,
} from "@/lib/admin";

export default function CourseBuilderPage() {
  const params = useParams<{ id: string }>();
  const courseId = Number(params.id);
  const invalidId = !Number.isInteger(courseId) || courseId <= 0;

  const { user } = useAuth();
  const isAdminLevel = !!user?.roles.some((r) => ["Admin", "SuperAdmin"].includes(r));

  const [course, setCourse] = useState<CourseDetail | null>(null);
  const [lessons, setLessons] = useState<CourseLesson[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (invalidId) return;
    let cancelled = false;
    Promise.all([getCourse(courseId), getCourseLessons(courseId)])
      .then(([c, l]) => {
        if (cancelled) return;
        setCourse(c);
        setLessons(l);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        if (err instanceof Error && "status" in err && (err as { status?: number }).status === 404) {
          setNotFound(true);
        } else {
          setLoadError("This course is unreachable right now.");
        }
      });
    return () => {
      cancelled = true;
    };
  }, [courseId, invalidId]);

  if (invalidId || notFound) {
    return (
      <div className="mx-auto max-w-5xl">
        <BackLink />
        <p className="mt-6 rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground">
          That course doesn&apos;t exist (or was deleted).
        </p>
      </div>
    );
  }

  if (loadError || course === null) {
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
          title={course.name}
          description="Changes save in place — you never lose your context."
          actions={
            <PublishToggle
              courseId={course.id}
              published={course.isPublished}
              onChanged={(pub) => setCourse({ ...course, isPublished: pub })}
            />
          }
        />
      </div>

      <IdentityCard course={course} onSaved={(patch) => setCourse({ ...course, ...patch })} />

      <CurriculumCard
        courseId={course.id}
        lessons={lessons ?? []}
        isLoading={lessons === null}
        onChanged={setLessons}
      />

      {isAdminLevel && <InstructorsCard courseId={course.id} />}
    </div>
  );
}

function BackLink() {
  return (
    <Link
      href="/admin/courses"
      className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
    >
      <ArrowLeft className="size-4" />
      All courses
    </Link>
  );
}

/* ---------------- identity ---------------- */

function PublishToggle({
  courseId,
  published,
  onChanged,
}: {
  courseId: number;
  published: boolean;
  onChanged: (published: boolean) => void;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function toggle() {
    if (busy) return;
    setBusy(true);
    setError(null);
    try {
      await (published ? unpublishCourse(courseId) : publishCourse(courseId));
      onChanged(!published);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Action failed.");
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

function IdentityCard({
  course,
  onSaved,
}: {
  course: CourseDetail;
  onSaved: (patch: Partial<CourseDetail>) => void;
}) {
  const [name, setName] = useState(course.name);
  const [basePrice, setBasePrice] = useState(String(course.basePrice));
  const [summary, setSummary] = useState(course.summary ?? "");
  const [thumbnailUrl, setThumbnailUrl] = useState(course.thumbnailUrl ?? "");
  const [coverImageUrl, setCoverImageUrl] = useState(course.coverImageUrl ?? "");

  const [busy, setBusy] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError(null);
    setSaved(false);
    try {
      await updateCourse(course.id, {
        name: name.trim(),
        basePrice: Math.max(0, Number(basePrice)),
        summary: summary.trim(),
        thumbnailUrl: thumbnailUrl.trim(),
        coverImageUrl: coverImageUrl.trim(),
      });
      onSaved({
        name: name.trim(),
        basePrice: Math.max(0, Number(basePrice)),
        summary: summary.trim() || null,
        thumbnailUrl: thumbnailUrl.trim() || null,
        coverImageUrl: coverImageUrl.trim() || null,
      });
      setSaved(true);
      setTimeout(() => setSaved(false), 2500);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Save failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="mt-6" aria-labelledby="identity-heading">
      <h2 id="identity-heading" className="font-display text-xl font-semibold tracking-tight">
        Identity
      </h2>
      <form onSubmit={submit} className="mt-3 rounded-xl bg-card p-6 ring-1 ring-foreground/10 shadow-rest">
        <div className="grid gap-4 sm:grid-cols-[1fr_10rem]">
          <div className="space-y-2">
            <Label htmlFor="cb-name">Name</Label>
            <Input id="cb-name" required value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="cb-price">Base price</Label>
            <Input
              id="cb-price"
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
          <Label htmlFor="cb-summary">Summary</Label>
          <Input id="cb-summary" value={summary} onChange={(e) => setSummary(e.target.value)} />
        </div>
        <div className="mt-4 grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="cb-thumb">Thumbnail URL</Label>
            <Input id="cb-thumb" placeholder="/covers/my-course.jpg" value={thumbnailUrl} onChange={(e) => setThumbnailUrl(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="cb-cover">Cover image URL</Label>
            <Input id="cb-cover" value={coverImageUrl} onChange={(e) => setCoverImageUrl(e.target.value)} />
          </div>
        </div>

        <div className="mt-5 flex items-center justify-end gap-3">
          {saved && (
            <p role="status" className="text-sm text-success">
              Saved.
            </p>
          )}
          {error && <p className="text-sm text-destructive">{error}</p>}
          <Button type="submit" disabled={busy}>
            Save changes
          </Button>
        </div>
      </form>
    </section>
  );
}

/* ---------------- curriculum ---------------- */

type LessonDraft = {
  id: number | null;
  title: string;
  videoUrl: string;
  summary: string;
  bodyText: string;
};

function CurriculumCard({
  courseId,
  lessons,
  isLoading,
  onChanged,
}: {
  courseId: number;
  lessons: CourseLesson[];
  isLoading: boolean;
  onChanged: (lessons: CourseLesson[]) => void;
}) {
  const [editing, setEditing] = useState<LessonDraft | null>(null);
  const [adding, setAdding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function openEdit(lesson: CourseLesson) {
    setError(null);
    try {
      // Full detail first — the list endpoint omits body/video.
      const d = await getLessonDetail(lesson.id);
      setEditing({
        id: d.id,
        title: d.title ?? "",
        videoUrl: d.videoUrl ?? "",
        summary: d.summary ?? "",
        bodyText: d.bodyText ?? "",
      });
    } catch {
      setError("Could not load that lesson for editing.");
    }
  }

  async function submitAdd(draft: LessonDraft) {
    // Products.Name drives catalog titles for lessons — keep both in sync.
    await addLessonToCourse(courseId, {
      name: draft.title.trim(),
      title: draft.title.trim(),
      bodyText: draft.bodyText.trim(),
      videoUrl: draft.videoUrl.trim() || undefined,
      summary: draft.summary.trim() || undefined,
    });
    onChanged(await getCourseLessons(courseId));
    setAdding(false);
  }

  async function submitEdit(lessonId: number, draft: LessonDraft) {
    await updateCourseLesson(courseId, lessonId, {
      name: draft.title.trim(),
      title: draft.title.trim(),
      bodyText: draft.bodyText.trim(),
      videoUrl: draft.videoUrl.trim() || undefined,
      summary: draft.summary.trim() || undefined,
    });
    onChanged(await getCourseLessons(courseId));
    setEditing(null);
  }

  return (
    <section className="mt-10" aria-labelledby="curriculum-heading">
      <div className="flex items-center justify-between gap-4">
        <h2 id="curriculum-heading" className="font-display text-xl font-semibold tracking-tight">
          Curriculum
        </h2>
        <Button variant="outline" size="sm" onClick={() => { setError(null); setAdding(true); }}>
          <CirclePlus data-icon="inline-start" />
          Add lesson
        </Button>
      </div>

      {error && (
        <p role="alert" className="mt-3 text-sm text-destructive">
          {error}
        </p>
      )}

      {isLoading ? (
        <p className="mt-3 rounded-xl border border-dashed p-6 text-sm text-muted-foreground">
          Loading lessons…
        </p>
      ) : lessons.length === 0 ? (
        <p className="mt-3 rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground">
          No lessons yet. Add the first one — students see them in order.
        </p>
      ) : (
        <ol className="mt-3 divide-y overflow-hidden rounded-xl bg-card ring-1 ring-foreground/10 shadow-rest">
          {lessons.map((lesson, i) => (
            <li key={lesson.id} className="flex items-center gap-4 px-5 py-4">
              <span className="font-mono text-xs text-muted-foreground">
                {String(i + 1).padStart(2, "0")}
              </span>
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium">{lesson.title}</p>
                {lesson.summary && (
                  <p className="truncate text-xs text-muted-foreground">{lesson.summary}</p>
                )}
              </div>
              {!lesson.isPublished && <Badge variant="secondary">Draft</Badge>}
              <Button
                variant="ghost"
                size="icon"
                aria-label={`Edit ${lesson.title}`}
                onClick={() => void openEdit(lesson)}
              >
                <Pencil className="size-4" />
              </Button>
            </li>
          ))}
        </ol>
      )}

      {/* add — keyed remount seeds a fresh form */}
      {adding && (
        <LessonModal
          key="add"
          open
          onClose={() => setAdding(false)}
          title="Add lesson"
          initial={{ id: null, title: "", videoUrl: "", summary: "", bodyText: "" }}
          onSubmit={submitAdd}
        />
      )}

      {/* edit */}
      {editing !== null && (
        <LessonModal
          key={`edit-${editing.id}`}
          open
          onClose={() => setEditing(null)}
          title="Edit lesson"
          initial={editing}
          onSubmit={(draft) => submitEdit(editing.id!, draft)}
        />
      )}
    </section>
  );
}

function LessonModal({
  open,
  onClose,
  title,
  initial,
  onSubmit,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  initial: LessonDraft;
  onSubmit: (draft: LessonDraft) => Promise<void>;
}) {
  const [draft, setDraft] = useState<LessonDraft>(initial);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (busy) return;
    if (!draft.title.trim()) {
      setError("A title is required.");
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await onSubmit(draft);
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
      title={title}
      description="Students see lessons in list order."
    >
      <form onSubmit={submit} className="space-y-4" noValidate>
        <div className="space-y-2">
          <Label htmlFor="ls-title">Title</Label>
          <Input
            id="ls-title"
            required
            value={draft.title}
            onChange={(e) => setDraft({ ...draft, title: e.target.value })}
          />
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="ls-video">Video URL</Label>
            <Input
              id="ls-video"
              placeholder="YouTube / Vimeo / mp4"
              value={draft.videoUrl}
              onChange={(e) => setDraft({ ...draft, videoUrl: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="ls-summary">Summary</Label>
            <Input
              id="ls-summary"
              value={draft.summary}
              onChange={(e) => setDraft({ ...draft, summary: e.target.value })}
            />
          </div>
        </div>
        <div className="space-y-2">
          <Label htmlFor="ls-body">Body text</Label>
          <textarea
            id="ls-body"
            rows={5}
            className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm shadow-xs transition-[color,box-shadow] outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
            value={draft.bodyText}
            onChange={(e) => setDraft({ ...draft, bodyText: e.target.value })}
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
            {busy && <LoaderCircle aria-hidden="true" className="animate-spin" />}
            Save lesson
          </Button>
        </div>
      </form>
    </Modal>
  );
}

/* ---------------- instructors (Admin/SuperAdmin) ---------------- */

function InstructorsCard({ courseId }: { courseId: number }) {
  const [instructors, setInstructors] = useState<CourseInstructorRef[] | null>(null);
  const [pool, setPool] = useState<AdminUser[]>([]);
  const [selected, setSelected] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [confirmRemove, setConfirmRemove] = useState<CourseInstructorRef | null>(null);

  const reload = useCallback(() => {
    getCourseInstructors(courseId)
      .then(setInstructors)
      .catch(() => setError("Instructor list is unreachable."));
  }, [courseId]);

  useEffect(() => {
    reload();
    getUsers("instructors", 1, 50)
      .then(setPool)
      .catch(() => undefined); // assignment still possible by knowing the pool failed silently
  }, [reload]);

  async function assign() {
    if (!selected) return;
    setError(null);
    try {
      await assignCourseInstructor(courseId, Number(selected));
      setSelected("");
      reload();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Assignment failed.");
    }
  }

  async function remove(instructor: CourseInstructorRef) {
    setError(null);
    try {
      await removeCourseInstructor(courseId, instructor.id);
      setConfirmRemove(null);
      reload();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Removal failed.");
    }
  }

  return (
    <section className="mt-10" aria-labelledby="instructors-heading">
      <h2 id="instructors-heading" className="font-display text-xl font-semibold tracking-tight">
        Instructors
      </h2>

      {error && (
        <p role="alert" className="mt-3 text-sm text-destructive">
          {error}
        </p>
      )}

      <ul className="mt-3 divide-y overflow-hidden rounded-xl bg-card ring-1 ring-foreground/10 shadow-rest">
        {instructors === null ? (
          <li className="px-5 py-4 text-sm text-muted-foreground">Loading…</li>
        ) : instructors.length === 0 ? (
          <li className="px-5 py-8 text-center text-sm text-muted-foreground">
            No instructors assigned to this course yet.
          </li>
        ) : (
          instructors.map((ins) => (
            <li key={ins.id} className="flex items-center gap-4 px-5 py-3.5">
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium">{ins.name}</p>
                <p className="truncate text-xs text-muted-foreground">{ins.email}</p>
              </div>
              <Button
                variant="ghost"
                size="icon"
                aria-label={`Remove ${ins.name}`}
                onClick={() => setConfirmRemove(ins)}
              >
                <Trash2 className="size-4 text-destructive" />
              </Button>
            </li>
          ))
        )}
      </ul>

      <div className="mt-3 flex flex-wrap items-center gap-2">
        <select
          aria-label="Choose an instructor to assign"
          value={selected}
          onChange={(e) => setSelected(e.target.value)}
          className="h-9 rounded-lg border border-input bg-transparent px-3 text-sm shadow-xs outline-none transition-[color,box-shadow] focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
        >
          <option value="">Assign an instructor…</option>
          {pool.map((p) => (
            <option key={p.id} value={p.id}>
              {p.name}
            </option>
          ))}
        </select>
        <Button variant="outline" size="sm" disabled={!selected} onClick={assign}>
          Assign
        </Button>
      </div>

      <Modal
        open={confirmRemove !== null}
        onClose={() => setConfirmRemove(null)}
        title="Remove instructor?"
        description={
          confirmRemove
            ? `${confirmRemove.name} will lose authorship access to this course's content.`
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
