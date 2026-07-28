import {
    GraduationCap,
  } from "lucide-react";
  
  import {
    useState,
    type FormEvent,
  } from "react";
  
  import {
    Navigate,
    useNavigate,
  } from "react-router-dom";
  
  import { useStudentAuth } from
    "../auth/StudentAuthContext";
  
  export default function StudentLoginPage() {
    const {
      login,
      isStudentAuthenticated,
    } = useStudentAuth();
  
    const navigate = useNavigate();
  
    const [email, setEmail] =
      useState("ayse@example.com");
  
    const [password, setPassword] =
      useState("Student123!");
  
    const [error, setError] =
      useState("");
  
    const [loading, setLoading] =
      useState(false);
  
    if (isStudentAuthenticated) {
      return (
        <Navigate
          to="/student/profile"
          replace
        />
      );
    }
  
    const handleSubmit = async (
      event: FormEvent
    ) => {
      event.preventDefault();
  
      setError("");
      setLoading(true);
  
      try {
        await login(
          email,
          password
        );
  
        navigate(
          "/student/profile"
        );
      } catch {
        setError(
          "Giriş başarısız. Bilgilerinizi kontrol edin."
        );
      } finally {
        setLoading(false);
      }
    };
  
    return (
      <div className="student-login-page">
        <form
          className="student-login-card"
          onSubmit={handleSubmit}
        >
          <div className="student-login-logo">
            <GraduationCap
              size={28}
            />
          </div>
  
          <span className="section-kicker">
            NOTMARKET
          </span>
  
          <h1>
            Öğrenci hesabına giriş
          </h1>
  
          <p>
            Üniversite doğrulamalarınızı
            yönetin ve NotMarket hesabınıza
            erişin.
          </p>
  
          <label>
            E-posta
  
            <input
              type="email"
              value={email}
              onChange={(event) =>
                setEmail(
                  event.target.value
                )
              }
              required
            />
          </label>
  
          <label>
            Şifre
  
            <input
              type="password"
              value={password}
              onChange={(event) =>
                setPassword(
                  event.target.value
                )
              }
              required
            />
          </label>
  
          {error && (
            <div className="form-error">
              {error}
            </div>
          )}
  
          <button
            className="primary-button primary-button--full"
            type="submit"
            disabled={loading}
          >
            {loading
              ? "Giriş yapılıyor..."
              : "Giriş yap"}
          </button>
        </form>
      </div>
    );
  }