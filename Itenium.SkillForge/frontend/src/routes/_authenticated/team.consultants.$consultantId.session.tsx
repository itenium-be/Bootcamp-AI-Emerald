import { createFileRoute } from '@tanstack/react-router';
import { LiveSession } from '@/pages/LiveSession';

export const Route = createFileRoute('/_authenticated/team/consultants/$consultantId/session')({
  component: LiveSession,
});
