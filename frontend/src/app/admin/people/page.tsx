"use client";

// ============================================================
// /admin/people — students, instructors, admins.
// Tab per backend endpoint; search is server-side. Role actions
// mirror the API's authorization: instructor promotion for
// Admin+, admin promotion for SuperAdmin only, with confirm
// modals on anything that isn't trivially reversible.
// ============================================================

import { useEffect, useState } from "react";
import {
  ArrowDownUp,
  ShieldPlus,
  ShieldMinus,
  UserRoundX,
} from "lucide-react";
import { AdminPageHeader, DataTable, TableEmpty } from "@/components/admin/data-table";
import { Modal } from "@/components/ui/modal";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/lib/auth-context";
import {
  getUsers,
  deactivateUser,
  promoteToInstructor,
  removeInstructorRole,
  promoteToAdmin,
  removeAdminRole,
  type AdminUser,
  type PeopleKind,
} from "@/lib/admin";

const TABS: { key: PeopleKind; label: string }[] = [
  { key: "students", label: "Students" },
  { key: "instructors", label: "Instructors" },
  { key: "admins", label: "Admins" },
];

const PAGE_SIZE = 20;

export default function AdminPeoplePage() {
  const { user } = useAuth();
  const isSuper = !!user?.roles.includes("SuperAdmin");

  const [kind, setKind] = useState<PeopleKind>("students");
  const [search, setSearch] = useState("");
  const [rows, setRows] = useState<AdminUser[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [confirm, setConfirm] = useState<
    { title: string; body: string; action: () => Promise<void> } | null
  >(null);

  const [refresh, setRefresh] = useState(0);

  // Debounced server-side search; reloads on tab change and after mutations.
  useEffect(() => {
    let cancelled = false;

    const t = setTimeout(
      () => {
        setError(null);
        setRows(null);
        getUsers(kind, 1, PAGE_SIZE, search.trim())
          .then((data) => !cancelled && setRows(data))
          .catch(() => !cancelled && setError("The people list is unreachable right now."));
      },
      search ? 300 : 0
    );

    return () => {
      cancelled = true;
      clearTimeout(t);
    };
  }, [kind, search, refresh]);

  async function run(action: () => Promise<void>, title: string, body: string) {
    setConfirm({ title, body, action });
  }

  async function perform() {
    if (!confirm) return;
    setActionError(null);
    try {
      await confirm.action();
      setConfirm(null);
      setRefresh((n) => n + 1);
    } catch (err) {
      setConfirm(null);
      setActionError(err instanceof Error ? err.message : "Action failed.");
    }
  }

  return (
    <div className="mx-auto max-w-5xl">
      <AdminPageHeader
        title="People"
        description="Accounts and roles across the platform."
      />

      {/* tabs */}
      <div role="tablist" aria-label="User types" className="mt-6 flex gap-1 rounded-lg bg-secondary p-1 w-fit">
        {TABS.map((t) => (
          <button
            key={t.key}
            role="tab"
            aria-selected={kind === t.key}
            onClick={() => setKind(t.key)}
            className={`relative rounded-md px-4 py-1.5 text-sm font-medium transition-colors duration-200 ${
              kind === t.key
                ? "bg-card text-foreground shadow-rest"
                : "text-muted-foreground hover:text-foreground"
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div className="mt-4 max-w-sm">
        <Input
          type="search"
          placeholder="Search by name or email…"
          aria-label="Search users"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {actionError && (
        <p role="alert" className="mt-3 text-sm text-destructive">
          {actionError}
        </p>
      )}

      <div className="mt-3">
        <DataTable head={["Name", "Email", "Joined", "Status", "Actions"]} isLoading={rows === null && !error} error={error}>
          {rows !== null && rows.length === 0 && (
            <TableEmpty colSpan={5} message="No matching accounts." />
          )}
          {rows?.map((u) => (
            <tr key={u.id} className="border-b border-border/50 last:border-0">
              <td className="px-5 py-3.5 font-medium">{u.name}</td>
              <td className="px-5 py-3.5 text-muted-foreground">{u.email}</td>
              <td className="px-5 py-3.5 text-muted-foreground">
                {new Date(u.createdAt).toLocaleDateString()}
              </td>
              <td className="px-5 py-3.5">
                {u.isActive ? (
                  <Badge variant="secondary">Active</Badge>
                ) : (
                  <Badge variant="outline">Deactivated</Badge>
                )}
              </td>
              <td className="px-5 py-3.5">
                <div className="flex flex-wrap items-center gap-1">
                  {/* deactivation is soft but has no reverse endpoint — always confirm */}
                  {u.isActive && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="text-destructive hover:text-destructive"
                      onClick={() =>
                        run(
                          () => deactivateUser(u.id),
                          "Deactivate account?",
                          `${u.name} will no longer be able to sign in. There is no re-activate endpoint yet — this needs a support path.`
                        )
                      }
                    >
                      <UserRoundX data-icon="inline-start" />
                      Deactivate
                    </Button>
                  )}

                  {kind === "students" && u.isActive && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() =>
                        run(
                          async () => {
                            await promoteToInstructor(u.id);
                          },
                          "Grant Instructor role?",
                          `${u.name} will be able to create and manage courses.`
                        )
                      }
                    >
                      <ArrowDownUp data-icon="inline-start" />
                      Make instructor
                    </Button>
                  )}

                  {kind === "instructors" && u.isActive && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() =>
                        run(
                          async () => {
                            await removeInstructorRole(u.id);
                          },
                          "Remove Instructor role?",
                          `${u.name} will keep their content but can no longer create courses.`
                        )
                      }
                    >
                      <ShieldMinus data-icon="inline-start" />
                      Remove instructor
                    </Button>
                  )}

                  {kind === "admins" &&
                    u.isActive &&
                    (isSuper ? (
                      <>
                        {!u.role.includes("Super") && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() =>
                              run(
                                async () => {
                                  await promoteToAdmin(u.id);
                                },
                                "Grant Admin role?",
                                `${u.name} gains full console access. SuperAdmin cannot be revoked here.`
                              )
                            }
                          >
                            <ShieldPlus data-icon="inline-start" />
                            Make superadmin
                          </Button>
                        )}
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() =>
                            run(
                              async () => {
                                await removeAdminRole(u.id);
                              },
                              "Remove Admin role?",
                              `${u.name} loses console access. The backend has no last-admin guard — double-check before continuing.`
                            )
                          }
                        >
                          <ShieldMinus data-icon="inline-start" />
                          Remove admin
                        </Button>
                      </>
                    ) : (
                      <span className="text-xs text-muted-foreground">SuperAdmin actions only</span>
                    ))}
                </div>
              </td>
            </tr>
          ))}
        </DataTable>
      </div>

      <Modal
        open={confirm !== null}
        onClose={() => setConfirm(null)}
        title={confirm?.title ?? ""}
        description={confirm?.body}
      >
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={() => setConfirm(null)}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={perform}>
            Confirm
          </Button>
        </div>
      </Modal>
    </div>
  );
}
