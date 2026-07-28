import {
    Navigate,
    Outlet,
  } from "react-router-dom";
  
  import { useStudentAuth } from
    "../auth/StudentAuthContext";
  
  export default function StudentProtectedRoute() {
    const {
      isStudentAuthenticated,
    } = useStudentAuth();
  
    return isStudentAuthenticated
      ? <Outlet />
      : (
        <Navigate
          to="/student/login"
          replace
        />
      );
  }