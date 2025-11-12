import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environment';
import { User } from '../model/user.model';
import { TokenService } from './token-service';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  
  public constructor(private http: HttpClient, private tokenService: TokenService) { }

  public registerUser(user: User) : Observable<User> {
    return this.http.post<User>(`${environment.api}/users/register`, user);
  }

  public getUsername() : string {
    const decodedToken = this.tokenService.getDecodedAccessToken();

    return decodedToken ? decodedToken.username : '';
  }

  public getUsersNotAssignedToThisBoard(searchTerm: string | null, boardName: string, boardOwnerUsername: string) : Observable<User[]> {
    let params = new HttpParams()
      .set('searchTerm', searchTerm || '')
      .set('boardName', boardName)
      .set('boardOwnerUsername', boardOwnerUsername);
    return this.http.get<User[]>(`${environment.api}/users/search/offBoard`, { params })
  }

  public getUsersAssignedToThisBoard(searchTerm: string | null, boardName: string, boardOwnerUsername: string) : Observable<User[]> {
    let params = new HttpParams()
      .set('searchTerm', searchTerm || '')
      .set('boardName', boardName)
      .set('boardOwnerUsername', boardOwnerUsername);
    return this.http.get<User[]>(`${environment.api}/users/search/onBoard`, { params })
  }

  public getAssignableUsersToThisBoard(searchTerm: string | null, boardName: string, boardOwnerUsername: string, cardId: number) : Observable<User[]>{
    let params = new HttpParams()
      .set('searchTerm', searchTerm || '')
      .set('boardName', boardName)
      .set('boardOwnerUsername', boardOwnerUsername)
      .set('cardId', cardId);
    return this.http.get<User[]>(`${environment.api}/users/search/assignableOnBoard`, { params })
  }
}
