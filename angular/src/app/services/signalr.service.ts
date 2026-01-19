import { Injectable, inject, signal, OnDestroy } from '@angular/core';
import { EnvironmentService, AuthService } from '@abp/ng.core';
import * as signalR from '@microsoft/signalr';

export interface UserPresence {
  userId: string;
  userName: string;
}

export interface TypingInfo {
  userId: string;
  userName: string;
  context: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService implements OnDestroy {
  private environment = inject(EnvironmentService);
  private authService = inject(AuthService);

  private hubConnection: signalR.HubConnection | null = null;
  private currentBoardId: string | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;

  // Observable state
  readonly isConnected = signal(false);
  readonly isConnecting = signal(false);
  readonly connectionError = signal<string | null>(null);
  readonly onlineUsers = signal<UserPresence[]>([]);
  readonly typingUsers = signal<TypingInfo[]>([]);

  // Event callbacks (to be set by components)
  onTaskCreated?: (task: any) => void;
  onTaskUpdated?: (task: any) => void;
  onTaskDeleted?: (taskId: string) => void;
  onTaskMoved?: (taskId: string, newColumnId: string, newOrder: number) => void;
  onColumnReordered?: (columns: any[]) => void;
  onUserJoined?: (userId: string, userName: string) => void;
  onUserLeft?: (userId: string, userName: string) => void;

  private get hubUrl(): string {
    const apiUrl = this.environment.getEnvironment().apis.default.url;
    return `${apiUrl}/hubs/board`;
  }

  /**
   * Connect to the SignalR hub and join a specific board.
   */
  async connect(boardId: string): Promise<boolean> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      if (this.currentBoardId === boardId) {
        console.log('[SignalR] Already connected to board', boardId);
        return true;
      }
      // Leave current board and join new one
      if (this.currentBoardId) {
        await this.leaveBoard(this.currentBoardId);
      }
    }

    this.isConnecting.set(true);
    this.connectionError.set(null);

    try {
      // Create hub connection if not exists
      if (!this.hubConnection) {
        this.hubConnection = new signalR.HubConnectionBuilder()
          .withUrl(this.hubUrl, {
            accessTokenFactory: () => this.getAccessToken(),
            withCredentials: true
          })
          .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: (retryContext) => {
              // Exponential backoff: 1s, 2s, 4s, 8s, 16s
              return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 16000);
            }
          })
          .configureLogging(signalR.LogLevel.Information)
          .build();

        this.setupEventHandlers();
      }

      // Start connection if not started
      if (this.hubConnection.state === signalR.HubConnectionState.Disconnected) {
        await this.hubConnection.start();
        console.log('[SignalR] Connected to hub');
      }

      // Join the board
      await this.hubConnection.invoke('JoinBoard', boardId);
      this.currentBoardId = boardId;
      this.isConnected.set(true);
      this.reconnectAttempts = 0;

      console.log('[SignalR] Joined board', boardId);
      return true;
    } catch (error: any) {
      console.error('[SignalR] Connection failed:', error);
      this.connectionError.set(error.message || 'Failed to connect to real-time service');
      this.isConnected.set(false);
      return false;
    } finally {
      this.isConnecting.set(false);
    }
  }

  /**
   * Disconnect from the SignalR hub.
   */
  async disconnect(): Promise<void> {
    if (this.hubConnection) {
      try {
        if (this.currentBoardId) {
          await this.leaveBoard(this.currentBoardId);
        }
        await this.hubConnection.stop();
        console.log('[SignalR] Disconnected from hub');
      } catch (error) {
        console.error('[SignalR] Error during disconnect:', error);
      }
    }

    this.isConnected.set(false);
    this.currentBoardId = null;
    this.onlineUsers.set([]);
    this.typingUsers.set([]);
  }

  /**
   * Leave a specific board's real-time group.
   */
  private async leaveBoard(boardId: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('LeaveBoard', boardId);
        console.log('[SignalR] Left board', boardId);
      } catch (error) {
        console.error('[SignalR] Error leaving board:', error);
      }
    }
  }

  /**
   * Send typing indicator.
   */
  async sendTyping(context: string = 'general'): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected && this.currentBoardId) {
      try {
        await this.hubConnection.invoke('SendTyping', this.currentBoardId, context);
      } catch (error) {
        console.error('[SignalR] Error sending typing indicator:', error);
      }
    }
  }

  /**
   * Stop typing indicator.
   */
  async stopTyping(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected && this.currentBoardId) {
      try {
        await this.hubConnection.invoke('StopTyping', this.currentBoardId);
      } catch (error) {
        console.error('[SignalR] Error stopping typing indicator:', error);
      }
    }
  }

  /**
   * Ping the server to test connection.
   */
  async ping(): Promise<boolean> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('Ping');
        return true;
      } catch (error) {
        console.error('[SignalR] Ping failed:', error);
        return false;
      }
    }
    return false;
  }

  /**
   * Get the access token for SignalR authentication.
   */
  private getAccessToken(): string {
    // ABP stores the token - we need to retrieve it
    // The token is typically stored by the OAuth module
    const token = localStorage.getItem('access_token') || sessionStorage.getItem('access_token');
    return token || '';
  }

  /**
   * Setup event handlers for SignalR events.
   */
  private setupEventHandlers(): void {
    if (!this.hubConnection) return;

    // Connection state events
    this.hubConnection.onclose((error) => {
      console.log('[SignalR] Connection closed', error);
      this.isConnected.set(false);
      this.onlineUsers.set([]);
      this.typingUsers.set([]);
    });

    this.hubConnection.onreconnecting((error) => {
      console.log('[SignalR] Reconnecting...', error);
      this.isConnected.set(false);
      this.isConnecting.set(true);
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.log('[SignalR] Reconnected with ID:', connectionId);
      this.isConnected.set(true);
      this.isConnecting.set(false);
      this.connectionError.set(null);

      // Rejoin the board after reconnection
      if (this.currentBoardId) {
        this.hubConnection?.invoke('JoinBoard', this.currentBoardId).catch(console.error);
      }
    });

    // Presence events
    this.hubConnection.on('PresenceUpdated', (users: UserPresence[]) => {
      console.log('[SignalR] Presence updated:', users);
      this.onlineUsers.set(users);
    });

    this.hubConnection.on('UserJoined', (userId: string, userName: string) => {
      console.log('[SignalR] User joined:', userName);
      this.onUserJoined?.(userId, userName);
    });

    this.hubConnection.on('UserLeft', (userId: string, userName: string) => {
      console.log('[SignalR] User left:', userName);
      this.onUserLeft?.(userId, userName);
    });

    // Typing events
    this.hubConnection.on('UserTyping', (userId: string, userName: string, context: string) => {
      const current = this.typingUsers();
      const exists = current.find(u => u.userId === userId);
      if (!exists) {
        this.typingUsers.set([...current, { userId, userName, context }]);
      }

      // Auto-remove after 3 seconds
      setTimeout(() => {
        const updated = this.typingUsers().filter(u => u.userId !== userId);
        this.typingUsers.set(updated);
      }, 3000);
    });

    this.hubConnection.on('UserStoppedTyping', (userId: string) => {
      const updated = this.typingUsers().filter(u => u.userId !== userId);
      this.typingUsers.set(updated);
    });

    // Task events (for future use - real-time sync)
    this.hubConnection.on('TaskCreated', (task: any) => {
      console.log('[SignalR] Task created:', task);
      this.onTaskCreated?.(task);
    });

    this.hubConnection.on('TaskUpdated', (task: any) => {
      console.log('[SignalR] Task updated:', task);
      this.onTaskUpdated?.(task);
    });

    this.hubConnection.on('TaskDeleted', (taskId: string) => {
      console.log('[SignalR] Task deleted:', taskId);
      this.onTaskDeleted?.(taskId);
    });

    this.hubConnection.on('TaskMoved', (taskId: string, newColumnId: string, newOrder: number) => {
      console.log('[SignalR] Task moved:', taskId, 'to column', newColumnId);
      this.onTaskMoved?.(taskId, newColumnId, newOrder);
    });

    this.hubConnection.on('ColumnReordered', (columns: any[]) => {
      console.log('[SignalR] Columns reordered:', columns);
      this.onColumnReordered?.(columns);
    });

    // Pong response
    this.hubConnection.on('Pong', (timestamp: Date) => {
      console.log('[SignalR] Pong received at:', timestamp);
    });
  }

  ngOnDestroy(): void {
    this.disconnect();
  }
}
