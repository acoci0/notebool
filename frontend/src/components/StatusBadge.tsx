const statusClasses: Record<string, string> = {
  Active: "badge badge-success",
  Approved: "badge badge-success",
  Pending: "badge badge-warning",
  Uploaded: "badge badge-muted",
  AiReview: "badge badge-info",
  ManualReview: "badge badge-warning",
  Suspended: "badge badge-danger",
  Rejected: "badge badge-danger",
  Closed: "badge badge-danger",
  Expired: "badge badge-muted"
};

export default function StatusBadge({ status }: { status: string }) {
  return (
    <span className={statusClasses[status] ?? "badge badge-muted"}>
      {status}
    </span>
  );
}
