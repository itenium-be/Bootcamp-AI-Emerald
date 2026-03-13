import { useTranslation } from 'react-i18next';
import { FlaskConical } from 'lucide-react';

export function TestReport() {
  const { t } = useTranslation();

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="rounded-lg bg-gradient-to-br from-pink-500 to-rose-400 p-2">
          <FlaskConical className="size-5 text-white" />
        </div>
        <div>
          <h1 className="text-3xl font-bold">{t('nav.testReport')}</h1>
          <p className="text-muted-foreground">{t('reports.testDescription')}</p>
        </div>
      </div>

      <div className="rounded-lg border border-dashed p-12 flex flex-col items-center justify-center text-center gap-3 text-muted-foreground">
        <FlaskConical className="size-10 opacity-30" />
        <p className="text-sm">Report content goes here.</p>
      </div>
    </div>
  );
}
