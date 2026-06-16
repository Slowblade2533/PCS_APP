import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CategoryBatchCreateDto,
  CategoryCreateDto,
  CategoryDto,
  CategoryUpdateDto,
} from '../models/category.models';

@Injectable({
  providedIn: 'root',
})
export class CategoryService {
  private http = inject(HttpClient);

  private apiUrl = `${environment.apiUrl}/categories`;

  createCategory(data: CategoryCreateDto): Observable<any> {
    return this.http.post<any>(this.apiUrl, data, { withCredentials: true });
  }

  createCategoryBatch(data: CategoryBatchCreateDto): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/batch`, data, { withCredentials: true });
  }

  deleteCategory(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`, { withCredentials: true });
  }

  getAllCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(this.apiUrl, { withCredentials: true });
  }

  getCategoryById(id: number): Observable<CategoryDto> {
    return this.http.get<CategoryDto>(`${this.apiUrl}/${id}`, { withCredentials: true });
  }

  searchCategories(query?: string): Observable<CategoryDto[]> {
    let params = new HttpParams();
    if (query) {
      params = params.set('q', query);
    }
    return this.http.get<CategoryDto[]>(`${this.apiUrl}/search`, { params, withCredentials: true });
  }

  updateCategory(id: number, data: CategoryUpdateDto): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data, { withCredentials: true });
  }
}
