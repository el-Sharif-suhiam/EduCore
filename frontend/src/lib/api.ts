// ============================================================
// NATIVE FETCH API CLIENT — the ONLY place HTTP happens.
// ============================================================
// HARD PROJECT RULE: use native fetch. NEVER install axios.
// (Owner decision — see docs/frontend/FRONTEND_CONTEXT.md)
//
// Responsibilities:
//   1. Prefix paths with NEXT_PUBLIC_API_BASE_URL ("" in dev →
//      same-origin /api/* → proxied by next.config.ts).
//   2. Attach the JWT access token when available.
//   3. On 401: try ONE refresh-token rotation, then retry once
//      (guards against infinite refresh loops).
//   4. Convert RFC7807 problem+json errors into typed ApiError.
//
// Backend error contract (ExceptionMiddleware): application/problem+json
// with { title, status, detail? }.
// ============================================================

const API_BASE = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "").replace(
  /\/$/,
  ""
);

const AUTH_STORAGE_KEY = "educore.auth.v1";

export type StoredAuth = {
  accessToken: string;
  refreshToken: string;
  email: string;
};

// ---------------- auth token storage (localStorage) ---------

export function getStoredAuth(): StoredAuth | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.localStorage.getItem(AUTH_STORAGE_KEY);
    return raw ? (JSON.parse(raw) as StoredAuth) : null;
  } catch {
    return null;
  }
}

export function setStoredAuth(auth: StoredAuth): void {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(auth));
}

export function clearStoredAuth(): void {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(AUTH_STORAGE_KEY);
}

// ---------------- typed errors -------------------------------

export class ApiError extends Error {
  status: number;
  title: string;

  constructor(status: number, message: string, title?: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.title = title ?? "Request failed";
  }
}

// ---------------- refresh flow --------------------------------

let refreshInFlight: Promise<boolean> | null = null;

async function rotateRefreshToken(): Promise<boolean> {
  // POST /api/auth/refresh { email, refreshToken } -> new tokens
  const stored = getStoredAuth();
  if (!stored) return false;

  const single = async () => {
    try {
      const res = await fetch(`${API_BASE}/api/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          email: stored.email,
          refreshToken: stored.refreshToken,
        }),
      });
      if (!res.ok) return false;
      const data = (await res.json()) as Partial<StoredAuth>;
      if (!data.accessToken || !data.refreshToken) return false;
      setStoredAuth({
        accessToken: data.accessToken,
        refreshToken: data.refreshToken,
        email: stored.email,
      });
      return true;
    } catch {
      return false;
    }
  };

  refreshInFlight ??= single().finally(() => {
    refreshInFlight = null;
  });
  return refreshInFlight;
}

// ---------------- core fetch wrapper --------------------------

type ApiOptions = {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown; // JSON-encoded unless FormData
  /** Attach Authorization header + allow refresh retry. Default true. */
  auth?: boolean;
};

export async function apiFetch<T>(
  path: string,
  options: ApiOptions = {}
): Promise<T> {
  const { method = "GET", body, auth = true } = options;

  const doFetch = async (): Promise<Response> => {
    const headers: Record<string, string> = { Accept: "application/json" };

    let payload: BodyInit | undefined;
    if (body !== undefined) {
      headers["Content-Type"] = "application/json";
      payload =
        body instanceof FormData ? body : JSON.stringify(body);
    }

    if (auth) {
      const stored = getStoredAuth();
      if (stored?.accessToken) {
        headers.Authorization = `Bearer ${stored.accessToken}`;
      }
    }

    return fetch(`${API_BASE}${path}`, {
      method,
      headers,
      body: payload,
    });
  };

  let res = await doFetch();

  // One-shot refresh on 401, then a single retry.
  if (res.status === 401 && auth && getStoredAuth()) {
    const refreshed = await rotateRefreshToken();
    if (refreshed) {
      res = await doFetch();
    } else {
      clearStoredAuth();
    }
  }

  if (!res.ok) {
    throw await toApiError(res);
  }

  if (res.status === 204) return undefined as T;

  const text = await res.text();
  if (!text) return undefined as T;
  return JSON.parse(text) as T;
}

async function toApiError(res: Response): Promise<ApiError> {
  try {
    const problem = (await res.json()) as {
      title?: string;
      detail?: string;
      status?: number;
    };
    return new ApiError(
      res.status,
      problem.detail ?? problem.title ?? `Request failed (${res.status})`,
      problem.title
    );
  } catch {
    return new ApiError(res.status, `Request failed (${res.status})`);
  }
}

// ---------------- convenience verbs ---------------------------

export const api = {
  get: <T>(path: string, auth = true) => apiFetch<T>(path, { method: "GET", auth }),
  post: <T>(path: string, body?: unknown, auth = true) =>
    apiFetch<T>(path, { method: "POST", body, auth }),
  put: <T>(path: string, body?: unknown, auth = true) =>
    apiFetch<T>(path, { method: "PUT", body, auth }),
  del: <T>(path: string, auth = true) => apiFetch<T>(path, { method: "DELETE", auth }),
};
