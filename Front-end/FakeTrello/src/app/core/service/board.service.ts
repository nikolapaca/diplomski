import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Board } from "../model/board.model";
import { environment } from "../../../environment";
import { BehaviorSubject, catchError, distinctUntilChanged, merge, Observable, of, Subject, switchMap, take, tap } from "rxjs";
import { BoardUpdate } from "../model/board-update.model";
import { BoardMember } from "../model/board-member.model";

@Injectable({
  providedIn: 'root'
})
export class BoardService {
  private _boardsSubject = new BehaviorSubject<Board[]>([]);
  public boards$: Observable<Board[]> = this._boardsSubject.asObservable();

  private searchTermSubject = new Subject<string>();

  public constructor(
    private http: HttpClient
  ) {
    this.searchTermSubject.pipe(
      distinctUntilChanged(),
      switchMap(query => this.fetchBoards(query)),
      catchError(() => of([]))
    ).subscribe(boards => {
      this._boardsSubject.next(boards);
    });
  }

  public fetchBoards(query: string): Observable<Board[]> {
    if (query.length > 0) {
      return this.searchBoards(query).pipe(
        catchError(() => of([]))
      );
    } else {
      return this.getAllBoards().pipe(
        catchError(() => of([]))
      );
    }
  }

  public search(searchTerm: string): void {
    this.searchTermSubject.next(searchTerm);
  }

  public createBoard(board: Board): Observable<Board> {
    return this.http.post<Board>(`${environment.api}/boards`, board);
  }

  public getAllBoards(): Observable<Board[]> {
    return this.http.get<Board[]>(`${environment.api}/boards`);
  }

  public searchBoards(searchTerm: string): Observable<Board[]> {
    return this.http.get<Board[]>(`${environment.api}/boards/search?query=${searchTerm}`).pipe(take(1)
    );
  }

  public updateBoard(board: BoardUpdate) : Observable<Board> {
    return this.http.put<Board>(`${environment.api}/boards`, board);
  }

  public getBoardByNameAndOwnerUsername(name: string | null, username: string | null): Observable<Board> {
    return this.http.get<Board>(`${environment.api}/boards/${name}/${username}`)
  }

  public getMembers(name: string, ownerUsername: string): Observable<BoardMember[]> {
    return this.http.get<BoardMember[]>(`${environment.api}/boards/${name}/${ownerUsername}/members`);
  }

  public removeCollaborator(board: Board | undefined, username: string) : Observable<void>{
    return this.http.post<void>(`${environment.api}/boards/collaborator/remove/${username}`, board);
  }

  public addCollaborator(board: Board, username: string) : Observable<void>{
    return this.http.post<void>(`${environment.api}/boards/collaborator/add/${username}`, board);
  }

  public leaveBoard(board: Board | undefined): Observable<void> {
    return this.http.post<void>(`${environment.api}/boards/leave`, board);
  }

  public toggleFavorite(name: string, ownerUsername: string): Observable<void> {
    return this.http.post<void>(`${environment.api}/boards/favorite/${name}/${ownerUsername}`, {});
  }

  public archiveBoard(name: string, ownerUsername: string): Observable<void> {
    return this.http.post<void>(`${environment.api}/boards/archive/${name}/${ownerUsername}`, {}).pipe(
      tap(() => {
        this.refreshState();
      })
    );
  }

  public deleteBoard(name: string | null, username: string | null): Observable<void> {
    return this.http.delete<void>(`${environment.api}/boards/${name}/${username}`).pipe(
      tap(() => {
        this.refreshState();
      })
    );
  }

  public refreshState(): void {
    this.fetchBoards("")
      .pipe(take(1))
      .subscribe(resp => {
        this._boardsSubject.next(resp);
      });
  }
}