import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
RECURRING_INVOICES_URL = f"{BASE_URL}/v1/recurring-invoices"
TIMEOUT = 30

def test_recurring_invoice_create_monthly():
    # Step 1: Authenticate and get Bearer token
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=TIMEOUT)
        login_resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Login request failed: {e}"
    
    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert access_token and isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid or missing accessToken in login response"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: Create recurring invoice (monthly)
    recurring_invoice_payload = {
        "documentType": "01",
        "serie": "F001",
        "customerDocType": "6",
        "customerDocNumber": "20100070970",
        "customerName": "Recurrente SAC",
        "customerAddress": "Av Mensual 100",
        "customerEmail": "recurrente@test.pe",
        "currency": "PEN",
        "frequency": "monthly",
        "dayOfMonth": 15,
        "startDate": "2026-05-01",
        "endDate": "2026-12-31",
        "notes": "Facturación mensual",
        "items": [
            {
                "productCode": "SERV-001",
                "description": "Servicio mensual de soporte",
                "quantity": 1,
                "unitPrice": 500.00,
                "unitMeasure": "ZZ",
                "igvType": "10"
            }
        ]
    }

    try:
        resp = requests.post(RECURRING_INVOICES_URL, json=recurring_invoice_payload, headers=headers, timeout=TIMEOUT)
        assert resp.status_code == 201, f"Expected 201 Created, got {resp.status_code}"
        data = resp.json()
        assert "id" in data and isinstance(data["id"], str) and data["id"], "Missing or invalid 'id' in response"
        assert data.get("frequency") == "monthly", f"Expected 'frequency'='monthly', got {data.get('frequency')}"
        assert data.get("dayOfMonth") == 15, f"Expected 'dayOfMonth'=15, got {data.get('dayOfMonth')}"
        assert isinstance(data.get("startDate"), str) and data.get("startDate"), "Missing or invalid 'startDate'"
        assert isinstance(data.get("endDate"), str) and data.get("endDate"), "Missing or invalid 'endDate'"
        assert isinstance(data.get("nextEmissionDate"), str) and data.get("nextEmissionDate"), "Missing or invalid 'nextEmissionDate'"
        assert data.get("status") == "active", f"Expected 'status'='active', got {data.get('status')}"
        assert data.get("emittedCount") == 0, f"Expected 'emittedCount'=0, got {data.get('emittedCount')}"
    except requests.RequestException as e:
        assert False, f"Recurring invoice creation failed: {e}"


test_recurring_invoice_create_monthly()
