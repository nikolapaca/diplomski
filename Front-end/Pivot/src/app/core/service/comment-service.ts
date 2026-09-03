import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environment';
import { Comment } from '../model/comment.model';

@Injectable({
  providedIn: 'root'
})
export class CommentService {

  public constructor(private http: HttpClient) { }

  public getByCard(cardId: number): Observable<Comment[]> {
    return this.http.get<Comment[]>(`${environment.api}/comments/card/${cardId}`);
  }

  public addComment(cardId: number, text: string): Observable<Comment> {
    return this.http.post<Comment>(`${environment.api}/comments/card/${cardId}`, { text });
  }

  public deleteComment(commentId: number): Observable<void> {
    return this.http.delete<void>(`${environment.api}/comments/${commentId}`);
  }
}
