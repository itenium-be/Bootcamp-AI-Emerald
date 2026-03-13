import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { LiveSession } from '../LiveSession';

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
  createValidation: vi.fn(),
  createCoachGoal: vi.fn(),
  endSession: vi.fn(),
}));

const mockNavigate = vi.fn();
vi.mock('@tanstack/react-router', () => ({
  useNavigate: () => mockNavigate,
  useParams: () => ({ consultantId: 'c1' }),
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return { CheckCircle: I, Plus: I, Target: I, X: I, Zap: I };
});

vi.mock('@itenium-forge/ui', () => ({
  Button: ({ children, onClick, disabled, variant }: React.ComponentProps<'button'> & { variant?: string }) => (
    <button onClick={onClick} disabled={disabled} data-variant={variant}>
      {children}
    </button>
  ),
  Input: ({ placeholder, value, onChange, type, id }: React.ComponentProps<'input'>) => (
    <input placeholder={placeholder} value={value} onChange={onChange} type={type} id={id} />
  ),
  Label: ({ children, htmlFor }: React.ComponentProps<'label'>) => <label htmlFor={htmlFor}>{children}</label>,
  Select: ({
    children,
    value,
    onValueChange,
  }: {
    children: React.ReactNode;
    value: string;
    onValueChange: (v: string) => void;
  }) => (
    <div data-value={value} data-testid="select" onClick={() => onValueChange('1')}>
      {children}
    </div>
  ),
  SelectTrigger: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  SelectContent: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  SelectItem: ({ children, value }: { children: React.ReactNode; value: string }) => (
    <div data-value={value}>{children}</div>
  ),
  SelectValue: () => <span />,
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
    { id: 10, title: 'Reach Clean Code L2', description: null, dueDate: null, skillId: 1, skillName: 'Clean Code' },
  ],
};

const mockUseQuery = useQuery as Mock;
const mockUseMutation = useMutation as Mock;
const mockUseQueryClient = useQueryClient as Mock;

const mockMutate = vi.fn();
const mockInvalidate = vi.fn();

beforeEach(() => {
  vi.clearAllMocks();
  mockUseQueryClient.mockReturnValue({ invalidateQueries: mockInvalidate });
  mockUseMutation.mockReturnValue({ mutate: mockMutate, isPending: false });
  mockUseQuery.mockReturnValue({ data: mockProfile, isLoading: false });
});

describe('LiveSession', () => {
  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<LiveSession />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows session subtitle with consultant name', () => {
    render(<LiveSession />);
    expect(screen.getByText(/session.subtitle/)).toBeInTheDocument();
  });

  it('shows skills for validation', () => {
    render(<LiveSession />);
    expect(screen.getByText('Clean Code')).toBeInTheDocument();
    expect(screen.getByText('TypeScript')).toBeInTheDocument();
  });

  it('shows active goals', () => {
    render(<LiveSession />);
    expect(screen.getByText('Reach Clean Code L2')).toBeInTheDocument();
  });

  it('shows level buttons for 2-tap validation', () => {
    render(<LiveSession />);
    // Should show level buttons above current niveau for Clean Code (current=1, max=3)
    expect(screen.getAllByText('2').length).toBeGreaterThan(0);
    expect(screen.getAllByText('3').length).toBeGreaterThan(0);
  });

  it('calls validate mutation when level button clicked', () => {
    render(<LiveSession />);
    const levelTwoBtns = screen.getAllByText('2');
    fireEvent.click(levelTwoBtns[0]);
    expect(mockMutate).toHaveBeenCalled();
  });

  it('shows session notes textarea', () => {
    render(<LiveSession />);
    expect(screen.getByPlaceholderText('session.sessionNotesPlaceholder')).toBeInTheDocument();
  });

  it('shows End Session button', () => {
    render(<LiveSession />);
    expect(screen.getByText('session.endSession')).toBeInTheDocument();
  });

  it('calls endSession and navigates back on End Session click', async () => {
    mockUseMutation.mockImplementation(({ onSuccess }: { onSuccess: () => void }) => ({
      mutate: () => {
        onSuccess();
      },
      isPending: false,
    }));
    render(<LiveSession />);
    fireEvent.click(screen.getByText('session.endSession'));
    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith({
        to: '/team/consultants/$consultantId',
        params: { consultantId: 'c1' },
      });
    });
  });

  it('shows Add SMART Goal section', () => {
    render(<LiveSession />);
    expect(screen.getByText('session.addGoal')).toBeInTheDocument();
  });

  it('creates a goal when form submitted', () => {
    render(<LiveSession />);
    fireEvent.click(screen.getByText('session.addGoal'));
    const titleInput = screen.getByPlaceholderText('session.goalTitle');
    fireEvent.change(titleInput, { target: { value: 'New Goal' } });
    fireEvent.click(screen.getByText('session.createGoal'));
    expect(mockMutate).toHaveBeenCalled();
  });
});
