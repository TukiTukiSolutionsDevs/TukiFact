import requests
import sys

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
QUOTATION_STATUS_URL_TEMPLATE = f"{BASE_URL}/v1/quotations/{{quotation_id}}/status"

TENANT_LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}

# Quotation ID from TC063 as per instructions
QUOTATION_ID_TC063 = "a7941b61-0290-484c-90d5-9d221ad36eb7"  # This ID must be replaced with the real saved one if stored externally

def test_quotation_update_status_to_sent():
    # Step 1: Authenticate tenant user to get access token
    try:
        login_resp = requests.post(LOGIN_URL, json=TENANT_LOGIN_PAYLOAD, timeout=30)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}: {login_resp.text}"
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken format"
    except Exception as e:
        print(f"Authentication failed: {e}", file=sys.stderr)
        raise

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: Update quotation status to 'sent'
    quotation_status_url = QUOTATION_STATUS_URL_TEMPLATE.format(quotation_id=QUOTATION_ID_TC063)
    payload = {"status": "sent"}
    try:
        resp = requests.put(quotation_status_url, json=payload, headers=headers, timeout=30)
        assert resp.status_code == 200, f"Status update failed with status {resp.status_code}: {resp.text}"
        resp_json = resp.json()
        status = resp_json.get("status")
        assert status == "sent", f"Expected status 'sent', got '{status}'"
    except Exception as e:
        print(f"Failed updating quotation status: {e}", file=sys.stderr)
        raise


test_quotation_update_status_to_sent()