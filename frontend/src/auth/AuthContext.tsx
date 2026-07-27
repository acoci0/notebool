import {
  createContext,
  useContext,
  useMemo,
  useState,
  type ReactNode
} from "react";
import api from "../api/client";
import type { AdminProfile, LoginResponse } from "../types";

type AuthContextValue = {
  admin: AdminProfile | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredAdmin(): AdminProfile | null {
  const value = localStorage.getItem("notmarket_admin_profile");

  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value) as AdminProfile;
  } catch {
    localStorage.removeItem("notmarket_admin_profile");
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [admin, setAdmin] = useState<AdminProfile | null>(
    readStoredAdmin()
  );

  const login = async (email: string, password: string) => {
    const { data } = await api.post<LoginResponse>("/auth/admin/login", {
      email,
      password
    });

    localStorage.setItem("notmarket_admin_token", data.accessToken);
    localStorage.setItem(
      "notmarket_admin_profile",
      JSON.stringify(data.admin)
    );

    setAdmin(data.admin);
  };

  const logout = () => {
    localStorage.removeItem("notmarket_admin_token");
    localStorage.removeItem("notmarket_admin_profile");
    setAdmin(null);
  };

  const value = useMemo<AuthContextValue>(
    () => ({
      admin,
      isAuthenticated:
        Boolean(admin) &&
        Boolean(localStorage.getItem("notmarket_admin_token")),
      login,
      logout
    }),
    [admin]
  );

  return (
    <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
  );
}

export function useAuth() {
  const value = useContext(AuthContext);

  if (!value) {
    throw new Error("useAuth, AuthProvider içinde kullanılmalıdır.");
  }

  return value;
}
