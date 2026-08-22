import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environment';
import { CardImage } from '../model/card-image.model';

@Injectable({
  providedIn: 'root'
})
export class CardImageService {

  public constructor(private http: HttpClient) { }

  public getByCard(cardId: number): Observable<CardImage[]> {
    return this.http.get<CardImage[]>(`${environment.api}/card-images/card/${cardId}`);
  }

  public upload(cardId: number, file: File): Observable<CardImage> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.http.post<CardImage>(`${environment.api}/card-images/card/${cardId}`, formData);
  }

  public delete(imageId: number): Observable<void> {
    return this.http.delete<void>(`${environment.api}/card-images/${imageId}`);
  }
}