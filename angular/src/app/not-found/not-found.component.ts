import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="not-found-container">
      <div class="not-found-content">
        <h1 class="not-found-title">404</h1>
        <h2 class="not-found-subtitle">Page Not Found</h2>
        <p class="not-found-message">
          The page you're looking for doesn't exist or has been moved.
        </p>
        <a routerLink="/" class="home-button">
          <span class="home-icon">&#8592;</span>
          Go to Home
        </a>
      </div>
    </div>
  `,
  styles: [`
    .not-found-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: calc(100vh - 200px);
      padding: 2rem;
      background-color: #F9FAFB;
    }

    .not-found-content {
      text-align: center;
      max-width: 500px;
    }

    .not-found-title {
      font-size: 8rem;
      font-weight: 700;
      color: #3B82F6;
      margin: 0;
      line-height: 1;
    }

    .not-found-subtitle {
      font-size: 2rem;
      font-weight: 600;
      color: #111827;
      margin: 1rem 0;
    }

    .not-found-message {
      font-size: 1.125rem;
      color: #6B7280;
      margin-bottom: 2rem;
    }

    .home-button {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.5rem;
      background-color: #3B82F6;
      color: white;
      font-weight: 600;
      text-decoration: none;
      border-radius: 8px;
      transition: background-color 0.2s ease;
    }

    .home-button:hover {
      background-color: #2563EB;
    }

    .home-icon {
      font-size: 1.25rem;
    }
  `]
})
export class NotFoundComponent {}
