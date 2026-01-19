import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService, ConfigStateService, EnvironmentService } from '@abp/ng.core';
import { Router } from '@angular/router';

interface TaskDto {
  id: string;
  columnId: string;
  title: string;
  description: string | null;
  dueDate: string | null;
  priority: number;
  assigneeId: string | null;
  assigneeName: string | null;
  order: number;
  creationTime: string;
  lastModificationTime: string | null;
}

interface ColumnDto {
  id: string;
  boardId: string;
  name: string;
  order: number;
}

interface ColumnWithTasks extends ColumnDto {
  tasks: TaskDto[];
}

interface BoardWithColumnsDto {
  id: string;
  name: string;
  ownerId: string;
  creationTime: string;
  columns: ColumnDto[];
  isOwner: boolean;
}

interface CreateTaskDto {
  columnId: string;
  title: string;
  description?: string;
  dueDate?: string;
  priority: number;
  assigneeId?: string;
}

interface InviteDto {
  id: string;
  boardId: string;
  email: string;
  token: string;
  expiresAt: string;
  createdAt: string;
  isExpired: boolean;
}

interface MemberDto {
  id: string;
  userId: string;
  email: string;
  displayName: string;
  joinedAt: string;
  isOwner: boolean;
}

@Component({
  selector: 'app-board',
  standalone: true,
  imports: [CommonModule, FormsModule],
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
  columnsWithTasks = signal<ColumnWithTasks[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  // Task creation state
  showAddTaskForm = signal<string | null>(null); // columnId if showing form, null otherwise
  newTaskTitle = signal('');
  creatingTask = signal(false);

  // Task detail modal state
  selectedTask = signal<TaskDto | null>(null);
  showTaskModal = signal(false);

  // Settings modal state
  showSettingsModal = signal(false);

  // Invite state
  inviteEmail = signal('');
  sendingInvite = signal(false);
  inviteError = signal<string | null>(null);
  inviteSuccess = signal<string | null>(null);
  pendingInvites = signal<InviteDto[]>([]);
  loadingInvites = signal(false);
  cancellingInvite = signal<string | null>(null);

  // Members state
  members = signal<MemberDto[]>([]);
  loadingMembers = signal(false);
  removingMember = signal<string | null>(null);

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }

  get currentUserName(): string {
    const currentUser = this.configState.getOne('currentUser');
    return currentUser?.userName || 'User';
  }

  get currentUserId(): string | null {
    const currentUser = this.configState.getOne('currentUser');
    return currentUser?.id || null;
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
        // Fallback for isOwner if not returned by backend (old server version)
        if (data.isOwner === undefined || data.isOwner === null) {
          data.isOwner = data.ownerId === this.currentUserId;
        }
        this.board.set(data);
        // Initialize columns with empty tasks array
        const columnsWithTasks = data.columns.map(col => ({
          ...col,
          tasks: [] as TaskDto[]
        }));
        this.columnsWithTasks.set(columnsWithTasks);
        this.loading.set(false);
        // Load tasks after board is loaded
        this.loadTasks();
      },
      error: (err) => {
        console.error('Failed to load board:', err);
        this.error.set('Failed to load board. Please try again.');
        this.loading.set(false);
      }
    });
  }

  private loadTasks(): void {
    this.http.get<TaskDto[]>(`${this.apiUrl}/api/app/task/tasks`).subscribe({
      next: (tasks) => {
        // Group tasks by columnId
        const columns = this.columnsWithTasks();
        const updatedColumns = columns.map(col => ({
          ...col,
          tasks: tasks.filter(t => t.columnId === col.id).sort((a, b) => a.order - b.order)
        }));
        this.columnsWithTasks.set(updatedColumns);
      },
      error: (err) => {
        console.error('Failed to load tasks:', err);
      }
    });
  }

  openAddTaskForm(columnId: string): void {
    this.showAddTaskForm.set(columnId);
    this.newTaskTitle.set('');
  }

  cancelAddTask(): void {
    this.showAddTaskForm.set(null);
    this.newTaskTitle.set('');
  }

  createTask(columnId: string): void {
    const title = this.newTaskTitle().trim();
    if (!title) {
      return;
    }

    this.creatingTask.set(true);

    const createDto: CreateTaskDto = {
      columnId: columnId,
      title: title,
      priority: 1 // Default to Medium
    };

    this.http.post<TaskDto>(`${this.apiUrl}/api/app/task`, createDto).subscribe({
      next: (newTask) => {
        // Add the new task to the appropriate column
        const columns = this.columnsWithTasks();
        const updatedColumns = columns.map(col => {
          if (col.id === columnId) {
            return {
              ...col,
              tasks: [...col.tasks, newTask]
            };
          }
          return col;
        });
        this.columnsWithTasks.set(updatedColumns);

        // Reset form
        this.showAddTaskForm.set(null);
        this.newTaskTitle.set('');
        this.creatingTask.set(false);
      },
      error: (err) => {
        console.error('Failed to create task:', err);
        this.creatingTask.set(false);
        alert('Failed to create task. Please try again.');
      }
    });
  }

  getTaskCount(column: ColumnWithTasks): number {
    return column.tasks?.length || 0;
  }

  getPriorityLabel(priority: number): string {
    switch (priority) {
      case 0: return 'Low';
      case 1: return 'Medium';
      case 2: return 'High';
      default: return 'Medium';
    }
  }

  getPriorityClass(priority: number): string {
    switch (priority) {
      case 0: return 'priority-low';
      case 1: return 'priority-medium';
      case 2: return 'priority-high';
      default: return 'priority-medium';
    }
  }

  // Task detail modal methods
  openTaskModal(task: TaskDto): void {
    this.selectedTask.set(task);
    this.showTaskModal.set(true);
  }

  closeTaskModal(): void {
    this.showTaskModal.set(false);
    this.selectedTask.set(null);
  }

  onModalBackdropClick(event: MouseEvent): void {
    // Close modal when clicking on the backdrop (not the modal content)
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.closeTaskModal();
    }
  }

  getColumnNameForTask(task: TaskDto): string {
    const columns = this.columnsWithTasks();
    const column = columns.find(c => c.id === task.columnId);
    return column?.name || 'Unknown';
  }

  // Settings modal methods
  openSettingsModal(): void {
    this.showSettingsModal.set(true);
    this.inviteEmail.set('');
    this.inviteError.set(null);
    this.inviteSuccess.set(null);
    this.loadInvites();
    this.loadMembers();
  }

  closeSettingsModal(): void {
    this.showSettingsModal.set(false);
    this.inviteEmail.set('');
    this.inviteError.set(null);
    this.inviteSuccess.set(null);
  }

  onSettingsBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.closeSettingsModal();
    }
  }

  // Invite methods
  private loadInvites(): void {
    this.loadingInvites.set(true);
    this.http.get<InviteDto[]>(`${this.apiUrl}/api/app/board/invites`).subscribe({
      next: (invites) => {
        this.pendingInvites.set(invites);
        this.loadingInvites.set(false);
      },
      error: (err) => {
        console.error('Failed to load invites:', err);
        this.loadingInvites.set(false);
      }
    });
  }

  sendInvite(): void {
    const email = this.inviteEmail().trim();
    if (!email) {
      return;
    }

    // Basic email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      this.inviteError.set('Please enter a valid email address.');
      return;
    }

    this.sendingInvite.set(true);
    this.inviteError.set(null);
    this.inviteSuccess.set(null);

    this.http.post<InviteDto>(`${this.apiUrl}/api/app/board/invite`, { email }).subscribe({
      next: (invite) => {
        this.inviteSuccess.set(`Invitation sent to ${email}. Check the server console for the invite link.`);
        this.inviteEmail.set('');
        this.sendingInvite.set(false);
        // Reload invites
        this.loadInvites();
      },
      error: (err) => {
        console.error('Failed to send invite:', err);
        const errorMessage = err.error?.error?.message || err.error?.message || 'Failed to send invitation. Please try again.';
        this.inviteError.set(errorMessage);
        this.sendingInvite.set(false);
      }
    });
  }

  cancelInvite(inviteId: string): void {
    this.cancellingInvite.set(inviteId);
    this.http.delete(`${this.apiUrl}/api/app/board/${inviteId}/invite`).subscribe({
      next: () => {
        this.cancellingInvite.set(null);
        this.loadInvites();
      },
      error: (err) => {
        console.error('Failed to cancel invite:', err);
        this.cancellingInvite.set(null);
        alert('Failed to cancel invitation. Please try again.');
      }
    });
  }

  // Member methods
  private loadMembers(): void {
    this.loadingMembers.set(true);
    this.http.get<MemberDto[]>(`${this.apiUrl}/api/app/board/members`).subscribe({
      next: (members) => {
        this.members.set(members);
        this.loadingMembers.set(false);
      },
      error: (err) => {
        console.error('Failed to load members:', err);
        this.loadingMembers.set(false);
      }
    });
  }

  removeMember(userId: string): void {
    if (!confirm('Are you sure you want to remove this member from the board?')) {
      return;
    }

    this.removingMember.set(userId);
    this.http.delete(`${this.apiUrl}/api/app/board/member/${userId}`).subscribe({
      next: () => {
        this.removingMember.set(null);
        this.loadMembers();
      },
      error: (err) => {
        console.error('Failed to remove member:', err);
        this.removingMember.set(null);
        alert('Failed to remove member. Please try again.');
      }
    });
  }

  getInitials(name: string): string {
    if (!name) return '?';
    const parts = name.trim().split(/\s+/);
    if (parts.length === 1) {
      return parts[0].charAt(0).toUpperCase();
    }
    return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
  }
}
