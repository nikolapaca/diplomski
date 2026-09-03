import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';

@Injectable({
    providedIn: 'root',
  })
  export class TokenService {
    public constructor() {}
  
    public saveAccessToken(token: string): void {
      localStorage.removeItem("access-token");
      localStorage.setItem("access-token", token);
    }
  
    public getAccessToken() : string | null {
      return localStorage.getItem("access-token");
    }

    public getDecodedAccessToken() : any {
      const token = this.getAccessToken();
      if(token){
        try{
          return jwtDecode(token);
        }catch (error) {
        console.error("Error:", error);
        return null;
        }
      }
    }
  
    public clear() : void {
      localStorage.removeItem("access-token");
    }
  }