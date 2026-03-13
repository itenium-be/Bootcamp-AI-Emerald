import { useTranslation } from 'react-i18next';
import { Mail, Phone, MapPin, Send } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent, Button, Input, Label } from '@itenium-forge/ui';

export function ContactUs() {
  const { t } = useTranslation();

  return (
    <div className="space-y-6 max-w-2xl">
      <div>
        <h1 className="text-3xl font-bold">{t('contact.title')}</h1>
        <p className="text-muted-foreground mt-1">{t('contact.description')}</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <Card className="bg-blue-50 dark:bg-blue-950/40 ring-1 ring-blue-200 dark:ring-blue-800">
          <CardContent className="flex flex-col items-center gap-2 pt-6 pb-4 text-center">
            <div className="rounded-lg bg-gradient-to-br from-blue-500 to-cyan-400 p-2">
              <Mail className="size-5 text-white" />
            </div>
            <p className="text-sm font-medium">{t('contact.email')}</p>
            <p className="text-xs text-muted-foreground">info@itenium.be</p>
          </CardContent>
        </Card>

        <Card className="bg-violet-50 dark:bg-violet-950/40 ring-1 ring-violet-200 dark:ring-violet-800">
          <CardContent className="flex flex-col items-center gap-2 pt-6 pb-4 text-center">
            <div className="rounded-lg bg-gradient-to-br from-violet-500 to-purple-400 p-2">
              <Phone className="size-5 text-white" />
            </div>
            <p className="text-sm font-medium">{t('contact.phone')}</p>
            <p className="text-xs text-muted-foreground">+32 (0)2 123 45 67</p>
          </CardContent>
        </Card>

        <Card className="bg-emerald-50 dark:bg-emerald-950/40 ring-1 ring-emerald-200 dark:ring-emerald-800">
          <CardContent className="flex flex-col items-center gap-2 pt-6 pb-4 text-center">
            <div className="rounded-lg bg-gradient-to-br from-emerald-500 to-green-400 p-2">
              <MapPin className="size-5 text-white" />
            </div>
            <p className="text-sm font-medium">{t('contact.address')}</p>
            <p className="text-xs text-muted-foreground">Brussel, België</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('contact.sendMessage')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="name">{t('contact.name')}</Label>
              <Input id="name" placeholder={t('contact.namePlaceholder')} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="email">{t('contact.email')}</Label>
              <Input id="email" type="email" placeholder={t('contact.emailPlaceholder')} />
            </div>
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="subject">{t('contact.subject')}</Label>
            <Input id="subject" placeholder={t('contact.subjectPlaceholder')} />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="message">{t('contact.message')}</Label>
            <textarea
              id="message"
              rows={5}
              placeholder={t('contact.messagePlaceholder')}
              className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-none"
            />
          </div>
          <Button className="gap-2">
            <Send className="size-4" />
            {t('contact.send')}
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
