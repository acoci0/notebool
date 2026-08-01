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
  useRef,
  useState,
  type FormEvent,
  type KeyboardEvent,
} from "react";

import { isAxiosError } from "axios";

import studentApi from "../api/studentClient";

import { useStudentAuth } from "../auth/StudentAuthContext";

import type {
  AcademicUniversity,
  StudentVerificationItem,
} from "../types";

type ApiErrorResponse = {
  message?: string;
};

const MAX_FILE_SIZE =
  10 * 1024 * 1024;

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

  /*
   * Üniversite autocomplete state'leri.
   */
  const [
    universityQuery,
    setUniversityQuery,
  ] = useState("");

  const [
    selectedUniversity,
    setSelectedUniversity,
  ] = useState<AcademicUniversity | null>(
    null
  );

  const [
    universityResults,
    setUniversityResults,
  ] = useState<AcademicUniversity[]>([]);

  const [
    universitySearching,
    setUniversitySearching,
  ] = useState(false);

  const [
    universityDropdownOpen,
    setUniversityDropdownOpen,
  ] = useState(false);

  const [
    highlightedUniversityIndex,
    setHighlightedUniversityIndex,
  ] = useState(-1);

  /*
   * Üniversite alanının odak durumunu tutar.
   *
   * API isteği geç tamamlansa bile odak
   * kaybedildiyse dropdown tekrar açılmaz.
   */
  const universityInputFocusedRef =
    useRef(false);

  /*
   * Üniversite alanı artık gerçek input değil,
   * contentEditable bir div olduğu için DOM
   * referansını ayrıca tutuyoruz.
   */
  const universityEditorRef =
    useRef<HTMLDivElement | null>(null);

  /*
   * Diğer form alanları.
   */
  const [facultyName, setFacultyName] =
    useState("");

  const [
    departmentName,
    setDepartmentName,
  ] = useState("");

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
    } catch (error: unknown) {
      setMessage(
        getErrorMessage(
          error,
          "Doğrulamalar yüklenirken bir hata oluştu."
        )
      );
    } finally {
      setLoading(false);
    }
  }, []);

  /*
   * Sayfa açıldığında doğrulamaları getirir.
   */
  useEffect(() => {
    void load();
  }, [load]);

  /*
   * contentEditable alanın görünen metnini
   * React state'i ile eşit tutar.
   *
   * Normal yazma sırasında metinler zaten
   * eşit olduğu için imleç konumu bozulmaz.
   */
  useEffect(() => {
    const editor =
      universityEditorRef.current;

    if (
      editor &&
      editor.textContent !== universityQuery
    ) {
      editor.textContent =
        universityQuery;
    }
  }, [universityQuery]);

  /*
   * Kullanıcı üniversite alanına yazdıkça
   * 300 ms debounce ile backend'de arama yapar.
   */
  useEffect(() => {
    const query =
      universityQuery.trim();

    /*
     * Seçili üniversitenin canonical adı
     * değişmeden duruyorsa yeniden arama yapma.
     */
    if (
      selectedUniversity &&
      query === selectedUniversity.name
    ) {
      setUniversityResults([]);
      setUniversityDropdownOpen(false);
      setUniversitySearching(false);
      setHighlightedUniversityIndex(-1);

      return;
    }

    /*
     * En az iki karakter girilmeden
     * backend isteği gönderilmez.
     */
    if (query.length < 2) {
      setUniversityResults([]);
      setUniversityDropdownOpen(false);
      setUniversitySearching(false);
      setHighlightedUniversityIndex(-1);

      return;
    }

    const controller =
      new AbortController();

    const timer =
      window.setTimeout(
        async () => {
          setUniversitySearching(true);

          setUniversityDropdownOpen(
            universityInputFocusedRef.current
          );

          try {
            const { data } =
              await studentApi.get<
                AcademicUniversity[]
              >(
                "/academic/universities",
                {
                  params: {
                    search: query,
                  },
                  signal:
                    controller.signal,
                }
              );

            setUniversityResults(data);

            setHighlightedUniversityIndex(
              data.length > 0
                ? 0
                : -1
            );

            /*
             * Input odağını kaybettiyse geç gelen
             * API yanıtı dropdown'ı açamaz.
             */
            setUniversityDropdownOpen(
              universityInputFocusedRef.current
            );
          } catch (error: unknown) {
            /*
             * Yeni sorgu başladığında önceki
             * isteğin iptal edilmesi normaldir.
             */
            if (!controller.signal.aborted) {
              console.error(
                "Üniversite arama hatası:",
                error
              );

              setUniversityResults([]);
              setHighlightedUniversityIndex(-1);

              setUniversityDropdownOpen(
                universityInputFocusedRef.current
              );
            }
          } finally {
            if (!controller.signal.aborted) {
              setUniversitySearching(false);
            }
          }
        },
        300
      );

    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [
    universityQuery,
    selectedUniversity,
  ]);

  /*
   * Üniversite alanındaki metin değiştiğinde
   * eski seçim geçersiz hâle gelirse temizlenir.
   */
  const handleUniversityQueryChange = (
    value: string
  ) => {
    setUniversityQuery(value);
    setMessage("");

    if (
      selectedUniversity &&
      value !== selectedUniversity.name
    ) {
      setSelectedUniversity(null);
    }

    if (value.trim().length < 2) {
      setUniversityResults([]);
      setUniversityDropdownOpen(false);
      setUniversitySearching(false);
      setHighlightedUniversityIndex(-1);
    }
  };

  /*
   * Dropdown içerisinden canonical
   * üniversite kaydını seçer.
   */
  const selectUniversity = (
    university: AcademicUniversity
  ) => {
    setSelectedUniversity(university);
    setUniversityQuery(university.name);
    setUniversityResults([]);
    setUniversityDropdownOpen(false);
    setUniversitySearching(false);
    setHighlightedUniversityIndex(-1);
    setMessage("");
  };

  /*
   * Üniversite dropdown'unda klavye
   * navigasyonunu yönetir.
   */
  const handleUniversityKeyDown = (
    event: KeyboardEvent<HTMLDivElement>
  ) => {
    /*
     * contentEditable içinde Enter tuşunun
     * yeni satır oluşturmasını engeller.
     */
    if (event.key === "Enter") {
      event.preventDefault();

      if (
        universityDropdownOpen &&
        highlightedUniversityIndex >= 0
      ) {
        const university =
          universityResults[
            highlightedUniversityIndex
          ];

        if (university) {
          selectUniversity(
            university
          );
        }
      }

      return;
    }

    if (event.key === "Escape") {
      event.preventDefault();

      setUniversityDropdownOpen(false);
      setHighlightedUniversityIndex(-1);

      return;
    }

    if (
      !universityDropdownOpen ||
      universityResults.length === 0
    ) {
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();

      setHighlightedUniversityIndex(
        (currentIndex) =>
          currentIndex >=
          universityResults.length - 1
            ? 0
            : currentIndex + 1
      );

      return;
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();

      setHighlightedUniversityIndex(
        (currentIndex) =>
          currentIndex <= 0
            ? universityResults.length - 1
            : currentIndex - 1
      );
    }
  };

  /*
   * Formu ve autocomplete state'lerini temizler.
   */
  const resetForm = () => {
    universityInputFocusedRef.current =
      false;

    if (universityEditorRef.current) {
      universityEditorRef.current
        .replaceChildren();
    }

    setUniversityQuery("");
    setSelectedUniversity(null);
    setUniversityResults([]);
    setUniversityDropdownOpen(false);
    setUniversitySearching(false);
    setHighlightedUniversityIndex(-1);

    setFacultyName("");
    setDepartmentName("");
    setDocumentIssueDate("");
    setDocument(null);
  };

  /*
   * Yeni öğrenci doğrulaması gönderir.
   */
  const submit = async (
    event: FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();

    /*
     * Inputa metin yazmak yeterli değildir.
     * Kullanıcı backend sonuçlarından gerçek
     * bir üniversite seçmiş olmalıdır.
     */
    if (
      !selectedUniversity ||
      universityQuery.trim() !==
        selectedUniversity.name
    ) {
      setMessage(
        "Lütfen arama sonuçlarından geçerli bir Türkiye üniversitesi seçin."
      );

      return;
    }

    if (
      !facultyName.trim() ||
      !departmentName.trim()
    ) {
      setMessage(
        "Fakülte ve bölüm bilgileri zorunludur."
      );

      return;
    }

    if (!documentIssueDate) {
      setMessage(
        "Belge tarihini seçmelisiniz."
      );

      return;
    }

    const minimumDocumentDate =
      getMinimumDocumentDateInputValue();

    const today =
      getTodayInputValue();

    if (
      documentIssueDate <
        minimumDocumentDate ||
      documentIssueDate > today
    ) {
      setMessage(
        "Öğrenci belgesi son 30 gün içinde alınmış olmalıdır."
      );

      return;
    }

    if (!document) {
      setMessage(
        "Öğrenci belgesi PDF dosyasını seçmelisiniz."
      );

      return;
    }

    /*
     * Bazı tarayıcılar PDF MIME tipini boş
     * döndürebildiği için uzantı da kontrol edilir.
     */
    if (
      document.type !== "application/pdf" &&
      !document.name
        .toLocaleLowerCase("tr-TR")
        .endsWith(".pdf")
    ) {
      setMessage(
        "Öğrenci belgesi PDF formatında olmalıdır."
      );

      return;
    }

    if (document.size > MAX_FILE_SIZE) {
      setMessage(
        "Öğrenci belgesi en fazla 10 MB olabilir."
      );

      return;
    }

    setSubmitting(true);
    setMessage("");

    try {
      const formData =
        new FormData();

      /*
       * UniversityName yerine canonical
       * UniversityId gönderilir.
       */
      formData.append(
        "UniversityId",
        selectedUniversity.id
      );

      formData.append(
        "FacultyName",
        facultyName.trim()
      );

      formData.append(
        "DepartmentName",
        departmentName.trim()
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

      resetForm();
      setShowForm(false);

      setMessage(
        "Öğrenci belgeniz incelemeye gönderildi."
      );

      await load();
    } catch (error: unknown) {
      setMessage(
        getErrorMessage(
          error,
          "Belge yüklenirken hata oluştu."
        )
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
    if (item.status !== "Pending") {
      setMessage(
        "Yalnızca inceleme bekleyen doğrulamalar silinebilir."
      );

      return;
    }

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

      await load();
    } catch (error: unknown) {
      setMessage(
        getErrorMessage(
          error,
          "Doğrulama silinirken hata oluştu."
        )
      );
    } finally {
      setDeletingId(null);
    }
  };

  /*
   * Yeni üniversite formunu açar veya kapatır.
   */
  const toggleForm = () => {
    const nextValue =
      !showForm;

    if (!nextValue) {
      resetForm();
    }

    setShowForm(nextValue);
    setMessage("");
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
        <div
          className="student-message"
          role="status"
          aria-live="polite"
        >
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
            onClick={toggleForm}
          >
            <Plus size={17} />

            {showForm
              ? "Formu kapat"
              : "Yeni üniversite ekle"}
          </button>
        </div>

        {showForm && (
          <form
            className="student-verification-form"
            onSubmit={submit}
            autoComplete="off"
          >
            <h3>
              Yeni öğrenci doğrulaması
            </h3>

            <div className="student-form-grid">
              <label className="university-autocomplete">
                <span id="university-field-label">
                  Üniversite
                </span>

                <div className="university-autocomplete__control">
                  {/*
                   * Gerçek input yerine contentEditable
                   * kullanıldığı için Safari bu alanı
                   * kişi veya rehber alanı olarak
                   * otomatik dolduramaz.
                   */}
                  <div
                    ref={universityEditorRef}
                    id="notmarket-university-search"
                    className="university-autocomplete__editor"
                    contentEditable
                    suppressContentEditableWarning
                    tabIndex={0}
                    data-placeholder="Üniversite adını yazın"
                    role="combobox"
                    aria-labelledby="university-field-label"
                    aria-required="true"
                    aria-autocomplete="list"
                    aria-haspopup="listbox"
                    aria-expanded={
                      universityDropdownOpen
                    }
                    aria-controls={
                      universityDropdownOpen
                        ? "university-search-results"
                        : undefined
                    }
                    aria-activedescendant={
                      universityDropdownOpen &&
                      highlightedUniversityIndex >= 0
                        ? `university-option-${highlightedUniversityIndex}`
                        : undefined
                    }
                    spellCheck={false}
                    onInput={(event) => {
                      const rawValue =
                        event.currentTarget
                          .textContent ?? "";

                      /*
                       * Satır sonlarını tek boşluğa
                       * dönüştürür.
                       */
                      const value =
                        rawValue
                          .replace(
                            /[\r\n]+/g,
                            " "
                          )
                          .replace(
                            /\u00a0/g,
                            " "
                          );

                      if (rawValue !== value) {
                        event.currentTarget
                          .textContent =
                            value;
                      }

                      handleUniversityQueryChange(
                        value
                      );
                    }}
                    onPaste={(event) => {
                      /*
                       * Biçimlendirilmiş içerik yerine
                       * yalnızca düz metin yapıştırılır.
                       */
                      event.preventDefault();

                      const text =
                        event.clipboardData
                          .getData("text/plain")
                          .replace(
                            /[\r\n]+/g,
                            " "
                          );

                      insertPlainTextAtCursor(
                        text
                      );

                      const currentValue =
                        event.currentTarget
                          .textContent ?? "";

                      handleUniversityQueryChange(
                        currentValue
                      );
                    }}
                    onKeyDown={
                      handleUniversityKeyDown
                    }
                    onFocus={() => {
                      universityInputFocusedRef.current =
                        true;

                      if (
                        universityQuery
                          .trim()
                          .length >= 2 &&
                        !selectedUniversity
                      ) {
                        setUniversityDropdownOpen(
                          true
                        );
                      }
                    }}
                    onBlur={() => {
                      universityInputFocusedRef.current =
                        false;

                      window.setTimeout(
                        () => {
                          setUniversityDropdownOpen(
                            false
                          );

                          setHighlightedUniversityIndex(
                            -1
                          );
                        },
                        150
                      );
                    }}
                  />

                  {/*
                   * Form gönderimi JavaScript ile
                   * yapılsa da seçilmiş canonical ID
                   * DOM içerisinde de tutulur.
                   */}
                  <input
                    type="hidden"
                    name="UniversityId"
                    value={
                      selectedUniversity?.id ??
                      ""
                    }
                  />

                  {selectedUniversity && (
                    <span className="university-selected-indicator">
                      Seçildi
                    </span>
                  )}
                </div>

                {universityDropdownOpen && (
                  <div
                    id="university-search-results"
                    className="university-autocomplete__dropdown"
                    role="listbox"
                    aria-label="Üniversite arama sonuçları"
                    aria-busy={
                      universitySearching
                    }
                  >
                    {universitySearching && (
                      <div
                        className="university-autocomplete__state"
                        role="status"
                      >
                        Üniversiteler aranıyor...
                      </div>
                    )}

                    {!universitySearching &&
                      universityResults.length ===
                        0 && (
                        <div
                          className="university-autocomplete__state"
                          role="status"
                        >
                          Eşleşen üniversite
                          bulunamadı.
                        </div>
                      )}

                    {!universitySearching &&
                      universityResults.map(
                        (
                          university,
                          index
                        ) => (
                          <button
                            id={`university-option-${index}`}
                            key={
                              university.id
                            }
                            type="button"
                            role="option"
                            aria-selected={
                              index ===
                              highlightedUniversityIndex
                            }
                            className={
                              index ===
                              highlightedUniversityIndex
                                ? "university-autocomplete__option university-autocomplete__option--active"
                                : "university-autocomplete__option"
                            }
                            onMouseEnter={() =>
                              setHighlightedUniversityIndex(
                                index
                              )
                            }
                            /*
                             * PointerDown sırasında
                             * editörün blur olmasını
                             * engeller.
                             */
                            onPointerDown={(
                              event
                            ) => {
                              event.preventDefault();
                            }}
                            onClick={() =>
                              selectUniversity(
                                university
                              )
                            }
                          >
                            {
                              university.name
                            }
                          </button>
                        )
                      )}
                  </div>
                )}

                <small>
                  En az iki karakter yazın ve
                  listeden bir Türkiye
                  üniversitesi seçin.
                </small>
              </label>

              <label>
                Fakülte

                <input
                  name="notmarket-faculty"
                  value={facultyName}
                  placeholder="Örneğin: Fen Fakültesi"
                  autoComplete="off"
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
                  name="notmarket-department"
                  value={departmentName}
                  placeholder="Örneğin: Matematik"
                  autoComplete="off"
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
                  name="notmarket-document-issue-date"
                  type="date"
                  value={documentIssueDate}
                  min={
                    getMinimumDocumentDateInputValue()
                  }
                  max={
                    getTodayInputValue()
                  }
                  autoComplete="off"
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
                name="notmarket-student-document"
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
                onClick={() => {
                  resetForm();
                  setShowForm(false);
                  setMessage("");
                }}
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
        <GraduationCap size={22} />
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
        <CheckCircle2 size={15} />
        Doğrulandı
      </span>
    );
  }

  if (status === "Rejected") {
    return (
      <span className="student-status student-status--rejected">
        <XCircle size={15} />
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

function getTodayInputValue(): string {
  return formatDateForInput(
    new Date()
  );
}

function getMinimumDocumentDateInputValue(): string {
  const minimumDate =
    new Date();

  minimumDate.setDate(
    minimumDate.getDate() - 30
  );

  return formatDateForInput(
    minimumDate
  );
}

function formatDateForInput(
  date: Date
): string {
  const year =
    date.getFullYear();

  const month =
    String(
      date.getMonth() + 1
    ).padStart(2, "0");

  const day =
    String(
      date.getDate()
    ).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

/*
 * Clipboard içeriğini contentEditable alana
 * yalnızca düz metin olarak ekler.
 */
function insertPlainTextAtCursor(
  text: string
) {
  const selection =
    window.getSelection();

  if (
    !selection ||
    selection.rangeCount === 0
  ) {
    return;
  }

  const range =
    selection.getRangeAt(0);

  range.deleteContents();

  const textNode =
    document.createTextNode(text);

  range.insertNode(textNode);
  range.setStartAfter(textNode);
  range.collapse(true);

  selection.removeAllRanges();
  selection.addRange(range);
}

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