import requests

BASE_URL = "http://localhost:80"
AUTH_URL = f"{BASE_URL}/v1/auth/login"
QUOTATIONS_URL = f"{BASE_URL}/v1/quotations"

login_payload = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}

# Quotation create payload (for TC063) - used to create the quotation if needed
quotation_create_payload = {
    "customerDocType": "6",
    "customerDocNumber": "20100070970",
    "customerName": "Cotización SAC",
    "customerAddress": "Av Cotización 100",
    "customerEmail": "cotizacion@test.pe",
    "customerPhone": "01-1111111",
    "currency": "PEN",
    "validUntil": "2026-05-15",
    "notes": "Cotización de prueba",
    "termsAndConditions": "Pago a 30 días",
    "items": [
        {
            "productCode": "PROD-001",
            "description": "Laptop HP",
            "quantity": 5,
            "unitMeasure": "NIU",
            "unitPrice": 2542.37,
            "igvType": "10",
            "discount": 0
        },
        {
            "productCode": "SERV-001",
            "description": "Instalación y configuración",
            "quantity": 5,
            "unitMeasure": "ZZ",
            "unitPrice": 200.00,
            "igvType": "10",
            "discount": 100.00
        }
    ]
}

def test_quotation_convert_to_invoice():
    session = requests.Session()
    try:
        # Step 1: Authenticate tenant to get Bearer token
        resp = session.post(AUTH_URL, json=login_payload, timeout=30)
        assert resp.status_code == 200, f"Login failed with status {resp.status_code}"
        auth_data = resp.json()
        access_token = auth_data.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken format"
        headers = {"Authorization": f"Bearer {access_token}", "Content-Type": "application/json"}
        
        # Step 2: Create a quotation (simulate TC063) to get a quotation ID
        create_resp = session.post(QUOTATIONS_URL, json=quotation_create_payload, headers=headers, timeout=30)
        assert create_resp.status_code == 201, f"Failed to create quotation, status {create_resp.status_code}"
        quotation = create_resp.json()
        quotation_id = quotation.get("id")
        assert quotation_id, "Quotation creation response missing 'id'"
        
        # Step 3: Convert quotation to invoice
        convert_url = f"{QUOTATIONS_URL}/{quotation_id}/convert-to-invoice"
        convert_payload = {
            "serie": "F001",
            "documentType": "01"
        }
        convert_resp = session.post(convert_url, json=convert_payload, headers=headers, timeout=30)
        assert convert_resp.status_code == 200, f"Convert to invoice failed with status {convert_resp.status_code}"
        convert_data = convert_resp.json()
        
        quotation_obj = convert_data.get("quotation")
        invoice_obj = convert_data.get("invoice")
        assert quotation_obj is not None, "Response missing 'quotation' object"
        assert invoice_obj is not None, "Response missing 'invoice' object"
        
        # Validate quotation status and invoiceDocumentNumber
        assert quotation_obj.get("status") == "invoiced", "Quotation status not 'invoiced'"
        invoice_doc_num = quotation_obj.get("invoiceDocumentNumber")
        assert invoice_doc_num is not None and invoice_doc_num != "", "quotation.invoiceDocumentNumber is null or empty"
        
        # Validate invoice fullNumber starts with "F001-"
        invoice_full_num = invoice_obj.get("fullNumber")
        assert invoice_full_num is not None and invoice_full_num.startswith("F001-"), "invoice.fullNumber does not start with 'F001-'"
        
        # Step 4: Verify quotation can only be converted once - attempt second conversion
        second_convert_resp = session.post(convert_url, json=convert_payload, headers=headers, timeout=30)
        assert second_convert_resp.status_code == 400, f"Second convert-to-invoice expected 400 but got {second_convert_resp.status_code}"
        second_error = second_convert_resp.json()
        # Extract error message robustly
        error_message = ""
        if isinstance(second_error, dict):
            if "error" in second_error:
                if isinstance(second_error["error"], str):
                    error_message = second_error["error"]
                elif isinstance(second_error["error"], dict):
                    error_message = second_error["error"].get("message", "")
            elif "message" in second_error:
                error_message = second_error["message"]
            else:
                # fallback to any string found in the dict
                for v in second_error.values():
                    if isinstance(v, str):
                        error_message = v
                        break
        error_message = error_message.lower()
        assert any(keyword in error_message for keyword in ["already", "invoiced", "once", "duplicate", "converted", "error"]), \
            "Error message on second conversion missing expected text"
    finally:
        # Cleanup attempt - DELETE quotation if supported (not mandatory as per instructions)
        # The PRD and instructions mention DELETE may return 405; here we try but ignore failure silently
        if 'quotation_id' in locals() and quotation_id:
            try:
                del_resp = session.delete(f"{QUOTATIONS_URL}/{quotation_id}", headers=headers, timeout=10)
                # Accept 204 or 405 or 404 or 200 as OK for cleanup
                if del_resp.status_code not in (200, 204, 404, 405):
                    pass
            except Exception:
                pass

test_quotation_convert_to_invoice()
