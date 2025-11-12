import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { CardList } from "../model/cardList.model";
import { Observable } from "rxjs";
import { environment } from "../../../environment";
import { Card } from "../model/card.model";
import { User } from "../model/user.model";

@Injectable({
  providedIn: 'root'
})
export class CardService {

    public constructor(private http: HttpClient){}

    public addCard(listId: number, card: Card) : Observable<Card> {
        return this.http.post<Card>(`${environment.api}/cards/${listId}`, card)
    }

    public getCardsByListId(listId: number): Observable<Card[]>{
        return this.http.get<Card[]>(`${environment.api}/cards/${listId}`);
    }

    public deleteCard(cardId: number) : Observable<void> {
        return this.http.delete<void>(`${environment.api}/cards/${cardId}`)
    }

    public updateCard(card: Card) : Observable<void> {
        return this.http.put<void>(`${environment.api}/cards`, card);
    }

    public assignCard(card: Card, username: string) : Observable<void> {
        return this.http.post<void>(`${environment.api}/cards/assign/${username}`, card)
    }

    public unassignCard(card: Card) : Observable<void> {
        return this.http.post<void>(`${environment.api}/cards/unassign`, card)
    }

    public getUserAssignedToCard(card: Card) : Observable<User> {
        return this.http.get<User>(`${environment.api}/cards/assignedUser/${card.id}`);
    }
    public reorderCardInsideList(card: Card | undefined, newIndex: number) : Observable<void> {
        return this.http.post<void>(`${environment.api}/cards/reorderInsideList/${newIndex}`, card);
    }

    public reorderCardOutsideList(card: Card | undefined, targetListId: number | undefined, targetIndex: number) : Observable<void> {
        return this.http.post<void>(`${environment.api}/cards/reorderOutsideList/${targetListId}/${targetIndex}`, card);
    }
}