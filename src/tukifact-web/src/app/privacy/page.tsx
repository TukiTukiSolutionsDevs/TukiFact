import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Política de Privacidad — TukiFact',
  description:
    'Política de Privacidad de TukiFact, plataforma SaaS de facturación electrónica para Perú, operada por Tukituki Solution S.A.C.',
};

const LAST_UPDATED = '28 de mayo de 2026';

export default function PrivacyPage() {
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
          <h1>Política de Privacidad</h1>
          <p className="text-muted-foreground">Última actualización: {LAST_UPDATED}</p>

          <h2>1. Identidad del responsable del tratamiento</h2>
          <p>
            <strong>Tukituki Solution S.A.C.</strong> (en adelante, &quot;TukiFact&quot;, &quot;nosotros&quot;), con RUC
            20613614509, es responsable del tratamiento de los datos personales recolectados a través del sitio{' '}
            <a href="https://tukifact.com.pe">https://tukifact.com.pe</a> y los servicios asociados.
          </p>
          <p>
            Para ejercer tus derechos o resolver dudas sobre esta política, puedes contactarnos a:{' '}
            <a href="mailto:legal@tukifact.com.pe">legal@tukifact.com.pe</a>.
          </p>

          <h2>2. Datos que recolectamos</h2>
          <p>Para operar TukiFact recolectamos los siguientes datos:</p>
          <ul>
            <li>
              <strong>Datos de cuenta</strong>: nombre completo, correo electrónico, contraseña cifrada, rol dentro de la
              empresa.
            </li>
            <li>
              <strong>Datos de empresa (tenant)</strong>: RUC, razón social, nombre comercial, domicilio fiscal, ubigeo,
              logotipo, credenciales SUNAT (usuario SOL y contraseña, cifrados con clave maestra).
            </li>
            <li>
              <strong>Certificado digital</strong>: el archivo .pfx y su contraseña se almacenan cifrados en reposo y
              solo se utilizan para firmar comprobantes electrónicos por encargo tuyo.
            </li>
            <li>
              <strong>Datos de facturación electrónica</strong>: comprobantes emitidos, clientes, productos, series,
              guías de remisión, retenciones, percepciones y cualquier documento procesado a través del servicio.
            </li>
            <li>
              <strong>Datos técnicos</strong>: direcciones IP, identificadores de sesión, registros de auditoría
              (acceso, cambios y emisiones), información del navegador y dispositivo.
            </li>
            <li>
              <strong>Datos de pago</strong>: cuando integremos pasarela de pago, recolectaremos el medio de pago a
              través del procesador autorizado; <em>no almacenaremos</em> directamente datos completos de tarjeta.
            </li>
          </ul>

          <h2>3. Finalidades del tratamiento</h2>
          <ul>
            <li>Proveerte el servicio de emisión, firma y envío de comprobantes electrónicos ante la SUNAT.</li>
            <li>Gestionar tu cuenta, suscripción y facturación dentro de TukiFact.</li>
            <li>Cumplir obligaciones legales (Ley 29733, Reglamento SUNAT, normativa tributaria peruana).</li>
            <li>Brindar soporte técnico cuando lo solicites.</li>
            <li>Mejorar la plataforma mediante análisis estadísticos agregados y anónimos.</li>
            <li>Detección de fraude, abuso o uso indebido del servicio.</li>
            <li>Comunicaciones operativas (cambios de servicio, vencimientos, alertas de seguridad).</li>
          </ul>

          <h2>4. Base legal del tratamiento</h2>
          <p>
            El tratamiento se basa en (i) la ejecución del contrato de servicio que celebras al registrarte, (ii) el
            cumplimiento de obligaciones legales aplicables a la facturación electrónica en Perú, y (iii) tu
            consentimiento expreso para finalidades adicionales (por ejemplo, comunicaciones comerciales, las cuales
            podrás retirar en cualquier momento).
          </p>

          <h2>5. Con quién compartimos tus datos</h2>
          <ul>
            <li>
              <strong>SUNAT</strong>: los comprobantes firmados se envían a la Administración Tributaria como exige la
              normativa.
            </li>
            <li>
              <strong>Proveedores de infraestructura</strong>: servicios de hosting, almacenamiento de archivos (XML,
              PDF, CDR), envío de correo electrónico transaccional y monitoreo, sujetos a acuerdos de confidencialidad.
            </li>
            <li>
              <strong>Pasarela de pago</strong>: cuando esté activa, los datos del medio de pago serán procesados por
              terceros con certificación PCI-DSS.
            </li>
            <li>
              <strong>Autoridades</strong>: cuando una orden judicial o requerimiento legal así lo exija.
            </li>
          </ul>
          <p>No vendemos ni cedemos tus datos personales a terceros con fines comerciales.</p>

          <h2>6. Transferencia internacional</h2>
          <p>
            Algunos proveedores de infraestructura podrían ubicarse fuera de Perú. En tales casos, exigimos garantías
            contractuales que aseguren un nivel de protección equivalente al previsto por la Ley 29733.
          </p>

          <h2>7. Plazo de conservación</h2>
          <p>
            Los datos se conservan mientras tengas una cuenta activa y por los plazos adicionales requeridos por la
            normativa tributaria peruana (mínimo 5 años para comprobantes electrónicos y su sustento). Una vez vencidos
            esos plazos, los datos son eliminados o anonimizados.
          </p>

          <h2>8. Seguridad</h2>
          <p>
            Aplicamos medidas técnicas y organizativas razonables: cifrado en tránsito (TLS), cifrado en reposo para
            certificados y credenciales sensibles, control de acceso por roles, aislamiento por tenant a nivel de base
            de datos (Row Level Security), registro de auditoría y monitoreo continuo. Pese a ello, ninguna plataforma
            es 100% inmune; te pedimos elegir contraseñas robustas y proteger tus credenciales.
          </p>

          <h2>9. Tus derechos (ARCO)</h2>
          <p>De acuerdo con la Ley 29733, tienes derecho a:</p>
          <ul>
            <li>
              <strong>Acceso</strong>: conocer qué datos tuyos tratamos.
            </li>
            <li>
              <strong>Rectificación</strong>: corregir datos inexactos.
            </li>
            <li>
              <strong>Cancelación</strong>: solicitar la supresión cuando ya no sean necesarios.
            </li>
            <li>
              <strong>Oposición</strong>: oponerte al tratamiento por motivos legítimos.
            </li>
            <li>
              <strong>Portabilidad</strong>: recibir tus datos en un formato estructurado y legible.
            </li>
            <li>
              <strong>Revocación del consentimiento</strong>: cuando el tratamiento se base en él.
            </li>
          </ul>
          <p>
            Para ejercerlos, escríbenos a{' '}
            <a href="mailto:legal@tukifact.com.pe">legal@tukifact.com.pe</a> con el asunto &quot;Ejercicio de derechos
            ARCO&quot; e identifícate adecuadamente. Responderemos en el plazo legal previsto. Si no estás satisfecho
            con la respuesta, puedes presentar reclamo ante la <strong>Autoridad Nacional de Protección de Datos
            Personales (ANPD)</strong> del Ministerio de Justicia y Derechos Humanos del Perú.
          </p>

          <h2>10. Cookies y tecnologías similares</h2>
          <p>
            Usamos cookies estrictamente necesarias para la autenticación y el funcionamiento del servicio, y cookies de
            preferencias (por ejemplo, tema claro/oscuro). No usamos cookies publicitarias de terceros. Puedes
            configurar tu navegador para bloquearlas, pero algunas funciones podrían dejar de operar.
          </p>

          <h2>11. Inicio de sesión con terceros (Google, Microsoft)</h2>
          <p>
            Si eliges iniciar sesión con tu cuenta de Google o Microsoft, recibimos únicamente tu correo electrónico,
            nombre y, opcionalmente, foto de perfil pública. No accedemos a tus contactos, archivos ni a ningún otro
            dato fuera del alcance del consentimiento que otorgas en el proveedor.
          </p>

          <h2>12. Cambios a esta política</h2>
          <p>
            Podemos actualizar esta política para reflejar cambios legales o de servicio. Publicaremos la nueva versión
            en esta misma página indicando la fecha de actualización; los cambios materiales serán notificados por
            correo electrónico al titular de la cuenta.
          </p>

          <h2>13. Contacto</h2>
          <p>
            Tukituki Solution S.A.C. — RUC 20613614509
            <br />
            Correo: <a href="mailto:legal@tukifact.com.pe">legal@tukifact.com.pe</a>
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
