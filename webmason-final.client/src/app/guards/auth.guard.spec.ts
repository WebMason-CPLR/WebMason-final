import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthGuard } from './auth.guard';

describe('AuthGuard', () => {
  let authGuard: AuthGuard;
  let router: Router;

  beforeEach(() => {
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        AuthGuard,
        { provide: Router, useValue: routerSpy }
      ]
    });

    authGuard = TestBed.inject(AuthGuard);
    router = TestBed.inject(Router);
  });

  it('should be created', () => {
    expect(authGuard).toBeTruthy();
  });

  it('should allow the authenticated user to access app', () => {
    spyOn(localStorage, 'getItem').and.returnValue('fake-token');
    const routeMock: any = { snapshot: {} };
    const routeStateMock: any = { snapshot: {}, url: '/' };
    expect(authGuard.canActivate(routeMock, routeStateMock)).toEqual(true);
  });

  it('should redirect an unauthenticated user to the login route', () => {
    spyOn(localStorage, 'getItem').and.returnValue(null);
    const routeMock: any = { snapshot: {} };
    const routeStateMock: any = { snapshot: {}, url: '/' };

    expect(authGuard.canActivate(routeMock, routeStateMock)).toEqual(false);
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});

