import { Component, inject, OnInit } from '@angular/core';
import { AuthService, LocalizationPipe } from '@abp/ng.core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  imports: [LocalizationPipe]
})
export class HomeComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated
  }

  ngOnInit(): void {
    // If user is already logged in, redirect to board
    if (this.hasLoggedIn) {
      this.router.navigate(['/board']);
    }
  }

  login() {
    this.authService.navigateToLogin();
  }
}
