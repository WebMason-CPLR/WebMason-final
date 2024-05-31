import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { AuthService } from './services/auth.service';
import { Router } from '@angular/router';

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  public forecasts: WeatherForecast[] = [];
  items: MenuItem[] = [];
  constructor(private http: HttpClient, private authService: AuthService, private router: Router) {}

  ngOnInit() {

    this.items = [
      {
        label: 'Accueil',
        icon: 'pi pi-fw pi-home',
        routerLink: ['/home']
      },
      {
        label: 'Inscription',
        icon: 'pi pi-fw pi-info',
        routerLink: ['/register']
      },
      {
        label: 'Connexion',
        icon: 'pi pi-fw pi-envelope',
        routerLink: ['/login']
      },
      {
        label: 'Settings',
        icon: 'pi pi-fw pi-cog',
        items: [
          { label: 'Profile', icon: 'pi pi-fw pi-user', routerLink: ['/profile'] },
          { label: 'Security', icon: 'pi pi-fw pi-lock', routerLink: ['/security'] }
        ]
      }
    ];
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  isLoggedIn() {
    return !!localStorage.getItem('token');
  }

  getForecasts() {
    this.http.get<WeatherForecast[]>('/weatherforecast').subscribe(
      (result) => {
        this.forecasts = result;
      },
      (error) => {
        console.error(error);
      }
    );
  }

  title = 'webmason-final.client';
}
