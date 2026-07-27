import type { LucideIcon } from "lucide-react";

type Props = {
  label: string;
  value: string | number;
  helper: string;
  icon: LucideIcon;
};

export default function StatCard({
  label,
  value,
  helper,
  icon: Icon
}: Props) {
  return (
    <article className="stat-card">
      <div className="stat-card__icon">
        <Icon size={20} />
      </div>
      <div>
        <p className="stat-card__label">{label}</p>
        <strong className="stat-card__value">{value}</strong>
        <p className="stat-card__helper">{helper}</p>
      </div>
    </article>
  );
}
