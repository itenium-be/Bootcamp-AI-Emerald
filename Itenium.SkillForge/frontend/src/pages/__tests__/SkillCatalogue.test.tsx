import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery } from '@tanstack/react-query';
import { SkillCatalogue } from '../SkillCatalogue';
import { fetchSkills } from '@/api/client';
import type { SkillListItem } from '@/api/client';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key}:${JSON.stringify(opts)}` : key),
  }),
}));

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to, params }: { children: React.ReactNode; to: string; params?: Record<string, string> }) => (
    <a href={params ? `${to}/${JSON.stringify(params)}` : to}>{children}</a>
  ),
  useNavigate: () => vi.fn(),
}));

vi.mock('@tanstack/react-query', () => ({ useQuery: vi.fn() }));

vi.mock('@/api/client', () => ({
  fetchSkills: vi.fn(),
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return { Search: I, Layers: I, ChevronRight: I, BookOpen: I, CheckCircle2: I };
});

const mockSkills: SkillListItem[] = [
  { id: 1, name: 'C#', categoryName: 'Language & Runtime', levelCount: 7, description: 'The C# language' },
  { id: 2, name: 'ASP.NET Core', categoryName: 'Web & API', levelCount: 5, description: 'Building REST APIs' },
  { id: 3, name: 'Git', categoryName: 'Tooling & DevOps', levelCount: 1, description: 'Version control' },
  { id: 4, name: 'Java', categoryName: 'Language & Runtime', levelCount: 7, description: 'The Java language' },
];

const mockUseQuery = useQuery as Mock;

beforeEach(() => {
  vi.clearAllMocks();
});

describe('SkillCatalogue', () => {
  it('shows loading state while fetching', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<SkillCatalogue />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('renders the page title', () => {
    mockUseQuery.mockReturnValue({ data: mockSkills, isLoading: false });
    render(<SkillCatalogue />);
    expect(screen.getByText('skills.title')).toBeInTheDocument();
  });

  it('renders a card for each skill', () => {
    mockUseQuery.mockReturnValue({ data: mockSkills, isLoading: false });
    render(<SkillCatalogue />);
    expect(screen.getByText('C#')).toBeInTheDocument();
    expect(screen.getByText('ASP.NET Core')).toBeInTheDocument();
    expect(screen.getByText('Git')).toBeInTheDocument();
    expect(screen.getByText('Java')).toBeInTheDocument();
  });

  it('shows skill description on each card', () => {
    mockUseQuery.mockReturnValue({ data: mockSkills, isLoading: false });
    render(<SkillCatalogue />);
    expect(screen.getByText('The C# language')).toBeInTheDocument();
  });

  it('shows the category name on each card', () => {
    mockUseQuery.mockReturnValue({ data: mockSkills, isLoading: false });
    render(<SkillCatalogue />);
    expect(screen.getAllByText('Language & Runtime').length).toBeGreaterThan(0);
  });

  it('filters skills by search query', async () => {
    mockUseQuery.mockReturnValue({ data: mockSkills, isLoading: false });
    render(<SkillCatalogue />);

    const search = screen.getByPlaceholderText('skills.searchPlaceholder');
    fireEvent.change(search, { target: { value: 'java' } });

    await waitFor(() => {
      expect(screen.getByText('Java')).toBeInTheDocument();
      expect(screen.queryByText('C#')).not.toBeInTheDocument();
      expect(screen.queryByText('ASP.NET Core')).not.toBeInTheDocument();
    });
  });

  it('shows all category tabs including "All"', () => {
    mockUseQuery.mockReturnValue({ data: mockSkills, isLoading: false });
    render(<SkillCatalogue />);
    expect(screen.getByRole('button', { name: 'skills.allCategories' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Language & Runtime' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Web & API' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Tooling & DevOps' })).toBeInTheDocument();
  });

  it('filters skills by category tab', async () => {
    mockUseQuery.mockReturnValue({ data: mockSkills, isLoading: false });
    render(<SkillCatalogue />);

    fireEvent.click(screen.getByRole('button', { name: 'Web & API' }));

    await waitFor(() => {
      expect(screen.getByText('ASP.NET Core')).toBeInTheDocument();
      expect(screen.queryByText('C#')).not.toBeInTheDocument();
      expect(screen.queryByText('Git')).not.toBeInTheDocument();
    });
  });

  it('shows all skills when "All" tab is clicked after filtering', async () => {
    mockUseQuery.mockReturnValue({ data: mockSkills, isLoading: false });
    render(<SkillCatalogue />);

    fireEvent.click(screen.getByRole('button', { name: 'Web & API' }));
    fireEvent.click(screen.getByRole('button', { name: 'skills.allCategories' }));

    await waitFor(() => {
      expect(screen.getByText('C#')).toBeInTheDocument();
      expect(screen.getByText('ASP.NET Core')).toBeInTheDocument();
    });
  });

  it('shows empty state when no skills match search', async () => {
    mockUseQuery.mockReturnValue({ data: mockSkills, isLoading: false });
    render(<SkillCatalogue />);

    fireEvent.change(screen.getByPlaceholderText('skills.searchPlaceholder'), {
      target: { value: 'xxxxxxxxxx' },
    });

    await waitFor(() => {
      expect(screen.getByText('skills.noSkills')).toBeInTheDocument();
    });
  });

  it('shows empty state when API returns no skills', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<SkillCatalogue />);
    expect(screen.getByText('skills.noSkills')).toBeInTheDocument();
  });

  it('each skill card links to its detail page', () => {
    mockUseQuery.mockReturnValue({ data: [mockSkills[0]], isLoading: false });
    render(<SkillCatalogue />);
    const link = screen.getByRole('link', { name: /C#/i });
    expect(link).toHaveAttribute('href', expect.stringContaining('1'));
  });

  it('shows level count for progression skills', () => {
    mockUseQuery.mockReturnValue({ data: [mockSkills[0]], isLoading: false });
    render(<SkillCatalogue />);
    // C# has 7 levels
    expect(screen.getByText(/7/)).toBeInTheDocument();
  });

  it('shows checkbox skill label for levelCount=1 skills', () => {
    mockUseQuery.mockReturnValue({ data: [mockSkills[2]], isLoading: false });
    render(<SkillCatalogue />);
    expect(screen.getByText('skills.checkboxSkill')).toBeInTheDocument();
  });

  it('calls fetchSkills as the query function', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<SkillCatalogue />);
    const options = mockUseQuery.mock.calls[0][0] as { queryFn: () => unknown };
    options.queryFn();
    expect(fetchSkills).toHaveBeenCalled();
  });

  it('filters by description when name does not match', async () => {
    mockUseQuery.mockReturnValue({ data: mockSkills, isLoading: false });
    render(<SkillCatalogue />);

    fireEvent.change(screen.getByPlaceholderText('skills.searchPlaceholder'), {
      target: { value: 'version control' },
    });

    await waitFor(() => {
      expect(screen.getByText('Git')).toBeInTheDocument();
      expect(screen.queryByText('C#')).not.toBeInTheDocument();
    });
  });

  it('renders skills with null description without crashing', () => {
    const skillWithoutDesc: SkillListItem = {
      id: 5,
      name: 'Rust',
      categoryName: 'Language & Runtime',
      levelCount: 3,
      description: null,
    };
    mockUseQuery.mockReturnValue({ data: [skillWithoutDesc], isLoading: false });
    render(<SkillCatalogue />);
    expect(screen.getByText('Rust')).toBeInTheDocument();
  });

  it('filters correctly when description is null and search does not match name', async () => {
    const skillWithoutDesc: SkillListItem = {
      id: 5,
      name: 'Rust',
      categoryName: 'Language & Runtime',
      levelCount: 3,
      description: null,
    };
    mockUseQuery.mockReturnValue({ data: [skillWithoutDesc], isLoading: false });
    render(<SkillCatalogue />);

    fireEvent.change(screen.getByPlaceholderText('skills.searchPlaceholder'), {
      target: { value: 'python' },
    });

    await waitFor(() => {
      expect(screen.queryByText('Rust')).not.toBeInTheDocument();
    });
  });

  it('renders skill with unknown category using fallback color', () => {
    const unknownCatSkill: SkillListItem = {
      id: 6,
      name: 'Cobol',
      categoryName: 'Legacy',
      levelCount: 2,
      description: null,
    };
    mockUseQuery.mockReturnValue({ data: [unknownCatSkill], isLoading: false });
    render(<SkillCatalogue />);
    // skill name renders, confirming categoryColor fallback did not throw
    expect(screen.getByText('Cobol')).toBeInTheDocument();
  });
});
