import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { CardList } from "../model/cardList.model";
import { Observable } from "rxjs";
import { environment } from "../../../environment";
import { CardListView } from "../model/card-list-view.model";

@Injectable({
  providedIn: 'root'
})
export class CardListService {

    public constructor(private http: HttpClient){

    }

    public addList(list: CardList): Observable<CardList> {
        return this.http.post<CardList>(`${environment.api}/cardLists`, list)
    }

    public getListsForBoard(boardName: string | undefined, boardOwnerUsername: string | undefined) : Observable<CardList[]>{
        return this.http.get<CardList[]>(`${environment.api}/cardLists/search?boardName=${boardName}&boardOwnerUsername=${boardOwnerUsername}`)
    }

    public deleteList(listId: number) : Observable<void> {
        return this.http.delete<void>(`${environment.api}/cardLists/${listId}`);
    }

    public updateList(list: CardList) : Observable<void> {
        return this.http.put<void>(`${environment.api}/cardLists`, list);
    }

    public reorderList(list: CardList | undefined, targetIndex: number) : Observable<void> {
        return this.http.post<void>(`${environment.api}/cardLists/reorderList/${targetIndex}`, list);
    }

    public togglePin(listId: number) : Observable<void> {
        return this.http.post<void>(`${environment.api}/cardLists/pin/${listId}`, {});
    }
}