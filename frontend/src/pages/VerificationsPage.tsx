import {
  Check,
  Eye,
  RefreshCw,
  X
} from "lucide-react";
import {
  useCallback,
  useEffect,
  useState
} from "react";

import api from "../api/client";
import StatusBadge from "../components/StatusBadge";

import type {
  Verification,
  VerificationDetail
} from "../types";

type Filter =
  | "All"
  | "Pending"
  | "Approved"
  | "Rejected";

export default function VerificationsPage() {
  const [items, setItems] = useState<Verification[]>([]);
  const [filter, setFilter] = useState<Filter>("Pending");
  const [loading, setLoading] = useState(false);

  const [selected, setSelected] =
    useState<VerificationDetail | null>(null);

  const [reviewNote, setReviewNote] = useState("");
  const [decisionLoading, setDecisionLoading] =
    useState(false);

  const load = useCallback(async () => {
    setLoading(true);

    try {
      const query =
        filter === "All"
          ? ""
          : `?status=${filter}`;

      const { data } = await api.get<Verification[]>(
        `/admin/verifications${query}`
      );

      setItems(data);
    } finally {
      setLoading(false);
    }
  }, [filter]);

  useEffect(() => {
    void load();
  }, [load]);

  const openDetail = async (id: string) => {
    const { data } =
      await api.get<VerificationDetail>(
        `/admin/verifications/${id}`
      );

    setSelected(data);
    setReviewNote(data.reviewNote ?? "");
  };

  const closeDetail = () => {
    setSelected(null);
    setReviewNote("");
  };

  const decide = async (approve: boolean) => {
    if (!selected) {
      return;
    }

    if (!approve && !reviewNote.trim()) {
      alert("Ret işlemi için gerekçe yazmalısınız.");
      return;
    }

    setDecisionLoading(true);

    try {
      await api.post(
        `/admin/verifications/${selected.id}/decision`,
        {
          approve,
          reviewNote: reviewNote.trim() || null
        }
      );

      closeDetail();
      await load();
    } catch (error: any) {
      alert(
        error.response?.data?.message ??
          "İşlem sırasında hata oluştu."
      );
    } finally {
      setDecisionLoading(false);
    }
  };

  return (
    <>
      <div className="page-heading">
        <div>
          <span className="section-kicker">
            Kimlik ve akademik yetki
          </span>

          <h1>Öğrenci doğrulamaları</h1>

          <p>
            Kullanıcıların üniversite ve bölüm
            yetkilerini inceleyin.
          </p>
        </div>

        <button
          className="secondary-button"
          type="button"
          onClick={() => void load()}
          disabled={loading}
        >
          <RefreshCw size={17} />
          Yenile
        </button>
      </div>

      <div className="verification-filters">
        {(
          [
            ["Pending", "Bekleyen"],
            ["Approved", "Onaylanan"],
            ["Rejected", "Reddedilen"],
            ["All", "Tümü"]
          ] as Array<[Filter, string]>
        ).map(([value, label]) => (
          <button
            key={value}
            type="button"
            className={
              filter === value
                ? "filter-button filter-button--active"
                : "filter-button"
            }
            onClick={() => setFilter(value)}
          >
            {label}
          </button>
        ))}
      </div>

      <article className="panel">
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Kullanıcı</th>
                <th>Üniversite</th>
                <th>Fakülte / Bölüm</th>
                <th>Belge tarihi</th>
                <th>Geçerlilik</th>
                <th>Durum</th>
                <th />
              </tr>
            </thead>

            <tbody>
              {items.length === 0 && !loading && (
                <tr>
                  <td colSpan={7}>
                    <div className="empty-state">
                      Bu filtreye ait doğrulama kaydı yok.
                    </div>
                  </td>
                </tr>
              )}

              {items.map((item) => (
                <tr key={item.id}>
                  <td>
                    <strong>
                      {item.userDisplayName}
                    </strong>

                    <span className="table-subtext">
                      {item.userEmail}
                    </span>
                  </td>

                  <td>
                    <strong>
                      {item.universityName}
                    </strong>
                  </td>

                  <td>
                    {item.facultyName}

                    <span className="table-subtext">
                      {item.departmentName}
                    </span>
                  </td>

                  <td>
                    {new Date(
                      item.documentIssueDate
                    ).toLocaleDateString("tr-TR")}
                  </td>

                  <td>
                    {item.expiresAt
                      ? new Date(
                          item.expiresAt
                        ).toLocaleDateString("tr-TR")
                      : "—"}
                  </td>

                  <td>
                    <StatusBadge
                      status={item.status}
                    />
                  </td>

                  <td>
                    <button
                      className="icon-link"
                      type="button"
                      onClick={() =>
                        void openDetail(item.id)
                      }
                    >
                      <Eye size={17} />
                      İncele
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </article>

      {selected && (
        <div
          className="modal-backdrop"
          onMouseDown={closeDetail}
        >
          <section
            className="verification-modal"
            onMouseDown={(event) =>
              event.stopPropagation()
            }
          >
            <div className="verification-modal__header">
              <div>
                <span className="section-kicker">
                  Doğrulama detayı
                </span>

                <h2>
                  {selected.userDisplayName}
                </h2>

                <p>{selected.userEmail}</p>
              </div>

              <button
                className="modal-close"
                type="button"
                onClick={closeDetail}
              >
                <X size={19} />
              </button>
            </div>

            <div className="verification-detail-grid">
              <Detail
                label="Üniversite"
                value={selected.universityName}
              />

              <Detail
                label="Fakülte"
                value={selected.facultyName}
              />

              <Detail
                label="Bölüm"
                value={selected.departmentName}
              />

              <Detail
                label="Belge tarihi"
                value={new Date(
                  selected.documentIssueDate
                ).toLocaleDateString("tr-TR")}
              />

              <Detail
                label="Durum"
                value={selected.status}
              />

              <Detail
                label="Son geçerlilik"
                value={
                  selected.expiresAt
                    ? new Date(
                        selected.expiresAt
                      ).toLocaleDateString("tr-TR")
                    : "Henüz belirlenmedi"
                }
              />
            </div>

            <div className="document-preview-placeholder">
              <strong>Öğrenci belgesi</strong>

              <span>
                {selected.documentBlobPath}
              </span>

              <small>
                Gerçek PDF görüntüleyiciyi bir sonraki
                geliştirme aşamasında bağlayacağız.
              </small>
            </div>

            {selected.status === "Pending" && (
              <>
                <label className="review-field">
                  <span>
                    İnceleme notu / ret gerekçesi
                  </span>

                  <textarea
                    rows={4}
                    value={reviewNote}
                    maxLength={600}
                    onChange={(event) =>
                      setReviewNote(
                        event.target.value
                      )
                    }
                    placeholder="Onay için isteğe bağlıdır. Ret durumunda zorunludur."
                  />

                  <small>
                    {reviewNote.length}/600
                  </small>
                </label>

                <div className="verification-actions">
                  <button
                    className="danger-button"
                    type="button"
                    disabled={decisionLoading}
                    onClick={() =>
                      void decide(false)
                    }
                  >
                    <X size={17} />
                    Reddet
                  </button>

                  <button
                    className="approve-button"
                    type="button"
                    disabled={decisionLoading}
                    onClick={() =>
                      void decide(true)
                    }
                  >
                    <Check size={17} />
                    Öğrenciyi doğrula
                  </button>
                </div>
              </>
            )}

            {selected.status !== "Pending" &&
              selected.reviewNote && (
                <div className="review-result">
                  <strong>
                    Admin inceleme notu
                  </strong>

                  <p>{selected.reviewNote}</p>
                </div>
              )}
          </section>
        </div>
      )}
    </>
  );
}

function Detail({
  label,
  value
}: {
  label: string;
  value: string;
}) {
  return (
    <div className="detail-item">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}