import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Goals } from '../Goals';
import type { GoalResponse } from '@/api/client';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key}:${JSON.stringify(opts)}` : key),
  }),
}));

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
  useMutation: vi.fn(),
  useQueryClient: vi.fn(() => ({ invalidateQueries: vi.fn() })),
}));

vi.mock('@/api/client', () => ({
  fetchMyGoals: vi.fn(),
  signalReadiness: vi.fn(),
  dismissReadiness: vi.fn(),
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return {
    AlertCircle: I,
    BookOpen: I,
    Calendar: I,
    CheckCircle: I,
    Flag: I,
    Target: I,
  };
});

vi.mock('sonner', () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

const mockUseQuery = useQuery as Mock;
const mockUseMutation = useMutation as Mock;

const mockGoalActive: GoalResponse = {
  id: 1,
  consultantUserId: 'user1',
  coachUserId: 'coach1',
  skillId: 10,
  skill: { id: 10, name: 'TypeScript', levelCount: 5 },
  currentNiveau: 2,
  targetNiveau: 4,
  deadline: null,
  status: 'Active',
  createdAt: '2025-01-01T00:00:00Z',
  goalResources: [],
  readinessFlag: null,
};

const mockGoalOverdue: GoalResponse = {
  id: 2,
  consultantUserId: 'user1',
  coachUserId: 'coach1',
  skillId: 11,
  skill: { id: 11, name: 'React', levelCount: 4 },
  currentNiveau: 1,
  targetNiveau: 3,
  deadline: '2020-01-01T00:00:00Z',
  status: 'Active',
  createdAt: '2025-01-01T00:00:00Z',
  goalResources: [
    {
      goalId: 2,
      resourceId: 5,
      resource: { id: 5, title: 'React Docs', url: 'https://react.dev', type: 'Documentation', fromNiveau: 1, toNiveau: 3 },
    },
  ],
  readinessFlag: null,
};

const mockGoalFlagged: GoalResponse = {
  id: 3,
  consultantUserId: 'user1',
  coachUserId: 'coach1',
  skillId: 12,
  skill: { id: 12, name: 'Node.js', levelCount: 5 },
  currentNiveau: 3,
  targetNiveau: 5,
  deadline: null,
  status: 'Active',
  createdAt: '2025-01-01T00:00:00Z',
  goalResources: [],
  readinessFlag: { id: 1, goalId: 3, raisedAt: '2025-03-01T00:00:00Z', dismissedAt: null },
};

beforeEach(() => {
  vi.clearAllMocks();
  mockUseMutation.mockReturnValue({ mutate: vi.fn(), isPending: false });
});

describe('Goals', () => {
  it('renders loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<Goals />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('renders empty state when no goals', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<Goals />);
    expect(screen.getByText('goals.noGoals')).toBeInTheDocument();
  });

  it('renders goal cards with skill name and levels', () => {
    mockUseQuery.mockReturnValue({ data: [mockGoalActive], isLoading: false });
    render(<Goals />);
    expect(screen.getByText('TypeScript')).toBeInTheDocument();
    expect(screen.getByText(/2/)).toBeInTheDocument();
    expect(screen.getByText(/4/)).toBeInTheDocument();
  });

  it('shows overdue badge for past deadline', () => {
    mockUseQuery.mockReturnValue({ data: [mockGoalOverdue], isLoading: false });
    render(<Goals />);
    expect(screen.getByText('goals.overdue')).toBeInTheDocument();
  });

  it('shows readiness flag state', () => {
    mockUseQuery.mockReturnValue({ data: [mockGoalFlagged], isLoading: false });
    render(<Goals />);
    expect(screen.getByText('goals.readinessFlagged')).toBeInTheDocument();
  });

  it('hides signal readiness button when flag active', () => {
    mockUseQuery.mockReturnValue({ data: [mockGoalFlagged], isLoading: false });
    render(<Goals />);
    expect(screen.queryByText('goals.signalReadiness')).not.toBeInTheDocument();
  });

  it('shows signal readiness button when no active flag', () => {
    mockUseQuery.mockReturnValue({ data: [mockGoalActive], isLoading: false });
    render(<Goals />);
    expect(screen.getByText('goals.signalReadiness')).toBeInTheDocument();
  });

  it('renders linked resources', () => {
    mockUseQuery.mockReturnValue({ data: [mockGoalOverdue], isLoading: false });
    render(<Goals />);
    expect(screen.getByText('React Docs')).toBeInTheDocument();
  });

  it('shows title and subtitle', () => {
    mockUseQuery.mockReturnValue({ data: [mockGoalActive], isLoading: false });
    render(<Goals />);
    expect(screen.getByText('goals.title')).toBeInTheDocument();
    expect(screen.getByText('goals.subtitle')).toBeInTheDocument();
  });
});
