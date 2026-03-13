import { createFileRoute } from '@tanstack/react-router';
import { AdminUsers } from '@/pages/AdminUsers';

export const Route = createFileRoute('/_authenticated/_admin/users')({
  component: AdminUsers,
});
