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
import { useMemo, useState } from "react";

type TimeRange =
  | "daily"
  | "weekly"
  | "monthly"
  | "halfYear"
  | "yearly"
  | "all";

type ChartPoint = {
  label: string;
  value: number;
};

const timeRanges: Array<{ value: TimeRange; label: string }> = [
  { value: "daily", label: "Günlük" },
  { value: "weekly", label: "Haftalık" },
  { value: "monthly", label: "Aylık" },
  { value: "halfYear", label: "Yarım Yıllık" },
  { value: "yearly", label: "Yıllık" },
  { value: "all", label: "Tüm Zamanlar" }
];

const visitSeries: Record<TimeRange, ChartPoint[]> = {
  daily: [
    { label: "00:00", value: 150 },
    { label: "03:00", value: 120 },
    { label: "06:00", value: 180 },
    { label: "09:00", value: 520 },
    { label: "12:00", value: 980 },
    { label: "15:00", value: 1430 },
    { label: "18:00", value: 1470 },
    { label: "21:00", value: 1160 },
    { label: "23:00", value: 310 }
  ],
  weekly: [
    { label: "Pzt", value: 6240 },
    { label: "Sal", value: 7010 },
    { label: "Çar", value: 6840 },
    { label: "Per", value: 7620 },
    { label: "Cum", value: 8190 },
    { label: "Cmt", value: 9380 },
    { label: "Paz", value: 8760 }
  ],
  monthly: [
    { label: "1 Ağu", value: 1680 },
    { label: "4 Ağu", value: 1940 },
    { label: "7 Ağu", value: 2180 },
    { label: "10 Ağu", value: 2070 },
    { label: "13 Ağu", value: 2520 },
    { label: "16 Ağu", value: 2840 },
    { label: "19 Ağu", value: 3120 },
    { label: "22 Ağu", value: 2980 },
    { label: "25 Ağu", value: 3360 },
    { label: "28 Ağu", value: 3610 }
  ],
  halfYear: [
    { label: "Mar", value: 28600 },
    { label: "Nis", value: 32100 },
    { label: "May", value: 37800 },
    { label: "Haz", value: 42300 },
    { label: "Tem", value: 46100 },
    { label: "Ağu", value: 50400 }
  ],
  yearly: [
    { label: "Oca", value: 23100 },
    { label: "Şub", value: 24900 },
    { label: "Mar", value: 28600 },
    { label: "Nis", value: 32100 },
    { label: "May", value: 37800 },
    { label: "Haz", value: 42300 },
    { label: "Tem", value: 46100 },
    { label: "Ağu", value: 50400 },
    { label: "Eyl", value: 53600 },
    { label: "Eki", value: 57900 },
    { label: "Kas", value: 61200 },
    { label: "Ara", value: 65800 }
  ],
  all: [
    { label: "2022", value: 48200 },
    { label: "2023", value: 128400 },
    { label: "2024", value: 246700 },
    { label: "2025", value: 389100 },
    { label: "2026", value: 512800 }
  ]
};

const rangeTitles: Record<TimeRange, string> = {
  daily: "Saatlik Site Ziyaretleri",
  weekly: "Haftalık Site Ziyaretleri",
  monthly: "Aylık Site Ziyaretleri",
  halfYear: "Yarım Yıllık Site Ziyaretleri",
  yearly: "Yıllık Site Ziyaretleri",
  all: "Tüm Zamanlardaki Site Ziyaretleri"
};

const universities = [
  { name: "Marmara", value: 32, color: "#6c5ce7" },
  { name: "İTÜ", value: 24, color: "#14a873" },
  { name: "Boğaziçi", value: 18, color: "#2782e7" },
  { name: "YTÜ", value: 14, color: "#e89a18" },
  { name: "Hacettepe", value: 12, color: "#dc4b56" }
];

const faculties = [
  { name: "Mühendislik Fakültesi", count: 3420, rate: 27 },
  { name: "Fen-Edebiyat Fakültesi", count: 2680, rate: 21 },
  { name: "İktisat Fakültesi", count: 1940, rate: 16 },
  { name: "Hukuk Fakültesi", count: 1520, rate: 12 },
  { name: "Eğitim Fakültesi", count: 1310, rate: 10 }
];

const moderationRows = [
  {
    user: "ahmet.yilmaz@ogr.marmara.edu.tr",
    type: "İçerik Şikâyeti",
    status: "İncelemede",
    badge: "badge-warning"
  },
  {
    user: "zeynep.kara@itu.edu.tr",
    type: "Öğrenci Doğrulama",
    status: "Çözüldü",
    badge: "badge-success"
  },
  {
    user: "emre.dogan@boun.edu.tr",
    type: "İçerik Şikâyeti",
    status: "Çözüldü",
    badge: "badge-success"
  },
  {
    user: "sena.tas@yildiz.edu.tr",
    type: "Kullanıcı Raporu",
    status: "Bekliyor",
    badge: "badge-warning"
  },
  {
    user: "burak.acar@hacettepe.edu.tr",
    type: "Öğrenci Doğrulama",
    status: "İncelemede",
    badge: "badge-warning"
  }
];

const numberFormatter = new Intl.NumberFormat("tr-TR");
const currencyFormatter = new Intl.NumberFormat("tr-TR", {
  style: "currency",
  currency: "TRY",
  maximumFractionDigits: 0
});

function VisitLineChart({ points }: { points: ChartPoint[] }) {
  const width = 760;
  const height = 280;
  const padding = { top: 20, right: 18, bottom: 44, left: 58 };
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;
  const maxValue = Math.max(...points.map((point) => point.value), 1);
  const roundedMax = Math.ceil(maxValue / 4) * 4;

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
            <text
              x={point.x}
              y={height - 14}
              textAnchor="middle"
              className="reports-chart-axis-label"
            >
              {point.label}
            </text>
          </g>
        ))}
      </svg>
    </div>
  );
}

function UniversityDonut() {
  const gradient = universities
    .reduce(
      (result, item) => {
        const start = result.total;
        const end = start + item.value;
        result.parts.push(`${item.color} ${start}% ${end}%`);
        result.total = end;
        return result;
      },
      { parts: [] as string[], total: 0 }
    )
    .parts.join(", ");

  return (
    <div className="reports-donut-layout">
      <div
        className="reports-donut"
        style={{ background: `conic-gradient(${gradient})` }}
        role="img"
        aria-label="Üniversitelere göre üye dağılımı"
      >
        <div className="reports-donut__center">
          <strong>12.480</strong>
          <span>üye</span>
        </div>
      </div>

      <div className="reports-donut-legend">
        {universities.map((item) => (
          <div key={item.name}>
            <span
              className="reports-legend-dot"
              style={{ background: item.color }}
            />
            <span>{item.name}</span>
            <strong>%{item.value}</strong>
          </div>
        ))}
      </div>
    </div>
  );
}

export default function ReportsPage() {
  const [timeRange, setTimeRange] = useState<TimeRange>("monthly");
  const [university, setUniversity] = useState("all");
  const [refreshing, setRefreshing] = useState(false);
  const [lastUpdated, setLastUpdated] = useState(new Date());

  const chartPoints = useMemo(() => visitSeries[timeRange], [timeRange]);

  const refreshData = () => {
    setRefreshing(true);
    window.setTimeout(() => {
      setLastUpdated(new Date());
      setRefreshing(false);
    }, 650);
  };

  const exportReport = () => {
    const rows = [
      ["NotMarket Raporu"],
      ["Zaman Aralığı", timeRanges.find((item) => item.value === timeRange)?.label],
      ["Üniversite", university === "all" ? "Tüm Üniversiteler" : university],
      [],
      ["Fakülte", "Üye Sayısı", "Oran"],
      ...faculties.map((item) => [item.name, item.count, `%${item.rate}`])
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
              value={university}
              onChange={(event) => setUniversity(event.target.value)}
              aria-label="Üniversite filtresi"
            >
              <option value="all">Tüm Üniversiteler</option>
              <option value="Marmara Üniversitesi">Marmara Üniversitesi</option>
              <option value="İstanbul Teknik Üniversitesi">İTÜ</option>
              <option value="Boğaziçi Üniversitesi">Boğaziçi Üniversitesi</option>
              <option value="Yıldız Teknik Üniversitesi">YTÜ</option>
              <option value="Hacettepe Üniversitesi">Hacettepe Üniversitesi</option>
            </select>
          </label>

          <button
            type="button"
            className="secondary-button"
            onClick={refreshData}
            disabled={refreshing}
          >
            <RefreshCw size={17} className={refreshing ? "is-spinning" : ""} />
            {refreshing ? "Güncelleniyor" : "Verileri Güncelle"}
          </button>

          <button type="button" className="primary-button" onClick={exportReport}>
            <Download size={17} />
            Raporu Dışa Aktar
          </button>
        </div>
      </div>

      <section className="reports-stats" aria-label="Rapor özeti">
        <article className="report-stat-card report-stat-card--purple">
          <Users size={21} />
          <div><span>Toplam Kullanıcı</span><strong>12.480</strong></div>
        </article>
        <article className="report-stat-card report-stat-card--green">
          <ShieldCheck size={21} />
          <div><span>Doğrulanmış Öğrenci</span><strong>8.942</strong></div>
        </article>
        <article className="report-stat-card report-stat-card--amber">
          <Clock3 size={21} />
          <div><span>Bekleyen Başvuru</span><strong>184</strong></div>
        </article>
        <article className="report-stat-card report-stat-card--green">
          <ShoppingCart size={21} />
          <div><span>Toplam Satış</span><strong>{currencyFormatter.format(486250)}</strong></div>
        </article>
        <article className="report-stat-card report-stat-card--purple">
          <WalletCards size={21} />
          <div><span>Platform Geliri</span><strong>{currencyFormatter.format(58350)}</strong></div>
        </article>
        <article className="report-stat-card report-stat-card--red">
          <TriangleAlert size={21} />
          <div><span>Açık Şikâyet</span><strong>23</strong></div>
        </article>
      </section>

      <section className="reports-range-filter" aria-label="Grafik zaman aralığı">
        <div className="reports-range-filter__label">
          <CalendarRange size={18} />
          <strong>Grafik Zaman Aralığı</strong>
        </div>
        <div className="reports-range-filter__options">
          {timeRanges.map((item) => (
            <button
              key={item.value}
              type="button"
              className={timeRange === item.value ? "is-active" : ""}
              onClick={() => setTimeRange(item.value)}
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
              <span className="reports-chart-legend">
                <i /> Ziyaret Sayısı
              </span>
            </div>
          </div>
          <VisitLineChart points={chartPoints} />
        </article>

        <article className="panel reports-chart-panel">
          <div className="reports-panel-heading">
            <h2>Üniversitelere Göre Üye Dağılımı</h2>
          </div>
          <UniversityDonut />
        </article>
      </section>

      <section className="reports-table-grid">
        <article className="panel reports-table-panel">
          <div className="reports-panel-heading">
            <h2>Fakültelere Göre Üye Dağılımı</h2>
          </div>
          <div className="table-wrapper">
            <table>
              <thead><tr><th>Fakülte</th><th>Üye Sayısı</th><th>Oran</th></tr></thead>
              <tbody>
                {faculties.map((faculty) => (
                  <tr key={faculty.name}>
                    <td>{faculty.name}</td>
                    <td>{numberFormatter.format(faculty.count)}</td>
                    <td>
                      <div className="reports-rate-cell">
                        <span>%{faculty.rate}</span>
                        <i><b style={{ width: `${faculty.rate * 2.5}%` }} /></i>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </article>

        <article className="panel reports-table-panel">
          <div className="reports-panel-heading">
            <h2>Son Moderasyon Kayıtları</h2>
          </div>
          <div className="table-wrapper">
            <table>
              <thead><tr><th>Kullanıcı</th><th>Tür</th><th>Durum</th></tr></thead>
              <tbody>
                {moderationRows.map((row) => (
                  <tr key={`${row.user}-${row.type}`}>
                    <td>{row.user}</td>
                    <td>{row.type}</td>
                    <td><span className={`badge ${row.badge}`}>{row.status}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </article>
      </section>

      <p className="reports-updated-at">
        Son güncelleme: {lastUpdated.toLocaleString("tr-TR", {
          day: "2-digit",
          month: "2-digit",
          year: "numeric",
          hour: "2-digit",
          minute: "2-digit"
        })}
      </p>
    </div>
  );
}
