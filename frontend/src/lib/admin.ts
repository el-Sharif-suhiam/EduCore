// ============================================================
// ADMIN API — typed fetchers for the admin/instructor console.
// Contracts mirror the controllers exactly (camelCase JSON):
//   * paged endpoints return plain arrays (NO totals) —
//     "has more" is inferred from page fullness.
//   * enums arrive as NUMBERS (no JsonStringEnumConverter);
//     label maps live at the bottom of this file.
//   * GET /api/courses returns ALL non-deleted courses but has
//     no isPublished flag — publish state comes from the detail
//     endpoint only (documented backend gap, do not fake it).
// ============================================================

import { api } from "./api";
import type { CourseSummary } from "./courses";

const qs = (params: Record<string, string | number | undefined>) => {
  const sp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== "") sp.set(k, String(v));
  }
  const s = sp.toString();
  return s ? `?${s}` : "";
};

// ---------------- people --------------------------------------

export type AdminUser = {
  id: number;
  name: string;
  birthDate: string;
  email: string;
  createdAt: string;
  isActive: boolean;
  role: string;
};

export type PeopleKind = "students" | "instructors" | "admins";

export function getUsers(
  kind: PeopleKind,
  pageNumber = 1,
  pageSize = 20,
  search = "",
  includeInactive = false
): Promise<AdminUser[]> {
  return api.get<AdminUser[]>(
    `/api/users/${kind}${qs({
      PageNumber: pageNumber,
      PageSize: pageSize,
      search,
      includeInactive: includeInactive ? "true" : undefined,
    })}`
  );
}

/** Soft-deactivate a user. */
export function deactivateUser(id: number): Promise<void> {
  return api.del<void>(`/api/users/${id}`);
}

/** Bring a deactivated account back. Admin/SuperAdmin. */
export function activateUser(id: number): Promise<void> {
  return api.post(`/api/users/${id}/activate`).then(() => undefined);
}

export function promoteToInstructor(id: number): Promise<unknown> {
  return api.post(`/api/users/toInstructor/${id}`);
}

export function removeInstructorRole(id: number): Promise<unknown> {
  return api.post(`/api/users/toInstructor/${id}/remove`);
}

/** SuperAdmin only. */
export function promoteToAdmin(id: number): Promise<unknown> {
  return api.post(`/api/users/toAdmin/${id}`);
}

/** SuperAdmin only. No last-admin guard on the backend — use with care. */
export function removeAdminRole(id: number): Promise<unknown> {
  return api.post(`/api/users/toAdmin/${id}/remove`);
}

// ---------------- courses -------------------------------------

export { type CourseSummary };

export function getCoursesPaged(
  pageNumber = 1,
  pageSize = 12,
  search = ""
): Promise<CourseSummary[]> {
  return api.get<CourseSummary[]>(
    `/api/courses${qs({
      PageNumber: pageNumber,
      PageSize: pageSize,
      search,
      includeUnpublished: "true",
    })}`
  );
}

export type CourseCreated = { id: number; name: string; basePrice: number };

export function createCourse(body: {
  name: string;
  basePrice: number;
  summary?: string;
  thumbnailUrl?: string;
  coverImageUrl?: string;
}): Promise<CourseCreated> {
  return api.post<CourseCreated>("/api/courses", body);
}

export function updateCourse(
  id: number,
  body: {
    name?: string;
    basePrice?: number;
    summary?: string;
    thumbnailUrl?: string;
    coverImageUrl?: string;
  }
): Promise<unknown> {
  return api.put(`/api/courses/${id}`, body);
}

export function publishCourse(id: number): Promise<{ success: boolean }> {
  return api.post(`/api/courses/${id}/publish`);
}

export function unpublishCourse(id: number): Promise<{ success: boolean }> {
  return api.post(`/api/courses/${id}/unpublish`);
}

// ---------------- course lessons -------------------------------

export type CourseLessonCreated = {
  id: number;
  name: string;
  title: string;
  courseId: number;
};

/** Creates an unpublished lesson inside a course (price forced to 0). */
export function addLessonToCourse(
  courseId: number,
  body: {
    name: string;
    title: string;
    bodyText: string;
    videoUrl?: string;
    thumbnailUrl?: string;
    summary?: string;
  }
): Promise<CourseLessonCreated> {
  return api.post<CourseLessonCreated>(`/api/courses/${courseId}/lessons`, body);
}

export function updateCourseLesson(
  courseId: number,
  lessonId: number,
  body: {
    name?: string;
    title?: string;
    basePrice?: number;
    bodyText?: string;
    videoUrl?: string;
    thumbnailUrl?: string;
    summary?: string;
  }
): Promise<unknown> {
  return api.put(`/api/courses/${courseId}/lessons/${lessonId}`, body);
}

/** Full lesson detail (owner/admin/enrolled per backend rules). */
export type LessonDetail = {
  id: number;
  name: string | null;
  title: string | null;
  basePrice: number;
  summary: string | null;
  bodyText: string | null;
  videoUrl: string | null;
  thumbnailUrl: string | null;
  isPublished: boolean;
  instructorId: number | null;
  courseId: number | null;
};

export function getLessonDetail(id: number): Promise<LessonDetail> {
  return api.get<LessonDetail>(`/api/lessons/${id}`);
}

// ---------------- standalone lessons ----------------------------

/** Mirrors LessonsWithOutCoursesViewModel.cs — admin feed includes drafts. */
export type AdminLessonSummary = {
  id: number;
  productId: number;
  title: string;
  summary: string | null;
  basePrice: number;
  createdAt: string;
  thumbnailUrl: string | null;
  isPublished: boolean;
  instructorId: number;
  instructorName: string;
};

/** Admin/SuperAdmin list — draft visibility arrives via includeUnpublished=true. */
export function getLessonsPaged(
  pageNumber = 1,
  pageSize = 12,
  search = "",
  includeUnpublished = true
): Promise<AdminLessonSummary[]> {
  return api.get<AdminLessonSummary[]>(
    `/api/lessons${qs({
      PageNumber: pageNumber,
      PageSize: pageSize,
      search,
      includeUnpublished: includeUnpublished ? "true" : undefined,
    })}`
  );
}

/** Mirrors LessonRequest.cs — BodyText required by the DTO. */
export type LessonInput = {
  name: string;
  title: string;
  basePrice: number;
  bodyText: string;
  videoUrl?: string;
  thumbnailUrl?: string;
  summary?: string;
};

export function createStandaloneLesson(body: LessonInput): Promise<{ id: number; title: string }> {
  return api.post<{ id: number; title: string }>("/api/lessons", body);
}

export function updateStandaloneLesson(
  id: number,
  body: Partial<LessonInput>
): Promise<unknown> {
  return api.put(`/api/lessons/${id}`, body);
}

export function publishLesson(id: number): Promise<{ success: boolean }> {
  return api.post(`/api/lessons/${id}/publish`);
}

export function unpublishLesson(id: number): Promise<{ success: boolean }> {
  return api.post(`/api/lessons/${id}/unpublish`);
}

export function deleteLesson(id: number): Promise<{ id: number; title: string }> {
  return api.del<{ id: number; title: string }>(`/api/lessons/${id}`);
}

export function restoreLesson(id: number): Promise<{ id: number; title: string }> {
  return api.put<{ id: number; title: string }>(`/api/lessons/${id}/restore`);
}

// ---------------- course instructors ---------------------------

export type CourseInstructorRef = {
  id: number;
  name: string;
  email: string;
};

/** Admin/SuperAdmin only. */
export function getCourseInstructors(courseId: number): Promise<CourseInstructorRef[]> {
  return api.get<CourseInstructorRef[]>(`/api/courses/${courseId}/instructors`);
}

/** Admin/SuperAdmin only. */
export function assignCourseInstructor(
  courseId: number,
  instructorId: number
): Promise<CourseInstructorRef> {
  return api.post(`/api/courses/${courseId}/instructors${qs({ instructorId })}`);
}

/** Admin/SuperAdmin only. */
export function removeCourseInstructor(
  courseId: number,
  instructorId: number
): Promise<{ success: boolean }> {
  return api.del(`/api/courses/${courseId}/instructors${qs({ instructorId })}`);
}

// ---------------- bundles ------------------------------------

export type AdminBundle = {
  id: number;
  /** Products-table id — the cart API requires this, not `id`. */
  productId: number;
  name: string;
  createdAt: string;
  basePrice: number;
  thumbnailUrl: string | null;
  summary: string | null;
  isPublished: boolean;
};

/** All bundles including drafts. Instructor/SuperAdmin only. */
export function getAdminBundles(): Promise<AdminBundle[]> {
  return api.get<AdminBundle[]>("/api/bundles/all", true);
}

export type BundleCreated = { id: number; name: string; basePrice: number };

export function createBundle(body: {
  name: string;
  basePrice: number;
  summary?: string;
  thumbnailUrl?: string;
}): Promise<BundleCreated> {
  return api.post<BundleCreated>("/api/bundles", body, true);
}

export function updateBundle(
  id: number,
  body: {
    name?: string;
    basePrice?: number;
    summary?: string;
    thumbnailUrl?: string;
  }
): Promise<unknown> {
  return api.put(`/api/bundles/${id}`, body, true);
}

export function publishBundle(id: number): Promise<{ success: boolean }> {
  return api.post(`/api/bundles/${id}/publish`, undefined, true);
}

export function unpublishBundle(id: number): Promise<{ success: boolean }> {
  return api.post(`/api/bundles/${id}/unpublish`, undefined, true);
}

/** Bundle + its member courses (public endpoint, works for owners too). */
export type BundleDetail = AdminBundle & {
  courses: {
    courseId: number;
    name: string;
    summary: string | null;
    thumbnailUrl: string | null;
  }[];
};

export async function getBundleDetail(id: number): Promise<BundleDetail> {
  const [detail, items] = await Promise.all([
    api.get<AdminBundle>(`/api/bundles/${id}`, false),
    api.get<{ id: number; bundleName: string; courses: BundleDetail["courses"] }>(
      `/api/bundles/${id}/items`,
      false
    ),
  ]);
  return { ...detail, courses: items.courses };
}

/** Add a course to a bundle. Instructor/SuperAdmin only. */
export function addBundleItem(bundleId: number, courseId: number): Promise<{ success: boolean }> {
  return api.post(`/api/bundles/${bundleId}/items`, courseId, true);
}

/** Remove a course from a bundle. Instructor/SuperAdmin only. */
export function removeBundleItem(
  bundleId: number,
  courseId: number
): Promise<{ success: boolean }> {
  return api.del(`/api/bundles/${bundleId}/items/${courseId}`, true);
}

// ---------------- discount codes -------------------------------

export type DiscountCode = {
  id: number;
  discountCode: string;
  discountRate: number | null;
  createdById: number;
  expireAt: string | null;
  allowedUseNumber: number | null;
  totalUsedNumber: number | null;
};

export function getValidDiscountCodes(): Promise<DiscountCode[]> {
  return api.get<DiscountCode[]>("/api/discount-codes/valid");
}

/** SuperAdmin only. CreatedById is taken from the JWT, not the body. */
export function createDiscountCode(body: {
  discountCode: string;
  discountRate?: number;
  expireAt?: string;
  allowedUseNumber?: number;
}): Promise<DiscountCode> {
  return api.post<DiscountCode>("/api/discount-codes", body);
}

/** SuperAdmin only. */
export function updateDiscountCode(
  id: number,
  body: {
    discountCode?: string;
    discountRate?: number;
    expireAt?: string;
    allowedUseNumber?: number;
  }
): Promise<unknown> {
  return api.put(`/api/discount-codes/${id}`, body);
}

/** SuperAdmin only. Hard delete — blocked by FK once payments reference it. */
export function deleteDiscountCode(id: number): Promise<void> {
  return api.del<void>(`/api/discount-codes/${id}`);
}

// ---------------- audit trail ----------------------------------

export type AuditEntry = {
  id: number;
  userId: number;
  actionType: number; // enAuditActionType (numeric over the wire)
  entityType: string;
  entityId: number;
  description: string;
  doneAt: string;
  ipAddress: string | null;
  userAgent: string | null;
};

export function getAudits(pageNumber = 1, pageSize = 20): Promise<AuditEntry[]> {
  return api.get<AuditEntry[]>(`/api/audit${qs({ PageNumber: pageNumber, PageSize: pageSize })}`);
}

// ---------------- system logs -----------------------------------

export type LogEntry = {
  id: number;
  logType: number; // enLogType (numeric over the wire)
  message: string;
  stackTrace: string | null;
  source: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  requestPath: string | null;
  createdAt: string;
};

export function getLogs(pageNumber = 1, pageSize = 20): Promise<LogEntry[]> {
  return api.get<LogEntry[]>(`/api/logs${qs({ PageNumber: pageNumber, PageSize: pageSize })}`);
}

// ---------------- commerce (orders & payments, admin) -----------

export type AdminOrder = {
  id: number;
  userId: number;
  userName: string;
  userEmail: string;
  totalPrice: number;
  status: number; // enOrderStatus as number
  createdAt: string;
};

export type AdminPayment = {
  id: number;
  orderId: number;
  userId: number;
  userName: string;
  userEmail: string;
  createdAt: string;
  paidAt: string | null;
  price: number;
  discountId: number | null;
  discountPrice: number | null;
  paymentMethod: string | null;
  status: number; // enPaymentStatus as number
  transactionId: string | null;
  finalPrice: number;
};

export function getAdminOrders(
  pageNumber = 1,
  pageSize = 20,
  search = ""
): Promise<AdminOrder[]> {
  return api.get<AdminOrder[]>(
    `/api/orders${qs({ PageNumber: pageNumber, PageSize: pageSize, search })}`
  );
}

export function getAdminPayments(
  pageNumber = 1,
  pageSize = 20,
  search = ""
): Promise<AdminPayment[]> {
  return api.get<AdminPayment[]>(
    `/api/payments${qs({ PageNumber: pageNumber, PageSize: pageSize, search })}`
  );
}

// ---------------- enum label maps (wire values are numeric) ----

export const AUDIT_ACTION_LABELS: Record<number, string> = {
  0: "Login",
  1: "Logout",
  2: "Create lesson",
  3: "Delete lesson",
  4: "Update lesson",
  5: "Restore lesson",
  6: "Create course",
  7: "Delete course",
  8: "Update course",
  9: "Restore course",
  10: "Create bundle",
  11: "Delete bundle",
  12: "Update bundle",
  13: "Create order",
  14: "Update order",
  15: "Create discount",
  16: "Update discount",
  17: "Delete discount",
  18: "Create payment",
  19: "Update payment",
  20: "Payment succeeded",
  21: "Payment failed",
  22: "Payment expired",
  23: "Promoted to admin",
  24: "Promoted to instructor",
  25: "Removed from admins",
  26: "Removed from instructors",
  27: "Enrolled in product",
28: "Create user",
   29: "Update user",
   30: "Delete user",
   31: "Reactivated user",
};

export const LOG_TYPE_LABELS: Record<number, string> = {
  0: "Information",
  1: "Warning",
  2: "Error",
  3: "Critical",
  4: "Debug",
  5: "Trace",
};

export const ORDER_STATUS_LABELS: Record<number, string> = {
  0: "Pending",
  1: "Completed",
  2: "Cancelled",
  3: "Empty",
};

export const PAYMENT_STATUS_LABELS: Record<number, string> = {
  0: "Pending",
  1: "Succeeded",
  2: "Failed",
  3: "Expired",
  4: "Cancelled",
};
