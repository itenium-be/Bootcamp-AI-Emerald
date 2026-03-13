import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';
import { Settings } from '../Settings';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: {
      language: 'en',
      changeLanguage: vi.fn(),
    },
  }),
}));

vi.mock('lucide-react', () => {
  const I = ({ className }: { className?: string }) => <span className={className} />;
  return {
    Sun: I,
    Moon: I,
    Monitor: I,
    Globe: I,
    User: I,
    Mail: I,
    Shield: I,
    Cpu: I,
  };
});

const mockSetTheme = vi.fn();
vi.mock('@/stores', () => ({
  useAuthStore: vi.fn(() => ({
    user: { name: 'Ada Lovelace', email: 'ada@itenium.be', roles: ['learner', 'manager'] },
  })),
  useThemeStore: vi.fn(() => ({
    theme: 'system',
    setTheme: mockSetTheme,
  })),
}));

describe('Settings', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the page title', () => {
    render(<Settings />);
    expect(screen.getByText('settings.title')).toBeInTheDocument();
  });

  it('shows the profile section heading', () => {
    render(<Settings />);
    expect(screen.getByText('settings.profile')).toBeInTheDocument();
  });

  it('displays the user name', () => {
    render(<Settings />);
    expect(screen.getAllByText('Ada Lovelace').length).toBeGreaterThan(0);
  });

  it('displays the user email', () => {
    render(<Settings />);
    expect(screen.getByText('ada@itenium.be')).toBeInTheDocument();
  });

  it('displays user roles as badges', () => {
    render(<Settings />);
    expect(screen.getByText('learner')).toBeInTheDocument();
    expect(screen.getByText('manager')).toBeInTheDocument();
  });

  it('shows the appearance section heading', () => {
    render(<Settings />);
    expect(screen.getByText('settings.appearance')).toBeInTheDocument();
  });

  it('shows all three theme options', () => {
    render(<Settings />);
    expect(screen.getByText('settings.light')).toBeInTheDocument();
    expect(screen.getByText('settings.dark')).toBeInTheDocument();
    expect(screen.getByText('settings.system')).toBeInTheDocument();
  });

  it('calls setTheme when a theme button is clicked', () => {
    render(<Settings />);
    fireEvent.click(screen.getByText('settings.light'));
    expect(mockSetTheme).toHaveBeenCalledWith('light');
  });

  it('shows the language section heading', () => {
    render(<Settings />);
    expect(screen.getByText('settings.language')).toBeInTheDocument();
  });

  it('shows EN and NL language options', () => {
    render(<Settings />);
    expect(screen.getByText('English')).toBeInTheDocument();
    expect(screen.getByText('Nederlands')).toBeInTheDocument();
  });

  it('shows the meme section', () => {
    render(<Settings />);
    expect(screen.getByText('settings.memeTitle')).toBeInTheDocument();
  });
});
