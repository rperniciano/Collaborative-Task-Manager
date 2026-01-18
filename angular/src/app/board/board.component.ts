import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService, ConfigStateService, EnvironmentService } from '@abp/ng.core';
import { Router } from '@angular/router';

interface ColumnDto {
  id: string;
  boardId: string;
  name: string;
  order: number;
}

interface BoardWithColumnsDto {
  id: string;
  name: string;
  ownerId: string;
  creationTime: string;
  columns: ColumnDto[];
}

@Component({
  selector: 'app-board',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './board.component.html',
  styleUrls: ['./board.component.scss']
})
export class BoardComponent implements OnInit {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private configState = inject(ConfigStateService);
  private router = inject(Router);
  private environment = inject(EnvironmentService);

  private get apiUrl(): string {
    return this.environment.getEnvironment().apis.default.url;
  }

  board = signal<BoardWithColumnsDto | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }

  get currentUserName(): string {
    const currentUser = this.configState.getOne('currentUser');
    return currentUser?.userName || 'User';
  }

  ngOnInit(): void {
    if (!this.isAuthenticated) {
      this.authService.navigateToLogin();
      return;
    }
    this.loadBoard();
  }

  private loadBoard(): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<BoardWithColumnsDto>(`${this.apiUrl}/api/app/board/board`).subscribe({
      next: (data) => {
        this.board.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load board:', err);
        this.error.set('Failed to load board. Please try again.');
        this.loading.set(false);
      }
    });
  }
}
