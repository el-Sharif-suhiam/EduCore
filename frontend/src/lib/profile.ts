// ============================================================
// PROFILE API — self-service account management.
// Contracts: PUT /api/users/{id} (UserOwnerOnly) and
// PUT /api/users/{id}/password (old password required).
// ============================================================

import { api } from "./api";

export type ProfileResponse = {
  id: number;
  name: string;
  email: string;
  birthDate?: string;
};

export function updateProfile(
  id: number,
  body: {
    name?: string;
    birthDate?: string; // ISO yyyy-mm-dd
    email?: string;
  }
): Promise<ProfileResponse> {
  return api.put<ProfileResponse>(`/api/users/${id}`, body);
}

/** Backend takes { oldPassword, newPassword } (lowercase keys). */
export function changePassword(
  id: number,
  oldPassword: string,
  newPassword: string
): Promise<unknown> {
  return api.put(`/api/users/${id}/password`, {
    oldPassword,
    newPassword,
  });
}
