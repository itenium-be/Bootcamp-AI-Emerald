import { createFileRoute } from '@tanstack/react-router';
import { ConsultantActivity } from '@/pages/ConsultantActivity';

export const Route = createFileRoute('/_authenticated/members/$consultantId')({
  component: function Page() {
    const { consultantId } = Route.useParams();
    return <ConsultantActivity consultantId={Number(consultantId)} />;
  },
});
