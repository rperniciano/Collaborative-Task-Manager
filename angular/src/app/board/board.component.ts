import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService, ConfigStateService, EnvironmentService } from '@abp/ng.core';
import { Router } from '@angular/router';
import { SignalRService, UserPresence } from '../services/signalr.service';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';

interface ChecklistItemDto {
  id: string;
  taskId: string;
  text: string;
  isCompleted: boolean;
  order: number;
}

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
  checklistItems: ChecklistItemDto[];
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
  imports: [CommonModule, FormsModule, DragDropModule],
  templateUrl: './board.component.html',
  styleUrls: ['./board.component.scss']
})
export class BoardComponent implements OnInit, OnDestroy {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private configState = inject(ConfigStateService);
  private router = inject(Router);
  private environment = inject(EnvironmentService);
  signalR = inject(SignalRService);

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
  deletingTask = signal(false);

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

  // Board rename state
  editBoardName = signal('');
  renamingBoard = signal(false);
  renameError = signal<string | null>(null);
  renameSuccess = signal<string | null>(null);

  // Checklist state
  newChecklistItemText = signal('');
  addingChecklistItem = signal(false);

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

  ngOnDestroy(): void {
    // Disconnect from SignalR when leaving the board
    this.signalR.disconnect();
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
        // Connect to SignalR for real-time updates
        this.connectToSignalR(data.id);
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
    this.deletingTask.set(false);
  }

  deleteTask(): void {
    const task = this.selectedTask();
    if (!task) {
      return;
    }

    if (!confirm('Are you sure you want to delete this task? This action cannot be undone.')) {
      return;
    }

    this.deletingTask.set(true);

    this.http.delete(`${this.apiUrl}/api/app/task/${task.id}`).subscribe({
      next: () => {
        // Remove the task from the local state
        const columns = this.columnsWithTasks();
        const updatedColumns = columns.map(col => {
          if (col.id === task.columnId) {
            return {
              ...col,
              tasks: col.tasks.filter(t => t.id !== task.id)
            };
          }
          return col;
        });
        this.columnsWithTasks.set(updatedColumns);

        // Close the modal
        this.closeTaskModal();
      },
      error: (err) => {
        console.error('Failed to delete task:', err);
        this.deletingTask.set(false);
        alert('Failed to delete task. Please try again.');
      }
    });
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
    this.editBoardName.set(this.board()?.name || '');
    this.renameError.set(null);
    this.renameSuccess.set(null);
    this.loadInvites();
    this.loadMembers();
  }

  closeSettingsModal(): void {
    this.showSettingsModal.set(false);
    this.inviteEmail.set('');
    this.inviteError.set(null);
    this.inviteSuccess.set(null);
    this.renameError.set(null);
    this.renameSuccess.set(null);
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

  // Board rename method
  renameBoard(): void {
    const newName = this.editBoardName().trim();
    const currentName = this.board()?.name;

    if (!newName) {
      this.renameError.set('Board name cannot be empty.');
      return;
    }

    if (newName === currentName) {
      this.renameSuccess.set('Board name is already set to this value.');
      return;
    }

    this.renamingBoard.set(true);
    this.renameError.set(null);
    this.renameSuccess.set(null);

    this.http.put<{ id: string; name: string; ownerId: string; creationTime: string }>(
      `${this.apiUrl}/api/app/board/board`,
      { name: newName }
    ).subscribe({
      next: (updatedBoard) => {
        // Update the board state with the new name
        const currentBoard = this.board();
        if (currentBoard) {
          this.board.set({
            ...currentBoard,
            name: updatedBoard.name
          });
        }
        this.renameSuccess.set('Board name updated successfully!');
        this.renamingBoard.set(false);
      },
      error: (err) => {
        console.error('Failed to rename board:', err);
        const errorMessage = err.error?.error?.message || err.error?.message || 'Failed to rename board. Please try again.';
        this.renameError.set(errorMessage);
        this.renamingBoard.set(false);
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

  // Column drag and drop methods
  onColumnDrop(event: CdkDragDrop<ColumnWithTasks[]>): void {
    if (event.previousIndex === event.currentIndex) {
      return; // No change
    }

    const columns = [...this.columnsWithTasks()];
    moveItemInArray(columns, event.previousIndex, event.currentIndex);

    // Update local state immediately for responsive UI
    this.columnsWithTasks.set(columns);

    // Persist to backend
    const columnIds = columns.map(col => col.id);
    this.http.post(`${this.apiUrl}/api/app/board/reorder-columns`, { columnIds }).subscribe({
      next: () => {
        console.log('Columns reordered successfully');
      },
      error: (err) => {
        console.error('Failed to reorder columns:', err);
        // Revert on error
        this.loadBoard();
        alert('Failed to reorder columns. Please try again.');
      }
    });
  }

  /**
   * Connect to SignalR hub for real-time updates.
   */
  private async connectToSignalR(boardId: string): Promise<void> {
    try {
      // Set up event handlers before connecting
      this.setupSignalRHandlers();

      const connected = await this.signalR.connect(boardId);
      if (connected) {
        console.log('[Board] Connected to SignalR for board:', boardId);
      } else {
        console.warn('[Board] Failed to connect to SignalR');
      }
    } catch (error) {
      console.error('[Board] SignalR connection error:', error);
    }
  }

  /**
   * Set up SignalR event handlers for real-time updates.
   */
  private setupSignalRHandlers(): void {
    // Handle task created by another user
    this.signalR.onTaskCreated = (task: TaskDto) => {
      console.log('[Board] Task created by another user:', task);
      this.addTaskToColumn(task);
    };

    // Handle task updated by another user
    this.signalR.onTaskUpdated = (task: TaskDto) => {
      console.log('[Board] Task updated by another user:', task);
      this.updateTaskInColumn(task);
    };

    // Handle task deleted by another user
    this.signalR.onTaskDeleted = (taskId: string) => {
      console.log('[Board] Task deleted by another user:', taskId);
      this.removeTaskFromColumn(taskId);
    };

    // Handle task moved by another user
    this.signalR.onTaskMoved = (taskId: string, newColumnId: string, newOrder: number) => {
      console.log('[Board] Task moved by another user:', taskId, 'to column', newColumnId);
      this.moveTaskToColumn(taskId, newColumnId, newOrder);
    };
  }

  /**
   * Add a task to the appropriate column (from SignalR event).
   */
  private addTaskToColumn(task: TaskDto): void {
    const columns = this.columnsWithTasks();
    const updatedColumns = columns.map(col => {
      if (col.id === task.columnId) {
        // Add task and sort by order
        const tasks = [...col.tasks, task].sort((a, b) => a.order - b.order);
        return { ...col, tasks };
      }
      return col;
    });
    this.columnsWithTasks.set(updatedColumns);
  }

  /**
   * Update a task in its column (from SignalR event).
   */
  private updateTaskInColumn(updatedTask: TaskDto): void {
    const columns = this.columnsWithTasks();
    const updatedColumns = columns.map(col => {
      const taskIndex = col.tasks.findIndex(t => t.id === updatedTask.id);
      if (taskIndex !== -1) {
        const tasks = [...col.tasks];
        tasks[taskIndex] = updatedTask;
        return { ...col, tasks };
      }
      return col;
    });
    this.columnsWithTasks.set(updatedColumns);
  }

  /**
   * Remove a task from its column (from SignalR event).
   */
  private removeTaskFromColumn(taskId: string): void {
    const columns = this.columnsWithTasks();
    const updatedColumns = columns.map(col => {
      const taskExists = col.tasks.some(t => t.id === taskId);
      if (taskExists) {
        return { ...col, tasks: col.tasks.filter(t => t.id !== taskId) };
      }
      return col;
    });
    this.columnsWithTasks.set(updatedColumns);
  }

  /**
   * Move a task to a different column (from SignalR event).
   */
  private moveTaskToColumn(taskId: string, newColumnId: string, newOrder: number): void {
    const columns = this.columnsWithTasks();

    // Find and remove task from its current column
    let movedTask: TaskDto | null = null;
    let updatedColumns = columns.map(col => {
      const taskIndex = col.tasks.findIndex(t => t.id === taskId);
      if (taskIndex !== -1) {
        movedTask = { ...col.tasks[taskIndex], columnId: newColumnId, order: newOrder };
        return { ...col, tasks: col.tasks.filter(t => t.id !== taskId) };
      }
      return col;
    });

    // Add task to new column
    if (movedTask) {
      updatedColumns = updatedColumns.map(col => {
        if (col.id === newColumnId) {
          const tasks = [...col.tasks, movedTask!].sort((a, b) => a.order - b.order);
          return { ...col, tasks };
        }
        return col;
      });
    }

    this.columnsWithTasks.set(updatedColumns);
  }

  // Checklist methods
  addChecklistItem(): void {
    const task = this.selectedTask();
    const text = this.newChecklistItemText().trim();

    if (!task || !text) {
      return;
    }

    this.addingChecklistItem.set(true);

    this.http.post<ChecklistItemDto>(
      `${this.apiUrl}/api/app/task/${task.id}/checklist-items`,
      { text }
    ).subscribe({
      next: (newItem) => {
        // Update the selected task with the new checklist item
        const updatedTask = {
          ...task,
          checklistItems: [...(task.checklistItems || []), newItem]
        };
        this.selectedTask.set(updatedTask);

        // Also update the task in the columns
        this.updateTaskInColumn(updatedTask);

        // Reset form
        this.newChecklistItemText.set('');
        this.addingChecklistItem.set(false);
      },
      error: (err) => {
        console.error('Failed to add checklist item:', err);
        this.addingChecklistItem.set(false);
        alert('Failed to add checklist item. Please try again.');
      }
    });
  }

  toggleChecklistItem(item: ChecklistItemDto): void {
    const task = this.selectedTask();
    if (!task) {
      return;
    }

    this.http.put<ChecklistItemDto>(
      `${this.apiUrl}/api/app/task/${task.id}/checklist-items/${item.id}`,
      { isCompleted: !item.isCompleted }
    ).subscribe({
      next: (updatedItem) => {
        // Update the selected task with the updated checklist item
        const updatedChecklistItems = task.checklistItems.map(ci =>
          ci.id === updatedItem.id ? updatedItem : ci
        );
        const updatedTask = { ...task, checklistItems: updatedChecklistItems };
        this.selectedTask.set(updatedTask);

        // Also update the task in the columns
        this.updateTaskInColumn(updatedTask);
      },
      error: (err) => {
        console.error('Failed to toggle checklist item:', err);
        alert('Failed to update checklist item. Please try again.');
      }
    });
  }

  deleteChecklistItem(item: ChecklistItemDto): void {
    const task = this.selectedTask();
    if (!task) {
      return;
    }

    this.http.delete(
      `${this.apiUrl}/api/app/task/${task.id}/checklist-items/${item.id}`
    ).subscribe({
      next: () => {
        // Update the selected task with the removed checklist item
        const updatedChecklistItems = task.checklistItems.filter(ci => ci.id !== item.id);
        const updatedTask = { ...task, checklistItems: updatedChecklistItems };
        this.selectedTask.set(updatedTask);

        // Also update the task in the columns
        this.updateTaskInColumn(updatedTask);
      },
      error: (err) => {
        console.error('Failed to delete checklist item:', err);
        alert('Failed to delete checklist item. Please try again.');
      }
    });
  }

  getCompletedChecklistCount(task: TaskDto): number {
    return (task.checklistItems || []).filter(item => item.isCompleted).length;
  }

  getTotalChecklistCount(task: TaskDto): number {
    return (task.checklistItems || []).length;
  }
}
