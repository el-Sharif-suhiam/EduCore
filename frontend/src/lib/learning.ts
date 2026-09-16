// Typed access to the learner's own data: enrollments, progress,
// lesson detail. Shapes mirror backend view models (camelCase JSON).

import { api } from "./api";

// ------------------------------------------------------------
// GET /api/enrollments/my — EnrollmentViewModel.cs
// ------------------------------------------------------------
export const PRODUCT_TYPE = {
  Lesson: 1,
  Course: 2,
  Bundle: 3,
} as const;

export type Enrollment = {
  id: number;
  productId: number;
  productName: string;
  productTypeId: number; // enProductType
  thumbnailUrl: string | null;
  summary: string | null;
  enrolledAt: string;
  expireAt: string | null;
  /** Courses.Id when the product is a course — deep-link target */
  courseId: number | null;
  /** Lessons.Id when the product is a standalone lesson */
  lessonId: number | null;
};

// ------------------------------------------------------------
// GET /api/progress/courses/{courseId} — CourseProgressViewModel.cs
// ------------------------------------------------------------
export type LessonProgressItem = {
  lessonId: number;
  title: string;
  isComplete: boolean;
  completedDate: string | null;
};

export type CourseProgress = {
  courseId: number;
  userId: number;
  totalLessons: number;
  completedLessons: number;
  progressPercentage: number;
  lessons: LessonProgressItem[] | null;
};

// ------------------------------------------------------------
// GET /api/lessons/{id} — LessonResponse.cs
// ------------------------------------------------------------
export type LessonDetail = {
  id: number;
  name: string;
  createdAt: string;
  updatedAt: string;
  basePrice: number;
  thumbnailUrl: string | null;
  isPublished: boolean;
  summary: string | null;
  title: string;
  videoUrl: string | null;
  bodyText: string | null;
  isDeleted: boolean;
  deletedAt: string | null;
  instructorId: number;
  courseId: number | null;
};

// ---------------- fetchers ----------------

/** Active enrollments of the signed-in user, newest first. */
export function getMyEnrollments(pageNumber = 1, pageSize = 24): Promise<Enrollment[]> {
  return api.get<Enrollment[]>(
    `/api/enrollments/my?PageNumber=${pageNumber}&PageSize=${pageSize}`,
    true
  );
}

export function getCourseProgress(courseId: number): Promise<CourseProgress> {
  return api.get<CourseProgress>(`/api/progress/courses/${courseId}`, true);
}

export function getLesson(lessonId: number): Promise<LessonDetail> {
  return api.get<LessonDetail>(`/api/lessons/${lessonId}`, true);
}

export function markLessonComplete(lessonId: number): Promise<void> {
  return api.put(`/api/progress/lessons/${lessonId}/complete`, undefined, true);
}

export function markLessonIncomplete(lessonId: number): Promise<void> {
  return api.put(`/api/progress/lessons/${lessonId}/incomplete`, undefined, true);
}

// ------------------------------------------------------------
// POST-equivalent: GET /api/progress/courses/{id}/certificate
// Marks issuance on the backend (audit trail). The certificate
// itself is rendered in-app and printed/saved as PDF by the
// browser — the API currently returns no file (gap M14).
// ------------------------------------------------------------
export function issueCertificate(courseId: number): Promise<void> {
  return api.get<void>(`/api/progress/courses/${courseId}/certificate`);
}
