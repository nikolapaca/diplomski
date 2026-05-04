// src/app/core/service/ai.service.ts

import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environment';
interface AiResponse {
    response: string;
}

@Injectable({
  providedIn: 'root'
})
export class AiService {

  constructor(private http: HttpClient) { }
  extractTasks(cardDescription: string): Observable<string> {
    const requestBody = { prompt: cardDescription }; 
    
    return this.http.post<AiResponse>(`${environment.api}/Ollama/ask`, requestBody)
      .pipe(
        map(res => res.response) 
      );
  }
}