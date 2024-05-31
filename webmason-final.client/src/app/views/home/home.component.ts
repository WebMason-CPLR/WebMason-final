import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  servers: any[] = [];

  constructor(private router: Router) { }

  ngOnInit(): void {
    this.servers = [
      {
        name: 'Basic Server',
        description: 'A basic server suitable for small projects.',
        image: 'assets/images/basic-server.jpg'
      },
      {
        name: 'Advanced Server',
        description: 'An advanced server for larger projects.',
        image: 'assets/images/advanced-server.jpg'
      },
      {
        name: 'Pro Server',
        description: 'A professional server for enterprise solutions.',
        image: 'assets/images/pro-server.jpg'
      }
    ];
  }

  orderServer(server: any): void {
    // Here you can handle the logic for ordering a server
    // For example, navigate to an order page or call an order API
    console.log(`Ordering server: ${server.name}`);
    this.router.navigate(['/order'], { queryParams: { server: server.name } });
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }
}

