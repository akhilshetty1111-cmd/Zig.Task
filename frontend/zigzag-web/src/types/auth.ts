export interface User {
  id: string;
  name: string;
  email: string;
  role: 'Admin' | 'ProjectManager' | 'Member';
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: User;
}
