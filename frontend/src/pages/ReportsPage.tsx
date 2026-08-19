import {
  Building2,
  CalendarRange,
  Clock3,
  Download,
  RefreshCw,
  ShieldCheck,
  ShoppingCart,
  TriangleAlert,
  Users,
  WalletCards
} from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import api from "../api/client";
import type {
  AdminReports,
  ReportChartPoint,
  ReportDistribution,
  ReportSummary
} from "../types";

type TimeRange =
  | "daily"
  | "weekly"
  | "monthly"
  | "halfyear"
  | "yearly"
  | "all";

const timeRanges: Array<{ value: TimeRange; label: string }> = [
  { value: "daily", label: "Günlük" },
  { value: "weekly", label: "Haftalık" },
  { value: "monthly", label: "Aylık" },
  { value: "halfyear", label: "Yarım Yıllık" },
  { value: "yearly", label: "Yıllık" },
  { value: "all", label: "Tüm Zamanlar" }
];

const rangeTitles: Record<TimeRange, string> = {
  daily: "Saatlik Site Ziyaretleri",
  weekly: "Haftalık Site Ziyaretleri",
  monthly: "Aylık Site Ziyaretleri",
  halfyear: "Yarım Yıllık Site Ziyaretleri",
  yearly: "Yıllık Site Ziyaretleri",
  all: "Tüm Zamanlardaki Site Ziyaretleri"
};

const distributionColors = [
  "#6c5ce7",
  "#14a873",
  "#2782e7",
  "#e89a18",
  "#dc4b56"
];

const emptySummary: ReportSummary = {
  totalUsers: 0,
  verifiedStudents: 0,
  pendingVerifications: 0,
  totalSales: 0,
  platformRevenue: 0,
  openComplaints: 0
};

const numberFormatter = new Intl.NumberFormat("tr-TR");
const currencyFormatter = new Intl.NumberFormat("tr-TR", {
  style: "currency",
  currency: "TRY",
  maximumFractionDigits: 0
});

function VisitLineChart({ points }: { points: ReportChartPoint[] }) {
  if (points.length === 0) {
    return <div className="reports-empty-chart">Bu dönem için ziyaret verisi yok.</div>;
  }

  const width = 760;
  const height = 280;
  const padding = { top: 20, right: 18, bottom: 44, left: 58 };
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;
  const maxValue = Math.max(...points.map((point) => point.value), 1);
  const roundedMax = Math.max(4, Math.ceil(maxValue / 4) * 4);

  const coordinates = points.map((point, index) => {
    const x =
      padding.left +
      (index / Math.max(points.length - 1, 1)) * chartWidth;
    const y =
      padding.top +
      chartHeight -
      (point.value / roundedMax) * chartHeight;

    return { ...point, x, y };
  });

  const linePath = coordinates
    .map((point, index) => `${index === 0 ? "M" : "L"} ${point.x} ${point.y}`)
    .join(" ");
  const areaPath = `${linePath} L ${
    coordinates[coordinates.length - 1].x
  } ${padding.top + chartHeight} L ${coordinates[0].x} ${
    padding.top + chartHeight
  } Z`;

  return (
    <div className="reports-line-chart" aria-label="Site ziyaret grafiği">
      <svg viewBox={`0 0 ${width} ${height}`} role="img">
        <defs>
          <linearGradient id="reportsAreaGradient" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#6c5ce7" stopOpacity="0.28" />
            <stop offset="100%" stopColor="#6c5ce7" stopOpacity="0.02" />
          </linearGradient>
        </defs>

        {[0, 1, 2, 3, 4].map((step) => {
          const y = padding.top + (step / 4) * chartHeight;
          const value = Math.round(roundedMax * (1 - step / 4));

          return (
            <g key={step}>
              <line
                x1={padding.left}
                y1={y}
                x2={width - padding.right}
                y2={y}
                className="reports-chart-grid-line"
              />
              <text x={padding.left - 12} y={y + 4} textAnchor="end">
                {numberFormatter.format(value)}
              </text>
            </g>
          );
        })}

        <path d={areaPath} fill="url(#reportsAreaGradient)" />
        <path d={linePath} className="reports-chart-line" />

        {coordinates.map((point, index) => (
          <g key={`${point.label}-${index}`}>
            <circle cx={point.x} cy={point.y} r="4" className="reports-chart-dot" />
            <text x={point.x} y={height - 14} textAnchor="middle">
              {point.label}
            </text>
          </g>
        ))}
      </svg>
    </div>
  );
}

function UniversityDonut({
  items,
  total
}: {
  items: ReportDistribution[];
  total: number;
}) {
  if (items.length === 0) {
    return <div className="reports-empty-chart">Doğrulanmış üniversite verisi yok.</div>;
  }

  let accumulated = 0;
  const gradient = items
    .map((item, index) => {
      const start = accumulated;
      accumulated = index === items.length - 1
        ? 100
        : accumulated + item.percentage;
      return `${distributionColors[index % distributionColors.length]} ${start}% ${accumulated}%`;
    })
    .join(", ");

  return (
    <div className="reports-donut-layout">
      <div
        className="reports-donut"
        style={{ background: `conic-gradient(${gradient})` }}
        role="img"
        aria-label="Üniversitelere göre üye dağılımı"
      >
        <div className="reports-donut__center">
          <strong>{numberFormatter.format(total)}</strong>
          <span>öğrenci</span>
        </div>
      </div>

      <div className="reports-donut-legend">
        {items.map((item, index) => (
          <div key={item.name}>
            <span
              className="reports-legend-dot"
              style={{
                background: distributionColors[index % distributionColors.length]
              }}
            />
            <span>{item.name}</span>
            <strong>%{item.percentage}</strong>
          </div>
        ))}
      </div>
    </div>
  );
}

function getBadgeClass(status: string) {
  if (status === "Çözüldü") return "badge-success";
  if (status === "Reddedildi" || status === "Süresi Doldu") return "badge-danger";
  return "badge-warning";
}

export default function ReportsPage() {
  const [timeRange, setTimeRange] = useState<TimeRange>("monthly");
  const [universityId, setUniversityId] = useState("all");
  const [reports, setReports] = useState<AdminReports | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadReports = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const { data } = await api.get<AdminReports>("/admin/reports", {
        params: {
          range: timeRange,
          universityId: universityId === "all" ? undefined : universityId
        }
      });

      setReports(data);
    } catch {
      setError("Rapor verileri alınamadı. Backend bağlantısını kontrol edin.");
    } finally {
      setLoading(false);
    }
  }, [timeRange, universityId]);

  useEffect(() => {
    void loadReports();
  }, [loadReports]);

  const summary = reports?.summary ?? emptySummary;

  const exportReport = () => {
    if (!reports) return;

    const selectedUniversity =
      reports.universityOptions.find((item) => item.id === universityId)?.name ??
      "Tüm Üniversiteler";
    const selectedRange =
      timeRanges.find((item) => item.value === timeRange)?.label ?? "Aylık";
    const rows: Array<Array<string | number>> = [
      ["NotMarket Raporu"],
      ["Zaman Aralığı", selectedRange],
      ["Üniversite", selectedUniversity],
      ["Toplam Kullanıcı", summary.totalUsers],
      ["Doğrulanmış Öğrenci", summary.verifiedStudents],
      ["Bekleyen Başvuru", summary.pendingVerifications],
      [],
      ["Fakülte", "Üye Sayısı", "Oran"],
      ...reports.facultyDistribution.map((item) => [
        item.name,
        item.count,
        `%${item.percentage}`
      ])
    ];
    const csv = rows.map((row) => row.join(";")).join("\n");
    const blob = new Blob([`\uFEFF${csv}`], {
      type: "text/csv;charset=utf-8"
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `notmarket-rapor-${new Date().toISOString().slice(0, 10)}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="reports-page">
      <div className="page-heading reports-heading">
        <div>
          <h1>Raporlar ve Analitik</h1>
          <p>Platform performansını ve operasyon süreçlerini takip edin.</p>
        </div>

        <div className="reports-heading__actions">
          <label className="reports-select">
            <Building2 size={17} />
            <select
              value={universityId}
              onChange={(event) => setUniversityId(event.target.value)}
              aria-label="Üniversite filtresi"
              disabled={loading}
            >
              <option value="all">Tüm Üniversiteler</option>
              {reports?.universityOptions.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </label>

          <button
            type="button"
            className="secondary-button"
            onClick={() => void loadReports()}
            disabled={loading}
          >
            <RefreshCw size={17} className={loading ? "is-spinning" : ""} />
            {loading ? "Güncelleniyor" : "Verileri Güncelle"}
          </button>

          <button
            type="button"
            className="primary-button"
            onClick={exportReport}
            disabled={!reports || loading}
          >
            <Download size={17} />
            Raporu Dışa Aktar
          </button>
        </div>
      </div>

      {error && <div className="reports-feedback reports-feedback--error">{error}</div>}

      <section className="reports-stats" aria-label="Rapor özeti">
        <article className="report-stat-card report-stat-card--purple">
          <Users size={21} /><div><span>Toplam Kullanıcı</span><strong>{numberFormatter.format(summary.totalUsers)}</strong></div>
        </article>
        <article className="report-stat-card report-stat-card--green">
          <ShieldCheck size={21} /><div><span>Doğrulanmış Öğrenci</span><strong>{numberFormatter.format(summary.verifiedStudents)}</strong></div>
        </article>
        <article className="report-stat-card report-stat-card--amber">
          <Clock3 size={21} /><div><span>Bekleyen Başvuru</span><strong>{numberFormatter.format(summary.pendingVerifications)}</strong></div>
        </article>
        <article className="report-stat-card report-stat-card--green">
          <ShoppingCart size={21} /><div><span>Toplam Satış</span><strong>{numberFormatter.format(summary.totalSales)}</strong></div>
        </article>
        <article className="report-stat-card report-stat-card--purple">
          <WalletCards size={21} /><div><span>Platform Geliri</span><strong>{currencyFormatter.format(summary.platformRevenue)}</strong></div>
        </article>
        <article className="report-stat-card report-stat-card--red">
          <TriangleAlert size={21} /><div><span>Açık Şikâyet</span><strong>{numberFormatter.format(summary.openComplaints)}</strong></div>
        </article>
      </section>

      <section className="reports-range-filter" aria-label="Grafik zaman aralığı">
        <div className="reports-range-filter__label">
          <CalendarRange size={18} /><strong>Grafik Zaman Aralığı</strong>
        </div>
        <div className="reports-range-filter__options">
          {timeRanges.map((item) => (
            <button
              key={item.value}
              type="button"
              className={timeRange === item.value ? "is-active" : ""}
              onClick={() => setTimeRange(item.value)}
              disabled={loading}
            >
              {item.label}
            </button>
          ))}
        </div>
      </section>

      <section className="reports-chart-grid">
        <article className="panel reports-chart-panel">
          <div className="reports-panel-heading">
            <div>
              <h2>{rangeTitles[timeRange]}</h2>
              <span className="reports-chart-legend"><i /> Ziyaret Sayısı</span>
            </div>
          </div>
          {loading && !reports ? (
            <div className="reports-empty-chart">Rapor verileri yükleniyor…</div>
          ) : (
            <VisitLineChart points={reports?.visits ?? []} />
          )}
        </article>

        <article className="panel reports-chart-panel">
          <div className="reports-panel-heading">
            <h2>Üniversitelere Göre Üye Dağılımı</h2>
          </div>
          <UniversityDonut
            items={reports?.universityDistribution ?? []}
            total={summary.verifiedStudents}
          />
        </article>
      </section>

      <section className="reports-table-grid">
        <article className="panel reports-table-panel">
          <div className="reports-panel-heading"><h2>Fakültelere Göre Üye Dağılımı</h2></div>
          <div className="table-wrapper">
            <table>
              <thead><tr><th>Fakülte</th><th>Üye Sayısı</th><th>Oran</th></tr></thead>
              <tbody>
                {(reports?.facultyDistribution ?? []).map((faculty) => (
                  <tr key={faculty.name}>
                    <td>{faculty.name}</td>
                    <td>{numberFormatter.format(faculty.count)}</td>
                    <td>
                      <div className="reports-rate-cell">
                        <span>%{faculty.percentage}</span>
                        <i><b style={{ width: `${faculty.percentage}%` }} /></i>
                      </div>
                    </td>
                  </tr>
                ))}
                {!loading && reports?.facultyDistribution.length === 0 && (
                  <tr><td colSpan={3}>Doğrulanmış fakülte verisi bulunmuyor.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </article>

        <article className="panel reports-table-panel">
          <div className="reports-panel-heading"><h2>Son Moderasyon Kayıtları</h2></div>
          <div className="table-wrapper">
            <table>
              <thead><tr><th>Kullanıcı</th><th>Tür</th><th>Durum</th></tr></thead>
              <tbody>
                {(reports?.recentModeration ?? []).map((row) => (
                  <tr key={`${row.userEmail}-${row.createdAt}`}>
                    <td>{row.userEmail}</td>
                    <td>{row.type}</td>
                    <td><span className={`badge ${getBadgeClass(row.status)}`}>{row.status}</span></td>
                  </tr>
                ))}
                {!loading && reports?.recentModeration.length === 0 && (
                  <tr><td colSpan={3}>Moderasyon kaydı bulunmuyor.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </article>
      </section>

      <p className="reports-updated-at">
        Son güncelleme:{" "}
        {reports
          ? new Date(reports.generatedAt).toLocaleString("tr-TR", {
              day: "2-digit",
              month: "2-digit",
              year: "numeric",
              hour: "2-digit",
              minute: "2-digit"
            })
          : "—"}
      </p>
    </div>
  );
}
