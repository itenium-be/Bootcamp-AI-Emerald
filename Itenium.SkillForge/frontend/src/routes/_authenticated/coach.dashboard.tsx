import { createFileRoute } from '@tanstack/react-router';
import { CoachDashboard } from '@/pages/CoachDashboard';

export const Route = createFileRoute('/_authenticated/coach/dashboard')({ component: CoachDashboard });
