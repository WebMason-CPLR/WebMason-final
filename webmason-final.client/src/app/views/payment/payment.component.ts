import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { StripeService, StripeCardComponent } from 'ngx-stripe';
import {
  StripeCardElementOptions,
  StripeElementsOptions,
  StripeCardElement
} from '@stripe/stripe-js';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { ServerService } from '../../services/server.service';

@Component({
  selector: 'app-payment',
  templateUrl: './payment.component.html',
  styleUrls: ['./payment.component.scss']
})
export class PaymentComponent implements OnInit {
  paymentForm: FormGroup;
  server: any;
  loading: boolean = false;
  cardElement: StripeCardElement | null = null;

  cardOptions: StripeCardElementOptions = {
    style: {
      base: {
        iconColor: '#666EE8',
        color: '#31325F',
        lineHeight: '40px',
        fontWeight: 300,
        fontFamily: '"Helvetica Neue", Helvetica, sans-serif',
        fontSize: '18px',
        '::placeholder': {
          color: '#CFD7E0'
        }
      }
    }
  };

  elementsOptions: StripeElementsOptions = {
    locale: 'en'
  };

  constructor(
    private fb: FormBuilder,
    private stripeService: StripeService,
    private http: HttpClient,
    private router: Router,
    private serverService: ServerService
  ) {
    const navigation = this.router.getCurrentNavigation();
    this.server = navigation?.extras.state?.['server'] ?? null;
    this.paymentForm = this.fb.group({
      name: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.stripeService.elements().subscribe(elements => {
      if (elements) {
        this.cardElement = elements.create('card', this.cardOptions);
        this.cardElement.mount('#card-element');
      }
    });
  }

  pay(): void {
    if (this.paymentForm.valid && this.cardElement) {
      this.loading = true;
      this.stripeService
        .createToken(this.cardElement, { name: this.paymentForm.get('name')?.value })
        .subscribe((result) => {
          if (result.token) {
            this.processPayment(result.token.id);
          } else if (result.error) {
            console.log(result.error.message);
            this.loading = false;
          }
        });
    }
  }

  processPayment(token: string): void {
    const paymentRequest = {
      token: token,
      amount: 1000, // Amount in cents
      currency: 'usd',
      description: 'Server Deployment',
      serverType: this.server?.name,
      userId: 'some-user-id' // Replace with actual user ID
    };

    this.http.post('/api/payment/process', paymentRequest).subscribe((response: any) => {
      console.log('Payment successful', response);
      this.deployServer();
    }, (error) => {
      console.log('Payment failed', error);
      this.loading = false;
    });
  }

  deployServer(): void {
    if (!this.server) {
      console.error('Server information is missing');
      return;
    }

    this.loading = true;
    if (this.server.name === 'Serveur Wordpress') {
      this.orderWordpressServer();
    } else if (this.server.name === 'Serveur Odoo') {
      this.orderOdooServer();
    } else if (this.server.name === 'Serveur Redmine') {
      this.orderRedmineServer();
    } else {
      console.error('Unknown server type');
      this.loading = false;
    }
  }

  orderWordpressServer(): void {
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

  orderOdooServer(): void {
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

  orderRedmineServer(): void {
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
}
