import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi, type Mock } from 'vitest';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { AdminUsers } from '../AdminUsers';
import type { UserResponse } from '@/api/client';
import { useTeamStore } from '@/stores';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
  useMutation: vi.fn(),
  useQueryClient: vi.fn(),
}));

vi.mock('@/api/client', () => ({
  fetchUsers: vi.fn(),
  fetchUnassignedUsers: vi.fn(),
  createUser: vi.fn(),
  archiveUser: vi.fn(),
  restoreUser: vi.fn(),
}));

vi.mock('@/stores', () => ({
  useTeamStore: vi.fn(),
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return { Search: I, UserPlus: I, Archive: I, RotateCcw: I, Users: I };
});

vi.mock('@itenium-forge/ui', () => ({
  Button: ({ children, onClick, disabled, type, variant }: React.ComponentProps<'button'> & { variant?: string }) => (
    <button onClick={onClick} disabled={disabled} type={type ?? 'button'} data-variant={variant}>
      {children}
    </button>
  ),
  Input: ({ placeholder, value, onChange, id, type, required }: React.ComponentProps<'input'>) => (
    <input placeholder={placeholder} value={value} onChange={onChange} id={id} type={type} required={required} />
  ),
  Label: ({ children, htmlFor }: React.ComponentProps<'label'>) => <label htmlFor={htmlFor}>{children}</label>,
  Badge: ({ children, variant }: { children: React.ReactNode; variant?: string }) => (
    <span data-variant={variant}>{children}</span>
  ),
  Table: ({ children }: { children: React.ReactNode }) => <table>{children}</table>,
  TableHeader: ({ children }: { children: React.ReactNode }) => <thead>{children}</thead>,
  TableBody: ({ children }: { children: React.ReactNode }) => <tbody>{children}</tbody>,
  TableHead: ({ children }: { children: React.ReactNode }) => <th>{children}</th>,
  TableRow: ({ children, className }: { children: React.ReactNode; className?: string }) => (
    <tr className={className}>{children}</tr>
  ),
  TableCell: ({ children, className }: { children: React.ReactNode; className?: string }) => (
    <td className={className}>{children}</td>
  ),
  Sheet: ({ children, open }: { children: React.ReactNode; open: boolean }) => <div data-open={open}>{children}</div>,
  SheetContent: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  SheetHeader: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  SheetTitle: ({ children }: { children: React.ReactNode }) => <h2>{children}</h2>,
  SheetFooter: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Select: ({
    children,
    value,
    onValueChange,
  }: {
    children: React.ReactNode;
    value: string;
    onValueChange: (v: string) => void;
  }) => (
    <div data-value={value} data-testid="select" onClick={() => onValueChange('manager')}>
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

const mockUsers: UserResponse[] = [
  {
    id: '1',
    email: 'alice@test.local',
    firstName: 'Alice',
    lastName: 'Smith',
    role: 'learner',
    teams: [],
    isArchived: false,
  },
  {
    id: '2',
    email: 'bob@test.local',
    firstName: 'Bob',
    lastName: 'Jones',
    role: 'manager',
    teams: [1],
    isArchived: false,
  },
  {
    id: '3',
    email: 'carol@test.local',
    firstName: 'Carol',
    lastName: 'Brown',
    role: 'learner',
    teams: [],
    isArchived: true,
  },
];

const mockUseQuery = useQuery as Mock;
const mockUseMutation = useMutation as Mock;
const mockUseQueryClient = useQueryClient as Mock;
const mockUseTeamStore = useTeamStore as unknown as Mock;

const mockMutate = vi.fn();
const mockInvalidate = vi.fn();

beforeEach(() => {
  vi.clearAllMocks();
  mockUseQueryClient.mockReturnValue({ invalidateQueries: mockInvalidate });
  mockUseMutation.mockReturnValue({ mutate: mockMutate, isPending: false });
  mockUseTeamStore.mockReturnValue({ teams: [] });
  mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
    if (queryKey.includes('unassigned')) return { data: [], isLoading: false };
    return { data: mockUsers, isLoading: false };
  });
});

describe('AdminUsers', () => {
  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<AdminUsers />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('renders the page title', () => {
    render(<AdminUsers />);
    expect(screen.getByText('users.title')).toBeInTheDocument();
  });

  it('renders a row for each user', () => {
    render(<AdminUsers />);
    expect(screen.getByText('alice@test.local')).toBeInTheDocument();
    expect(screen.getByText('bob@test.local')).toBeInTheDocument();
    expect(screen.getByText('carol@test.local')).toBeInTheDocument();
  });

  it('shows archived badge for archived users', () => {
    render(<AdminUsers />);
    expect(screen.getByText('users.archived')).toBeInTheDocument();
  });

  it('shows restore button for archived users and archive button for active users', () => {
    render(<AdminUsers />);
    expect(screen.getAllByText('users.archive').length).toBeGreaterThan(0);
    expect(screen.getByText('users.restore')).toBeInTheDocument();
  });

  it('filters users by search query', async () => {
    render(<AdminUsers />);
    const search = screen.getByPlaceholderText('users.searchPlaceholder');
    fireEvent.change(search, { target: { value: 'alice' } });
    await waitFor(() => {
      expect(screen.getByText('alice@test.local')).toBeInTheDocument();
      expect(screen.queryByText('bob@test.local')).not.toBeInTheDocument();
    });
  });

  it('shows empty state when no users match', async () => {
    render(<AdminUsers />);
    fireEvent.change(screen.getByPlaceholderText('users.searchPlaceholder'), {
      target: { value: 'xxxxxxxxx' },
    });
    await waitFor(() => {
      expect(screen.getByText('users.noUsers')).toBeInTheDocument();
    });
  });

  it('calls archiveUser mutation when archive button clicked', () => {
    render(<AdminUsers />);
    const archiveButtons = screen.getAllByText('users.archive');
    fireEvent.click(archiveButtons[0]);
    expect(mockMutate).toHaveBeenCalled();
  });

  it('calls restoreUser mutation when restore button clicked', () => {
    render(<AdminUsers />);
    fireEvent.click(screen.getByText('users.restore'));
    expect(mockMutate).toHaveBeenCalled();
  });

  it('switches to unassigned tab', async () => {
    render(<AdminUsers />);
    fireEvent.click(screen.getByText('users.unassigned'));
    await waitFor(() => {
      expect(mockUseQuery).toHaveBeenCalledWith(expect.objectContaining({ queryKey: ['users', 'unassigned'] }));
    });
  });

  it('shows "Show archived" checkbox only on all-users tab', () => {
    render(<AdminUsers />);
    expect(screen.getByText('users.showArchived')).toBeInTheDocument();
    fireEvent.click(screen.getByText('users.unassigned'));
    expect(screen.queryByText('users.showArchived')).not.toBeInTheDocument();
  });

  it('opens create user sheet when button clicked', () => {
    render(<AdminUsers />);
    const createBtn = screen.getAllByText('users.createUser')[0];
    fireEvent.click(createBtn);
    // Sheet should be open (data-open=true)
    expect(screen.getAllByText('users.createUser').length).toBeGreaterThan(1);
  });

  it('shows team toggles in create form when teams are available', () => {
    mockUseTeamStore.mockReturnValue({
      teams: [
        { id: 1, name: 'Alpha' },
        { id: 2, name: 'Beta' },
      ],
    });
    render(<AdminUsers />);
    fireEvent.click(screen.getAllByText('users.createUser')[0]);
    expect(screen.getByText('Alpha')).toBeInTheDocument();
    expect(screen.getByText('Beta')).toBeInTheDocument();
  });

  it('calls fetchUsers with includeArchived=false by default', () => {
    render(<AdminUsers />);
    expect(mockUseQuery).toHaveBeenCalledWith(expect.objectContaining({ queryKey: ['users', false] }));
  });

  it('passes includeArchived=true when checkbox is checked', async () => {
    render(<AdminUsers />);
    fireEvent.click(screen.getByRole('checkbox'));
    await waitFor(() => {
      expect(mockUseQuery).toHaveBeenCalledWith(expect.objectContaining({ queryKey: ['users', true] }));
    });
  });
});
