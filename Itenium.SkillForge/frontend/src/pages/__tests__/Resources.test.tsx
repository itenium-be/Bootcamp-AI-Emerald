import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Resources } from '../Resources';
import type { ResourceDto } from '@/api/client';

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
  fetchResources: vi.fn(),
  fetchSkills: vi.fn(),
  createResource: vi.fn(),
  completeResource: vi.fn(),
  rateResource: vi.fn(),
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return {
    CheckCircle2: I,
    ExternalLink: I,
    Filter: I,
    Library: I,
    Plus: I,
    ThumbsDown: I,
    ThumbsUp: I,
    X: I,
  };
});

const mockUseQuery = useQuery as Mock;
const mockUseMutation = useMutation as Mock;

const noopMutation = { mutate: vi.fn(), isPending: false, isSuccess: false };

const mockResources: ResourceDto[] = [
  {
    id: 1,
    title: 'Clean Code',
    url: 'https://example.com/clean-code',
    type: 'Book',
    skillId: 10,
    skillName: 'C#',
    fromNiveau: 1,
    toNiveau: 5,
    addedByUserId: 'user-1',
    addedAt: '2026-01-01T00:00:00Z',
    completionCount: 12,
    positiveRatings: 8,
    negativeRatings: 1,
  },
  {
    id: 2,
    title: 'Docker Deep Dive',
    url: 'https://example.com/docker',
    type: 'Video',
    skillId: 11,
    skillName: 'Docker',
    fromNiveau: 1,
    toNiveau: 3,
    addedByUserId: 'user-2',
    addedAt: '2026-02-01T00:00:00Z',
    completionCount: 5,
    positiveRatings: 4,
    negativeRatings: 0,
  },
];

const mockSkills = [
  { id: 10, name: 'C#', categoryName: 'Language & Runtime', levelCount: 7, description: null },
  { id: 11, name: 'Docker', categoryName: 'Tooling & DevOps', levelCount: 5, description: null },
];

function setupQueries(resources: ResourceDto[] | null, skills = mockSkills) {
  mockUseQuery.mockImplementation(({ queryKey }: { queryKey: unknown[] }) => {
    if (queryKey[0] === 'skills') return { data: skills, isLoading: false };
    return { data: resources, isLoading: false };
  });
}

beforeEach(() => {
  vi.clearAllMocks();
  mockUseMutation.mockReturnValue(noopMutation);
});

describe('Resources', () => {
  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<Resources />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows empty state when no resources', () => {
    setupQueries([]);
    render(<Resources />);
    expect(screen.getByText('resources.noResources')).toBeInTheDocument();
  });

  it('shows page title and subtitle', () => {
    setupQueries(mockResources);
    render(<Resources />);
    expect(screen.getByText('resources.title')).toBeInTheDocument();
    expect(screen.getByText('resources.subtitle')).toBeInTheDocument();
  });

  it('renders a card for each resource', () => {
    setupQueries(mockResources);
    render(<Resources />);
    expect(screen.getByText('Clean Code')).toBeInTheDocument();
    expect(screen.getByText('Docker Deep Dive')).toBeInTheDocument();
  });

  it('shows skill names in resource cards', () => {
    setupQueries(mockResources);
    render(<Resources />);
    expect(screen.getAllByText('C#').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Docker').length).toBeGreaterThan(0);
  });

  it('shows add resource button', () => {
    setupQueries(mockResources);
    render(<Resources />);
    expect(screen.getByText('resources.addResource')).toBeInTheDocument();
  });

  it('shows rating counts', () => {
    setupQueries(mockResources);
    render(<Resources />);
    expect(screen.getByText('8')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
  });

  it('shows mark complete button', () => {
    setupQueries(mockResources);
    render(<Resources />);
    expect(screen.getAllByText('resources.markComplete').length).toBeGreaterThan(0);
  });

  it('shows resource count', () => {
    setupQueries(mockResources);
    render(<Resources />);
    expect(screen.getByText(/resources\.count/)).toBeInTheDocument();
  });

  it('opens add resource modal when button is clicked', () => {
    setupQueries(mockResources);
    render(<Resources />);
    fireEvent.click(screen.getAllByText('resources.addResource')[0]);
    expect(screen.getByText('common.cancel')).toBeInTheDocument();
  });

  it('closes modal when cancel is clicked', () => {
    setupQueries(mockResources);
    render(<Resources />);
    fireEvent.click(screen.getAllByText('resources.addResource')[0]);
    fireEvent.click(screen.getByText('common.cancel'));
    expect(screen.queryByText('common.save')).not.toBeInTheDocument();
  });
});
