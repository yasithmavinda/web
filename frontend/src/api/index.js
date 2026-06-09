import api from './axios';

/**
 * AUTH API — All authentication-related API calls.
 *
 * Beginner: Each function here is a "wrapper" around an API endpoint.
 * The component calls authApi.login(data) and gets back clean data,
 * not a raw Axios response.
 */
export const authApi = {
  register:       (data)        => api.post('/auth/register', data).then(r => r.data),
  login:          (data)        => api.post('/auth/login', data).then(r => r.data),
  logout:         (refreshToken)=> api.post('/auth/logout', { refreshToken }).then(r => r.data),
  logoutAll:      ()            => api.post('/auth/logout-all').then(r => r.data),
  refreshToken:   (token)       => api.post('/auth/refresh-token', { refreshToken: token }).then(r => r.data),
  forgotPassword: (email)       => api.post('/auth/forgot-password', { email }).then(r => r.data),
  resetPassword:  (data)        => api.post('/auth/reset-password', data).then(r => r.data),
  changePassword: (data)        => api.put('/auth/change-password', data).then(r => r.data),
  verifyEmail:    (token)       => api.get(`/auth/verify-email/${token}`).then(r => r.data),
  getMe:          ()            => api.get('/auth/me').then(r => r.data),
  getSessions:    ()            => api.get('/auth/sessions').then(r => r.data),
  revokeSession:  (id)          => api.delete(`/auth/sessions/${id}`).then(r => r.data),
};

export const usersApi = {
  getAll:        (params)       => api.get('/users', { params }).then(r => r.data),
  getById:       (id)           => api.get(`/users/${id}`).then(r => r.data),
  getMyProfile:  ()             => api.get('/users/profile').then(r => r.data),
  updateProfile: (data)         => api.put('/users/profile', data).then(r => r.data),
  updateAvatar:  (avatarUrl)    => api.patch('/users/profile/avatar', { avatarUrl }).then(r => r.data),
  toggleStatus:  (id, data)     => api.patch(`/users/${id}/status`, data).then(r => r.data),
  assignRole:    (id, roleId)   => api.patch(`/users/${id}/role`, { userId: id, roleId }).then(r => r.data),
  getStats:      (id)           => api.get(`/users/${id}/stats`).then(r => r.data),
  getWorkload:   (projectId)    => api.get('/users/workload', { params: { projectId } }).then(r => r.data),
};

export const projectsApi = {
  getAll:       (params)        => api.get('/projects', { params }).then(r => r.data),
  getById:      (id)            => api.get(`/projects/${id}`).then(r => r.data),
  create:       (data)          => api.post('/projects', data).then(r => r.data),
  update:       (id, data)      => api.put(`/projects/${id}`, data).then(r => r.data),
  archive:      (id)            => api.delete(`/projects/${id}`).then(r => r.data),
  getMembers:   (id)            => api.get(`/projects/${id}/members`).then(r => r.data),
  addMember:    (id, data)      => api.post(`/projects/${id}/members`, data).then(r => r.data),
  removeMember: (id, userId)    => api.delete(`/projects/${id}/members/${userId}`).then(r => r.data),
};

export const tasksApi = {
  getAll:        (params)       => api.get('/tasks', { params }).then(r => r.data),
  getById:       (id)           => api.get(`/tasks/${id}`).then(r => r.data),
  create:        (data)         => api.post('/tasks', data).then(r => r.data),
  update:        (id, data)     => api.put(`/tasks/${id}`, data).then(r => r.data),
  updateStatus:  (id, data)     => api.patch(`/tasks/${id}/status`, data).then(r => r.data),
  updatePosition:(id, data)     => api.patch(`/tasks/${id}/position`, data).then(r => r.data),
  assignUsers:   (id, userIds)  => api.put(`/tasks/${id}/assignees`, { userIds }).then(r => r.data),
  archive:       (id)           => api.delete(`/tasks/${id}`).then(r => r.data),
  getHistory:    (id)           => api.get(`/tasks/${id}/history`).then(r => r.data),
};

export const commentsApi = {
  getByTask: (taskId, params)   => api.get(`/comments/task/${taskId}`, { params }).then(r => r.data),
  create:    (data)             => api.post('/comments', data).then(r => r.data),
  update:    (id, data)         => api.put(`/comments/${id}`, data).then(r => r.data),
  delete:    (id)               => api.delete(`/comments/${id}`).then(r => r.data),
};

export const notificationsApi = {
  getAll:        (params)       => api.get('/notifications', { params }).then(r => r.data),
  getUnreadCount:()             => api.get('/notifications/unread-count').then(r => r.data),
  markRead:      (id)           => api.patch(`/notifications/${id}/read`).then(r => r.data),
  markAllRead:   ()             => api.post('/notifications/mark-all-read').then(r => r.data),
  getSettings:   ()             => api.get('/notifications/settings').then(r => r.data),
  updateSettings:(data)         => api.put('/notifications/settings', data).then(r => r.data),
};
