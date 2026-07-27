export default function PlaceholderPage({
  title,
  description
}: {
  title: string;
  description: string;
}) {
  return (
    <>
      <div className="page-heading">
        <div>
          <span className="section-kicker">Sonraki geliştirme fazı</span>
          <h1>{title}</h1>
          <p>{description}</p>
        </div>
      </div>

      <article className="panel empty-state empty-state--large">
        Bu ekran temel admin altyapısından sonra geliştirilecek.
      </article>
    </>
  );
}
