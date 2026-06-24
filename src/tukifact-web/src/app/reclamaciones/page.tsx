import type { Metadata } from 'next';
import Link from 'next/link';
import { ReclamacionesForm } from './ReclamacionesForm';

export const metadata: Metadata = {
  title: 'Libro de Reclamaciones — TukiFact',
  description:
    'Libro de Reclamaciones Virtual de TukiFact conforme al Código de Protección y Defensa del Consumidor (Ley 29571). Registra tu queja o reclamo.',
};

export default function ReclamacionesPage() {
  return (
    <div className="min-h-screen bg-background">
      <header className="border-b">
        <div className="container mx-auto max-w-4xl px-6 py-6">
          <Link href="/" className="text-sm text-muted-foreground hover:text-foreground">
            ← Volver a TukiFact
          </Link>
        </div>
      </header>

      <main className="container mx-auto max-w-4xl px-6 py-12 space-y-10">
        {/* Encabezado legal */}
        <div className="rounded-lg border border-amber-200 bg-amber-50 px-6 py-5">
          <div className="flex items-start gap-3">
            <span className="text-2xl">📋</span>
            <div>
              <h1 className="text-xl font-bold text-amber-900">Libro de Reclamaciones Virtual</h1>
              <p className="mt-1 text-sm text-amber-800">
                Conforme al Art. 150° del Código de Protección y Defensa del Consumidor —{' '}
                <strong>Ley N.° 29571</strong> y su Reglamento (D.S. 011-2011-PCM).
              </p>
            </div>
          </div>
        </div>

        {/* Información del proveedor */}
        <section className="prose prose-neutral dark:prose-invert max-w-none">
          <h2 className="text-lg font-semibold">Datos del proveedor</h2>
          <div className="not-prose overflow-hidden rounded-lg border">
            <table className="w-full text-sm">
              <tbody>
                <tr className="border-b">
                  <td className="w-40 bg-muted/40 px-4 py-3 font-medium">Razón social</td>
                  <td className="px-4 py-3">Tukituki Solution S.A.C.</td>
                </tr>
                <tr className="border-b">
                  <td className="bg-muted/40 px-4 py-3 font-medium">RUC</td>
                  <td className="px-4 py-3">20613614509</td>
                </tr>
                <tr className="border-b">
                  <td className="bg-muted/40 px-4 py-3 font-medium">Actividad</td>
                  <td className="px-4 py-3">Plataforma SaaS de facturación electrónica (tukifact.com.pe)</td>
                </tr>
                <tr>
                  <td className="bg-muted/40 px-4 py-3 font-medium">Contacto</td>
                  <td className="px-4 py-3">
                    <a href="mailto:soporte@tukifact.com.pe" className="text-primary hover:underline">
                      soporte@tukifact.com.pe
                    </a>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        {/* Diferencia queja vs reclamo */}
        <section className="rounded-lg border bg-muted/20 px-6 py-5">
          <h2 className="font-semibold mb-3">¿Queja o reclamo?</h2>
          <div className="grid gap-4 sm:grid-cols-2 text-sm">
            <div>
              <p className="font-medium text-foreground">📌 Reclamo</p>
              <p className="text-muted-foreground mt-1">
                Disconformidad relacionada con el servicio o producto contratado (ej.: cobro incorrecto, falla
                técnica que afectó la emisión de comprobantes, etc.).
              </p>
            </div>
            <div>
              <p className="font-medium text-foreground">💬 Queja</p>
              <p className="text-muted-foreground mt-1">
                Malestar o descontento respecto a la atención o al proceso de compra, sin que implique una
                afectación directa al servicio contratado.
              </p>
            </div>
          </div>
          <p className="mt-4 text-xs text-muted-foreground">
            La formulación de un reclamo no impide acudir a otras vías de solución de controversias ni es
            requisito previo para interponer una denuncia ante el INDECOPI.
          </p>
        </section>

        {/* Formulario */}
        <section>
          <h2 className="text-lg font-semibold mb-6">Registrar queja o reclamo</h2>
          <ReclamacionesForm />
        </section>

        {/* Compromisos */}
        <section className="prose prose-neutral dark:prose-invert max-w-none text-sm">
          <h2>Nuestros compromisos</h2>
          <ul>
            <li>Respondemos tu reclamo dentro de <strong>30 días hábiles</strong> contados desde la fecha de registro.</li>
            <li>
              Si lo requieres, puedes también comunicarte directamente con{' '}
              <strong>INDECOPI</strong> (0800-4-4040, llamada gratuita) o a través de{' '}
              <a href="https://www.indecopi.gob.pe" target="_blank" rel="noopener noreferrer">
                www.indecopi.gob.pe
              </a>.
            </li>
            <li>
              Para reembolsos o devoluciones, consulta nuestra{' '}
              <Link href="/devoluciones">Política de Devoluciones y Reembolsos</Link>.
            </li>
          </ul>
        </section>
      </main>

      <footer className="border-t mt-12">
        <div className="container mx-auto max-w-4xl px-6 py-6 text-sm text-muted-foreground flex flex-wrap gap-4">
          <Link href="/privacy" className="hover:text-foreground">Política de Privacidad</Link>
          <Link href="/terms" className="hover:text-foreground">Términos del Servicio</Link>
          <Link href="/devoluciones" className="hover:text-foreground">Devoluciones</Link>
          <Link href="/reclamaciones" className="hover:text-foreground">Libro de Reclamaciones</Link>
          <span className="ml-auto">© {new Date().getFullYear()} Tukituki Solution S.A.C.</span>
        </div>
      </footer>
    </div>
  );
}
