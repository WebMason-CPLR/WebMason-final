import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ServerService {
  private apiUrl = '/api/wordpress';

  constructor(private http: HttpClient) { }

  deployWordpress(config: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/deploy`, config);
  }

  deleteAllContainers(): Observable<any> {
    return this.http.delete(`${this.apiUrl}/delete-all`);
  }

  getUserContainers(): Observable<any> {
    return this.http.get(`${this.apiUrl}/user-containers`);
  }

  deleteContainer(containerId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/delete/${containerId}`);
  }
}
