import {
  BookCheck,
  FileBadge2,
  ShieldAlert,
  UserCheck,
  Users
} from "lucide-react";
import { useEffect, useState } from "react";
import api from "../api/client";
import StatCard from "../components/StatCard";
import type { DashboardSummary } from "../types";

const emptySummary: DashboardSummary = {
  totalUsers: 0,
  activeUsers: 0,
  pendingVerifications: 0,
  pendingNoteReviews: 0,
  approvedNotes: 0,
  totalRevenue: 0,
  recentActivities: []
};

export default function DashboardPage() {
  const [summary, setSummary] =
    useState<DashboardSummary>(emptySummary);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get<DashboardSummary>("/admin/dashboard")
      .then(({ data }) => setSummary(data))
      .finally(() => setLoading(false));
  }, []);

  return (
    <>
      <div className="page-heading">
        <div>
          <span className="section-kicker">Genel görünüm</span>
          <h1>Operasyon özeti</h1>
          <p>
            Kullanıcı, doğrulama ve not moderasyonu metriklerinin güncel
            görünümü.
          </p>
        </div>
        <span className="live-indicator">
          <span />
          {loading ? "Veriler alınıyor" : "Sistem çevrimiçi"}
        </span>
      </div>

      <div className="stats-grid">
        <StatCard
          label="Toplam kullanıcı"
          value={summary.totalUsers}
          helper="Platform hesapları"
          icon={Users}
        />
        <StatCard
          label="Aktif kullanıcı"
          value={summary.activeUsers}
          helper="İşlem yapabilir hesaplar"
          icon={UserCheck}
        />
        <StatCard
          label="Bekleyen doğrulama"
          value={summary.pendingVerifications}
          helper="İnceleme gerektiriyor"
          icon={FileBadge2}
        />
        <StatCard
          label="Bekleyen not"
          value={summary.pendingNoteReviews}
          helper="AI veya manuel kontrol"
          icon={ShieldAlert}
        />
        <StatCard
          label="Onaylı not"
          value={summary.approvedNotes}
          helper="Satışa uygun içerik"
          icon={BookCheck}
        />
      </div>

      <div className="dashboard-grid">
        <article className="panel">
          <div className="panel__heading">
            <div>
              <span className="section-kicker">İş yükü</span>
              <h2>Moderasyon kuyruğu</h2>
            </div>
          </div>

          <div className="queue-list">
            <div>
              <span>Öğrenci doğrulamaları</span>
              <strong>{summary.pendingVerifications}</strong>
            </div>
            <div>
              <span>Not incelemeleri</span>
              <strong>{summary.pendingNoteReviews}</strong>
            </div>
            <div>
              <span>Onaylı içerikler</span>
              <strong>{summary.approvedNotes}</strong>
            </div>
          </div>
        </article>

        <article className="panel">
          <div className="panel__heading">
            <div>
              <span className="section-kicker">Denetim izi</span>
              <h2>Son yönetim işlemleri</h2>
            </div>
          </div>

          {summary.recentActivities.length === 0 ? (
            <div className="empty-state">
              İlk işlemden sonra audit kayıtları burada görünecek.
            </div>
          ) : (
            <div className="activity-list">
              {summary.recentActivities.map((item, index) => (
                <div key={`${item.entityId}-${index}`}>
                  <span className="activity-list__dot" />
                  <div>
                    <strong>{item.action}</strong>
                    <span>
                      {item.entityType} ·{" "}
                      {new Date(item.createdAt).toLocaleString("tr-TR")}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </article>
      </div>
    </>
  );
}
