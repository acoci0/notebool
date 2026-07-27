import { Check, FileText, X } from "lucide-react";
import { useEffect, useState } from "react";
import api from "../api/client";
import StatusBadge from "../components/StatusBadge";
import type { NoteSubmission } from "../types";

function Score({ label, value, risk = false }: {
  label: string;
  value: number;
  risk?: boolean;
}) {
  return (
    <div className="score">
      <span>{label}</span>
      <strong className={risk && value >= 60 ? "danger-text" : ""}>
        {value}
      </strong>
    </div>
  );
}

export default function NotesPage() {
  const [items, setItems] = useState<NoteSubmission[]>([]);

  const load = async () => {
    const { data } = await api.get<NoteSubmission[]>("/admin/notes");
    setItems(data);
  };

  useEffect(() => {
    void load();
  }, []);

  const decide = async (id: string, approve: boolean) => {
    const reviewNote = window.prompt(
      approve ? "Onay notu" : "Ret gerekçesi"
    );

    if (!approve && !reviewNote?.trim()) {
      return;
    }

    await api.post(`/admin/notes/${id}/decision`, {
      approve,
      reviewNote
    });

    await load();
  };

  return (
    <>
      <div className="page-heading">
        <div>
          <span className="section-kicker">İçerik moderasyonu</span>
          <h1>Not kontrolü</h1>
          <p>
            AI skorlarını, talep eşleşmesini ve oluşturulan PDF’i birlikte
            değerlendirin.
          </p>
        </div>
      </div>

      <div className="review-grid">
        {items.map((item) => (
          <article className="review-card" key={item.id}>
            <div className="review-card__header">
              <div className="review-card__file">
                <FileText size={21} />
              </div>
              <div>
                <h2>{item.title}</h2>
                <p>
                  {item.universityName} · {item.departmentName} ·{" "}
                  {item.courseName}
                </p>
              </div>
              <StatusBadge status={item.status} />
            </div>

            <div className="score-grid">
              <Score label="Talep eşleşmesi" value={item.matchScore} />
              <Score label="Okunabilirlik" value={item.readabilityScore} />
              <Score
                label="Orijinallik riski"
                value={item.originalityRiskScore}
                risk
              />
            </div>

            <div className="review-card__meta">
              <span>Satıcı: {item.sellerName}</span>
              <span>
                {new Date(item.createdAt).toLocaleString("tr-TR")}
              </span>
            </div>

            <div className="review-card__actions">
              <button className="secondary-button">
                <FileText size={17} />
                PDF önizleme
              </button>

              {(item.status === "ManualReview" ||
                item.status === "AiReview") && (
                <>
                  <button
                    className="approve-button"
                    onClick={() => void decide(item.id, true)}
                  >
                    <Check size={17} />
                    Onayla
                  </button>
                  <button
                    className="reject-button"
                    onClick={() => void decide(item.id, false)}
                  >
                    <X size={17} />
                    Reddet
                  </button>
                </>
              )}
            </div>
          </article>
        ))}
      </div>
    </>
  );
}
