import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.models';
import {
  Branch,
  Role,
  UserCreateRequest,
  UserDetail,
  UserListItem,
  UserListQuery,
  UserUpdateRequest,
  Permission,
} from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UserManagementService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/users`;

  getUsers(query: UserListQuery): Observable<PagedResult<UserListItem>> {
    let params = new HttpParams().set('pageNumber', query.page).set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.isActive !== undefined) params = params.set('isActive', query.isActive);
    return this.http.get<PagedResult<UserListItem>>(this.apiUrl, { params });
  }

  getUserById(id: number): Observable<UserDetail> {
    return this.http.get<UserDetail>(`${this.apiUrl}/${id}`);
  }

  createUser(payload: UserCreateRequest): Observable<UserDetail> {
    return this.http.post<UserDetail>(this.apiUrl, payload);
  }

  updateUser(id: number, payload: UserUpdateRequest): Observable<UserDetail> {
    return this.http.put<UserDetail>(`${this.apiUrl}/${id}`, payload);
  }

  deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  toggleUserActive(id: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/active`, { isActive });
  }

  getRoles(): Observable<Role[]> {
    return this.http.get<Role[]>(`${environment.apiUrl}/roles`);
  }

  getBranches(): Observable<Branch[]> {
    return this.http.get<Branch[]>(`${environment.apiUrl}/branches`);
  }

  getPermissions(): Observable<Permission[]> {
    return this.http.get<Permission[]>(`${this.apiUrl}/permissions`);
  }
}
