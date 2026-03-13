import { createFileRoute } from '@tanstack/react-router';
import { TestReport } from '@/pages/reports/Test';

export const Route = createFileRoute('/_authenticated/reports/test')({
  component: TestReport,
});
