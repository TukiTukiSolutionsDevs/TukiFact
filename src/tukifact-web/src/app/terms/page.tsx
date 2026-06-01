import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Términos del Servicio — TukiFact',
  description:
    'Términos y condiciones del servicio TukiFact, plataforma SaaS de facturación electrónica para Perú, operada por Tukituki Solution S.A.C.',
};

const LAST_UPDATED = '28 de mayo de 2026';

export default function TermsPage() {
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
          <h1>Términos del Servicio</h1>
          <p className="text-muted-foreground">Última actualización: {LAST_UPDATED}</p>

          <h2>1. Aceptación</h2>
          <p>
            Al crear una cuenta o usar TukiFact (en adelante, &quot;la Plataforma&quot;) aceptas estos Términos del
            Servicio (&quot;Términos&quot;) y nuestra{' '}
            <Link href="/privacy">Política de Privacidad</Link>. Si no estás de acuerdo, no uses la Plataforma.
          </p>

          <h2>2. Quiénes somos</h2>
          <p>
            La Plataforma es operada por <strong>Tukituki Solution S.A.C.</strong> (RUC 20613614509), una sociedad
            constituida en el Perú, con domicilio en la República del Perú y correo de contacto{' '}
            <a href="mailto:soporte@tukifact.com.pe">soporte@tukifact.com.pe</a>.
          </p>

          <h2>3. Descripción del servicio</h2>
          <p>
            TukiFact es una plataforma SaaS multitenant de facturación electrónica que permite a empresas peruanas
            emitir, firmar digitalmente y enviar a SUNAT comprobantes electrónicos (facturas, boletas, notas de
            crédito, notas de débito, guías de remisión, retenciones, percepciones), gestionar cotizaciones,
            recurrentes, clientes, productos, series y catálogos. La Plataforma actúa como herramienta tecnológica; la
            responsabilidad tributaria y comercial de cada documento es del contribuyente emisor.
          </p>

          <h2>4. Requisitos de uso</h2>
          <ul>
            <li>Ser persona natural o jurídica con capacidad legal para contratar.</li>
            <li>Contar con RUC activo y habido, y con clave SOL vigente.</li>
            <li>
              Disponer de un certificado digital válido emitido por una entidad de certificación reconocida por INDECOPI
              para la firma de comprobantes electrónicos.
            </li>
            <li>Proporcionar información veraz, exacta y actualizada al registrarte.</li>
          </ul>

          <h2>5. Cuenta y seguridad</h2>
          <p>
            Eres responsable de mantener la confidencialidad de tus credenciales y de toda actividad realizada bajo tu
            cuenta. Debes notificarnos de inmediato cualquier acceso no autorizado. Podemos suspender o cerrar cuentas
            que presenten riesgos de seguridad o que infrinjan estos Términos.
          </p>

          <h2>6. Planes, pagos y suscripción</h2>
          <p>
            La Plataforma se ofrece bajo planes con distintos cupos mensuales de documentos y funcionalidades. Los
            precios y características de cada plan se publican en la Plataforma y pueden actualizarse con previo aviso
            razonable.
          </p>
          <ul>
            <li>
              <strong>Facturación</strong>: la suscripción se factura por mensualidad adelantada. Al integrarse la
              pasarela de pago, el cargo se realiza automáticamente en la fecha de renovación.
            </li>
            <li>
              <strong>Cupos excedidos</strong>: si superas el cupo de documentos del plan, la emisión adicional puede
              tarifarse según la tarifa publicada o requerir cambio de plan.
            </li>
            <li>
              <strong>Renovación</strong>: la suscripción se renueva de forma automática hasta que la canceles desde tu
              panel.
            </li>
            <li>
              <strong>Reembolsos</strong>: los pagos no son reembolsables salvo disposición legal en contrario o cuando
              la falla del servicio sea imputable a TukiFact y nos lo notifiques dentro de los 7 días siguientes al
              cobro.
            </li>
          </ul>

          <h2>7. Uso aceptable</h2>
          <p>Te comprometes a no:</p>
          <ul>
            <li>Emitir comprobantes con datos falsos o sin sustento real de la operación.</li>
            <li>Usar la Plataforma para evadir obligaciones tributarias o cometer fraude.</li>
            <li>
              Vulnerar la seguridad de la Plataforma, realizar ingeniería inversa, escaneos no autorizados o
              denegaciones de servicio.
            </li>
            <li>Suplantar a otra persona o empresa.</li>
            <li>Revender o sublicenciar el servicio sin nuestro consentimiento escrito.</li>
            <li>Usar bots automatizados para superar los límites de uso o cupos contratados.</li>
          </ul>
          <p>El incumplimiento puede dar lugar a la suspensión inmediata de la cuenta sin reembolso.</p>

          <h2>8. Datos del cliente y comprobantes</h2>
          <p>
            Tú conservas la titularidad sobre los datos que cargas a la Plataforma (clientes, productos, comprobantes,
            etc.). Nos otorgas una licencia limitada, no exclusiva y revocable para procesarlos con el único objeto de
            prestarte el servicio, incluyendo su envío a SUNAT y a tus clientes finales cuando lo dispongas.
          </p>

          <h2>9. Disponibilidad y mantenimiento</h2>
          <p>
            Hacemos esfuerzos comercialmente razonables para mantener la Plataforma disponible 24/7, pero no
            garantizamos disponibilidad ininterrumpida. Podemos realizar mantenimiento programado avisándote con
            anticipación razonable, y mantenimiento de emergencia cuando sea necesario.
          </p>

          <h2>10. Propiedad intelectual</h2>
          <p>
            La Plataforma, su código fuente, diseño, marca, logotipo y documentación son propiedad de Tukituki Solution
            S.A.C. o de sus licenciantes. Estos Términos no te transfieren ningún derecho sobre dicha propiedad
            intelectual más allá del derecho de uso limitado durante la vigencia de tu suscripción.
          </p>

          <h2>11. Servicios de terceros</h2>
          <p>
            La Plataforma se integra con servicios de SUNAT, proveedores de envío de correo, almacenamiento y, en su
            momento, pasarelas de pago e inicio de sesión federado (Google, Microsoft). El uso de esos servicios se rige
            por sus propios términos y políticas. No somos responsables por interrupciones, errores o decisiones de
            esos terceros.
          </p>

          <h2>12. Limitación de responsabilidad</h2>
          <p>
            En la máxima medida permitida por la ley, TukiFact no será responsable por daños indirectos, lucro cesante,
            pérdida de oportunidades comerciales o de datos derivados del uso o imposibilidad de uso de la Plataforma.
            Nuestra responsabilidad total acumulada frente a cualquier reclamo no excederá el monto efectivamente
            pagado por el cliente durante los doce meses anteriores al hecho que generó el reclamo.
          </p>
          <p>
            La Plataforma se ofrece &quot;tal cual&quot; y sin garantías implícitas sobre comerciabilidad, idoneidad
            para un propósito particular o no infracción.
          </p>

          <h2>13. Indemnidad</h2>
          <p>
            Te comprometes a mantener indemne a TukiFact frente a reclamos de terceros derivados de (i) tu uso indebido
            de la Plataforma, (ii) el contenido de los comprobantes que emitas y (iii) la infracción de obligaciones
            legales o de estos Términos.
          </p>

          <h2>14. Suspensión y terminación</h2>
          <p>
            Podemos suspender o cancelar tu cuenta por incumplimiento material de estos Términos, falta de pago,
            requerimiento de autoridad competente, o por riesgos de seguridad o integridad de la Plataforma. Tú puedes
            cancelar tu cuenta en cualquier momento desde el panel. Tras la terminación, conservaremos los datos por
            los plazos exigidos por la normativa tributaria y luego los eliminaremos o anonimizaremos.
          </p>

          <h2>15. Modificaciones a los Términos</h2>
          <p>
            Podemos modificar estos Términos en cualquier momento. Los cambios materiales se comunicarán con al menos
            15 días de anticipación al correo registrado. Continuar usando la Plataforma después de la entrada en
            vigor implica aceptación.
          </p>

          <h2>16. Ley aplicable y jurisdicción</h2>
          <p>
            Estos Términos se rigen por las leyes de la República del Perú. Cualquier controversia será sometida a la
            jurisdicción de los jueces y tribunales de Lima Cercado, salvo que la normativa de protección al consumidor
            disponga lo contrario.
          </p>

          <h2>17. Contacto</h2>
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
          <Link href="/privacy" className="hover:text-foreground">
            Política de Privacidad
          </Link>
          <Link href="/terms" className="hover:text-foreground">
            Términos del Servicio
          </Link>
          <span className="ml-auto">© {new Date().getFullYear()} Tukituki Solution S.A.C.</span>
        </div>
      </footer>
    </div>
  );
}
