import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Políticas de Cambio y Devoluciones — TukiFact',
  description:
    'Conoce las políticas de cambio de plan, cancelación y reembolsos de TukiFact, plataforma SaaS de facturación electrónica operada por Tukituki Solution S.A.C.',
};

const LAST_UPDATED = '2 de junio de 2026';

export default function DevolucionesPage() {
  return (
    <div className="min-h-screen bg-background">
      <header className="border-b">
        <div className="container mx-auto max-w-4xl px-6 py-6">
          <Link href="/" className="text-sm text-muted-foreground hover:text-foreground">
            ← Volver a TukiFact
          </Link>
        </div>
      </header>

      <main className="container mx-auto max-w-4xl px-6 py-12">
        <article className="prose prose-neutral dark:prose-invert max-w-none">
          <h1>Políticas de Cambio y Devoluciones</h1>
          <p className="text-muted-foreground">Última actualización: {LAST_UPDATED}</p>

          <p>
            TukiFact es un servicio SaaS (Software como Servicio) operado por{' '}
            <strong>Tukituki Solution S.A.C.</strong> (RUC 20613614509). Al ser un servicio digital
            de suscripción, no existen bienes físicos que devolver; por ello, la presente política
            regula los cambios de plan, cancelaciones y reembolsos aplicables.
          </p>

          <h2>1. Cambio de plan</h2>
          <p>
            Puedes cambiar tu plan en cualquier momento desde el panel de administración de tu cuenta:
          </p>
          <ul>
            <li>
              <strong>Upgrade (plan superior):</strong> el cambio es inmediato. Se aplicará un
              prorrateo del período restante del plan actual y se cargará la diferencia al medio de
              pago registrado.
            </li>
            <li>
              <strong>Downgrade (plan inferior):</strong> el cambio se aplica al inicio del siguiente
              período de facturación. Los documentos emitidos durante el período actual no se ven
              afectados.
            </li>
            <li>
              <strong>Plan gratuito / período de prueba:</strong> si estás en período de prueba, el
              cambio a un plan pago es inmediato y el período de prueba se da por concluido.
            </li>
          </ul>

          <h2>2. Cancelación de la suscripción</h2>
          <p>
            Puedes cancelar tu suscripción en cualquier momento desde{' '}
            <strong>Configuración › Plan y facturación</strong> en tu panel.
          </p>
          <ul>
            <li>
              La cancelación tiene efecto al término del período de facturación vigente; no se emiten
              cargos adicionales.
            </li>
            <li>
              Conservarás el acceso a la plataforma y a todos tus comprobantes hasta el fin del
              período pagado.
            </li>
            <li>
              Tras la cancelación, tus datos (comprobantes, clientes, series) se conservan por{' '}
              <strong>90 días calendario</strong> para permitirte reactivar o exportar tu información.
              Pasado ese plazo, se procede a la anonimización según lo exige la normativa tributaria
              peruana.
            </li>
            <li>
              Los documentos emitidos durante la suscripción permanecen válidos ante SUNAT con
              independencia del estado de tu cuenta.
            </li>
          </ul>

          <h2>3. Política de reembolsos</h2>
          <p>
            Dado que TukiFact otorga acceso inmediato al servicio desde el momento del pago, como
            regla general <strong>los pagos no son reembolsables</strong>. No obstante, se reconocen
            las siguientes excepciones:
          </p>

          <h3>3.1 Garantía de satisfacción (primeros 7 días)</h3>
          <p>
            Si eres un cliente nuevo y contratas un plan por primera vez, tienes{' '}
            <strong>7 días calendario</strong> desde la fecha del primer cargo para solicitar el
            reembolso completo si el servicio no satisface tus necesidades, sin necesidad de
            justificación. Esta garantía aplica una sola vez por RUC.
          </p>

          <h3>3.2 Falla imputable a TukiFact</h3>
          <p>
            Si el servicio presenta una interrupción o falla técnica atribuible a TukiFact que impida
            de manera sustancial la emisión de comprobantes durante <strong>más de 48 horas</strong>{' '}
            continuas en un mismo mes de facturación, tendrás derecho a un crédito o reembolso
            proporcional al tiempo de indisponibilidad, calculado sobre el valor mensual del plan.
            Deberás notificarlo a{' '}
            <a href="mailto:soporte@tukifact.com.pe">soporte@tukifact.com.pe</a> dentro de los{' '}
            <strong>7 días siguientes</strong> al incidente.
          </p>

          <h3>3.3 Cobro duplicado o error de facturación</h3>
          <p>
            Si identificas un cargo duplicado o un error de facturación imputable a TukiFact,
            reembolsaremos el monto incorrecto dentro de <strong>10 días hábiles</strong> desde la
            confirmación del error, usando el mismo medio de pago original.
          </p>

          <h3>3.4 Casos no reembolsables</h3>
          <ul>
            <li>Períodos ya consumidos más allá de los 7 días de garantía inicial.</li>
            <li>
              Interrupciones de SUNAT, del servicio SOL o de terceros ajenos a TukiFact (NATS,
              proveedores de certificados digitales, etc.).
            </li>
            <li>Incumplimiento de los requisitos mínimos de uso (RUC inactivo, certificado vencido).</li>
            <li>Uso excesivo, abuso o violación de los Términos del Servicio.</li>
            <li>Cambios de plan realizados por el propio usuario.</li>
          </ul>

          <h2>4. Cómo solicitar un reembolso</h2>
          <ol>
            <li>
              Envía un correo a <a href="mailto:soporte@tukifact.com.pe">soporte@tukifact.com.pe</a>{' '}
              con el asunto: <em>"Solicitud de reembolso — [tu RUC]"</em>.
            </li>
            <li>Indica el motivo de la solicitud y adjunta el comprobante del cargo.</li>
            <li>
              Nuestro equipo evaluará tu caso y te responderá dentro de <strong>3 días hábiles</strong>.
            </li>
            <li>
              Si el reembolso procede, se procesará dentro de <strong>10 días hábiles</strong>. El
              tiempo de acreditación depende de tu entidad bancaria o pasarela de pago.
            </li>
          </ol>

          <h2>5. Exportación de datos antes de cancelar</h2>
          <p>
            Antes de cancelar tu suscripción, te recomendamos descargar tus comprobantes (PDF y XML)
            y exportar tus datos desde <strong>Reportes</strong> en el panel. TukiFact no se
            responsabiliza por datos no exportados una vez vencido el período de retención de 90 días.
          </p>

          <h2>6. Normativa aplicable</h2>
          <p>
            La presente política se rige por el{' '}
            <strong>Código de Protección y Defensa del Consumidor (Ley 29571)</strong>, las normas
            emitidas por INDECOPI y demás legislación peruana aplicable. En caso de controversia,
            puedes acudir a INDECOPI o a la jurisdicción ordinaria de Lima Cercado.
          </p>

          <h2>7. Contacto</h2>
          <p>
            Tukituki Solution S.A.C. — RUC 20613614509
            <br />
            Correo: <a href="mailto:soporte@tukifact.com.pe">soporte@tukifact.com.pe</a>
            <br />
            Sitio: <a href="https://tukifact.com.pe">https://tukifact.com.pe</a>
          </p>
        </article>
      </main>

      <footer className="border-t mt-12">
        <div className="container mx-auto max-w-4xl px-6 py-6 text-sm text-muted-foreground flex flex-wrap gap-4">
          <Link href="/terms" className="hover:text-foreground">Términos del Servicio</Link>
          <Link href="/privacy" className="hover:text-foreground">Política de Privacidad</Link>
          <Link href="/devoluciones" className="hover:text-foreground">Devoluciones</Link>
          <Link href="/reclamaciones" className="hover:text-foreground">Libro de Reclamaciones</Link>
          <span className="ml-auto">© {new Date().getFullYear()} Tukituki Solution S.A.C.</span>
        </div>
      </footer>
    </div>
  );
}
