import {
  BarChart3,
  BookCheck,
  FileBadge2,
  LayoutDashboard,
  LogOut,
  Settings,
  ShieldCheck,
  Users
} from "lucide-react";
import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

const menu = [
  { to: "/", label: "Panel", icon: LayoutDashboard },
  { to: "/users", label: "Kullanıcılar", icon: Users },
  {
    to: "/verifications",
    label: "Öğrenci Doğrulamaları",
    icon: FileBadge2
  },
  { to: "/notes", label: "Not Kontrolü", icon: BookCheck },
  { to: "/reports", label: "Raporlar", icon: BarChart3 },
  { to: "/settings", label: "Ayarlar", icon: Settings }
];

export default function AdminLayout() {
  const { admin, logout } = useAuth();

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand__mark">
            <ShieldCheck size={21} />
          </div>
          <div>
            <strong>NotMarket</strong>
            <span>Admin</span>
          </div>
        </div>

        <nav className="sidebar__nav" aria-label="Ana menü">
          {menu.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === "/"}
              className={({ isActive }) =>
                isActive ? "nav-item nav-item--active" : "nav-item"
              }
            >
              <Icon size={19} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>

        <button className="logout-button" type="button" onClick={logout}>
          <LogOut size={18} />
          Çıkış yap
        </button>
      </aside>

      <main className="main-area">
        <header className="topbar">
          <div>
            <span className="topbar__eyebrow">Yönetim merkezi</span>
            <strong>Operasyon ve moderasyon paneli</strong>
          </div>

          <div className="admin-chip">
            <div className="admin-chip__avatar">
              {admin?.displayName.charAt(0).toUpperCase()}
            </div>
            <div>
              <strong>{admin?.displayName}</strong>
              <span>{admin?.email}</span>
            </div>
          </div>
        </header>

        <section className="page-content">
          <Outlet />
        </section>
      </main>
    </div>
  );
}
