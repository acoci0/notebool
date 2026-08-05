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
  AcademicProgram,
  AcademicUnit,
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

  const universityInputFocusedRef =
    useRef(false);

  const universityEditorRef =
    useRef<HTMLDivElement | null>(null);

  /*
   * Akademik birim autocomplete state'leri.
   */
  const [
    academicUnitQuery,
    setAcademicUnitQuery,
  ] = useState("");

  const [
    selectedAcademicUnit,
    setSelectedAcademicUnit,
  ] = useState<AcademicUnit | null>(
    null
  );

  const [
    academicUnitResults,
    setAcademicUnitResults,
  ] = useState<AcademicUnit[]>([]);

  const [
    academicUnitSearching,
    setAcademicUnitSearching,
  ] = useState(false);

  const [
    academicUnitDropdownOpen,
    setAcademicUnitDropdownOpen,
  ] = useState(false);

  const [
    highlightedAcademicUnitIndex,
    setHighlightedAcademicUnitIndex,
  ] = useState(-1);

  const academicUnitInputFocusedRef =
    useRef(false);

  const academicUnitEditorRef =
    useRef<HTMLDivElement | null>(null);

  /*
   * Bölüm / program autocomplete state'leri.
   */
  const [
    academicProgramQuery,
    setAcademicProgramQuery,
  ] = useState("");

  const [
    selectedAcademicProgram,
    setSelectedAcademicProgram,
  ] = useState<AcademicProgram | null>(
    null
  );

  const [
    academicProgramResults,
    setAcademicProgramResults,
  ] = useState<AcademicProgram[]>([]);

  const [
    academicProgramSearching,
    setAcademicProgramSearching,
  ] = useState(false);

  const [
    academicProgramDropdownOpen,
    setAcademicProgramDropdownOpen,
  ] = useState(false);

  const [
    highlightedAcademicProgramIndex,
    setHighlightedAcademicProgramIndex,
  ] = useState(-1);

  const academicProgramInputFocusedRef =
    useRef(false);

  const academicProgramEditorRef =
    useRef<HTMLDivElement | null>(null);

  /*
   * Belge alanları.
   */
  const [
    documentIssueDate,
    setDocumentIssueDate,
  ] = useState("");

  const [document, setDocument] =
    useState<File | null>(null);

  /*
   * Öğrencinin doğrulamalarını getirir.
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

  useEffect(() => {
    void load();
  }, [load]);

  /*
   * contentEditable alanların görünen
   * metinlerini React state'leriyle
   * senkronize eder.
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

  useEffect(() => {
    const editor =
      academicUnitEditorRef.current;

    if (
      editor &&
      editor.textContent !== academicUnitQuery
    ) {
      editor.textContent =
        academicUnitQuery;
    }
  }, [academicUnitQuery]);

  useEffect(() => {
    const editor =
      academicProgramEditorRef.current;

    if (
      editor &&
      editor.textContent !==
        academicProgramQuery
    ) {
      editor.textContent =
        academicProgramQuery;
    }
  }, [academicProgramQuery]);

  /*
   * Üniversite araması.
   */
  useEffect(() => {
    const query =
      universityQuery.trim();

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

            setUniversityDropdownOpen(
              universityInputFocusedRef.current
            );
          } catch (error: unknown) {
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
   * Seçili üniversiteye bağlı akademik
   * birimlerin aranması.
   */
  useEffect(() => {
    const query =
      academicUnitQuery.trim();

    if (!selectedUniversity) {
      setAcademicUnitResults([]);
      setAcademicUnitDropdownOpen(false);
      setAcademicUnitSearching(false);
      setHighlightedAcademicUnitIndex(-1);

      return;
    }

    if (
      selectedAcademicUnit &&
      query === selectedAcademicUnit.name
    ) {
      setAcademicUnitResults([]);
      setAcademicUnitDropdownOpen(false);
      setAcademicUnitSearching(false);
      setHighlightedAcademicUnitIndex(-1);

      return;
    }

    if (query.length < 2) {
      setAcademicUnitResults([]);
      setAcademicUnitDropdownOpen(false);
      setAcademicUnitSearching(false);
      setHighlightedAcademicUnitIndex(-1);

      return;
    }

    const controller =
      new AbortController();

    const timer =
      window.setTimeout(
        async () => {
          setAcademicUnitSearching(true);

          setAcademicUnitDropdownOpen(
            academicUnitInputFocusedRef.current
          );

          try {
            const { data } =
              await studentApi.get<
                AcademicUnit[]
              >(
                "/academic/units",
                {
                  params: {
                    universityId:
                      selectedUniversity.id,
                    search: query,
                  },
                  signal:
                    controller.signal,
                }
              );

            setAcademicUnitResults(data);

            setHighlightedAcademicUnitIndex(
              data.length > 0
                ? 0
                : -1
            );

            setAcademicUnitDropdownOpen(
              academicUnitInputFocusedRef.current
            );
          } catch (error: unknown) {
            if (!controller.signal.aborted) {
              console.error(
                "Akademik birim arama hatası:",
                error
              );

              setAcademicUnitResults([]);
              setHighlightedAcademicUnitIndex(-1);

              setAcademicUnitDropdownOpen(
                academicUnitInputFocusedRef.current
              );
            }
          } finally {
            if (!controller.signal.aborted) {
              setAcademicUnitSearching(false);
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
    academicUnitQuery,
    selectedAcademicUnit,
    selectedUniversity,
  ]);

  /*
   * Seçili akademik birime bağlı bölüm
   * ve programların aranması.
   */
  useEffect(() => {
    const query =
      academicProgramQuery.trim();

    if (!selectedAcademicUnit) {
      setAcademicProgramResults([]);
      setAcademicProgramDropdownOpen(false);
      setAcademicProgramSearching(false);
      setHighlightedAcademicProgramIndex(-1);

      return;
    }

    if (
      selectedAcademicProgram &&
      query === selectedAcademicProgram.name
    ) {
      setAcademicProgramResults([]);
      setAcademicProgramDropdownOpen(false);
      setAcademicProgramSearching(false);
      setHighlightedAcademicProgramIndex(-1);

      return;
    }

    if (query.length < 2) {
      setAcademicProgramResults([]);
      setAcademicProgramDropdownOpen(false);
      setAcademicProgramSearching(false);
      setHighlightedAcademicProgramIndex(-1);

      return;
    }

    const controller =
      new AbortController();

    const timer =
      window.setTimeout(
        async () => {
          setAcademicProgramSearching(true);

          setAcademicProgramDropdownOpen(
            academicProgramInputFocusedRef.current
          );

          try {
            const { data } =
              await studentApi.get<
                AcademicProgram[]
              >(
                "/academic/programs",
                {
                  params: {
                    academicUnitId:
                      selectedAcademicUnit.id,
                    search: query,
                  },
                  signal:
                    controller.signal,
                }
              );

            setAcademicProgramResults(data);

            setHighlightedAcademicProgramIndex(
              data.length > 0
                ? 0
                : -1
            );

            setAcademicProgramDropdownOpen(
              academicProgramInputFocusedRef.current
            );
          } catch (error: unknown) {
            if (!controller.signal.aborted) {
              console.error(
                "Bölüm/program arama hatası:",
                error
              );

              setAcademicProgramResults([]);
              setHighlightedAcademicProgramIndex(-1);

              setAcademicProgramDropdownOpen(
                academicProgramInputFocusedRef.current
              );
            }
          } finally {
            if (!controller.signal.aborted) {
              setAcademicProgramSearching(false);
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
    academicProgramQuery,
    selectedAcademicProgram,
    selectedAcademicUnit,
  ]);

  /*
   * Program alanını temizler.
   */
  const resetAcademicProgramSelection = () => {
    academicProgramInputFocusedRef.current =
      false;

    if (academicProgramEditorRef.current) {
      academicProgramEditorRef.current
        .replaceChildren();
    }

    setAcademicProgramQuery("");
    setSelectedAcademicProgram(null);
    setAcademicProgramResults([]);
    setAcademicProgramDropdownOpen(false);
    setAcademicProgramSearching(false);
    setHighlightedAcademicProgramIndex(-1);
  };

  /*
   * Akademik birim ve program alanlarını
   * birlikte temizler.
   */
  const resetAcademicUnitAndProgramSelections =
    () => {
      academicUnitInputFocusedRef.current =
        false;

      if (academicUnitEditorRef.current) {
        academicUnitEditorRef.current
          .replaceChildren();
      }

      setAcademicUnitQuery("");
      setSelectedAcademicUnit(null);
      setAcademicUnitResults([]);
      setAcademicUnitDropdownOpen(false);
      setAcademicUnitSearching(false);
      setHighlightedAcademicUnitIndex(-1);

      resetAcademicProgramSelection();
    };

  /*
   * Üniversite alanındaki metin değişimi.
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

      resetAcademicUnitAndProgramSelections();
    }

    if (value.trim().length < 2) {
      setUniversityResults([]);
      setUniversityDropdownOpen(false);
      setUniversitySearching(false);
      setHighlightedUniversityIndex(-1);
    }
  };

  /*
   * Üniversite seçimi.
   *
   * Üniversite değiştiğinde önceki akademik
   * birim ve program seçimleri geçersiz olur.
   */
  const selectUniversity = (
    university: AcademicUniversity
  ) => {
    const universityChanged =
      selectedUniversity?.id !==
      university.id;

    setSelectedUniversity(university);
    setUniversityQuery(university.name);
    setUniversityResults([]);
    setUniversityDropdownOpen(false);
    setUniversitySearching(false);
    setHighlightedUniversityIndex(-1);
    setMessage("");

    if (universityChanged) {
      resetAcademicUnitAndProgramSelections();
    }
  };

  /*
   * Akademik birim alanındaki metin değişimi.
   */
  const handleAcademicUnitQueryChange = (
    value: string
  ) => {
    setAcademicUnitQuery(value);
    setMessage("");

    if (
      selectedAcademicUnit &&
      value !== selectedAcademicUnit.name
    ) {
      setSelectedAcademicUnit(null);

      resetAcademicProgramSelection();
    }

    if (value.trim().length < 2) {
      setAcademicUnitResults([]);
      setAcademicUnitDropdownOpen(false);
      setAcademicUnitSearching(false);
      setHighlightedAcademicUnitIndex(-1);
    }
  };

  /*
   * Akademik birim seçimi.
   */
  const selectAcademicUnit = (
    academicUnit: AcademicUnit
  ) => {
    const academicUnitChanged =
      selectedAcademicUnit?.id !==
      academicUnit.id;

    setSelectedAcademicUnit(academicUnit);
    setAcademicUnitQuery(academicUnit.name);
    setAcademicUnitResults([]);
    setAcademicUnitDropdownOpen(false);
    setAcademicUnitSearching(false);
    setHighlightedAcademicUnitIndex(-1);
    setMessage("");

    if (academicUnitChanged) {
      resetAcademicProgramSelection();
    }
  };

  /*
   * Program alanındaki metin değişimi.
   */
  const handleAcademicProgramQueryChange = (
    value: string
  ) => {
    setAcademicProgramQuery(value);
    setMessage("");

    if (
      selectedAcademicProgram &&
      value !== selectedAcademicProgram.name
    ) {
      setSelectedAcademicProgram(null);
    }

    if (value.trim().length < 2) {
      setAcademicProgramResults([]);
      setAcademicProgramDropdownOpen(false);
      setAcademicProgramSearching(false);
      setHighlightedAcademicProgramIndex(-1);
    }
  };

  /*
   * Bölüm / program seçimi.
   */
  const selectAcademicProgram = (
    academicProgram: AcademicProgram
  ) => {
    setSelectedAcademicProgram(
      academicProgram
    );

    setAcademicProgramQuery(
      academicProgram.name
    );

    setAcademicProgramResults([]);
    setAcademicProgramDropdownOpen(false);
    setAcademicProgramSearching(false);
    setHighlightedAcademicProgramIndex(-1);
    setMessage("");
  };

  /*
   * Üniversite klavye navigasyonu.
   */
  const handleUniversityKeyDown = (
    event: KeyboardEvent<HTMLDivElement>
  ) => {
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
          selectUniversity(university);
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
   * Akademik birim klavye navigasyonu.
   */
  const handleAcademicUnitKeyDown = (
    event: KeyboardEvent<HTMLDivElement>
  ) => {
    if (!selectedUniversity) {
      event.preventDefault();
      return;
    }

    if (event.key === "Enter") {
      event.preventDefault();

      if (
        academicUnitDropdownOpen &&
        highlightedAcademicUnitIndex >= 0
      ) {
        const academicUnit =
          academicUnitResults[
            highlightedAcademicUnitIndex
          ];

        if (academicUnit) {
          selectAcademicUnit(
            academicUnit
          );
        }
      }

      return;
    }

    if (event.key === "Escape") {
      event.preventDefault();

      setAcademicUnitDropdownOpen(false);
      setHighlightedAcademicUnitIndex(-1);

      return;
    }

    if (
      !academicUnitDropdownOpen ||
      academicUnitResults.length === 0
    ) {
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();

      setHighlightedAcademicUnitIndex(
        (currentIndex) =>
          currentIndex >=
          academicUnitResults.length - 1
            ? 0
            : currentIndex + 1
      );

      return;
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();

      setHighlightedAcademicUnitIndex(
        (currentIndex) =>
          currentIndex <= 0
            ? academicUnitResults.length - 1
            : currentIndex - 1
      );
    }
  };

  /*
   * Bölüm / program klavye navigasyonu.
   */
  const handleAcademicProgramKeyDown = (
    event: KeyboardEvent<HTMLDivElement>
  ) => {
    if (!selectedAcademicUnit) {
      event.preventDefault();
      return;
    }

    if (event.key === "Enter") {
      event.preventDefault();

      if (
        academicProgramDropdownOpen &&
        highlightedAcademicProgramIndex >= 0
      ) {
        const academicProgram =
          academicProgramResults[
            highlightedAcademicProgramIndex
          ];

        if (academicProgram) {
          selectAcademicProgram(
            academicProgram
          );
        }
      }

      return;
    }

    if (event.key === "Escape") {
      event.preventDefault();

      setAcademicProgramDropdownOpen(false);
      setHighlightedAcademicProgramIndex(-1);

      return;
    }

    if (
      !academicProgramDropdownOpen ||
      academicProgramResults.length === 0
    ) {
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();

      setHighlightedAcademicProgramIndex(
        (currentIndex) =>
          currentIndex >=
          academicProgramResults.length - 1
            ? 0
            : currentIndex + 1
      );

      return;
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();

      setHighlightedAcademicProgramIndex(
        (currentIndex) =>
          currentIndex <= 0
            ? academicProgramResults.length - 1
            : currentIndex - 1
      );
    }
  };

  /*
   * Formu tamamen temizler.
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

    resetAcademicUnitAndProgramSelections();

    setDocumentIssueDate("");
    setDocument(null);
  };

  /*
   * Yeni doğrulama gönderimi.
   */
  const submit = async (
    event: FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();

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
      !selectedAcademicUnit ||
      academicUnitQuery.trim() !==
        selectedAcademicUnit.name
    ) {
      setMessage(
        "Lütfen seçilen üniversiteye bağlı geçerli bir fakülte veya akademik birim seçin."
      );

      return;
    }

    if (
      !selectedAcademicProgram ||
      academicProgramQuery.trim() !==
        selectedAcademicProgram.name
    ) {
      setMessage(
        "Lütfen seçilen akademik birime bağlı geçerli bir bölüm veya program seçin."
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

      formData.append(
        "UniversityId",
        selectedUniversity.id
      );

      formData.append(
        "AcademicUnitId",
        selectedAcademicUnit.id
      );

      formData.append(
        "AcademicProgramId",
        selectedAcademicProgram.id
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

                      const value =
                        normalizeEditableText(
                          rawValue
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
                      event.preventDefault();

                      const text =
                        normalizeEditableText(
                          event.clipboardData
                            .getData("text/plain")
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
                      <div className="university-autocomplete__state">
                        Üniversiteler aranıyor...
                      </div>
                    )}

                    {!universitySearching &&
                      universityResults.length ===
                        0 && (
                        <div className="university-autocomplete__state">
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

              <label className="university-autocomplete">
                <span id="academic-unit-field-label">
                  Fakülte / Akademik birim
                </span>

                <div className="university-autocomplete__control">
                  <div
                    ref={academicUnitEditorRef}
                    id="notmarket-academic-unit-search"
                    className="university-autocomplete__editor"
                    contentEditable={
                      Boolean(
                        selectedUniversity
                      )
                    }
                    suppressContentEditableWarning
                    tabIndex={
                      selectedUniversity
                        ? 0
                        : -1
                    }
                    data-placeholder={
                      selectedUniversity
                        ? "Fakülte veya akademik birim yazın"
                        : "Önce üniversite seçin"
                    }
                    role="combobox"
                    aria-labelledby="academic-unit-field-label"
                    aria-required="true"
                    aria-disabled={
                      !selectedUniversity
                    }
                    aria-autocomplete="list"
                    aria-haspopup="listbox"
                    aria-expanded={
                      academicUnitDropdownOpen
                    }
                    aria-controls={
                      academicUnitDropdownOpen
                        ? "academic-unit-search-results"
                        : undefined
                    }
                    aria-activedescendant={
                      academicUnitDropdownOpen &&
                      highlightedAcademicUnitIndex >=
                        0
                        ? `academic-unit-option-${highlightedAcademicUnitIndex}`
                        : undefined
                    }
                    spellCheck={false}
                    onInput={(event) => {
                      if (!selectedUniversity) {
                        return;
                      }

                      const rawValue =
                        event.currentTarget
                          .textContent ?? "";

                      const value =
                        normalizeEditableText(
                          rawValue
                        );

                      if (rawValue !== value) {
                        event.currentTarget
                          .textContent =
                            value;
                      }

                      handleAcademicUnitQueryChange(
                        value
                      );
                    }}
                    onPaste={(event) => {
                      if (!selectedUniversity) {
                        event.preventDefault();
                        return;
                      }

                      event.preventDefault();

                      const text =
                        normalizeEditableText(
                          event.clipboardData
                            .getData("text/plain")
                        );

                      insertPlainTextAtCursor(
                        text
                      );

                      const currentValue =
                        event.currentTarget
                          .textContent ?? "";

                      handleAcademicUnitQueryChange(
                        currentValue
                      );
                    }}
                    onKeyDown={
                      handleAcademicUnitKeyDown
                    }
                    onFocus={() => {
                      if (!selectedUniversity) {
                        return;
                      }

                      academicUnitInputFocusedRef.current =
                        true;

                      if (
                        academicUnitQuery
                          .trim()
                          .length >= 2 &&
                        !selectedAcademicUnit
                      ) {
                        setAcademicUnitDropdownOpen(
                          true
                        );
                      }
                    }}
                    onBlur={() => {
                      academicUnitInputFocusedRef.current =
                        false;

                      window.setTimeout(
                        () => {
                          setAcademicUnitDropdownOpen(
                            false
                          );

                          setHighlightedAcademicUnitIndex(
                            -1
                          );
                        },
                        150
                      );
                    }}
                  />

                  <input
                    type="hidden"
                    name="AcademicUnitId"
                    value={
                      selectedAcademicUnit?.id ??
                      ""
                    }
                  />

                  {selectedAcademicUnit && (
                    <span className="university-selected-indicator">
                      Seçildi
                    </span>
                  )}
                </div>

                {academicUnitDropdownOpen && (
                  <div
                    id="academic-unit-search-results"
                    className="university-autocomplete__dropdown"
                    role="listbox"
                    aria-label="Akademik birim arama sonuçları"
                    aria-busy={
                      academicUnitSearching
                    }
                  >
                    {academicUnitSearching && (
                      <div className="university-autocomplete__state">
                        Akademik birimler
                        aranıyor...
                      </div>
                    )}

                    {!academicUnitSearching &&
                      academicUnitResults.length ===
                        0 && (
                        <div className="university-autocomplete__state">
                          Eşleşen akademik birim
                          bulunamadı.
                        </div>
                      )}

                    {!academicUnitSearching &&
                      academicUnitResults.map(
                        (
                          academicUnit,
                          index
                        ) => (
                          <button
                            id={`academic-unit-option-${index}`}
                            key={
                              academicUnit.id
                            }
                            type="button"
                            role="option"
                            aria-selected={
                              index ===
                              highlightedAcademicUnitIndex
                            }
                            className={
                              index ===
                              highlightedAcademicUnitIndex
                                ? "university-autocomplete__option university-autocomplete__option--active"
                                : "university-autocomplete__option"
                            }
                            onMouseEnter={() =>
                              setHighlightedAcademicUnitIndex(
                                index
                              )
                            }
                            onPointerDown={(
                              event
                            ) => {
                              event.preventDefault();
                            }}
                            onClick={() =>
                              selectAcademicUnit(
                                academicUnit
                              )
                            }
                          >
                            <span>
                              {
                                academicUnit.name
                              }
                            </span>
                          </button>
                        )
                      )}
                  </div>
                )}

                <small>
                  Üniversiteyi seçtikten sonra
                  en az iki karakter yazın.
                </small>
              </label>

              <label className="university-autocomplete">
                <span id="academic-program-field-label">
                  Bölüm / Program
                </span>

                <div className="university-autocomplete__control">
                  <div
                    ref={academicProgramEditorRef}
                    id="notmarket-academic-program-search"
                    className="university-autocomplete__editor"
                    contentEditable={
                      Boolean(
                        selectedAcademicUnit
                      )
                    }
                    suppressContentEditableWarning
                    tabIndex={
                      selectedAcademicUnit
                        ? 0
                        : -1
                    }
                    data-placeholder={
                      selectedAcademicUnit
                        ? "Bölüm veya program yazın"
                        : "Önce akademik birim seçin"
                    }
                    role="combobox"
                    aria-labelledby="academic-program-field-label"
                    aria-required="true"
                    aria-disabled={
                      !selectedAcademicUnit
                    }
                    aria-autocomplete="list"
                    aria-haspopup="listbox"
                    aria-expanded={
                      academicProgramDropdownOpen
                    }
                    aria-controls={
                      academicProgramDropdownOpen
                        ? "academic-program-search-results"
                        : undefined
                    }
                    aria-activedescendant={
                      academicProgramDropdownOpen &&
                      highlightedAcademicProgramIndex >=
                        0
                        ? `academic-program-option-${highlightedAcademicProgramIndex}`
                        : undefined
                    }
                    spellCheck={false}
                    onInput={(event) => {
                      if (
                        !selectedAcademicUnit
                      ) {
                        return;
                      }

                      const rawValue =
                        event.currentTarget
                          .textContent ?? "";

                      const value =
                        normalizeEditableText(
                          rawValue
                        );

                      if (rawValue !== value) {
                        event.currentTarget
                          .textContent =
                            value;
                      }

                      handleAcademicProgramQueryChange(
                        value
                      );
                    }}
                    onPaste={(event) => {
                      if (
                        !selectedAcademicUnit
                      ) {
                        event.preventDefault();
                        return;
                      }

                      event.preventDefault();

                      const text =
                        normalizeEditableText(
                          event.clipboardData
                            .getData("text/plain")
                        );

                      insertPlainTextAtCursor(
                        text
                      );

                      const currentValue =
                        event.currentTarget
                          .textContent ?? "";

                      handleAcademicProgramQueryChange(
                        currentValue
                      );
                    }}
                    onKeyDown={
                      handleAcademicProgramKeyDown
                    }
                    onFocus={() => {
                      if (
                        !selectedAcademicUnit
                      ) {
                        return;
                      }

                      academicProgramInputFocusedRef.current =
                        true;

                      if (
                        academicProgramQuery
                          .trim()
                          .length >= 2 &&
                        !selectedAcademicProgram
                      ) {
                        setAcademicProgramDropdownOpen(
                          true
                        );
                      }
                    }}
                    onBlur={() => {
                      academicProgramInputFocusedRef.current =
                        false;

                      window.setTimeout(
                        () => {
                          setAcademicProgramDropdownOpen(
                            false
                          );

                          setHighlightedAcademicProgramIndex(
                            -1
                          );
                        },
                        150
                      );
                    }}
                  />

                  <input
                    type="hidden"
                    name="AcademicProgramId"
                    value={
                      selectedAcademicProgram?.id ??
                      ""
                    }
                  />

                  {selectedAcademicProgram && (
                    <span className="university-selected-indicator">
                      Seçildi
                    </span>
                  )}
                </div>

                {academicProgramDropdownOpen && (
                  <div
                    id="academic-program-search-results"
                    className="university-autocomplete__dropdown"
                    role="listbox"
                    aria-label="Bölüm ve program arama sonuçları"
                    aria-busy={
                      academicProgramSearching
                    }
                  >
                    {academicProgramSearching && (
                      <div className="university-autocomplete__state">
                        Bölüm ve programlar
                        aranıyor...
                      </div>
                    )}

                    {!academicProgramSearching &&
                      academicProgramResults.length ===
                        0 && (
                        <div className="university-autocomplete__state">
                          Eşleşen bölüm veya
                          program bulunamadı.
                        </div>
                      )}

                    {!academicProgramSearching &&
                      academicProgramResults.map(
                        (
                          academicProgram,
                          index
                        ) => (
                          <button
                            id={`academic-program-option-${index}`}
                            key={
                              academicProgram.id
                            }
                            type="button"
                            role="option"
                            aria-selected={
                              index ===
                              highlightedAcademicProgramIndex
                            }
                            className={
                              index ===
                              highlightedAcademicProgramIndex
                                ? "university-autocomplete__option university-autocomplete__option--active"
                                : "university-autocomplete__option"
                            }
                            onMouseEnter={() =>
                              setHighlightedAcademicProgramIndex(
                                index
                              )
                            }
                            onPointerDown={(
                              event
                            ) => {
                              event.preventDefault();
                            }}
                            onClick={() =>
                              selectAcademicProgram(
                                academicProgram
                              )
                            }
                          >
                            {
                              academicProgram.name
                            }
                          </button>
                        )
                      )}
                  </div>
                )}

                <small>
                  Akademik birimi seçtikten
                  sonra en az iki karakter
                  yazın.
                </small>
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
 * contentEditable metnini tek satıra
 * ve düz boşluklara dönüştürür.
 */
function normalizeEditableText(
  value: string
): string {
  return value
    .replace(
      /[\r\n]+/g,
      " "
    )
    .replace(
      /\u00a0/g,
      " "
    );
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