import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
QUOTATIONS_URL = f"{BASE_URL}/v1/quotations"
TIMEOUT = 30

def test_quotation_convert_already_invoiced_returns_400():
    # Step 0: Authenticate and get token
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=TIMEOUT)
        login_resp.raise_for_status()
    except Exception as e:
        assert False, f"Login failed: {e}"
    login_data = login_resp.json()
    access_token = login_data.get("accessToken")
    assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid access token"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    quotation_payload = {
        "customerDocType": "6",
        "customerDocNumber": "20888999001",
        "customerName": "Test Corp",
        "currency": "PEN",
        "items": [
            {
                "productCode": "Q68",
                "description": "Quotation item",
                "quantity": 1,
                "unitMeasure": "NIU",
                "unitPrice": 100.00,
                "igvType": "10"
            }
        ]
    }

    quotation_id = None
    try:
        # Step 1: Create quotation
        create_resp = requests.post(QUOTATIONS_URL, json=quotation_payload, headers=headers, timeout=TIMEOUT)
        create_resp.raise_for_status()
        create_data = create_resp.json()
        quotation_id = create_data.get("id")
        assert quotation_id, "Quotation ID not returned"

        # Step 2: Convert to invoice (first time)
        convert_url = f"{QUOTATIONS_URL}/{quotation_id}/convert-to-invoice"
        convert_payload = {"serie": "F001"}
        convert_resp_1 = requests.post(convert_url, json=convert_payload, headers=headers, timeout=TIMEOUT)
        assert convert_resp_1.status_code == 200, f"Expected 200 on first convert, got {convert_resp_1.status_code}"

        # Step 3: Convert to invoice AGAIN (second time)
        convert_resp_2 = requests.post(convert_url, json=convert_payload, headers=headers, timeout=TIMEOUT)
        assert convert_resp_2.status_code == 400, f"Expected 400 on second convert, got {convert_resp_2.status_code}"
        error_data = convert_resp_2.json()
        error_message = error_data.get("error") or error_data.get("message") or ""
        assert ("already invoiced" in error_message.lower() 
                or "ya facturado" in error_message.lower() 
                or "invoiced" in error_message.lower()
                or "convertida a factura" in error_message.lower()), \
            f"Expected error message about already invoiced, got: {error_message}"

    finally:
        # Clean up: No DELETE on quotations mentioned; leaving data
        pass

test_quotation_convert_already_invoiced_returns_400()
