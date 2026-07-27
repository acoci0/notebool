import { Search } from "lucide-react";
import { useEffect, useState } from "react";
import api from "../api/client";
import StatusBadge from "../components/StatusBadge";
import type { AdminUser } from "../types";

export default function UsersPage() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [search, setSearch] = useState("");

  const load = async () => {
    const { data } = await api.get<AdminUser[]>("/admin/users", {
      params: search ? { search } : undefined
    });
    setUsers(data);
  };

  useEffect(() => {
    void load();
  }, []);

  const toggleStatus = async (user: AdminUser) => {
    const nextStatus = user.status === "Active" ? "Suspended" : "Active";
    await api.patch(`/admin/users/${user.id}/status`, {
      status: nextStatus
    });
    await load();
  };

  return (
    <>
      <div className="page-heading">
        <div>
          <span className="section-kicker">Hesap yönetimi</span>
          <h1>Kullanıcılar</h1>
          <p>Hesap durumlarını ve doğrulama sayılarını yönetin.</p>
        </div>
      </div>

      <article className="panel">
        <div className="table-toolbar">
          <form
            className="search-box"
            onSubmit={(event) => {
              event.preventDefault();
              void load();
            }}
          >
            <Search size={18} />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="İsim veya e-posta ara"
            />
          </form>
        </div>

        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Kullanıcı</th>
                <th>Rol</th>
                <th>Durum</th>
                <th>Doğrulama</th>
                <th>Kayıt tarihi</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id}>
                  <td>
                    <strong>{user.displayName}</strong>
                    <span className="table-subtext">{user.email}</span>
                  </td>
                  <td>{user.role}</td>
                  <td>
                    <StatusBadge status={user.status} />
                  </td>
                  <td>{user.verificationCount}</td>
                  <td>
                    {new Date(user.createdAt).toLocaleDateString("tr-TR")}
                  </td>
                  <td className="table-actions">
                    {user.role !== "Admin" && (
                      <button
                        className={
                          user.status === "Active"
                            ? "secondary-button danger-text"
                            : "secondary-button"
                        }
                        onClick={() => void toggleStatus(user)}
                      >
                        {user.status === "Active"
                          ? "Askıya al"
                          : "Aktifleştir"}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </article>
    </>
  );
}
