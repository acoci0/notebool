import axios from "axios";

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ??
  "https://localhost:7001";

const studentApi = axios.create({
  baseURL: `${apiBaseUrl}/api`,
  timeout: 15000,
});

studentApi.interceptors.request.use(
  (config) => {
    const token =
      localStorage.getItem(
        "notmarket_student_token"
      );

    if (token) {
      config.headers = config.headers ?? {};
      (config.headers as Record<string, string>).Authorization = `Bearer ${token}`;
    }


    return config;
  }
);

studentApi.interceptors.response.use(
  (response) => response,

  (error) => {
    if (
      error.response?.status === 401
    ) {
      localStorage.removeItem(
        "notmarket_student_token"
      );

      localStorage.removeItem(
        "notmarket_student_profile"
      );

      if (
        window.location.pathname !==
        "/student/login"
      ) {
        window.location.href =
          "/student/login";
      }
    }

    return Promise.reject(error);
  }
);

export default studentApi;