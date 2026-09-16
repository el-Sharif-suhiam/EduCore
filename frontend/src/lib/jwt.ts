// Minimal client-side JWT payload decoder (NO verification —
// the backend verifies signatures; we only read claims for UI).

export type JwtClaims = {
  userId: number;
  email: string;
  roles: string[];
  expiresAtMs: number | null;
};

// ASP.NET Core outbound claim map: NameIdentifier→nameid, Email→email, Role→role
export function decodeJwt(token: string): JwtClaims | null {
  try {
    const [, payload] = token.split(".");
    if (!payload) return null;

    const json = JSON.parse(fromBase64Url(payload)) as {
      nameid?: string;
      email?: string;
      role?: string | string[];
      exp?: number;
    };

    return {
      userId: Number(json.nameid ?? 0),
      email: json.email ?? "",
      roles: Array.isArray(json.role) ? json.role : json.role ? [json.role] : [],
      expiresAtMs: json.exp ? json.exp * 1000 : null,
    };
  } catch {
    return null;
  }
}

function fromBase64Url(segment: string): string {
  const base64 = segment.replace(/-/g, "+").replace(/_/g, "/");
  const padded = base64 + "=".repeat((4 - (base64.length % 4)) % 4);
  // atob handles the binary-safe decode; UTF-8 chars decoded manually.
  const binary = atob(padded);
  const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}
