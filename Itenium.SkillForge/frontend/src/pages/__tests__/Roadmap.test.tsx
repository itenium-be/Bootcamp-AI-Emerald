import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery } from '@tanstack/react-query';
import { Roadmap } from '../Roadmap';
import type { RoadmapSkillNode, SeniorityProgressResult } from '@/api/client';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key}:${JSON.stringify(opts)}` : key),
  }),
}));

vi.mock('@tanstack/react-query', () => ({ useQuery: vi.fn() }));

vi.mock('@/api/client', () => ({
  fetchRoadmap: vi.fn(),
  fetchSeniorityProgress: vi.fn(),
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return {
    AlertTriangle: I,
    CheckCircle2: I,
    ChevronDown: I,
    ChevronUp: I,
    Lock: I,
    Map: I,
    Target: I,
    Trophy: I,
  };
});

const mockUseQuery = useQuery as Mock;

const mockNodes: RoadmapSkillNode[] = [
  {
    skillId: 1,
    skillName: 'C#',
    categoryName: 'Language & Runtime',
    levelCount: 7,
    currentNiveau: 3,
    targetNiveau: 5,
    prerequisitesMet: true,
    unmetPrerequisites: [],
  },
  {
    skillId: 2,
    skillName: 'Docker',
    categoryName: 'Tooling & DevOps',
    levelCount: 5,
    currentNiveau: 0,
    targetNiveau: null,
    prerequisitesMet: false,
    unmetPrerequisites: [
      {
        requiredSkillId: 1,
        requiredSkillName: 'C#',
        requiredMinNiveau: 2,
        currentNiveau: 0,
      },
    ],
  },
];

const mockSeniority: SeniorityProgressResult = {
  currentLevel: 'Junior',
  nextLevel: 'Medior',
  met: 5,
  required: 8,
  unmetCriteria: [
    { skillId: 2, skillName: 'Docker', minNiveau: 2, currentNiveau: 0 },
    { skillId: 3, skillName: 'Kubernetes', minNiveau: 1, currentNiveau: 0 },
  ],
};

const mockSeniorityMaxed: SeniorityProgressResult = {
  currentLevel: 'Senior',
  nextLevel: null,
  met: 10,
  required: 10,
  unmetCriteria: [],
};

function setupQueries(nodes: RoadmapSkillNode[] | null, seniority: SeniorityProgressResult | null) {
  mockUseQuery.mockImplementation(({ queryKey }: { queryKey: unknown[] }) => {
    if (queryKey[0] === 'roadmap') return { data: nodes, isLoading: false };
    return { data: seniority, isLoading: false };
  });
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('Roadmap', () => {
  it('shows loading when roadmap is loading', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<Roadmap />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows no-profile state when nodes is null', () => {
    setupQueries(null, null);
    render(<Roadmap />);
    expect(screen.getByText('roadmap.noProfile')).toBeInTheDocument();
    expect(screen.getByText('roadmap.noProfileHint')).toBeInTheDocument();
  });

  it('shows page title and subtitle', () => {
    setupQueries(mockNodes, mockSeniority);
    render(<Roadmap />);
    expect(screen.getByText('roadmap.title')).toBeInTheDocument();
    expect(screen.getByText('roadmap.subtitle')).toBeInTheDocument();
  });

  it('renders a card for each skill node', () => {
    setupQueries(mockNodes, null);
    render(<Roadmap />);
    expect(screen.getByText('C#')).toBeInTheDocument();
    expect(screen.getByText('Docker')).toBeInTheDocument();
  });

  it('shows skill category badge', () => {
    setupQueries(mockNodes, null);
    render(<Roadmap />);
    expect(screen.getByText('Language & Runtime')).toBeInTheDocument();
    expect(screen.getByText('Tooling & DevOps')).toBeInTheDocument();
  });

  it('shows target niveau badge when targetNiveau is set', () => {
    setupQueries(mockNodes, null);
    render(<Roadmap />);
    expect(screen.getByText(/roadmap\.targetLevel/)).toBeInTheDocument();
  });

  it('shows prerequisite warning for unmet prerequisites', () => {
    setupQueries(mockNodes, null);
    render(<Roadmap />);
    expect(screen.getByText(/roadmap\.prereqWarning/)).toBeInTheDocument();
  });

  it('shows seniority card when seniority data is available', () => {
    setupQueries(mockNodes, mockSeniority);
    render(<Roadmap />);
    expect(screen.getByText('Medior')).toBeInTheDocument();
    expect(screen.getByText('roadmap.seniority.targeting')).toBeInTheDocument();
  });

  it('shows unmet seniority criteria as chips', () => {
    setupQueries([], mockSeniority);
    render(<Roadmap />);
    expect(screen.getAllByText('Docker').length).toBeGreaterThan(0);
    expect(screen.getByText('Kubernetes')).toBeInTheDocument();
  });

  it('shows trophy icon and achieved text when Senior is maxed', () => {
    setupQueries(mockNodes, mockSeniorityMaxed);
    render(<Roadmap />);
    expect(screen.getByText('roadmap.seniority.achieved')).toBeInTheDocument();
    expect(screen.getByText('Senior')).toBeInTheDocument();
  });

  it('shows show-all toggle button', () => {
    setupQueries(mockNodes, null);
    render(<Roadmap />);
    expect(screen.getByRole('button', { name: /roadmap\.showAll/ })).toBeInTheDocument();
  });

  it('toggles to show-less when show-all is clicked', () => {
    setupQueries(mockNodes, null);
    render(<Roadmap />);
    const toggle = screen.getByRole('button', { name: /roadmap\.showAll/ });
    fireEvent.click(toggle);
    expect(screen.getByRole('button', { name: /roadmap\.showLess/ })).toBeInTheDocument();
  });

  it('shows empty state when nodes array is empty', () => {
    setupQueries([], null);
    render(<Roadmap />);
    expect(screen.getByText('roadmap.noSkills')).toBeInTheDocument();
  });

  it('shows current niveau progress indicator', () => {
    setupQueries(mockNodes, null);
    render(<Roadmap />);
    // C# has currentNiveau 3 / levelCount 7 → shows "3 / 7"
    expect(screen.getByText('3 / 7')).toBeInTheDocument();
  });

  it('does not render seniority card when seniority is null', () => {
    setupQueries(mockNodes, null);
    render(<Roadmap />);
    expect(screen.queryByText('roadmap.seniority.targeting')).not.toBeInTheDocument();
  });
});
