import { createFileRoute, redirect, Outlet } from '@tanstack/react-router';
import { useAuthStore } from '@/stores';

export const Route = createFileRoute('/_authenticated/_admin')({
  component: () => <Outlet />,
  beforeLoad: () => {
    const { user } = useAuthStore.getState();
    if (!user?.isBackOffice) {
      throw redirect({ to: '/' });
    }
  },
});
