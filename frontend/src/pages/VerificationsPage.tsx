import {
  Check,
  ExternalLink,
  Eye,
  RefreshCw,
  X,
} from "lucide-react";

import {
  useCallback,
  useEffect,
  useState,
} from "react";

import { isAxiosError } from "axios";

import api from "../api/client";
import StatusBadge from "../components/StatusBadge";

import type {
  Verification,
  VerificationDetail,
} from "../types";

type Filter =
  | "All"
  | "Pending"
  | "Approved"
  | "Rejected";

type ApiErrorResponse = {
  message?: string;
};

export default function VerificationsPage() {
  const [items, setItems] =
    useState<Verification[]>([]);

  const [filter, setFilter] =
    useState<Filter>("Pending");

  const [loading, setLoading] =
    useState(false);

  const [selected, setSelected] =
    useState<VerificationDetail | null>(null);

  const [documentUrl, setDocumentUrl] =
    useState<string | null>(null);

  const [documentLoading, setDocumentLoading] =
    useState(false);

  const [documentError, setDocumentError] =
    useState<string | null>(null);

  const [reviewNote, setReviewNote] =
    useState("");

  const [decisionLoading, setDecisionLoading] =
    useState(false);

  /*
   * Doğrulama kayıtlarını backend'den getirir.
   */
  const load = useCallback(async () => {
    setLoading(true);

    try {
      const query =
        filter === "All"
          ? ""
          : `?status=${filter}`;

      const { data } =
        await api.get<Verification[]>(
          `/admin/verifications${query}`
        );

      setItems(data);
    } catch (error: unknown) {
      console.error(
        "Doğrulamalar yüklenemedi:",
        error
      );
    } finally {
      setLoading(false);
    }
  }, [filter]);

  /*
   * Filtre değiştiğinde listeyi yeniden yükler.
   */
  useEffect(() => {
    void load();
  }, [load]);

  /*
   * Component kapanırken oluşturulmuş Blob URL'i
   * bellekte bırakmamak için temizler.
   */
  useEffect(() => {
    return () => {
      if (documentUrl) {
        URL.revokeObjectURL(documentUrl);
      }
    };
  }, [documentUrl]);

  /*
   * Doğrulama detayını ve PDF belgesini getirir.
   */
  const openDetail = async (id: string) => {
    setDocumentLoading(true);
    setDocumentError(null);

    /*
     * Önce eski PDF URL'ini temizle.
     */
    if (documentUrl) {
      URL.revokeObjectURL(documentUrl);
      setDocumentUrl(null);
    }

    try {
      /*
       * 1. Önce doğrulama detayını getir.
       *
       * PDF yüklenemese bile modalın açılabilmesi için
       * bunu belge isteğinden ayrı yapıyoruz.
       */
      const detailResponse =
        await api.get<VerificationDetail>(
          `/admin/verifications/${id}`
        );

      setSelected(detailResponse.data);

      setReviewNote(
        detailResponse.data.reviewNote ?? ""
      );

      /*
       * 2. Private PDF dosyasını JWT ile backend'den çek.
       */
      try {
        const documentResponse =
          await api.get(
            `/admin/verifications/${id}/document`,
            {
              responseType: "blob",
            }
          );

        const blob = new Blob(
          [documentResponse.data],
          {
            type: "application/pdf",
          }
        );

        const url =
          URL.createObjectURL(blob);

        setDocumentUrl(url);
      } catch (error: unknown) {
        console.error(
          "PDF yükleme hatası:",
          error
        );

        setDocumentError(
          getErrorMessage(
            error,
            "Öğrenci belgesi görüntülenemedi."
          )
        );
      }
    } catch (error: unknown) {
      console.error(
        "Doğrulama detayı yüklenemedi:",
        error
      );

      setDocumentError(
        getErrorMessage(
          error,
          "Doğrulama bilgileri yüklenemedi."
        )
      );
    } finally {
      setDocumentLoading(false);
    }
  };

  /*
   * Modalı kapatır.
   */
  const closeDetail = () => {
    if (documentUrl) {
      URL.revokeObjectURL(documentUrl);
    }

    setDocumentUrl(null);
    setDocumentError(null);
    setSelected(null);
    setReviewNote("");
  };

  /*
   * Admin onay / ret işlemi.
   */
  const decide = async (
    approve: boolean
  ) => {
    if (!selected) {
      return;
    }

    /*
     * Ret durumunda açıklama zorunlu.
     */
    if (
      !approve &&
      !reviewNote.trim()
    ) {
      alert(
        "Ret işlemi için gerekçe yazmalısınız."
      );

      return;
    }

    setDecisionLoading(true);

    try {
      await api.post(
        `/admin/verifications/${selected.id}/decision`,
        {
          approve,
          reviewNote:
            reviewNote.trim() || null,
        }
      );

      closeDetail();

      await load();
    } catch (error: unknown) {
      alert(
        getErrorMessage(
          error,
          "İşlem sırasında hata oluştu."
        )
      );
    } finally {
      setDecisionLoading(false);
    }
  };

  /*
   * Blob PDF'i yeni sekmede açar.
   */
  const openDocumentInNewTab = () => {
    if (!documentUrl) {
      return;
    }

    window.open(
      documentUrl,
      "_blank",
      "noopener,noreferrer"
    );
  };

  return (
    <>
      <div className="page-heading">
        <div>
          <span className="section-kicker">
            Kimlik ve akademik yetki
          </span>

          <h1>
            Öğrenci doğrulamaları
          </h1>

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

          {loading
            ? "Yükleniyor..."
            : "Yenile"}
        </button>
      </div>

      {/* Filtreler */}

      <div className="verification-filters">
        {(
          [
            ["Pending", "Bekleyen"],
            ["Approved", "Onaylanan"],
            ["Rejected", "Reddedilen"],
            ["All", "Tümü"],
          ] as Array<
            [Filter, string]
          >
        ).map(
          ([value, label]) => (
            <button
              key={value}
              type="button"
              className={
                filter === value
                  ? "filter-button filter-button--active"
                  : "filter-button"
              }
              onClick={() =>
                setFilter(value)
              }
            >
              {label}
            </button>
          )
        )}
      </div>

      {/* Doğrulama tablosu */}

      <article className="panel">
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Kullanıcı</th>

                <th>Üniversite</th>

                <th>
                  Fakülte / Bölüm
                </th>

                <th>
                  Belge tarihi
                </th>

                <th>
                  Geçerlilik
                </th>

                <th>Durum</th>

                <th />
              </tr>
            </thead>

            <tbody>
              {loading && (
                <tr>
                  <td colSpan={7}>
                    <div className="empty-state">
                      Doğrulamalar
                      yükleniyor...
                    </div>
                  </td>
                </tr>
              )}

              {!loading &&
                items.length === 0 && (
                  <tr>
                    <td colSpan={7}>
                      <div className="empty-state">
                        Bu filtreye ait
                        doğrulama kaydı yok.
                      </div>
                    </td>
                  </tr>
                )}

              {!loading &&
                items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>
                        {
                          item.userDisplayName
                        }
                      </strong>

                      <span className="table-subtext">
                        {item.userEmail}
                      </span>
                    </td>

                    <td>
                      <strong>
                        {
                          item.universityName
                        }
                      </strong>
                    </td>

                    <td>
                      {
                        item.facultyName
                      }

                      <span className="table-subtext">
                        {
                          item.departmentName
                        }
                      </span>
                    </td>

                    <td>
                      {formatDate(
                        item.documentIssueDate
                      )}
                    </td>

                    <td>
                      {item.expiresAt
                        ? formatDate(
                            item.expiresAt
                          )
                        : "—"}
                    </td>

                    <td>
                      <StatusBadge
                        status={
                          item.status
                        }
                      />
                    </td>

                    <td>
                      <button
                        className="icon-link"
                        type="button"
                        onClick={() =>
                          void openDetail(
                            item.id
                          )
                        }
                      >
                        <Eye
                          size={17}
                        />

                        İncele
                      </button>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </article>

      {/* Doğrulama detay modalı */}

      {selected && (
        <div
          className="modal-backdrop"
          onMouseDown={
            closeDetail
          }
        >
          <section
            className="verification-modal"
            onMouseDown={(
              event
            ) =>
              event.stopPropagation()
            }
          >
            {/* Modal başlık */}

            <div className="verification-modal__header">
              <div>
                <span className="section-kicker">
                  Doğrulama detayı
                </span>

                <h2>
                  {
                    selected.userDisplayName
                  }
                </h2>

                <p>
                  {
                    selected.userEmail
                  }
                </p>
              </div>

              <button
                className="modal-close"
                type="button"
                onClick={
                  closeDetail
                }
                aria-label="Pencereyi kapat"
              >
                <X size={19} />
              </button>
            </div>

            {/* Akademik bilgiler */}

            <div className="verification-detail-grid">
              <Detail
                label="Üniversite"
                value={
                  selected.universityName
                }
              />

              <Detail
                label="Fakülte"
                value={
                  selected.facultyName
                }
              />

              <Detail
                label="Bölüm"
                value={
                  selected.departmentName
                }
              />

              <Detail
                label="Belge tarihi"
                value={formatDate(
                  selected.documentIssueDate
                )}
              />

              <Detail
                label="Durum"
                value={
                  selected.status
                }
              />

              <Detail
                label="Son geçerlilik"
                value={
                  selected.expiresAt
                    ? formatDate(
                        selected.expiresAt
                      )
                    : "Henüz belirlenmedi"
                }
              />
            </div>

            {/* PDF görüntüleyici */}

            <div className="verification-document">
              <div className="verification-document__header">
                <div>
                  <strong>
                    Öğrenci belgesi
                  </strong>

                  <span>
                    Admin erişimine özel
                    PDF önizleme
                  </span>
                </div>

                {documentUrl && (
                  <button
                    type="button"
                    className="secondary-button"
                    onClick={
                      openDocumentInNewTab
                    }
                  >
                    <ExternalLink
                      size={16}
                    />

                    Yeni sekmede aç
                  </button>
                )}
              </div>

              {documentLoading && (
                <div className="document-state">
                  PDF yükleniyor...
                </div>
              )}

              {!documentLoading &&
                documentError && (
                  <div className="document-error">
                    {
                      documentError
                    }
                  </div>
                )}

              {!documentLoading &&
                !documentError &&
                documentUrl && (
                  <iframe
                    className="verification-document__viewer"
                    src={
                      documentUrl
                    }
                    title="Öğrenci Belgesi"
                  />
                )}

              {!documentLoading &&
                !documentError &&
                !documentUrl && (
                  <div className="document-state">
                    PDF bulunamadı.
                  </div>
                )}
            </div>

            {/* Pending ise admin aksiyonları */}

            {selected.status ===
              "Pending" && (
              <>
                <label className="review-field">
                  <span>
                    İnceleme notu /
                    ret gerekçesi
                  </span>

                  <textarea
                    rows={4}
                    value={
                      reviewNote
                    }
                    maxLength={600}
                    onChange={(
                      event
                    ) =>
                      setReviewNote(
                        event.target
                          .value
                      )
                    }
                    placeholder="Onay için isteğe bağlıdır. Ret durumunda zorunludur."
                  />

                  <small>
                    {
                      reviewNote.length
                    }
                    /600
                  </small>
                </label>

                <div className="verification-actions">
                  <button
                    className="danger-button"
                    type="button"
                    disabled={
                      decisionLoading
                    }
                    onClick={() =>
                      void decide(
                        false
                      )
                    }
                  >
                    <X size={17} />

                    {decisionLoading
                      ? "İşleniyor..."
                      : "Reddet"}
                  </button>

                  <button
                    className="approve-button"
                    type="button"
                    disabled={
                      decisionLoading
                    }
                    onClick={() =>
                      void decide(
                        true
                      )
                    }
                  >
                    <Check
                      size={17}
                    />

                    {decisionLoading
                      ? "İşleniyor..."
                      : "Öğrenciyi doğrula"}
                  </button>
                </div>
              </>
            )}

            {/* Daha önce incelenmiş kayıt */}

            {selected.status !==
              "Pending" &&
              selected.reviewNote && (
                <div className="review-result">
                  <strong>
                    Admin inceleme notu
                  </strong>

                  <p>
                    {
                      selected.reviewNote
                    }
                  </p>
                </div>
              )}
          </section>
        </div>
      )}
    </>
  );
}

/*
 * Detay kartı.
 */
function Detail({
  label,
  value,
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

/*
 * API'den gelen tarihleri Türkçe formatlar.
 */
function formatDate(
  value: string
): string {
  const date =
    new Date(value);

  if (
    Number.isNaN(
      date.getTime()
    )
  ) {
    return value;
  }

  return date.toLocaleDateString(
    "tr-TR"
  );
}

/*
 * Axios hatalarından backend mesajını güvenli şekilde çıkarır.
 */
function getErrorMessage(
  error: unknown,
  fallback: string
): string {
  if (
    isAxiosError<ApiErrorResponse>(
      error
    )
  ) {
    return (
      error.response?.data
        ?.message ??
      fallback
    );
  }

  return fallback;
}