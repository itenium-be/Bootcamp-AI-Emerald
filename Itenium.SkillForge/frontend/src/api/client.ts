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

export interface SkillPrerequisiteWarning {
  requiredSkillId: number;
  requiredSkillName: string;
  requiredMinNiveau: number;
  currentNiveau: number;
}

export interface RoadmapSkillNode {
  skillId: number;
  skillName: string;
  categoryName: string;
  levelCount: number;
  currentNiveau: number;
  targetNiveau: number | null;
  prerequisitesMet: boolean;
  unmetPrerequisites: SkillPrerequisiteWarning[];
}

export interface SeniorityProgressCriterion {
  skillId: number;
  skillName: string;
  minNiveau: number;
  currentNiveau: number;
}

export type SeniorityLevel = 'Junior' | 'Medior' | 'Senior';

export interface SeniorityProgressResult {
  currentLevel: SeniorityLevel | null;
  nextLevel: SeniorityLevel | null;
  met: number;
  required: number;
  unmetCriteria: SeniorityProgressCriterion[];
}

export async function fetchRoadmap(full?: boolean): Promise<RoadmapSkillNode[] | null> {
  try {
    const response = await api.get<RoadmapSkillNode[]>('/api/consultants/me/roadmap', {
      params: full ? { full: true } : undefined,
    });
    return response.data;
  } catch (err: unknown) {
    if ((err as { response?: { status?: number } })?.response?.status === 404) return null;
    throw err;
  }
}

export async function fetchSeniorityProgress(): Promise<SeniorityProgressResult | null> {
  try {
    const response = await api.get<SeniorityProgressResult>('/api/consultants/me/seniority-progress');
    return response.data;
  } catch (err: unknown) {
    if ((err as { response?: { status?: number } })?.response?.status === 404) return null;
    throw err;
  }
}

// ── Goals (#18, #30) ──────────────────────────────────────────────────────────

export type ResourceType = 'Article' | 'Video' | 'Book' | 'Course' | 'Documentation' | 'Other';

export interface ReadinessFlagDto {
  id: number;
  raisedAt: string;
  ageDays: number;
}

export interface LinkedResourceDto {
  resourceId: number;
  title: string;
  url: string;
  type: ResourceType;
  isCompleted: boolean;
}

export type GoalStatus = 'Active' | 'Achieved' | 'Cancelled';

export interface GoalDto {
  id: number;
  consultantUserId: string;
  coachUserId: string;
  skillId: number;
  skillName: string;
  currentNiveau: number;
  targetNiveau: number;
  deadline: string | null;
  status: GoalStatus;
  createdAt: string;
  resources: LinkedResourceDto[];
  activeReadinessFlag: ReadinessFlagDto | null;
}

export interface CreateGoalRequest {
  skillId: number;
  currentNiveau: number;
  targetNiveau: number;
  deadline?: string | null;
  resourceIds?: number[];
}

export interface UpdateGoalRequest {
  currentNiveau: number;
  targetNiveau: number;
  deadline?: string | null;
  status: GoalStatus;
}

export async function fetchMyGoals(): Promise<GoalDto[]> {
  const response = await api.get<GoalDto[]>('/api/consultants/me/goals');
  return response.data;
}

export async function fetchGoalsForConsultant(consultantId: number): Promise<GoalDto[]> {
  const response = await api.get<GoalDto[]>(`/api/consultants/${consultantId}/goals`);
  return response.data;
}

export async function createGoal(consultantId: number, request: CreateGoalRequest): Promise<GoalDto> {
  const response = await api.post<GoalDto>(`/api/consultants/${consultantId}/goals`, request);
  return response.data;
}

export async function updateGoal(goalId: number, request: UpdateGoalRequest): Promise<GoalDto> {
  const response = await api.put<GoalDto>(`/api/goals/${goalId}`, request);
  return response.data;
}

export async function addResourceToGoal(goalId: number, resourceId: number): Promise<void> {
  await api.post(`/api/goals/${goalId}/resources`, { resourceId });
}

export async function removeResourceFromGoal(goalId: number, resourceId: number): Promise<void> {
  await api.delete(`/api/goals/${goalId}/resources/${resourceId}`);
}

export async function raiseReadinessFlag(goalId: number): Promise<void> {
  await api.post(`/api/goals/${goalId}/readiness-flag`);
}

export async function dismissReadinessFlag(goalId: number): Promise<void> {
  await api.delete(`/api/goals/${goalId}/readiness-flag`);
}

// ── Resources (#19) ───────────────────────────────────────────────────────────

export interface ResourceDto {
  id: number;
  title: string;
  url: string;
  type: ResourceType;
  skillId: number;
  skillName: string;
  fromNiveau: number;
  toNiveau: number;
  addedByUserId: string;
  addedAt: string;
  completionCount: number;
  positiveRatings: number;
  negativeRatings: number;
}

export interface CreateResourceRequest {
  title: string;
  url: string;
  type: ResourceType;
  skillId: number;
  fromNiveau: number;
  toNiveau: number;
}

export async function fetchResources(params?: {
  skillId?: number;
  type?: ResourceType;
  fromNiveau?: number;
  toNiveau?: number;
}): Promise<ResourceDto[]> {
  const response = await api.get<ResourceDto[]>('/api/resources', { params });
  return response.data;
}

export async function createResource(request: CreateResourceRequest): Promise<ResourceDto> {
  const response = await api.post<ResourceDto>('/api/resources', request);
  return response.data;
}

export async function completeResource(resourceId: number): Promise<void> {
  await api.post(`/api/resources/${resourceId}/complete`);
}

export async function rateResource(resourceId: number, isPositive: boolean): Promise<void> {
  await api.post(`/api/resources/${resourceId}/rate`, { isPositive });
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

export interface TeamMemberResponse {
  id: number;
  firstName: string | null;
  lastName: string | null;
  email: string;
  teamId: number;
  profileName: string | null;
}

export async function fetchTeamMembers(): Promise<TeamMemberResponse[]> {
  const response = await api.get<TeamMemberResponse[]>('/api/consultants');
  return response.data;
}
