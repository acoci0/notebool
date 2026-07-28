/* eslint-disable react-refresh/only-export-components */

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";

import studentApi from "../api/studentClient";

import type {
  StudentLoginResponse,
  StudentProfile,
} from "../types";

type StudentAuthContextValue = {
  student: StudentProfile | null;

  isStudentAuthenticated: boolean;

  login: (
    email: string,
    password: string
  ) => Promise<void>;

  logout: () => void;
};

const StudentAuthContext =
  createContext<
    StudentAuthContextValue | undefined
  >(undefined);

function readStoredStudent():
  StudentProfile | null {
  const value =
    localStorage.getItem(
      "notmarket_student_profile"
    );

  if (!value) {
    return null;
  }

  try {
    return JSON.parse(
      value
    ) as StudentProfile;
  } catch {
    localStorage.removeItem(
      "notmarket_student_profile"
    );

    return null;
  }
}

export function StudentAuthProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [student, setStudent] =
    useState<StudentProfile | null>(
      () => readStoredStudent()
    );

  const login = useCallback(
    async (
      email: string,
      password: string
    ) => {
      const { data } =
        await studentApi.post<
          StudentLoginResponse
        >(
          "/auth/student/login",
          {
            email,
            password,
          }
        );

      localStorage.setItem(
        "notmarket_student_token",
        data.accessToken
      );

      localStorage.setItem(
        "notmarket_student_profile",
        JSON.stringify(
          data.student
        )
      );

      setStudent(
        data.student
      );
    },
    []
  );

  const logout =
    useCallback(() => {
      localStorage.removeItem(
        "notmarket_student_token"
      );

      localStorage.removeItem(
        "notmarket_student_profile"
      );

      setStudent(null);
    }, []);

  const isStudentAuthenticated =
    Boolean(student) &&
    Boolean(
      localStorage.getItem(
        "notmarket_student_token"
      )
    );

  const value =
    useMemo<StudentAuthContextValue>(
      () => ({
        student,
        isStudentAuthenticated,
        login,
        logout,
      }),
      [
        student,
        isStudentAuthenticated,
        login,
        logout,
      ]
    );

  return (
    <StudentAuthContext.Provider
      value={value}
    >
      {children}
    </StudentAuthContext.Provider>
  );
}

export function useStudentAuth() {
  const context =
    useContext(
      StudentAuthContext
    );

  if (!context) {
    throw new Error(
      "useStudentAuth yalnızca StudentAuthProvider içerisinde kullanılabilir."
    );
  }

  return context;
}