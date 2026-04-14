import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
RECURRING_INVOICES_URL = f"{BASE_URL}/v1/recurring-invoices"


def test_recurring_invoice_cancel():
    # Step 1: Authenticate
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=30)
        login_resp.raise_for_status()
    except Exception as e:
        assert False, f"Login request failed: {e}"
    login_data = login_resp.json()
    access_token = login_data.get("accessToken")
    assert access_token and isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken in login response"

    headers = {"Authorization": f"Bearer {access_token}"}

    # Step 2: Create another recurring invoice
    # Use unique name with timestamp for test independence
    recurring_payload = {
        "documentType": "03",
        "serie": "B001",
        "customerDocType": "1",
        "customerDocNumber": "71234567",
        "customerName": f"Cancelar-{int(time.time())}",
        "currency": "PEN",
        "frequency": "weekly",
        "dayOfWeek": 1,
        "startDate": "2026-05-01",
        "items": [
            {
                "description": "Servicio semanal",
                "quantity": 1,
                "unitPrice": 100.00,
                "unitMeasure": "ZZ",
                "igvType": "10"
            }
        ]
    }
    recurring_id = None
    try:
        create_resp = requests.post(RECURRING_INVOICES_URL, json=recurring_payload, headers=headers, timeout=30)
        create_resp.raise_for_status()
        create_data = create_resp.json()
        recurring_id = create_data.get("id")
        assert recurring_id and isinstance(recurring_id, str), "No valid 'id' returned on recurring invoice creation"

        # Step 3: Cancel the newly created recurring invoice
        cancel_url = f"{RECURRING_INVOICES_URL}/{recurring_id}"
        update_payload = {"status": "cancelled"}

        cancel_resp = requests.put(cancel_url, json=update_payload, headers=headers, timeout=30)
        cancel_resp.raise_for_status()
        cancel_data = cancel_resp.json()

        # Validate response status
        status = cancel_data.get("status")
        next_emission_date = cancel_data.get("nextEmissionDate")
        assert status == "cancelled", f"Expected status 'cancelled', got '{status}'"
        assert next_emission_date is None, f"Expected 'nextEmissionDate' to be null, got '{next_emission_date}'"

    finally:
        # Cleanup: Delete the created recurring invoice if it exists
        if recurring_id:
            try:
                del_resp = requests.delete(f"{RECURRING_INVOICES_URL}/{recurring_id}", headers=headers, timeout=30)
                # Accept 200, 204 or 404 (if already deleted)
                assert del_resp.status_code in (200, 204, 404)
            except Exception:
                pass


test_recurring_invoice_cancel()