import { createFileRoute } from '@tanstack/react-router';
import { SkillDetail } from '@/pages/SkillDetail';

export const Route = createFileRoute('/_authenticated/skills/$skillId')({
  component: SkillDetail,
});
