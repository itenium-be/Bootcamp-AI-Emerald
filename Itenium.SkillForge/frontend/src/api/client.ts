import axios from 'axios';
import { useAuthStore } from '../stores';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const api = axios.create({
  baseURL: API_BASE_URL,
});

// Add auth token to requests
api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Handle 401 responses
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout();
    }
    return Promise.reject(error);
  },
);

interface LoginResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
}

export async function loginApi(username: string, password: string): Promise<LoginResponse> {
  const params = new URLSearchParams();
  params.append('grant_type', 'password');
  params.append('username', username);
  params.append('password', password);
  params.append('client_id', 'skillforge-spa');
  params.append('scope', 'openid profile email');

  const response = await axios.post<LoginResponse>(`${API_BASE_URL}/connect/token`, params, {
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
    },
  });

  return response.data;
}

interface Team {
  id: number;
  name: string;
}

export async function fetchUserTeams(): Promise<Team[]> {
  const response = await api.get<Team[]>('/api/team');
  return response.data;
}

interface Course {
  id: number;
  name: string;
  description: string | null;
  category: string | null;
  level: string | null;
}

export async function fetchCourses(): Promise<Course[]> {
  const response = await api.get<Course[]>('/api/course');
  return response.data;
}

export interface SkillListItem {
  id: number;
  name: string;
  categoryName: string;
  levelCount: number;
  description: string | null;
}

export interface SkillLevel {
  niveau: number;
  descriptor: string;
}

export interface SkillPrerequisite {
  requiredSkillId: number;
  requiredSkillName: string;
  requiredMinNiveau: number;
}

export interface SkillDetail {
  id: number;
  name: string;
  categoryName: string;
  levelCount: number;
  description: string | null;
  levels: SkillLevel[];
  prerequisites: SkillPrerequisite[];
}

export async function fetchSkills(params?: { categoryId?: number; profileId?: number }): Promise<SkillListItem[]> {
  const response = await api.get<SkillListItem[]>('/api/skills', { params });
  return response.data;
}

export async function fetchSkillDetail(id: number): Promise<SkillDetail> {
  const response = await api.get<SkillDetail>(`/api/skills/${id}`);
  return response.data;
}

export interface UserResponse {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  role: string;
  teams: number[];
  isArchived: boolean;
}

export interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  password: string;
  teams: number[] | null;
}

export async function fetchUsers(includeArchived = false): Promise<UserResponse[]> {
  const response = await api.get<UserResponse[]>('/api/user', { params: { includeArchived } });
  return response.data;
}

export async function fetchUnassignedUsers(): Promise<UserResponse[]> {
  const response = await api.get<UserResponse[]>('/api/user/unassigned');
  return response.data;
}

export async function createUser(request: CreateUserRequest): Promise<UserResponse> {
  const response = await api.post<UserResponse>('/api/user', request);
  return response.data;
}

export async function archiveUser(id: string): Promise<void> {
  await api.post(`/api/user/${id}/archive`);
}

export async function restoreUser(id: string): Promise<void> {
  await api.post(`/api/user/${id}/restore`);
}
