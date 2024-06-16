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
        description: 'Un serveur simple déployant un wordpress et un mysql prêt à la configuration',
        image: 'assets/images/basic-server.png',
        prix: "5€/mois"
      },
      {
        name: 'Serveur Odoo',
        description: 'Un serveur simple déployant un Odoo prêt à être configuré',
        image: 'assets/images/advanced-server.png',
        prix: "5€/mois"
      },
      {
        name: 'Serveur Redmine',
        description: "Un serveur simple déployant un Redmine prêt à l'emploi",
        image: 'assets/images/pro-server.png',
        prix: "5€/mois"
      },
      {
        name: 'Serveur Wordpress pro',
        description: 'Un serveur puissant déployant un Wordpress prêt à être configuré',
        image: 'assets/images/basic-server-pro.png',
        prix: "10€/mois"
      },
      {
        name: 'Serveur Odoo pro',
        description: 'Un serveur puissant déployant un Odoo prêt à être configuré',
        image: 'assets/images/advanced-server-pro.png',
        prix: "10€/mois"
      },
      {
        name: 'Serveur Redmine pro',
        description: "Un serveur puissant déployant un Redmine prêt à l'emploi",
        image: 'assets/images/pro-server-pro.png',
        prix: "10€/mois"
      },
      {
        name: 'Serveur Wordpress ultimate',
        description: 'Un serveur très puissant déployant un Wordpress prêt à être configuré',
        image: 'assets/images/basic-server-ultimate.png',
        prix: "15€/mois"
      },
      {
        name: 'Serveur Odoo ultimate',
        description: 'Un serveur très puissant déployant un Odoo prêt à être configuré',
        image: 'assets/images/advanced-server-ultimate.png',
        prix: "15€/mois"
      },
      {
        name: 'Serveur Redmine ultimate',
        description: "Un serveur très puissant déployant un Redmine prêt à l'emploi",
        image: 'assets/images/pro-server-ultimate.png',
        prix: "15€/mois"
      },
    ];
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  redirectToPayment(server: any): void {
    this.router.navigate(['/payment'], { state: { server } });
  }
}
