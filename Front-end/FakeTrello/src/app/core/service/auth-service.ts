import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../../environment';
import { CredentialsDTO } from '../model/login-credentials.model';
import { AccessToken } from '../model/access-token.model';
import { jwtDecode } from 'jwt-decode';
import { isPlatformBrowser } from '@angular/common';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private _isLoggedIn = new BehaviorSubject<boolean>(false);
  public isLoggedIn$ = this._isLoggedIn.asObservable();
  
  public constructor(@Inject(PLATFORM_ID) private platformId: Object, private http: HttpClient) { }

  public loginUser(credentials: CredentialsDTO) : Observable<AccessToken> {
    return this.http.post<AccessToken>(`${environment.api}/auth/login`, credentials);
  }

  public isAuthenticated(): boolean {
    if (isPlatformBrowser(this.platformId)) {
      const token = localStorage.getItem('access-token');

      if (!token || token.trim() === '') {
        return false;
      }
      
      try {
        const decoded: any = jwtDecode(token);
        if (decoded.exp * 1000 < Date.now()) {
          localStorage.removeItem('access-token');
          return false;
        }
        this._isLoggedIn.next(true);
        return true;
      } catch (error) {
        localStorage.removeItem('access-token');
        return false;
      }
    }
    return false;
  }

  public getLoggedInUser(): string | null {
    if (isPlatformBrowser(this.platformId)) {
      const token = localStorage.getItem('access-token');
      if (token) {
        try {
          const decoded: any = jwtDecode(token);
          return decoded.username;
        } catch (error) {
          console.error('Failed to decode token', error);
          return null;
        }
      }
    }
    return null;
  }

  public confirmEmail(token: string) {

    return this.http.post(
      `${environment.api}/auth/confirm-email`,
      JSON.stringify(token),
      {
        headers: {
          'Content-Type': 'application/json'
        }
      }
    );

  }
}
