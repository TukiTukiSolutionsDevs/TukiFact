import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
RECURRING_INVOICE_URL_TEMPLATE = f"{BASE_URL}/v1/recurring-invoices/{{}}"

def test_recurring_invoice_get_by_id():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        # Authenticate to get accessToken
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=30)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid accessToken"

        headers = {
            "Authorization": f"Bearer {access_token}"
        }

        # The recurring invoice ID from TC069 as per instructions (hardcoded since not provided)
        recurring_invoice_id = "e82c950c-2257-4b32-b7d4-0a6604851753"

        url = RECURRING_INVOICE_URL_TEMPLATE.format(recurring_invoice_id)

        # GET recurring invoice by ID
        resp = requests.get(url, headers=headers, timeout=30)
        assert resp.status_code == 200, f"Expected 200 OK but got {resp.status_code}"
        data = resp.json()

        # Validate required fields
        assert data.get("frequency") == "monthly", f"Expected frequency 'monthly', got {data.get('frequency')}"
        assert data.get("customerName") == "Recurrente SAC", f"Expected customerName 'Recurrente SAC', got {data.get('customerName')}"
        assert data.get("status") == "active", f"Expected status 'active', got {data.get('status')}"

    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

test_recurring_invoice_get_by_id()