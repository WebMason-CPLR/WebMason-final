import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {
  user = {
    username: '',
    email: '',
    password: ''
  };

  constructor(private authService: AuthService, private router: Router) { }

  onSubmit() {
    this.authService.register(this.user).subscribe(response => {
      console.log('User registered successfully', response);
      this.router.navigate(['/login']);
    }, error => {
      console.error('Error registering user', error);
    });
  }
}

