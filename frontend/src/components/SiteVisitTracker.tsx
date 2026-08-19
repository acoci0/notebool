import { useEffect } from "react";
import { useLocation } from "react-router-dom";
import api from "../api/client";

const sessionStorageKey = "notmarket_analytics_session";

function getSessionId() {
  const existing = localStorage.getItem(sessionStorageKey);

  if (existing) {
    return existing;
  }

  const sessionId = `${crypto.randomUUID()}-${crypto.randomUUID()}`;
  localStorage.setItem(sessionStorageKey, sessionId);
  return sessionId;
}

export default function SiteVisitTracker() {
  const location = useLocation();

  useEffect(() => {
    const controller = new AbortController();

    api
      .post(
        "/analytics/visits",
        {
          sessionId: getSessionId(),
          path: location.pathname
        },
        {
          signal: controller.signal
        }
      )
      .catch(() => {
        // Analitik kaydı, sayfanın çalışmasını engellememelidir.
      });

    return () => controller.abort();
  }, [location.pathname]);

  return null;
}
