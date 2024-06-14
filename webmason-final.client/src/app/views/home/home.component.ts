import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ServerService } from '../../services/server.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  servers: any[] = [];
  loading: boolean = false;

  constructor(private router: Router, private serverService: ServerService) { }

  ngOnInit(): void {
    this.servers = [
      {
        name: 'Serveur Wordpress',
        description: 'Un serveur simple deployant un wordpress et un mysql prêt à la configuration',
        image: 'assets/images/basic-server.png'
      },
      {
        name: 'Serveur Odoo',
        description: 'Un serveur simple déployant un Odoo prêt à être configuré',
        image: 'assets/images/advanced-server.png'
      },
      {
        name: 'Serveur Redmine',
        description: 'Un serveur simple déployant un Redmine² prêt à être configuré',
        image: 'assets/images/pro-server.png'
      }
    ];
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  redirectToPayment(server: any): void {
    this.router.navigate(['/payment'], { state: { server } });
  }
}
