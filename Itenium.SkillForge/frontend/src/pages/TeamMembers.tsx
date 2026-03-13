import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Search, Users } from 'lucide-react';
import { Badge, Input, Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@itenium-forge/ui';
import { fetchTeamMembers, type TeamMemberResponse } from '@/api/client';

export function TeamMembers() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');

  const { data: members = [], isLoading } = useQuery({
    queryKey: ['team-members'],
    queryFn: fetchTeamMembers,
  });

  const filtered = useMemo(() => {
    const q = search.toLowerCase();
    return members.filter((m: TeamMemberResponse) => {
      const name = `${m.firstName ?? ''} ${m.lastName ?? ''}`.toLowerCase();
      return !q || name.includes(q) || m.email.toLowerCase().includes(q);
    });
  }, [members, search]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('members.title')}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t('members.subtitle')}</p>
      </div>

      <div className="relative w-64">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
        <Input
          placeholder={t('members.searchPlaceholder')}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-9"
        />
      </div>

      {isLoading ? (
        <div className="text-muted-foreground">{t('common.loading')}</div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-20 text-center">
          <Users className="size-10 text-muted-foreground/50 mb-3" />
          <p className="text-lg font-medium">{t('members.noMembers')}</p>
        </div>
      ) : (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('members.name')}</TableHead>
                <TableHead>{t('members.email')}</TableHead>
                <TableHead>{t('members.profile')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((member: TeamMemberResponse) => (
                <TableRow key={member.id}>
                  <TableCell className="font-medium">
                    {member.firstName || member.lastName
                      ? `${member.firstName ?? ''} ${member.lastName ?? ''}`.trim()
                      : '—'}
                  </TableCell>
                  <TableCell className="text-muted-foreground">{member.email || '—'}</TableCell>
                  <TableCell>
                    {member.profileName ? (
                      <Badge variant="secondary">{member.profileName}</Badge>
                    ) : (
                      <span className="text-sm text-muted-foreground">{t('members.noProfile')}</span>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}
