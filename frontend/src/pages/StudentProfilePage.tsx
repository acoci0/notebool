import {
  CheckCircle2,
  Clock3,
  GraduationCap,
  Plus,
  Trash2,
  XCircle,
} from "lucide-react";

import {
  useCallback,
  useEffect,
  useState,
  type FormEvent,
} from "react";

import studentApi from "../api/studentClient";

import { useStudentAuth } from "../auth/StudentAuthContext";

import type {
  StudentVerificationItem,
} from "../types";

export default function StudentProfilePage() {
  const {
    student,
    logout,
  } = useStudentAuth();

  const [items, setItems] =
    useState<StudentVerificationItem[]>([]);

  const [loading, setLoading] =
    useState(true);

  const [showForm, setShowForm] =
    useState(false);

  const [submitting, setSubmitting] =
    useState(false);

  const [deletingId, setDeletingId] =
    useState<string | null>(null);

  const [message, setMessage] =
    useState("");

  const [universityName, setUniversityName] =
    useState("");

  const [facultyName, setFacultyName] =
    useState("");

  const [departmentName, setDepartmentName] =
    useState("");

  const [
    documentIssueDate,
    setDocumentIssueDate,
  ] = useState("");

  const [document, setDocument] =
    useState<File | null>(null);

  /*
   * Öğrencinin mevcut doğrulamalarını getirir.
   */
  const load = useCallback(async () => {
    setLoading(true);

    try {
      const { data } =
        await studentApi.get<
          StudentVerificationItem[]
        >(
          "/student/verifications"
        );

      setItems(data);
    } catch {
      setMessage(
        "Doğrulamalar yüklenirken bir hata oluştu."
      );
    } finally {
      setLoading(false);
    }
  }, []);

  /*
   * Sayfa açıldığında doğrulamaları yükle.
   */
  useEffect(() => {
    void load();
  }, [load]);

  /*
   * Yeni öğrenci doğrulaması gönderir.
   */
  const submit = async (
    event: FormEvent
  ) => {
    event.preventDefault();

    if (!document) {
      setMessage(
        "Öğrenci belgesi PDF dosyasını seçmelisiniz."
      );

      return;
    }

    setSubmitting(true);
    setMessage("");

    try {
      const formData =
        new FormData();

      formData.append(
        "UniversityName",
        universityName
      );

      formData.append(
        "FacultyName",
        facultyName
      );

      formData.append(
        "DepartmentName",
        departmentName
      );

      formData.append(
        "DocumentIssueDate",
        documentIssueDate
      );

      formData.append(
        "Document",
        document
      );

      await studentApi.post(
        "/student/verifications",
        formData
      );

      /*
       * Form alanlarını temizle.
       */
      setUniversityName("");
      setFacultyName("");
      setDepartmentName("");
      setDocumentIssueDate("");
      setDocument(null);

      setShowForm(false);

      setMessage(
        "Öğrenci belgeniz incelemeye gönderildi."
      );

      await load();
    } catch (error: any) {
      setMessage(
        error.response?.data?.message ??
          "Belge yüklenirken hata oluştu."
      );
    } finally {
      setSubmitting(false);
    }
  };

  /*
   * Pending doğrulama başvurusunu siler.
   */
  const deleteVerification = async (
    item: StudentVerificationItem
  ) => {
    /*
     * Frontend seviyesinde de yalnızca
     * Pending kayıtların silinmesine izin ver.
     */
    if (item.status !== "Pending") {
      setMessage(
        "Yalnızca inceleme bekleyen doğrulamalar silinebilir."
      );

      return;
    }

    /*
     * Kullanıcıdan ikinci kez onay alınır.
     */
    const confirmed =
      window.confirm(
        `"${item.universityName} - ${item.departmentName}" doğrulamasını silmek istediğinize emin misiniz?\n\nBu işlem geri alınamaz.`
      );

    if (!confirmed) {
      return;
    }

    setDeletingId(item.id);
    setMessage("");

    try {
      await studentApi.delete(
        `/student/verifications/${item.id}`
      );

      setMessage(
        "Doğrulama başvurunuz başarıyla silindi."
      );

      /*
       * Silme işleminden sonra listeyi
       * backend'den tekrar getir.
       */
      await load();
    } catch (error: any) {
      setMessage(
        error.response?.data?.message ??
          "Doğrulama silinirken hata oluştu."
      );
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <div className="student-profile-page">
      <header className="student-profile-header">
        <div>
          <span className="section-kicker">
            ÖĞRENCİ HESABI
          </span>

          <h1>
            Hoş geldin,{" "}
            {student?.displayName}
          </h1>

          <p>
            Üniversite ve bölüm
            doğrulamalarınızı buradan
            yönetebilirsiniz.
          </p>
        </div>

        <button
          className="secondary-button"
          type="button"
          onClick={logout}
        >
          Çıkış yap
        </button>
      </header>

      {message && (
        <div className="student-message">
          {message}
        </div>
      )}

      <section className="student-profile-section">
        <div className="student-section-heading">
          <div>
            <h2>
              Üniversite doğrulamalarım
            </h2>

            <p>
              Onaylanan her bölüm
              NotMarket içerisinde ayrı
              akademik yetki sağlar.
            </p>
          </div>

          <button
            className="primary-button"
            type="button"
            onClick={() =>
              setShowForm(
                (value) => !value
              )
            }
          >
            <Plus size={17} />

            Yeni üniversite ekle
          </button>
        </div>

        {/* Yeni doğrulama formu */}

        {showForm && (
          <form
            className="student-verification-form"
            onSubmit={submit}
          >
            <h3>
              Yeni öğrenci doğrulaması
            </h3>

            <div className="student-form-grid">
              <label>
                Üniversite

                <input
                  value={universityName}
                  onChange={(event) =>
                    setUniversityName(
                      event.target.value
                    )
                  }
                  required
                />
              </label>

              <label>
                Fakülte

                <input
                  value={facultyName}
                  onChange={(event) =>
                    setFacultyName(
                      event.target.value
                    )
                  }
                  required
                />
              </label>

              <label>
                Bölüm

                <input
                  value={departmentName}
                  onChange={(event) =>
                    setDepartmentName(
                      event.target.value
                    )
                  }
                  required
                />
              </label>

              <label>
                Belge tarihi

                <input
                  type="date"
                  value={documentIssueDate}
                  onChange={(event) =>
                    setDocumentIssueDate(
                      event.target.value
                    )
                  }
                  required
                />
              </label>
            </div>

            <label className="student-file-field">
              Öğrenci belgesi

              <input
                type="file"
                accept="application/pdf,.pdf"
                onChange={(event) =>
                  setDocument(
                    event.target.files?.[0] ??
                      null
                  )
                }
                required
              />

              <small>
                Yalnızca PDF • Maksimum
                10 MB • Son 30 gün içinde
                alınmış belge
              </small>
            </label>

            <div className="student-form-actions">
              <button
                type="button"
                className="secondary-button"
                disabled={submitting}
                onClick={() =>
                  setShowForm(false)
                }
              >
                Vazgeç
              </button>

              <button
                type="submit"
                className="primary-button"
                disabled={submitting}
              >
                {submitting
                  ? "Gönderiliyor..."
                  : "İncelemeye gönder"}
              </button>
            </div>
          </form>
        )}

        {/* Doğrulama listesi */}

        <div className="student-verification-list">
          {loading && (
            <div className="empty-state">
              Doğrulamalar yükleniyor...
            </div>
          )}

          {!loading &&
            items.length === 0 && (
              <div className="empty-state">
                Henüz öğrenci
                doğrulamanız yok.
              </div>
            )}

          {!loading &&
            items.map((item) => (
              <VerificationCard
                key={item.id}
                item={item}
                deleting={
                  deletingId === item.id
                }
                onDelete={
                  deleteVerification
                }
              />
            ))}
        </div>
      </section>
    </div>
  );
}

function VerificationCard({
  item,
  deleting,
  onDelete,
}: {
  item: StudentVerificationItem;
  deleting: boolean;
  onDelete: (
    item: StudentVerificationItem
  ) => Promise<void>;
}) {
  return (
    <article className="student-verification-card">
      <div className="student-verification-icon">
        <GraduationCap
          size={22}
        />
      </div>

      <div className="student-verification-main">
        <div className="student-verification-title">
          <div>
            <h3>
              {item.universityName}
            </h3>

            <p>
              {item.facultyName}
              {" • "}
              {item.departmentName}
            </p>
          </div>

          <StudentVerificationStatus
            status={item.status}
          />
        </div>

        <div className="student-verification-meta">
          <span>
            Belge tarihi:{" "}
            <strong>
              {formatDate(
                item.documentIssueDate
              )}
            </strong>
          </span>

          <span>
            Geçerlilik:{" "}
            <strong>
              {item.expiresAt
                ? formatDate(
                    item.expiresAt
                  )
                : "Henüz yok"}
            </strong>
          </span>
        </div>

        {/* Pending kayıt silme */}

        {item.status === "Pending" && (
          <div className="student-verification-card__actions">
            <button
              type="button"
              className="student-delete-button"
              disabled={deleting}
              onClick={() =>
                void onDelete(item)
              }
            >
              <Trash2 size={16} />

              {deleting
                ? "Siliniyor..."
                : "Doğrulamayı sil"}
            </button>
          </div>
        )}

        {/* Admin tarafından reddedilmiş kayıt */}

        {item.status === "Rejected" &&
          item.reviewNote && (
            <div className="student-rejection-note">
              <strong>
                Ret gerekçesi
              </strong>

              <p>
                {item.reviewNote}
              </p>
            </div>
          )}
      </div>
    </article>
  );
}

function StudentVerificationStatus({
  status,
}: {
  status: string;
}) {
  if (status === "Approved") {
    return (
      <span className="student-status student-status--approved">
        <CheckCircle2
          size={15}
        />

        Doğrulandı
      </span>
    );
  }

  if (status === "Rejected") {
    return (
      <span className="student-status student-status--rejected">
        <XCircle
          size={15}
        />

        Reddedildi
      </span>
    );
  }

  return (
    <span className="student-status student-status--pending">
      <Clock3 size={15} />

      İncelemede
    </span>
  );
}

function formatDate(
  value: string
) {
  return new Date(
    value
  ).toLocaleDateString(
    "tr-TR"
  );
}
