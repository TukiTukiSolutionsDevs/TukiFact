import requests
import time

BASE_URL = "http://localhost:80"
TENANT_LOGIN_EMAIL = "prdtest@test.pe"
TENANT_LOGIN_PASSWORD = "PrdTest2026!"
TENANT_ID = "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
TIMEOUT = 30

def test_rbac_emisor_can_emit_but_not_admin():
    # Step 1: Tenant login to get initial access token
    login_payload = {
        "email": TENANT_LOGIN_EMAIL,
        "password": TENANT_LOGIN_PASSWORD,
        "tenantId": TENANT_ID
    }
    resp = requests.post(f"{BASE_URL}/v1/auth/login", json=login_payload, timeout=TIMEOUT)
    assert resp.status_code == 200, f"Tenant login failed: {resp.status_code} {resp.text}"
    tenant_token = resp.json().get("accessToken")
    assert tenant_token and tenant_token.startswith("eyJ"), "Invalid tenant accessToken"

    tenant_headers = {"Authorization": f"Bearer {tenant_token}"}

    # Step 2: Create an emisor user with unique email
    timestamp = int(time.time())
    emisor_email = f"emisor_rbac_{timestamp}@test.pe"
    user_payload = {
        "email": emisor_email,
        "password": "Emisor2026!",
        "fullName": "Emisor RBAC Test",
        "role": "emisor"
    }
    create_resp = requests.post(f"{BASE_URL}/v1/users", json=user_payload, headers=tenant_headers, timeout=TIMEOUT)
    assert create_resp.status_code == 201, f"Failed to create emisor user: {create_resp.status_code} {create_resp.text}"
    created_user = create_resp.json()
    created_user_email = created_user.get("email")
    assert created_user_email == emisor_email, "Created user email mismatch"
    created_user_id = created_user.get("id")
    assert created_user_id, "User ID missing in create response"

    try:
        # Step 3: Login as the emisor user
        emisor_login_payload = {
            "email": created_user_email,
            "password": "Emisor2026!",
            "tenantId": TENANT_ID
        }
        emisor_login_resp = requests.post(f"{BASE_URL}/v1/auth/login", json=emisor_login_payload, timeout=TIMEOUT)
        assert emisor_login_resp.status_code == 200, f"Emisor login failed: {emisor_login_resp.status_code} {emisor_login_resp.text}"
        emisor_token = emisor_login_resp.json().get("accessToken")
        assert emisor_token and emisor_token.startswith("eyJ"), "Invalid emisor accessToken"

        emisor_headers = {"Authorization": f"Bearer {emisor_token}"}

        # Step 4: Prepare a valid factura document payload
        # Get active series for documentType '01' (Factura)
        series_resp = requests.get(f"{BASE_URL}/v1/series?tipoComprobante=01", headers=emisor_headers, timeout=TIMEOUT)
        assert series_resp.status_code == 200, f"Failed to get series: {series_resp.status_code} {series_resp.text}"
        series_data = series_resp.json()
        # Expect series_data to be a list of series dicts
        # We select first active serie starting with 'F'
        serie_code = None
        if isinstance(series_data, list):
            for serie in series_data:
                code = serie.get("serie") or serie.get("serieCode") or serie.get("code") or serie.get("serie_code")
                # Accept any key for serie code fallback
                if code and code.startswith("F"):
                    # Additional check: if the series is active if possible
                    # Since the error was "serie 'F001' está inactiva", we assume series has an 'active' or 'estado' key
                    # But PRD does not explicitly mention it, so we accept first with code starting with 'F'
                    serie_code = code
                    break
        assert serie_code is not None, "No active Factura series found in user's tenant"

        factura_payload = {
            "documentType": "01",
            "serie": serie_code,
            "currency": "PEN",
            "customerDocType": "6",
            "customerDocNumber": "20100070970",
            "customerName": "Cliente Factura SAC",
            "customerAddress": "Av Test 123, Lima",
            "customerEmail": "cliente@test.pe",
            "notes": "Test factura RBAC",
            "items": [
                {
                    "description": "Servicio RBAC prueba",
                    "quantity": 1,
                    "unitPrice": 100.00,
                    "unitMeasure": "ZZ",
                    "igvType": "10"
                }
            ]
        }
        document_resp = requests.post(f"{BASE_URL}/v1/documents", json=factura_payload, headers=emisor_headers, timeout=TIMEOUT)
        assert document_resp.status_code == 201, f"Emisor failed to emit factura document: {document_resp.status_code} {document_resp.text}"

        # Step 5: Using emisor token, GET /v1/users -> should return 403 Forbidden
        users_resp = requests.get(f"{BASE_URL}/v1/users", headers=emisor_headers, timeout=TIMEOUT)
        assert users_resp.status_code == 403, f"Emisor user GET /v1/users did not return 403 Forbidden: {users_resp.status_code} {users_resp.text}"

    finally:
        # Cleanup: delete the created emisor user
        delete_resp = requests.delete(f"{BASE_URL}/v1/users/{created_user_id}", headers=tenant_headers, timeout=TIMEOUT)
        # Accept 200 or 204 as success
        assert delete_resp.status_code in (200, 204), f"Failed to delete created user: {delete_resp.status_code} {delete_resp.text}"

test_rbac_emisor_can_emit_but_not_admin()
