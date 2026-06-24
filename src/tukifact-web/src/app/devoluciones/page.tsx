import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Política de Devoluciones y Reembolsos — TukiFact',
  description:
    'Condiciones para solicitar devoluciones o reembolsos en TukiFact, plataforma SaaS de facturación electrónica para Perú, operada por Tukituki Solution S.A.C.',
};

const LAST_UPDATED = '10 de junio de 2026';

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
          <h1>Política de Devoluciones y Reembolsos</h1>
          <p className="text-muted-foreground">Última actualización: {LAST_UPDATED}</p>

          <h2>1. Ámbito de aplicación</h2>
          <p>
            Esta política aplica a todos los pagos de suscripciones y servicios realizados en TukiFact, plataforma
            operada por <strong>Tukituki Solution S.A.C.</strong> (RUC 20613614509). Los reembolsos se procesan
            únicamente sobre cargos realizados a través de los medios de pago habilitados en la Plataforma.
          </p>

          <h2>2. Casos en los que procede el reembolso</h2>
          <ul>
            <li>
              <strong>Cargo duplicado:</strong> si se realizaron dos o más cargos por el mismo período de facturación
              debido a un error técnico, reembolsamos el o los cargos duplicados en su totalidad.
            </li>
            <li>
              <strong>Falla imputable al servicio:</strong> si la Plataforma presentó una indisponibilidad mayor a
              72 horas continuas durante un período de facturación activo, tienes derecho a un crédito proporcional
              al tiempo afectado o a un reembolso parcial según el caso.
            </li>
            <li>
              <strong>Cargo no reconocido:</strong> si no reconoces un cargo y puedes acreditar que no autorizaste
              la suscripción, atenderemos el caso dentro de los plazos indicados.
            </li>
            <li>
              <strong>Cancelación dentro del período de prueba:</strong> si la Plataforma ofrece un período de prueba
              gratuito y fuiste cobrado antes de su vencimiento, reembolsamos el importe íntegro.
            </li>
          </ul>

          <h2>3. Casos en los que NO procede el reembolso</h2>
          <ul>
            <li>
              Cancelación de la suscripción vigente una vez iniciado el período de facturación, salvo los casos
              del punto 2.
            </li>
            <li>
              Documentos electrónicos ya enviados a SUNAT, dado que la operación genera efectos tributarios
              irreversibles.
            </li>
            <li>Uso parcial o no uso del servicio durante el período contratado.</li>
            <li>Cargos realizados correctamente según los planes y tarifas publicadas.</li>
          </ul>

          <h2>4. Cómo solicitar un reembolso</h2>
          <p>
            Escríbenos a{' '}
            <a href="mailto:soporte@tukifact.com.pe">soporte@tukifact.com.pe</a> con el asunto{' '}
            <strong>&quot;Solicitud de Reembolso&quot;</strong>, indicando:
          </p>
          <ul>
            <li>Nombre completo y RUC de la empresa.</li>
            <li>Correo electrónico registrado en TukiFact.</li>
            <li>Fecha y monto del cargo a reembolsar.</li>
            <li>Número de transacción o comprobante de pago (si lo tienes disponible).</li>
            <li>Motivo de la solicitud.</li>
          </ul>

          <h2>5. Plazos de atención</h2>
          <ul>
            <li>
              <strong>Acuse de recibo:</strong> te confirmamos la recepción dentro de las 24 horas hábiles.
            </li>
            <li>
              <strong>Resolución:</strong> respondemos con la decisión dentro de los 7 días hábiles siguientes a
              contar con toda la información requerida.
            </li>
            <li>
              <strong>Acreditación del reembolso:</strong> si procede, el tiempo depende del método de pago
              original:
              <ul>
                <li>Tarjeta de crédito/débito: entre 5 y 15 días hábiles según el banco emisor.</li>
                <li>Transferencia bancaria: entre 3 y 5 días hábiles.</li>
              </ul>
            </li>
          </ul>

          <h2>6. Cancelación de la suscripción</h2>
          <p>
            Puedes cancelar tu suscripción en cualquier momento desde el panel de control (
            <strong>Configuración → Plan → Cancelar suscripción</strong>). La cancelación surte efecto al término
            del período de facturación en curso; podrás continuar usando el servicio hasta esa fecha. La cancelación
            no genera reembolso automático del período vigente salvo que aplique alguno de los supuestos del punto 2.
          </p>

          <h2>7. Disputas y protección al consumidor</h2>
          <p>Si tu solicitud no fue atendida de manera adecuada, puedes:</p>
          <ul>
            <li>
              Presentar un reclamo formal a través de nuestro{' '}
              <Link href="/reclamaciones">Libro de Reclamaciones Virtual</Link>.
            </li>
            <li>
              Contactar con tu entidad bancaria para activar el proceso de contracargo (<em>chargeback</em>) si el
              cargo fue con tarjeta.
            </li>
            <li>
              Acudir al <strong>INDECOPI</strong> si no llegamos a una solución satisfactoria.
            </li>
          </ul>

          <h2>8. Contacto</h2>
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
