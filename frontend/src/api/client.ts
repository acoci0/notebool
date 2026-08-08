import axios from "axios";

const configuredBaseUrl =
  (
    import.meta.env.VITE_API_BASE_URL ??
    "https://localhost:7001"
  )
    .trim()
    .replace(/\/+$/, "");

const apiBaseUrl =
  configuredBaseUrl.endsWith("/api")
    ? configuredBaseUrl
    : `${configuredBaseUrl}/api`;

const api = axios.create({
  baseURL: apiBaseUrl,
  timeout: 15000,
});

api.interceptors.request.use(
  (config) => {
    const token =
      localStorage.getItem(
        "notmarket_admin_token"
      );

    if (token) {
      config.headers.Authorization =
        `Bearer ${token}`;
    }

    return config;
  }
);

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (
      error.response?.status === 401
    ) {
      localStorage.removeItem(
        "notmarket_admin_token"
      );

      localStorage.removeItem(
        "notmarket_admin_profile"
      );

      if (
        window.location.pathname !==
        "/login"
      ) {
        window.location.href =
          "/login";
      }
    }

    return Promise.reject(error);
  }
);

export default api;