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
        description: 'An advanced server for larger projects.',
        image: 'assets/images/advanced-server.png'
      },
      {
        name: 'Serveur redmine',
        description: 'A professional server for enterprise solutions.',
        image: 'assets/images/pro-server.png'
      }
    ];
  }

  orderServer(server: any): void {
    this.loading = true;
    const config = {
      UserId: 'some-user-id',  // Remplacez par l'ID utilisateur réel
      MysqlRootPassword: 'rootpassword',
      MysqlDatabase: 'wordpress',
      MysqlUser: 'wpuser',
      MysqlPassword: 'wppassword',
      HostPort: 8080
    };

    this.serverService.deployWordpress(config).subscribe(
      response => {
        console.log('WordPress deployed successfully', response);
        this.loading = false;
        alert('WordPress deployed successfully');
        // You can navigate to a different page if needed
      },
      error => {
        console.error('Error deploying WordPress', error);
        this.loading = false;
        alert('Error deploying WordPress');
      }
    );
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }
}
