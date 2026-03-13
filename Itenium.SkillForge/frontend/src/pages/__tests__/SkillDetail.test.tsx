import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery } from '@tanstack/react-query';
import { SkillDetail } from '../SkillDetail';
import { fetchSkillDetail } from '@/api/client';
import type { SkillDetail as SkillDetailType } from '@/api/client';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key}:${JSON.stringify(opts)}` : key),
  }),
}));

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to, params }: { children: React.ReactNode; to: string; params?: Record<string, unknown> }) => (
    <a href={params ? `${to}/${String(params.skillId ?? '')}` : to}>{children}</a>
  ),
  useParams: () => ({ skillId: '1' }),
}));

vi.mock('@tanstack/react-query', () => ({ useQuery: vi.fn() }));

vi.mock('@/api/client', () => ({
  fetchSkillDetail: vi.fn(),
}));

vi.mock('lucide-react', () => {
  const I = () => <span />;
  return { ArrowLeft: I, CheckCircle2: I, AlertTriangle: I, ChevronRight: I };
});

const mockSkill: SkillDetailType = {
  id: 1,
  name: 'C#',
  categoryName: 'Language & Runtime',
  levelCount: 5,
  description: 'The C# programming language.',
  levels: [
    { niveau: 1, descriptor: 'Writes basic code' },
    { niveau: 2, descriptor: 'Uses OOP concepts' },
    { niveau: 3, descriptor: 'Applies generics' },
    { niveau: 4, descriptor: 'Writes async code' },
    { niveau: 5, descriptor: 'Deep CLR knowledge' },
  ],
  prerequisites: [{ requiredSkillId: 10, requiredSkillName: '.NET', requiredMinNiveau: 1 }],
};

const mockUseQuery = useQuery as Mock;

beforeEach(() => {
  vi.clearAllMocks();
});

describe('SkillDetail', () => {
  it('shows loading state while fetching', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<SkillDetail />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('renders the skill name', () => {
    mockUseQuery.mockReturnValue({ data: mockSkill, isLoading: false });
    render(<SkillDetail />);
    expect(screen.getByRole('heading', { name: 'C#' })).toBeInTheDocument();
  });

  it('renders the category badge', () => {
    mockUseQuery.mockReturnValue({ data: mockSkill, isLoading: false });
    render(<SkillDetail />);
    expect(screen.getByText('Language & Runtime')).toBeInTheDocument();
  });

  it('renders the skill description', () => {
    mockUseQuery.mockReturnValue({ data: mockSkill, isLoading: false });
    render(<SkillDetail />);
    expect(screen.getByText('The C# programming language.')).toBeInTheDocument();
  });

  it('renders all level descriptors', () => {
    mockUseQuery.mockReturnValue({ data: mockSkill, isLoading: false });
    render(<SkillDetail />);
    expect(screen.getByText('Writes basic code')).toBeInTheDocument();
    expect(screen.getByText('Uses OOP concepts')).toBeInTheDocument();
    expect(screen.getByText('Deep CLR knowledge')).toBeInTheDocument();
  });

  it('renders prerequisite skill names', () => {
    mockUseQuery.mockReturnValue({ data: mockSkill, isLoading: false });
    render(<SkillDetail />);
    expect(screen.getByText('.NET')).toBeInTheDocument();
  });

  it('renders prerequisites as clickable links', () => {
    mockUseQuery.mockReturnValue({ data: mockSkill, isLoading: false });
    render(<SkillDetail />);
    const prereqLink = screen.getByRole('link', { name: /.NET/ });
    expect(prereqLink).toBeInTheDocument();
    expect(prereqLink).toHaveAttribute('href', expect.stringContaining('10'));
  });

  it('shows checkbox skill label when levelCount is 1', () => {
    const checkboxSkill: SkillDetailType = {
      ...mockSkill,
      levelCount: 1,
      levels: [],
    };
    mockUseQuery.mockReturnValue({ data: checkboxSkill, isLoading: false });
    render(<SkillDetail />);
    expect(screen.getByText('skills.checkboxSkill')).toBeInTheDocument();
    expect(screen.getByText('skills.checkboxSkillHint')).toBeInTheDocument();
  });

  it('does not render levels section for checkbox skills', () => {
    const checkboxSkill: SkillDetailType = {
      ...mockSkill,
      levelCount: 1,
      levels: [],
    };
    mockUseQuery.mockReturnValue({ data: checkboxSkill, isLoading: false });
    render(<SkillDetail />);
    expect(screen.queryByText('Writes basic code')).not.toBeInTheDocument();
  });

  it('shows back to skills link', () => {
    mockUseQuery.mockReturnValue({ data: mockSkill, isLoading: false });
    render(<SkillDetail />);
    const backLink = screen.getByRole('link', { name: /skills.backToSkills/i });
    expect(backLink).toHaveAttribute('href', '/skills');
  });

  it('shows not-found message when skill is null', () => {
    mockUseQuery.mockReturnValue({ data: null, isLoading: false });
    render(<SkillDetail />);
    expect(screen.getByText('skills.notFound')).toBeInTheDocument();
    expect(screen.getByText('skills.notFoundHint')).toBeInTheDocument();
  });

  it('shows prerequisites section header', () => {
    mockUseQuery.mockReturnValue({ data: mockSkill, isLoading: false });
    render(<SkillDetail />);
    expect(screen.getByText('skills.prerequisites')).toBeInTheDocument();
  });

  it('hides prerequisites section when there are none', () => {
    const noPrereqs: SkillDetailType = { ...mockSkill, prerequisites: [] };
    mockUseQuery.mockReturnValue({ data: noPrereqs, isLoading: false });
    render(<SkillDetail />);
    expect(screen.queryByText('skills.prerequisites')).not.toBeInTheDocument();
  });

  it('calls fetchSkillDetail as the query function', () => {
    mockUseQuery.mockReturnValue({ data: mockSkill, isLoading: false });
    render(<SkillDetail />);
    const options = mockUseQuery.mock.calls[0][0] as { queryFn: () => unknown };
    options.queryFn();
    expect(fetchSkillDetail).toHaveBeenCalledWith(1);
  });

  it('does not render description paragraph when description is null', () => {
    const noDesc: SkillDetailType = { ...mockSkill, description: null };
    mockUseQuery.mockReturnValue({ data: noDesc, isLoading: false });
    render(<SkillDetail />);
    expect(screen.queryByText('The C# programming language.')).not.toBeInTheDocument();
  });

  it('does not show checkbox badge for progression skills', () => {
    mockUseQuery.mockReturnValue({ data: mockSkill, isLoading: false });
    render(<SkillDetail />);
    expect(screen.queryByText('skills.checkboxSkill')).not.toBeInTheDocument();
    expect(screen.queryByText('skills.checkboxSkillHint')).not.toBeInTheDocument();
  });

  it('renders skill with unknown category using fallback color', () => {
    const unknownCatSkill: SkillDetailType = { ...mockSkill, categoryName: 'Legacy' };
    mockUseQuery.mockReturnValue({ data: unknownCatSkill, isLoading: false });
    render(<SkillDetail />);
    // skill heading renders, confirming categoryColor fallback did not throw
    expect(screen.getByRole('heading', { name: 'C#' })).toBeInTheDocument();
  });

  it('shows back to skills link on not-found page', () => {
    mockUseQuery.mockReturnValue({ data: null, isLoading: false });
    render(<SkillDetail />);
    const backLink = screen.getByRole('link', { name: /skills.backToSkills/i });
    expect(backLink).toHaveAttribute('href', '/skills');
  });
});
