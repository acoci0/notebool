export type AdminProfile = {
  id: string;
  email: string;
  displayName: string;
  role: string;
};

export type LoginResponse = {
  accessToken: string;
  expiresAt: string;
  admin: AdminProfile;
};

export type DashboardSummary = {
  totalUsers: number;
  activeUsers: number;
  pendingVerifications: number;
  pendingNoteReviews: number;
  approvedNotes: number;
  totalRevenue: number;
  recentActivities: Array<{
    action: string;
    entityType: string;
    entityId: string;
    createdAt: string;
  }>;
};

export type AdminUser = {
  id: string;
  email: string;
  displayName: string;
  role: string;
  status: string;
  verificationCount: number;
  createdAt: string;
};

export type Verification = {
  id: string;
  userId: string;
  userDisplayName: string;
  userEmail: string;
  universityName: string;
  facultyName: string;
  departmentName: string;
  documentIssueDate: string;
  status: string;
  documentBlobPath: string;
  reviewNote?: string | null;
  reviewedAt?: string | null;
  expiresAt?: string | null;
  createdAt: string;
};

export type VerificationDetail = Verification & {
  documentHash: string;
};

export type NoteSubmission = {
  id: string;
  title: string;
  sellerName: string;
  universityName: string;
  departmentName: string;
  courseName: string;
  matchScore: number;
  readabilityScore: number;
  originalityRiskScore: number;
  status: string;
  generatedPdfBlobPath?: string | null;
  createdAt: string;
};

export type StudentProfile = {
  id: string;
  email: string;
  displayName: string;
  role: string;
};

export type StudentLoginResponse = {
  accessToken: string;
  expiresAt: string;
  student: StudentProfile;
};

export type StudentVerificationItem = {
  id: string;
  universityName: string;
  facultyName: string;
  departmentName: string;
  status: string;
  documentIssueDate: string;
  reviewNote?: string | null;
  reviewedAt?: string | null;
  expiresAt?: string | null;
  createdAt: string;
};

export type AcademicUniversity = {
  id: string;
  name: string;
};

export type AcademicUnit = {
  id: string;
  universityId: string;
  name: string;
  unitType: string;
};

export type AcademicProgram = {
  id: string;
  academicUnitId: string;
  name: string;
};

