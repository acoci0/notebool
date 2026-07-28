import {
  Navigate,
  Route,
  Routes,
} from "react-router-dom";

import ProtectedRoute from "./components/ProtectedRoute";
import StudentProtectedRoute from "./components/StudentProtectedRoute";

import AdminLayout from "./layout/AdminLayout";

import DashboardPage from "./pages/DashboardPage";
import LoginPage from "./pages/LoginPage";
import NotesPage from "./pages/NotesPage";
import PlaceholderPage from "./pages/PlaceholderPage";
import StudentLoginPage from "./pages/StudentLoginPage";
import StudentProfilePage from "./pages/StudentProfilePage";
import UsersPage from "./pages/UsersPage";
import VerificationsPage from "./pages/VerificationsPage";

export default function App() {
  return (
    <Routes>
      {/* ========================= */}
      {/* ÖĞRENCİ TARAFI            */}
      {/* ========================= */}

      <Route
        path="/student/login"
        element={<StudentLoginPage />}
      />

      <Route
        element={<StudentProtectedRoute />}
      >
        <Route
          path="/student/profile"
          element={<StudentProfilePage />}
        />
      </Route>

      {/* ========================= */}
      {/* ADMIN TARAFI              */}
      {/* ========================= */}

      <Route
        path="/login"
        element={<LoginPage />}
      />

      <Route
        element={<ProtectedRoute />}
      >
        <Route
          element={<AdminLayout />}
        >
          <Route
            index
            element={<DashboardPage />}
          />

          <Route
            path="users"
            element={<UsersPage />}
          />

          <Route
            path="verifications"
            element={<VerificationsPage />}
          />

          <Route
            path="notes"
            element={<NotesPage />}
          />

          <Route
            path="reports"
            element={
              <PlaceholderPage
                title="Raporlar"
                description="Satış, komisyon ve operasyon raporları."
              />
            }
          />

          <Route
            path="settings"
            element={
              <PlaceholderPage
                title="Ayarlar"
                description="Komisyon, fiyatlandırma ve sistem ayarları."
              />
            }
          />
        </Route>
      </Route>

      {/* ========================= */}
      {/* BİLİNMEYEN ADRESLER       */}
      {/* ========================= */}

      <Route
        path="*"
        element={
          <Navigate
            to="/"
            replace
          />
        }
      />
    </Routes>
  );
}