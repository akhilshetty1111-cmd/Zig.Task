export type ProjectRole = 'Viewer' | 'Member' | 'Manager' | 'Owner';

export interface Project {
  id: string;
  name: string;
  description: string | null;
  ownerId: string;
  ownerName: string;
  isArchived: boolean;
  memberCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ProjectMember {
  userId: string;
  name: string;
  email: string;
  role: ProjectRole;
  joinedAt: string;
}
