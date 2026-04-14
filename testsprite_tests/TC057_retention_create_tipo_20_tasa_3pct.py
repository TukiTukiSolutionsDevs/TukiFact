import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
RETENTIONS_ENDPOINT = "/v1/retentions"
TIMEOUT = 30

def test_retention_create_tipo_20_tasa_3pct():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    retention_payload = {
        "serie": "R001",
        "supplierDocType": "6",
        "supplierDocNumber": "20100070970",
        "supplierName": "Proveedor Retenido SAC",
        "supplierAddress": "Av Proveedor 456",
        "regimeCode": "01",
        "retentionPercent": 3.00,
        "currency": "PEN",
        "notes": "Retención de prueba PRD",
        "references": [
            {
                "documentType": "01",
                "documentNumber": "F001-00000100",
                "documentDate": "2026-04-01",
                "invoiceAmount": 5000.00,
                "invoiceCurrency": "PEN",
                "paymentDate": "2026-04-10",
                "paymentNumber": 1,
                "paymentAmount": 5000.00
            },
            {
                "documentType": "01",
                "documentNumber": "F001-00000101",
                "documentDate": "2026-04-02",
                "invoiceAmount": 3000.00,
                "invoiceCurrency": "PEN",
                "paymentDate": "2026-04-10",
                "paymentNumber": 1,
                "paymentAmount": 3000.00
            }
        ]
    }
    retention_id = None
    headers = {}
    try:
        # Step 1: Authenticate and get Bearer token
        login_response = requests.post(
            f"{BASE_URL}{LOGIN_ENDPOINT}",
            json=login_payload,
            timeout=TIMEOUT
        )
        assert login_response.status_code == 200, f"Login failed with status {login_response.status_code}"
        login_json = login_response.json()
        access_token = login_json.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid accessToken received"
        
        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }
        
        # Step 2: Create retention
        retention_response = requests.post(
            f"{BASE_URL}{RETENTIONS_ENDPOINT}",
            json=retention_payload,
            headers=headers,
            timeout=TIMEOUT
        )
        assert retention_response.status_code == 201, f"Retention creation failed with status {retention_response.status_code}"
        retention_json = retention_response.json()
        
        # Validate required fields in response
        retention_id = retention_json.get("id")
        assert retention_id, "Retention response does not contain 'id'"
        
        full_number = retention_json.get("fullNumber")
        assert full_number and full_number.startswith("R001-"), f"fullNumber invalid or does not start with 'R001-': {full_number}"
        
        regime_code = retention_json.get("regimeCode")
        assert regime_code == "01", f"regimeCode is '{regime_code}', expected '01'"
        
        retention_percent = retention_json.get("retentionPercent")
        assert retention_percent == 3.00, f"retentionPercent is {retention_percent}, expected 3.00"
        
        total_invoice_amount = retention_json.get("totalInvoiceAmount")
        assert isinstance(total_invoice_amount, (int, float)) and abs(total_invoice_amount - 8000.00) < 0.01, \
            f"totalInvoiceAmount is {total_invoice_amount}, expected 8000.00"
        
        total_retained = retention_json.get("totalRetained")
        assert isinstance(total_retained, (int, float)) and abs(total_retained - 240.00) < 0.01, \
            f"totalRetained is {total_retained}, expected 240.00"
        
        total_paid = retention_json.get("totalPaid")
        assert isinstance(total_paid, (int, float)) and abs(total_paid - 7760.00) < 0.01, \
            f"totalPaid is {total_paid}, expected 7760.00"
        
        references = retention_json.get("references")
        assert isinstance(references, list) and len(references) == 2, \
            f"references array length is {len(references) if references is not None else 'None'}, expected 2"
        
    finally:
        # Cleanup if possible - delete the created retention to keep tests independent
        if retention_id:
            try:
                delete_response = requests.delete(
                    f"{BASE_URL}{RETENTIONS_ENDPOINT}/{retention_id}",
                    headers=headers,
                    timeout=TIMEOUT
                )
                # Accept 200 or 204 on delete
                assert delete_response.status_code in (200, 204), f"Failed to delete retention id {retention_id}"
            except Exception:
                # In case of failure in delete, do not raise to not mask test result
                pass

test_retention_create_tipo_20_tasa_3pct()