import { createFileRoute } from '@tanstack/react-router';
import { Goals } from '@/pages/Goals';

export const Route = createFileRoute('/_authenticated/goals')({
  component: Goals,
});
