import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery } from '@tanstack/react-query';
import { ConsultantActivity } from '../ConsultantActivity';
import type { ActivityEventDto, SeniorityProgressResult } from '@/api/client';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key}:${JSON.stringify(opts)}` : key),
  }),
}));

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
}));

vi.mock('@/api/client', () => ({
  fetchConsultantActivityEvents: vi.fn(),
  fetchConsultantSeniorityProgress: vi.fn(),
}));

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to }: { children: React.ReactNode; to: string }) => <a href={to}>{children}</a>,
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return {
    ArrowLeft: I,
    BookOpen: I,
    CheckCircle2: I,
    ChevronRight: I,
    Target: I,
    Trophy: I,
    Zap: I,
  };
});

const mockUseQuery = useQuery as Mock;

const mockActivity: ActivityEventDto[] = [
  {
    eventType: 'SkillValidated',
    occurredAt: '2026-03-01T10:00:00Z',
    description: 'Validated C# at level 3',
    skillName: 'C#',
    niveau: 3,
    resourceTitle: null,
  },
  {
    eventType: 'GoalAchieved',
    occurredAt: '2026-03-05T14:00:00Z',
    description: 'Goal achieved: Docker level 4',
    skillName: 'Docker',
    niveau: 4,
    resourceTitle: null,
  },
  {
    eventType: 'ResourceCompleted',
    occurredAt: '2026-02-20T09:00:00Z',
    description: 'Completed resource: Clean Code',
    skillName: null,
    niveau: null,
    resourceTitle: 'Clean Code',
  },
];

const mockSeniority: SeniorityProgressResult = {
  currentLevel: 'Junior',
  nextLevel: 'Medior',
  met: 3,
  required: 5,
  unmetCriteria: [
    { skillId: 10, skillName: 'C#', minNiveau: 4, currentNiveau: 3 },
    { skillId: 11, skillName: 'Docker', minNiveau: 3, currentNiveau: 2 },
  ],
};

const mockSeniorityMaxed: SeniorityProgressResult = {
  currentLevel: 'Senior',
  nextLevel: null,
  met: 8,
  required: 8,
  unmetCriteria: [],
};

beforeEach(() => {
  vi.clearAllMocks();
});

describe('ConsultantActivity', () => {
  it('shows loading state while activity is loading', () => {
    mockUseQuery.mockReturnValueOnce({ data: undefined, isLoading: true }).mockReturnValueOnce({ data: undefined });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows page title and subtitle', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: mockActivity, isLoading: false })
      .mockReturnValueOnce({ data: mockSeniority });
    render(<ConsultantActivity consultantId={1} />);
    // activity.title appears in h1 and in the timeline section h2
    expect(screen.getAllByText('activity.title').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('activity.subtitle')).toBeInTheDocument();
  });

  it('shows back-to-team link', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: mockActivity, isLoading: false })
      .mockReturnValueOnce({ data: mockSeniority });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText('activity.backToTeam')).toBeInTheDocument();
  });

  it('shows empty state when no activity', () => {
    mockUseQuery.mockReturnValueOnce({ data: [], isLoading: false }).mockReturnValueOnce({ data: undefined });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText('activity.noActivity')).toBeInTheDocument();
    expect(screen.getByText('activity.noActivityHint')).toBeInTheDocument();
  });

  it('renders activity event descriptions', () => {
    mockUseQuery.mockReturnValueOnce({ data: mockActivity, isLoading: false }).mockReturnValueOnce({ data: undefined });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText('Validated C# at level 3')).toBeInTheDocument();
    expect(screen.getByText('Goal achieved: Docker level 4')).toBeInTheDocument();
    expect(screen.getByText('Completed resource: Clean Code')).toBeInTheDocument();
  });

  it('shows event type labels', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: [mockActivity[0]], isLoading: false })
      .mockReturnValueOnce({ data: undefined });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText('activity.eventType.SkillValidated')).toBeInTheDocument();
  });

  it('shows niveau badge when niveau is present', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: [mockActivity[0]], isLoading: false })
      .mockReturnValueOnce({ data: undefined });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText(/activity\.niveau/)).toBeInTheDocument();
  });

  it('groups events by month', () => {
    mockUseQuery.mockReturnValueOnce({ data: mockActivity, isLoading: false }).mockReturnValueOnce({ data: undefined });
    render(<ConsultantActivity consultantId={1} />);
    // Two events in March 2026, one in February 2026 — so two month groups
    const monthHeaders = screen.getAllByText(/2026/);
    expect(monthHeaders.length).toBeGreaterThanOrEqual(2);
  });

  it('shows seniority section heading', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: mockActivity, isLoading: false })
      .mockReturnValueOnce({ data: mockSeniority });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText('activity.seniorityProgress')).toBeInTheDocument();
  });

  it('shows seniority targeting label when not maxed', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: mockActivity, isLoading: false })
      .mockReturnValueOnce({ data: mockSeniority });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText('roadmap.seniority.targeting')).toBeInTheDocument();
    expect(screen.getByText('Medior')).toBeInTheDocument();
  });

  it('shows seniority criteria count', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: mockActivity, isLoading: false })
      .mockReturnValueOnce({ data: mockSeniority });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText(/3 \/ 5/)).toBeInTheDocument();
  });

  it('shows unmet criteria badges', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: mockActivity, isLoading: false })
      .mockReturnValueOnce({ data: mockSeniority });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText('C#')).toBeInTheDocument();
    expect(screen.getByText('Docker')).toBeInTheDocument();
  });

  it('shows trophy icon and achieved label when maxed out', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: mockActivity, isLoading: false })
      .mockReturnValueOnce({ data: mockSeniorityMaxed });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText('roadmap.seniority.achieved')).toBeInTheDocument();
    expect(screen.getByText('Senior')).toBeInTheDocument();
  });

  it('shows no profile message when seniority data is unavailable', () => {
    mockUseQuery.mockReturnValueOnce({ data: mockActivity, isLoading: false }).mockReturnValueOnce({ data: undefined });
    render(<ConsultantActivity consultantId={1} />);
    expect(screen.getByText('roadmap.noProfile')).toBeInTheDocument();
  });
});
