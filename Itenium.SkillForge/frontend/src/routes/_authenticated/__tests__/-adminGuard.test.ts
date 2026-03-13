import { vi, describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '@/stores/authStore';

const mockRedirect = vi.fn();

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    redirect: (opts: unknown) => {
      mockRedirect(opts);
      const err = new Error('redirect') as Error & { _isRedirect: boolean };
      err._isRedirect = true;
      return err;
    },
  };
});

function setUser(isBackOffice: boolean) {
  useAuthStore.setState({
    accessToken: 'tok',
    isAuthenticated: true,
    user: { id: '1', email: 'x@x.com', name: 'X', roles: isBackOffice ? ['backoffice'] : [], isBackOffice },
  });
}

beforeEach(() => {
  mockRedirect.mockClear();
  useAuthStore.setState({ accessToken: null, user: null, isAuthenticated: false });
});

describe('_admin route guard', () => {
  it('allows backoffice users through', async () => {
    setUser(true);
    const { Route } = await import('../_admin/route');
    expect(() => Route.options.beforeLoad?.({ location: {} } as never)).not.toThrow();
  });

  it('redirects non-backoffice users to /', async () => {
    setUser(false);
    const { Route } = await import('../_admin/route');
    try {
      Route.options.beforeLoad?.({ location: {} } as never);
    } catch {
      expect(mockRedirect).toHaveBeenCalledWith({ to: '/' });
      return;
    }
    throw new Error('expected redirect to be thrown');
  });
});
