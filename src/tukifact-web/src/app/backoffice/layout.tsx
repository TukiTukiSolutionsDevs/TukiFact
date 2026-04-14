import { BackofficeAuthProvider } from '@/lib/backoffice-auth-context';

export const metadata = {
  title: 'TukiFact Backoffice',
  description: 'Panel de administración de la plataforma TukiFact',
};

export default function BackofficeRootLayout({ children }: { children: React.ReactNode }) {
  return <BackofficeAuthProvider>{children}</BackofficeAuthProvider>;
}
