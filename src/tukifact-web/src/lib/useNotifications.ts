'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import { api } from './api';

export interface Notification {
  id: string;
  type: string;
  title: string;
  body: string | null;
  entityType: string | null;
  entityId: string | null;
  isRead: boolean;
  createdAt: string;
}

interface NotificationsState {
  notifications: Notification[];
  unreadCount: number;
  isConnected: boolean;
}

export function useNotifications() {
  const [state, setState] = useState<NotificationsState>({
    notifications: [],
    unreadCount: 0,
    isConnected: false,
  });
  const eventSourceRef = useRef<EventSource | null>(null);

  // Fetch initial notifications
  const fetchNotifications = useCallback(async () => {
    try {
      const data = await api.get<{
        data: Notification[];
        unreadCount: number;
      }>('/v1/notifications?pageSize=10');
      setState((prev) => ({
        ...prev,
        notifications: data.data,
        unreadCount: data.unreadCount,
      }));
    } catch (err) {
      console.error('Failed to fetch notifications:', err);
    }
  }, []);

  // Fetch unread count only
  const fetchUnreadCount = useCallback(async () => {
    try {
      const data = await api.get<{ count: number }>('/v1/notifications/unread-count');
      setState((prev) => ({ ...prev, unreadCount: data.count }));
    } catch (err) {
      console.error('Failed to fetch unread count:', err);
    }
  }, []);

  // Mark single notification as read
  const markAsRead = useCallback(async (id: string) => {
    try {
      await api.put(`/v1/notifications/${id}/read`, {});
      setState((prev) => ({
        ...prev,
        notifications: prev.notifications.map((n) =>
          n.id === id ? { ...n, isRead: true } : n
        ),
        unreadCount: Math.max(0, prev.unreadCount - 1),
      }));
    } catch (err) {
      console.error('Failed to mark notification as read:', err);
    }
  }, []);

  // Mark all as read
  const markAllAsRead = useCallback(async () => {
    try {
      await api.put('/v1/notifications/read-all', {});
      setState((prev) => ({
        ...prev,
        notifications: prev.notifications.map((n) => ({ ...n, isRead: true })),
        unreadCount: 0,
      }));
    } catch (err) {
      console.error('Failed to mark all as read:', err);
    }
  }, []);

  // Initial fetch on mount
  useEffect(() => {
    void fetchNotifications();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Initial fetch on mount
  useEffect(() => {
    void fetchNotifications();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // SSE connection for real-time updates
  useEffect(() => {
    const token = api.getToken();
    if (!token) return;

    const baseUrl = process.env.NEXT_PUBLIC_API_URL || '';
    const url = `${baseUrl}/v1/notifications/stream`;

    // EventSource doesn't support custom headers, so we pass token as query param
    // The backend should accept ?token= as fallback auth
    const es = new EventSource(`${url}?token=${encodeURIComponent(token)}`);
    eventSourceRef.current = es;

    es.addEventListener('notification', (event) => {
      try {
        const notification: Notification = JSON.parse(event.data);
        setState((prev) => ({
          ...prev,
          notifications: [notification, ...prev.notifications].slice(0, 50),
          unreadCount: prev.unreadCount + 1,
        }));

        // Optional: play notification sound
        if (typeof window !== 'undefined' && 'Notification' in window) {
          if (window.Notification.permission === 'granted') {
            new window.Notification(notification.title, {
              body: notification.body ?? undefined,
              icon: '/logo.png',
            });
          }
        }
      } catch (err) {
        console.error('Failed to parse SSE notification:', err);
      }
    });

    es.addEventListener('ping', () => {
      setState((prev) => ({ ...prev, isConnected: true }));
    });

    es.onerror = () => {
      setState((prev) => ({ ...prev, isConnected: false }));
      // EventSource auto-reconnects
    };

    es.onopen = () => {
      setState((prev) => ({ ...prev, isConnected: true }));
    };

    return () => {
      es.close();
      eventSourceRef.current = null;
    };
  }, []);

  return {
    ...state,
    fetchNotifications,
    fetchUnreadCount,
    markAsRead,
    markAllAsRead,
  };
}
