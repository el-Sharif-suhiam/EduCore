import type { NextConfig } from "next";

// ============================================================
// DEV API PROXY (CORS-free development)
// ============================================================
// The browser never talks to https://localhost:7009 directly.
// Every request to same-origin /api/* is rewritten (proxied by
// the Next.js server) to the .NET API below. We default to the
// HTTP port (5087) because Kestrel's HTTPS dev cert is
// self-signed and would fail server-side TLS validation.
//
// Override with API_PROXY_TARGET in .env.local if needed.
// No changes are required on the backend (its CORS policy can
// stay untouched).
// ============================================================
const API_PROXY_TARGET =
  process.env.API_PROXY_TARGET ?? "http://localhost:5087";

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${API_PROXY_TARGET}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
