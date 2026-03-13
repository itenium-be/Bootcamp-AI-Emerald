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

  // TEST-22.2 — filter by skill
  it('skill filter dropdown renders skill options', () => {
    setupQueries(mockResources);
    render(<Resources />);
    const selects = screen.getAllByRole('combobox');
    const skillSelect = selects[0];
    expect(skillSelect).toContainElement(screen.getByText('resources.allSkills'));
    expect(skillSelect).toContainElement(screen.getAllByText('C#')[0]);
    expect(skillSelect).toContainElement(screen.getAllByText('Docker')[0]);
  });

  it('selecting a skill filter shows the clear filters button', () => {
    setupQueries(mockResources);
    render(<Resources />);
    expect(screen.queryByText('resources.clearFilters')).not.toBeInTheDocument();
    fireEvent.change(screen.getAllByRole('combobox')[0], { target: { value: '10' } });
    expect(screen.getByText('resources.clearFilters')).toBeInTheDocument();
  });

  // TEST-22.3 — filter by type
  it('type filter dropdown renders all resource types', () => {
    setupQueries(mockResources);
    render(<Resources />);
    const selects = screen.getAllByRole('combobox');
    const typeSelect = selects[1];
    expect(typeSelect).toContainElement(screen.getByText('resources.allTypes'));
    for (const type of ['Article', 'Video', 'Book', 'Course', 'Documentation', 'Other']) {
      expect(typeSelect.querySelector(`option[value="${type}"]`)).toBeInTheDocument();
    }
  });

  it('selecting a type filter shows the clear filters button', () => {
    setupQueries(mockResources);
    render(<Resources />);
    fireEvent.change(screen.getAllByRole('combobox')[1], { target: { value: 'Video' } });
    expect(screen.getByText('resources.clearFilters')).toBeInTheDocument();
  });

  it('clear filters button resets both filters and hides itself', () => {
    setupQueries(mockResources);
    render(<Resources />);
    fireEvent.change(screen.getAllByRole('combobox')[0], { target: { value: '10' } });
    fireEvent.click(screen.getByText('resources.clearFilters'));
    expect(screen.queryByText('resources.clearFilters')).not.toBeInTheDocument();
  });

  // TEST-22.4 — add resource form
  it('add resource modal shows title, url, type, and skill fields', () => {
    setupQueries(mockResources);
    render(<Resources />);
    fireEvent.click(screen.getAllByText('resources.addResource')[0]);
    // 'resources.title' also appears as the page h1, so use getAllByText
    expect(screen.getAllByText('resources.title').length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('resources.url')).toBeInTheDocument();
    expect(screen.getByText('resources.type')).toBeInTheDocument();
    expect(screen.getByText('resources.skill')).toBeInTheDocument();
  });

  it('submitting form without required fields shows validation error', () => {
    setupQueries(mockResources);
    render(<Resources />);
    fireEvent.click(screen.getAllByText('resources.addResource')[0]);
    // fireEvent.click on submit button doesn't propagate as form submit in jsdom — use fireEvent.submit
    const form = document.querySelector('form')!;
    fireEvent.submit(form);
    expect(screen.getByText('resources.requiredFields')).toBeInTheDocument();
  });

  it('submitting valid form calls createResource mutation', () => {
    const createMutate = vi.fn();
    mockUseMutation.mockReturnValue({ mutate: createMutate, isPending: false, isSuccess: false });
    setupQueries(mockResources);
    render(<Resources />);
    fireEvent.click(screen.getAllByText('resources.addResource')[0]);

    const textboxes = screen.getAllByRole('textbox');
    fireEvent.change(textboxes[0], { target: { value: 'My Resource' } });
    fireEvent.change(textboxes[1], { target: { value: 'https://example.com/new' } });

    const modalSelects = screen.getAllByRole('combobox');
    // In modal: index 2 = type select, index 3 = skill select (0 and 1 are page filters)
    fireEvent.change(modalSelects[3], { target: { value: '10' } });

    const form = document.querySelector('form')!;
    fireEvent.submit(form);
    expect(createMutate).toHaveBeenCalledWith({
      title: 'My Resource',
      url: 'https://example.com/new',
      type: 'Article',
      skillId: 10,
      fromNiveau: 1,
      toNiveau: 3,
    });
  });

  // TEST-22.6 — rating
  it('clicking thumbs up calls rate mutation with true', () => {
    setupQueries([mockResources[0]]);
    render(<Resources />);
    fireEvent.click(screen.getByLabelText('resources.ratePositive'));
    expect(noopMutation.mutate).toHaveBeenCalledWith(true);
  });

  it('clicking thumbs down calls rate mutation with false', () => {
    setupQueries([mockResources[0]]);
    render(<Resources />);
    fireEvent.click(screen.getByLabelText('resources.rateNegative'));
    expect(noopMutation.mutate).toHaveBeenCalledWith(false);
  });

  // TEST-22.7 — completed badge
  it('resource card shows completed state when mutation succeeds', () => {
    const successMutation = { mutate: vi.fn(), isPending: false, isSuccess: true };
    mockUseMutation.mockReturnValue(successMutation);
    setupQueries([mockResources[0]]);
    render(<Resources />);
    expect(screen.getByText('resources.completed')).toBeInTheDocument();
    expect(screen.queryByText('resources.markComplete')).not.toBeInTheDocument();
  });

  // TEST-22.8 — mark complete
  it('clicking mark complete button calls complete mutation', () => {
    setupQueries([mockResources[0]]);
    render(<Resources />);
    fireEvent.click(screen.getByText('resources.markComplete'));
    expect(noopMutation.mutate).toHaveBeenCalled();
  });

  // Additional card content tests
  it('shows resource type badge on each card', () => {
    setupQueries(mockResources);
    render(<Resources />);
    expect(screen.getByText('Book')).toBeInTheDocument();
    expect(screen.getByText('Video')).toBeInTheDocument();
  });

  it('shows niveau range on resource card', () => {
    setupQueries([mockResources[0]]);
    render(<Resources />);
    expect(screen.getByText('resources.niveauRange:{"from":1,"to":5}')).toBeInTheDocument();
  });

  it('shows completions count on resource card', () => {
    setupQueries([mockResources[0]]);
    render(<Resources />);
    expect(screen.getByText('resources.completions:{"count":12}')).toBeInTheDocument();
  });

  it('resource title is a link with correct href and opens in new tab', () => {
    setupQueries([mockResources[0]]);
    render(<Resources />);
    const link = screen.getByRole('link', { name: /Clean Code/ });
    expect(link).toHaveAttribute('href', 'https://example.com/clean-code');
    expect(link).toHaveAttribute('target', '_blank');
  });
});
