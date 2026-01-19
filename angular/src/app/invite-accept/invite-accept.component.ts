import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, EnvironmentService } from '@abp/ng.core';

interface InviteDto {
  id: string;
  boardId: string;
  boardName: string;
  email: string;
  token: string;
  expiresAt: string;
  createdAt: string;
  isExpired: boolean;
}

interface BoardDto {
  id: string;
  name: string;
  ownerId: string;
  creationTime: string;
}

@Component({
  selector: 'app-invite-accept',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './invite-accept.component.html',
  styleUrls: ['./invite-accept.component.scss']
})
export class InviteAcceptComponent implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private environment = inject(EnvironmentService);

  private get apiUrl(): string {
    return this.environment.getEnvironment().apis.default.url;
  }

  token = signal<string | null>(null);
  invite = signal<InviteDto | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  accepting = signal(false);
  acceptError = signal<string | null>(null);
  acceptSuccess = signal(false);

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }

  ngOnInit(): void {
    // Get token from query params
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.error.set('Invalid invite link. No token provided.');
      this.loading.set(false);
      return;
    }
    this.token.set(token);

    // Fetch invite details
    this.loadInvite(token);
  }

  private loadInvite(token: string): void {
    this.http.get<InviteDto>(`${this.apiUrl}/api/app/board/invite-by-token?token=${encodeURIComponent(token)}`).subscribe({
      next: (invite) => {
        this.invite.set(invite);
        this.loading.set(false);

        // Check if invite has expired
        if (invite.isExpired) {
          this.error.set('This invitation has expired. Please ask the board owner for a new invitation.');
        }
      },
      error: (err) => {
        console.error('Failed to load invite:', err);
        this.error.set('Invitation not found or has expired.');
        this.loading.set(false);
      }
    });
  }

  login(): void {
    // Store the return URL so we can redirect back after login
    const returnUrl = `/invite/accept?token=${this.token()}`;
    this.authService.navigateToLogin({ returnUrl });
  }

  acceptInvite(): void {
    const token = this.token();
    if (!token) {
      this.acceptError.set('No invite token.');
      return;
    }

    this.accepting.set(true);
    this.acceptError.set(null);

    this.http.post<BoardDto>(`${this.apiUrl}/api/app/board/accept-invite?token=${encodeURIComponent(token)}`, {}).subscribe({
      next: (board) => {
        this.accepting.set(false);
        this.acceptSuccess.set(true);

        // Redirect to the board after a short delay
        setTimeout(() => {
          this.router.navigate(['/board']);
        }, 1500);
      },
      error: (err) => {
        console.error('Failed to accept invite:', err);
        const errorMessage = err.error?.error?.message || err.error?.message || 'Failed to accept invitation. Please try again.';
        this.acceptError.set(errorMessage);
        this.accepting.set(false);
      }
    });
  }

  goHome(): void {
    this.router.navigate(['/']);
  }
}
