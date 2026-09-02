import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  public constructor() {
  }

  public intercept( request: HttpRequest<any>,next: HttpHandler): Observable<HttpEvent<any>> {
    const publicRoutes = ['/users/register', '/users/login']

    const isPublicRoute = publicRoutes.some(url => request.url.includes(url))

    if(isPublicRoute){
      return next.handle(request);
    }
    const accessTokenRequest = request.clone({
      setHeaders: {
        Authorization: `Bearer ` + localStorage.getItem("access-token"),
      },
    });
    return next.handle(accessTokenRequest);
  }
}