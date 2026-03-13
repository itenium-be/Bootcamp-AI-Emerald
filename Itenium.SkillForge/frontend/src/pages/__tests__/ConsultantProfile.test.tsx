import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ConsultantProfile } from '../ConsultantProfile';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key}:${JSON.stringify(opts)}` : key),
  }),
}));

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
  useMutation: vi.fn(),
  useQueryClient: vi.fn(),
}));

vi.mock('@/api/client', () => ({
  fetchConsultantProfile: vi.fn(),
  fetchConsultantActivity: vi.fn(),
  startSession: vi.fn(),
}));

const mockNavigate = vi.fn();
vi.mock('@tanstack/react-router', () => ({
  useNavigate: () => mockNavigate,
  useParams: () => ({ consultantId: 'c1' }),
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return { Activity: I, BookOpen: I, Flag: I, Star: I, Target: I, Users: I, Zap: I };
});

vi.mock('@itenium-forge/ui', () => ({
  Button: ({ children, onClick, disabled }: React.ComponentProps<'button'>) => (
    <button onClick={onClick} disabled={disabled}>
      {children}
    </button>
  ),
  Badge: ({ children, variant }: { children: React.ReactNode; variant?: string }) => (
    <span data-variant={variant}>{children}</span>
  ),
}));

const mockProfile = {
  id: 'c1',
  name: 'Lea Martin',
  email: 'lea@example.com',
  skills: [
    { skillId: 1, skillName: 'Clean Code', categoryName: 'Engineering', levelCount: 3, currentNiveau: 1 },
    { skillId: 2, skillName: 'TypeScript', categoryName: 'Engineering', levelCount: 4, currentNiveau: 2 },
  ],
  activeGoals: [
    {
      id: 10,
      title: 'Reach Clean Code L2',
      description: null,
      dueDate: '2026-06-01',
      skillId: 1,
      skillName: 'Clean Code',
    },
  ],
};

const mockActivity = [
  {
    id: 1,
    type: 'validation' as const,
    description: 'Clean Code validated at level 1',
    occurredAt: '2026-03-10T10:00:00Z',
  },
  { id: 2, type: 'session' as const, description: 'Session recorded', occurredAt: '2026-03-08T09:00:00Z' },
];

const mockUseQuery = useQuery as Mock;
const mockUseMutation = useMutation as Mock;
const mockUseQueryClient = useQueryClient as Mock;

const mockMutate = vi.fn();
const mockInvalidate = vi.fn();

beforeEach(() => {
  vi.clearAllMocks();
  mockUseQueryClient.mockReturnValue({ invalidateQueries: mockInvalidate });
  mockUseMutation.mockReturnValue({ mutate: mockMutate, isPending: false });
  mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
    if (queryKey.includes('activity')) return { data: mockActivity, isLoading: false };
    return { data: mockProfile, isLoading: false };
  });
});

describe('ConsultantProfile', () => {
  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<ConsultantProfile />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows consultant name', () => {
    render(<ConsultantProfile />);
    expect(screen.getByText('Lea Martin')).toBeInTheDocument();
  });

  it('shows skills section', () => {
    render(<ConsultantProfile />);
    expect(screen.getByText('consultant.skills')).toBeInTheDocument();
    expect(screen.getByText('Clean Code')).toBeInTheDocument();
    expect(screen.getByText('TypeScript')).toBeInTheDocument();
  });

  it('shows active goals section', () => {
    render(<ConsultantProfile />);
    expect(screen.getByText('consultant.activeGoals')).toBeInTheDocument();
    expect(screen.getByText('Reach Clean Code L2')).toBeInTheDocument();
  });

  it('shows activity feed section', () => {
    render(<ConsultantProfile />);
    expect(screen.getByText('consultant.recentActivity')).toBeInTheDocument();
    expect(screen.getByText('Clean Code validated at level 1')).toBeInTheDocument();
  });

  it('shows empty states when no data', () => {
    mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
      if (queryKey.includes('activity')) return { data: [], isLoading: false };
      return { data: { ...mockProfile, skills: [], activeGoals: [] }, isLoading: false };
    });
    render(<ConsultantProfile />);
    expect(screen.getByText('consultant.noSkills')).toBeInTheDocument();
    expect(screen.getByText('consultant.noGoals')).toBeInTheDocument();
    expect(screen.getByText('consultant.noActivity')).toBeInTheDocument();
  });

  it('shows Start Session button', () => {
    render(<ConsultantProfile />);
    expect(screen.getByText('consultant.startSession')).toBeInTheDocument();
  });

  it('calls startSession mutation and navigates on button click', () => {
    const setItemSpy = vi.spyOn(Storage.prototype, 'setItem');
    mockUseMutation.mockImplementation(({ onSuccess }: { onSuccess: (data: { sessionId: string }) => void }) => ({
      mutate: () => {
        onSuccess({ sessionId: 'sess-1' });
      },
      isPending: false,
    }));
    render(<ConsultantProfile />);
    fireEvent.click(screen.getByText('consultant.startSession'));
    expect(setItemSpy).toHaveBeenCalledWith('currentSessionId', 'sess-1');
    expect(mockNavigate).toHaveBeenCalledWith({
      to: '/team/consultants/$consultantId/session',
      params: { consultantId: 'c1' },
    });
  });
});
