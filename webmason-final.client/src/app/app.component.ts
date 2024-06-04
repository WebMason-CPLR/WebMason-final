import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { AuthService } from './services/auth.service';
import { Router } from '@angular/router';
import { ServerService } from './services/server.service';
import { MessageService } from 'primeng/api';

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
  constructor(private http: HttpClient, private authService: AuthService, private router: Router, private serveurService : ServerService, private messageService : MessageService) {}

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

  deleteAllContainers(): void {
    this.serveurService.deleteAllContainers().subscribe(
      response => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'All containers deleted successfully' });
      },
      error => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Error deleting containers' });
      }
    );
  }

  logout(): void {
    this.authService.logout();
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
