// ─── User & RBAC Models ─────────────────────────────────────────────────────
export interface User {
  id: string;
  email: string;
  name: string;
  roles?: string[];
  permissions?: string[];
}

export interface LoginCredentials {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface UserListItem {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
  roleId: number;
  roleName: string;
  permissions: UserPermissionSummary[];
}

export interface UserPermissionSummary {
  permissionId: number;
  permissionCode: string;
}

export interface UserDetail extends UserListItem {
  updatedAt: string;
}

export interface UserCreateRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roleId: number;
  permissionAssignments: UserPermissionAssignmentRequest[];
}

export interface UserUpdateRequest {
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roleId: number;
  permissionAssignments: UserPermissionAssignmentRequest[];
}

export interface UserPermissionAssignmentRequest {
  permissionId: number;
}

export interface Role {
  id: number;
  roleName: string;
  description: string | null;
}

export interface Permission {
  id: number;
  permissionCode: string;
  description: string | null;
}

export interface Branch {
  id: number;
  branchCode: string;
  branchName: string;
  isActive: boolean;
}

export interface UserListQuery {
  search?: string;
  isActive?: boolean;
  page: number;
  pageSize: number;
}
