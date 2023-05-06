import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUri = environment.apiUrl;

  constructor(
    private readonly http: HttpClient
  ) { }

  public signOutExternal(): void {
    localStorage.removeItem('token');
  }

  public loginWithGoogle(credentials: string): Observable<any> {
    const header = new HttpHeaders().set('Content-Type', 'application/json');
    const url = `${this.baseUri}/api/auth/LoginWithGoogle`;
    return this.http.post(url, { credentials }, { headers: header });
  }
}
