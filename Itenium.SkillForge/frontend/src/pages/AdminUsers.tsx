import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Search, UserPlus, Archive, RotateCcw, Users } from 'lucide-react';
import {
  Button,
  Input,
  Label,
  Badge,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetFooter,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@itenium-forge/ui';
import {
  fetchUsers,
  fetchUnassignedUsers,
  createUser,
  archiveUser,
  restoreUser,
  type CreateUserRequest,
  type UserResponse,
} from '@/api/client';
import { useTeamStore } from '@/stores';

const ROLES = ['learner', 'manager', 'backoffice'] as const;

const emptyForm: CreateUserRequest = {
  email: '',
  firstName: '',
  lastName: '',
  role: 'learner',
  password: '',
  teams: null,
};

export function AdminUsers() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { teams } = useTeamStore();

  const [tab, setTab] = useState<'all' | 'unassigned'>('all');
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('all');
  const [includeArchived, setIncludeArchived] = useState(false);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [form, setForm] = useState<CreateUserRequest>(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);

  const { data: users = [], isLoading } = useQuery({
    queryKey: ['users', includeArchived],
    queryFn: () => fetchUsers(includeArchived),
    enabled: tab === 'all',
  });

  const { data: unassigned = [], isLoading: isLoadingUnassigned } = useQuery({
    queryKey: ['users', 'unassigned'],
    queryFn: fetchUnassignedUsers,
    enabled: tab === 'unassigned',
  });

  const createMutation = useMutation({
    mutationFn: createUser,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setSheetOpen(false);
      setForm(emptyForm);
      setFormError(null);
    },
    onError: () => setFormError(t('users.createError')),
  });

  const archiveMutation = useMutation({
    mutationFn: archiveUser,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }),
  });

  const restoreMutation = useMutation({
    mutationFn: restoreUser,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }),
  });

  const activeList = tab === 'all' ? users : unassigned;

  const filtered = useMemo(() => {
    const q = search.toLowerCase();
    return activeList.filter((u: UserResponse) => {
      const name = `${u.firstName ?? ''} ${u.lastName ?? ''}`.toLowerCase();
      const matchSearch = !q || name.includes(q) || u.email.toLowerCase().includes(q);
      const matchRole = roleFilter === 'all' || u.role === roleFilter;
      return matchSearch && matchRole;
    });
  }, [activeList, search, roleFilter]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    createMutation.mutate(form);
  };

  const roleBadgeVariant = (role: string) => {
    if (role === 'backoffice') return 'default' as const;
    if (role === 'manager') return 'secondary' as const;
    return 'outline' as const;
  };

  const loading = tab === 'all' ? isLoading : isLoadingUnassigned;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('users.title')}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t('users.subtitle')}</p>
        </div>
        <Button onClick={() => setSheetOpen(true)}>
          <UserPlus className="size-4 mr-2" />
          {t('users.createUser')}
        </Button>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 border-b">
        <button
          type="button"
          onClick={() => setTab('all')}
          className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
            tab === 'all'
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          {t('users.allUsers')}
        </button>
        <button
          type="button"
          onClick={() => setTab('unassigned')}
          className={`flex items-center gap-1.5 px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
            tab === 'unassigned'
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Users className="size-3.5" />
          {t('users.unassigned')}
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative w-64">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
          <Input
            placeholder={t('users.searchPlaceholder')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>

        <Select value={roleFilter} onValueChange={setRoleFilter}>
          <SelectTrigger className="w-36">
            <SelectValue placeholder={t('users.allRoles')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('users.allRoles')}</SelectItem>
            {ROLES.map((r) => (
              <SelectItem key={r} value={r}>
                {t(`users.roles.${r}`)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {tab === 'all' && (
          <label className="flex items-center gap-2 text-sm text-muted-foreground cursor-pointer">
            <input
              type="checkbox"
              checked={includeArchived}
              onChange={(e) => setIncludeArchived(e.target.checked)}
              className="rounded"
            />
            {t('users.showArchived')}
          </label>
        )}
      </div>

      {/* Table */}
      {loading ? (
        <div className="text-muted-foreground">{t('common.loading')}</div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-20 text-center">
          <Users className="size-10 text-muted-foreground/50 mb-3" />
          <p className="text-lg font-medium">{t('users.noUsers')}</p>
        </div>
      ) : (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('users.name')}</TableHead>
                <TableHead>{t('users.email')}</TableHead>
                <TableHead>{t('users.role')}</TableHead>
                <TableHead>{t('users.teams')}</TableHead>
                <TableHead>{t('users.status')}</TableHead>
                <TableHead className="text-right">{t('common.edit')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((user: UserResponse) => (
                <TableRow key={user.id} className={user.isArchived ? 'opacity-50' : ''}>
                  <TableCell className="font-medium">
                    {user.firstName} {user.lastName}
                  </TableCell>
                  <TableCell className="text-muted-foreground">{user.email}</TableCell>
                  <TableCell>
                    <Badge variant={roleBadgeVariant(user.role)}>{t(`users.roles.${user.role}`)}</Badge>
                  </TableCell>
                  <TableCell className="text-muted-foreground text-sm">
                    {user.teams.length === 0 ? '—' : user.teams.join(', ')}
                  </TableCell>
                  <TableCell>
                    {user.isArchived ? (
                      <Badge variant="destructive">{t('users.archived')}</Badge>
                    ) : (
                      <Badge variant="outline">{t('common.active')}</Badge>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    {user.isArchived ? (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => restoreMutation.mutate(user.id)}
                        disabled={restoreMutation.isPending}
                      >
                        <RotateCcw className="size-4 mr-1" />
                        {t('users.restore')}
                      </Button>
                    ) : (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => archiveMutation.mutate(user.id)}
                        disabled={archiveMutation.isPending}
                      >
                        <Archive className="size-4 mr-1" />
                        {t('users.archive')}
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Create User Sheet */}
      <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
        <SheetContent side="right" className="w-[400px] sm:w-[480px]">
          <SheetHeader>
            <SheetTitle>{t('users.createUser')}</SheetTitle>
          </SheetHeader>

          <form onSubmit={handleSubmit} className="mt-6 space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="firstName">{t('users.firstName')}</Label>
                <Input
                  id="firstName"
                  value={form.firstName}
                  onChange={(e) => setForm((f: CreateUserRequest) => ({ ...f, firstName: e.target.value }))}
                  required
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="lastName">{t('users.lastName')}</Label>
                <Input
                  id="lastName"
                  value={form.lastName}
                  onChange={(e) => setForm((f: CreateUserRequest) => ({ ...f, lastName: e.target.value }))}
                  required
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="email">{t('users.email')}</Label>
              <Input
                id="email"
                type="email"
                value={form.email}
                onChange={(e) => setForm((f: CreateUserRequest) => ({ ...f, email: e.target.value }))}
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="password">{t('auth.password')}</Label>
              <Input
                id="password"
                type="password"
                value={form.password}
                onChange={(e) => setForm((f: CreateUserRequest) => ({ ...f, password: e.target.value }))}
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label>{t('users.role')}</Label>
              <Select value={form.role} onValueChange={(v) => setForm((f: CreateUserRequest) => ({ ...f, role: v }))}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {ROLES.map((r) => (
                    <SelectItem key={r} value={r}>
                      {t(`users.roles.${r}`)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {teams.length > 0 && (
              <div className="space-y-1.5">
                <Label>{t('users.teams')}</Label>
                <div className="flex flex-wrap gap-2">
                  {teams.map((team) => {
                    const selected = (form.teams ?? []).includes(team.id);
                    return (
                      <button
                        key={team.id}
                        type="button"
                        onClick={() =>
                          setForm((f: CreateUserRequest) => ({
                            ...f,
                            teams: selected
                              ? (f.teams ?? []).filter((id: number) => id !== team.id)
                              : [...(f.teams ?? []), team.id],
                          }))
                        }
                        className={`rounded-full px-3 py-1 text-sm font-medium transition-colors ${
                          selected
                            ? 'bg-primary text-primary-foreground'
                            : 'bg-muted text-muted-foreground hover:bg-accent'
                        }`}
                      >
                        {team.name}
                      </button>
                    );
                  })}
                </div>
              </div>
            )}

            {formError && <p className="text-sm text-destructive">{formError}</p>}

            <SheetFooter className="pt-4">
              <Button type="button" variant="outline" onClick={() => setSheetOpen(false)}>
                {t('common.cancel')}
              </Button>
              <Button type="submit" disabled={createMutation.isPending}>
                {createMutation.isPending ? t('common.loading') : t('users.createUser')}
              </Button>
            </SheetFooter>
          </form>
        </SheetContent>
      </Sheet>
    </div>
  );
}
