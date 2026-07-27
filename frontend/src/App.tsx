import { Navigate, Route, Routes } from "react-router-dom";
import ProtectedRoute from "./components/ProtectedRoute";
import AdminLayout from "./layout/AdminLayout";
import DashboardPage from "./pages/DashboardPage";
import LoginPage from "./pages/LoginPage";
import NotesPage from "./pages/NotesPage";
import PlaceholderPage from "./pages/PlaceholderPage";
import UsersPage from "./pages/UsersPage";
import VerificationsPage from "./pages/VerificationsPage";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AdminLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="users" element={<UsersPage />} />
          <Route
            path="verifications"
            element={<VerificationsPage />}
          />
          <Route path="notes" element={<NotesPage />} />
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

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
