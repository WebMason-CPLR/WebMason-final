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

  orderWordpressServer(server: any): void {
    this.loading = true;
    const config = {
      UserId: 'some-user-id',  // sera remplacé par l'id de l'utilisateur connecté
      MysqlRootPassword: 'rootpassword',
      MysqlDatabase: 'wordpress',
      MysqlUser: 'wpuser',
      MysqlPassword: 'wppassword',
      HostPort: 8080 // sera remplacé par un port disponible côté serveur
    };

    this.serverService.deployWordpress(config).subscribe(
      response => {
        console.log('Wordpress déployé avec succès, retrouvez les infos sur votre service dans la page "Mes services"', response);
        this.loading = false;
        alert('Wordpress déployé avec succès, retrouvez les infos sur votre service dans la page "Mes services"');
        this.router.navigate(['/containers']);
      },
      error => {
        console.error('Error deploying WordPress', error);
        this.loading = false;
        alert('Erreur lors du déploiement de votre service, veuillez contacter le service technique');
      }
    );
  }


  orderOdooServer(server: any): void {
    this.loading = true;
    const config = {
      UserId: 'some-user-id',  // sera remplacé par l'id de l'utilisateur connecté
      PostgresPassword: 'postgrespassword',
      PostgresDatabase: 'odoo',
      PostgresUser: 'odoo',
      PostgresPort: 5432 // sera remplacé par un port disponible côté serveur
    };

    this.serverService.deployOdoo(config).subscribe(
      response => {
        console.log('Odoo déployé avec succès, retrouvez les infos sur votre service dans la page "Mes services"', response);
        this.loading = false;
        alert('Odoo déployé avec succès, retrouvez les infos sur votre service dans la page "Mes services"');
        this.router.navigate(['/containers']);
      },
      error => {
        console.error('Error deploying Odoo', error);
        this.loading = false;
        if (error.status === 400) {
          alert('Erreur de validation : ' + JSON.stringify(error.error.errors));
        } else {
          alert('Erreur lors du déploiement de votre service, veuillez contacter le service technique');
        }
      }
    );
  }

  orderRedmineServer(server: any): void {
    this.loading = true;
    const config = {
      UserId: 'some-user-id',  // sera remplacé par l'id de l'utilisateur connecté
      MysqlRootPassword: 'rootpassword',
      MysqlDatabase: 'redmine',
      MysqlUser: 'rmuser',
      MysqlPassword: 'rmpassword',
      HostPort: 3000 // sera remplacé par un port disponible côté serveur
    };

    this.serverService.deployRedmine(config).subscribe(
      response => {
        console.log('Redmine déployé avec succès, retrouvez les infos sur votre service dans la page "Mes services"', response);
        this.loading = false;
        alert('Redmine déployé avec succès, retrouvez les infos sur votre service dans la page "Mes services"');
        this.router.navigate(['/containers']);
      },
      error => {
        console.error('Error deploying Redmine', error);
        this.loading = false;
        alert('Erreur lors du déploiement de votre service, veuillez contacter le service technique');
      }
    );
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }
}
