import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Goals } from '../Goals';
import type { GoalDto } from '@/api/client';

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
  raiseReadinessFlag: vi.fn(),
  dismissReadinessFlag: vi.fn(),
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return {
    AlertTriangle: I,
    Bell: I,
    BellOff: I,
    BookOpen: I,
    CalendarDays: I,
    CheckCircle2: I,
    ExternalLink: I,
    Flag: I,
    Target: I,
  };
});

const mockUseQuery = useQuery as Mock;
const mockUseMutation = useMutation as Mock;

const noopMutation = { mutate: vi.fn(), isPending: false, isSuccess: false };

const mockGoals: GoalDto[] = [
  {
    id: 1,
    consultantUserId: 'user-1',
    coachUserId: 'coach-1',
    skillId: 10,
    skillName: 'C#',
    currentNiveau: 3,
    targetNiveau: 5,
    deadline: null,
    status: 'Active',
    createdAt: '2026-01-01T00:00:00Z',
    resources: [{ resourceId: 1, title: 'Clean Code', url: 'https://example.com', type: 'Book', isCompleted: true }],
    activeReadinessFlag: null,
  },
  {
    id: 2,
    consultantUserId: 'user-1',
    coachUserId: 'coach-1',
    skillId: 11,
    skillName: 'Docker',
    currentNiveau: 2,
    targetNiveau: 4,
    deadline: '2026-06-01T00:00:00Z',
    status: 'Achieved',
    createdAt: '2026-01-15T00:00:00Z',
    resources: [],
    activeReadinessFlag: null,
  },
  {
    id: 3,
    consultantUserId: 'user-1',
    coachUserId: 'coach-1',
    skillId: 12,
    skillName: 'Kubernetes',
    currentNiveau: 1,
    targetNiveau: 3,
    deadline: null,
    status: 'Active',
    createdAt: '2026-02-01T00:00:00Z',
    resources: [],
    activeReadinessFlag: { id: 5, raisedAt: '2026-03-10T00:00:00Z', ageDays: 3 },
  },
];

beforeEach(() => {
  vi.clearAllMocks();
  mockUseMutation.mockReturnValue(noopMutation);
});

describe('Goals', () => {
  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<Goals />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows empty state when no goals', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<Goals />);
    expect(screen.getByText('goals.noGoals')).toBeInTheDocument();
    expect(screen.getByText('goals.noGoalsHint')).toBeInTheDocument();
  });

  it('shows page title', () => {
    mockUseQuery.mockReturnValue({ data: mockGoals, isLoading: false });
    render(<Goals />);
    expect(screen.getByText('goals.title')).toBeInTheDocument();
  });

  it('renders a card for each goal', () => {
    mockUseQuery.mockReturnValue({ data: mockGoals, isLoading: false });
    render(<Goals />);
    expect(screen.getByText('C#')).toBeInTheDocument();
    expect(screen.getByText('Docker')).toBeInTheDocument();
    expect(screen.getByText('Kubernetes')).toBeInTheDocument();
  });

  it('shows status filter tabs with counts', () => {
    mockUseQuery.mockReturnValue({ data: mockGoals, isLoading: false });
    render(<Goals />);
    expect(screen.getByText('goals.allGoals')).toBeInTheDocument();
    // The tabs are buttons - verify they exist as buttons
    expect(screen.getAllByRole('button', { name: /goals\.status\.active/ }).length).toBeGreaterThan(0);
    expect(screen.getAllByRole('button', { name: /goals\.status\.achieved/ }).length).toBeGreaterThan(0);
  });

  it('filters goals by status', () => {
    mockUseQuery.mockReturnValue({ data: mockGoals, isLoading: false });
    render(<Goals />);
    fireEvent.click(screen.getAllByRole('button', { name: /goals\.status\.achieved/ })[0]);
    expect(screen.getByText('Docker')).toBeInTheDocument();
    expect(screen.queryByText('C#')).not.toBeInTheDocument();
  });

  it('shows linked resources', () => {
    mockUseQuery.mockReturnValue({ data: mockGoals, isLoading: false });
    render(<Goals />);
    expect(screen.getByText('Clean Code')).toBeInTheDocument();
  });

  it('shows readiness flag button for active goals without a flag', () => {
    mockUseQuery.mockReturnValue({ data: mockGoals, isLoading: false });
    render(<Goals />);
    expect(screen.getAllByText('goals.raiseFlag').length).toBeGreaterThan(0);
  });

  it('shows active flag info when flag is present', () => {
    mockUseQuery.mockReturnValue({ data: mockGoals, isLoading: false });
    render(<Goals />);
    expect(screen.getByText(/goals\.flagActive/)).toBeInTheDocument();
  });

  it('shows dismiss button when flag is active', () => {
    mockUseQuery.mockReturnValue({ data: mockGoals, isLoading: false });
    render(<Goals />);
    expect(screen.getByText('goals.dismissFlag')).toBeInTheDocument();
  });

  it('does not show raise flag button for achieved goals', () => {
    const achievedGoal: GoalDto = { ...mockGoals[1] };
    mockUseQuery.mockReturnValue({ data: [achievedGoal], isLoading: false });
    render(<Goals />);
    expect(screen.queryByText('goals.raiseFlag')).not.toBeInTheDocument();
  });

  it('shows progress percentage', () => {
    mockUseQuery.mockReturnValue({ data: [mockGoals[0]], isLoading: false });
    render(<Goals />);
    expect(screen.getByText('60%')).toBeInTheDocument();
  });

  it('shows empty filter state when filtered list is empty', () => {
    mockUseQuery.mockReturnValue({ data: mockGoals, isLoading: false });
    render(<Goals />);
    fireEvent.click(screen.getByText('goals.status.cancelled'));
    expect(screen.getByText('goals.noGoalsInFilter')).toBeInTheDocument();
  });
});
