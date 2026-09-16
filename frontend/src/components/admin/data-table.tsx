import { Skeleton } from "@/components/ui/skeleton";

// ============================================================
// DATA TABLE — calm admin table language (UiUxDesign §22):
// strong hierarchy, readable spacing, skeleton loading,
// inline error state. Rows come in as plain <tr> children;
// use <TableEmpty> when a loaded page has no rows.
// ============================================================

export function DataTable({
  head,
  children,
  isLoading,
  error,
}: {
  head: string[];
  children: React.ReactNode;
  isLoading?: boolean;
  error?: string | null;
}) {
  return (
    <div className="overflow-hidden rounded-xl bg-card ring-1 ring-foreground/10 shadow-rest">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[40rem] border-collapse text-sm">
          <thead>
            <tr className="border-b border-border/70">
              {head.map((h) => (
                <th
                  key={h}
                  scope="col"
                  className="px-5 py-3.5 text-start font-mono text-xs font-medium uppercase tracking-widest text-muted-foreground"
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <SkeletonRows cols={head.length} />
            ) : error ? (
              <tr>
                <td
                  colSpan={head.length}
                  className="px-5 py-12 text-center text-sm text-muted-foreground"
                >
                  {error}
                </td>
              </tr>
            ) : (
              children
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function SkeletonRows({ cols }: { cols: number }) {
  return (
    <>
      {[0, 1, 2, 3].map((r) => (
        <tr key={r} className="border-b border-border/50 last:border-0">
          {Array.from({ length: cols }).map((_, c) => (
            <td key={c} className="px-5 py-4">
              <Skeleton className="h-4 w-full max-w-[10rem]" />
            </td>
          ))}
        </tr>
      ))}
    </>
  );
}

/** Centered empty-state row to drop inside <tbody>. */
export function TableEmpty({
  message,
  colSpan,
}: {
  message: string;
  colSpan: number;
}) {
  return (
    <tr>
      <td colSpan={colSpan} className="px-5 py-12 text-center text-sm text-muted-foreground">
        {message}
      </td>
    </tr>
  );
}

/** Page-level header used across the admin console. */
export function AdminPageHeader({
  title,
  description,
  actions,
}: {
  title: string;
  description?: string;
  actions?: React.ReactNode;
}) {
  return (
    <header className="flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 className="font-display text-3xl font-semibold tracking-tight">{title}</h1>
        {description && (
          <p className="mt-1.5 max-w-xl text-sm leading-relaxed text-muted-foreground">
            {description}
          </p>
        )}
      </div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </header>
  );
}
