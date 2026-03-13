import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery } from '@tanstack/react-query';
import { CoachDashboard } from '../CoachDashboard';
import type { ConsultantDashboardRow } from '@/api/client';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key}:${JSON.stringify(opts)}` : key),
  }),
}));

vi.mock('@tanstack/react-query', () => ({ useQuery: vi.fn() }));

vi.mock('@/api/client', () => ({
  fetchCoachDashboard: vi.fn(),
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return {
    AlertCircle: I,
    Clock: I,
    Flag: I,
    Target: I,
    TrendingUp: I,
    Users: I,
  };
});

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to, params }: { children: React.ReactNode; to: string; params?: Record<string, string> }) => (
    <a href={`${to}${params ? '/' + Object.values(params).join('/') : ''}`}>{children}</a>
  ),
}));

const mockUseQuery = useQuery as Mock;

const mockRows: ConsultantDashboardRow[] = [
  {
    userId: 'alice',
    fullName: 'Alice Smith',
    activeGoalCount: 3,
    readinessFlagCount: 2,
    maxFlagAgeInDays: 5,
    overdueGoalCount: 1,
    lastActivityAt: '2025-03-10T00:00:00Z',
    isInactive: false,
  },
  {
    userId: 'bob',
    fullName: 'Bob Jones',
    activeGoalCount: 1,
    readinessFlagCount: 0,
    maxFlagAgeInDays: null,
    overdueGoalCount: 0,
    lastActivityAt: null,
    isInactive: true,
  },
];

beforeEach(() => {
  vi.clearAllMocks();
});

describe('CoachDashboard', () => {
  it('renders loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<CoachDashboard />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('renders summary cards with counts', () => {
    mockUseQuery.mockReturnValue({ data: mockRows, isLoading: false });
    render(<CoachDashboard />);
    // summary card labels
    expect(screen.getByText('coachDashboard.consultants')).toBeInTheDocument();
    expect(screen.getByText('coachDashboard.readinessFlags')).toBeInTheDocument();
    expect(screen.getByText('coachDashboard.overdueGoals')).toBeInTheDocument();
    expect(screen.getByText('coachDashboard.activeGoals')).toBeInTheDocument();
    // there should be at least one numeric count rendered
    expect(screen.getAllByText('2').length).toBeGreaterThan(0);
  });

  it('renders consultant rows', () => {
    mockUseQuery.mockReturnValue({ data: mockRows, isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    expect(screen.getByText('Bob Jones')).toBeInTheDocument();
  });

  it('shows inactive badge for inactive consultants', () => {
    mockUseQuery.mockReturnValue({ data: mockRows, isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText('coachDashboard.inactive')).toBeInTheDocument();
  });

  it('sorts consultants by flag age (highest first)', () => {
    const rowsForSort: ConsultantDashboardRow[] = [
      { ...mockRows[1], userId: 'bob', fullName: 'Bob Jones', maxFlagAgeInDays: null, readinessFlagCount: 0 },
      { ...mockRows[0], userId: 'alice', fullName: 'Alice Smith', maxFlagAgeInDays: 10, readinessFlagCount: 3 },
    ];
    mockUseQuery.mockReturnValue({ data: rowsForSort, isLoading: false });
    render(<CoachDashboard />);
    const names = screen.getAllByText(/Alice Smith|Bob Jones/);
    // Alice (flagAge=10) should appear before Bob (flagAge=null)
    expect(names[0].textContent).toBe('Alice Smith');
  });

  it('shows no-consultants empty state', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText('coachDashboard.noConsultants')).toBeInTheDocument();
  });
});
