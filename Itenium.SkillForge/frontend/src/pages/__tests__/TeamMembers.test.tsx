import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery } from '@tanstack/react-query';
import { TeamMembers } from '../TeamMembers';
import type { ConsultantSummaryDto } from '@/api/client';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key}:${JSON.stringify(opts)}` : key),
  }),
}));

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
}));

vi.mock('@/api/client', () => ({
  fetchTeamMembers: vi.fn(),
}));

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to, params }: { children: React.ReactNode; to: string; params?: Record<string, string> }) => (
    <a href={`${to}${params ? `/${Object.values(params).join('/')}` : ''}`}>{children}</a>
  ),
}));

vi.mock('lucide-react', () => {
  const I = ({ className, 'aria-label': ariaLabel }: { className?: string; 'aria-label'?: string }) => (
    <span className={className} aria-label={ariaLabel} />
  );
  return {
    Activity: I,
    AlertTriangle: I,
    Flag: I,
    Search: I,
    Target: I,
    Users: I,
  };
});

const mockUseQuery = useQuery as Mock;

const mockMembers: ConsultantSummaryDto[] = [
  {
    id: 1,
    userId: 'user-1',
    email: 'alice@example.com',
    profileName: 'Backend Dev',
    teamName: 'Alpha',
    activeGoalCount: 3,
    activeFlagCount: 0,
  },
  {
    id: 2,
    userId: 'user-2',
    email: 'bob@example.com',
    profileName: null,
    teamName: 'Beta',
    activeGoalCount: 1,
    activeFlagCount: 2,
  },
  {
    id: 3,
    userId: 'user-3',
    email: null,
    profileName: 'Frontend Dev',
    teamName: 'Alpha',
    activeGoalCount: 0,
    activeFlagCount: 0,
  },
];

beforeEach(() => {
  vi.clearAllMocks();
});

describe('TeamMembers', () => {
  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<TeamMembers />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows empty state when no members', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<TeamMembers />);
    expect(screen.getByText('team.noMembers')).toBeInTheDocument();
  });

  it('shows page title', () => {
    mockUseQuery.mockReturnValue({ data: mockMembers, isLoading: false });
    render(<TeamMembers />);
    expect(screen.getByText('team.title')).toBeInTheDocument();
    expect(screen.getByText('team.subtitle')).toBeInTheDocument();
  });

  it('renders a card for each member', () => {
    mockUseQuery.mockReturnValue({ data: mockMembers, isLoading: false });
    render(<TeamMembers />);
    expect(screen.getByText('alice@example.com')).toBeInTheDocument();
    expect(screen.getByText('bob@example.com')).toBeInTheDocument();
  });

  it('falls back to userId when email is null', () => {
    mockUseQuery.mockReturnValue({ data: mockMembers, isLoading: false });
    render(<TeamMembers />);
    expect(screen.getByText('user-3')).toBeInTheDocument();
  });

  it('shows profile name and team name', () => {
    mockUseQuery.mockReturnValue({ data: [mockMembers[0]], isLoading: false });
    render(<TeamMembers />);
    expect(screen.getByText(/Backend Dev/)).toBeInTheDocument();
    expect(screen.getByText(/Alpha/)).toBeInTheDocument();
  });

  it('shows "no profile" when profileName is null', () => {
    mockUseQuery.mockReturnValue({ data: [mockMembers[1]], isLoading: false });
    render(<TeamMembers />);
    expect(screen.getByText(/team\.noProfile/)).toBeInTheDocument();
  });

  it('shows active goal count', () => {
    mockUseQuery.mockReturnValue({ data: [mockMembers[0]], isLoading: false });
    render(<TeamMembers />);
    expect(screen.getByText(/team\.activeGoals/)).toBeInTheDocument();
  });

  it('shows flag warning icon when member has active flags', () => {
    mockUseQuery.mockReturnValue({ data: [mockMembers[1]], isLoading: false });
    render(<TeamMembers />);
    expect(screen.getByLabelText('active flags')).toBeInTheDocument();
  });

  it('does not show flag warning icon when no flags', () => {
    mockUseQuery.mockReturnValue({ data: [mockMembers[0]], isLoading: false });
    render(<TeamMembers />);
    expect(screen.queryByLabelText('active flags')).not.toBeInTheDocument();
  });

  it('shows active flag count when flags exist', () => {
    mockUseQuery.mockReturnValue({ data: [mockMembers[1]], isLoading: false });
    render(<TeamMembers />);
    expect(screen.getByText(/team\.activeFlags/)).toBeInTheDocument();
  });

  it('shows view activity link for each member', () => {
    mockUseQuery.mockReturnValue({ data: mockMembers, isLoading: false });
    render(<TeamMembers />);
    const links = screen.getAllByText('team.viewActivity');
    expect(links).toHaveLength(3);
  });

  it('filters members by email search', () => {
    mockUseQuery.mockReturnValue({ data: mockMembers, isLoading: false });
    render(<TeamMembers />);
    const input = screen.getByPlaceholderText('team.searchPlaceholder');
    fireEvent.change(input, { target: { value: 'alice' } });
    expect(screen.getByText('alice@example.com')).toBeInTheDocument();
    expect(screen.queryByText('bob@example.com')).not.toBeInTheDocument();
  });

  it('filters members by profile name', () => {
    mockUseQuery.mockReturnValue({ data: mockMembers, isLoading: false });
    render(<TeamMembers />);
    const input = screen.getByPlaceholderText('team.searchPlaceholder');
    fireEvent.change(input, { target: { value: 'frontend' } });
    expect(screen.getByText('user-3')).toBeInTheDocument();
    expect(screen.queryByText('alice@example.com')).not.toBeInTheDocument();
  });

  it('shows empty state when search yields no results', () => {
    mockUseQuery.mockReturnValue({ data: mockMembers, isLoading: false });
    render(<TeamMembers />);
    const input = screen.getByPlaceholderText('team.searchPlaceholder');
    fireEvent.change(input, { target: { value: 'zzznomatch' } });
    expect(screen.getByText('team.noMembers')).toBeInTheDocument();
  });

  it('shows initials avatar from email', () => {
    mockUseQuery.mockReturnValue({ data: [mockMembers[0]], isLoading: false });
    render(<TeamMembers />);
    expect(screen.getByText('AL')).toBeInTheDocument();
  });
});
