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
}

interface CreateTaskDto {
  columnId: string;
  title: string;
  description?: string;
  dueDate?: string;
  priority: number;
  assigneeId?: string;
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
}
