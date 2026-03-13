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

// Goals
export interface GoalResource {
  goalId: number;
  resourceId: number;
  resource: { id: number; title: string; url: string; type: string; fromNiveau: number; toNiveau: number };
}

export interface ReadinessFlag {
  id: number;
  goalId: number;
  raisedAt: string;
  dismissedAt: string | null;
}

export interface GoalResponse {
  id: number;
  consultantUserId: string;
  coachUserId: string;
  skillId: number;
  skill: { id: number; name: string; levelCount: number };
  currentNiveau: number;
  targetNiveau: number;
  deadline: string | null;
  status: 'Active' | 'Achieved' | 'Cancelled';
  createdAt: string;
  goalResources: GoalResource[];
  readinessFlag: ReadinessFlag | null;
}

export async function fetchMyGoals(): Promise<GoalResponse[]> {
  const response = await api.get<GoalResponse[]>('/api/goal/mine');
  return response.data;
}

export async function fetchConsultantGoals(consultantUserId: string): Promise<GoalResponse[]> {
  const response = await api.get<GoalResponse[]>(`/api/goal/consultant/${consultantUserId}`);
  return response.data;
}

export interface CreateGoalRequest {
  consultantUserId: string;
  skillId: number;
  currentNiveau: number;
  targetNiveau: number;
  deadline: string | null;
}

export async function createGoal(request: CreateGoalRequest): Promise<GoalResponse> {
  const response = await api.post<GoalResponse>('/api/goal', request);
  return response.data;
}

export async function signalReadiness(goalId: number): Promise<void> {
  await api.post(`/api/goal/${goalId}/readiness`);
}

export async function dismissReadiness(goalId: number): Promise<void> {
  await api.delete(`/api/goal/${goalId}/readiness`);
}

// Resources
export interface ResourceResponse {
  id: number;
  title: string;
  url: string;
  type: string;
  skillId: number;
  fromNiveau: number;
  toNiveau: number;
  addedByUserId: string;
  addedAt: string;
  completions: { userId: string; completedAt: string }[];
  ratings: { userId: string; isPositive: boolean }[];
}

export async function fetchResources(skillId?: number): Promise<ResourceResponse[]> {
  const response = await api.get<ResourceResponse[]>('/api/resource', { params: skillId ? { skillId } : undefined });
  return response.data;
}

export interface CreateResourceRequest {
  title: string;
  url: string;
  type: string;
  skillId: number;
  fromNiveau: number;
  toNiveau: number;
}

export async function createResource(request: CreateResourceRequest): Promise<ResourceResponse> {
  const response = await api.post<ResourceResponse>('/api/resource', request);
  return response.data;
}

export async function markResourceComplete(resourceId: number): Promise<void> {
  await api.post(`/api/resource/${resourceId}/complete`);
}

export async function unmarkResourceComplete(resourceId: number): Promise<void> {
  await api.delete(`/api/resource/${resourceId}/complete`);
}

export async function rateResource(resourceId: number, isPositive: boolean): Promise<void> {
  await api.post(`/api/resource/${resourceId}/rate`, { isPositive });
}

// Coach Dashboard
export interface ConsultantDashboardRow {
  userId: string;
  fullName: string;
  activeGoalCount: number;
  readinessFlagCount: number;
  maxFlagAgeInDays: number | null;
  overdueGoalCount: number;
  lastActivityAt: string | null;
  isInactive: boolean;
}

export async function fetchCoachDashboard(): Promise<ConsultantDashboardRow[]> {
  const response = await api.get<ConsultantDashboardRow[]>('/api/coach/dashboard');
  return response.data;
}

// Coaching Sessions
export interface CoachingSession {
  id: number;
  consultantUserId: string;
  coachUserId: string;
  startedAt: string;
  closedAt: string | null;
  notes: string | null;
}

export async function startSession(consultantUserId: string): Promise<CoachingSession> {
  const response = await api.post<CoachingSession>('/api/coaching-sessions', { consultantUserId });
  return response.data;
}

export async function closeSession(id: number, notes: string | null): Promise<void> {
  await api.post(`/api/coaching-sessions/${id}/close`, { notes });
}

export async function fetchSessions(consultantUserId?: string): Promise<CoachingSession[]> {
  const response = await api.get<CoachingSession[]>('/api/coaching-sessions', {
    params: consultantUserId ? { consultantUserId } : undefined,
  });
  return response.data;
}

// Skill Validations
export interface SkillValidation {
  id: number;
  consultantUserId: string;
  coachUserId: string;
  skillId: number;
  skill: { id: number; name: string };
  niveau: number;
  validatedAt: string;
  sessionId: number | null;
}

export async function validateSkill(request: { consultantUserId: string; skillId: number; niveau: number; sessionId?: number }): Promise<SkillValidation> {
  const response = await api.post<SkillValidation>('/api/skill-validations', request);
  return response.data;
}

export async function fetchValidations(consultantUserId?: string): Promise<SkillValidation[]> {
  const response = await api.get<SkillValidation[]>('/api/skill-validations', {
    params: consultantUserId ? { consultantUserId } : undefined,
  });
  return response.data;
}
