import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery } from '@tanstack/react-query';
import { CoachDashboard } from '../CoachDashboard';
import type { ConsultantSummary } from '@/api/client';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key}:${JSON.stringify(opts)}` : key),
  }),
}));

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
}));

vi.mock('@/api/client', () => ({
  fetchTeamDashboard: vi.fn(),
}));

const mockNavigate = vi.fn();
vi.mock('@tanstack/react-router', () => ({
  useNavigate: () => mockNavigate,
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return { AlertTriangle: I, Clock: I, Flag: I, Target: I, Users: I };
});

vi.mock('@itenium-forge/ui', () => ({
  Badge: ({ children, variant }: { children: React.ReactNode; variant?: string }) => (
    <span data-variant={variant}>{children}</span>
  ),
  Table: ({ children }: { children: React.ReactNode }) => <table>{children}</table>,
  TableHeader: ({ children }: { children: React.ReactNode }) => <thead>{children}</thead>,
  TableBody: ({ children }: { children: React.ReactNode }) => <tbody>{children}</tbody>,
  TableHead: ({ children }: { children: React.ReactNode }) => <th>{children}</th>,
  TableRow: ({
    children,
    onClick,
    className,
  }: {
    children: React.ReactNode;
    onClick?: () => void;
    className?: string;
  }) => (
    <tr onClick={onClick} className={className}>
      {children}
    </tr>
  ),
  TableCell: ({ children, className }: { children: React.ReactNode; className?: string }) => (
    <td className={className}>{children}</td>
  ),
}));

const mockConsultants: ConsultantSummary[] = [
  {
    id: 'c1',
    name: 'Lea Martin',
    email: 'lea@example.com',
    activeGoalCount: 2,
    readinessFlags: [{ skillName: 'Clean Code', raisedAt: '2026-03-11T00:00:00Z', ageInDays: 2 }],
    lastActivityDate: '2026-03-11T00:00:00Z',
    daysSinceActivity: 2,
  },
  {
    id: 'c2',
    name: 'Thomas Dupont',
    email: 'thomas@example.com',
    activeGoalCount: 0,
    readinessFlags: [],
    lastActivityDate: '2026-02-18T00:00:00Z',
    daysSinceActivity: 23,
  },
];

const mockUseQuery = useQuery as Mock;

beforeEach(() => {
  vi.clearAllMocks();
  mockUseQuery.mockReturnValue({ data: mockConsultants, isLoading: false });
});

describe('CoachDashboard', () => {
  it('shows page title', () => {
    render(<CoachDashboard />);
    expect(screen.getByText('coach.dashboard')).toBeInTheDocument();
  });

  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<CoachDashboard />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('renders a row for each consultant', () => {
    render(<CoachDashboard />);
    expect(screen.getByText('Lea Martin')).toBeInTheDocument();
    expect(screen.getByText('Thomas Dupont')).toBeInTheDocument();
  });

  it('shows empty state when no consultants', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText('coach.noConsultants')).toBeInTheDocument();
  });

  it('highlights inactive consultant rows (>21 days)', () => {
    render(<CoachDashboard />);
    const rows = screen.getAllByRole('row');
    const thomasRow = rows.find((r) => r.textContent?.includes('Thomas Dupont'));
    expect(thomasRow).toHaveAttribute('class');
    expect(thomasRow?.className).toMatch(/inactive|destructive|orange|bg-red|bg-amber/i);
  });

  it('shows readiness flag age badge', () => {
    render(<CoachDashboard />);
    expect(screen.getByText(/coach\.flagAge/)).toBeInTheDocument();
  });

  it('shows active goal count', () => {
    render(<CoachDashboard />);
    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('navigates to consultant profile on row click', () => {
    render(<CoachDashboard />);
    fireEvent.click(screen.getByText('Lea Martin'));
    expect(mockNavigate).toHaveBeenCalledWith({
      to: '/team/consultants/$consultantId',
      params: { consultantId: 'c1' },
    });
  });
});
