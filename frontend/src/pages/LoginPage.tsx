import { ShieldCheck } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export default function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("admin@notmarket.local");
  const [password, setPassword] = useState("ChangeMe123!");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);

    try {
      await login(email, password);
      navigate("/");
    } catch {
      setError(
        "Giriş başarısız. API'nin çalıştığını ve bilgileri kontrol edin."
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="login-page">
      <section className="login-visual">
        <div className="login-visual__content">
          <div className="brand brand--large">
            <div className="brand__mark">
              <ShieldCheck size={23} />
            </div>
            <div>
              <strong>NotMarket</strong>
              <span>Admin Paneli</span>
            </div>
          </div>

          <h1>Güvenilir içerik, kontrollü pazar.</h1>
          <p>
            Öğrenci doğrulamalarını, AI destekli not incelemelerini ve
            platform operasyonlarını tek merkezden yönetin.
          </p>

          <div className="login-feature-grid">
            <article>
              <strong>Çoklu doğrulama</strong>
              <span>Bir kullanıcı için birden fazla üniversite profili.</span>
            </article>
            <article>
              <strong>AI moderasyon</strong>
              <span>Eşleşme, okunabilirlik ve risk skorları.</span>
            </article>
            <article>
              <strong>Audit kayıtları</strong>
              <span>Her yönetim kararının izlenebilir geçmişi.</span>
            </article>
          </div>
        </div>
      </section>

      <section className="login-form-panel">
        <form className="login-card" onSubmit={handleSubmit}>
          <span className="section-kicker">Yetkili giriş</span>
          <h2>Admin paneline giriş yap</h2>
          <p>Geliştirme hesabı form üzerinde hazır gelir.</p>

          <label>
            E-posta
            <input
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              autoComplete="username"
              required
            />
          </label>

          <label>
            Şifre
            <input
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="current-password"
              required
            />
          </label>

          {error && <div className="form-error">{error}</div>}

          <button
            className="primary-button primary-button--full"
            type="submit"
            disabled={isSubmitting}
          >
            {isSubmitting ? "Giriş yapılıyor..." : "Giriş yap"}
          </button>

          <small>
            Canlı ortamda varsayılan hesabı ve JWT anahtarını mutlaka
            değiştirin.
          </small>
        </form>
      </section>
    </div>
  );
}
