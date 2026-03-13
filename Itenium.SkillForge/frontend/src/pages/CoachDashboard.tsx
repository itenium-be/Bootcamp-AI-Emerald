import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { AlertTriangle, Clock, Flag, Users } from 'lucide-react';
import { Badge, Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@itenium-forge/ui';
import { fetchTeamDashboard, type ConsultantSummary } from '@/api/client';

const INACTIVE_THRESHOLD = 21;

function FlagBadge({ ageInDays, skillName }: { ageInDays: number; skillName: string }) {
  const { t } = useTranslation();
  return (
    <Badge variant="outline" title={skillName}>
      <Flag className="size-3 mr-0.5" />
      {t('coach.flagAge', { days: ageInDays })}
    </Badge>
  );
}

function ConsultantRow({ consultant, onClick }: { consultant: ConsultantSummary; onClick: () => void }) {
  const { t } = useTranslation();
  const isInactive = (consultant.daysSinceActivity ?? 0) > INACTIVE_THRESHOLD;
  const sortedFlags = [...consultant.readinessFlags].sort((a, b) => b.ageInDays - a.ageInDays);

  return (
    <TableRow
      onClick={onClick}
      className={`cursor-pointer hover:bg-accent ${isInactive ? 'bg-amber-50 dark:bg-amber-900/10 border-l-2 border-l-amber-500' : ''}`}
    >
      <TableCell className="font-medium">
        <div className="flex items-center gap-2">
          {isInactive && <AlertTriangle className="size-4 text-amber-500 shrink-0" />}
          {consultant.name}
        </div>
      </TableCell>
      <TableCell>{consultant.activeGoalCount}</TableCell>
      <TableCell>
        <div className="flex flex-wrap gap-1">
          {sortedFlags.map((flag, i) => (
            <FlagBadge key={i} ageInDays={flag.ageInDays} skillName={flag.skillName} />
          ))}
        </div>
      </TableCell>
      <TableCell>
        {consultant.daysSinceActivity !== null ? (
          <div className="flex items-center gap-1.5 text-sm">
            <Clock className="size-3.5 text-muted-foreground" />
            <span className={isInactive ? 'text-amber-600 font-medium' : 'text-muted-foreground'}>
              {t('coach.inactiveDays', { days: consultant.daysSinceActivity })}
            </span>
          </div>
        ) : (
          <span className="text-muted-foreground text-sm">—</span>
        )}
      </TableCell>
    </TableRow>
  );
}

export function CoachDashboard() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const { data: consultants, isLoading } = useQuery({
    queryKey: ['coach', 'team'],
    queryFn: fetchTeamDashboard,
  });

  if (isLoading) {
    return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('coach.dashboard')}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t('coach.dashboardSubtitle')}</p>
      </div>

      {consultants && consultants.length > 0 ? (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t('coach.name')}</TableHead>
              <TableHead>{t('coach.activeGoals')}</TableHead>
              <TableHead>{t('coach.readinessFlags')}</TableHead>
              <TableHead>{t('coach.lastActivity')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {consultants.map((c) => (
              <ConsultantRow
                key={c.id}
                consultant={c}
                onClick={() => navigate({ to: '/team/consultants/$consultantId', params: { consultantId: c.id } })}
              />
            ))}
          </TableBody>
        </Table>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-20 text-center">
          <Users className="size-10 text-muted-foreground/50 mb-3" />
          <p className="text-lg font-medium">{t('coach.noConsultants')}</p>
        </div>
      )}
    </div>
  );
}
