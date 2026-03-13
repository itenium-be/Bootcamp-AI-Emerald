import { createFileRoute } from '@tanstack/react-router';
import { ConsultantProfile } from '@/pages/ConsultantProfile';

export const Route = createFileRoute('/_authenticated/team/consultants/$consultantId')({
  component: ConsultantProfile,
});
